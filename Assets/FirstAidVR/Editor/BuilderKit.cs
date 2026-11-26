using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

namespace FirstAidVR.EditorTools
{
    /// <summary>Shared building blocks for the scene builder: materials, primitives, the XR rig, items.</summary>
    public static class BuilderKit
    {
        public const string Root = "Assets/FirstAidVR";
        public const string MatDir = Root + "/Materials";
        public const string RigPrefab = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
        public const string InputActions = "Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/XRI Default Input Actions.inputactions";

        // ---- Materials ---------------------------------------------------------------------

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public static Material Mat(string name, Color c, float smooth = 0.15f, bool unlit = false)
        {
            if (mats.TryGetValue(name, out var cached) && cached != null) return cached;
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Simple Lit");
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", c);
            if (!unlit)
            {
                m.SetFloat("_Smoothness", smooth);
                m.SetFloat("_SpecularHighlights", smooth > 0.3f ? 1f : 0f);
                if (smooth <= 0.3f) m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
            if (c.a < 1f) MaterialLibrary.MakeTransparent(m);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        public static void ClearCache() => mats.Clear();

        // ---- Primitives ----------------------------------------------------------------------

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale,
            Material mat, bool collider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = false, Vector3? euler = null)
            => Prim(PrimitiveType.Cube, name, parent, pos, size, mat, collider, euler);

        public static GameObject Ball(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = false)
            => Prim(PrimitiveType.Sphere, name, parent, pos, size, mat, collider);

        public static GameObject Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = false, Vector3? euler = null)
            => Prim(PrimitiveType.Cylinder, name, parent, pos, size, mat, collider, euler);

        /// <summary>A capsule stretched between two local points (arms, legs).</summary>
        public static GameObject Limb(string name, Transform parent, Vector3 from, Vector3 to, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            var dir = to - from;
            go.transform.localPosition = (from + to) * 0.5f;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(radius * 2f, dir.magnitude * 0.5f + radius, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static Transform Empty(string name, Transform parent, Vector3 localPos, Vector3? euler = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return t;
        }

        public static T Zone<T>(string name, Transform parent, Vector3 pos, Vector3 size, Vector3? euler = null) where T : Component
        {
            var t = Empty(name, parent, pos, euler);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            return t.gameObject.AddComponent<T>();
        }

        // ---- Grabbable items -------------------------------------------------------------------

        /// <summary>
        /// Builds a grabbable item: root with collider + rigidbody + XRGrabInteractable +
        /// FirstAidItem, a "Visual" child, and a "Grip" attach point.
        /// </summary>
        public static FirstAidItem Item(string name, string itemId, Transform parent, Vector3 worldPos, Vector3 colliderSize,
            Vector3 gripLocalPos, Vector3 gripEuler, float mass = 0.1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            var col = go.AddComponent<BoxCollider>();
            col.size = colliderSize;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1f;
            var grab = go.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear();
            grab.colliders.Add(col); // only the item itself is grabbable, never trigger zones on it
            var grip = Empty("Grip", go.transform, gripLocalPos, gripEuler);
            FirstAidItem.Configure(grab, rb, grip);
            var item = go.AddComponent<FirstAidItem>();
            item.itemId = itemId;
            item.displayName = name;
            item.grip = grip;
            return item;
        }

        // ---- Scene essentials -------------------------------------------------------------------

        public static void Lighting(Color ambientSky, Color ambientEquator, Color ambientGround, Color sun, float intensity, Vector3 sunEuler, bool fog, Color fogColor)
        {
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = sun;
            light.intensity = intensity;
            light.shadows = LightShadows.Hard;
            light.shadowStrength = 0.6f;
            light.transform.rotation = Quaternion.Euler(sunEuler);
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 60f;
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
        }

        /// <summary>Adds the XR rig (from the VR template), interaction manager and XR UI event system.</summary>
        public static GameObject Rig(Vector3 pos, float yaw)
        {
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<XRUIInputModule>();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));

            // Far grabs pull the item into the hand instead of leaving it on the end of the ray.
            foreach (var nf in rig.GetComponentsInChildren<NearFarInteractor>(true))
            {
                nf.farAttachMode = InteractorFarAttachMode.Near;
                EditorUtility.SetDirty(nf);
            }

            var actions = AssetDatabase.LoadAllAssetsAtPath(InputActions);
            InputActionReference Ref(string path)
            {
                foreach (var a in actions)
                    if (a is InputActionReference r && r.action != null && $"{r.action.actionMap.name}/{r.action.name}" == path) return r;
                Debug.LogWarning("[FA] Missing input action " + path);
                return null;
            }

            foreach (var side in new[] { "Left", "Right" })
            {
                var ctrl = FindDeep(rig.transform, side + " Controller");
                if (ctrl == null) { Debug.LogError("[FA] No " + side + " Controller in rig"); continue; }
                var sphere = ctrl.gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.045f;
                sphere.center = new Vector3(0, -0.01f, 0.03f);
                sphere.isTrigger = true;
                var rb = ctrl.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                var probe = ctrl.gameObject.AddComponent<HandProbe>();
                probe.isLeft = side == "Left";
                probe.grip = Ref($"XRI {side} Interaction/Select Value");
                probe.trigger = Ref($"XRI {side} Interaction/Activate Value");
                var visual = FindDeep(ctrl, side + " Controller Visual");
                probe.gloveRenderers = visual != null ? visual.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            }

            var settings = rig.AddComponent<RigSettings>();
            settings.cameraOffset = FindDeep(rig.transform, "Camera Offset");
            var vignette = FindDeep(rig.transform, "TunnelingVignette");
            settings.vignette = vignette != null ? vignette.gameObject : null;
            var cam = rig.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 120f;
                cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
            }
            return rig;
        }

        public static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>A flat label in the world made from TextMeshPro (signs, kit labels).</summary>
        public static TextMeshPro Sign(string text, Transform parent, Vector3 pos, Vector3 euler, float size, Color color)
        {
            var go = new GameObject("Sign " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FirstAidSceneBuilder.FontPath);
            if (font != null) t.font = font;
            t.rectTransform.sizeDelta = new Vector2(4, 1);
            return t;
        }

        /// <summary>A world-space TMP canvas on an object (phone screen).</summary>
        public static TextMeshProUGUI Screen(Transform parent, Vector3 pos, Vector3 euler, Vector2 sizePx, float scale, string text, float fontSize)
        {
            var go = new GameObject("Screen", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.localPosition = pos;
            rt.localRotation = Quaternion.Euler(euler);
            rt.localScale = Vector3.one * scale;
            rt.sizeDelta = sizePx;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var tgo = new GameObject("Text", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = tgo.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            t.enableAutoSizing = true;
            t.fontSizeMin = 5;
            t.fontSizeMax = fontSize;
            t.margin = new Vector4(4, 4, 4, 4);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FirstAidSceneBuilder.FontPath);
            if (font != null) t.font = font;
            return t;
        }
    }
}
