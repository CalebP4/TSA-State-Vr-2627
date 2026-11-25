using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    public enum ScenarioPhase { Intro, Active, Finished }

    /// <summary>
    /// The heart of every scenario scene. Interaction scripts call <see cref="Report"/>
    /// with an action id ("call_911", "finger_sweep", ...). The controller decides if
    /// that action completes a step, is a mistake, or has to wait for an earlier step,
    /// logs it, and raises events that the Training guide / Certification HUD show.
    /// </summary>
    public class ScenarioController : MonoBehaviour
    {
        public static ScenarioController Active { get; private set; }

        [Tooltip("Content for this scenario.")]
        public ScenarioDefinition definition;
        [Tooltip("Mode to use when the scene is played directly (not from the main menu).")]
        public SimMode defaultMode = SimMode.Training;
        [Tooltip("Seconds between the last required step and the results screen.")]
        public float finishDelay = 7f;

        public SimMode Mode { get; private set; }
        public ScenarioPhase Phase { get; private set; } = ScenarioPhase.Intro;
        public float Elapsed { get; private set; }
        /// <summary>True while a pause menu or a question card is open: the clock stops.</summary>
        public bool Held { get; set; }
        public EventLogger Log { get; } = new EventLogger();
        public ScenarioResult Result { get; private set; }
        public IReadOnlyList<StepState> Steps => steps;
        public IReadOnlyList<MistakeRecord> Mistakes => mistakes;
        public bool IsTraining => Mode == SimMode.Training;
        public float TimeLeft => Mathf.Max(0f, definition.certificationTimeLimit - Elapsed);

        // Events for UI / scenario scripts --------------------------------------------
        public event Action Started;
        public event Action<StepState> StepProgressed;          // count went up but not finished
        public event Action<StepState> StepCompleted;
        public event Action<StepState, StepDefinition> OutOfOrder; // step, the earlier step that was skipped
        public event Action<StepDefinition, StepDefinition> Blocked; // step attempted, step required first
        public event Action<MistakeRecord> MistakeMade;
        public event Action AllRequiredDone;
        public event Action<ScenarioResult> Finished;

        readonly List<StepState> steps = new List<StepState>();
        readonly List<MistakeRecord> mistakes = new List<MistakeRecord>();
        float finishAt = -1f;

        void Awake()
        {
            Active = this;
            Mode = SimulationManager.HasSelection ? SimulationManager.Mode : defaultMode;
            Reset();
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        /// <summary>Clears all progress (used on load and by tests).</summary>
        public void Reset()
        {
            steps.Clear();
            mistakes.Clear();
            Log.Clear();
            Elapsed = 0f;
            finishAt = -1f;
            Result = null;
            Phase = ScenarioPhase.Intro;
            if (definition != null)
                foreach (var d in definition.steps) steps.Add(new StepState { def = d });
        }

        public void SetMode(SimMode mode) => Mode = mode;

        /// <summary>Starts the clock. Called when the user closes the intro panel.</summary>
        public void Begin()
        {
            if (Phase != ScenarioPhase.Intro) return;
            Phase = ScenarioPhase.Active;
            Log.Log(Elapsed, LogKind.Milestone, $"Scenario started ({Mode})");
            Started?.Invoke();
        }

        void Update()
        {
            if (Phase != ScenarioPhase.Active || Held) return;
            Elapsed += Time.deltaTime;

            if (finishAt >= 0f && Elapsed >= finishAt) Finish(false);
            else if (Mode == SimMode.Certification && Elapsed >= definition.certificationTimeLimit) Finish(true);
        }

        // ---- Queries --------------------------------------------------------------------

        public StepState GetStep(string id) => steps.Find(s => s.def.id == id);
        public bool IsDone(string id) { var s = GetStep(id); return s != null && s.done; }
        public int CountOf(string id) { var s = GetStep(id); return s?.count ?? 0; }
        public bool HasMistake(string id) => mistakes.Exists(m => m.def.id == id);

        /// <summary>The next step the Training guide should talk about.</summary>
        public StepState CurrentStep
        {
            get
            {
                foreach (var s in steps)
                    if (!s.done && !s.def.optional && RequirementsMet(s.def)) return s;
                foreach (var s in steps)
                    if (!s.done && !s.def.optional) return s;
                return null;
            }
        }

        public bool AllRequiredStepsDone
        {
            get
            {
                foreach (var s in steps) if (!s.done && !s.def.optional) return false;
                return true;
            }
        }

        bool RequirementsMet(StepDefinition d) => FirstMissing(d.requires) == null;

        StepDefinition FirstMissing(List<string> ids)
        {
            foreach (var id in ids)
            {
                var s = GetStep(id);
                if (s != null && !s.done) return s.def;
            }
            return null;
        }

        // ---- The main entry point --------------------------------------------------------

        /// <summary>
        /// Report that the user did something. Returns true if it moved a step forward.
        /// Unknown ids, repeats of finished steps and anything outside the active phase
        /// are ignored, so extra harmless movement is never penalised.
        /// </summary>
        public bool Report(string actionId)
        {
            if (Phase != ScenarioPhase.Active || definition == null || string.IsNullOrEmpty(actionId)) return false;

            var mistakeDef = definition.FindMistake(actionId);
            if (mistakeDef != null)
            {
                var rec = mistakes.Find(m => m.def == mistakeDef);
                if (rec == null) { rec = new MistakeRecord { def = mistakeDef, firstAt = Elapsed }; mistakes.Add(rec); }
                rec.count++;
                Log.Log(Elapsed, LogKind.Incorrect, mistakeDef.title);
                MistakeMade?.Invoke(rec);
                return false;
            }

            var step = GetStep(actionId);
            if (step == null) { Log.Log(Elapsed, LogKind.Info, "action " + actionId); return false; }
            if (step.done) return false;

            var missing = FirstMissing(step.def.requires);
            if (missing != null)
            {
                Log.Log(Elapsed, LogKind.Hint, $"{step.def.title} needs {missing.title} first");
                Blocked?.Invoke(step.def, missing);
                return false;
            }

            step.count++;
            if (step.count < Mathf.Max(1, step.def.requiredCount))
            {
                StepProgressed?.Invoke(step);
                return true;
            }

            step.done = true;
            step.doneAt = Elapsed;
            var skipped = FirstMissing(step.def.after);
            if (skipped != null)
            {
                step.outOfOrder = true;
                Log.Log(Elapsed, LogKind.Incorrect, $"{step.def.title} before {skipped.title}");
                OutOfOrder?.Invoke(step, skipped);
            }
            Log.Log(Elapsed, LogKind.Correct, step.def.title);
            StepCompleted?.Invoke(step);

            if (AllRequiredStepsDone && finishAt < 0f)
            {
                Log.Log(Elapsed, LogKind.Milestone, "All required steps done");
                AllRequiredDone?.Invoke();
                finishAt = Elapsed + finishDelay;
            }
            return true;
        }

        /// <summary>Ends the run, scores it, saves it and raises <see cref="Finished"/>.</summary>
        public ScenarioResult Finish(bool timeExpired)
        {
            if (Phase == ScenarioPhase.Finished) return Result;
            Phase = ScenarioPhase.Finished;
            Log.Log(Elapsed, LogKind.Milestone, timeExpired ? "Time expired" : "Scenario finished");
            Result = ScoringSystem.Compute(definition, steps, mistakes, Elapsed, timeExpired, Mode, Log);
            ResultsStore.Save(Result);
            SimulationManager.LastResult = Result;
            Finished?.Invoke(Result);
            return Result;
        }
    }
}
