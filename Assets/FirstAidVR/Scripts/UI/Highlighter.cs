using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Training Mode helper: a bobbing arrow and a pulsing ring that point at the object
    /// the user should use next. Hidden in Certification Mode.
    /// </summary>
    public class Highlighter : MonoBehaviour
    {
        static Highlighter instance;
        Transform arrow, ring;
        Transform target;
        Material mat;

        public static Highlighter Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("Training Highlighter").AddComponent<Highlighter>();
                return instance;
            }
        }

        public static Transform Current => instance != null ? instance.target : null;

        void Awake()
        {
            instance = this;
            mat = MaterialLibrary.Unlit(Theme.Amber);
            arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            Strip(arrow, "Arrow");
            arrow.localScale = new Vector3(0.025f, 0.06f, 0.025f);
            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Strip(tip, "Tip");
            tip.SetParent(arrow, false);
            tip.localPosition = new Vector3(0, -1.25f, 0);
            tip.localRotation = Quaternion.Euler(45, 0, 45);
            tip.localScale = new Vector3(2.2f, 0.5f, 2.2f);
            ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            Strip(ring, "Ring");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Unlit(Theme.Amber.WithAlpha(0.28f));
            ring.localScale = new Vector3(0.2f, 0.002f, 0.2f);
            Show(null);
        }

        void Strip(Transform t, string n)
        {
            t.name = n;
            Destroy(t.GetComponent<Collider>());
            t.GetComponent<Renderer>().sharedMaterial = mat;
            t.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (t.parent == null) t.SetParent(transform, false);
        }

        /// <summary>Point at a scene object by name (null or empty hides the highlight).</summary>
        public static void Point(string objectName)
        {
            Transform t = null;
            if (!string.IsNullOrEmpty(objectName))
            {
                var go = GameObject.Find(objectName);
                if (go != null) t = go.transform;
            }
            Instance.Show(t);
        }

        public void Show(Transform t)
        {
            target = t;
            arrow.gameObject.SetActive(t != null);
            ring.gameObject.SetActive(t != null);
        }

        void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (arrow.gameObject.activeSelf) { arrow.gameObject.SetActive(false); ring.gameObject.SetActive(false); }
                return;
            }
            if (!arrow.gameObject.activeSelf) { arrow.gameObject.SetActive(true); ring.gameObject.SetActive(true); }
            var b = Bounds(target);
            float bob = Mathf.Sin(Time.time * 4f) * 0.03f;
            arrow.position = new Vector3(b.center.x, b.max.y + 0.16f + bob, b.center.z);
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.12f;
            float size = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) * 1.2f, 0.1f, 0.35f) * pulse;
            ring.position = new Vector3(b.center.x, b.min.y + 0.005f, b.center.z);
            ring.localScale = new Vector3(size, 0.002f, size);
        }

        static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                var c = t.GetComponent<Collider>();
                return c != null ? c.bounds : new Bounds(t.position, Vector3.one * 0.1f);
            }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
