using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FirstAidVR.Tests
{
    /// <summary>Checks the scoring rules on a small made-up scenario.</summary>
    public class ScoringTests
    {
        static ScenarioDefinition Def()
        {
            var d = ScriptableObject.CreateInstance<ScenarioDefinition>();
            d.id = "test"; d.title = "Test"; d.targetTime = 60; d.certificationTimeLimit = 120;
            d.steps = new List<StepDefinition>
            {
                new StepDefinition { id = "a", title = "A", points = 10, critical = true },
                new StepDefinition { id = "b", title = "B", points = 20, critical = true, after = new List<string> { "a" } },
                new StepDefinition { id = "c", title = "C", points = 10, critical = false, requiredCount = 4 },
                new StepDefinition { id = "o", title = "O", points = 5, optional = true },
            };
            d.mistakes = new List<MistakeDefinition>
            {
                new MistakeDefinition { id = "x", title = "X", penalty = 10, correction = "Don't X." },
                new MistakeDefinition { id = "bad", title = "Bad", penalty = 5, critical = true },
            };
            return d;
        }

        static List<StepState> States(ScenarioDefinition d, params string[] done)
        {
            var l = new List<StepState>();
            foreach (var s in d.steps)
            {
                bool isDone = System.Array.IndexOf(done, s.id) >= 0;
                l.Add(new StepState { def = s, done = isDone, count = isDone ? s.requiredCount : 0, doneAt = isDone ? 1f : -1f });
            }
            return l;
        }

        [Test]
        public void Perfect_FastRun_Scores100_AndPasses()
        {
            var d = Def();
            var r = ScoringSystem.Compute(d, States(d, "a", "b", "c"), new List<MistakeRecord>(), 50f, false, SimMode.Certification);
            Assert.AreEqual(100, r.technicalScore);
            Assert.AreEqual(100, r.efficiencyScore);
            Assert.AreEqual(100, r.overallScore);
            Assert.IsTrue(r.completed && r.passed);
            Assert.AreEqual(3, r.correctActions);
        }

        [Test]
        public void Slow_Run_LosesEfficiencyOnly()
        {
            var d = Def();
            var r = ScoringSystem.Compute(d, States(d, "a", "b", "c"), new List<MistakeRecord>(), 120f, false, SimMode.Certification);
            Assert.AreEqual(100, r.technicalScore);
            Assert.AreEqual(50, r.efficiencyScore);
            Assert.AreEqual(85, r.overallScore);
            Assert.IsTrue(r.passed);
        }

        [Test]
        public void Mistakes_CountOncePerKind_AndCriticalFails()
        {
            var d = Def();
            var mistakes = new List<MistakeRecord> { new MistakeRecord { def = d.mistakes[0], count = 3 } };
            var r = ScoringSystem.Compute(d, States(d, "a", "b", "c"), mistakes, 50f, false, SimMode.Certification);
            Assert.AreEqual(90, r.technicalScore, "a repeated mistake is penalised once");
            Assert.AreEqual(3, r.incorrectActions, "but every occurrence is counted");
            Assert.IsTrue(r.passed);
            mistakes.Add(new MistakeRecord { def = d.mistakes[1], count = 1 });
            r = ScoringSystem.Compute(d, States(d, "a", "b", "c"), mistakes, 50f, false, SimMode.Certification);
            Assert.IsFalse(r.passed, "critical mistake fails certification");
            Assert.Contains("Don't X.", r.improvements);
        }

        [Test]
        public void MissedCriticalStep_Fails_AndIsListed()
        {
            var d = Def();
            var r = ScoringSystem.Compute(d, States(d, "a", "c"), new List<MistakeRecord>(), 50f, false, SimMode.Certification);
            Assert.IsFalse(r.completed);
            Assert.IsFalse(r.passed);
            CollectionAssert.Contains(r.missedCritical, "B");
            Assert.AreEqual(50, r.technicalScore);
        }

        [Test]
        public void OutOfOrder_SmallPenalty_PartialCredit_ForRepeats()
        {
            var d = Def();
            var states = States(d, "a", "b");
            states[1].outOfOrder = true;
            states[2].count = 2; // 2 of 4 compressions
            var r = ScoringSystem.Compute(d, states, new List<MistakeRecord>(), 50f, false, SimMode.Training);
            // earned 10 + 20 + floor(10 * 0.5 * 0.5)=2 -> 32/40 = 80, minus 5 for order
            Assert.AreEqual(75, r.technicalScore);
            Assert.AreEqual(1, r.incorrectActions);
        }

        [Test]
        public void TimeScore_Curve()
        {
            Assert.AreEqual(100f, ScoringSystem.TimeScore(30, 60, 120));
            Assert.AreEqual(75f, ScoringSystem.TimeScore(90, 60, 120), 0.01f);
            Assert.AreEqual(50f, ScoringSystem.TimeScore(120, 60, 120), 0.01f);
            Assert.GreaterOrEqual(ScoringSystem.TimeScore(1000, 60, 120), 30f);
        }

        [Test]
        public void Controller_IgnoresRepeats_AndBlocksUntilRequired()
        {
            var go = new GameObject("ctrl");
            var c = go.AddComponent<ScenarioController>();
            var d = Def();
            d.steps[1].requires = new List<string> { "c" };
            c.definition = d;
            c.Reset();
            c.Begin();
            Assert.IsFalse(c.Report("b"), "blocked until c done");
            Assert.IsFalse(c.IsDone("b"));
            Assert.IsTrue(c.Report("a"));
            Assert.IsFalse(c.Report("a"), "repeat ignored, no penalty");
            for (int i = 0; i < 4; i++) c.Report("c");
            Assert.IsTrue(c.IsDone("c"));
            Assert.IsTrue(c.Report("b"));
            Assert.IsFalse(c.Report("unknown_action"));
            Assert.AreEqual(0, c.Mistakes.Count);
            Object.DestroyImmediate(go);
        }
    }
}
