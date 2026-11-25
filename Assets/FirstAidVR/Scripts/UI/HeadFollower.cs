using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Keeps a panel comfortably in front of the user. It only moves when the user has
    /// turned well away from it, then glides smoothly (no head-locked UI, which causes
    /// discomfort in VR).
    /// </summary>
    public class HeadFollower : MonoBehaviour
    {
        public float distance = 1.1f;
        public float height = -0.25f;
        [Tooltip("Degrees the head can turn before the panel follows.")]
        public float deadZone = 28f;
        public float speed = 4f;

        Vector3 targetPos;
        bool moving;

        void OnEnable() => Snap();

        public void Snap()
        {
            var cam = Camera.main;
            if (cam == null) return;
            targetPos = Target(cam.transform);
            transform.position = targetPos;
            Face(cam.transform);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var head = cam.transform;
            var flatFwd = Flat(head.forward);
            var toPanel = Flat(transform.position - head.position);
            float angle = Vector3.Angle(flatFwd, toPanel);
            float dist = toPanel.magnitude;
            if (angle > deadZone || Mathf.Abs(dist - distance) > 0.5f) moving = true;
            if (moving)
            {
                targetPos = Target(head);
                transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-speed * Time.deltaTime));
                if (Vector3.Distance(transform.position, targetPos) < 0.02f) moving = false;
            }
            Face(head);
        }

        Vector3 Target(Transform head) => head.position + Flat(head.forward) * distance + Vector3.up * height;

        void Face(Transform head)
        {
            var dir = transform.position - head.position;
            dir.y *= 0.5f;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0;
            return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
        }
    }
}
