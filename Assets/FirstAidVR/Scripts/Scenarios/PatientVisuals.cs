using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Simple "acting" for the low-poly patients: breathing, blinking, skin colour
    /// (normal / pale) and a speech bubble above the head.
    /// </summary>
    public class PatientVisuals : MonoBehaviour
    {
        public Transform chest;
        public Transform head;
        public Renderer[] skin;
        public Transform[] eyes;
        public Color normalSkin = new Color(0.85f, 0.66f, 0.52f);
        public Color paleSkin = new Color(0.86f, 0.84f, 0.8f);
        [Range(0, 1)] public float pallor;
        [Tooltip("Breaths per minute (adult ~12-20; fast in shock).")]
        public float breathRate = 16f;
        public float breathDepth = 0.04f;
        public bool breathing = true;

        public SpeechBubble Bubble { get; private set; }

        Vector3 chestScale;
        float blinkAt;
        MaterialPropertyBlock block;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        float appliedPallor = -1f;

        void Awake()
        {
            if (chest != null) chestScale = chest.localScale;
            block = new MaterialPropertyBlock();
            Bubble = SpeechBubble.Create(head != null ? head : transform, new Vector3(0, 0.38f, 0), name + " Speech");
        }

        public void Say(string line, float seconds = 0f) => Bubble.Say(line, seconds);

        void Update()
        {
            if (chest != null)
            {
                float b = breathing ? (Mathf.Sin(Time.time * breathRate / 60f * Mathf.PI * 2f) * 0.5f + 0.5f) : 0f;
                chest.localScale = chestScale + new Vector3(0, chestScale.y * breathDepth * b, chestScale.z * breathDepth * 0.5f * b);
            }
            if (eyes != null && eyes.Length > 0)
            {
                bool closed = Time.time > blinkAt && Time.time < blinkAt + 0.12f;
                if (Time.time > blinkAt + 0.12f) blinkAt = Time.time + Random.Range(2.5f, 5f);
                foreach (var e in eyes) if (e != null) e.localScale = new Vector3(e.localScale.x, closed ? 0.002f : e.localScale.x, e.localScale.z);
            }
            if (!Mathf.Approximately(appliedPallor, pallor)) ApplySkin();
        }

        void ApplySkin()
        {
            appliedPallor = pallor;
            var c = Color.Lerp(normalSkin, paleSkin, pallor);
            if (skin == null) return;
            foreach (var r in skin)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
