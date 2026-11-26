using TMPro;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>Holds the UI font so runtime-built screens can find it (Resources/FirstAidVR/UiTheme).</summary>
    [CreateAssetMenu(menuName = "First Aid VR/UI Theme", fileName = "UiTheme")]
    public class UiThemeAsset : ScriptableObject
    {
        public TMP_FontAsset font;
    }
}
