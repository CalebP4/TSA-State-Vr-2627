using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FirstAidVR
{
    /// <summary>
    /// Any piece of equipment the user can pick up. Sets the XR grab up so the item
    /// snaps into the hand at its grip point (no floating offset, no throwing, no jitter),
    /// remembers where it started and puts itself back if it is dropped out of reach.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class FirstAidItem : MonoBehaviour
    {
        public string itemId;
        public string displayName;
        [Tooltip("Where the hand holds the item. Its forward axis points away from the palm.")]
        public Transform grip;
        [Tooltip("Return to the starting spot when dropped on the floor or out of reach.")]
        public bool returnHome = true;
        public float homeRadius = 4f;

        public XRGrabInteractable Grab { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool IsHeld => Grab != null && Grab.isSelected;
        public PlacementZone PlacedIn { get; internal set; }
        public HandProbe HeldBy { get; private set; }

        public event Action<FirstAidItem> Grabbed;
        public event Action<FirstAidItem> Released;
        /// <summary>Trigger pulled while holding the item.</summary>
        public event Action<FirstAidItem> Used;

        /// <summary>Zones the item is currently inside (kept by PlacementZone).</summary>
        internal readonly List<PlacementZone> zones = new List<PlacementZone>();

        Vector3 homePos;
        Quaternion homeRot;
        Transform homeParent;
        float droppedAt = -1f;

        void Awake()
        {
            Grab = GetComponent<XRGrabInteractable>();
            Body = GetComponent<Rigidbody>();
            Configure(Grab, Body, grip);
            homePos = transform.position;
            homeRot = transform.rotation;
            homeParent = transform.parent;
            Grab.selectEntered.AddListener(OnGrab);
            Grab.selectExited.AddListener(OnRelease);
            Grab.activated.AddListener(_ => Used?.Invoke(this));
        }

        /// <summary>The grab settings used for every item (also used by the scene builder).</summary>
        public static void Configure(XRGrabInteractable g, Rigidbody rb, Transform grip)
        {
            g.attachTransform = grip;
            g.useDynamicAttach = false;              // always hold by the grip point
            g.movementType = XRBaseInteractable.MovementType.Instantaneous; // follows the hand exactly, no lag/jitter
            g.trackPosition = true;
            g.trackRotation = true;
            g.smoothPosition = false;
            g.smoothRotation = false;
            g.throwOnDetach = false;                 // items drop, they never fly away
            g.farAttachMode = InteractableFarAttachMode.Near; // ray grabs bring the item into the hand
            g.attachEaseInTime = 0.08f;
            g.retainTransformParent = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.maxAngularVelocity = 7f;
        }

        void OnGrab(SelectEnterEventArgs args)
        {
            droppedAt = -1f;
            HeldBy = (args.interactorObject as Component)?.GetComponentInParent<HandProbe>();
            if (HeldBy == null) HeldBy = FindProbeNear(args.interactorObject.transform.position);
            if (HeldBy != null) { HeldBy.Holding = this; HeldBy.Pulse(0.3f, 0.06f); }
            Sfx.Play(Sound.Grab, transform.position, 0.6f);
            if (PlacedIn != null) PlacedIn.OnItemRemoved(this);
            Grabbed?.Invoke(this);
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (!isActiveAndEnabled || !gameObject.scene.isLoaded) { HeldBy = null; return; } // scene unloading / object hidden
            releasing = true;
            try { HandleRelease(args.isCanceled); }
            finally { releasing = false; }
        }

        void HandleRelease(bool canceled)
        {
            if (HeldBy != null && HeldBy.Holding == this) HeldBy.Holding = null;
            HeldBy = null;
            droppedAt = Time.time;
            Released?.Invoke(this);
            if (canceled) return;          // e.g. tracking lost: just drop it
            if (PlacedIn != null) return;  // it was just snapped into a zone, nothing to judge

            // Released inside a zone? Let the best zone take it.
            PlacementZone best = null;
            foreach (var z in zones)
                if (z != null && z.isActiveAndEnabled && z.Wants(this) && (best == null || z.priority > best.priority)) best = z;
            if (best != null) best.Place(this);
            else
            {
                // Only judge "wrong place" if no zone here is meant for this kind of item
                // (e.g. a second dressing dropped on the wound too early is not a mistake).
                bool meantForHere = zones.Exists(z => z != null && z.isActiveAndEnabled && z.Accepts(this));
                if (!meantForHere)
                    foreach (var z in new List<PlacementZone>(zones)) if (z != null && z.isActiveAndEnabled) z.OnReleasedInside(this);
                Sfx.Play(Sound.Release, transform.position, 0.4f);
            }
        }

        static HandProbe FindProbeNear(Vector3 p)
        {
            HandProbe best = null;
            float bd = 0.4f;
            foreach (var h in HandProbe.All)
            {
                float d = Vector3.Distance(h.transform.position, p);
                if (d < bd) { bd = d; best = h; }
            }
            return best;
        }

        void Update()
        {
            if (!returnHome || IsHeld || PlacedIn != null || droppedAt < 0f) return;
            bool fellOff = transform.position.y < homePos.y - 3f || transform.position.y < -1f;
            bool tooFar = Vector3.Distance(transform.position, homePos) > homeRadius;
            bool onFloor = transform.position.y < 0.08f && Time.time - droppedAt > 2.5f && Body.linearVelocity.sqrMagnitude < 0.01f;
            if (fellOff || tooFar || onFloor) ReturnHome();
        }

        public void ReturnHome()
        {
            droppedAt = -1f;
            transform.SetParent(homeParent, true);
            Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(homePos, homeRot);
            Sfx.Play(Sound.Whoosh, homePos, 0.4f);
        }

        /// <summary>Force the hand to let go (used when an item snaps into a zone).</summary>
        public void ForceRelease()
        {
            if (!IsHeld || releasing) return;
            ReleaseAll(Grab);
        }

        /// <summary>Makes every hand let go of an interactable (cleanly ends scripted/test grabs too).</summary>
        public static void ReleaseAll(XRBaseInteractable target)
        {
            if (target == null || !target.isSelected) return;
            var mgr = target.interactionManager;
            foreach (var i in new List<IXRSelectInteractor>(target.interactorsSelecting))
            {
                if (i is XRBaseInteractor b && b.isPerformingManualInteraction) b.EndManualInteraction();
                else if (mgr != null && target.interactorsSelecting.Contains(i)) mgr.SelectExit(i, target);
            }
        }

        bool releasing;

        /// <summary>Freeze the item at a spot (placed dressing, blanket on patient, ...).</summary>
        public void LockAt(Transform anchor, Vector3 localOffset, bool stillGrabbable)
        {
            ForceRelease();
            Body.isKinematic = true;
            transform.SetParent(anchor, false);
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.identity;
            Grab.enabled = stillGrabbable;
        }

        /// <summary>Picks a test interactor up programmatically (automated tests).</summary>
        public void TestGrab(XRBaseInteractor interactor) => interactor.StartManualInteraction((IXRSelectInteractable)Grab);
    }
}
