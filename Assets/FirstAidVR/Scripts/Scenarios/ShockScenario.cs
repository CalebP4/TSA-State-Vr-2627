using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Scenario 3 - Shock (non-injury: fluid loss from vomiting and diarrhoea), following the
    /// AHA/Red Cross 2024 First Aid Guidelines: recognise shock -> call 911 -> help the person lie
    /// on their back -> (no injury) feet may be raised 6-12 in -> keep warm -> no food or drink ->
    /// reassure -> monitor breathing until EMS arrives.
    /// </summary>
    public class ShockScenario : ScenarioScript
    {
        public TouchZone shoulderTouch;
        public GuidedLowering lowering;
        public PlacementZone feetZone;
        public Transform legs;
        public PlacementZone blanketZone;
        public PlacementZone mouthZone;
        public TouchZone breathZone;
        public PatientVisuals friend;

        float legRaise, legTarget;
        bool askedForWater;

        protected override void Start()
        {
            base.Start();
            patient.pallor = 0.75f;
            patient.breathRate = 28f;
            lowering.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().enabled = false;
            lowering.Lowered += () =>
            {
                patient.Say("That's... a little better.", 3f);
                breathZone.armed = true;
            };
            shoulderTouch.Touched += _ => OnChecked();
            feetZone.CanPlace = _ => lowering.Done;
            feetZone.Placed += (item, i) => legTarget = 14f;
            blanketZone.Placed += (item, i) => patient.Say("Thank you... I was freezing.", 3f);
            breathZone.actionId = "monitor";
            breathZone.dwellSeconds = 3f;
            breathZone.armed = false;
            breathZone.Touched += _ => patient.Say("*breathing fast*", 2f);
            if (phone != null)
                phone.dispatcherLines = new[]
                {
                    "911. What is the address of your emergency?",
                    "An ambulance is on the way. Stay with him and call back if anything changes.",
                };
        }

        protected override void OnBegin()
        {
            if (friend != null) friend.Say("Please help! My friend has been sick all day and now he can barely stay awake!", 5f);
        }

        void OnChecked()
        {
            patient.Say("I feel... really weak. Dizzy. And so thirsty...");
            if (controller.IsDone("recognize")) return;
            Later(1.2f, () => Ask("Check the person",
                "His skin is pale, cool and clammy. He is breathing fast, seems restless and a little confused, and feels weak. " +
                "He has lost a lot of fluid from vomiting and diarrhoea. There are no injuries.",
                "What is happening?",
                new List<Choice>
                {
                    new Choice { label = "He may be in shock - this is a medical emergency", actionId = "recognize", correct = true,
                        explanation = "Pale, cool, clammy skin, fast breathing and confusion after losing fluid are signs of shock. Call 911." },
                    new Choice { label = "He is just tired from hiking - let him rest", actionId = "underestimated",
                        explanation = "These are warning signs of shock, which can get worse quickly. Call 911." },
                    new Choice { label = "He is only dehydrated - some water will fix it", actionId = "underestimated",
                        explanation = "He may be in shock. Do not give food or drink, and call 911." },
                },
                _ =>
                {
                    lowering.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().enabled = true;
                    ShowTalk(new List<TalkOption>
                    {
                        new TalkOption { label = "Help is coming. Lie still - I'm staying right here with you.", actionId = "reassure",
                            reply = "Okay... thank you. Please stay." },
                        new TalkOption { label = "Try to get up and walk it off.", actionId = "walk_it_off", reply = "I... I can't. Everything is spinning." },
                        new TalkOption { label = "Here, have a sip of water.", actionId = "gave_drink", reply = "*sips* ...I feel sick again." },
                    });
                }));
        }

        void Update()
        {
            if (controller.Phase == ScenarioPhase.Active && !askedForWater && controller.IsDone("recognize") && controller.Elapsed > 0f)
            {
                askedForWater = true;
                Later(12f, () => { if (controller.Phase == ScenarioPhase.Active) patient.Say("Could I have some water? Please... I'm so thirsty.", 4f); });
            }
            if (legs != null && !Mathf.Approximately(legRaise, legTarget))
            {
                legRaise = Mathf.MoveTowards(legRaise, legTarget, Time.deltaTime * 20f);
                legs.localRotation = Quaternion.Euler(0, 0, legRaise);
            }
        }
    }
}
