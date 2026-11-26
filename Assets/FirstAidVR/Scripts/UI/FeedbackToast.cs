using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstAidVR
{
    public enum ToastKind { Good, Bad, Hint, Info }

    /// <summary>
    /// Short feedback messages ("Correct - firm pressure!", "Don't remove the dressing")
    /// that appear just below the user's line of sight and fade out.
    /// </summary>
    public class FeedbackToast : MonoBehaviour
    {
        static FeedbackToast instance;
        Canvas canvas;
        CanvasGroup group;
        Image stripe, iconBg;
        TextMeshProUGUI title, body, icon;
        readonly Queue<(ToastKind kind, string title, string body)> queue = new Queue<(ToastKind, string, string)>();
        float hideAt;
        bool showing;

        public static string LastTitle { get; private set; }
        public static ToastKind LastKind { get; private set; }

        public static FeedbackToast Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("Feedback Toast").AddComponent<FeedbackToast>();
                return instance;
            }
        }

        void Awake()
        {
            instance = this;
            var follow = gameObject.AddComponent<HeadFollower>();
            follow.distance = 1.0f;
            follow.height = -0.32f;
            follow.deadZone = 18f;
            canvas = UIFactory.WorldCanvas("Toast Canvas", transform, Vector3.zero, Quaternion.identity, new Vector2(680, 180));
            group = canvas.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var rt = (RectTransform)canvas.transform;
            UIFactory.Card(rt, Theme.Panel.WithAlpha(0.96f));
            stripe = UIFactory.Panel(rt, 0, 0, 14, 180, Theme.Green, 0.5f, "Stripe");
            iconBg = UIFactory.Panel(rt, 34, 50, 80, 80, Theme.Green, 2f, "Icon Bg");
            icon = UIFactory.Label(iconBg.transform, "", 0, 0, 80, 80, 48, Color.white, TextAlignmentOptions.Center, true);
            title = UIFactory.Label(rt, "", 134, 18, 526, 46, 31, Theme.Text, TextAlignmentOptions.Left, true);
            body = UIFactory.Label(rt, "", 134, 64, 526, 104, 24, Theme.Muted);
            body.enableAutoSizing = true;
            body.fontSizeMin = 17;
            body.fontSizeMax = 24;
            group.alpha = 0f;
        }

        public static void Show(ToastKind kind, string title, string body = "")
        {
            LastTitle = title;
            LastKind = kind;
            Instance.queue.Enqueue((kind, title, body));
        }

        public static void Clear()
        {
            if (instance == null) return;
            instance.queue.Clear();
            instance.hideAt = 0f;
        }

        void Update()
        {
            bool promptOpen = ChoicePrompt.Open != null;
            if (showing && (Time.time >= hideAt || promptOpen)) showing = false;
            if (!showing && queue.Count > 0 && !promptOpen) Present(queue.Dequeue()); // never cover a question card
            float target = showing ? 1f : 0f;
            group.alpha = promptOpen ? 0f : Mathf.MoveTowards(group.alpha, target, Time.deltaTime * 6f);
            canvas.transform.localScale = Vector3.one * UIFactory.MetersPerPixel * Mathf.Lerp(0.92f, 1f, group.alpha);
        }

        void Present((ToastKind kind, string title, string body) t)
        {
            var c = t.kind == ToastKind.Good ? Theme.Green : t.kind == ToastKind.Bad ? Theme.Red : t.kind == ToastKind.Hint ? Theme.Amber : Theme.Blue;
            stripe.color = c;
            iconBg.color = c;
            icon.text = t.kind == ToastKind.Good ? "✓" : t.kind == ToastKind.Bad ? "!" : t.kind == ToastKind.Hint ? "?" : "i";
            title.text = t.title;
            body.text = t.body;
            showing = true;
            hideAt = Time.time + Mathf.Clamp(2.2f + (t.title.Length + t.body.Length) * 0.035f, 2.5f, 6.5f);
            if (queue.Count > 0) hideAt = Mathf.Min(hideAt, Time.time + 2.5f);
            if (group.alpha < 0.1f) GetComponent<HeadFollower>().Snap();
        }
    }
}
