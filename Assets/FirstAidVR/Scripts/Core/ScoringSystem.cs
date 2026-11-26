using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>Live progress of one step while a scenario is running.</summary>
    public class StepState
    {
        public StepDefinition def;
        public int count;
        public bool done;
        public float doneAt = -1f;
        public bool outOfOrder;
    }

    /// <summary>One mistake id and how many times it happened.</summary>
    public class MistakeRecord
    {
        public MistakeDefinition def;
        public int count;
        public float firstAt;
    }

    /// <summary>
    /// Turns what happened in a run into scores. Kept separate from the scene so
    /// it can be unit tested and explained to judges.
    ///
    ///   Technical proficiency = step points earned / points available
    ///                           - mistake penalties (each kind of mistake counts once)
    ///                           - 5 points for each step done out of order
    ///   Response efficiency   = 100 at or under the target time, falling to 50 at the
    ///                           time limit, multiplied by the share of required steps done
    ///   Overall               = 70% technical + 30% efficiency
    ///   Certification pass    = overall >= 80, no critical step missed, no critical mistake
    /// </summary>
    public static class ScoringSystem
    {
        public const float TechnicalWeight = 0.7f;
        public const float EfficiencyWeight = 0.3f;
        public const int OutOfOrderPenalty = 5;
        public const int PassMark = 80;

        public static ScenarioResult Compute(ScenarioDefinition def, IList<StepState> steps,
            IList<MistakeRecord> mistakes, float elapsed, bool timeExpired, SimMode mode,
            EventLogger log = null)
        {
            var r = new ScenarioResult
            {
                scenarioId = def.id,
                scenarioTitle = def.title,
                mode = mode,
                dateIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                timeSeconds = elapsed,
                targetSeconds = def.targetTime,
                timeLimitSeconds = def.certificationTimeLimit,
                timeExpired = timeExpired,
            };

            // ---- Technical proficiency -------------------------------------------------
            int available = 0, earned = 0, requiredTotal = 0, requiredDone = 0, outOfOrder = 0;
            foreach (var s in steps)
            {
                var d = s.def;
                int pts = Mathf.Max(0, d.points);
                if (!d.optional) { available += pts; requiredTotal++; }
                int got = 0;
                if (s.done)
                {
                    got = pts;
                    if (!d.optional) requiredDone++;
                    if (s.outOfOrder) outOfOrder++;
                    r.correctActions++;
                }
                else if (d.requiredCount > 1 && s.count > 0)
                {
                    // Partial credit for repeated actions (e.g. 3 of 5 back blows).
                    got = Mathf.FloorToInt(pts * (float)s.count / d.requiredCount * 0.5f);
                }
                earned += got;
                r.steps.Add(new StepOutcome
                {
                    id = d.id, title = d.title, done = s.done, critical = d.critical, optional = d.optional,
                    outOfOrder = s.outOfOrder, doneAt = s.doneAt, points = pts, earned = got,
                });
                if (!s.done && d.critical && !d.optional) r.missedCritical.Add(d.title);
            }

            int penalty = outOfOrder * OutOfOrderPenalty;
            bool criticalMistake = false;
            foreach (var m in mistakes)
            {
                penalty += Mathf.Max(0, m.def.penalty);
                criticalMistake |= m.def.critical;
                r.mistakes.Add(m.count > 1 ? $"{m.def.title} (x{m.count})" : m.def.title);
            }
            r.incorrectActions = CountMistakeEvents(mistakes) + outOfOrder;

            float tech = available > 0 ? 100f * Mathf.Min(earned, available) / available : 100f;
            r.technicalScore = Mathf.Clamp(Mathf.RoundToInt(tech - penalty), 0, 100);

            // ---- Response efficiency ---------------------------------------------------
            r.completed = requiredDone == requiredTotal;
            float timeScore = TimeScore(elapsed, def.targetTime, def.certificationTimeLimit);
            float completion = requiredTotal > 0 ? (float)requiredDone / requiredTotal : 1f;
            r.efficiencyScore = Mathf.Clamp(Mathf.RoundToInt(timeScore * completion), 0, 100);

            // ---- Overall ---------------------------------------------------------------
            r.overallScore = Mathf.Clamp(Mathf.RoundToInt(
                r.technicalScore * TechnicalWeight + r.efficiencyScore * EfficiencyWeight), 0, 100);
            r.passed = r.completed && r.overallScore >= PassMark && r.missedCritical.Count == 0 && !criticalMistake;
            r.grade = Grade(r.overallScore);

            // ---- Areas for improvement -------------------------------------------------
            foreach (var s in steps)
                if (!s.done && !s.def.optional)
                    r.improvements.Add($"Remember to {Lower(s.def.title)}.");
            foreach (var m in mistakes)
                if (!string.IsNullOrEmpty(m.def.correction)) r.improvements.Add(m.def.correction);
            foreach (var s in steps)
                if (s.outOfOrder)
                    r.improvements.Add($"Do \"{s.def.title}\" in the right order: {OrderHint(def, s.def)}.");
            if (r.completed && elapsed > def.targetTime)
                r.improvements.Add($"Work on speed: aim to finish in under {FormatTime(def.targetTime)}.");
            if (timeExpired)
                r.improvements.Add("Time ran out. Practise in Training Mode until the steps feel automatic.");
            foreach (var s in steps)
                if (!s.done && s.def.optional)
                    r.improvements.Add($"Also consider: {Lower(s.def.title)}.");
            if (r.improvements.Count == 0) r.improvements.Add("Great work - keep practising to stay ready.");

            // ---- Timeline for the dashboard chart ----------------------------------------
            if (log != null)
                foreach (var e in log.Entries)
                    if (e.kind == LogKind.Correct || e.kind == LogKind.Incorrect)
                        r.timeline.Add(new TimelineMark { time = e.time, good = e.kind == LogKind.Correct, label = e.message });

            return r;
        }

        /// <summary>100 at/below target, linearly down to 50 at the limit, 30 floor after it.</summary>
        public static float TimeScore(float elapsed, float target, float limit)
        {
            if (elapsed <= target) return 100f;
            if (limit <= target) return 50f;
            float t = Mathf.Clamp01((elapsed - target) / (limit - target));
            float s = Mathf.Lerp(100f, 50f, t);
            if (elapsed > limit) s = Mathf.Max(30f, 50f - (elapsed - limit) / limit * 20f);
            return s;
        }

        public static string Grade(int overall)
        {
            if (overall >= 90) return "Excellent";
            if (overall >= PassMark) return "Proficient";
            if (overall >= 60) return "Needs Practice";
            return "Not Yet Proficient";
        }

        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int m = (int)(seconds / 60), s = (int)(seconds % 60);
            return $"{m}:{s:00}";
        }

        static int CountMistakeEvents(IList<MistakeRecord> mistakes)
        {
            int n = 0;
            foreach (var m in mistakes) n += m.count;
            return n;
        }

        static string OrderHint(ScenarioDefinition def, StepDefinition step)
        {
            var names = new List<string>();
            foreach (var a in step.after)
            {
                var p = def.FindStep(a);
                if (p != null) names.Add(p.title);
            }
            return names.Count == 0 ? "follow the checklist" : "first " + string.Join(", then ", names.ConvertAll(Lower));
        }

        static string Lower(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
    }
}
