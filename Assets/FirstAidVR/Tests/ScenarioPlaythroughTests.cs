using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FirstAidVR.Tests
{
    /// <summary>
    /// Full playthroughs of every scenario in the real scenes, using the XR rig's own
    /// interactors. Run with Window > General > Test Runner > PlayMode, or in batch mode.
    /// </summary>
    public class ScenarioPlaythroughTests
    {
        [SetUp]
        public void Setup()
        {
            LogAssert.ignoreFailingMessages = true; // OpenXR logs errors when no headset is connected
            Application.targetFrameRate = 72;
        }

        static ScenarioController Ctrl => ScenarioController.Active;

        static IEnumerator Begin()
        {
            var ui = Object.FindFirstObjectByType<ScenarioUI>();
            yield return TestRig.Until(() => ui.Board != null, 3f, "guide board");
            yield return TestRig.Wait(0.3f);
            ui.Begin();
            yield return null;
            Assert.AreEqual(ScenarioPhase.Active, Ctrl.Phase);
        }

        static IEnumerator WaitForResult(float timeout = 20f)
        {
            yield return TestRig.Until(() => Ctrl.Phase == ScenarioPhase.Finished, timeout, "scenario finished");
            yield return TestRig.Wait(0.5f);
        }

        static void Expect(string step) => Assert.IsTrue(Ctrl.IsDone(step), "Step should be done: " + step);

        static string Summary(ScenarioResult r) =>
            $"overall {r.overallScore} tech {r.technicalScore} eff {r.efficiencyScore} time {r.timeSeconds:0.0}s mistakes [{string.Join(", ", r.mistakes)}] missed [{string.Join(", ", r.missedCritical)}]";

        // ---- Severe bleeding ------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Bleeding_Training_FullCorrectRun() => BleedingRun(SimMode.Training);

        [UnityTest]
        public IEnumerator Bleeding_Certification_FullCorrectRun_Passes() => BleedingRun(SimMode.Certification);

        IEnumerator BleedingRun(SimMode mode)
        {
            yield return TestRig.Load("bleeding", "Bleeding", mode);
            Assert.AreEqual(mode, Ctrl.Mode);
            TestRig.Shot($"bleeding_{mode}_intro", new Vector3(-0.2f, 1.55f, -0.4f), Quaternion.LookRotation(new Vector3(-0.85f, -0.25f, 1.6f)));
            yield return Begin();
            var script = Object.FindFirstObjectByType<BleedingScenario>();

            yield return TestRig.ScanScene();
            Expect("scene_safe");

            yield return TestRig.MoveHand(false, TestRig.Find<TouchZone>("Patient Shoulder").transform.position);
            Expect("check_patient");
            yield return TestRig.Until(() => ChoicePrompt.Open != null, 5f, "recognition question");
            yield return TestRig.Wait(0.4f);
            TestRig.Shot($"bleeding_{mode}_question", new Vector3(0, 1.6f, -0.1f), ChoicePrompt.Open.transform.rotation);
            yield return TestRig.AnswerCorrect();
            Expect("recognize");
            yield return TestRig.MoveHand(false, new Vector3(0.3f, 1.0f, 0.2f));

            var phone = TestRig.Find<FirstAidItem>("Phone");
            yield return TestRig.Grab(false, phone);
            yield return TestRig.Use(phone);
            yield return TestRig.Until(() => Ctrl.IsDone("call_911"), 4f, "911 call connected");
            yield return TestRig.Release(false);

            var gloves = TestRig.Find<FirstAidItem>("Gloves");
            yield return TestRig.Grab(true, gloves);
            yield return TestRig.Until(() => Ctrl.IsDone("gloves"), 3f, "gloves on");
            yield return TestRig.Release(true);
            Assert.IsTrue(TestRig.Probe(true).Gloved && TestRig.Probe(false).Gloved, "both hands gloved");

            var dressingA = TestRig.Find<FirstAidItem>("Dressing A");
            if (mode == SimMode.Training)
            {
                Assert.AreEqual("apply_dressing", Ctrl.CurrentStep.def.id);
                Assert.IsNotNull(Highlighter.Current, "Training highlights the next item");
            }
            yield return TestRig.Grab(false, dressingA);
            Assert.AreEqual(dressingA.transform, TestRig.Hand(false).GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>().interactablesSelected[0].transform);
            yield return TestRig.PlaceIn(false, dressingA, script.woundZone);
            Expect("apply_dressing");

            // Firm pressure until blood soaks through
            var press = script.pressureZone;
            yield return TestRig.MoveHand(false, press.transform.position);
            TestRig.Probe(false).SimulatedPress = true;
            yield return TestRig.Until(() => script.Soaked, 12f, "dressing soaked through");
            TestRig.Shot($"bleeding_{mode}_pressing", new Vector3(-0.1f, 1.45f, 0.35f), Quaternion.LookRotation(new Vector3(-0.15f, -1.2f, 0.75f)));
            yield return TestRig.AnswerCorrect();
            TestRig.Probe(false).SimulatedPress = false;
            Expect("soak_decision");

            var dressingB = TestRig.Find<FirstAidItem>("Dressing B");
            yield return TestRig.Grab(false, dressingB);
            yield return TestRig.PlaceIn(false, dressingB, script.woundZone);
            Expect("second_dressing");
            yield return TestRig.MoveHand(false, press.transform.position);
            TestRig.Probe(false).SimulatedPress = true;
            yield return TestRig.Until(() => Ctrl.IsDone("apply_pressure"), 12f, "bleeding controlled");
            TestRig.Probe(false).SimulatedPress = false;

            var roll = TestRig.Find<FirstAidItem>("Roller Bandage");
            yield return TestRig.Grab(false, roll);
            yield return TestRig.PlaceIn(false, roll, script.bandageZone);
            Expect("bandage");
            yield return TestRig.MoveHand(true, TestRig.Find<TouchZone>("Patient Fingers").transform.position);
            Expect("check_circulation");

            var blanket = TestRig.Find<FirstAidItem>("Emergency Blanket");
            yield return TestRig.Grab(false, blanket);
            yield return TestRig.PlaceIn(false, blanket, script.blanketZone);
            Expect("keep_warm");
            script.Talk.Say("reassure");

            yield return TestRig.Wait(1.5f);
            TestRig.Shot($"bleeding_{mode}_care_done", new Vector3(1.2f, 1.6f, -0.6f), Quaternion.LookRotation(new Vector3(-1.1f, -1.2f, 1.9f)));
            yield return WaitForResult();
            var r = Ctrl.Result;
            Debug.Log("[FA] Bleeding " + mode + ": " + Summary(r));
            TestRig.Shot($"bleeding_{mode}_dashboard");
            Assert.IsTrue(r.completed, Summary(r));
            Assert.AreEqual(0, r.mistakes.Count, Summary(r));
            Assert.AreEqual(0, r.missedCritical.Count, Summary(r));
            Assert.GreaterOrEqual(r.technicalScore, 95, Summary(r));
            Assert.GreaterOrEqual(r.overallScore, 90, Summary(r));
            if (mode == SimMode.Certification) Assert.IsTrue(r.passed, Summary(r));
            Assert.IsNotNull(Object.FindFirstObjectByType<ScenarioUI>().Dashboard, "dashboard shown");
        }

        [UnityTest]
        public IEnumerator Bleeding_Mistakes_AreRecordedWithFeedback()
        {
            yield return TestRig.Load("bleeding", "Bleeding", SimMode.Training);
            yield return Begin();
            var script = Object.FindFirstObjectByType<BleedingScenario>();
            // Skip straight to care without checking the scene: dressing goes on the legs (wrong place).
            var dressingA = TestRig.Find<FirstAidItem>("Dressing A");
            yield return TestRig.Grab(false, dressingA);
            var legs = TestRig.Find<PlacementZone>("Body Zone").transform.position + new Vector3(0.7f, 0, 0);
            yield return TestRig.MoveHand(false, legs, 0.3f);
            yield return TestRig.Release(false);
            yield return TestRig.Wait(0.3f);
            Assert.IsTrue(Ctrl.HasMistake("dressing_wrong_place"), "wrong place recorded");
            Assert.AreEqual(ToastKind.Bad, FeedbackToast.LastKind);

            // Wrong answer on the recognition question counts as a mistake; training lets you retry.
            yield return TestRig.MoveHand(true, TestRig.Find<TouchZone>("Patient Shoulder").transform.position);
            yield return TestRig.Until(() => ChoicePrompt.Open != null, 5f, "question");
            ChoicePrompt.Open.PickWrong();
            yield return null;
            Assert.IsTrue(Ctrl.HasMistake("underestimated"));
            Assert.IsNotNull(ChoicePrompt.Open, "training keeps the question open after a wrong answer");
            ChoicePrompt.Open.PickCorrect();
            yield return null;

            // Asking him to walk is a mistake.
            script.Talk.Say("moved_patient");
            Assert.IsTrue(Ctrl.HasMistake("moved_patient"));
            var r = Ctrl.Finish(false);
            Debug.Log("[FA] Bleeding mistakes: " + Summary(r));
            Assert.AreEqual(3, r.mistakes.Count);
            Assert.IsFalse(r.completed);
            Assert.Less(r.technicalScore, 60);
        }

        // ---- Infant choking -------------------------------------------------------------------------

        static readonly Quaternion FaceDown = Quaternion.Euler(30, 0, 0);
        static readonly Quaternion FaceUp = Quaternion.Euler(30, 0, 180);

        static IEnumerator BackBlows(InfantChokingScenario s, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var z = s.backZone.transform;
                yield return TestRig.Snap(true, z.position + z.up * 0.3f);
                yield return TestRig.Wait(0.32f);
                yield return TestRig.Snap(true, z.position);
                yield return TestRig.Wait(0.05f);
            }
            yield return TestRig.Snap(true, s.backZone.transform.position + s.backZone.transform.up * 0.3f);
        }

        static IEnumerator ChestThrusts(InfantChokingScenario s, int n)
        {
            var z = s.chestZone.transform;
            for (int i = 0; i < n; i++)
            {
                yield return TestRig.MoveHand(true, z.position + z.up * 0.05f, 0.08f);
                yield return TestRig.MoveHand(true, z.position - z.up * 0.035f, 0.08f);
            }
            yield return TestRig.MoveHand(true, z.position + z.up * 0.05f, 0.08f);
            yield return TestRig.MoveHand(true, z.position + z.up * 0.3f, 0.08f);
        }

        [UnityTest]
        public IEnumerator Infant_Training_FullCorrectRun() => InfantRun(SimMode.Training);

        [UnityTest]
        public IEnumerator Infant_Certification_FullCorrectRun_Passes() => InfantRun(SimMode.Certification);

        IEnumerator InfantRun(SimMode mode)
        {
            yield return TestRig.Load("infant", "InfantChoking", mode);
            TestRig.Shot($"infant_{mode}_intro", new Vector3(-0.2f, 1.55f, -0.4f), Quaternion.LookRotation(new Vector3(-0.85f, -0.2f, 1.4f)));
            yield return Begin();
            var s = Object.FindFirstObjectByType<InfantChokingScenario>();
            yield return TestRig.ScanScene();
            Expect("scene_safe");

            yield return TestRig.Grab(false, s.infant);
            Expect("check_infant");
            yield return TestRig.AnswerCorrect();
            Expect("recognize");
            yield return TestRig.Until(() => s.Talk != null, 2f, "parent talk menu");
            s.Talk.Say("call_911");
            Expect("call_911");

            // Hold face-down, head lower than chest
            yield return TestRig.MoveHand(false, new Vector3(0.05f, 1.05f, 0.45f), 0.3f, FaceDown);
            yield return TestRig.Until(() => Ctrl.IsDone("position_down"), 3f, "face-down position");
            Assert.IsTrue(s.FaceDown && s.HeadLow);
            TestRig.Shot($"infant_{mode}_facedown", new Vector3(0.7f, 1.4f, 0.1f), Quaternion.LookRotation(new Vector3(-0.65f, -0.35f, 0.4f)));

            yield return BackBlows(s, 5);
            Assert.AreEqual(5, Ctrl.CountOf("back_blows_1"), "5 back blows counted");
            Expect("back_blows_1");

            yield return TestRig.MoveHand(false, new Vector3(0.05f, 1.05f, 0.45f), 0.3f, FaceUp);
            Assert.IsTrue(s.FaceUp && s.HeadLow, "face-up, head low");
            yield return ChestThrusts(s, 5);
            Assert.AreEqual(5, Ctrl.CountOf("chest_thrusts_1"), "5 chest thrusts counted");
            Expect("chest_thrusts_1");

            yield return TestRig.MoveHand(false, new Vector3(0.05f, 1.05f, 0.45f), 0.3f, FaceDown);
            yield return BackBlows(s, 5);
            Expect("back_blows_2");
            Assert.IsTrue(s.ObjectOut, "object expelled");

            yield return TestRig.MoveHand(true, s.breathZone.transform.position, 0.2f);
            yield return TestRig.Until(() => Ctrl.IsDone("monitor"), 4f, "breathing check");
            yield return TestRig.MoveHand(true, new Vector3(-0.3f, 1.1f, 0.3f), 0.2f);

            yield return WaitForResult();
            var r = Ctrl.Result;
            Debug.Log("[FA] Infant " + mode + ": " + Summary(r));
            TestRig.Shot($"infant_{mode}_dashboard");
            Assert.IsTrue(r.completed, Summary(r));
            Assert.AreEqual(0, r.mistakes.Count, Summary(r));
            Assert.GreaterOrEqual(r.overallScore, 90, Summary(r));
            if (mode == SimMode.Certification) Assert.IsTrue(r.passed, Summary(r));
        }

        [UnityTest]
        public IEnumerator Infant_DangerousActions_AreCriticalMistakes()
        {
            yield return TestRig.Load("infant", "InfantChoking", SimMode.Certification);
            yield return Begin();
            var s = Object.FindFirstObjectByType<InfantChokingScenario>();
            yield return TestRig.Grab(false, s.infant);
            yield return TestRig.AnswerCorrect();
            // Blind finger sweep: a deliberate poke into the mouth with the free hand
            yield return TestRig.MoveHand(true, s.mouthZone.transform.position, 0.2f);
            TestRig.Probe(true).SimulatedPress = true;
            yield return TestRig.Wait(0.7f);
            TestRig.Probe(true).SimulatedPress = false;
            Assert.IsTrue(Ctrl.HasMistake("finger_sweep"), "finger sweep recorded");
            yield return TestRig.MoveHand(true, new Vector3(-0.3f, 1.2f, 0.3f), 0.15f);
            // Abdominal thrust while face-up
            yield return TestRig.MoveHand(false, new Vector3(0.05f, 1.05f, 0.45f), 0.3f, FaceUp);
            var b = s.bellyZone.transform;
            yield return TestRig.MoveHand(true, b.position + b.up * 0.05f, 0.1f);
            yield return TestRig.MoveHand(true, b.position - b.up * 0.035f, 0.1f);
            Assert.IsTrue(Ctrl.HasMistake("abdominal_thrust"), "abdominal thrust recorded");
            // In certification there is no corrective feedback
            Assert.AreNotEqual("Abdominal thrusts on an infant", FeedbackToast.LastTitle);
            var r = Ctrl.Finish(false);
            Debug.Log("[FA] Infant dangerous: " + Summary(r));
            Assert.IsFalse(r.passed);
        }

        [UnityTest]
        public IEnumerator Infant_GrabNearHead_AndRelease_AreNotFingerSweeps()
        {
            yield return TestRig.Load("infant", "InfantChoking", SimMode.Certification);
            yield return Begin();
            var s = Object.FindFirstObjectByType<InfantChokingScenario>();
            // Reach in at the head end with grip held, then pick the baby up from there.
            yield return TestRig.MoveHand(false, s.mouthZone.transform.position, 0.2f);
            TestRig.Probe(false).SimulatedPress = true;
            TestRig.Interactor(false).StartManualInteraction((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)s.infant.Grab);
            yield return TestRig.Wait(1.0f);
            Assert.IsTrue(s.Held);
            Assert.IsFalse(Ctrl.HasMistake("finger_sweep"), "grabbing near the head is not a finger sweep");
            yield return TestRig.AnswerCorrect();
            // Hold face-down, start back blows, then let go mid-procedure with grip still held near the face.
            yield return TestRig.MoveHand(false, new Vector3(0.05f, 1.05f, 0.45f), 0.3f, FaceDown);
            yield return TestRig.Until(() => Ctrl.IsDone("position_down"), 3f, "position");
            yield return TestRig.Release(false);
            yield return TestRig.MoveHand(false, s.mouthZone.transform.position, 0.1f);
            yield return TestRig.Wait(1.0f);
            TestRig.Probe(false).SimulatedPress = false;
            Assert.IsTrue(Ctrl.HasMistake("unsupported"), "letting go mid-procedure is recorded");
            Assert.IsFalse(Ctrl.HasMistake("finger_sweep"), "letting go is not a finger sweep");
            yield return TestRig.Until(() => Vector3.Distance(s.infant.transform.position, s.restPoint.position) < 0.01f, 3f, "baby back in seat");
        }

        [UnityTest]
        public IEnumerator Bleeding_PauseStopsClock_AndReleasedPressureIsMistake()
        {
            yield return TestRig.Load("bleeding", "Bleeding", SimMode.Training);
            yield return Begin();
            var ui = Object.FindFirstObjectByType<ScenarioUI>();
            var script = Object.FindFirstObjectByType<BleedingScenario>();
            yield return TestRig.Wait(0.5f);
            ui.TogglePause();
            Assert.IsTrue(ui.PauseOpen && Ctrl.Held);
            float t = Ctrl.Elapsed;
            yield return TestRig.Wait(1.0f);
            Assert.AreEqual(t, Ctrl.Elapsed, 0.0001f, "clock stopped while paused");
            TestRig.Shot("bleeding_pause");
            ui.TogglePause();
            Assert.IsFalse(ui.PauseOpen || Ctrl.Held);

            var dressing = TestRig.Find<FirstAidItem>("Dressing A");
            yield return TestRig.Grab(false, dressing);
            yield return TestRig.PlaceIn(false, dressing, script.woundZone);
            yield return TestRig.MoveHand(false, script.pressureZone.transform.position);
            TestRig.Probe(false).SimulatedPress = true;
            yield return TestRig.Until(() => script.pressureZone.progress > 0.2f, 6f, "pressing");
            TestRig.Probe(false).SimulatedPress = false;
            yield return TestRig.Wait(0.2f);
            Assert.IsTrue(Ctrl.HasMistake("pressure_released"), "letting go early is recorded");
            Assert.IsFalse(Ctrl.IsDone("apply_pressure"));
        }

        [UnityTest]
        public IEnumerator Infant_Certification_TimeLimit_EndsRun()
        {
            yield return TestRig.Load("infant", "InfantChoking", SimMode.Certification);
            var copy = Object.Instantiate(Ctrl.definition);
            copy.certificationTimeLimit = 2f;
            Ctrl.definition = copy;
            Ctrl.Reset();
            yield return Begin();
            yield return TestRig.Until(() => Ctrl.Phase == ScenarioPhase.Finished, 5f, "time limit");
            var r = Ctrl.Result;
            Debug.Log("[FA] Infant time-up: " + Summary(r));
            Assert.IsTrue(r.timeExpired);
            Assert.IsFalse(r.passed);
            Assert.Greater(r.missedCritical.Count, 0);
            yield return TestRig.Wait(0.5f);
            TestRig.Shot("infant_timeup_dashboard");
        }

        // ---- Shock -------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Shock_Training_FullCorrectRun() => ShockRun(SimMode.Training);

        [UnityTest]
        public IEnumerator Shock_Certification_FullCorrectRun_Passes() => ShockRun(SimMode.Certification);

        IEnumerator ShockRun(SimMode mode)
        {
            yield return TestRig.Load("shock", "Shock", mode);
            TestRig.Shot($"shock_{mode}_intro", new Vector3(-0.2f, 1.55f, -0.4f), Quaternion.LookRotation(new Vector3(-0.85f, -0.25f, 1.6f)));
            yield return Begin();
            var s = Object.FindFirstObjectByType<ShockScenario>();
            yield return TestRig.ScanScene();
            Expect("scene_safe");
            yield return TestRig.MoveHand(false, s.shoulderTouch.transform.position);
            Expect("check_patient");
            yield return TestRig.AnswerCorrect();
            Expect("recognize");

            var phone = TestRig.Find<FirstAidItem>("Phone");
            yield return TestRig.Grab(false, phone);
            yield return TestRig.Use(phone);
            yield return TestRig.Until(() => Ctrl.IsDone("call_911"), 4f, "911 call");
            yield return TestRig.Release(false);

            // Help him lie down: hold the shoulder and lower your hand
            var handle = s.lowering.GetComponent<XRSimpleInteractable>();
            yield return TestRig.GrabSimple(false, handle);
            var start = TestRig.Hand(false).position;
            yield return TestRig.MoveHand(false, start + Vector3.down * 0.42f + Vector3.left * 0.2f, 1.0f);
            yield return TestRig.Until(() => Ctrl.IsDone("lay_down"), 3f, "lying down");
            yield return TestRig.Release(false);
            yield return TestRig.MoveHand(false, new Vector3(0.3f, 1.1f, 0.3f), 0.2f);

            var blanket = TestRig.Find<FirstAidItem>("Blanket");
            yield return TestRig.Grab(false, blanket);
            yield return TestRig.PlaceIn(false, blanket, s.blanketZone);
            Expect("keep_warm");

            var pack = TestRig.Find<FirstAidItem>("Backpack");
            yield return TestRig.Grab(false, pack);
            yield return TestRig.PlaceIn(false, pack, s.feetZone);
            Expect("raise_legs");

            yield return TestRig.Until(() => s.Talk != null, 2f, "talk menu");
            s.Talk.Say("reassure");
            Expect("reassure");

            yield return TestRig.MoveHand(true, s.breathZone.transform.position, 0.2f);
            yield return TestRig.Until(() => Ctrl.IsDone("monitor"), 5f, "breathing monitored");
            yield return TestRig.MoveHand(true, new Vector3(-0.3f, 1.1f, 0.3f), 0.2f);
            TestRig.Shot($"shock_{mode}_care_done", new Vector3(1.1f, 1.6f, 0.0f), Quaternion.LookRotation(new Vector3(-1.0f, -1.3f, 1.3f)));

            yield return WaitForResult();
            var r = Ctrl.Result;
            Debug.Log("[FA] Shock " + mode + ": " + Summary(r));
            TestRig.Shot($"shock_{mode}_dashboard");
            Assert.IsTrue(r.completed, Summary(r));
            Assert.AreEqual(0, r.mistakes.Count, Summary(r));
            Assert.GreaterOrEqual(r.overallScore, 90, Summary(r));
            if (mode == SimMode.Certification) Assert.IsTrue(r.passed, Summary(r));
        }

        [UnityTest]
        public IEnumerator Shock_GivingWater_IsCriticalMistake()
        {
            yield return TestRig.Load("shock", "Shock", SimMode.Training);
            yield return Begin();
            var s = Object.FindFirstObjectByType<ShockScenario>();
            var water = TestRig.Find<FirstAidItem>("Water Bottle");
            yield return TestRig.Grab(false, water);
            yield return TestRig.MoveHand(false, s.mouthZone.transform.position, 0.3f);
            yield return TestRig.Until(() => Ctrl.HasMistake("gave_drink"), 3f, "drink mistake");
            Assert.AreEqual(ToastKind.Bad, FeedbackToast.LastKind);
            yield return TestRig.Release(false);
            var r = Ctrl.Finish(false);
            Assert.IsFalse(r.passed);
        }

        // ---- Main menu ---------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Menu_Navigation_StartsScenarioInChosenMode()
        {
            yield return TestRig.Load(null, "MainMenu", SimMode.Training);
            var menu = Object.FindFirstObjectByType<MainMenuManager>();
            yield return null;
            Assert.AreEqual(MainMenuManager.Page.Home, menu.CurrentPage);
            TestRig.Shot("menu_home", new Vector3(0, 1.5f, 0.1f), Quaternion.LookRotation(new Vector3(0, -0.05f, 1)));
            Click("Settings Button");
            yield return null;
            Assert.AreEqual(MainMenuManager.Page.Settings, menu.CurrentPage);
            bool before = GameSettings.SmoothTurn;
            Click("Setting Turning");
            Assert.AreNotEqual(before, GameSettings.SmoothTurn);
            GameSettings.SmoothTurn = before;
            TestRig.Shot("menu_settings", new Vector3(0, 1.5f, 0.1f), Quaternion.LookRotation(new Vector3(0, -0.05f, 1)));
            Click("Back Button");
            yield return null;
            Click("Controls Button");
            yield return null;
            TestRig.Shot("menu_controls", new Vector3(0, 1.5f, 0.1f), Quaternion.LookRotation(new Vector3(0, -0.05f, 1)));
            Click("Back Button");
            yield return null;
            Click("Results Button");
            yield return null;
            TestRig.Shot("menu_results", new Vector3(0, 1.5f, 0.1f), Quaternion.LookRotation(new Vector3(0, -0.05f, 1)));
            Click("Back Button");
            yield return null;
            Click("Certification Button");
            yield return null;
            Assert.AreEqual(MainMenuManager.Page.Select, menu.CurrentPage);
            Assert.AreEqual(SimMode.Certification, menu.PendingMode);
            TestRig.Shot("menu_select", new Vector3(0, 1.5f, 0.1f), Quaternion.LookRotation(new Vector3(0, -0.05f, 1)));
            Click("Start shock");
            yield return TestRig.Until(() => SceneManager.GetActiveScene().name == "Shock" && ScenarioController.Active != null, 5f, "shock scene loaded");
            Assert.AreEqual(SimMode.Certification, ScenarioController.Active.Mode);
            Assert.AreEqual("shock", SimulationManager.ScenarioId);
        }

        static void Click(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, "button " + name);
            go.GetComponent<Button>().onClick.Invoke();
        }

        // ---- Content sanity ----------------------------------------------------------------------------

        [Test]
        public void Content_AllHighlightTargetsAndIdsAreConsistent()
        {
            var cat = SimulationManager.Catalog;
            Assert.IsNotNull(cat, "catalog in Resources");
            Assert.AreEqual(3, cat.scenarios.Count);
            foreach (var d in cat.scenarios)
            {
                var ids = new HashSet<string>();
                foreach (var s in d.steps)
                {
                    Assert.IsTrue(ids.Add(s.id), $"{d.id}: duplicate step {s.id}");
                    foreach (var a in s.after) Assert.IsNotNull(d.FindStep(a), $"{d.id}.{s.id} after unknown {a}");
                    foreach (var a in s.requires) Assert.IsNotNull(d.FindStep(a), $"{d.id}.{s.id} requires unknown {a}");
                    Assert.IsFalse(string.IsNullOrEmpty(s.instruction), $"{d.id}.{s.id} has instruction");
                }
                foreach (var m in d.mistakes) Assert.IsTrue(ids.Add(m.id), $"{d.id}: mistake id clashes {m.id}");
                Assert.Greater(d.certificationTimeLimit, d.targetTime);
            }
        }

        [Test]
        public void Font_HasSymbolGlyphs()
        {
            var f = Theme.Font;
            Assert.IsNotNull(f);
            foreach (char ch in "✓✗•●▶←")
                Assert.IsTrue(f.HasCharacter(ch, false, true), $"font {f.name} is missing U+{(int)ch:X4}");
        }
    }
}
