using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// All of the teaching content for one emergency scenario: the steps the user
    /// should perform, the mistakes we watch for, and the time targets.
    /// Edit these assets in Assets/FirstAidVR/Data to change wording or scoring.
    /// </summary>
    [CreateAssetMenu(menuName = "First Aid VR/Scenario Definition", fileName = "Scenario")]
    public class ScenarioDefinition : ScriptableObject
    {
        [Tooltip("Short id used by code and save files, e.g. \"bleeding\".")]
        public string id;
        public string title;
        [TextArea] public string subtitle;
        [Tooltip("Scene that contains this scenario (must be in Build Settings).")]
        public string sceneName;
        [TextArea(3, 8)] public string introduction;
        [Tooltip("Accent colour used on menus and the dashboard for this scenario.")]
        public Color accent = new Color(0.1f, 0.6f, 0.9f);

        [Header("Timing (seconds)")]
        [Tooltip("Finishing at or under this time earns full efficiency.")]
        public float targetTime = 120f;
        [Tooltip("Certification Mode ends when this runs out.")]
        public float certificationTimeLimit = 300f;

        [Header("Content")]
        public List<StepDefinition> steps = new List<StepDefinition>();
        public List<MistakeDefinition> mistakes = new List<MistakeDefinition>();
        [TextArea(2, 6)] public List<string> keyPoints = new List<string>();
        [Tooltip("Where the guidance in this scenario comes from (shown on the intro panel).")]
        public string sources;

        public StepDefinition FindStep(string stepId) => steps.Find(s => s.id == stepId);
        public MistakeDefinition FindMistake(string mistakeId) => mistakes.Find(m => m.id == mistakeId);

        /// <summary>Total points available from steps.</summary>
        public int TotalPoints
        {
            get { int t = 0; foreach (var s in steps) t += Mathf.Max(0, s.points); return t; }
        }
    }

    [Serializable]
    public class StepDefinition
    {
        [Tooltip("Action id reported by the interaction scripts, e.g. \"call_911\".")]
        public string id;
        [Tooltip("Short checklist label, e.g. \"Call 911\".")]
        public string title;
        [TextArea(2, 5), Tooltip("Training Mode instruction shown when this is the next step.")]
        public string instruction;
        [TextArea(1, 4), Tooltip("Shown as positive feedback after the step is done.")]
        public string feedback;
        [Tooltip("Critical steps must be done to pass Certification.")]
        public bool critical = true;
        [Tooltip("Optional steps add points but are not needed to finish the scenario.")]
        public bool optional;
        public int points = 10;
        [Tooltip("How many times the action must be reported (e.g. 5 back blows).")]
        public int requiredCount = 1;
        [Tooltip("Steps that should be finished first. Doing this step early counts as out of order (it still counts).")]
        public List<string> after = new List<string>();
        [Tooltip("Steps that must be finished first. The action is ignored with a hint until they are done.")]
        public List<string> requires = new List<string>();
        [Tooltip("Name of the scene object to highlight in Training Mode for this step.")]
        public string highlightTarget;
    }

    [Serializable]
    public class MistakeDefinition
    {
        [Tooltip("Action id reported by the interaction scripts, e.g. \"finger_sweep\".")]
        public string id;
        [Tooltip("What went wrong, e.g. \"Blind finger sweep\".")]
        public string title;
        [TextArea(2, 4), Tooltip("What the user should do instead (shown as feedback).")]
        public string correction;
        public int penalty = 10;
        [Tooltip("A critical mistake means Certification cannot be passed.")]
        public bool critical;
    }
}
