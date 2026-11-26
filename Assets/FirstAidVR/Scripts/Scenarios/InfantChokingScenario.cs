using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Scenario 2 - Conscious choking infant (under 1 year), following AHA/AAP 2025:
    /// recognise severe choking -> have someone call 911 -> 5 back blows (face-down along the
    /// forearm, head lower than chest) -> 5 chest thrusts (face-up, heel of hand) -> repeat
    /// until the object comes out -> monitor until EMS arrives.
    ///
    /// How the hold works in VR: grab the baby and it lies along your forearm, face-down
    /// when your hand is palm-down. Tilt your hand down to lower the head. Turn your hand
    /// over to roll the baby face-up.
    /// </summary>
    public class InfantChokingScenario : ScenarioScript
    {
        [Header("Infant")]
        public FirstAidItem infant;
        public Transform restPoint;
        public PatientVisuals parentNpc;
        public Transform parentTalkAnchor;
        public TouchZone checkTouch;
        public StrikeZone backZone;
        public CompressionZone chestZone;
        public CompressionZone bellyZone;
        public TouchZone mouthZone;
        public TouchZone breathZone;
        public GameObject foodPiece;

        public bool Held => infant != null && infant.IsHeld;
        public bool FaceDown { get; private set; }
        public bool FaceUp { get; private set; }
        public bool HeadLow { get; private set; }
        public bool ObjectOut { get; private set; }

        float goodPoseSince = -1f;
        bool releaseGrace; // the hand that just let go may still be near the baby's face
        float hintAt;
        Coroutine returning;

        protected override void Start()
        {
            base.Start();
            infant.returnHome = false;
            infant.Grabbed += OnGrab;
            infant.Released += OnRelease;
            checkTouch.Touched += _ => OnChecked();
            controller.StepProgressed += _ => lastPose = null;

            backZone.actionId = "";
            backZone.Condition = () => Held && FaceDown && HeadLow && !ObjectOut;
            backZone.Struck += OnBackBlow;
            backZone.NotReady += () => Hint("Position first", "Hold the baby face-down along your forearm with the head lower than the chest.");
            backZone.WeakStrike += () => Hint("Firmer", "Give firm back blows with the heel of your hand.");

            chestZone.actionId = "";
            chestZone.Condition = () => Held && FaceUp && HeadLow && !ObjectOut;
            chestZone.Compressed += () => controller.Report("chest_thrusts_1");
            chestZone.NotReady += () => Hint("Turn the baby over", "Turn your hand over so the baby is face-up, head still lower than the chest.");

            bellyZone.actionId = "abdominal_thrust";
            bellyZone.Condition = () => Held && FaceUp && !ObjectOut;
            // A finger sweep needs a deliberate poke (grip/trigger held, short dwell) with the FREE hand,
            // so simply picking the baby up near the head is never counted as a mistake.
            mouthZone.actionId = "finger_sweep";
            mouthZone.requirePress = true;
            mouthZone.dwellSeconds = 0.4f;
            mouthZone.Condition = () => !releaseGrace;
            breathZone.actionId = "monitor";
            breathZone.dwellSeconds = 2f;
            breathZone.armed = false;
            if (foodPiece != null) foodPiece.SetActive(false);
            patient.pallor = 0.35f;

            if (phone != null)
                phone.dispatcherLines = new[]
                {
                    "911. What is the address of your emergency?",
                    "Paramedics are on the way. Stay on the line on speaker.",
                };
        }

        protected override void OnBegin()
        {
            if (parentNpc != null) parentNpc.Say("She's choking! She can't breathe! What do I do?!", 5f);
        }

        void OnChecked()
        {
            if (controller.IsDone("recognize")) return;
            Sfx.Play(Sound.Breath, infant.transform.position, 0.2f, 1.6f);
            Later(0.6f, () => Ask("Check the baby",
                "The baby's mouth is open but no sound comes out. She cannot cry or cough, and her lips are starting to turn bluish. She looks panicked.",
                "What is happening?",
                new List<Choice>
                {
                    new Choice { label = "Severe choking - the airway is blocked", actionId = "recognize", correct = true,
                        explanation = "No cry, no cough and no breathing means a severe blockage. Act now: back blows and chest thrusts." },
                    new Choice { label = "Mild choking - let her keep coughing", actionId = "underestimated",
                        explanation = "She cannot cough or make a sound. Letting a baby cough is only right if they CAN cough forcefully or cry." },
                    new Choice { label = "She is just upset - give her a cuddle", actionId = "underestimated",
                        explanation = "A silent, bluish baby who cannot breathe is choking. Every second counts." },
                },
                _ =>
                {
                    if (parentTalkAnchor != null && parentNpc != null)
                        talk = TalkMenu.Create(parentTalkAnchor, parentNpc, new List<TalkOption>
                        {
                            new TalkOption { label = "Call 911 now and put it on speaker!", actionId = "call_911", reply = "Okay! Calling 911... It's ringing!" },
                            new TalkOption { label = "Let's wait and see if it comes out by itself.", actionId = "waited", reply = "Wait?! She's turning blue!" },
                        });
                }));
        }

        void OnGrab(FirstAidItem _)
        {
            if (returning != null) { StopCoroutine(returning); returning = null; }
            var hand = infant.HeldBy;
            foreach (var z in new HandZone[] { backZone, chestZone, bellyZone, mouthZone, breathZone, checkTouch })
                if (z != null) z.Ignore = hand;
            if (!controller.IsDone("check_infant")) controller.Report("check_infant");
        }

        void OnRelease(FirstAidItem _)
        {
            if (this == null || !isActiveAndEnabled) return;
            if (controller.Phase == ScenarioPhase.Active && controller.IsDone("position_down") && !ObjectOut)
                controller.Report("unsupported");
            foreach (var z in new HandZone[] { backZone, chestZone, bellyZone, mouthZone, breathZone, checkTouch })
                if (z != null) z.Ignore = null;
            releaseGrace = true;
            Later(1.2f, () => releaseGrace = false);
            returning = StartCoroutine(ReturnToRest());
        }

        IEnumerator ReturnToRest()
        {
            infant.Body.isKinematic = true;
            Vector3 p0 = infant.transform.position;
            Quaternion r0 = infant.transform.rotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
            {
                float e = Mathf.SmoothStep(0, 1, t);
                infant.transform.SetPositionAndRotation(Vector3.Lerp(p0, restPoint.position, e), Quaternion.Slerp(r0, restPoint.rotation, e));
                yield return null;
            }
            infant.transform.SetPositionAndRotation(restPoint.position, restPoint.rotation);
            returning = null;
        }

        void OnBackBlow()
        {
            if (!controller.IsDone("back_blows_1")) controller.Report("back_blows_1");
            else if (!controller.IsDone("chest_thrusts_1"))
                Hint("Now chest thrusts", "You have given 5 back blows. Turn the baby face-up for 5 chest thrusts.");
            else if (controller.Report("back_blows_2") && controller.IsDone("back_blows_2")) Expel();
        }

        void Expel()
        {
            if (ObjectOut) return;
            ObjectOut = true;
            if (foodPiece != null)
            {
                foodPiece.SetActive(true);
                foodPiece.transform.SetParent(null, true);
                var rb = foodPiece.AddComponent<Rigidbody>();
                rb.mass = 0.01f;
                rb.AddForce(Vector3.down * 0.3f, ForceMode.VelocityChange);
            }
            patient.pallor = 0f;
            patient.breathRate = 40f;
            Sfx.Play(Sound.Cough, infant.transform.position, 0.6f, 1.7f);
            Later(0.5f, () => Sfx.Play(Sound.Cry, infant.transform.position, 0.8f));
            patient.Say("*coughs* ... WAAAH!", 3f);
            if (parentNpc != null) Later(1.5f, () => parentNpc.Say("She's crying! Oh, thank goodness!", 4f));
            breathZone.armed = true;
            Guide("The object is out!", "The baby is crying. Rest your hand on the baby's chest to check breathing, and stay with her until EMS arrives.", "Breathing Zone");
        }

        void Update()
        {
            if (infant == null) return;
            var t = infant.transform;
            // Infant local axes: +Y = front (face/chest), -X = towards the head.
            float faceDot = Vector3.Dot(t.up, Vector3.down);
            FaceDown = faceDot > 0.45f;
            FaceUp = faceDot < -0.45f;
            HeadLow = Vector3.Dot(-t.right, Vector3.down) > 0.1f;

            if (Held && controller.Phase == ScenarioPhase.Active && !controller.IsDone("position_down") && FaceDown && HeadLow)
            {
                if (goodPoseSince < 0f) goodPoseSince = Time.time;
                if (Time.time - goodPoseSince > 0.4f) controller.Report("position_down");
            }
            else goodPoseSince = -1f;

            if (!ObjectOut && controller.Phase == ScenarioPhase.Active)
                patient.pallor = Mathf.Min(0.9f, patient.pallor + Time.deltaTime * 0.004f);

            if (Held && IsTraining && controller.Phase == ScenarioPhase.Active && !ObjectOut && ChoicePrompt.Open == null)
                ShowPose();
        }

        string lastPose;

        void ShowPose()
        {
            string Mark(bool ok) => ok ? "<color=#2FBF71>✓</color>" : "<color=#E5484D>✗</color>";
            var cur = controller.CurrentStep;
            if (cur == null) return;
            bool wantUp = cur.def.id == "chest_thrusts_1";
            string pose = $"\n\n<b>Position</b>   {Mark(wantUp ? FaceUp : FaceDown)} {(wantUp ? "Face-up" : "Face-down")}   {Mark(HeadLow)} Head lower than chest";
            if (pose == lastPose) return;
            lastPose = pose;
            string count = cur.def.requiredCount > 1 ? $"\n<color=#F5A524><b>{cur.count} of {cur.def.requiredCount}</b></color>" : "";
            Guide(cur.def.title, cur.def.instruction + count + pose, cur.def.highlightTarget);
        }

        void Hint(string title, string text)
        {
            if (!IsTraining || Time.time < hintAt) return;
            hintAt = Time.time + 3f;
            FeedbackToast.Show(ToastKind.Hint, title, text);
        }

        protected override void OnStep(string id)
        {
            lastPose = null;
            if (id == "call_911" && parentNpc != null && phone != null && !phone.Called)
                Later(2f, () => parentNpc.Say("Dispatcher says help is on the way!", 3f));
        }
    }
}
