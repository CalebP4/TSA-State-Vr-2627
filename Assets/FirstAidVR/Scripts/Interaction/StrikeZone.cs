using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{

    /// <summary>
    /// Counts firm strikes, e.g. back blows between the infant's shoulder blades.
    /// A strike is a hand entering the zone fast enough, moving against the zone's up axis.
    /// </summary>
    public class StrikeZone : HandZone
    {
        public string actionId;
        public float minSpeed = 0.9f;
        public float weakSpeed = 0.35f;
        public float cooldown = 0.3f;
        public event Action Struck;
        public event Action WeakStrike;
        public event Action NotReady;

        float last = -99f;

        protected override void OnHandEnter(HandProbe h)
        {
            if (h == Ignore || Time.time - last < cooldown) return;
            // Speed of the hand moving INTO the surface (against the zone's up axis).
            float into = h.PeakSpeedAlong(-transform.up);
            if (into < weakSpeed) return;
            if (!Ready) { NotReady?.Invoke(); return; }
            last = Time.time;
            if (into < minSpeed) { WeakStrike?.Invoke(); h.Pulse(0.1f, 0.05f); return; }
            h.Pulse(0.7f, 0.08f);
            Sfx.Play(Sound.Thump, transform.position, 0.8f);
            Report(actionId);
            Struck?.Invoke();
        }

        /// <summary>For tests: register a strike as if a hand hit the zone.</summary>
        public void SimulateStrike(HandProbe h) => OnHandEnter(h);
    }
}

