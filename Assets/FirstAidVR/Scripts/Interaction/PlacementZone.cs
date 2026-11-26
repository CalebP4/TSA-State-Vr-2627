using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// A trigger area on the patient (or a table) where an item can be put, e.g. the
    /// wound for a dressing or the torso for a blanket. When the right item is let go
    /// inside it, or held in it for a moment, it snaps into place and an action is
    /// reported to the ScenarioController. A wrong item can report a mistake.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlacementZone : MonoBehaviour
    {
        [Tooltip("Item ids this zone accepts.")]
        public List<string> acceptedItems = new List<string>();
        [Tooltip("Action reported for the 1st, 2nd, ... item placed here.")]
        public List<string> actionIds = new List<string>();
        [Tooltip("Mistake/action reported when an item NOT in the list is let go here (optional).")]
        public string wrongItemAction;
        [Tooltip("Items listed here are judged wrong for this zone (others are ignored).")]
        public List<string> wrongItems = new List<string>();
        [Tooltip("Where placed items sit. Defaults to this transform.")]
        public Transform anchor;
        public Vector3 stackOffset = new Vector3(0f, 0.012f, 0f);
        public int capacity = 1;
        [Tooltip("Snap after the item has been held inside the zone this long (0 = only on release).")]
        public float holdToPlace = 0.6f;
        [Tooltip("Placed items can be picked up again.")]
        public bool keepGrabbable;
        public int priority;
        [Tooltip("Optional ghost shown while a matching item is held nearby.")]
        public GameObject ghost;

        public readonly List<FirstAidItem> placed = new List<FirstAidItem>();
        public event Action<FirstAidItem, int> Placed;   // item, index (0 = first)
        public event Action<FirstAidItem> Removed;
        /// <summary>Lets a scenario script veto placement (e.g. not before the question is answered).</summary>
        public Func<FirstAidItem, bool> CanPlace;

        readonly Dictionary<FirstAidItem, float> heldSince = new Dictionary<FirstAidItem, float>();
        readonly HashSet<FirstAidItem> reportedWrong = new HashSet<FirstAidItem>();

        public bool IsFull => placed.Count >= capacity;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (anchor == null) anchor = transform;
            if (ghost != null) ghost.SetActive(false);
        }

        public bool Accepts(FirstAidItem item) => item != null && acceptedItems.Contains(item.itemId);

        public bool Wants(FirstAidItem item) =>
            Accepts(item) && !IsFull && !placed.Contains(item) && (CanPlace == null || CanPlace(item));

        void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<FirstAidItem>();
            if (item == null || item.zones.Contains(this)) return;
            item.zones.Add(this);
            heldSince[item] = Time.time;
        }

        void OnTriggerExit(Collider other)
        {
            var item = other.GetComponentInParent<FirstAidItem>();
            if (item == null) return;
            item.zones.Remove(this);
            heldSince.Remove(item);
            reportedWrong.Remove(item);
        }

        void Update()
        {
            bool showGhost = false;
            foreach (var kv in new List<KeyValuePair<FirstAidItem, float>>(heldSince))
            {
                var item = kv.Key;
                if (item == null) { heldSince.Remove(item); continue; }
                if (!item.IsHeld) continue;
                if (Wants(item))
                {
                    showGhost = true;
                    if (holdToPlace > 0f && Time.time - kv.Value >= holdToPlace) Place(item);
                }
                else if (holdToPlace > 0f && Time.time - kv.Value >= holdToPlace + 0.4f) ReportWrong(item);
            }
            // The green outline is a Training aid only - no hints in Certification.
            if (ScenarioController.Active != null && !ScenarioController.Active.IsTraining) showGhost = false;
            if (ghost != null && ghost.activeSelf != showGhost) ghost.SetActive(showGhost);
        }

        /// <summary>Snap the item into the zone and report the matching action.</summary>
        public void Place(FirstAidItem item)
        {
            if (!Wants(item)) return;
            int index = placed.Count;
            placed.Add(item);
            item.PlacedIn = this;
            item.zones.Remove(this);
            heldSince.Remove(item);
            item.LockAt(anchor, stackOffset * index, keepGrabbable);
            Sfx.Play(Sound.Place, anchor.position);
            HandProbe.PulseAll(0.25f, 0.05f);
            if (ghost != null) ghost.SetActive(false);
            if (index < actionIds.Count && ScenarioController.Active != null)
                ScenarioController.Active.Report(actionIds[index]);
            Placed?.Invoke(item, index);
        }

        /// <summary>Called by an item that was let go here but was not taken.</summary>
        public void OnReleasedInside(FirstAidItem item) => ReportWrong(item);

        void ReportWrong(FirstAidItem item)
        {
            if (string.IsNullOrEmpty(wrongItemAction) || reportedWrong.Contains(item)) return;
            if (wrongItems.Count > 0 && !wrongItems.Contains(item.itemId)) return;
            if (Accepts(item) && !IsFull) return;
            reportedWrong.Add(item);
            ScenarioController.Active?.Report(wrongItemAction);
        }

        public void OnItemRemoved(FirstAidItem item)
        {
            if (!placed.Remove(item)) return;
            item.PlacedIn = null;
            Removed?.Invoke(item);
        }
    }
}
