using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// "Check the scene is safe": the user has to look around (turn their head across a
    /// wide arc) before starting care. Only counts at the start: once another step is
    /// done the chance has passed, so it cannot be earned by accident later.
    /// </summary>
    public class SceneSafetyScan : MonoBehaviour
    {
        public string actionId = "scene_safe";
        [Tooltip("Total head turn needed, in degrees (left + right).")]
        public float requiredDegrees = 120f;
        [Tooltip("Optional marker objects that light up while scanning (Training only).")]
        public GameObject[] lookMarkers;

        public float Progress => Mathf.Clamp01((maxYaw - minYaw) / requiredDegrees);
        public bool Done { get; private set; }

        float startYaw, minYaw, maxYaw, lastYaw, unwrapped;
        bool tracking;

        void Update()
        {
            var ctrl = ScenarioController.Active;
            var cam = Camera.main;
            if (ctrl == null || cam == null || Done) return;
            bool show = ctrl.Phase == ScenarioPhase.Active && ctrl.IsTraining && !Window(ctrl);
            if (lookMarkers != null)
                foreach (var m in lookMarkers) if (m != null && m.activeSelf != show) m.SetActive(show);
            if (ctrl.Phase != ScenarioPhase.Active) return;
            if (Window(ctrl)) { enabled = false; return; }

            float yaw = cam.transform.eulerAngles.y;
            if (!tracking)
            {
                tracking = true;
                startYaw = lastYaw = yaw;
                unwrapped = minYaw = maxYaw = 0f;
            }
            unwrapped += Mathf.DeltaAngle(lastYaw, yaw);
            lastYaw = yaw;
            minYaw = Mathf.Min(minYaw, unwrapped);
            maxYaw = Mathf.Max(maxYaw, unwrapped);
            if (maxYaw - minYaw >= requiredDegrees) Complete();
        }

        /// <summary>True when another step has been completed (the scan window is over).</summary>
        bool Window(ScenarioController ctrl)
        {
            foreach (var s in ctrl.Steps) if (s.done && s.def.id != actionId) return true;
            return false;
        }

        public void Complete()
        {
            if (Done) return;
            Done = true;
            if (lookMarkers != null) foreach (var m in lookMarkers) if (m != null) m.SetActive(false);
            ScenarioController.Active?.Report(actionId);
        }
    }
}
