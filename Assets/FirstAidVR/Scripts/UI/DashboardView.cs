using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstAidVR
{
    /// <summary>
    /// Draws the performance dashboard for a <see cref="ScenarioResult"/>: overall score
    /// ring, technical/efficiency bars, action counts, step checklist, areas for
    /// improvement and a timeline of correct/incorrect actions.
    /// </summary>
    public static class DashboardView
    {
        public const float Width = 1400, Height = 960;

        public struct Buttons
        {
            public Action retry, switchMode, menu, close;
        }

        public static RectTransform Build(RectTransform parent, ScenarioResult r, Buttons buttons)
        {
            var root = UIFactory.Rect("Dashboard", parent, 0, 0, Width, Height);
            UIFactory.Card(root);
            var def = SimulationManager.Find(r.scenarioId);
            var accent = def != null ? def.accent : Theme.Blue;

            // ---- Header ---------------------------------------------------------------
            UIFactory.Panel(root, 0, 0, Width, 10, accent, 0.3f, "Accent");
            UIFactory.Logo(root, 40, 34, 70);
            UIFactory.Label(root, "PERFORMANCE DASHBOARD", 130, 34, 700, 30, 22, Theme.Muted, TextAlignmentOptions.Left, true);
            UIFactory.Label(root, r.scenarioTitle, 130, 62, 760, 50, 40, Theme.Text, TextAlignmentOptions.Left, true);
            Chip(root, r.mode == SimMode.Training ? "TRAINING MODE" : "CERTIFICATION MODE", 900, 40,
                r.mode == SimMode.Training ? Theme.Blue : Theme.Purple);

            string badge; Color badgeColor;
            if (r.mode == SimMode.Certification)
            {
                badge = r.passed ? "PASSED" : r.timeExpired ? "TIME EXPIRED" : "NOT PASSED";
                badgeColor = r.passed ? Theme.Green : Theme.Red;
            }
            else
            {
                badge = r.completed ? "COMPLETED" : "INCOMPLETE";
                badgeColor = r.completed ? Theme.Green : Theme.Amber;
            }
            var b = UIFactory.Panel(root, 1120, 34, 240, 76, badgeColor, 0.8f, "Badge");
            UIFactory.Label(b.transform, badge, 0, 0, 240, 76, 28, Color.white, TextAlignmentOptions.Center, true);

            // ---- Left: scores -----------------------------------------------------------
            var left = UIFactory.Panel(root, 40, 140, 400, 590, Theme.PanelLight.WithAlpha(0.5f), 1f, "Scores");
            UIFactory.ScoreRing(left.transform, 70, 24, 260, r.overallScore, "OVERALL");
            UIFactory.Label(left.transform, r.grade, 0, 292, 400, 40, 30, Theme.ScoreColor(r.overallScore), TextAlignmentOptions.Center, true);
            ScoreBar(left.transform, "Technical proficiency", r.technicalScore, 360, "Right steps, right order, no mistakes");
            ScoreBar(left.transform, "Response efficiency", r.efficiencyScore, 470, $"Target time {ScoringSystem.FormatTime(r.targetSeconds)}");

            // ---- Middle: stats + checklist -------------------------------------------------
            Tile(root, 470, 140, "Correct actions", r.correctActions.ToString(), Theme.Green);
            Tile(root, 700, 140, "Incorrect actions", r.incorrectActions.ToString(), r.incorrectActions == 0 ? Theme.Green : Theme.Red);
            Tile(root, 470, 268, "Time taken", ScoringSystem.FormatTime(r.timeSeconds),
                r.timeSeconds <= r.targetSeconds ? Theme.Green : Theme.Amber);
            Tile(root, 700, 268, "Missed critical", r.missedCritical.Count.ToString(), r.missedCritical.Count == 0 ? Theme.Green : Theme.Red);

            var list = UIFactory.Panel(root, 470, 396, 430, 334, Theme.PanelLight.WithAlpha(0.5f), 1f, "Checklist");
            UIFactory.Label(list.transform, "STEP CHECKLIST", 20, 14, 390, 28, 20, Theme.Muted, TextAlignmentOptions.Left, true);
            int shown = Mathf.Min(r.steps.Count, 10);
            float rowH = Mathf.Min(30f, 280f / Mathf.Max(1, shown));
            for (int i = 0; i < shown; i++)
            {
                var s = r.steps[i];
                string mark = s.done ? (s.outOfOrder ? "<color=#F5A524>✓</color>" : "<color=#2FBF71>✓</color>") :
                    s.optional ? "<color=#9BAAC0>-</color>" : "<color=#E5484D>✗</color>";
                string time = s.done ? $"<color=#9BAAC0>{ScoringSystem.FormatTime(s.doneAt)}</color>" : s.optional ? "<color=#9BAAC0>optional</color>" : "<color=#E5484D>missed</color>";
                var row = UIFactory.Label(list.transform, $"{mark}  {s.title}", 20, 48 + i * rowH, 300, rowH, Mathf.Min(22, rowH * 0.78f), Theme.Text);
                row.textWrappingMode = TextWrappingModes.NoWrap;
                row.overflowMode = TextOverflowModes.Ellipsis;
                UIFactory.Label(list.transform, time, 300, 48 + i * rowH, 110, rowH, Mathf.Min(20, rowH * 0.7f), Theme.Muted, TextAlignmentOptions.Right);
            }

            // ---- Right: areas for improvement ----------------------------------------------
            var imp = UIFactory.Panel(root, 930, 140, 430, 590, Theme.PanelLight.WithAlpha(0.5f), 1f, "Improvement");
            UIFactory.Label(imp.transform, "AREAS FOR IMPROVEMENT", 20, 14, 390, 28, 20, Theme.Muted, TextAlignmentOptions.Left, true);
            var sb = new System.Text.StringBuilder();
            int count = 0;
            foreach (var s in r.improvements)
            {
                if (count++ >= 7) break;
                sb.Append("<color=#F5A524>•</color> ").Append(s).Append("\n");
            }
            if (r.mistakes.Count > 0)
            {
                sb.Append("\n<color=#E5484D><b>Mistakes</b></color>\n");
                foreach (var m in r.mistakes) sb.Append("<color=#E5484D>•</color> ").Append(m).Append("\n");
            }
            var impText = UIFactory.Label(imp.transform, sb.ToString(), 20, 50, 390, 520, 22, Theme.Text);
            impText.enableAutoSizing = true;
            impText.fontSizeMin = 15;
            impText.fontSizeMax = 22;

            // ---- Timeline ---------------------------------------------------------------
            Timeline(root, r, 40, 752, 1320, 86);

            // ---- Buttons ----------------------------------------------------------------
            float x = 40;
            if (buttons.retry != null) { UIFactory.Button(root, "Try Again", x, 862, 300, 74, buttons.retry, Theme.Blue, 30, "Retry Button"); x += 320; }
            if (buttons.switchMode != null)
            {
                string label = r.mode == SimMode.Training ? "Take Certification" : "Back to Training";
                UIFactory.Button(root, label, x, 862, 360, 74, buttons.switchMode, Theme.Purple, 30, "Switch Mode Button");
                x += 380;
            }
            if (buttons.menu != null) UIFactory.Button(root, "Main Menu", x, 862, 300, 74, buttons.menu, Theme.PanelLight, 30, "Menu Button");
            if (buttons.close != null) UIFactory.Button(root, "Close", 1060, 862, 300, 74, buttons.close, Theme.PanelLight, 30, "Close Button");
            UIFactory.Label(root, "Educational simulation only - not a substitute for certified first-aid training.",
                x + 320 > 1060 ? 40 : x + 320, 878, 1360 - (x + 320 > 1060 ? 40 : x + 320), 50, 18, Theme.Muted, TextAlignmentOptions.Right);
            return root;
        }

        static void Chip(Transform parent, string text, float x, float y, Color c)
        {
            var chip = UIFactory.Panel(parent, x, y, 200, 40, c.WithAlpha(0.22f), 2f, "Chip");
            UIFactory.Label(chip.transform, text, 0, 0, 200, 40, 17, c, TextAlignmentOptions.Center, true);
        }

        static void ScoreBar(Transform parent, string label, int value, float y, string caption)
        {
            UIFactory.Label(parent, label, 30, y, 270, 34, 22, Theme.Text, TextAlignmentOptions.Left, true);
            UIFactory.Label(parent, value + "%", 300, y, 70, 34, 26, Theme.ScoreColor(value), TextAlignmentOptions.Right, true);
            UIFactory.Bar(parent, 30, y + 40, 340, 18, Theme.ScoreColor(value), value / 100f);
            UIFactory.Label(parent, caption, 30, y + 64, 340, 28, 18, Theme.Muted);
        }

        static void Tile(Transform parent, float x, float y, string label, string value, Color c)
        {
            var t = UIFactory.Panel(parent, x, y, 200, 112, Theme.PanelLight.WithAlpha(0.5f), 1f, "Tile " + label);
            UIFactory.Panel(t.transform, 0, 0, 8, 112, c, 0.3f, "Stripe");
            UIFactory.Label(t.transform, value, 24, 12, 170, 56, 44, c, TextAlignmentOptions.Left, true);
            UIFactory.Label(t.transform, label, 24, 70, 170, 32, 19, Theme.Muted);
        }

        static void Timeline(Transform parent, ScenarioResult r, float x, float y, float w, float h)
        {
            var p = UIFactory.Panel(parent, x, y, w, h, Theme.PanelLight.WithAlpha(0.5f), 1f, "Timeline");
            UIFactory.Label(p.transform, "TIMELINE", 20, 10, 200, 24, 18, Theme.Muted, TextAlignmentOptions.Left, true);
            UIFactory.Label(p.transform, "<color=#2FBF71>●</color> correct   <color=#E5484D>●</color> incorrect", w - 360, 10, 340, 24, 18, Theme.Muted, TextAlignmentOptions.Right);
            float span = Mathf.Max(r.timeSeconds, 1f);
            float lx = 20, lw = w - 40, ly = 52;
            UIFactory.Panel(p.transform, lx, ly, lw, 4, Theme.Border, 0.2f, "Axis");
            UIFactory.Label(p.transform, "0:00", lx, ly + 8, 80, 22, 16, Theme.Muted);
            UIFactory.Label(p.transform, ScoringSystem.FormatTime(span), lx + lw - 80, ly + 8, 80, 22, 16, Theme.Muted, TextAlignmentOptions.Right);
            if (r.targetSeconds > 0 && r.targetSeconds < span)
            {
                float tx = lx + lw * r.targetSeconds / span;
                UIFactory.Panel(p.transform, tx - 1, ly - 14, 3, 32, Theme.Amber, 0.2f, "Target");
                UIFactory.Label(p.transform, "target", tx - 40, ly + 8, 80, 22, 15, Theme.Amber, TextAlignmentOptions.Center);
            }
            foreach (var m in r.timeline)
            {
                float mx = lx + lw * Mathf.Clamp01(m.time / span);
                var dot = UIFactory.Icon(p.transform, Theme.Circle, mx - 9, ly - 7, 18, m.good ? Theme.Green : Theme.Red);
                dot.name = m.good ? "Correct Mark" : "Incorrect Mark";
            }
        }
    }
}
