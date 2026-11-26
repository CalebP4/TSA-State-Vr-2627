using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Scenario 1 - Severe bleeding. A cyclist has cut his forearm on broken glass in a park.
    /// Flow: check scene -> check person -> recognise life-threatening bleeding -> call 911 ->
    /// gloves -> dressing on the wound -> firm continuous pressure -> blood soaks through
    /// (leave it, add one more on top) -> pressure until bleeding stops -> roller bandage ->
    /// check circulation -> keep warm and reassure -> EMS arrives.
    /// </summary>
    public class BleedingScenario : ScenarioScript
    {
        [Header("Scene parts")]
        public TouchZone shoulderTouch;
        public PlacementZone woundZone;
        public PressHoldZone pressureZone;
        public PlacementZone bandageZone;
        public TouchZone fingerTouch;
        public PlacementZone blanketZone;
        public Transform bloodPool;
        public GameObject bandageWrap;
        public float maxPoolScale = 0.75f;

        [Header("Look")]
        public Color dressingSoaked = new Color(0.62f, 0.12f, 0.12f);

        public bool Soaked { get; private set; }
        public bool DecisionMade { get; private set; }
        public bool BleedingControlled { get; private set; }

        float pool = 0.25f;
        readonly List<FirstAidItem> dressings = new List<FirstAidItem>();

        protected override void Start()
        {
            base.Start();
            shoulderTouch.Touched += _ => OnChecked();
            woundZone.CanPlace = item => woundZone.placed.Count == 0 || (Soaked && DecisionMade);
            woundZone.Placed += OnDressingPlaced;
            pressureZone.armed = false;
            pressureZone.Progressed += OnPressure;
            pressureZone.Completed += OnBleedingStopped;
            bandageZone.gameObject.SetActive(false);
            bandageZone.Placed += (item, i) => { item.gameObject.SetActive(false); if (bandageWrap != null) bandageWrap.SetActive(true); fingerTouch.armed = true; };
            if (bandageWrap != null) bandageWrap.SetActive(false);
            fingerTouch.armed = false;
            fingerTouch.Touched += _ => patient.Say("My fingers feel warm. I can wiggle them.");
            blanketZone.Placed += (item, i) => patient.Say("Thanks... that's warmer.");
            if (phone != null)
                phone.dispatcherLines = new[]
                {
                    "911. What is the address of your emergency?",
                    "Okay. An ambulance is on the way. Stay with him and keep the phone on speaker.",
                };
            SetPool();
        }

        protected override void OnBegin()
        {
            patient.Say("Help! Somebody, please... my arm!", 4f);
        }

        void OnChecked()
        {
            patient.Say("I fell off my bike onto some broken glass. It won't stop bleeding!");
            if (controller.IsDone("recognize")) return;
            Later(1.2f, () => Ask("Check the person",
                "He is awake and talking. Bright red blood is flowing steadily from a deep cut on his forearm and pooling on the ground. His face looks a little pale.",
                "What is happening?",
                new List<Choice>
                {
                    new Choice { label = "Life-threatening bleeding - I need to act now", actionId = "recognize", correct = true,
                        explanation = "Blood that is flowing steadily or pooling is life-threatening. Call 911 and control the bleeding immediately." },
                    new Choice { label = "A small cut - a plaster will be enough", actionId = "underestimated",
                        explanation = "Steady, pooling blood is never minor. Treat it as life-threatening." },
                    new Choice { label = "He just needs to rest for a while", actionId = "underestimated",
                        explanation = "Severe bleeding will not stop on its own. He needs help now." },
                },
                _ => ShowTalk(new List<TalkOption>
                {
                    new TalkOption { label = "I'm here to help. An ambulance is coming - try to stay still.", actionId = "reassure",
                        reply = "Okay... okay. Thank you. Please don't leave." },
                    new TalkOption { label = "Can you get up and walk to my car?", actionId = "moved_patient",
                        reply = "I... I feel dizzy. I don't think I can." },
                    new TalkOption { label = "What happened?", actionId = "asked_history", reply = "I hit a pothole and landed on a broken bottle." },
                })));
        }

        void OnDressingPlaced(FirstAidItem item, int index)
        {
            dressings.Add(item);
            if (index == 0)
            {
                pressureZone.armed = true;
                patient.Say("Ow! That hurts...", 2.5f);
            }
            else
            {
                pressureZone.armed = true;
                Guide("Keep pressing", "Press firmly on both dressings with your hand (hold GRIP or TRIGGER) until the bleeding stops.", "Pressure Zone");
            }
        }

        void OnPressure(float p)
        {
            if (Soaked || p < 0.45f) return;
            Soaked = true;
            pressureZone.armed = false;
            Tint(dressings, dressingSoaked);
            Sfx.Play(Sound.Wrong, woundZone.transform.position, 0.3f, 0.7f);
            Guide("Blood is soaking through", "The dressing is soaking with blood. Decide what to do next.");
            Later(1.0f, () => Ask("Blood is soaking through",
                "You have been pressing firmly, but blood is soaking through the dressing.",
                "What should you do?",
                new List<Choice>
                {
                    new Choice { label = "Leave it in place, add another dressing on top and keep pressing", actionId = "soak_decision", correct = true,
                        explanation = "Never remove the first dressing - it would pull away the clot. Add a dressing on top and keep firm pressure." },
                    new Choice { label = "Remove the soaked dressing and replace it with a clean one", actionId = "removed_dressing",
                        explanation = "Removing the first dressing tears away the clot and restarts the bleeding." },
                    new Choice { label = "Lift the dressing to look at the wound", actionId = "peeked",
                        explanation = "Lifting to check releases the pressure. Keep pressing continuously." },
                },
                _ =>
                {
                    DecisionMade = true;
                    Guide("Add a second dressing", "Pick up the second dressing and place it on top of the first one. Do not remove the first dressing.", "Dressing B");
                }));
        }

        void OnBleedingStopped()
        {
            BleedingControlled = true;
            patient.Say("I think... I think it's stopped bleeding.");
            bandageZone.gameObject.SetActive(true);
            patient.pallor = Mathf.Min(patient.pallor, 0.35f);
        }

        void Update()
        {
            if (controller.Phase != ScenarioPhase.Active || BleedingControlled) return;
            float rate = pressureZone.Pressing ? 0.004f : 0.025f;
            pool = Mathf.Min(1f, pool + rate * Time.deltaTime);
            SetPool();
            patient.pallor = Mathf.Clamp01(0.15f + pool * 0.6f);
        }

        void SetPool()
        {
            if (bloodPool != null)
            {
                float s = Mathf.Lerp(0.12f, maxPoolScale, pool);
                bloodPool.localScale = new Vector3(s, bloodPool.localScale.y, s * 0.75f);
            }
        }

        static void Tint(List<FirstAidItem> items, Color c)
        {
            var block = new MaterialPropertyBlock();
            foreach (var i in items)
                foreach (var r in i.GetComponentsInChildren<Renderer>())
                {
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", c);
                    r.SetPropertyBlock(block);
                }
        }
    }
}
