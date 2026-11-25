using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FirstAidVR
{
    /// <summary>
    /// All the screens inside a scenario: the intro briefing, the Training guide
    /// (current step, checklist, highlights, feedback) or the Certification HUD
    /// (countdown only, no hints), the pause menu and the results dashboard.
    /// </summary>
    public class ScenarioUI : MonoBehaviour
    {
        public ScenarioController controller;
        [Tooltip("Where the guide board stands (faces the player).")]
        public Transform guideAnchor;

        Canvas board, dashboard, pause;
        RectTransform boardRoot;
        TextMeshProUGUI timerText, stepCounter, stepTitle, stepText, checklist, certInfo;
        Image progressFill;
        readonly List<GameObject> introObjects = new List<GameObject>();
        InputAction menuAction;
        string lastHighlight;

        public Canvas Dashboard => dashboard;
        public Canvas Board => board;
        public bool PauseOpen => pause != null && pause.gameObject.activeSelf;

        void Start()
        {
            if (controller == null) controller = ScenarioController.Active;
            BuildBoard();
            ShowIntro();
            controller.Started += OnStarted;
            controller.StepCompleted += OnStepCompleted;
            controller.StepProgressed += OnStepProgressed;
            controller.OutOfOrder += OnOutOfOrder;
            controller.Blocked += OnBlocked;
            controller.MistakeMade += OnMistake;
            controller.Finished += OnFinished;

            menuAction = new InputAction("Menu", InputActionType.Button);
            menuAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            menuAction.AddBinding("<Keyboard>/escape");
            menuAction.performed += _ => TogglePause();
            menuAction.Enable();
        }

        void OnDestroy()
        {
            menuAction?.Disable();
            menuAction?.Dispose();
        }

        // ---- Guide board --------------------------------------------------------------

        void BuildBoard()
        {
            var anchor = guideAnchor != null ? guideAnchor : transform;
            board = UIFactory.WorldCanvas("Guide Board", anchor, Vector3.zero, Quaternion.identity, new Vector2(760, 940));
            boardRoot = (RectTransform)board.transform;
            UIFactory.Card(boardRoot);
            var def = controller.definition;
            UIFactory.Panel(boardRoot, 0, 0, 760, 10, def.accent, 0.3f, "Accent");
            UIFactory.Logo(boardRoot, 36, 34, 64);
            UIFactory.Label(boardRoot, Theme.ProductName.ToUpperInvariant(), 118, 34, 400, 26, 20, Theme.Muted, TextAlignmentOptions.Left, true);
            UIFactory.Label(boardRoot, def.title, 118, 60, 440, 46, 36, Theme.Text, TextAlignmentOptions.Left, true);
            var chipColor = controller.IsTraining ? Theme.Blue : Theme.Purple;
            var chip = UIFactory.Panel(boardRoot, 540, 36, 186, 38, chipColor.WithAlpha(0.22f), 2f, "Mode Chip");
            UIFactory.Label(chip.transform, controller.IsTraining ? "TRAINING" : "CERTIFICATION", 0, 0, 186, 38, 17, chipColor, TextAlignmentOptions.Center, true);
            timerText = UIFactory.Label(boardRoot, "", 540, 76, 186, 40, 30, Theme.Text, TextAlignmentOptions.Right, true);
        }

        void ShowIntro()
        {
            var def = controller.definition;
            var t = UIFactory.Label(boardRoot, "THE SITUATION", 36, 134, 690, 30, 20, def.accent, TextAlignmentOptions.Left, true);
            introObjects.Add(t.gameObject);
            var intro = UIFactory.Label(boardRoot, def.introduction, 36, 170, 690, 280, 27, Theme.Text);
            intro.enableAutoSizing = true; intro.fontSizeMin = 18; intro.fontSizeMax = 27;
            introObjects.Add(intro.gameObject);

            string modeText = controller.IsTraining
                ? "<b>Training Mode</b>\nStep-by-step instructions, highlights and instant feedback guide you. Take your time."
                : $"<b>Certification Mode</b>\nNo instructions or hints. You have <b>{ScoringSystem.FormatTime(def.certificationTimeLimit)}</b>. Every action is recorded and scored.";
            var modeCard = UIFactory.Panel(boardRoot, 36, 470, 690, 150, (controller.IsTraining ? Theme.Blue : Theme.Purple).WithAlpha(0.16f), 1f, "Mode Card");
            UIFactory.Label(modeCard.transform, modeText, 24, 18, 642, 120, 24, Theme.Text);
            introObjects.Add(modeCard.gameObject);

            var src = UIFactory.Label(boardRoot, "Based on: " + def.sources + "\nEducational simulation only - not a substitute for certified first-aid training.",
                36, 640, 690, 110, 18, Theme.Muted);
            introObjects.Add(src.gameObject);

            var begin = UIFactory.Button(boardRoot, "Begin", 36, 790, 450, 100, Begin, Theme.Green, 40, "Begin Button");
            introObjects.Add(begin.gameObject);
            var menu = UIFactory.Button(boardRoot, "Menu", 506, 790, 220, 100, () => SimulationManager.ReturnToMenu(), Theme.PanelLight, 30, "Intro Menu Button");
            introObjects.Add(menu.gameObject);
            timerText.text = "";
        }

        /// <summary>Closes the intro and starts the scenario (Begin button; also used by tests).</summary>
        public void Begin()
        {
            if (controller.Phase != ScenarioPhase.Intro) return;
            controller.Begin();
        }

        void OnStarted()
        {
            foreach (var o in introObjects) if (o != null) Destroy(o);
            introObjects.Clear();
            if (controller.IsTraining) BuildTrainingGuide();
            else BuildCertHud();
            UIFactory.Button(boardRoot, "Pause / Menu", 36, 840, 690, 66, TogglePause, Theme.PanelLight, 26, "Pause Button");
            Refresh();
        }

        void BuildTrainingGuide()
        {
            stepCounter = UIFactory.Label(boardRoot, "", 36, 134, 690, 30, 20, controller.definition.accent, TextAlignmentOptions.Left, true);
            stepTitle = UIFactory.Label(boardRoot, "", 36, 166, 690, 50, 36, Theme.Text, TextAlignmentOptions.Left, true);
            stepTitle.textWrappingMode = TextWrappingModes.NoWrap;
            stepTitle.enableAutoSizing = true; stepTitle.fontSizeMin = 20; stepTitle.fontSizeMax = 36;
            stepText = UIFactory.Label(boardRoot, "", 36, 220, 690, 190, 27, Theme.Text);
            stepText.enableAutoSizing = true; stepText.fontSizeMin = 18; stepText.fontSizeMax = 27;
            progressFill = UIFactory.Bar(boardRoot, 36, 424, 690, 14, Theme.Green, 0f);
            var list = UIFactory.Panel(boardRoot, 36, 458, 690, 366, Theme.PanelLight.WithAlpha(0.5f), 1f, "Checklist");
            UIFactory.Label(list.transform, "CHECKLIST", 20, 12, 400, 26, 18, Theme.Muted, TextAlignmentOptions.Left, true);
            checklist = UIFactory.Label(list.transform, "", 20, 42, 650, 316, 22, Theme.Text);
            checklist.enableAutoSizing = true; checklist.fontSizeMin = 14; checklist.fontSizeMax = 22;
        }

        void BuildCertHud()
        {
            UIFactory.Label(boardRoot, "RESPOND TO THE EMERGENCY", 36, 134, 690, 30, 20, Theme.Purple, TextAlignmentOptions.Left, true);
            certInfo = UIFactory.Label(boardRoot, "", 36, 170, 690, 640, 28, Theme.Text);
        }

        void Update()
        {
            if (controller == null || timerText == null) return;
            if (controller.Phase == ScenarioPhase.Active)
            {
                if (controller.IsTraining) timerText.text = ScoringSystem.FormatTime(controller.Elapsed);
                else
                {
                    float left = controller.TimeLeft;
                    timerText.text = ScoringSystem.FormatTime(left);
                    timerText.color = left < 30f ? Theme.Red : left < 60f ? Theme.Amber : Theme.Text;
                    if (certInfo != null)
                        certInfo.text = "Act as you would in a real emergency. No instructions or hints are given in Certification Mode.\n\n" +
                            $"<color=#9BAAC0>Time remaining</color>\n<size=150%><b>{ScoringSystem.FormatTime(left)}</b></size>\n\n" +
                            $"<color=#9BAAC0>Actions recorded</color>\n<size=130%><b>{controller.Log.Count(LogKind.Correct) + controller.Log.Count(LogKind.Incorrect)}</b></size>\n\n" +
                            "<color=#9BAAC0>The scenario ends when care is complete or time runs out.</color>";
                }
            }
        }

        void Refresh()
        {
            if (!controller.IsTraining || stepTitle == null) return;
            var steps = controller.Steps;
            int done = 0, required = 0;
            var sb = new System.Text.StringBuilder();
            var current = controller.CurrentStep;
            foreach (var s in steps)
            {
                if (!s.def.optional) required++;
                if (s.done && !s.def.optional) done++;
                string mark = s.done ? "<color=#2FBF71>✓</color>" : s == current ? "<color=#F5A524>▶</color>" : "<color=#9BAAC0>•</color>";
                string label = s.def.title + (s.def.optional ? " <color=#9BAAC0>(optional)</color>" : "");
                if (s.def.requiredCount > 1 && !s.done && s.count > 0) label += $" <color=#F5A524>{s.count}/{s.def.requiredCount}</color>";
                if (s == current) label = "<b>" + label + "</b>";
                else if (s.done) label = "<color=#9BAAC0>" + label + "</color>";
                sb.Append(mark).Append("  ").Append(label).Append('\n');
            }
            checklist.text = sb.ToString();
            progressFill.fillAmount = required > 0 ? (float)done / required : 1f;
            if (current != null)
            {
                int index = 0;
                for (int i = 0; i < steps.Count; i++) if (steps[i] == current) index = i;
                stepCounter.text = $"STEP {index + 1} OF {steps.Count}";
                stepTitle.text = current.def.title;
                string count = current.def.requiredCount > 1 ? $"\n\n<color=#F5A524><b>{current.count} of {current.def.requiredCount}</b></color>" : "";
                stepText.text = current.def.instruction + count;
                Highlight(current.def.highlightTarget);
            }
            else
            {
                stepCounter.text = "ALL STEPS COMPLETE";
                stepTitle.text = "Well done";
                stepText.text = "Stay with the patient. Emergency medical services are arriving and will take over care.";
                Highlight(null);
            }
        }

        void Highlight(string target)
        {
            if (target == lastHighlight) return;
            lastHighlight = target;
            Highlighter.Point(target);
        }

        /// <summary>Lets scenario scripts change the instruction text for special moments.</summary>
        public void OverrideInstruction(string title, string text, string highlight = null)
        {
            if (!controller.IsTraining || stepTitle == null) return;
            stepTitle.text = title;
            stepText.text = text;
            lastHighlight = null;
            Highlight(highlight);
        }

        public void RefreshGuide() => Refresh();

        // ---- Feedback -------------------------------------------------------------------

        void OnStepCompleted(StepState s)
        {
            if (controller.IsTraining)
            {
                Sfx.Play(Sound.Correct);
                FeedbackToast.Show(ToastKind.Good, s.def.title, s.def.feedback);
            }
            else Sfx.Play(Sound.Tick, null, 0.6f);
            Refresh();
        }

        void OnStepProgressed(StepState s)
        {
            if (controller.IsTraining) Sfx.Play(Sound.Tick, null, 0.7f, 1f + 0.08f * s.count);
            Refresh();
        }

        void OnOutOfOrder(StepState s, StepDefinition skipped)
        {
            if (!controller.IsTraining) return;
            FeedbackToast.Show(ToastKind.Hint, "Order matters", $"Try to {Lower(skipped.title)} before you {Lower(s.def.title)}.");
        }

        void OnBlocked(StepDefinition step, StepDefinition first)
        {
            if (!controller.IsTraining) return;
            Sfx.Play(Sound.Hover);
            FeedbackToast.Show(ToastKind.Hint, "Not yet", $"First: {first.title}.");
        }

        void OnMistake(MistakeRecord m)
        {
            if (controller.IsTraining)
            {
                Sfx.Play(Sound.Wrong);
                HandProbe.PulseAll(0.6f, 0.15f);
                FeedbackToast.Show(ToastKind.Bad, m.def.title, m.def.correction);
            }
            else Sfx.Play(Sound.Tick, null, 0.6f);
            Refresh();
        }

        static string Lower(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

        // ---- Pause ------------------------------------------------------------------------

        public void TogglePause()
        {
            if (controller.Phase == ScenarioPhase.Finished) return;
            if (pause == null) BuildPause();
            bool open = !pause.gameObject.activeSelf;
            pause.gameObject.SetActive(open);
            if (open) pause.GetComponentInParent<HeadFollower>().Snap();
            controller.Held = open || ChoicePrompt.Open != null;
        }

        void BuildPause()
        {
            var holder = new GameObject("Pause Menu");
            var f = holder.AddComponent<HeadFollower>();
            f.distance = 0.9f; f.height = -0.05f;
            pause = UIFactory.WorldCanvas("Pause Canvas", holder.transform, Vector3.zero, Quaternion.identity, new Vector2(560, 520));
            var rt = (RectTransform)pause.transform;
            UIFactory.Card(rt);
            UIFactory.Label(rt, "Paused", 0, 34, 560, 60, 44, Theme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Label(rt, "The clock is stopped.", 0, 96, 560, 30, 22, Theme.Muted, TextAlignmentOptions.Center);
            UIFactory.Button(rt, "Resume", 60, 150, 440, 86, TogglePause, Theme.Green, 32, "Resume Button");
            UIFactory.Button(rt, "Restart Scenario", 60, 252, 440, 86, SimulationManager.Restart, Theme.Blue, 32, "Restart Button");
            UIFactory.Button(rt, "Main Menu", 60, 354, 440, 86, SimulationManager.ReturnToMenu, Theme.PanelLight, 32, "Main Menu Button");
            pause.gameObject.SetActive(false);
        }

        // ---- Results ------------------------------------------------------------------------

        void OnFinished(ScenarioResult r)
        {
            Highlighter.Point(null);
            FeedbackToast.Clear();
            if (pause != null) pause.gameObject.SetActive(false);
            Sfx.Play(r.mode == SimMode.Certification && !r.passed ? Sound.Fail : Sound.Complete);
            board.gameObject.SetActive(false);
            var holder = new GameObject("Results Dashboard");
            var cam = Camera.main;
            if (cam != null)
            {
                var fwd = cam.transform.forward; fwd.y = 0; fwd = fwd.sqrMagnitude < 0.01f ? Vector3.forward : fwd.normalized;
                holder.transform.position = cam.transform.position + fwd * 1.45f + Vector3.up * -0.1f;
                holder.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            dashboard = UIFactory.WorldCanvas("Dashboard Canvas", holder.transform, Vector3.zero, Quaternion.identity,
                new Vector2(DashboardView.Width, DashboardView.Height));
            DashboardView.Build((RectTransform)dashboard.transform, r, new DashboardView.Buttons
            {
                retry = SimulationManager.Restart,
                switchMode = () => SimulationManager.StartScenario(r.scenarioId, r.mode == SimMode.Training ? SimMode.Certification : SimMode.Training),
                menu = SimulationManager.ReturnToMenu,
            });
        }
    }
}
