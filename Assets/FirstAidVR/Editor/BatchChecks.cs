using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstAidVR.EditorTools
{
    /// <summary>Batch-mode checks used during development (player build, simulator toggle).</summary>
    public static class BatchChecks
    {
        /// <summary>
        /// Builds a Windows player to Builds/Win64. This proves the game code compiles and the
        /// scenes package for a player build (it does not test Android or the headset).
        /// </summary>
        [MenuItem("Tools/First Aid VR/Build Windows Check", priority = 40)]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Win64");
            var scenes = new string[FirstAidSceneBuilder.SceneOrder.Length];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = $"{FirstAidSceneBuilder.SceneDir}/{FirstAidSceneBuilder.SceneOrder[i]}.unity";
            // Only the Mono backend is installed for Windows on the dev PC; restore the setting afterwards.
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            var backend = PlayerSettings.GetScriptingBackend(target);
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(scenes, "Builds/Win64/FirstResponseVR.exe", BuildTarget.StandaloneWindows64, BuildOptions.None); }
            finally { PlayerSettings.SetScriptingBackend(target, backend); }
            var s = report.summary;
            File.WriteAllText("Logs/FirstAidBuildCheck.txt",
                $"result: {s.result}\nerrors: {s.totalErrors}\nwarnings: {s.totalWarnings}\nsize: {s.totalSize / (1024 * 1024)} MB\ntime: {s.totalTime}\n");
            Debug.Log("[FA] Windows build: " + s.result);
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Turns the desktop simulator on, records the settings, then turns it back off.</summary>
        public static void SimulatorToggleCheck()
        {
            const string path = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
            string Read()
            {
                var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
                var prefab = so.FindProperty("m_SimulatorPrefab").objectReferenceValue;
                return $"auto={so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue} editorOnly={so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue} prefab={(prefab == null ? "none" : AssetDatabase.GetAssetPath(prefab))}";
            }
            var log = "before: " + Read() + "\n";
            DesktopSimulatorToggle.Toggle();
            log += "after ON: " + Read() + "\n";
            DesktopSimulatorToggle.Toggle();
            log += "after OFF: " + Read() + "\n";
            File.WriteAllText("Logs/FirstAidSimulatorCheck.txt", log);
        }
    }
}
