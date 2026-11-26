using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FirstAidVR
{
    /// <summary>
    /// Lets the user help a seated patient lie down: grab the handle on their shoulder
    /// and move your hand down and away. The patient's upper body follows your hand.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class GuidedLowering : MonoBehaviour
    {
        public string actionId = "lay_down";
        [Tooltip("Pivot at the hips; rotated from sitting to lying.")]
        public Transform upperBody;
        public Vector3 sittingEuler = Vector3.zero;
        public Vector3 lyingEuler = new Vector3(-90f, 0f, 0f);
        [Tooltip("How far (m) the hand moves to lie the patient fully down.")]
        public float handTravel = 0.45f;

        public float Amount { get; private set; }
        public bool Done { get; private set; }
        public event Action Lowered;

        XRSimpleInteractable interactable;
        Transform hand;
        Vector3 grabStart;
        float startAmount;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(a => { hand = a.interactorObject.transform; grabStart = hand.position; startAmount = Amount; });
            interactable.selectExited.AddListener(_ =>
            {
                hand = null;
                if (Amount > 0.7f) Complete();
            });
        }

        void Update()
        {
            if (Done)
            {
                Amount = Mathf.MoveTowards(Amount, 1f, Time.deltaTime * 2f);
            }
            else if (hand != null)
            {
                var c = ScenarioController.Active;
                if (c == null || c.Phase == ScenarioPhase.Active)
                {
                    float travel = grabStart.y - hand.position.y;
                    Amount = Mathf.Clamp01(startAmount + travel / handTravel);
                    if (Amount >= 0.98f) Complete();
                }
            }
            Apply();
        }

        public void SetAmount(float a) { Amount = Mathf.Clamp01(a); Apply(); }

        void Apply()
        {
            if (upperBody != null)
                upperBody.localRotation = Quaternion.Slerp(Quaternion.Euler(sittingEuler), Quaternion.Euler(lyingEuler), Amount);
        }

        public void Complete()
        {
            if (Done) return;
            Done = true;
            hand = null;
            FirstAidItem.ReleaseAll(interactable);
            interactable.enabled = false;
            Sfx.Play(Sound.Place, transform.position, 0.6f);
            ScenarioController.Active?.Report(actionId);
            Lowered?.Invoke();
        }
    }
}
