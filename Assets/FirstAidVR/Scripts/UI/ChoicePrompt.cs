using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstAidVR
{
    /// <summary>One answer on a <see cref="ChoicePrompt"/>.</summary>
    [Serializable]
    public class Choice
    {
        public string label;
        [Tooltip("Action (step or mistake id) reported when picked.")]
        public string actionId;
        public bool correct;
        [TextArea] public string explanation;
    }

    /// <summary>
    /// A decision card in front of the user: what they observe, a question and a few
    /// answers ("This person may be in shock" / "They are just tired" ...).
    /// The clock is paused while it is open. In Training Mode a wrong answer explains why
    /// and lets the user try again; in Certification Mode the first answer counts.
    /// </summary>
    public class ChoicePrompt : MonoBehaviour
    {
        public static ChoicePrompt Open { get; private set; }

        public string Question { get; private set; }
        public IReadOnlyList<Choice> Choices => choices;

        List<Choice> choices;
        Action<Choice> onDone;
        TextMeshProUGUI feedback;
        readonly List<Button> buttons = new List<Button>();
        bool training;

        public static ChoicePrompt Show(string heading, string observation, string question, List<Choice> choices, Action<Choice> onDone)
        {
            if (Open != null) Destroy(Open.gameObject);
            var go = new GameObject("Choice Prompt");
            var p = go.AddComponent<ChoicePrompt>();
            p.Build(heading, observation, question, choices, onDone);
            return p;
        }

        void Build(string heading, string observation, string question, List<Choice> list, Action<Choice> done)
        {
            Open = this;
            Question = question;
            choices = new List<Choice>(list);
            onDone = done;
            var ctrl = ScenarioController.Active;
            training = ctrl == null || ctrl.IsTraining;
            if (ctrl != null) ctrl.Held = true;

            var follow = gameObject.AddComponent<HeadFollower>();
            follow.distance = 0.95f;
            follow.height = -0.08f;
            follow.deadZone = 35f;

            int n = choices.Count;
            float h = 300 + n * 92 + 70;
            var canvas = UIFactory.WorldCanvas("Choice Canvas", transform, Vector3.zero, Quaternion.identity, new Vector2(760, h));
            var rt = (RectTransform)canvas.transform;
            UIFactory.Card(rt);
            UIFactory.Panel(rt, 0, 0, 760, 10, Theme.Amber, 0.3f, "Top");
            UIFactory.Label(rt, heading.ToUpperInvariant(), 40, 34, 680, 30, 22, Theme.Amber, TextAlignmentOptions.Left, true);
            var obs = UIFactory.Label(rt, observation, 40, 70, 680, 130, 26, Theme.Muted);
            obs.overflowMode = TextOverflowModes.Overflow;
            UIFactory.Label(rt, question, 40, 200, 680, 80, 32, Theme.Text, TextAlignmentOptions.Left, true);

            // Shuffle so the right answer is not always first (fixed seed per question for repeatability).
            var rng = new System.Random(question.GetHashCode());
            for (int i = choices.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (choices[i], choices[j]) = (choices[j], choices[i]); }

            for (int i = 0; i < n; i++)
            {
                var c = choices[i];
                var b = UIFactory.Button(rt, c.label, 40, 290 + i * 92, 680, 78, () => Pick(c), Theme.PanelLight, 26, "Choice " + i);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                buttons.Add(b);
            }
            feedback = UIFactory.Label(rt, training ? "" : "Certification Mode: your first answer counts.", 40, h - 64, 680, 50, 22, Theme.Muted);
            Sfx.Play(Sound.Whoosh, null, 0.5f);
        }

        /// <summary>Pick an answer (also used by automated tests).</summary>
        public void Pick(Choice c)
        {
            ScenarioController.Active?.Report(c.actionId);
            if (!c.correct && training)
            {
                Sfx.Play(Sound.Wrong);
                feedback.color = Theme.Red;
                feedback.text = "Not quite. " + c.explanation;
                for (int i = 0; i < choices.Count; i++) if (choices[i] == c) buttons[i].interactable = false;
                return;
            }
            if (training)
            {
                Sfx.Play(Sound.Correct);
                FeedbackToast.Show(ToastKind.Good, "Correct", c.explanation);
            }
            Close();
            onDone?.Invoke(c);
        }

        public void PickCorrect() { foreach (var c in choices) if (c.correct) { Pick(c); return; } }
        public void PickWrong() { foreach (var c in choices) if (!c.correct) { Pick(c); return; } }

        void Close()
        {
            var ctrl = ScenarioController.Active;
            if (ctrl != null) ctrl.Held = false;
            if (Open == this) Open = null;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Open == this)
            {
                Open = null;
                if (ScenarioController.Active != null) ScenarioController.Active.Held = false;
            }
        }
    }
}
