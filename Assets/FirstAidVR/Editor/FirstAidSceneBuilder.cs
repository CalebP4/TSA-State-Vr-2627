using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using static FirstAidVR.EditorTools.BuilderKit;

namespace FirstAidVR.EditorTools
{
    /// <summary>
    /// Generates the project's scenes and data from code, so the whole simulation can be
    /// rebuilt in one click: Tools > First Aid VR > Build All Scenes.
    /// NOTE: rebuilding a scene replaces it - hand edits to generated scenes are lost.
    /// Edit scenario wording/scoring in Assets/FirstAidVR/Data instead (those are kept).
    /// </summary>
    public static partial class FirstAidSceneBuilder
    {
        public const string SceneDir = Root + "/Scenes";
        public const string DataDir = Root + "/Data";
        public const string ResDir = Root + "/Resources/FirstAidVR";
        public const string FontPath = Root + "/Fonts/Inter-Dynamic SDF.asset";
        const string InterTtf = "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf";

        public static readonly string[] SceneOrder = { "MainMenu", "Bleeding", "InfantChoking", "Shock" };

        [MenuItem("Tools/First Aid VR/Build All Scenes", priority = 1)]
        public static void BuildAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ClearCache();
            SetupData(false);
            BuildMenuScene();
            BuildBleedingScene();
            BuildInfantScene();
            BuildShockScene();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneDir}/MainMenu.unity");
            Debug.Log("[FA] Built all scenes.");
        }

        [MenuItem("Tools/First Aid VR/Reset Scenario Content", priority = 20)]
        public static void ResetContent()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Reset scenario content",
                    "Overwrite the step and mistake text in Assets/FirstAidVR/Data with the defaults from ScenarioContent.cs?", "Reset", "Cancel")) return;
            SetupData(true);
        }

        /// <summary>Batch-mode entry point: -executeMethod FirstAidVR.EditorTools.FirstAidSceneBuilder.BatchBuild</summary>
        public static void BatchBuild()
        {
            BuildAll();
            QuestSetup.Configure();
        }

        // ---- Data ----------------------------------------------------------------------------

        public static void SetupData(bool overwrite)
        {
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(ResDir);
            Directory.CreateDirectory(SceneDir);
            var bleeding = Def("Bleeding", ScenarioContent.FillBleeding, overwrite);
            var infant = Def("InfantChoking", ScenarioContent.FillInfant, overwrite);
            var shock = Def("Shock", ScenarioContent.FillShock, overwrite);

            var catPath = ResDir + "/ScenarioCatalog.asset";
            var cat = AssetDatabase.LoadAssetAtPath<ScenarioCatalog>(catPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<ScenarioCatalog>(); AssetDatabase.CreateAsset(cat, catPath); }
            cat.scenarios = new List<ScenarioDefinition> { bleeding, infant, shock };
            EditorUtility.SetDirty(cat);

            var themePath = ResDir + "/UiTheme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeAsset>(themePath);
            if (theme == null) { theme = ScriptableObject.CreateInstance<UiThemeAsset>(); AssetDatabase.CreateAsset(theme, themePath); }
            theme.font = MakeFont();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }

        static ScenarioDefinition Def(string file, System.Action<ScenarioDefinition> fill, bool overwrite)
        {
            string path = $"{DataDir}/{file}.asset";
            var d = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<ScenarioDefinition>();
                fill(d);
                AssetDatabase.CreateAsset(d, path);
            }
            else if (overwrite) { fill(d); EditorUtility.SetDirty(d); }
            return d;
        }

        /// <summary>A dynamic TMP font from Inter, so symbols like the check mark render.</summary>
        /// <summary>Symbols used by the UI: check, cross, bullets, play, arrow, quotes, dash, ellipsis.</summary>
        const string Symbols = "✓✗•●▶←“”—…";

        static TMP_FontAsset MakeFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;
            Directory.CreateDirectory(Path.GetDirectoryName(FontPath));
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(InterTtf);
            if (ttf == null) { Debug.LogWarning("[FA] Inter font not found, using TMP default"); return TMP_Settings.defaultFontAsset; }
            var fa = TMP_FontAsset.CreateFontAsset(ttf, 72, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = "Inter-Dynamic SDF";
            AssetDatabase.CreateAsset(fa, FontPath);
            fa.atlasTextures[0].name = "Atlas";
            fa.material.name = "Material";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'\"()-+/%&#" + Symbols, out string missing);
            if (!string.IsNullOrEmpty(missing?.Trim())) Debug.LogWarning("[FA] Font is missing glyphs: " + missing + " (" + string.Join(",", System.Array.ConvertAll(missing.ToCharArray(), ch => ((int)ch).ToString("X"))) + ")");
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (fallback != null) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static void SetBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var s in SceneOrder) list.Add(new EditorBuildSettingsScene($"{SceneDir}/{s}.unity", true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static Scene NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        static void Save(Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, $"{SceneDir}/{name}.unity");
        }

        static ScenarioDefinition LoadDef(string file) => AssetDatabase.LoadAssetAtPath<ScenarioDefinition>($"{DataDir}/{file}.asset");

        // ---- Main menu ------------------------------------------------------------------------

        public static void BuildMenuScene()
        {
            var scene = NewScene("MainMenu");
            Lighting(new Color(0.55f, 0.6f, 0.7f), new Color(0.35f, 0.38f, 0.45f), new Color(0.2f, 0.2f, 0.24f),
                new Color(1f, 0.96f, 0.9f), 0.9f, new Vector3(50, 30, 0), false, Color.black);
            var env = new GameObject("Environment").transform;
            var floor = Mat("Menu Floor", Theme.Hex("2A3446"), 0.35f);
            var wall = Mat("Menu Wall", Theme.Hex("DDE3EA"), 0.05f);
            var trim = Mat("Menu Trim", Theme.Hex("1B2534"), 0.2f);
            var red = Mat("Brand Red", Theme.Red, 0.3f);
            var white = Mat("White", Color.white, 0.2f);
            Box("Floor", env, new Vector3(0, -0.05f, 0), new Vector3(10, 0.1f, 10), floor, true);
            Box("Ceiling", env, new Vector3(0, 3.2f, 0), new Vector3(10, 0.1f, 10), wall);
            Box("Wall Front", env, new Vector3(0, 1.6f, 3.2f), new Vector3(10, 3.2f, 0.1f), wall, true);
            Box("Wall Back", env, new Vector3(0, 1.6f, -3.2f), new Vector3(10, 3.2f, 0.1f), wall, true);
            Box("Wall Left", env, new Vector3(-3.6f, 1.6f, 0), new Vector3(0.1f, 3.2f, 10), wall, true);
            Box("Wall Right", env, new Vector3(3.6f, 1.6f, 0), new Vector3(0.1f, 3.2f, 10), wall, true);
            Box("Skirting", env, new Vector3(0, 0.06f, 3.14f), new Vector3(7.2f, 0.12f, 0.04f), trim);
            Box("Accent Stripe", env, new Vector3(0, 2.6f, 3.14f), new Vector3(7.2f, 0.08f, 0.03f), red);
            // Big logo cross on the wall above the board
            var logo = Box("Wall Logo", env, new Vector3(0, 2.62f, 3.12f), new Vector3(0.5f, 0.5f, 0.04f), red).transform;
            Box("Cross V", logo, new Vector3(0, 0, -0.6f), new Vector3(0.28f, 0.8f, 0.2f), white);
            Box("Cross H", logo, new Vector3(0, 0, -0.6f), new Vector3(0.8f, 0.28f, 0.2f), white);
            // Simple training-room props: first-aid wall cabinet and a mat
            var cab = Box("First Aid Cabinet", env, new Vector3(-2.6f, 1.5f, 3.05f), new Vector3(0.7f, 0.8f, 0.2f), white).transform;
            Box("Cabinet Cross V", cab, new Vector3(0, 0, -0.55f), new Vector3(0.12f, 0.45f, 0.1f), red);
            Box("Cabinet Cross H", cab, new Vector3(0, 0, -0.55f), new Vector3(0.38f, 0.14f, 0.1f), red);
            Box("Training Mat", env, new Vector3(2.2f, 0.01f, 1.2f), new Vector3(1.4f, 0.02f, 2.0f), Mat("Mat Blue", Theme.Hex("2F5D9E"), 0.1f));
            for (int i = 0; i < 3; i++)
                Box("Ceiling Light " + i, env, new Vector3(-2 + i * 2, 3.13f, 0.5f), new Vector3(1.2f, 0.04f, 0.3f), Mat("Light Panel", new Color(1f, 0.98f, 0.92f), 0, true));
            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 8f;
            fill.intensity = 1.2f;
            fill.transform.position = new Vector3(0, 2.8f, 0.8f);

            Rig(new Vector3(0, 1.35f, 0), 0f);

            var menu = new GameObject("Main Menu");
            var mm = menu.AddComponent<MainMenuManager>();
            mm.boardAnchor = Empty("Menu Board Anchor", menu.transform, new Vector3(0, 1.45f, 1.75f));
            Save(scene, "MainMenu");
        }

        // ---- Common scenario pieces ----------------------------------------------------------------

        static (ScenarioController ctrl, ScenarioUI ui) ScenarioCore(ScenarioDefinition def, Vector3 guidePos)
        {
            var go = new GameObject("Scenario");
            var ctrl = go.AddComponent<ScenarioController>();
            ctrl.definition = def;
            var ui = go.AddComponent<ScenarioUI>();
            ui.controller = ctrl;
            var flat = new Vector3(guidePos.x, 0, guidePos.z);
            ui.guideAnchor = Empty("Guide Anchor", go.transform, guidePos);
            ui.guideAnchor.rotation = Quaternion.LookRotation(flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward, Vector3.up);
            var scan = go.AddComponent<SceneSafetyScan>();
            var yellow = Mat("Marker Amber", Theme.Amber, 0, true);
            var markers = new List<GameObject>();
            foreach (var x in new[] { -1f, 1f })
            {
                var m = Ball("Look Marker", go.transform, new Vector3(x * 2.6f, 1.6f, 0.6f), Vector3.one * 0.12f, yellow);
                var s = Sign(x < 0 ? "Look left" : "Look right", m.transform, new Vector3(0, 1.8f, 0), new Vector3(0, x * 70f, 0), 3f, Theme.Amber);
                s.transform.localScale = Vector3.one * 8f;
                markers.Add(m);
            }
            scan.lookMarkers = markers.ToArray();
            return (ctrl, ui);
        }

        static PhoneItem Phone(Transform parent, Vector3 worldPos, float yaw)
        {
            var item = Item("Phone", "phone", parent, worldPos, new Vector3(0.075f, 0.012f, 0.15f), new Vector3(0, -0.02f, -0.02f), new Vector3(60, 0, 0), 0.15f);
            item.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var body = Mat("Phone Body", Theme.Hex("1A1D24"), 0.6f);
            Box("Body", item.transform, Vector3.zero, new Vector3(0.075f, 0.01f, 0.15f), body);
            Box("Glass", item.transform, new Vector3(0, 0.0055f, 0), new Vector3(0.068f, 0.001f, 0.138f), Mat("Phone Screen", Theme.Hex("0E2A4A"), 0.9f));
            var screen = Screen(item.transform, new Vector3(0, 0.0065f, 0), new Vector3(90, 0, 0), new Vector2(68, 138), 0.001f, "911", 14);
            var phone = item.gameObject.AddComponent<PhoneItem>();
            phone.screen = screen;
            return phone;
        }

        static FirstAidItem Dressing(Transform parent, string name, Vector3 pos)
        {
            var item = Item(name, "dressing", parent, pos, new Vector3(0.1f, 0.012f, 0.1f), new Vector3(0, 0.03f, -0.02f), Vector3.zero, 0.05f);
            Box("Pad", item.transform, Vector3.zero, new Vector3(0.1f, 0.01f, 0.1f), Mat("Gauze", Theme.Hex("F4F4EE"), 0.05f));
            Box("Wrapper Edge", item.transform, new Vector3(0, -0.002f, 0), new Vector3(0.106f, 0.006f, 0.106f), Mat("Wrapper Blue", Theme.Hex("7FB2E5"), 0.2f));
            return item;
        }

        static FirstAidItem Gloves(Transform parent, Vector3 pos)
        {
            var item = Item("Gloves", "gloves", parent, pos, new Vector3(0.12f, 0.05f, 0.08f), new Vector3(0, 0.04f, -0.02f), Vector3.zero, 0.08f);
            var blue = Mat("Glove Blue", Theme.Hex("6C7BEA"), 0.3f);
            Box("Box", item.transform, Vector3.zero, new Vector3(0.12f, 0.05f, 0.08f), blue);
            Box("Glove Top", item.transform, new Vector3(0, 0.03f, 0), new Vector3(0.06f, 0.012f, 0.05f), Mat("Glove Light", Theme.Hex("A9B3FF"), 0.3f));
            item.gameObject.AddComponent<GloveBox>();
            return item;
        }

        static FirstAidItem Blanket(Transform parent, string name, Vector3 pos)
        {
            var item = Item(name, "blanket", parent, pos, new Vector3(0.16f, 0.03f, 0.12f), new Vector3(0, 0.04f, -0.02f), Vector3.zero, 0.1f);
            Box("Folded", item.transform, Vector3.zero, new Vector3(0.16f, 0.03f, 0.12f), Mat("Foil Gold", Theme.Hex("D8B44A"), 0.8f));
            return item;
        }

        static GameObject FirstAidKit(Transform parent, Vector3 pos, float yaw)
        {
            var kit = Empty("First Aid Kit", parent, pos, new Vector3(0, yaw, 0));
            var red = Mat("Kit Red", Theme.Hex("C8383D"), 0.35f);
            var white = Mat("White", Color.white, 0.2f);
            Box("Base", kit, new Vector3(0, 0.03f, 0), new Vector3(0.36f, 0.06f, 0.24f), red, true);
            var lid = Box("Lid", kit, new Vector3(0, 0.12f, 0.125f), new Vector3(0.36f, 0.18f, 0.015f), red, false, new Vector3(-15, 0, 0)).transform;
            Box("Cross V", lid, new Vector3(0, 0, -0.6f), new Vector3(0.12f, 0.55f, 0.2f), white);
            Box("Cross H", lid, new Vector3(0, 0, -0.6f), new Vector3(0.36f, 0.18f, 0.2f), white);
            return kit.gameObject;
        }

        static GameObject Ambulance(Transform parent, Vector3 start, Vector3 arrive, out EmsArrival ems)
        {
            var root = new GameObject("EMS").transform;
            root.SetParent(parent, false);
            ems = root.gameObject.AddComponent<EmsArrival>();
            var v = Empty("Ambulance", root, start, new Vector3(0, -90, 0));
            var white = Mat("Ambulance White", Theme.Hex("F2F4F7"), 0.4f);
            var stripe = Mat("Ambulance Red", Theme.Red, 0.3f);
            var dark = Mat("Tyre", Theme.Hex("202020"), 0.1f);
            var glass = Mat("Window", Theme.Hex("2B3E55"), 0.9f);
            Box("Box Body", v, new Vector3(0, 1.35f, -0.4f), new Vector3(2.1f, 2.1f, 3.4f), white);
            Box("Cab", v, new Vector3(0, 1.0f, 1.75f), new Vector3(2.0f, 1.4f, 1.1f), white);
            Box("Windscreen", v, new Vector3(0, 1.35f, 2.31f), new Vector3(1.8f, 0.6f, 0.02f), glass);
            Box("Stripe", v, new Vector3(0, 1.1f, -0.1f), new Vector3(2.12f, 0.25f, 4.6f), stripe);
            var cross = Box("Side Cross", v, new Vector3(1.06f, 1.8f, -0.6f), new Vector3(0.02f, 0.7f, 0.7f), white).transform;
            Box("Cross V", cross, new Vector3(1f, 0, 0), new Vector3(1f, 0.85f, 0.3f), stripe);
            Box("Cross H", cross, new Vector3(1f, 0, 0), new Vector3(1f, 0.3f, 0.85f), stripe);
            foreach (var z in new[] { -1.4f, 1.6f })
            foreach (var x in new[] { -1f, 1f })
                Cyl("Wheel", v, new Vector3(x, 0.4f, z), new Vector3(0.8f, 0.12f, 0.8f), dark, false, new Vector3(0, 0, 90));
            var lightA = Box("Light Red", v, new Vector3(-0.5f, 2.47f, 1.0f), new Vector3(0.5f, 0.15f, 0.25f), Mat("Light Red", new Color(1f, 0.15f, 0.15f), 0, true));
            var lightB = Box("Light Blue", v, new Vector3(0.5f, 2.47f, 1.0f), new Vector3(0.5f, 0.15f, 0.25f), Mat("Light Blue", new Color(0.2f, 0.45f, 1f), 0, true));
            ems.vehicle = v;
            ems.arrivePoint = Empty("Arrive Point", root, arrive);
            ems.lights = new[] { lightA.GetComponent<Renderer>(), lightB.GetComponent<Renderer>() };
            return root.gameObject;
        }

        static void Tree(Transform parent, Vector3 pos, float h, System.Random rng)
        {
            var t = Empty("Tree", parent, pos, new Vector3(0, (float)rng.NextDouble() * 360f, 0));
            Cyl("Trunk", t, new Vector3(0, h * 0.25f, 0), new Vector3(0.22f, h * 0.25f, 0.22f), Mat("Bark", Theme.Hex("6B4A33"), 0.05f));
            var leaf = Mat("Leaves", Theme.Hex("3E8E4F"), 0.05f);
            var leaf2 = Mat("Leaves Dark", Theme.Hex("2F7342"), 0.05f);
            Ball("Canopy", t, new Vector3(0, h * 0.62f, 0), new Vector3(h * 0.55f, h * 0.5f, h * 0.55f), leaf);
            Ball("Canopy 2", t, new Vector3(h * 0.12f, h * 0.78f, h * 0.05f), new Vector3(h * 0.38f, h * 0.36f, h * 0.38f), leaf2);
        }

        static void Bench(Transform parent, Vector3 pos, float yaw, float length = 1.6f)
        {
            var b = Empty("Bench", parent, pos, new Vector3(0, yaw, 0));
            var wood = Mat("Bench Wood", Theme.Hex("A9744A"), 0.15f);
            var metal = Mat("Bench Metal", Theme.Hex("3A3F47"), 0.5f);
            Box("Seat", b, new Vector3(0, 0.44f, 0), new Vector3(length, 0.05f, 0.42f), wood, true);
            Box("Back", b, new Vector3(0, 0.75f, 0.2f), new Vector3(length, 0.32f, 0.04f), wood, true, new Vector3(-10, 0, 0));
            foreach (var x in new[] { -length * 0.42f, length * 0.42f })
                Box("Leg", b, new Vector3(x, 0.21f, 0), new Vector3(0.06f, 0.42f, 0.4f), metal, true);
        }

        /// <summary>
        /// A low-poly adult lying on their back. Local space: head toward -X, feet toward +X,
        /// face up (+Y). The person's right side is -Z (towards the player).
        /// </summary>
        static (Transform root, PatientVisuals vis, Transform rightHand, Transform rightForearmMid) AdultLying(Transform parent, Vector3 pos,
            Color shirt, Color trousers, Color skinTone, Color hair)
        {
            var root = Empty("Patient", parent, pos);
            var vis = root.gameObject.AddComponent<PatientVisuals>();
            var skinM = Mat("Skin " + ColorUtility.ToHtmlStringRGB(skinTone), skinTone, 0.25f);
            var shirtM = Mat("Shirt " + ColorUtility.ToHtmlStringRGB(shirt), shirt, 0.1f);
            var pantsM = Mat("Trousers " + ColorUtility.ToHtmlStringRGB(trousers), trousers, 0.1f);
            var hairM = Mat("Hair " + ColorUtility.ToHtmlStringRGB(hair), hair, 0.2f);
            var dark = Mat("Eye", Theme.Hex("1A1A1A"), 0.6f);
            var shoe = Mat("Shoe", Theme.Hex("2B2B2B"), 0.3f);
            var skin = new List<Renderer>();

            var upper = root;
            var chest = Box("Chest", upper, new Vector3(-0.42f, 0.13f, 0), new Vector3(0.42f, 0.2f, 0.38f), shirtM).transform;
            Box("Belly", upper, new Vector3(-0.14f, 0.12f, 0), new Vector3(0.2f, 0.18f, 0.34f), shirtM);
            var head = Ball("Head", upper, new Vector3(-0.78f, 0.12f, 0), new Vector3(0.2f, 0.22f, 0.19f), skinM).transform;
            skin.Add(head.GetComponent<Renderer>());
            Ball("Hair", head, new Vector3(-0.12f, 0.1f, 0), new Vector3(0.9f, 0.85f, 1.02f), hairM);
            var neck = Limb("Neck", upper, new Vector3(-0.62f, 0.12f, 0), new Vector3(-0.7f, 0.12f, 0), 0.05f, skinM);
            skin.Add(neck.GetComponent<Renderer>());
            var eyeL = Ball("Eye L", head, new Vector3(0.05f, 0.42f, 0.18f), new Vector3(0.12f, 0.12f, 0.12f), dark).transform;
            var eyeR = Ball("Eye R", head, new Vector3(0.05f, 0.42f, -0.18f), new Vector3(0.12f, 0.12f, 0.12f), dark).transform;
            Ball("Nose", head, new Vector3(0.18f, 0.5f, 0), new Vector3(0.12f, 0.1f, 0.1f), skinM);

            // Arms (left = +Z, right = -Z). The right arm is stretched out towards the player.
            var sleeve = shirtM;
            Limb("Upper Arm L", upper, new Vector3(-0.56f, 0.1f, 0.22f), new Vector3(-0.3f, 0.08f, 0.27f), 0.055f, sleeve);
            skin.Add(Limb("Forearm L", upper, new Vector3(-0.3f, 0.08f, 0.27f), new Vector3(-0.05f, 0.07f, 0.28f), 0.045f, skinM).GetComponent<Renderer>());
            skin.Add(Ball("Hand L", upper, new Vector3(0.02f, 0.065f, 0.28f), new Vector3(0.08f, 0.05f, 0.1f), skinM).GetComponent<Renderer>());
            Limb("Upper Arm R", upper, new Vector3(-0.56f, 0.1f, -0.22f), new Vector3(-0.32f, 0.07f, -0.33f), 0.055f, sleeve);
            skin.Add(Limb("Forearm R", upper, new Vector3(-0.32f, 0.07f, -0.33f), new Vector3(-0.06f, 0.06f, -0.4f), 0.045f, skinM).GetComponent<Renderer>());
            var handR = Ball("Hand R", upper, new Vector3(0.01f, 0.055f, -0.42f), new Vector3(0.08f, 0.05f, 0.1f), skinM).transform;
            skin.Add(handR.GetComponent<Renderer>());
            var forearmMid = Empty("Forearm R Mid", upper, new Vector3(-0.19f, 0.07f, -0.365f));

            // Legs
            var hips = Box("Hips", root, new Vector3(0.02f, 0.11f, 0), new Vector3(0.22f, 0.18f, 0.34f), pantsM).transform;
            foreach (var z in new[] { -0.09f, 0.09f })
            {
                Limb("Thigh", root, new Vector3(0.08f, 0.1f, z), new Vector3(0.5f, 0.08f, z * 1.1f), 0.075f, pantsM);
                Limb("Shin", root, new Vector3(0.5f, 0.08f, z * 1.1f), new Vector3(0.9f, 0.07f, z * 1.2f), 0.06f, pantsM);
                Box("Shoe", root, new Vector3(0.96f, 0.1f, z * 1.2f), new Vector3(0.1f, 0.12f, 0.1f), shoe);
            }

            vis.chest = chest;
            vis.head = head;
            vis.skin = skin.ToArray();
            vis.eyes = new[] { eyeL, eyeR };
            vis.normalSkin = skinTone;
            return (root, vis, handR, forearmMid);
        }

        // ---- Scenario 1: Severe bleeding ----------------------------------------------------------

        public static void BuildBleedingScene()
        {
            var def = LoadDef("Bleeding");
            var scene = NewScene("Bleeding");
            Lighting(new Color(0.62f, 0.74f, 0.9f), new Color(0.55f, 0.6f, 0.55f), new Color(0.32f, 0.3f, 0.25f),
                new Color(1f, 0.95f, 0.85f), 1.15f, new Vector3(48, -35, 0), true, new Color(0.72f, 0.82f, 0.9f));
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");

            var env = new GameObject("Environment").transform;
            Box("Ground", env, new Vector3(0, -0.05f, 0), new Vector3(60, 0.1f, 60), Mat("Grass", Theme.Hex("6FA55A"), 0.05f), true);
            Box("Path", env, new Vector3(0, 0.005f, 3.2f), new Vector3(60, 0.02f, 2.6f), Mat("Path", Theme.Hex("B9B2A3"), 0.1f), true);
            Box("Path Edge A", env, new Vector3(0, 0.02f, 1.88f), new Vector3(60, 0.04f, 0.08f), Mat("Kerb", Theme.Hex("8D877C"), 0.1f));
            Box("Path Edge B", env, new Vector3(0, 0.02f, 4.52f), new Vector3(60, 0.04f, 0.08f), Mat("Kerb", Theme.Hex("8D877C"), 0.1f));
            var rng = new System.Random(7);
            for (int i = 0; i < 22; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = 6f + (float)rng.NextDouble() * 16f;
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                if (Mathf.Abs(p.z - 3.2f) < 2.2f) p.z += 4.5f * Mathf.Sign(p.z - 3.2f + 0.01f);
                Tree(env, p, 3.5f + (float)rng.NextDouble() * 2.5f, rng);
            }
            Bench(env, new Vector3(0.95f, 0, 0.35f), -90f);
            // Lamp post
            var lamp = Empty("Lamp Post", env, new Vector3(-2.4f, 0, 2.0f));
            Cyl("Pole", lamp, new Vector3(0, 1.6f, 0), new Vector3(0.08f, 1.6f, 0.08f), Mat("Bench Metal", Theme.Hex("3A3F47"), 0.5f));
            Ball("Lamp", lamp, new Vector3(0, 3.25f, 0), new Vector3(0.3f, 0.3f, 0.3f), Mat("Lamp Glass", Theme.Hex("FFF4D6"), 0, true));
            // The fallen bicycle and broken bottle (the cause, kept non-graphic)
            var bike = Empty("Bicycle", env, new Vector3(1.6f, 0.05f, 1.9f), new Vector3(0, 20, 90));
            var frame = Mat("Bike Frame", Theme.Hex("2F80ED"), 0.5f);
            var tyre = Mat("Tyre", Theme.Hex("202020"), 0.1f);
            Cyl("Wheel A", bike, new Vector3(0, -0.02f, -0.5f), new Vector3(0.66f, 0.02f, 0.66f), tyre);
            Cyl("Wheel B", bike, new Vector3(0, -0.02f, 0.5f), new Vector3(0.66f, 0.02f, 0.66f), tyre);
            Box("Frame", bike, new Vector3(0, -0.04f, 0), new Vector3(0.04f, 0.04f, 1.0f), frame, false, new Vector3(30, 0, 0));
            var glass = Mat("Bottle Glass", new Color(0.35f, 0.6f, 0.4f, 0.75f), 0.9f);
            for (int i = 0; i < 5; i++)
                Box("Glass Shard", env, new Vector3(0.45f + i * 0.05f, 0.012f, 1.62f + (i % 2) * 0.05f), new Vector3(0.03f, 0.005f, 0.02f), glass, false, new Vector3(0, i * 40, 0));
            Ball("Helmet", env, new Vector3(-0.9f, 0.08f, 0.75f), new Vector3(0.24f, 0.16f, 0.3f), Mat("Helmet", Theme.Hex("F2C94C"), 0.5f));

            var (ctrl, ui) = ScenarioCore(def, new Vector3(-0.9f, 1.4f, 0.95f));
            Rig(new Vector3(0, 1.35f, 0), 0f);

            // Patient lying on the grass beside the path
            var (patient, vis, handR, forearmMid) = AdultLying(env, new Vector3(0.05f, 0, 1.25f),
                Theme.Hex("E0663C"), Theme.Hex("2E3B55"), new Color(0.80f, 0.6f, 0.46f), Theme.Hex("3B2A20"));
            patient.name = "Patient";
            var blood = Mat("Blood", Theme.Hex("7A1414"), 0.6f);
            Box("Wound", patient, forearmMid.localPosition + new Vector3(0, 0.042f, 0), new Vector3(0.07f, 0.006f, 0.03f), Mat("Wound", Theme.Hex("8E1B1B"), 0.7f), false, new Vector3(0, -15, 0));
            var pool = Cyl("Blood Pool", patient, forearmMid.localPosition + new Vector3(0, -0.065f, -0.06f), new Vector3(0.2f, 0.002f, 0.15f), blood).transform;

            // Interaction zones on the patient
            var shoulder = Zone<TouchZone>("Patient Shoulder", patient, new Vector3(-0.55f, 0.25f, -0.2f), new Vector3(0.22f, 0.4f, 0.22f));
            shoulder.actionId = "check_patient";
            var wound = Zone<PlacementZone>("Wound Zone", patient, forearmMid.localPosition + new Vector3(0, 0.15f, 0), new Vector3(0.24f, 0.32f, 0.22f));
            wound.acceptedItems = new List<string> { "dressing" };
            wound.actionIds = new List<string> { "apply_dressing", "second_dressing" };
            wound.capacity = 2;
            wound.anchor = Empty("Dressing Anchor", patient, forearmMid.localPosition + new Vector3(0, 0.052f, 0), new Vector3(0, -15, 0));
            wound.priority = 5;
            wound.ghost = Box("Dressing Ghost", wound.anchor, Vector3.zero, new Vector3(0.1f, 0.01f, 0.1f), Mat("Ghost", new Color(0.3f, 0.9f, 0.5f, 0.35f), 0, true));
            var body = Zone<PlacementZone>("Body Zone", patient, new Vector3(-0.1f, 0.2f, 0), new Vector3(1.9f, 0.4f, 0.7f));
            body.wrongItemAction = "dressing_wrong_place";
            body.wrongItems = new List<string> { "dressing" };
            body.holdToPlace = 0f;
            body.priority = -1;
            var press = Zone<PressHoldZone>("Pressure Zone", patient, forearmMid.localPosition + new Vector3(0, 0.17f, 0), new Vector3(0.22f, 0.36f, 0.22f));
            press.actionId = "apply_pressure";
            press.interruptedAction = "pressure_released";
            press.holdSeconds = 12f;
            var bandage = Zone<PlacementZone>("Bandage Zone", patient, forearmMid.localPosition + new Vector3(0, 0.15f, 0), new Vector3(0.3f, 0.34f, 0.26f));
            bandage.acceptedItems = new List<string> { "bandage" };
            bandage.actionIds = new List<string> { "bandage" };
            bandage.priority = 6;
            var wrap = Cyl("Bandage Wrap", patient, forearmMid.localPosition, new Vector3(0.11f, 0.07f, 0.11f), Mat("Bandage", Theme.Hex("F1EEE6"), 0.05f), false,
                new Vector3(0, -15, 90)).gameObject;
            var fingers = Zone<TouchZone>("Patient Fingers", patient, handR.localPosition + new Vector3(0, 0.12f, 0), new Vector3(0.16f, 0.3f, 0.16f));
            fingers.actionId = "check_circulation";
            var blanketZone = Zone<PlacementZone>("Blanket Zone", patient, new Vector3(-0.2f, 0.3f, 0.05f), new Vector3(1.2f, 0.5f, 0.5f));
            blanketZone.acceptedItems = new List<string> { "blanket" };
            blanketZone.actionIds = new List<string> { "keep_warm" };
            blanketZone.anchor = Empty("Blanket Anchor", patient, new Vector3(0.1f, 0.2f, 0.06f));
            blanketZone.priority = 1;
            blanketZone.holdToPlace = 0.5f;

            // Equipment on the bench
            var equip = new GameObject("Equipment").transform;
            FirstAidKit(equip, new Vector3(0.95f, 0.465f, 0.05f), -90f);
            Gloves(equip, new Vector3(1.0f, 0.5f, 0.4f));
            Dressing(equip, "Dressing A", new Vector3(0.85f, 0.48f, 0.55f));
            Dressing(equip, "Dressing B", new Vector3(1.0f, 0.48f, 0.6f));
            var roll = Item("Roller Bandage", "bandage", equip, new Vector3(0.88f, 0.5f, 0.75f), new Vector3(0.07f, 0.06f, 0.07f), new Vector3(0, 0.05f, -0.02f), Vector3.zero, 0.06f);
            Cyl("Roll", roll.transform, Vector3.zero, new Vector3(0.065f, 0.03f, 0.065f), Mat("Bandage", Theme.Hex("F1EEE6"), 0.05f));
            var blanket = Blanket(equip, "Emergency Blanket", new Vector3(1.02f, 0.49f, 0.82f));
            var phone = Phone(equip, new Vector3(0.85f, 0.475f, 0.95f), 180f);

            Ambulance(env, new Vector3(26f, 0, 3.4f), new Vector3(5.5f, 0, 3.4f), out var ems);

            var script = ctrl.gameObject.AddComponent<BleedingScenario>();
            script.controller = ctrl;
            script.ui = ui;
            script.patient = vis;
            script.phone = phone;
            script.ems = ems;
            script.talkAnchor = Empty("Talk Anchor", ctrl.transform, new Vector3(-0.35f, 0.95f, 1.95f));
            script.talkAnchor.rotation = Quaternion.LookRotation(new Vector3(-0.35f, 0, 1.95f).normalized);
            script.shoulderTouch = shoulder;
            script.woundZone = wound;
            script.pressureZone = press;
            script.bandageZone = bandage;
            script.fingerTouch = fingers;
            script.blanketZone = blanketZone;
            script.bloodPool = pool;
            script.bandageWrap = wrap;
            var unfold = blanket.gameObject.AddComponent<UnfoldOnPlace>();
            unfold.unfoldedScale = new Vector3(6.5f, 0.6f, 4.2f);
            Save(scene, "Bleeding");
        }
    }
}
