using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace FirstAidVR.EditorTools
{
    /// <summary>
    /// Sets the project up for Meta Quest 2: Android player settings and OpenXR with the
    /// Meta Quest feature and Touch controller profile. Tools > First Aid VR > Configure for Quest 2.
    /// </summary>
    public static class QuestSetup
    {
        public const string PackageId = "com.tsastate.firstresponsevr";

        [MenuItem("Tools/First Aid VR/Configure for Quest 2", priority = 2)]
        public static void Configure()
        {
            PlayerSettings.productName = Theme.ProductName;
            PlayerSettings.companyName = "TSA State VR Team";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            // OpenXR loader on Android
            var perTarget = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (perTarget != null && perTarget.Manager != null)
            {
                var mgr = perTarget.Manager;
                bool has = false;
                foreach (var l in mgr.activeLoaders) if (l is OpenXRLoader) has = true;
                if (!has) UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.AssignLoader(mgr, typeof(OpenXRLoader).FullName, BuildTargetGroup.Android);
                perTarget.InitManagerOnStart = true;
                EditorUtility.SetDirty(perTarget);
            }
            else Debug.LogWarning("[FA] No XR settings for Android yet - open Project Settings > XR Plug-in Management once.");

            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings != null)
            {
                foreach (var f in settings.GetFeatures())
                {
                    string id = FeatureId(f);
                    // Quest needs the Meta Quest feature and its controller profiles.
                    if (id == "com.unity.openxr.feature.metaquest" ||
                        id == "com.unity.openxr.feature.input.oculustouch" || id == "com.unity.openxr.feature.input.metaquestplus" ||
                        id == "com.unity.openxr.feature.input.metaquestpro")
                        f.enabled = true;
                    // Android XR (non-Quest) features are not used by this project on Quest.
                    if (id.Contains("androidxr") || id == "com.unity.openxr.feature.oculusquest") f.enabled = false;
                    EditorUtility.SetDirty(f);
                }
                settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
            Report();
            Debug.Log("[FA] Quest 2 configuration applied.");
        }

        static string FeatureId(OpenXRFeature f) =>
            new SerializedObject(f).FindProperty("featureIdInternal")?.stringValue ?? "";

        [MenuItem("Tools/First Aid VR/Write XR Report", priority = 30)]
        public static void Report()
        {
            var sb = new StringBuilder();
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                sb.AppendLine("== " + group);
                var per = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
                if (per != null && per.Manager != null)
                    foreach (var l in per.Manager.activeLoaders) sb.AppendLine("  loader: " + l.GetType().Name);
                var s = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (s == null) continue;
                foreach (var f in s.GetFeatures())
                    sb.AppendLine($"  {(f.enabled ? "[x]" : "[ ]")} {f.name}  ({FeatureId(f)})");
            }
            sb.AppendLine("Android support installed: " + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/FirstAidXRReport.txt", sb.ToString());
        }

        [MenuItem("Tools/First Aid VR/Build Quest 2 APK", priority = 3)]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[FA] Android Build Support is not installed. Add it in Unity Hub > Installs > Add modules.");
                return;
            }
            Configure();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Directory.CreateDirectory("Builds");
            var scenes = new string[FirstAidSceneBuilder.SceneOrder.Length];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = $"{FirstAidSceneBuilder.SceneDir}/{FirstAidSceneBuilder.SceneOrder[i]}.unity";
            var report = BuildPipeline.BuildPlayer(scenes, "Builds/FirstResponseVR.apk", BuildTarget.Android, BuildOptions.None);
            Debug.Log("[FA] Build result: " + report.summary.result + " size " + report.summary.totalSize);
        }
    }
}
