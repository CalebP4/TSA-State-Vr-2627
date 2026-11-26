using UnityEditor;
using UnityEngine;

namespace FirstAidVR.EditorTools
{
    /// <summary>
    /// Turns the XR Interaction Simulator on/off for Play mode in the editor, so the
    /// simulation can be tried with keyboard and mouse when no headset is connected.
    /// Leave it OFF when testing with a Quest over Link.
    /// </summary>
    public static class DesktopSimulatorToggle
    {
        const string Menu = "Tools/First Aid VR/Desktop Simulator (no headset)";
        const string Prefab = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        const string SettingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

        static SerializedObject Settings()
        {
            var s = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsPath);
            return s == null ? null : new SerializedObject(s);
        }

        [MenuItem(Menu, priority = 10)]
        public static void Toggle()
        {
            var so = Settings();
            if (so == null) { Debug.LogWarning("[FA] XRI simulator settings not found at " + SettingsPath); return; }
            var auto = so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
            bool on = !auto.boolValue;
            auto.boolValue = on;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[FA] Desktop simulator " + (on ? "ON - press Play and use keyboard + mouse." : "OFF"));
        }

        [MenuItem(Menu, true)]
        static bool Validate()
        {
            var so = Settings();
            UnityEditor.Menu.SetChecked(Menu, so != null && so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue);
            return so != null;
        }
    }
}
