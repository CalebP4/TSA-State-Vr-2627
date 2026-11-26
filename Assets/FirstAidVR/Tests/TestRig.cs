using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FirstAidVR.Tests
{
    /// <summary>
    /// Drives the real XR rig without a headset: moves the controllers, grabs with the
    /// rig's own Near-Far interactors (XRI manual interaction), and takes screenshots.
    /// </summary>
    public static class TestRig
    {
        public static string ShotDir = "Logs/Shots";
        static int shotIndex;

        public static IEnumerator Load(string scenarioId, string scene, SimMode mode)
        {
            ResultsStore.FileName = "firstaid_results_tests.json";
            EventLogger.EchoToConsole = true;
            if (scenarioId != null) SimulationManager.Select(scenarioId, mode);
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            FreezeTracking();
            yield return null;
        }

        /// <summary>No headset in tests: stop tracked pose drivers so code can place the head and hands.</summary>
        public static void FreezeTracking()
        {
            // The rig hides controllers that are not tracked; in tests we show them and drive them.
            foreach (var m in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>(FindObjectsSortMode.None))
            {
                m.enabled = false;
                if (m.leftController != null) m.leftController.SetActive(true);
                if (m.rightController != null) m.rightController.SetActive(true);
            }
            foreach (var tpd in UnityEngine.Object.FindObjectsByType<UnityEngine.InputSystem.XR.TrackedPoseDriver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                tpd.enabled = false;
            Assert.IsNotNull(Probe(true), "left hand probe");
            Assert.IsNotNull(Probe(false), "right hand probe");
            var cam = Camera.main;
            Assert.IsNotNull(cam, "Main camera");
            cam.transform.SetPositionAndRotation(new Vector3(0, 1.6f, -0.1f), Quaternion.Euler(25, 0, 0));
            Hand(true).SetPositionAndRotation(new Vector3(-0.25f, 1.1f, 0.25f), Quaternion.identity);
            Hand(false).SetPositionAndRotation(new Vector3(0.25f, 1.1f, 0.25f), Quaternion.identity);
        }

        public static HandProbe Probe(bool left) => HandProbe.Get(left);
        public static Transform Hand(bool left) => Probe(left).transform;

        public static NearFarInteractor Interactor(bool left) => Probe(left).GetComponentInChildren<NearFarInteractor>();

        public static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        public static IEnumerator Until(Func<bool> cond, float timeout, string what)
        {
            float end = Time.time + timeout;
            while (!cond())
            {
                if (Time.time > end) Assert.Fail("Timed out waiting for: " + what);
                yield return null;
            }
        }

        /// <summary>Glide a hand to a world position over a few frames (so triggers fire).</summary>
        public static IEnumerator MoveHand(bool left, Vector3 to, float seconds = 0.25f, Quaternion? rot = null)
        {
            var h = Hand(left);
            Vector3 from = h.position;
            Quaternion r0 = h.rotation, r1 = rot ?? h.rotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / seconds)
            {
                h.SetPositionAndRotation(Vector3.Lerp(from, to, t), Quaternion.Slerp(r0, r1, t));
                yield return null;
            }
            h.SetPositionAndRotation(to, r1);
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        /// <summary>Teleport a hand (one frame) - used for fast strikes.</summary>
        public static IEnumerator Snap(bool left, Vector3 to)
        {
            Hand(left).position = to;
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        public static IEnumerator Grab(bool left, FirstAidItem item)
        {
            yield return MoveHand(left, item.transform.position + Vector3.up * 0.05f, 0.15f);
            Interactor(left).StartManualInteraction((IXRSelectInteractable)item.Grab);
            yield return Until(() => item.IsHeld, 2f, "grab " + item.name);
            yield return null;
        }

        public static IEnumerator GrabSimple(bool left, XRSimpleInteractable target)
        {
            yield return MoveHand(left, target.transform.position, 0.15f);
            Interactor(left).StartManualInteraction((IXRSelectInteractable)target);
            yield return Until(() => target.isSelected, 2f, "select " + target.name);
        }

        public static IEnumerator Release(bool left)
        {
            var i = Interactor(left);
            if (i.isPerformingManualInteraction) i.EndManualInteraction();
            yield return null;
            yield return null;
        }

        /// <summary>Carry the held item to a zone and wait for it to snap in.</summary>
        public static IEnumerator PlaceIn(bool left, FirstAidItem item, PlacementZone zone)
        {
            yield return MoveHand(left, zone.transform.position + Vector3.up * 0.02f, 0.3f);
            yield return Until(() => item.PlacedIn == zone, 3f, $"{item.name} placed in {zone.name}");
            yield return Release(left);
        }

        public static T Find<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, "Scene object " + name);
            var c = go.GetComponent<T>();
            Assert.IsNotNull(c, $"{typeof(T).Name} on {name}");
            return c;
        }

        public static IEnumerator ScanScene()
        {
            var cam = Camera.main.transform;
            for (float a = -80; a <= 80; a += 4)
            {
                cam.rotation = Quaternion.Euler(10, a, 0);
                yield return null;
            }
            cam.rotation = Quaternion.Euler(25, 0, 0);
            yield return null;
        }

        public static IEnumerator AnswerCorrect()
        {
            yield return Until(() => ChoicePrompt.Open != null, 5f, "choice prompt");
            yield return Wait(0.3f);
            ChoicePrompt.Open.PickCorrect();
            yield return null;
        }

        public static IEnumerator Use(FirstAidItem item)
        {
            item.Grab.activated.Invoke(new UnityEngine.XR.Interaction.Toolkit.ActivateEventArgs());
            yield return null;
        }

        public static bool CanShoot => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        /// <summary>Saves what the headset camera sees (world-space UI included) to Logs/Shots.</summary>
        public static void Shot(string name, Vector3? camPos = null, Quaternion? camRot = null)
        {
            if (!CanShoot) return;
            var cam = Camera.main;
            var p = cam.transform.position; var r = cam.transform.rotation;
            if (camPos.HasValue) cam.transform.position = camPos.Value;
            if (camRot.HasValue) cam.transform.rotation = camRot.Value;
            float fov = cam.fieldOfView;
            cam.fieldOfView = 75f;
            var rt = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.aspect = 1280f / 800f;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = prevTarget;
            cam.fieldOfView = fov;
            cam.ResetAspect();
            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes($"{ShotDir}/{shotIndex++:00}_{name}.png", tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            cam.transform.SetPositionAndRotation(p, r);
        }

        public static void LookAt(Vector3 target, Vector3 from)
        {
            Camera.main.transform.SetPositionAndRotation(from, Quaternion.LookRotation(target - from));
        }
    }
}
