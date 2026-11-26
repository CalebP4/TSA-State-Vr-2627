using TMPro;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>A caption that floats above a person (patient, parent, dispatcher) and faces the user.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        public Transform anchor;
        public Vector3 offset = new Vector3(0, 0.35f, 0);
        TextMeshProUGUI text;
        CanvasGroup group;
        float hideAt;

        public string Current => text != null ? text.text : "";

        public static SpeechBubble Create(Transform anchor, Vector3 offset, string name = "Speech Bubble")
        {
            var go = new GameObject(name);
            var b = go.AddComponent<SpeechBubble>();
            b.anchor = anchor;
            b.offset = offset;
            return b;
        }

        void Awake()
        {
            var canvas = UIFactory.WorldCanvas("Bubble Canvas", transform, Vector3.zero, Quaternion.identity, new Vector2(560, 130));
            group = canvas.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var rt = (RectTransform)canvas.transform;
            UIFactory.Card(rt, Color.white.WithAlpha(0.97f));
            text = UIFactory.Label(rt, "", 24, 14, 512, 102, 28, Theme.Background, TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 28;
            group.alpha = 0f;
        }

        public void Say(string line, float seconds = 0f)
        {
            text.text = line;
            hideAt = Time.time + (seconds > 0 ? seconds : Mathf.Clamp(1.5f + line.Length * 0.06f, 3f, 8f));
        }

        void LateUpdate()
        {
            if (anchor != null) transform.position = anchor.position + offset;
            var cam = Camera.main;
            if (cam != null)
            {
                var d = transform.position - cam.transform.position;
                if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d, Vector3.up);
            }
            group.alpha = Mathf.MoveTowards(group.alpha, Time.time < hideAt ? 1f : 0f, Time.deltaTime * 5f);
        }
    }
}
