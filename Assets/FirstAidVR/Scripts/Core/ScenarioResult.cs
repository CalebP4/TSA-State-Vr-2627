using System;
using System.Collections.Generic;

namespace FirstAidVR
{
    public enum SimMode { Training, Certification }

    /// <summary>
    /// The outcome of one run of a scenario. Plain data so it can be saved as JSON
    /// and shown on the performance dashboard.
    /// </summary>
    [Serializable]
    public class ScenarioResult
    {
        public string scenarioId;
        public string scenarioTitle;
        public SimMode mode;
        public string dateIso;

        public int overallScore;          // 0-100
        public int technicalScore;        // 0-100: did the right things, avoided mistakes
        public int efficiencyScore;       // 0-100: how quickly the care was given
        public float timeSeconds;
        public float targetSeconds;
        public float timeLimitSeconds;
        public bool completed;            // every required step was done
        public bool timeExpired;
        public bool passed;               // Certification only
        public string grade;

        public int correctActions;
        public int incorrectActions;

        public List<StepOutcome> steps = new List<StepOutcome>();
        public List<string> mistakes = new List<string>();
        public List<string> missedCritical = new List<string>();
        public List<string> improvements = new List<string>();
        public List<TimelineMark> timeline = new List<TimelineMark>();
    }

    [Serializable]
    public class StepOutcome
    {
        public string id;
        public string title;
        public bool done;
        public bool critical;
        public bool optional;
        public bool outOfOrder;
        public float doneAt;
        public int points;
        public int earned;
    }

    [Serializable]
    public class TimelineMark
    {
        public float time;
        public bool good;
        public string label;
    }

    [Serializable]
    public class ResultHistory
    {
        public List<ScenarioResult> results = new List<ScenarioResult>();
    }
}
