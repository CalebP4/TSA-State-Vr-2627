using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstAidVR
{
    /// <summary>One thing the user can say to the patient (or a bystander).</summary>
    public class TalkOption
    {
        public string label;
        public string actionId;
        public string reply;
        public bool once = true;
    }

    /// <summary>
    /// A small "Say..." panel next to the patient. Each option reports an action
    /// (e.g. "reassure" or the mistake "moved_patient") and the patient answers.
    /// </summary>
    public class TalkMenu : MonoBehaviour
    {
        public PatientVisuals patient;
        readonly List<TalkOption> options = new List<TalkOption>();
        readonly List<Button> buttons = new List<Button>();
        RectTransform root;
        Canvas canvas;

        public IReadOnlyList<TalkOption> Options => options;

        public static TalkMenu Create(Transform anchor, PatientVisuals patient, List<TalkOption> opts)
        {
            var go = new GameObject("Talk Menu");
            go.transform.SetParent(anchor, false);
            var m = go.AddComponent<TalkMenu>();
            m.patient = patient;
            m.options.AddRange(opts);
            m.Build();
            return m;
        }

        void Build()
        {
            float h = 90 + options.Count * 84;
            canvas = UIFactory.WorldCanvas("Talk Canvas", transform, Vector3.zero, Quaternion.identity, new Vector2(560, h));
            root = (RectTransform)canvas.transform;
            UIFactory.Card(root, Theme.Panel.WithAlpha(0.94f));
            UIFactory.Label(root, "SAY...", 28, 22, 500, 30, 20, Theme.Teal, TextAlignmentOptions.Left, true);
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                var b = UIFactory.Button(root, "“" + o.label + "”", 24, 64 + i * 84, 512, 72, () => Say(o), Theme.PanelLight, 22, "Talk " + i);
                b.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
                buttons.Add(b);
            }
        }

        /// <summary>Say an option (button press; also used by tests).</summary>
        public void Say(TalkOption o)
        {
            var c = ScenarioController.Active;
            if (c != null && c.Phase != ScenarioPhase.Active) return;
            c?.Report(o.actionId);
            if (patient != null && !string.IsNullOrEmpty(o.reply)) patient.Say(o.reply);
            if (o.once)
            {
                int i = options.IndexOf(o);
                if (i >= 0) buttons[i].interactable = false;
            }
        }

        public void Say(string actionId)
        {
            foreach (var o in options) if (o.actionId == actionId) { Say(o); return; }
        }

        public void SetVisible(bool v) => canvas.gameObject.SetActive(v);
    }
}
