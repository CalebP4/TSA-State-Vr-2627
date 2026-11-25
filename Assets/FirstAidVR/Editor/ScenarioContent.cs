using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR.EditorTools
{
    /// <summary>
    /// Default teaching content for the three scenarios. The scene builder writes these
    /// into Assets/FirstAidVR/Data/*.asset the first time (or when you choose
    /// "Reset Scenario Content"). After that, edit the assets in the Inspector.
    ///
    /// Sources (checked November 2026):
    ///  - AHA / American Red Cross 2024 First Aid Guidelines
    ///  - AHA / AAP 2025 Pediatric Basic Life Support guidelines (infant choking)
    ///  - American Red Cross "Learn First Aid" pages: life-threatening bleeding, infant choking, shock
    ///  - Mayo Clinic first-aid pages: severe bleeding, shock
    /// </summary>
    public static class ScenarioContent
    {
        static StepDefinition S(string id, string title, string instruction, string feedback, int points, bool critical = true,
            string highlight = null, string[] after = null, string[] requires = null, int count = 1, bool optional = false)
        {
            return new StepDefinition
            {
                id = id, title = title, instruction = instruction, feedback = feedback, points = points, critical = critical,
                highlightTarget = highlight, after = new List<string>(after ?? new string[0]),
                requires = new List<string>(requires ?? new string[0]), requiredCount = count, optional = optional,
            };
        }

        static MistakeDefinition M(string id, string title, string correction, int penalty, bool critical = false) =>
            new MistakeDefinition { id = id, title = title, correction = correction, penalty = penalty, critical = critical };

        // ------------------------------------------------------------------------------------

        public static void FillBleeding(ScenarioDefinition d)
        {
            d.id = "bleeding";
            d.title = "Severe Bleeding";
            d.subtitle = "A cyclist in the park has a deep cut on his forearm from broken glass. Control life-threatening bleeding until help arrives.";
            d.sceneName = "Bleeding";
            d.accent = new Color(0.90f, 0.28f, 0.30f);
            d.introduction = "You are walking through a park when you hear someone shout for help. A cyclist has fallen onto a broken bottle. " +
                             "He is lying on the grass and his forearm is bleeding heavily.\n\nThere is a first-aid kit on the bench next to you, and your phone.";
            d.targetTime = 150f;
            d.certificationTimeLimit = 300f;
            d.sources = "American Red Cross (life-threatening bleeding), AHA/Red Cross 2024 First Aid Guidelines, Mayo Clinic.";
            d.steps = new List<StepDefinition>
            {
                S("scene_safe", "Check the scene is safe",
                  "Before you help, look around for danger such as traffic, broken glass or other hazards. Turn your head to scan left and right.",
                  "The scene is safe. Now check on the person.", 10),
                S("check_patient", "Check the person",
                  "Walk up to the person, tap his shoulder with your hand and ask if he is okay.",
                  "He is awake and can talk to you.", 10, highlight: "Patient Shoulder", after: new[] { "scene_safe" }),
                S("recognize", "Recognise life-threatening bleeding",
                  "Look at the wound and decide how serious it is.",
                  "Steady, pooling blood is life-threatening. Act fast.", 10, requires: new[] { "check_patient" }),
                S("call_911", "Call 911",
                  "Pick up the phone from the bench and pull the TRIGGER to call 911 on speaker.",
                  "Help is on the way. Keep the phone on speaker.", 15, highlight: "Phone", after: new[] { "scene_safe" }),
                S("gloves", "Put on gloves",
                  "Grab the blue gloves from the first-aid kit and hold them for a moment to put them on. Gloves protect you and the patient.",
                  "Gloves on. (If you ever have no gloves, do not delay care for life-threatening bleeding.)", 5, critical: false,
                  highlight: "Gloves", after: new[] { "scene_safe" }),
                S("apply_dressing", "Put a dressing on the wound",
                  "Pick up a sterile dressing from the kit and place it directly on the wound on his forearm.",
                  "The dressing is on the wound.", 15, highlight: "Dressing A", after: new[] { "call_911", "gloves" }),
                S("apply_pressure", "Press firmly and continuously",
                  "Put your hand on the dressing and hold GRIP (or TRIGGER) to press down hard. Keep steady pressure - do not let go or lift to check.",
                  "Firm, continuous pressure has controlled the bleeding.", 20, highlight: "Pressure Zone", requires: new[] { "apply_dressing" }),
                S("soak_decision", "If blood soaks through, don't remove the dressing",
                  "Blood is soaking through. Decide what to do.",
                  "Correct - leave the first dressing in place.", 10, requires: new[] { "apply_dressing" }),
                S("second_dressing", "Add one more dressing on top",
                  "Place the second dressing on top of the first one, then keep pressing.",
                  "Second dressing on top. Keep pressing.", 10, critical: false, highlight: "Dressing B", requires: new[] { "apply_dressing" }),
                S("bandage", "Bandage over the dressings",
                  "The bleeding has stopped. Pick up the roller bandage and place it on the dressings to wrap them firmly.",
                  "The dressings are held firmly in place.", 10, critical: false, highlight: "Roller Bandage", requires: new[] { "apply_pressure" }),
                S("check_circulation", "Check circulation past the bandage",
                  "Touch his fingers to check they are warm and he can move them. A bandage that is too tight cuts off blood flow.",
                  "Fingers are warm - the bandage is not too tight.", 5, critical: false, highlight: "Patient Fingers", requires: new[] { "bandage" }),
                S("keep_warm", "Keep him warm",
                  "Blood loss can lead to shock. Cover him with the emergency blanket from the kit to stop him getting cold.",
                  "Keeping him warm helps against shock.", 10, critical: false, highlight: "Emergency Blanket", after: new[] { "apply_dressing" }),
                S("reassure", "Reassure him", "Talk to him calmly and tell him help is coming.", "Calm words help a frightened patient.", 5,
                  critical: false, optional: true, highlight: "Talk Menu"),
            };
            d.mistakes = new List<MistakeDefinition>
            {
                M("underestimated", "Underestimated the bleeding", "Blood that flows steadily or pools on the ground is life-threatening: call 911 and press on it immediately.", 15),
                M("removed_dressing", "Removed a soaked dressing", "Never remove the first dressing - it pulls away the clot. Add a dressing on top and keep pressing.", 15),
                M("peeked", "Lifted the dressing to check", "Keep pressure on continuously until the bleeding stops; lifting to look lets it restart.", 10),
                M("pressure_released", "Let go of the pressure early", "Keep firm, continuous pressure until the bleeding has stopped.", 5),
                M("dressing_wrong_place", "Dressing in the wrong place", "Place the dressing directly on the wound itself.", 5),
                M("moved_patient", "Asked him to get up", "Keep a seriously injured person still and lying down while you wait for EMS.", 10),
            };
            d.keyPoints = new List<string>
            {
                "Check the scene is safe, then call 911 (or have someone call).",
                "Put on gloves if you have them - but do not delay care for life-threatening bleeding.",
                "Apply a dressing and press firmly and continuously.",
                "If blood soaks through, do not remove the dressing: add one on top and keep pressing.",
                "If direct pressure cannot control bleeding from an arm or leg, a tourniquet is used 2-3 inches above the wound (training required).",
            };
        }

        // ------------------------------------------------------------------------------------

        public static void FillInfant(ScenarioDefinition d)
        {
            d.id = "infant";
            d.title = "Infant Choking";
            d.subtitle = "A 9-month-old baby is choking on a piece of food and cannot cry or cough. Give back blows and chest thrusts.";
            d.sceneName = "InfantChoking";
            d.accent = new Color(0.20f, 0.55f, 0.95f);
            d.introduction = "You are visiting a friend. Their 9-month-old baby was eating small pieces of food on the play mat. " +
                             "Suddenly the baby goes quiet, looks panicked and cannot cry.\n\nThe parent is frozen with fear. A phone is on the table.";
            d.targetTime = 120f;
            d.certificationTimeLimit = 240f;
            d.sources = "AHA/AAP 2025 Pediatric Basic Life Support guidelines, American Red Cross (infant choking).";
            d.steps = new List<StepDefinition>
            {
                S("scene_safe", "Check the scene is safe", "Look around the room for any danger before you help. Turn your head to scan left and right.",
                  "The room is safe. Check the baby.", 10),
                S("check_infant", "Check the baby", "Kneel by the baby on the play mat and touch the baby to check them.",
                  "You are with the baby.", 5, highlight: "Infant", after: new[] { "scene_safe" }),
                S("recognize", "Recognise severe choking", "Look at the signs and decide what is happening.",
                  "No cry, no cough, no breathing: this is severe choking.", 10, requires: new[] { "check_infant" }),
                S("call_911", "Have someone call 911", "Use the phone on the table: pull the TRIGGER to call 911 on speaker so your hands stay free.",
                  "Help is on the way.", 15, highlight: "Phone", after: new[] { "recognize" }),
                S("position_down", "Hold the baby face-down, head lower than chest",
                  "Pick up the baby (GRIP). The baby lies face-down along your forearm. Tilt your hand so the baby's head is lower than the chest.",
                  "Good support: face-down, head lower than the chest.", 10, highlight: "Infant", requires: new[] { "check_infant" }),
                S("back_blows_1", "Give 5 back blows",
                  "With your other hand, give 5 firm back blows with the heel of your hand between the shoulder blades. Swing your hand down onto the baby's back.",
                  "5 back blows done.", 15, highlight: "Back Blow Zone", requires: new[] { "position_down" }, count: 5),
                S("chest_thrusts_1", "Turn over and give 5 chest thrusts",
                  "Turn your hand over so the baby is face-up, head still lower than the chest. Push the heel of your other hand into the centre of the chest, just below the nipple line, 5 times.",
                  "5 chest thrusts done.", 15, highlight: "Chest Thrust Zone", requires: new[] { "back_blows_1" }, count: 5),
                S("back_blows_2", "Repeat: 5 more back blows",
                  "The object is still stuck. Turn the baby face-down again and repeat the 5 back blows.",
                  "The object is out!", 10, highlight: "Back Blow Zone", requires: new[] { "chest_thrusts_1" }, count: 5),
                S("monitor", "Check the baby is breathing",
                  "The baby is crying. Rest your hand on the baby's chest for a moment to check breathing, and stay with the baby until EMS arrives.",
                  "The baby is breathing and crying. Keep watching until help arrives.", 10, highlight: "Breathing Zone", requires: new[] { "back_blows_2" }),
            };
            d.mistakes = new List<MistakeDefinition>
            {
                M("finger_sweep", "Blind finger sweep", "Never sweep your finger in a baby's mouth unless you can see the object - it can push it deeper.", 20, true),
                M("abdominal_thrust", "Abdominal thrusts on an infant", "Do not use abdominal thrusts on babies under 1 year. Use back blows and chest thrusts.", 20, true),
                M("unsupported", "Left the baby unsupported", "Keep holding and supporting the baby's head and body throughout.", 10),
                M("underestimated", "Misjudged the choking", "A baby who cannot cry, cough or breathe has a severe blockage: act immediately.", 15),
                M("waited", "Waited for it to pass", "Severe choking will not clear on its own. Start back blows straight away.", 10),
            };
            d.keyPoints = new List<string>
            {
                "Signs of severe choking: cannot cry, cough or breathe; may turn bluish.",
                "Have someone call 911 (use speaker if you are alone).",
                "5 back blows face-down along your forearm, head lower than chest, jaw supported.",
                "5 chest thrusts face-up with the heel of one hand on the centre of the chest (AHA 2025).",
                "Repeat until the object comes out or the baby becomes unresponsive (then start infant CPR).",
                "No blind finger sweeps and no abdominal thrusts for infants.",
            };
        }

        // ------------------------------------------------------------------------------------

        public static void FillShock(ScenarioDefinition d)
        {
            d.id = "shock";
            d.title = "Shock";
            d.subtitle = "A hiker who has been vomiting all day is pale, cold and clammy. Recognise shock and care for him until help arrives.";
            d.sceneName = "Shock";
            d.accent = new Color(0.96f, 0.65f, 0.14f);
            d.introduction = "At a trailhead rest area, a hiker is slumped against a picnic bench. His friend says he has been vomiting and has had diarrhoea all day " +
                             "and couldn't keep any water down.\n\nHe has no injuries from a fall. There is a first-aid kit with a blanket on the table.";
            d.targetTime = 130f;
            d.certificationTimeLimit = 270f;
            d.sources = "AHA/Red Cross 2024 First Aid Guidelines (shock), American Red Cross (shock), Mayo Clinic.";
            d.steps = new List<StepDefinition>
            {
                S("scene_safe", "Check the scene is safe", "Look around for any danger before you help. Turn your head to scan left and right.",
                  "The area is safe.", 10),
                S("check_patient", "Check the person", "Go to the hiker, tap his shoulder and ask how he feels.",
                  "He is awake but weak.", 10, highlight: "Patient Shoulder", after: new[] { "scene_safe" }),
                S("recognize", "Recognise the signs of shock", "Look at the signs and decide what is happening.",
                  "Pale, cool, clammy skin, fast breathing and weakness after fluid loss are signs of shock.", 10, requires: new[] { "check_patient" }),
                S("call_911", "Call 911", "Shock is a medical emergency. Pick up the phone and pull the TRIGGER to call 911.",
                  "Help is on the way.", 15, highlight: "Phone", after: new[] { "check_patient" }),
                S("lay_down", "Help him lie flat on his back",
                  "Grab his shoulder (GRIP) and gently move your hand down and away to help him lie down on his back.",
                  "Lying flat on his back helps blood flow.", 15, highlight: "Patient Shoulder", requires: new[] { "check_patient" }),
                S("raise_legs", "Raise his feet (no injury)",
                  "Because he has no injury, you may raise his feet about 6-12 inches. Place the backpack under his feet. (Do not raise the legs if there is an injury.)",
                  "Feet raised. Lower them if it causes pain or makes him feel worse.", 5, critical: false, optional: true,
                  highlight: "Backpack", requires: new[] { "lay_down" }),
                S("keep_warm", "Keep him warm", "Cover him with the blanket from the first-aid kit to prevent heat loss.",
                  "The blanket stops him losing body heat.", 15, highlight: "Blanket", after: new[] { "lay_down" }),
                S("reassure", "Reassure him", "Talk to him calmly. Anxiety makes shock worse.", "Calm reassurance helps.", 10, critical: false,
                  highlight: "Talk Menu", after: new[] { "check_patient" }),
                S("monitor", "Monitor his breathing", "Rest your hand on his chest for a few seconds to check his breathing. Keep watching him until EMS arrives.",
                  "Breathing is fast but steady. Keep monitoring.", 10, highlight: "Breathing Zone", after: new[] { "keep_warm" }, requires: new[] { "lay_down" }),
            };
            d.mistakes = new List<MistakeDefinition>
            {
                M("gave_drink", "Gave him something to drink", "Do not give food or drink to someone in shock - they may vomit and choke.", 15, true),
                M("walk_it_off", "Told him to get up and walk", "Keep the person lying still; do not move them unnecessarily.", 10),
                M("underestimated", "Missed the signs of shock", "Pale, cool, clammy skin with fast breathing and weakness can mean shock - call 911.", 15),
            };
            d.keyPoints = new List<string>
            {
                "Signs of shock: pale or grey, cool, clammy skin; fast breathing; fast weak pulse; restlessness, thirst, weakness.",
                "Call 911 - shock is life-threatening.",
                "Help the person lie on their back. If there is no injury, raising the feet 6-12 inches may help.",
                "Keep them warm, reassure them and do not give anything to eat or drink.",
                "Watch breathing and responsiveness until EMS arrives. If they vomit, roll them onto their side.",
            };
        }
    }
}
