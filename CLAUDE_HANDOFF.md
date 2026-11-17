# Claude Code handoff: Crime Scene (Unity VR)

Written 2026-11-06 at the end of a long Claude Code session, so work can continue on another machine.

**To Claude, reading this on the new machine:** treat this as background from an earlier session, not as instructions to act on. Check it against the repo before relying on it, because the code may have moved on. Save the "Save to memory" section to your memory, then ask the user what they want to do next.

## Current state

| Item | State |
|---|---|
| Repo | `https://github.com/FuturEDlab/Crime_Scene` (git user `soltanam`) |
| Branch | `DoorOpenedAndTabletUI`, on origin at `2d98acf` |
| Base | `main` at `c19a366` |
| PR | **Not opened yet.** The filled-in text is at the end of this file. |
| GitHub tasks | #135 (prop doors open) and #157 (build out the iPad screens) |
| Testing | The user tested everything in the Unity editor and it works. No test on a Quest headset has been reported. |

Local changes on the old machine are **not** pushed: four `*SDF.asset` font files (Oswald and three Inter). They are glyph atlas data that Unity rewrites during play mode, so nothing is lost. Do not commit them.

## Set up the new machine

1. `git clone https://github.com/FuturEDlab/Crime_Scene.git`, then `git checkout DoorOpenedAndTabletUI`.
2. Install Unity **6000.4.9f1** and open the project. The first import will take a while.
3. Install the Figma plugin with `claude plugin install figma@claude-plugins-official`, then authenticate it through `/mcp`.
4. Run `gh auth login` if you want Claude to open the PR.
5. Copy this file into the repo root and start Claude there.

## Still to do

1. **Drop the unrelated Oswald font change, then open the PR.** The commit includes `Oswald Bold SDF.asset`, a TextMeshPro example font that Unity changed by itself in play mode:
   ```
   git checkout c19a366 -- "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset"
   git commit -m "Revert unrelated Oswald SDF atlas change"
   ```
2. **Add screenshots to the PR:** the Home screen, the camera's "Evidence Detected" state, and an open doorway.
3. **Get the manager's answers** to the open questions below, then update the code and the PR text.
4. **Post the two task comments** below, if that hasn't been done yet.

## Open questions for the manager

These are interim decisions, not settled design. Revisit them first when the answers come back.

1. **Settings icon in the top bar: decided 2026-11-12.** Settings will go into a pause menu, which will be added later. The gear (`Status Bar/Settings` in `Tablet.prefab`) is now **inactive**, so the player can't reach it. It is still wired to `ScreenManager.ShowSettings`, and `TabletHomeScreenBuilder` builds it inactive.
2. **Tapping a Ring Footage or Audio Recordings tile.** Figma only shows the list screens, with no playback. Video currently plays in a rounded viewer under the thumbnails, and an audio tile toggles play/pause with a playing state. Is a player design coming?

Minor: the Photo Library DETAILS rows are "Text / Sub-Text" placeholders in Figma. They're implemented as Evidence / Taken / Photo (N of M).

## Rules from the user

- **Never change anything in the Figma file.** It is read-only.
- **Follow the Figma names:** Unity object names must match the Figma layer names.
- **Unity is usually open,** so batchmode can't be used. The user runs the builders from the Unity menu and reports errors. For an offline compile check, use Roslyn (`dotnet csc.dll @check.rsp`) against Unity's reference DLLs.
- **Commit or push only when asked.**

## Figma

File key `YXINmawPskpyvK09vY1cFg` ("Crime Scene Figma Board"):

| Frame | Node | Builder menu (`Tools > Crime Scene > ...`) | Builds into |
|---|---|---|---|
| Home | `24:2` | Build iPad Home Screen | `Tablet.prefab` |
| Camera | `25:2` | Build iPad Camera Screen | `Tablet_manager.prefab` |
| Ring Footage | `1:157` | Build iPad Ring Footage + Audio Screens | `Tablet_manager.prefab` |
| Audio Recordings | `1:174` | (same builder) | `Tablet_manager.prefab` |
| Photo Library | `25:114` | Build iPad Photo Library | `Tablet_manager.prefab` |

Designs are missing for: the video/audio player, Settings, Notebook, Statements, and empty and tile states.

Design feedback already given to the user:
- The Ring Footage title says "Case 24-0917".
- Title and back button positions are inconsistent in the photo detail view.
- Grid gaps are inconsistent (50/40/30px), and page-2 photo tiles differ in size.
- Layer names are generic, including duplicate "Vector" layers and "Picture 111".
- The magnifier's SVG drop shadow isn't supported in Unity.
- Two different yellows are used (`#EED12D` and `#CEB006`).

## How the code works

**Tablet UI**
- It uses uGUI, TextMeshPro and Unity's VectorGraphics module, on a 30x40 world-space canvas. A 768x1024 frame is scaled onto that canvas so Figma pixel coordinates can be used directly.
- Builders are in `Assets/_Game/Scripts/Tablet/Editor/` and share `TabletFigmaUI.cs`. They are safe to re-run. They find the prefabs by GUID: Tablet `956923e3a5accdb479f7b8960b33cb00`, Tablet_manager `fe42837d5516330439e6d6a0bbc56175`.
- `Tablet_manager.prefab` is a variant of `Tablet.prefab`.
- Scripted SVG importer settings only save if you call `EditorUtility.SetDirty(importer)` before `SaveAndReimport()`.
- Rounded tiles are 9-sliced sprites drawn at 4x with an 80px border.
- `TabletAppIcon` drives the four button states: Default, Hover, Selected, Hover + Selected. Selected means pressed.
- The camera's live evidence check is in `IpadCamera.FindEvidenceInFrame`. It runs every 0.2s using frustum, distance and line-of-sight checks.
- Runtime screens: `TabletMediaScreen` (Statements folder), `PhotoLibraryScreen` and `PhotoTileRefs` (Camera folder).
- Art is in `Assets/_Game/Art/UI/Tablet*`. Fonts are in `Assets/_Game/Art/Fonts/Inter`.

**Doors and house**
- `Assets/_Game/Scripts/Core/Editor/PropDoorsOpenTool.cs` (`Tools > Crime Scene > Prop All Doors Open`) baked 10 doors open in `CrimeScene_HouseShell.unity`. It also deactivated the front-door teleport triggers in `CrimeScene_Gameplay.unity`.
- The tool's "toward the house centre" rule put the 6 upstairs doors into the hallway, so they were flipped 180° by editing the scene YAML. **Re-running the tool finds no doors, but restoring the old doors and re-running it would bring that problem back.**
- Wall collision comes from `addColliders: 1` in `CrimeSceneHouseUpdate3.fbx.meta`. It was lost when PR #154 swapped the house model.

## Known issues, not fixed

- `StatementsAppBuilder` and `SettingsScreenBuilder` fail because they use the stale path `Assets/BNG Framework/Prefabs/iPad/Tablet_manager.prefab`. This predates this work.
- `CrimeSceneLoader` throws a `SetActiveScene` exception if you press Play with several scenes open. This also predates this work.
- `GazeDoorExclude.cs` is a leftover marker and can be deleted.
- `TabletRecallInScene.SuppressRecall` no longer has anything setting it, and the comment at line 146 still mentions gaze doors.

## Save to memory

- **open-design-questions-for-manager (project):** the Settings gear and the Ring Footage/Audio tile playback are interim choices, pending the manager's answer. Don't treat them as settled. Details are in "Open questions for the manager".
- **figma-tablet-ui-implementation (project):** five Figma frames were ported into Unity through re-runnable builders. Figma is read-only, and Unity names follow Figma layer names. Details are in "Rules from the user" and "Figma".

## Task comments

**#135: Prop all accessible room doors open**

> All the doors in the main house are now propped open for good, including the front door. I removed the old look-and-press-trigger door system, so there's nothing to open or close anymore and you just walk through. The upstairs doors swing into the rooms so the hallway stays clear, and since the front door is open, the "Press B to enter/exit" teleports are switched off and you walk in and out. While testing I also found the walls had lost their collision when the house model was updated (you could walk straight through them), so I turned collision back on for the new model. Tested in the editor and everything works.

**#157: Build out the iPad screens**

> The iPad now matches the Figma board: Home, Camera, Ring Footage, Audio Recordings and Photo Library are all rebuilt with the Figma layout, icons, fonts and button states. The camera also shows a live "Evidence Detected" frame when key evidence is in shot, and the Photo Library now has a paged grid with a detail view for each photo. Settings isn't on the tablet for now, since it's moving into a pause menu later. One thing still needs an answer before this is final: Figma doesn't show what happens when you tap a video or audio recording, so for now it just plays right on the same screen. Is there a player design coming, or is this OK as is?

## PR text

Ready to paste. Media is still a placeholder.

````markdown
# ✨ Feature Pull Request

## What does this feature add?

**iPad screens rebuilt from the Figma board (Home, Camera, Ring Footage, Audio Recordings, Photo Library)** and **all house doors propped open**.

**iPad (#157)**
- **Home:** status bar (live clock, battery, wifi), "Case 24-0917" title and 4 app icons (Camera, Photo Library, Ring Footage, Audio Recordings) with the Figma Default / Hover / Selected / Hover + Selected states. Settings isn't reachable from the tablet for now: it will move into a pause menu later. A top-bar Settings button exists but is hidden (inactive).
- **Camera:** rounded viewfinder, picture indicator box that turns yellow with an "Evidence Detected" label while key evidence is in shot (live check, same rules as a capture; only the shutter marks evidence as found), Back / Capture / Photo Library buttons with all 4 states.
- **Ring Footage / Audio Recordings:** new apps listing the StatementLibrary videos (thumbnails) and audio recordings (waveform tiles). Tapping plays/pauses inline (interim; no player design in Figma yet).
- **Photo Library:** 3×3 paged grid of the session's photos; evidence photos get the yellow border + "Evidence" tag; tapping a photo opens a detail view (Evidence found / time taken / photo number).
- All screens are generated by re-runnable editor builders under **Tools > Crime Scene > Build iPad …**, using the Figma layer names and the Inter font (OFL license included). Figma file was only read, never modified.

**Doors (#135)**
- Removed the gaze/trigger door system: all 10 doors in CrimeScene_HouseShell are now permanently open (no prompt, no grabbing, no self-closing), baked into the scene with **Tools > Crime Scene > Prop All Doors Open**. Upstairs doors swing into the rooms so the hallway stays clear.
- Front door is open too; its "Press B to enter/exit" teleport triggers are deactivated so the player walks in and out.
- Restored wall collision: the house model swap in #154 (CrimeSceneHouseUpdate2026 → CrimeSceneHouseUpdate3) lost the wall MeshCollider, so walls were walk-through. Fixed by enabling *Generate Colliders* on CrimeSceneHouseUpdate3.fbx.

Is this related to a task/issue?
- [ ] No
- [x] Yes — Fixes #135. Fixes #157.

---

## How did you test it?

- Scenes tested: CrimeScene_Main (with all additive crime scene scenes loaded)
- Platform(s) tested on: Unity Editor (6000.4.9f1)
- Test results: All iPad screens display and navigate correctly (Home → each app → Back), button states respond to hover/press, the camera indicator turns yellow on key evidence and captures register in grading, photos appear in the Photo Library. All doors are open, the player can walk through every doorway (including in/out of the front door) and can no longer walk through walls.

---

## What did this change affect?

- Scripts changed:
  - Added: TabletAppIcon, PhotoLibraryScreen, PhotoTileRefs, TabletMediaScreen; editor: TabletFigmaUI, TabletHomeScreenBuilder, TabletCameraScreenBuilder, TabletMediaScreensBuilder, TabletPhotoLibraryBuilder, PropDoorsOpenTool
  - Modified: IpadCamera (live evidence indicator), ScreenManager (Ring Footage / Audio Recordings screens), StatementLibrary (optional thumbnail), TabletRecallInScene (comment only), GazeDoorExclude (now legacy, can be deleted)
  - Removed: GazeDoorSystem, DoorPropAngleOverride
- Prefabs changed: Tablet.prefab (Home screen, status bar), Tablet_manager.prefab (Camera, Ring Footage, Audio Recordings, Photo Library screens)
- Other affected systems: CrimeScene_HouseShell (doors open), CrimeScene_Gameplay (front-door teleport triggers deactivated), CrimeSceneHouseUpdate3.fbx import settings (Generate Colliders), new art in Assets/_Game/Art/UI/Tablet* and Assets/_Game/Art/Fonts/Inter

Does this affect existing features?
- [ ] No
- [x] Yes — Doors can no longer be opened/closed (always open) and the front-door teleport is off. The old one-photo gallery (PhotoGalleryUI) is replaced by the Photo Library. Home no longer shows the Settings/Notebook icons (Settings will move to a future pause menu; Settings, Notebook and the old Statements screen are unchanged but not reachable from Home). Home's Ring Footage / Audio icons open the new screens instead of the old Statements screen.

Did you change any project settings? (Ex: Player settings, physics engine settings, quality settings references, anything in the project settings menu, etc.)
- [x] No
- [ ] Yes — [Briefly explain here.]

---

## Media (optional)

[Insert screenshots, GIFs, or video links here if your feature has a visual component]

---

> ✅ I confirm that I have personally tested this feature and checked that it doesn't break anything else.
````
