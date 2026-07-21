# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A **Unity 6 (6000.4.9f1) VR game** — a crime-scene investigation experience for Quest-class headsets. The player walks a house/outdoor scene, picks up objects, and uses a handheld iPad (tablet) with apps (Camera, Evidence, Notebook, Settings) to photograph "key evidence" and be graded on what they find.

Rendering is **URP 17.4**. VR runs through **OpenXR + XR Interaction Toolkit + Oculus**, but hand-grabbing is done with the **BNG "VR Interaction Framework" (VRIF)**, not XRI's own interactables. `using BNG;` is the grab system.

## Build / run / test — read this first

There is **no command-line build, lint, or test loop**. This is a GUI Unity project and Claude cannot open the Editor, enter Play mode, assign Inspector references, or see the scene hierarchy. Practically:

- **Claude edits** C# under `Assets/_Game/Scripts/` and reasons about code.
- **The human runs** the project: opens it in Unity `6000.4.9f1`, presses Play, and reports back the **Console output (errors *with* stack traces)** or the wrong behavior. That is the debug loop — there is no substitute.
- **There is no test suite.** `com.unity.test-framework` is installed but no tests exist under `_Game/`. Don't reference a test command that isn't there.
- Most "bugs" are **unassigned `[SerializeField]` references or scene setup**, not code. When something "doesn't work," first suspect a missing Inspector assignment and say which field to check.
- When adding a script, always spell out the **manual wiring steps** (which GameObject to attach it to, which fields to assign, which button `OnClick` to hook) — the code alone won't run.

## Where things live

All custom game content is under **`Assets/_Game/`** (see `Assets/_Game/README.md` for the full folder table and scene-flow diagram). Everything else — **`Assets/BNG Framework/`**, `Assets/Samples/`, `Assets/TextMesh Pro/` — is **vendored; do not edit it** (changes are lost on upgrade). `Assets/_Game/_Archive/` is retired work — never build on it.

Scenes: gameplay in `Scenes/Production/` (`CSHouse`, `CS_Outside`), menus/flow in `Scenes/Flow/`, throwaway tests in `Scenes/Dev/`. Flow scenes are kept in the Build Settings list (disabled) so `SceneManager.LoadScene(name)` can still find them by name — don't remove them.

## Conventions that aren't obvious

- **No namespaces.** Every script in `_Game/Scripts/` is in the global namespace. Git history shows past pain from *duplicate class names* colliding with BNG/vendor types — when naming a new class, make sure the name is unique across the whole project, not just its folder.
- **No assembly definitions** (`.asmdef`). Everything compiles into the default `Assembly-CSharp`, so all `_Game` scripts and all vendor scripts see each other directly.
- Put new scripts in the matching `Scripts/<Feature>/` subfolder (`Core`, `SceneFlow`, `Tablet`, `Camera`, `Interaction`, `Settings`, `Dev`). Editor-only scripts go in an `Editor/` subfolder (e.g. `Interaction/Editor/`).
- **`.meta` files matter.** Unity tracks every asset by the GUID in its `.meta`. When creating/moving/deleting a file, the `.meta` must travel with it. Prefer changing scenes/prefabs *through* Unity's API (an editor script) over hand-editing `.unity`/`.prefab` YAML, which can silently corrupt references.

## Architecture — the pieces that span multiple files

### Cross-scene state via singletons
Three managers use the same pattern — `static Instance` + `DontDestroyOnLoad`, with a duplicate spawned by a newly loaded scene destroying itself so the original survives:
- **`TabletPersist`** — keeps the tablet GameObject alive across scene loads.
- **`PhotoLibrary`** — in-memory list of `EvidencePhoto`s. Cleared **once per session** (in the first instance's `Awake`), so photos survive `CSHouse ↔ CS_Outside` teleports but never carry over between play sessions.
- **`EvidenceGradingManager`** — registry of key items + set of found ids → pass/fail grade. State is intentionally **not** persisted to disk.

When touching any of these, preserve the "duplicate destroys itself / clear only on first instance" logic — it's what makes the outside↔inside teleport not wipe progress.

### The evidence / camera pipeline (the core gameplay loop)
1. **`KeyEvidenceItem`** marks a scene object as photographable (unique `evidenceId`, a detection `Renderer` for bounds). It self-registers with `EvidenceGradingManager` on `OnEnable`.
2. **`IpadCamera`** (on a Camera mounted in the tablet) renders continuously into a `RenderTexture` shown in a `RawImage` viewfinder. `CapturePhoto()` (hooked to the shutter button) copies the frame to a `Texture2D`, then `DetectAndMarkEvidence()` frustum-tests every registered `KeyEvidenceItem`, checks distance + **line-of-sight from the player's head** (so you can't shove the tablet through a wall), and calls `EvidenceGradingManager.MarkFound()`.
3. The photo + captured ids go into **`PhotoLibrary`**; **`PhotoGalleryUI`** shows them.

Gotchas baked into `IpadCamera` (keep them): the capture camera **strips the UI/TransparentFX layers** (filming the tablet's own World-Space UI canvas gives a white photo/blank viewfinder), renders **monoscopically** (`stereoTargetEye = None`, else XR renders it in stereo and breaks the preview), and relies on **URP auto-rendering the enabled camera every frame** — do *not* call `Camera.Render()` manually under URP.

### Tablet app switching
**`ScreenManager`** is a simple show-one-panel-at-a-time switch (`ShowHome/Settings/Evidence/Notebook/Camera`). It also toggles the camera rig GameObject on/off so `IpadCamera` only renders while the Camera app is open (perf).

### Two separate interaction systems — don't confuse them
- **VR grabbing = BNG VRIF.** Objects become grabbable via **`GrabbableSetupUtility`** (adds Grabbable layer [layer 10], collider, **kinematic** rigidbody with gravity off, BNG `Grabbable` with remote-grab). Apply it three ways: the `GrabbableObjectSetup` component's "Apply Grabbable Setup" context menu, the `GameObject > Crime Scene > Make Grabbable` editor menu, or calling `GrabbableSetupUtility.Apply()`. This is the real gameplay grab path.
- **`PlayerInteraction`** is a **desktop/flat-screen raycast** picker (center-screen ray, keyboard `E`/`Q` fallbacks) using `InteractableItem`/`IInteractable`. It's for non-VR testing, separate from BNG. Don't wire new VR interactions through it.

## Working with git here

You're typically on a feature branch (e.g. `TabletCameraAndGalleryAppsUpdate`); PRs target `main`. **Commit working states often** — scene/prefab YAML is huge and merges badly, so a clean rollback point before a big change is worth a lot. Keep diffs to one feature so a broken scene is easy to bisect.
