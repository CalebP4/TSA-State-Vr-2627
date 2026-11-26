# Development Notes

## Technical architecture

Everything for the simulation lives in `Assets/FirstAidVR/`:

```
FirstAidVR/
  Scripts/                (assembly FirstAidVR)
    Core/        ScenarioDefinition, ScenarioController, ScoringSystem, EventLogger,
                 ScenarioResult, ResultsStore, SimulationManager, ScenarioCatalog,
                 GameSettings, RigSettings
    Interaction/ FirstAidItem, PlacementZone, HandProbe, HandZone + TouchZone /
                 PressHoldZone / StrikeZone / CompressionZone, SceneSafetyScan,
                 PhoneItem, GloveBox, GuidedLowering, UnfoldOnPlace
    Scenarios/   ScenarioScript (base), BleedingScenario, InfantChokingScenario,
                 ShockScenario, PatientVisuals, EmsArrival, TalkMenu
    UI/          Theme, UIFactory, MainMenuManager, ScenarioUI (intro, Training
                 guide, Certification HUD, pause, results), DashboardView,
                 ChoicePrompt, FeedbackToast, SpeechBubble, Highlighter, HeadFollower
    Audio/       Sfx (all sounds generated in code)
  Editor/                 (assembly FirstAidVR.Editor)
    FirstAidSceneBuilder*.cs  builds every scene from code
    ScenarioContent.cs        default steps / mistakes text
    QuestSetup.cs             Quest 2 Android + OpenXR configuration, APK build
    DesktopSimulatorToggle.cs XR Interaction Simulator on/off
  Tests/                  (assembly FirstAidVR.Tests) PlayMode tests
  Data/                   Bleeding / InfantChoking / Shock ScenarioDefinition assets
  Resources/FirstAidVR/   ScenarioCatalog (menu order), UiTheme (font)
  Scenes/                 MainMenu, Bleeding, InfantChoking, Shock
```

### How a scenario works

1. **`ScenarioDefinition`** (ScriptableObject) holds the content: steps (id, title, Training instruction, feedback, points, critical / optional, `after` = soft order, `requires` = hard prerequisite, `requiredCount` for repeated actions), mistakes (id, correction text, penalty, critical), target time and Certification time limit.
2. **Interaction components** report **action ids** to the scene's `ScenarioController`:
   - `PlacementZone`: an item is put on a spot, e.g. a dressing on the wound
   - `PressHoldZone`: pressure is held
   - `StrikeZone`: back blows
   - `CompressionZone`: chest thrusts
   - `TouchZone`: touching or checking
   - `SceneSafetyScan`: looking around
   - `PhoneItem`, `ChoicePrompt` answers and `TalkMenu` lines
3. **`ScenarioController.Report(id)`** decides what the action means:
   - **Step done.** If it was done out of order, it still counts with a small penalty.
   - **Blocked.** A prerequisite is missing; Training shows a hint and nothing is counted.
   - **Mistake.**
   - **Ignored.** Repeats and unknown actions never cost points.
   
   It logs the result in the `EventLogger` and raises events.
4. **`ScenarioUI`** listens to those events. In Training it shows instructions, the checklist, highlights and green/red feedback. In Certification it shows only a countdown, with no hints and neutral sounds.
5. **The scenario script** (`BleedingScenario` etc.) adds the story: patient speech, decision cards, blood soaking through, the infant's pose checks, the ambulance.
6. **When every required step is done**, EMS arrives. `Finish()` then runs `ScoringSystem.Compute`, saves the result with `ResultsStore` (JSON in `Application.persistentDataPath`) and shows `DashboardView`.

### Scoring (`ScoringSystem.cs`)

| Score | How it is calculated |
|---|---|
| Technical proficiency | Points from completed steps ÷ points available, minus mistake penalties and 5 points per out-of-order step. Each kind of mistake is penalised once. Repeated actions such as back blows earn partial credit. |
| Response efficiency | 100 at or under the target time, falling linearly to 50 at the time limit. This is multiplied by the share of required steps completed. |
| Overall | 70% technical + 30% efficiency |
| Certification pass | Overall ≥ 80, all required steps done, no critical step missed, no critical mistake |

Harmless extra movement is never penalised. Only listed mistakes and out-of-order critical care cost points.

### VR interaction details

- **Rig.** The XR rig is the VR template's `Complete XR Origin Set Up Variant` (XRI 3.6 Near-Far interactors, teleport, snap turn, tunnelling vignette). The builder sets **Far Attach Mode = Near**, so an item grabbed with the laser flies into the hand instead of staying at the end of the ray.
- **`FirstAidItem.Configure` settings for every item:**
  - It is held by its `Grip` attach point, with no dynamic attach, so there is no odd offset.
  - It uses **Instantaneous** movement, which avoids lag and jitter.
  - `throwOnDetach` is off, so items drop and never fly away.
  - The rigidbody uses continuous collision detection.
  - Items dropped on the floor or out of reach glide back to where they started.
- **Hand probes.** `HandProbe` adds a small trigger sphere to each controller, so zones on the patient can feel the hand (press, strike, touch). It also reads grip/trigger and plays haptics.
- **UI.** All UI is world-space canvases built in code (`UIFactory`). They use the `TrackedDeviceGraphicRaycaster`, so both the laser and poking work. Panels that follow you move only after you turn away (`HeadFollower`); nothing is head-locked.

## Important design decisions

- **Scenes are generated by code** (`FirstAidSceneBuilder`). The whole project can be rebuilt reliably and reviewed in Git, and every object is placed by readable code. The downside is that hand edits to the scene files are lost on rebuild. Content stays editable in the Data assets.
- **Primitive-based low-poly art** (built from cubes, spheres and capsules). There are no third-party models, so there are no licensing questions, and it keeps draw calls and polygons tiny for Quest 2. The only external assets are Unity's own VR template and the XRI samples (Unity Companion License) and the template's Inter font (SIL Open Font License).
- **Synthesized sound effects** (`Sfx.cs`), so there are no audio files to license. Drop a clip in `Resources/FirstAidVR/Audio/<SoundName>` to replace one.
- **Time is compressed.** Holding pressure takes 12 seconds in the simulation, not minutes. The text explains that real pressure is held until the bleeding stops or help arrives.
- **Decision cards** (`ChoicePrompt`) are used for recognition and judgement moments ("Is this life-threatening?", "Blood soaked through, what now?"). Recognition is a key learning goal and is hard to test with physical actions alone. The clock pauses while a card is open, for fairness.
- **Certification gives no hints.** Feedback is a neutral tick for every recorded action, so the sound does not reveal whether an action was right.
- **No `Time.timeScale` changes.** Pausing just stops the scenario clock (`ScenarioController.Held`).

## Medical content and sources

The steps were checked against these sources in November 2026:

- **AHA / American Red Cross 2024 First Aid Guidelines**: bleeding control, tourniquets, shock positioning. https://cpr.heart.org/en/resuscitation-science/2024-first-aid-guidelines
- **AHA / AAP 2025 Pediatric Basic Life Support**: infant choking. https://cpr.heart.org/en/resuscitation-science/cpr-and-ecc-guidelines/pediatric-basic-life-support
- **American Red Cross, Learn First Aid**:
  - life-threatening bleeding: https://www.redcross.org/take-a-class/resources/learn-first-aid/bleeding-life-threatening-external
  - infant choking: https://www.redcross.org/take-a-class/resources/learn-first-aid/infant-choking
  - shock: https://www.redcross.org/take-a-class/resources/learn-first-aid/shock
- **Mayo Clinic first aid**: severe bleeding, shock.

Decisions where the sources differ:

- **Infant chest thrusts.** AHA 2025 now recommends the **heel of one hand** on the centre of the chest instead of two fingers. The Red Cross web page still shows two fingers. This project follows **AHA 2025**.
- **Soaked dressing.** All sources say **do not remove the first dressing**. The Red Cross says to add one more pad on top rather than stacking many, so the scenario uses exactly one extra dressing.
- **Raising the legs in shock.** This is only appropriate when there is **no trauma or injury** (AHA/Red Cross 2024). That is why the shock patient is ill and dehydrated, not injured, and why raising the feet is an **optional** step.
- **Tourniquets.** A tourniquet is used for life-threatening arm or leg bleeding that direct pressure cannot control. In this scenario pressure works, so the tourniquet is taught in the key points and not required.
- **Elevating the wound.** Mayo mentions it, but it is not part of AHA/Red Cross 2024, so it is left out.

The app states on the menu, the intro boards and the dashboard that it is not a substitute for certified training.

## Dependencies

- **Unity 6000.6.4f1** with URP 17.6.
- **XR Interaction Toolkit 3.6.1** and its samples: Starter Assets, plus XR Interaction Simulator for desktop testing.
- **OpenXR 1.18** with the Meta Quest feature and the Oculus Touch / Touch Plus profiles. The Android XR (non-Quest) features are turned off on Android.
- **Input System 1.20.**
- **TextMeshPro.** The builder creates a dynamic Inter font asset so ✓ ✗ • render.
- **Unity Test Framework** for the PlayMode tests.

## Known limitations

- **Simplified infant hold.** The baby is held in one hand along the forearm, and turning your hand over flips them. Real technique sandwiches the baby between both forearms and rests on your thigh. The text explains the real technique.
- **Pressure and thrusts aren't measured as force.** Firm pressure is detected as "hand on the dressing while holding grip or trigger". Chest thrusts are detected by how far the hand moves into the chest (≥ 2.5 cm).
- **Simple patients.** They are low-poly figures with breathing, blinking, skin colour and speech bubbles. There is no facial animation.
- **Some animated grabs aren't fully physical.** Placed items snap to fixed spots, the infant glides back to the baby seat when let go, and the seated patient's upper body pivots at the hips.
- **No headset testing yet.** The automated tests cover all the logic with the real XR interactors, but grab feel, comfort and the frame rate on a real Quest 2 still need a person in a headset (see PROJECT_STATUS.md).

## Testing approach

The PlayMode tests in `Assets/FirstAidVR/Tests/` load each real scene and stop the tracked pose drivers. They move the controller transforms from code and grab with XRI's `StartManualInteraction` on the rig's own Near-Far interactors. Then they play each scenario start to finish and check the scores. When run with graphics, `TestRig.Shot` saves screenshots of what the headset camera sees to `Logs/Shots/`.

Batch-mode command (run with Unity closed):

```
Unity.exe -batchmode -projectPath <project> -runTests -testPlatform PlayMode -testResults Logs/results.xml
```
