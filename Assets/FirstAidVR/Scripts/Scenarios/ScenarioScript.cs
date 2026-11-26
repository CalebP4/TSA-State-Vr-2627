using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Base class for the scenario-specific scripts (Bleeding, Infant Choking, Shock).
    /// It wires the shared pieces together: the controller, the UI, the patient, the
    /// phone and the ambulance. Subclasses add the scenario's own story and reactions.
    /// </summary>
    public abstract class ScenarioScript : MonoBehaviour
    {
        public ScenarioController controller;
        public ScenarioUI ui;
        public PatientVisuals patient;
        public PhoneItem phone;
        public EmsArrival ems;
        public Transform talkAnchor;

        protected TalkMenu talk;
        protected SpeechBubble phoneBubble;
        protected bool IsTraining => controller.IsTraining;

        protected virtual void Start()
        {
            if (controller == null) controller = ScenarioController.Active;
            if (phone != null)
            {
                phoneBubble = SpeechBubble.Create(phone.transform, new Vector3(0, 0.22f, 0), "Dispatcher Speech");
                phone.Speak = line => phoneBubble.Say(line, 3.2f);
            }
            controller.StepCompleted += s => { if (s.def.id == "call_911" && ems != null) ems.Dispatch(); OnStep(s.def.id); };
            controller.AllRequiredDone += OnAllDone;
            controller.Started += OnBegin;
        }

        protected virtual void OnBegin() { }
        protected virtual void OnStep(string id) { }

        protected virtual void OnAllDone()
        {
            if (ems != null) ems.Arrive();
        }

        /// <summary>Show a decision card. Training lets the user retry; Certification takes the first answer.</summary>
        protected void Ask(string heading, string observation, string question, List<Choice> choices, System.Action<Choice> done = null)
        {
            if (controller.Phase != ScenarioPhase.Active) return;
            ChoicePrompt.Show(heading, observation, question, choices, c => done?.Invoke(c));
        }

        protected void ShowTalk(List<TalkOption> options)
        {
            if (talk != null || talkAnchor == null) return;
            talk = TalkMenu.Create(talkAnchor, patient, options);
        }

        public TalkMenu Talk => talk;

        protected void Later(float seconds, System.Action a) => StartCoroutine(LaterRoutine(seconds, a));

        IEnumerator LaterRoutine(float s, System.Action a)
        {
            yield return new WaitForSeconds(s);
            a();
        }

        protected void Guide(string title, string text, string highlight = null)
        {
            if (ui != null && IsTraining) ui.OverrideInstruction(title, text, highlight);
        }
    }
}
