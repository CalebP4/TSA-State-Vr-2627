using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{

    /// <summary>Base for trigger areas that react to the user's hands (HandProbe).</summary>
    [RequireComponent(typeof(Collider))]
    public abstract class HandZone : MonoBehaviour
    {
        [Tooltip("When false the zone ignores hands (scenario scripts switch zones on and off).")]
        public bool armed = true;
        /// <summary>Optional extra condition, e.g. "the infant is face-down".</summary>
        public Func<bool> Condition;
        /// <summary>A hand to ignore (e.g. the one holding the infant).</summary>
        public HandProbe Ignore { get; set; }

        protected readonly List<HandProbe> inside = new List<HandProbe>();
        public bool HandInside => FirstHand() != null;

        protected virtual void Awake() => GetComponent<Collider>().isTrigger = true;

        protected virtual void OnTriggerEnter(Collider other)
        {
            var h = other.GetComponent<HandProbe>();
            if (h != null && !inside.Contains(h)) { inside.Add(h); OnHandEnter(h); }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            var h = other.GetComponent<HandProbe>();
            if (h != null && inside.Remove(h)) OnHandExit(h);
        }

        protected virtual void OnDisable() => inside.Clear();

        protected HandProbe FirstHand()
        {
            inside.RemoveAll(h => h == null || !h.isActiveAndEnabled);
            foreach (var h in inside) if (h != Ignore) return h;
            return null;
        }

        protected bool Ready => armed && (Condition == null || Condition());
        protected virtual void OnHandEnter(HandProbe h) { }
        protected virtual void OnHandExit(HandProbe h) { }
        protected static void Report(string id) { if (!string.IsNullOrEmpty(id)) ScenarioController.Active?.Report(id); }
    }
}

