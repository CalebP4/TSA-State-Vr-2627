using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{

    /// <summary>
    /// Keep a hand pressed here (grip or trigger held) to fill a progress meter,
    /// e.g. firm direct pressure on a dressing.
    /// </summary>
    public class PressHoldZone : HandZone
    {
        public string actionId;
        public float holdSeconds = 8f;
        [Tooltip("Mistake reported (once) if the hand lets go part-way.")]
        public string interruptedAction;
        [Range(0, 1)] public float progress;

        public bool Pressing { get; private set; }
        public bool Complete => progress >= 1f;
        public event Action<float> Progressed;
        public event Action Completed;
        public event Action Interrupted;

        float pulseAt;
        bool reportedInterrupt;

        void Update()
        {
            if (Complete) { Pressing = false; return; }
            var h = FirstHand();
            bool now = Ready && h != null && h.Pressing;
            if (now)
            {
                progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(0.1f, holdSeconds));
                if (Time.time >= pulseAt) { h.Pulse(0.15f, 0.05f); pulseAt = Time.time + 0.3f; }
                Progressed?.Invoke(progress);
                if (Complete)
                {
                    Sfx.Play(Sound.Press, transform.position);
                    Report(actionId);
                    Completed?.Invoke();
                }
            }
            else if (Pressing && armed && progress > 0.1f && !Complete)
            {
                Interrupted?.Invoke();
                if (!reportedInterrupt && !string.IsNullOrEmpty(interruptedAction))
                {
                    reportedInterrupt = true;
                    Report(interruptedAction);
                }
            }
            Pressing = now;
        }
    }
}

