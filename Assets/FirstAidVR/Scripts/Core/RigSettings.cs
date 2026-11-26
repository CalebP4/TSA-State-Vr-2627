using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace FirstAidVR
{
    /// <summary>
    /// Sits on the XR Origin and applies the comfort options from <see cref="GameSettings"/>
    /// (smooth or teleport movement, snap or smooth turning, vignette, seated height).
    /// </summary>
    public class RigSettings : MonoBehaviour
    {
        public Transform cameraOffset;
        public GameObject vignette;

        void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        void OnDisable() => GameSettings.Changed -= Apply;

        public void Apply()
        {
            foreach (var m in GetComponentsInChildren<ControllerInputActionManager>(true))
            {
                m.smoothMotionEnabled = GameSettings.SmoothMove;
                m.smoothTurnEnabled = GameSettings.SmoothTurn;
            }
            if (vignette != null) vignette.SetActive(GameSettings.Comfort);
            ApplyHeight();
        }

        // XROrigin owns the Camera Offset height (0 in Floor mode, eye height in Device mode).
        // The seated-mode offset is added on top of whatever XROrigin last set.
        float baseY, appliedY = float.NaN;

        void LateUpdate() => ApplyHeight();

        void ApplyHeight()
        {
            if (cameraOffset == null) return;
            var p = cameraOffset.localPosition;
            if (float.IsNaN(appliedY) || !Mathf.Approximately(p.y, appliedY)) baseY = p.y;
            p.y = baseY + GameSettings.HeightOffset;
            appliedY = p.y;
            cameraOffset.localPosition = p;
        }
    }
}
