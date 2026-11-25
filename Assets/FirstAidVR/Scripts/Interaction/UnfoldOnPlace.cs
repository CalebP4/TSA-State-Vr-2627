using UnityEngine;

namespace FirstAidVR
{
    /// <summary>A folded blanket that spreads out over the patient once it is placed on them.</summary>
    [RequireComponent(typeof(FirstAidItem))]
    public class UnfoldOnPlace : MonoBehaviour
    {
        public Vector3 unfoldedScale = new Vector3(6f, 0.6f, 4f);
        public float seconds = 0.5f;
        FirstAidItem item;
        float t = -1f;
        Vector3 from;

        void Awake() => item = GetComponent<FirstAidItem>();

        void Update()
        {
            if (t < 0f)
            {
                if (item.PlacedIn == null) return;
                t = 0f;
                from = transform.localScale;
                Sfx.Play(Sound.Whoosh, transform.position, 0.6f);
            }
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            transform.localScale = Vector3.Lerp(from, unfoldedScale, Mathf.SmoothStep(0, 1, t));
        }
    }
}
