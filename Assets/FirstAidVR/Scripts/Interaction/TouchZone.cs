using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{

    /// <summary>
    /// Touch a spot (optionally holding grip/trigger, optionally for a while), e.g. tap the
    /// shoulder to check responsiveness, rest a hand on the chest to check breathing.
    /// </summary>
    public class TouchZone : HandZone
    {
        public string actionId;
        public float dwellSeconds;
        public bool requirePress;
        public bool repeatable;
        public float cooldown = 1.5f;
        public event Action<TouchZone> Touched;

        float since = -1f, lastFired = -99f;
        bool fired;

        void Update()
        {
            var h = FirstHand();
            bool active = Ready && h != null && (!requirePress || h.Pressing);
            if (!active) { since = -1f; return; }
            if (since < 0f) since = Time.time;
            if (fired && !repeatable) return;
            if (Time.time - since < dwellSeconds || Time.time - lastFired < cooldown) return;
            fired = true;
            lastFired = Time.time;
            since = Time.time + 999f; // wait until the hand leaves before firing again
            h.Pulse(0.3f, 0.08f);
            Report(actionId);
            Touched?.Invoke(this);
        }

        protected override void OnHandExit(HandProbe h) { if (!HandInside) since = -1f; }

        public void ResetTouch() { fired = false; since = -1f; }
        public float DwellProgress => since < 0f || dwellSeconds <= 0f ? 0f : Mathf.Clamp01((Time.time - since) / dwellSeconds);
    }
}

