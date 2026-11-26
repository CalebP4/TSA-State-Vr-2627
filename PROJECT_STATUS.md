# Project Status

_Last updated: 2026-11-25_

## Completed

- [x] **Project setup.**
  - Unity 6000.6.4f1, URP, XR Interaction Toolkit 3.6, OpenXR.
  - Quest 2 Android/OpenXR configuration: **Tools > First Aid VR > Configure for Quest 2**.
- [x] **Main menu.**
  - Branding, Training / Certification, and scenario selection with best scores and certified badges.
  - Settings: movement, turning, vignette, volume, seated height, clear results.
  - Controls page and results history (each result opens its dashboard).
- [x] **Scenario 1, Severe Bleeding.**
  - Park scene and patient, with a blood pool that grows while bleeding.
  - First-aid kit, gloves, two dressings, roller bandage, emergency blanket and phone.
  - Scene check, shoulder tap, recognition decision, a 911 call with dispatcher, gloves, a dressing on the wound, pressure held, the soak-through decision, a second dressing, bandage, circulation check, blanket, reassurance and the ambulance arriving.
- [x] **Scenario 2, Infant Choking.**
  - Living room, infant in a baby seat and a parent NPC.
  - Recognition and the parent calling 911.
  - Face-down / head-low pose detection, 5 back blows, flipping face-up, 5 heel-of-hand chest thrusts, a repeat round, the object coming out, a breathing check and paramedics.
  - Detects the dangerous mistakes: a blind finger sweep, abdominal thrusts, and letting go of the baby.
- [x] **Scenario 3, Shock.**
  - Trailhead, seated hiker and friend NPC.
  - Recognition, a 911 call, lowering him to lie flat with your hand, raising the feet with the backpack (optional), the blanket, reassurance and monitoring breathing.
  - "Give water" is a critical mistake, offered both as a talk option and by holding the bottle to his mouth.
- [x] **Training Mode.** Step board, checklist, highlight arrow, instant feedback (green / amber / red with explanations), retries on decision cards, and a live pose indicator for the infant.
- [x] **Certification Mode.** No instructions or hints, time limit with countdown, neutral sounds, every action and mistake logged, first answer counts, pass/fail.
- [x] **Performance dashboard.**
  - Overall score ring, technical and efficiency bars, and grade.
  - Correct and incorrect actions, time vs target, and missed critical steps.
  - Step checklist with times, a timeline chart, and areas for improvement.
- [x] **Results saved on the device** (JSON) and listed in the menu.
- [x] **Pause menu** (left Menu button or the board button): resume, restart, main menu.
- [x] **Sound effects** (generated in code), haptics on grabs, presses, strikes and mistakes, and gloved hands turning blue.
- [x] **Desktop testing** with the XR Interaction Simulator (menu toggle).
  - The toggle was verified to set the simulator prefab in the XRI settings.
  - Playing with it by hand has not been tried yet.
- [x] **22 automated PlayMode tests.**
- [x] **Windows player build** (`Tools > First Aid VR > Build Windows Check`) succeeds with 0 errors.
  - This proves the game code compiles for a player and the scenes package.
  - It does **not** test Android, the Quest, or rendering in a headset.
- [x] **Documentation.** README, CONTROLS, DEVELOPMENT_NOTES and this file.

## In progress / next

- [ ] **First on-headset play session.** Check grab feel, reach to patients on the ground, text readability and comfort.
- [ ] **Quest 2 build.** It needs Android Build Support installed in Unity Hub (see "Blocked" below).
- [ ] **Profile frame rate on Quest 2.** The target is 72 fps.

## Remaining ideas (not required)

- Voice narration of the instructions.
- A tourniquet practice variant of the bleeding scenario, where pressure fails.
- An "unresponsive infant", CPR, follow-on scenario.
- Nicer character models, if a free, properly licensed low-poly set is found.

## Blocked: needs a person

| What | Why | How |
|---|---|---|
| Android Build Support | It isn't installed on the development PC, and it must be added through Unity Hub. | Unity Hub > Installs > 6000.6.4f1 > ⚙ > Add modules > **Android Build Support** (+ OpenJDK + SDK & NDK). Then use **Tools > First Aid VR > Build Quest 2 APK**. |
| Team Unity version | The repo was created in 6000.0.32f1; this work was built and tested only in **6000.6.4f1** (packages upgraded). | The team decides: either everyone moves to 6000.6.4f1, or the work is ported back and retested on 6000.0.32f1. |
| Headset testing | A machine cannot judge grab feel, comfort or readability. | Play all 3 scenarios in both modes on the Quest, and note anything awkward here. |

## Testing status

| Area | Tested how | Result |
|---|---|---|
| Scoring rules | 7 unit tests (`ScoringTests`) | Pass |
| Scenario content (ids, prerequisites, glyphs in the font) | 2 tests | Pass |
| Bleeding: full correct run, Training + Certification | PlayMode test driving the real rig | Pass, 100% in both |
| Bleeding: mistakes (dressing in the wrong place, wrong answer, asking him to walk) | PlayMode test | Pass |
| Infant: full correct run, Training + Certification | PlayMode test (real back-blow strikes and chest compressions) | Pass, 100% in both |
| Infant: finger sweep + abdominal thrust are critical, with no feedback in Certification | PlayMode test | Pass |
| Infant: Certification time limit ends the run and fails it | PlayMode test | Pass |
| Infant: grabbing near the head is not a finger sweep, and letting go mid-procedure is recorded as "unsupported" | PlayMode test | Pass |
| Bleeding: pause stops the clock; letting go of pressure early is a mistake | PlayMode test | Pass |
| Windows player build (compile + packaging only) | Batch build | Pass |
| Desktop simulator toggle | Batch check of the settings asset | Pass (not played by hand) |
| Shock: full correct run, Training + Certification | PlayMode test (guided lowering, placements, breathing dwell) | Pass, 100% in both |
| Shock: giving water is a critical mistake | PlayMode test | Pass |
| Main menu: settings, controls, results, mode select, loading a scenario | PlayMode test | Pass |
| Visual check of every screen | Screenshots from the test run (`Logs/Shots`) were reviewed | OK |
| On a real Quest 2 | **Not yet tested** (no Android module, no headset session). This includes grab feel, the controller Menu button, the seated-height option and frame rate. | — |

## Known bugs / rough edges

- **Infant hold is simplified.** Real technique uses both forearms; see DEVELOPMENT_NOTES.md.
- **Patients lie on the ground.** Standing players have to kneel; Seated mode height helps.
- **OpenXR logs errors in editor Play mode when no headset is connected.** This is harmless; use the Desktop Simulator.
