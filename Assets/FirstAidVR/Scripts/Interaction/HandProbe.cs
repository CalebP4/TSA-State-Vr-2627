using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace FirstAidVR
{
    /// <summary>
    /// Put on each controller. Gives the hand a small trigger sphere so zones on the
    /// patient (press here, strike here, touch here) can feel it, tracks hand speed,
    /// reads grip/trigger and plays haptics.
    /// </summary>
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public class HandProbe : MonoBehaviour
    {
        public static readonly List<HandProbe> All = new List<HandProbe>();

        public bool isLeft;
        public InputActionReference grip;
        public InputActionReference trigger;
        [Tooltip("Renderers tinted when gloves are put on.")]
        public Renderer[] gloveRenderers;

        /// <summary>Set by tests (and the desktop simulator fallback) to fake a press.</summary>
        public bool SimulatedPress { get; set; }
        public Vector3 Velocity { get; private set; }
        /// <summary>Speed since the last frame, valid during physics callbacks (used for strikes).</summary>
        public Vector3 StrikeVelocity
        {
            get
            {
                var best = Time.deltaTime > 0f ? (transform.position - lastPos) / Time.deltaTime : Vector3.zero;
                for (int i = 0; i < recent.Length; i++)
                    if (Time.time - recentAt[i] <= 0.12f && recent[i].sqrMagnitude > best.sqrMagnitude) best = recent[i];
                return best;
            }
        }
        /// <summary>Fastest recent speed in one direction (e.g. into the baby's back), over ~0.12 s.</summary>
        public float PeakSpeedAlong(Vector3 dir)
        {
            dir.Normalize();
            float best = Time.deltaTime > 0f ? Vector3.Dot((transform.position - lastPos) / Time.deltaTime, dir) : 0f;
            for (int i = 0; i < recent.Length; i++)
                if (Time.time - recentAt[i] <= 0.12f) best = Mathf.Max(best, Vector3.Dot(recent[i], dir));
            return best;
        }

        readonly Vector3[] recent = new Vector3[8];
        readonly float[] recentAt = new float[8];
        int recentIndex;
        public bool Gloved { get; private set; }
        /// <summary>Whatever the hand is holding right now (set by FirstAidItem).</summary>
        public FirstAidItem Holding { get; set; }

        HapticImpulsePlayer haptics;
        Vector3 lastPos;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public bool Pressing
        {
            get
            {
                if (SimulatedPress) return true;
                float g = Read(grip), t = Read(trigger);
                return g > 0.5f || t > 0.5f;
            }
        }

        static float Read(InputActionReference r)
        {
            if (r == null || r.action == null) return 0f;
            try { return r.action.ReadValue<float>(); } catch { return 0f; }
        }

        void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            if (col.radius > 0.2f) col.radius = 0.045f;
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            haptics = GetComponentInParent<HapticImpulsePlayer>();
            lastPos = transform.position;
        }

        void OnEnable() { All.Add(this); grip?.action?.Enable(); trigger?.action?.Enable(); }
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var p = transform.position;
            if (Time.deltaTime > 0f)
            {
                var v = (p - lastPos) / Time.deltaTime;
                Velocity = Vector3.Lerp(Velocity, v, 0.6f);
                recent[recentIndex] = v;
                recentAt[recentIndex] = Time.time;
                recentIndex = (recentIndex + 1) % recent.Length;
            }
            lastPos = p;
        }

        /// <summary>Resets the speed estimate (used after teleporting the hand in tests).</summary>
        public void ResetVelocity()
        {
            lastPos = transform.position;
            Velocity = Vector3.zero;
            for (int i = 0; i < recent.Length; i++) recent[i] = Vector3.zero;
        }

        public void Pulse(float amplitude, float duration)
        {
            if (haptics != null) haptics.SendHapticImpulse(amplitude, duration);
        }

        public void SetGloved(bool on)
        {
            Gloved = on;
            if (gloveRenderers == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var r in gloveRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                if (on) block.SetColor(BaseColor, Theme.Hex("6C7BEA"));
                else block.Clear();
                r.SetPropertyBlock(block);
            }
        }

        public static void PulseAll(float amplitude, float duration)
        {
            foreach (var h in All) h.Pulse(amplitude, duration);
        }

        public static HandProbe Get(bool left)
        {
            foreach (var h in All) if (h.isLeft == left) return h;
            return null;
        }
    }
}
