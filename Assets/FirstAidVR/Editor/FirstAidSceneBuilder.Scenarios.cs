using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static FirstAidVR.EditorTools.BuilderKit;

namespace FirstAidVR.EditorTools
{
    public static partial class FirstAidSceneBuilder
    {
        /// <summary>A standing adult (parent, friend, paramedic) facing local +Z.</summary>
        static PatientVisuals Standing(string name, Transform parent, Vector3 pos, float yaw, Color shirt, Color trousers, Color skinTone, Color hair)
        {
            var root = Empty(name, parent, pos, new Vector3(0, yaw, 0));
            var vis = root.gameObject.AddComponent<PatientVisuals>();
            var skinM = Mat("Skin " + ColorUtility.ToHtmlStringRGB(skinTone), skinTone, 0.25f);
            var shirtM = Mat("Shirt " + ColorUtility.ToHtmlStringRGB(shirt), shirt, 0.1f);
            var pantsM = Mat("Trousers " + ColorUtility.ToHtmlStringRGB(trousers), trousers, 0.1f);
            var hairM = Mat("Hair " + ColorUtility.ToHtmlStringRGB(hair), hair, 0.2f);
            var dark = Mat("Eye", Theme.Hex("1A1A1A"), 0.6f);
            foreach (var x in new[] { -0.1f, 0.1f })
            {
                Limb("Leg", root, new Vector3(x, 0.08f, 0), new Vector3(x, 0.85f, 0), 0.07f, pantsM);
                Box("Shoe", root, new Vector3(x, 0.05f, 0.04f), new Vector3(0.1f, 0.1f, 0.24f), Mat("Shoe", Theme.Hex("2B2B2B"), 0.3f));
            }
            var chest = Box("Torso", root, new Vector3(0, 1.17f, 0), new Vector3(0.4f, 0.62f, 0.24f), shirtM).transform;
            foreach (var x in new[] { -0.26f, 0.26f })
                Limb("Arm", root, new Vector3(x, 1.42f, 0), new Vector3(x * 1.1f, 0.88f, 0.04f), 0.05f, shirtM);
            var head = Ball("Head", root, new Vector3(0, 1.64f, 0), new Vector3(0.21f, 0.25f, 0.22f), skinM).transform;
            Ball("Hair", head, new Vector3(0, 0.15f, -0.1f), new Vector3(1.04f, 0.8f, 0.95f), hairM);
            Ball("Eye L", head, new Vector3(-0.2f, 0.08f, 0.45f), Vector3.one * 0.12f, dark);
            Ball("Eye R", head, new Vector3(0.2f, 0.08f, 0.45f), Vector3.one * 0.12f, dark);
            vis.chest = chest;
            vis.head = head;
            vis.skin = new[] { head.GetComponent<Renderer>() };
            vis.normalSkin = skinTone;
            vis.breathDepth = 0.01f;
            return vis;
        }

        static void Reparent(Transform root, Transform pivot, params string[] names)
        {
            var list = new List<Transform>();
            foreach (Transform c in root) if (System.Array.IndexOf(names, c.name) >= 0) list.Add(c);
            foreach (var c in list) c.SetParent(pivot, true);
        }

        // ---- Scenario 2: Infant choking --------------------------------------------------------------

        public static void BuildInfantScene()
        {
            var def = LoadDef("InfantChoking");
            var scene = NewScene("InfantChoking");
            Lighting(new Color(0.75f, 0.72f, 0.68f), new Color(0.5f, 0.46f, 0.42f), new Color(0.28f, 0.24f, 0.2f),
                new Color(1f, 0.93f, 0.82f), 0.9f, new Vector3(40, 140, 0), false, Color.black);

            var env = new GameObject("Environment").transform;
            var wood = Mat("Floor Wood", Theme.Hex("B58A5E"), 0.3f);
            var wall = Mat("Wall Warm", Theme.Hex("EFE6DA"), 0.05f);
            var trim = Mat("Wall Trim", Theme.Hex("FFFFFF"), 0.2f);
            Box("Floor", env, new Vector3(0, -0.05f, 0.5f), new Vector3(7, 0.1f, 7), wood, true);
            Box("Ceiling", env, new Vector3(0, 2.7f, 0.5f), new Vector3(7, 0.1f, 7), trim);
            Box("Wall Front", env, new Vector3(0, 1.35f, 3.0f), new Vector3(7, 2.7f, 0.1f), wall, true);
            Box("Wall Back", env, new Vector3(0, 1.35f, -2.0f), new Vector3(7, 2.7f, 0.1f), wall, true);
            Box("Wall Left", env, new Vector3(-3.0f, 1.35f, 0.5f), new Vector3(0.1f, 2.7f, 7), wall, true);
            Box("Wall Right", env, new Vector3(3.0f, 1.35f, 0.5f), new Vector3(0.1f, 2.7f, 7), wall, true);
            Box("Window", env, new Vector3(1.2f, 1.5f, 2.94f), new Vector3(1.4f, 1.0f, 0.02f), Mat("Window Sky", Theme.Hex("BFE3FF"), 0, true));
            Box("Window Frame", env, new Vector3(1.2f, 1.5f, 2.95f), new Vector3(1.5f, 1.1f, 0.02f), trim);
            Box("Door", env, new Vector3(-2.2f, 1.0f, 2.94f), new Vector3(0.9f, 2.0f, 0.04f), Mat("Door", Theme.Hex("8A6545"), 0.2f));
            Box("Rug", env, new Vector3(0, 0.005f, 0.6f), new Vector3(2.6f, 0.01f, 1.8f), Mat("Rug", Theme.Hex("7AA6C2"), 0.05f));
            // Sofa
            var sofa = Empty("Sofa", env, new Vector3(-1.9f, 0, 1.0f), new Vector3(0, 90, 0));
            var fabric = Mat("Sofa Fabric", Theme.Hex("5C6F8F"), 0.05f);
            Box("Seat", sofa, new Vector3(0, 0.25f, 0), new Vector3(1.8f, 0.4f, 0.8f), fabric, true);
            Box("Back", sofa, new Vector3(0, 0.6f, -0.32f), new Vector3(1.8f, 0.5f, 0.2f), fabric, true);
            // Table with the baby seat
            var table = Empty("Table", env, new Vector3(0, 0, 0.8f));
            var tableM = Mat("Table Top", Theme.Hex("D9C3A0"), 0.3f);
            Box("Top", table, new Vector3(0, 0.73f, 0), new Vector3(1.3f, 0.04f, 0.75f), tableM, true);
            foreach (var x in new[] { -0.58f, 0.58f })
            foreach (var z in new[] { -0.3f, 0.3f })
                Box("Leg", table, new Vector3(x, 0.36f, z), new Vector3(0.05f, 0.72f, 0.05f), tableM, true);
            var padM = Mat("Baby Seat", Theme.Hex("F2A7B8"), 0.1f);
            Box("Baby Seat", env, new Vector3(0, 0.77f, 0.78f), new Vector3(0.5f, 0.04f, 0.3f), padM);
            Box("Baby Seat Back", env, new Vector3(-0.27f, 0.82f, 0.78f), new Vector3(0.06f, 0.12f, 0.3f), padM);
            Ball("Toy", env, new Vector3(0.35f, 0.79f, 0.95f), Vector3.one * 0.08f, Mat("Toy Yellow", Theme.Hex("F5C542"), 0.4f));
            Box("Snack Bowl", env, new Vector3(-0.45f, 0.77f, 0.95f), new Vector3(0.12f, 0.04f, 0.12f), Mat("Bowl", Theme.Hex("6FCF97"), 0.4f));

            var (ctrl, ui) = ScenarioCore(def, new Vector3(-0.95f, 1.45f, 0.75f));
            Rig(new Vector3(0, 1.35f, -0.05f), 0f);

            // ---- The infant (grabbable). Local: head toward -X, face up (+Y). ----
            var restPos = new Vector3(0.02f, 0.84f, 0.78f);
            var infant = Item("Infant", "infant", env, restPos, new Vector3(0.46f, 0.12f, 0.2f), new Vector3(-0.13f, 0.035f, 0f), Vector3.zero, 0.8f);
            infant.grip.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.down);
            var box = infant.GetComponent<BoxCollider>();
            box.center = new Vector3(-0.05f, 0, 0);
            var infantRb = infant.GetComponent<Rigidbody>();
            infantRb.isKinematic = true;
            infantRb.useGravity = false;
            var ivis = infant.gameObject.AddComponent<PatientVisuals>();
            var babySkin = new Color(0.93f, 0.76f, 0.64f);
            var skinM = Mat("Skin Baby", babySkin, 0.3f);
            var onesie = Mat("Onesie", Theme.Hex("FFE08A"), 0.1f);
            var dark = Mat("Eye", Theme.Hex("1A1A1A"), 0.6f);
            var it = infant.transform;
            var chest = Box("Torso", it, new Vector3(0.0f, 0f, 0), new Vector3(0.2f, 0.1f, 0.15f), onesie).transform;
            var head = Ball("Head", it, new Vector3(-0.17f, 0.01f, 0), new Vector3(0.14f, 0.13f, 0.13f), skinM).transform;
            Ball("Hair", head, new Vector3(-0.2f, 0.15f, 0), new Vector3(0.75f, 0.7f, 0.95f), Mat("Hair Baby", Theme.Hex("8A5A3B"), 0.2f));
            var e1 = Ball("Eye L", head, new Vector3(0.12f, 0.42f, 0.2f), Vector3.one * 0.12f, dark).transform;
            var e2 = Ball("Eye R", head, new Vector3(0.12f, 0.42f, -0.2f), Vector3.one * 0.12f, dark).transform;
            Ball("Mouth", head, new Vector3(0.32f, 0.38f, 0), new Vector3(0.18f, 0.08f, 0.22f), Mat("Mouth", Theme.Hex("8E3B46"), 0.4f));
            foreach (var z in new[] { -1f, 1f })
            {
                Limb("Arm", it, new Vector3(-0.07f, 0.0f, z * 0.085f), new Vector3(0.0f, 0.02f, z * 0.11f), 0.018f, onesie);
                Ball("Hand", it, new Vector3(0.02f, 0.025f, z * 0.115f), Vector3.one * 0.03f, skinM);
                Limb("Leg", it, new Vector3(0.1f, 0f, z * 0.042f), new Vector3(0.2f, 0.0f, z * 0.048f), 0.022f, onesie);
                Ball("Foot", it, new Vector3(0.225f, 0.012f, z * 0.048f), new Vector3(0.035f, 0.03f, 0.03f), skinM);
            }
            ivis.chest = chest;
            ivis.head = head;
            ivis.skin = new[] { head.GetComponent<Renderer>() };
            ivis.eyes = new[] { e1, e2 };
            ivis.normalSkin = babySkin;
            ivis.paleSkin = new Color(0.72f, 0.74f, 0.88f); // bluish
            ivis.breathRate = 0f;
            ivis.breathDepth = 0.06f;

            // Zones that move with the infant
            var check = Zone<TouchZone>("Check Zone", it, new Vector3(-0.05f, 0.08f, 0), new Vector3(0.42f, 0.22f, 0.24f));
            check.actionId = "check_infant";
            var back = Zone<StrikeZone>("Back Blow Zone", it, new Vector3(-0.05f, -0.05f, 0), new Vector3(0.14f, 0.16f, 0.16f), new Vector3(180, 0, 0));
            back.GetComponent<BoxCollider>().center = new Vector3(0, 0.05f, 0);
            var chestZone = Zone<CompressionZone>("Chest Thrust Zone", it, new Vector3(-0.01f, 0.05f, 0), new Vector3(0.09f, 0.16f, 0.12f));
            chestZone.depth = 0.025f;
            var belly = Zone<CompressionZone>("Abdomen Zone", it, new Vector3(0.09f, 0.05f, 0), new Vector3(0.07f, 0.14f, 0.12f));
            belly.depth = 0.025f;
            var mouth = Zone<TouchZone>("Mouth Zone", it, new Vector3(-0.17f, 0.07f, 0), new Vector3(0.06f, 0.06f, 0.06f));
            var breath = Zone<TouchZone>("Breathing Zone", it, new Vector3(0.0f, 0.08f, 0), new Vector3(0.16f, 0.12f, 0.14f));
            var food = Ball("Food Piece", it, new Vector3(-0.15f, 0.07f, 0), Vector3.one * 0.022f, Mat("Food", Theme.Hex("C9E265"), 0.4f), true);

            var rest = Empty("Infant Rest", env, restPos);
            var parentNpc = Standing("Parent", env, new Vector3(1.05f, 0, 1.45f), 210f, Theme.Hex("9B59B6"), Theme.Hex("34495E"), new Color(0.62f, 0.45f, 0.33f), Theme.Hex("1E1410"));
            var phone = Phone(env, new Vector3(0.45f, 0.76f, 0.6f), 160f);

            // Paramedics come through the door
            var emsRoot = new GameObject("EMS").transform;
            var ems = emsRoot.gameObject.AddComponent<EmsArrival>();
            var medics = new GameObject("Paramedics").transform;
            medics.SetParent(emsRoot, false);
            Standing("Paramedic A", medics, new Vector3(-2.0f, 0, 2.3f), 160f, Theme.Hex("1F5FA8"), Theme.Hex("1B2B44"), new Color(0.85f, 0.68f, 0.55f), Theme.Hex("3B2A20"));
            Standing("Paramedic B", medics, new Vector3(-2.5f, 0, 2.0f), 140f, Theme.Hex("1F5FA8"), Theme.Hex("1B2B44"), new Color(0.48f, 0.33f, 0.24f), Theme.Hex("111111"));
            ems.paramedics = medics.gameObject;
            ems.driveSeconds = 4f;

            var script = ctrl.gameObject.AddComponent<InfantChokingScenario>();
            script.controller = ctrl;
            script.ui = ui;
            script.patient = ivis;
            script.phone = phone;
            script.ems = ems;
            script.infant = infant;
            script.restPoint = rest;
            script.parentNpc = parentNpc;
            script.parentTalkAnchor = Empty("Parent Talk Anchor", env, new Vector3(1.0f, 1.2f, 1.0f));
            script.parentTalkAnchor.rotation = Quaternion.LookRotation(new Vector3(1.0f, 0, 1.0f).normalized);
            script.checkTouch = check;
            script.backZone = back;
            script.chestZone = chestZone;
            script.bellyZone = belly;
            script.mouthZone = mouth;
            script.breathZone = breath;
            script.foodPiece = food;
            Save(scene, "InfantChoking");
        }

        // ---- Scenario 3: Shock ------------------------------------------------------------------------

        public static void BuildShockScene()
        {
            var def = LoadDef("Shock");
            var scene = NewScene("Shock");
            Lighting(new Color(0.7f, 0.74f, 0.82f), new Color(0.56f, 0.55f, 0.5f), new Color(0.3f, 0.28f, 0.22f),
                new Color(1f, 0.9f, 0.75f), 1.05f, new Vector3(32, -60, 0), true, new Color(0.8f, 0.82f, 0.85f));
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");

            var env = new GameObject("Environment").transform;
            Box("Ground", env, new Vector3(0, -0.05f, 0), new Vector3(60, 0.1f, 60), Mat("Grass Dry", Theme.Hex("8DA55A"), 0.05f), true);
            Box("Gravel", env, new Vector3(0, 0.004f, 1.0f), new Vector3(7, 0.01f, 6), Mat("Gravel", Theme.Hex("B4A68E"), 0.05f), true);
            var rng = new System.Random(11);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = 6.5f + (float)rng.NextDouble() * 15f;
                Tree(env, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), 4f + (float)rng.NextDouble() * 3f, rng);
            }
            // Picnic table
            var pt = Empty("Picnic Table", env, new Vector3(0.2f, 0, 2.6f));
            var wood = Mat("Bench Wood", Theme.Hex("A9744A"), 0.15f);
            Box("Top", pt, new Vector3(0, 0.75f, 0), new Vector3(1.8f, 0.05f, 0.8f), wood, true);
            Box("Seat A", pt, new Vector3(0, 0.45f, -0.6f), new Vector3(1.8f, 0.05f, 0.3f), wood, true);
            Box("Seat B", pt, new Vector3(0, 0.45f, 0.6f), new Vector3(1.8f, 0.05f, 0.3f), wood, true);
            foreach (var x in new[] { -0.7f, 0.7f }) Box("Leg", pt, new Vector3(x, 0.37f, 0), new Vector3(0.08f, 0.74f, 1.4f), wood, true);
            // Trail sign
            var sign = Empty("Trail Sign", env, new Vector3(-2.2f, 0, 2.4f), new Vector3(0, 20, 0));
            Box("Post", sign, new Vector3(0, 0.8f, 0), new Vector3(0.1f, 1.6f, 0.1f), wood);
            Box("Board", sign, new Vector3(0, 1.5f, -0.06f), new Vector3(0.9f, 0.5f, 0.04f), Mat("Sign Green", Theme.Hex("2E6B3F"), 0.2f));
            var st = Sign("PINE RIDGE TRAIL", sign, new Vector3(0, 1.5f, -0.09f), Vector3.zero, 1.6f, Color.white);
            st.rectTransform.sizeDelta = new Vector2(0.85f, 0.4f);
            // Supply table (stone wall) on the right
            var wall = Empty("Supply Wall", env, new Vector3(1.0f, 0, 0.45f), new Vector3(0, -90, 0));
            Box("Stone Wall", wall, new Vector3(0, 0.33f, 0), new Vector3(1.4f, 0.66f, 0.45f), Mat("Stone", Theme.Hex("8C8C86"), 0.1f), true);

            var (ctrl, ui) = ScenarioCore(def, new Vector3(-0.95f, 1.4f, 0.9f));
            Rig(new Vector3(0, 1.35f, 0), 0f);

            // ---- Patient sitting on the ground (can be helped to lie back) ----
            var (patient, vis, handR, forearmMid) = AdultLying(env, new Vector3(0.1f, 0, 1.3f),
                Theme.Hex("3C8D6E"), Theme.Hex("6B5B4B"), new Color(0.88f, 0.72f, 0.6f), Theme.Hex("C79A5B"));
            var upper = Empty("Upper Body", patient, new Vector3(0.0f, 0.13f, 0));
            Reparent(patient, upper, "Chest", "Belly", "Head", "Neck", "Upper Arm L", "Forearm L", "Hand L", "Upper Arm R", "Forearm R", "Hand R", "Forearm R Mid");
            var legs = Empty("Legs", patient, new Vector3(0.06f, 0.1f, 0));
            Reparent(patient, legs, "Thigh", "Shin", "Shoe");
            Box("Hiking Pack", env, new Vector3(-0.55f, 0.18f, 1.75f), new Vector3(0.3f, 0.36f, 0.2f), Mat("Pack Orange", Theme.Hex("E07B39"), 0.2f), true);

            var shoulderTouch = Zone<TouchZone>("Patient Shoulder", upper, new Vector3(-0.55f, 0.08f, -0.2f), new Vector3(0.24f, 0.3f, 0.26f));
            shoulderTouch.actionId = "check_patient";
            var handle = Empty("Shoulder Handle", upper, new Vector3(-0.55f, 0.12f, -0.16f));
            var hc = handle.gameObject.AddComponent<SphereCollider>();
            hc.radius = 0.09f;
            var simple = handle.gameObject.AddComponent<XRSimpleInteractable>();
            simple.colliders.Add(hc);
            var lowering = handle.gameObject.AddComponent<GuidedLowering>();
            lowering.upperBody = upper;
            lowering.sittingEuler = new Vector3(0, 0, -75f);
            lowering.lyingEuler = Vector3.zero;
            lowering.handTravel = 0.35f;
            upper.localRotation = Quaternion.Euler(lowering.sittingEuler);

            var blanketZone = Zone<PlacementZone>("Blanket Zone", patient, new Vector3(0.0f, 0.35f, 0.0f), new Vector3(1.4f, 0.7f, 0.7f));
            blanketZone.acceptedItems = new List<string> { "blanket" };
            blanketZone.actionIds = new List<string> { "keep_warm" };
            blanketZone.anchor = Empty("Blanket Anchor", patient, new Vector3(0.05f, 0.2f, 0.02f));
            blanketZone.holdToPlace = 0.5f;
            var feet = Zone<PlacementZone>("Feet Zone", patient, new Vector3(0.95f, 0.2f, 0), new Vector3(0.35f, 0.4f, 0.4f));
            feet.acceptedItems = new List<string> { "backpack" };
            feet.actionIds = new List<string> { "raise_legs" };
            feet.anchor = Empty("Feet Anchor", patient, new Vector3(0.92f, 0.0f, 0));
            feet.priority = 3;
            var mouthZone = Zone<PlacementZone>("Mouth Zone", upper, new Vector3(-0.78f, 0.15f, 0), new Vector3(0.25f, 0.3f, 0.3f));
            mouthZone.wrongItemAction = "gave_drink";
            mouthZone.wrongItems = new List<string> { "water" };
            mouthZone.holdToPlace = 0.6f;
            var breath = Zone<TouchZone>("Breathing Zone", upper, new Vector3(-0.42f, 0.2f, 0), new Vector3(0.3f, 0.3f, 0.36f));

            var friend = Standing("Friend", env, new Vector3(-0.9f, 0, 2.1f), 160f, Theme.Hex("D35400"), Theme.Hex("2C3E50"), new Color(0.55f, 0.4f, 0.3f), Theme.Hex("2A1C14"));

            // Supplies on the wall
            var equip = new GameObject("Equipment").transform;
            FirstAidKit(equip, new Vector3(1.0f, 0.66f, 0.15f), -90f);
            var blanket = Blanket(equip, "Blanket", new Vector3(1.0f, 0.69f, 0.5f));
            var unfold = blanket.gameObject.AddComponent<UnfoldOnPlace>();
            unfold.unfoldedScale = new Vector3(7f, 0.6f, 4.2f);
            var phone = Phone(equip, new Vector3(0.9f, 0.675f, 0.75f), 180f);
            var water = Item("Water Bottle", "water", equip, new Vector3(1.08f, 0.76f, 0.82f), new Vector3(0.07f, 0.2f, 0.07f), new Vector3(0, 0, -0.04f), new Vector3(0, 0, 0), 0.4f);
            Cyl("Bottle", water.transform, Vector3.zero, new Vector3(0.065f, 0.09f, 0.065f), Mat("Bottle Plastic", new Color(0.6f, 0.8f, 1f, 0.7f), 0.8f));
            Cyl("Cap", water.transform, new Vector3(0, 0.1f, 0), new Vector3(0.03f, 0.015f, 0.03f), Mat("Cap Blue", Theme.Blue, 0.4f));
            var pack = Item("Backpack", "backpack", env, new Vector3(0.75f, 0.12f, 0.75f), new Vector3(0.32f, 0.22f, 0.2f), new Vector3(0, 0.14f, 0), Vector3.zero, 1.0f);
            Box("Pack", pack.transform, Vector3.zero, new Vector3(0.32f, 0.22f, 0.2f), Mat("Pack Blue", Theme.Hex("2D6CDF"), 0.2f));
            Box("Pocket", pack.transform, new Vector3(0, -0.02f, -0.11f), new Vector3(0.22f, 0.12f, 0.03f), Mat("Pack Dark", Theme.Hex("1E4BA0"), 0.2f));

            Ambulance(env, new Vector3(-26f, 0, 5.5f), new Vector3(-4.5f, 0, 5.5f), out var ems);
            ems.vehicle.localRotation = Quaternion.Euler(0, 90, 0);

            var script = ctrl.gameObject.AddComponent<ShockScenario>();
            script.controller = ctrl;
            script.ui = ui;
            script.patient = vis;
            script.phone = phone;
            script.ems = ems;
            script.talkAnchor = Empty("Talk Anchor", ctrl.transform, new Vector3(0.55f, 1.05f, 1.95f));
            script.talkAnchor.rotation = Quaternion.LookRotation(new Vector3(0.55f, 0, 1.95f).normalized);
            script.shoulderTouch = shoulderTouch;
            script.lowering = lowering;
            script.feetZone = feet;
            script.legs = legs;
            script.blanketZone = blanketZone;
            script.mouthZone = mouthZone;
            script.breathZone = breath;
            script.friend = friend;
            Save(scene, "Shock");
        }
    }
}
