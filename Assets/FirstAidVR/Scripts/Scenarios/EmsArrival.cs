using System.Collections;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// The ambulance / paramedics. After 911 is called a siren is heard far away; when
    /// care is complete the vehicle pulls up with flashing lights and EMS takes over.
    /// </summary>
    public class EmsArrival : MonoBehaviour
    {
        public Transform vehicle;
        public Transform arrivePoint;
        public Renderer[] lights;
        public float driveSeconds = 5f;
        [Tooltip("For indoor scenes: no vehicle, paramedics knock and the door opens.")]
        public GameObject paramedics;

        public bool Arrived { get; private set; }
        AudioSource siren;
        Vector3 startPos;

        void Awake()
        {
            if (vehicle != null) { startPos = vehicle.position; vehicle.gameObject.SetActive(false); }
            if (paramedics != null) paramedics.SetActive(false);
        }

        /// <summary>911 has been called: start a distant siren.</summary>
        public void Dispatch()
        {
            if (siren != null) return;
            siren = Sfx.Loop(Sound.Siren, transform, 0.05f);
            siren.transform.position = vehicle != null ? startPos : transform.position + Vector3.forward * 30f;
        }

        public void Arrive()
        {
            if (Arrived) return;
            Arrived = true;
            Dispatch();
            StartCoroutine(Drive());
        }

        IEnumerator Drive()
        {
            if (vehicle != null) vehicle.gameObject.SetActive(true);
            float t = 0f;
            Vector3 end = arrivePoint != null ? arrivePoint.position : startPos;
            while (t < 1f)
            {
                t += Time.deltaTime / driveSeconds;
                float e = 1f - (1f - t) * (1f - t);
                if (vehicle != null) vehicle.position = Vector3.Lerp(startPos, end, e);
                if (siren != null)
                {
                    siren.volume = Mathf.Lerp(0.05f, 0.45f, t);
                    siren.transform.position = vehicle != null ? vehicle.position : siren.transform.position;
                }
                yield return null;
            }
            if (paramedics != null) paramedics.SetActive(true);
            if (siren != null) siren.volume = 0.15f;
            FeedbackToast.Show(ToastKind.Info, "EMS has arrived", "Paramedics are taking over care. Tell them what happened and what you did.");
        }

        void Update()
        {
            if (lights == null || !Arrived) return;
            bool phase = Mathf.Repeat(Time.time * 3f, 1f) < 0.5f;
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].enabled = (i % 2 == 0) == phase;
        }
    }
}
