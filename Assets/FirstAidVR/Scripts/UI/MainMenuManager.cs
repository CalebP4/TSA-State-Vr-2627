using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstAidVR
{
    /// <summary>
    /// The main menu board: title and branding, Training / Certification, scenario
    /// selection, settings, controller instructions and saved results.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public enum Page { Home, Select, Settings, Controls, Results }

        public Transform boardAnchor;
        public Page CurrentPage { get; private set; }
        public SimMode PendingMode { get; private set; }

        Canvas canvas;
        RectTransform root, page;
        Canvas dashboard;

        const float W = 1500, H = 900;

        void Start()
        {
            var anchor = boardAnchor != null ? boardAnchor : transform;
            canvas = UIFactory.WorldCanvas("Main Menu Board", anchor, Vector3.zero, Quaternion.identity, new Vector2(W, H));
            root = (RectTransform)canvas.transform;
            UIFactory.Card(root);
            UIFactory.Panel(root, 0, 0, W, 10, Theme.Red, 0.3f, "Accent");
            ShowHome();
        }

        RectTransform NewPage(Page p)
        {
            if (page != null) Destroy(page.gameObject);
            if (dashboard != null) Destroy(dashboard.gameObject);
            CurrentPage = p;
            page = UIFactory.Rect("Page " + p, root, 0, 10, W, H - 10);
            return page;
        }

        void Header(RectTransform p, string title, string subtitle)
        {
            UIFactory.Logo(p, 50, 34, 72);
            UIFactory.Label(p, title, 144, 30, 1000, 54, 44, Theme.Text, TextAlignmentOptions.Left, true);
            UIFactory.Label(p, subtitle, 144, 84, 1100, 34, 24, Theme.Muted);
            UIFactory.Button(p, "← Back", W - 250, 36, 200, 70, ShowHome, Theme.PanelLight, 28, "Back Button");
        }

        // ---- Home ----------------------------------------------------------------------

        public void ShowHome()
        {
            var p = NewPage(Page.Home);
            UIFactory.Logo(p, 80, 80, 140);
            var title = UIFactory.Label(p, Theme.ProductName, 80, 250, 700, 90, 68, Theme.Text, TextAlignmentOptions.Left, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Label(p, Theme.Tagline, 80, 350, 700, 44, 30, Theme.Red, TextAlignmentOptions.Left, true);
            UIFactory.Label(p,
                "Respond to three real-world emergencies - severe bleeding, a choking infant and shock - with your own hands. " +
                "Learn the steps in Training Mode, then prove your skills in Certification Mode.",
                80, 424, 680, 180, 27, Theme.Muted);
            UIFactory.Label(p, "Educational simulation only. It does not replace certified first-aid training from organisations such as the American Red Cross or American Heart Association. In a real emergency, call 911.",
                80, 740, 680, 110, 19, Theme.Muted);

            BigButton(p, 820, 80, "Training Mode", "Learn each scenario step by step with guidance and feedback", Theme.Blue,
                () => ShowSelect(SimMode.Training), "Training Button");
            BigButton(p, 820, 250, "Certification Mode", "Timed, no hints - your performance is scored", Theme.Purple,
                () => ShowSelect(SimMode.Certification), "Certification Button");
            UIFactory.Button(p, "Results", 820, 430, 290, 96, ShowResults, Theme.PanelLight, 30, "Results Button");
            UIFactory.Button(p, "Controls", 1130, 430, 290, 96, ShowControls, Theme.PanelLight, 30, "Controls Button");
            UIFactory.Button(p, "Settings", 820, 546, 290, 96, ShowSettings, Theme.PanelLight, 30, "Settings Button");
            UIFactory.Button(p, "Quit", 1130, 546, 290, 96, Application.Quit, Theme.PanelLight, 30, "Quit Button");

            int passed = 0;
            foreach (var s in SimulationManager.Scenarios) if (s != null && ResultsStore.EverPassed(s.id)) passed++;
            var prog = UIFactory.Panel(p, 820, 680, 600, 150, Theme.PanelLight.WithAlpha(0.5f), 1f, "Certification Progress");
            UIFactory.Label(prog.transform, "CERTIFICATION PROGRESS", 24, 18, 552, 28, 19, Theme.Muted, TextAlignmentOptions.Left, true);
            int total = Mathf.Max(1, SimulationManager.Scenarios.Count);
            UIFactory.Label(prog.transform, $"{passed} of {total} scenarios passed", 24, 52, 552, 40, 30, Theme.Text, TextAlignmentOptions.Left, true);
            UIFactory.Bar(prog.transform, 24, 104, 552, 18, Theme.Green, (float)passed / total);
        }

        static void BigButton(Transform p, float x, float y, string title, string sub, Color c, System.Action onClick, string name)
        {
            var b = UIFactory.Button(p, "", x, y, 600, 150, onClick, c, 30, name);
            UIFactory.Label(b.transform, title, 36, 28, 540, 56, 44, Color.white, TextAlignmentOptions.Left, true);
            UIFactory.Label(b.transform, sub, 36, 88, 540, 40, 23, Color.white.WithAlpha(0.85f));
        }

        // ---- Scenario select -------------------------------------------------------------

        public void ShowSelect(SimMode mode)
        {
            PendingMode = mode;
            var p = NewPage(Page.Select);
            Header(p, mode == SimMode.Training ? "Training Mode" : "Certification Mode",
                mode == SimMode.Training ? "Choose a scenario to practise. Instructions and feedback will guide you."
                                         : "Choose a scenario. No instructions or hints - you are timed and scored. Pass mark: 80%.");
            var list = SimulationManager.Scenarios;
            float cardW = 440, gap = 30, x0 = (W - (list.Count * cardW + (list.Count - 1) * gap)) / 2f;
            for (int i = 0; i < list.Count; i++)
            {
                var def = list[i];
                if (def == null) continue;
                ScenarioCard(p, def, mode, x0 + i * (cardW + gap), 160, cardW, 680);
            }
        }

        void ScenarioCard(Transform p, ScenarioDefinition def, SimMode mode, float x, float y, float w, float h)
        {
            var card = UIFactory.Panel(p, x, y, w, h, Theme.PanelLight.WithAlpha(0.6f), 1f, "Card " + def.id);
            UIFactory.Panel(card.transform, 0, 0, w, 12, def.accent, 0.3f, "Accent");
            var icon = UIFactory.Panel(card.transform, 32, 44, 96, 96, def.accent, 1f, "Icon");
            UIFactory.Icon(icon.transform, Theme.Cross, 16, 16, 64, Color.white);
            UIFactory.Label(card.transform, def.title, 32, 162, w - 64, 56, 40, Theme.Text, TextAlignmentOptions.Left, true);
            var sub = UIFactory.Label(card.transform, def.subtitle, 32, 222, w - 64, 170, 24, Theme.Muted);
            sub.enableAutoSizing = true; sub.fontSizeMin = 16; sub.fontSizeMax = 24;

            int bt = ResultsStore.Best(def.id, SimMode.Training), bc = ResultsStore.Best(def.id, SimMode.Certification);
            UIFactory.Label(card.transform, "BEST SCORES", 32, 408, w - 64, 26, 18, Theme.Muted, TextAlignmentOptions.Left, true);
            UIFactory.Label(card.transform, $"Training  <b>{(bt < 0 ? "-" : bt + "%")}</b>\nCertification  <b>{(bc < 0 ? "-" : bc + "%")}</b>",
                32, 440, w - 64, 80, 24, Theme.Text);
            if (ResultsStore.EverPassed(def.id))
            {
                var badge = UIFactory.Panel(card.transform, w - 180, 54, 150, 44, Theme.Green.WithAlpha(0.2f), 2f, "Certified");
                UIFactory.Label(badge.transform, "✓ CERTIFIED", 0, 0, 150, 44, 18, Theme.Green, TextAlignmentOptions.Center, true);
            }
            UIFactory.Label(card.transform, $"Time limit {ScoringSystem.FormatTime(def.certificationTimeLimit)}", 32, 524, w - 64, 30, 20, Theme.Muted);
            UIFactory.Button(card.transform, mode == SimMode.Training ? "Start Training" : "Start Certification",
                32, h - 120, w - 64, 90, () => SimulationManager.StartScenario(def.id, mode),
                mode == SimMode.Training ? Theme.Blue : Theme.Purple, 32, "Start " + def.id);
        }

        // ---- Settings ---------------------------------------------------------------------

        public void ShowSettings()
        {
            var p = NewPage(Page.Settings);
            Header(p, "Settings", "Comfort and audio options. Saved on this headset.");
            float y = 170;
            Row(p, ref y, "Movement", () => GameSettings.SmoothMove ? "Teleport + smooth" : "Teleport only",
                () => GameSettings.SmoothMove = !GameSettings.SmoothMove);
            Row(p, ref y, "Turning", () => GameSettings.SmoothTurn ? "Smooth turn" : "Snap turn",
                () => GameSettings.SmoothTurn = !GameSettings.SmoothTurn);
            Row(p, ref y, "Comfort vignette", () => GameSettings.Comfort ? "On" : "Off",
                () => GameSettings.Comfort = !GameSettings.Comfort);
            Stepper(p, ref y, "Volume", () => Mathf.RoundToInt(GameSettings.Volume * 100) + "%",
                () => GameSettings.Volume -= 0.1f, () => { GameSettings.Volume += 0.1f; Sfx.Play(Sound.Correct); });
            Stepper(p, ref y, "Seated mode height", () => "+" + GameSettings.HeightOffset.ToString("0.0") + " m",
                () => GameSettings.HeightOffset -= 0.1f, () => GameSettings.HeightOffset += 0.1f);
            UIFactory.Button(p, "Clear saved results", 200, y + 20, 400, 80, () => { ResultsStore.ClearAll(); ShowSettings(); }, Theme.Red, 28, "Clear Results Button");
        }

        void Row(Transform p, ref float y, string label, System.Func<string> value, System.Action toggle)
        {
            UIFactory.Label(p, label, 200, y + 18, 500, 50, 32, Theme.Text, TextAlignmentOptions.Left, true);
            Button b = null;
            b = UIFactory.Button(p, value(), 760, y, 540, 86, () => { toggle(); UIFactory.SetLabel(b, value()); }, Theme.PanelLight, 30, "Setting " + label);
            y += 110;
        }

        void Stepper(Transform p, ref float y, string label, System.Func<string> value, System.Action down, System.Action up)
        {
            UIFactory.Label(p, label, 200, y + 18, 500, 50, 32, Theme.Text, TextAlignmentOptions.Left, true);
            var v = UIFactory.Label(p, value(), 880, y + 18, 300, 50, 32, Theme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Button(p, "-", 760, y, 110, 86, () => { down(); v.text = value(); }, Theme.PanelLight, 44, "Setting " + label + " Down");
            UIFactory.Button(p, "+", 1190, y, 110, 86, () => { up(); v.text = value(); }, Theme.PanelLight, 44, "Setting " + label + " Up");
            y += 110;
        }

        // ---- Controls ---------------------------------------------------------------------

        public void ShowControls()
        {
            var p = NewPage(Page.Controls);
            Header(p, "VR Controls", "Meta Quest 2 Touch controllers");
            Box(p, 60, 160, 680, 660, "INTERACT",
                "<b>Grip</b> (side button)\nGrab equipment. Hold to keep it in your hand, release to let go.\n\n" +
                "<b>Trigger</b> (index finger)\nUse what you are holding (e.g. call on the phone) and press menu buttons.\n\n" +
                "<b>Point + Trigger</b>\nAim the laser at a button and pull the trigger. You can also touch buttons with your hand.\n\n" +
                "<b>Pressing on the patient</b>\nPut your hand on the spot and hold Grip or Trigger.");
            Box(p, 760, 160, 680, 660, "MOVE & MENU",
                "<b>Right thumbstick forward</b>\nAim and release to teleport.\n\n" +
                "<b>Right thumbstick left / right</b>\nTurn (snap or smooth in Settings).\n\n" +
                "<b>Left thumbstick</b>\nSmooth movement (when enabled in Settings).\n\n" +
                "<b>Menu button</b> (left controller)\nPause, restart or return to the main menu.\n\n" +
                "<b>Tip:</b> kneel down or use Seated mode to reach a patient on the ground.");
        }

        static void Box(Transform p, float x, float y, float w, float h, string title, string body)
        {
            var b = UIFactory.Panel(p, x, y, w, h, Theme.PanelLight.WithAlpha(0.5f), 1f, "Box " + title);
            UIFactory.Label(b.transform, title, 32, 24, w - 64, 30, 20, Theme.Muted, TextAlignmentOptions.Left, true);
            var t = UIFactory.Label(b.transform, body, 32, 64, w - 64, h - 88, 26, Theme.Text);
            t.enableAutoSizing = true; t.fontSizeMin = 16; t.fontSizeMax = 26;
        }

        // ---- Results -----------------------------------------------------------------------

        public void ShowResults()
        {
            var p = NewPage(Page.Results);
            Header(p, "Results", "Your most recent attempts on this headset. Select one to open its dashboard.");
            var all = ResultsStore.Load().results;
            if (all.Count == 0)
            {
                UIFactory.Label(p, "No results yet. Finish a scenario to see your performance dashboard here.", 60, 200, 1380, 60, 30, Theme.Muted, TextAlignmentOptions.Center);
                return;
            }
            UIFactory.Label(p, "<b>DATE</b>", 80, 160, 300, 30, 20, Theme.Muted);
            UIFactory.Label(p, "<b>SCENARIO</b>", 340, 160, 400, 30, 20, Theme.Muted);
            UIFactory.Label(p, "<b>MODE</b>", 760, 160, 260, 30, 20, Theme.Muted);
            UIFactory.Label(p, "<b>SCORE</b>", 1040, 160, 300, 30, 20, Theme.Muted);
            int rows = Mathf.Min(7, all.Count);
            for (int i = 0; i < rows; i++)
            {
                var r = all[all.Count - 1 - i];
                float y = 200 + i * 92;
                var b = UIFactory.Button(p, "", 60, y, 1380, 80, () => OpenDashboard(r), Theme.PanelLight.WithAlpha(0.7f), 24, "Result " + i);
                UIFactory.Label(b.transform, r.dateIso, 20, 0, 260, 80, 22, Theme.Muted, TextAlignmentOptions.Left);
                UIFactory.Label(b.transform, r.scenarioTitle, 280, 0, 420, 80, 26, Theme.Text, TextAlignmentOptions.Left, true);
                string mode = r.mode == SimMode.Training ? "Training" : r.passed ? "<color=#2FBF71>Certification - passed</color>" : "<color=#E5484D>Certification - not passed</color>";
                UIFactory.Label(b.transform, mode, 700, 0, 300, 80, 22, Theme.Text, TextAlignmentOptions.Left);
                UIFactory.Bar(b.transform, 980, 31, 240, 18, Theme.ScoreColor(r.overallScore), r.overallScore / 100f);
                UIFactory.Label(b.transform, r.overallScore + "%", 1240, 0, 110, 80, 28, Theme.ScoreColor(r.overallScore), TextAlignmentOptions.Right, true);
            }
        }

        public void OpenDashboard(ScenarioResult r)
        {
            if (dashboard != null) Destroy(dashboard.gameObject);
            canvas.gameObject.SetActive(false);
            dashboard = UIFactory.WorldCanvas("Dashboard Canvas", boardAnchor != null ? boardAnchor : transform,
                new Vector3(0, -0.03f, -0.02f), Quaternion.identity, new Vector2(DashboardView.Width, DashboardView.Height));
            DashboardView.Build((RectTransform)dashboard.transform, r, new DashboardView.Buttons
            {
                retry = () => SimulationManager.StartScenario(r.scenarioId, r.mode),
                close = () => { Destroy(dashboard.gameObject); canvas.gameObject.SetActive(true); },
            });
        }
    }
}
