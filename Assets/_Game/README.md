# Crime Scene — Game Assets

All custom Crime Scene content lives under `Assets/_Game/`. Third-party packages (BNG Framework, XR Samples, TextMesh Pro) stay in their original locations.

## Folder layout

| Folder | Contents |
|--------|----------|
| `Scenes/Production/CrimeScene/` | The crime scene, split into additive scenes (see **Additive crime scene** below). Room scenes are in `Rooms/` |
| `Scenes/Flow/` | Menus, tutorial, and experience flow scenes |
| `Scenes/Dev/` | Test and prototype scenes only |
| `Scripts/Core/` | XR recenter, gaze doors, ambient audio, map border (`MapBoundary`) |
| `Scripts/SceneFlow/` | Scene loading (`CrimeSceneLoader`), spawn points, teleport triggers, fall/out-of-bounds respawn (`PlayerFallRespawn`), tablet persistence |
| `Scripts/Tablet/` | Tablet UI (`ScreenManager` is the one tablet UI controller), clock, notebook, recall |
| `Scripts/Interaction/` | Raycast grab/drop interactables |
| `Scripts/Settings/` | Player settings persistence |
| `Scripts/Dev/` | Editor/desktop-only helpers (movement, `RigSwitcher` for the dev test scene) |
| `Art/Environment/` | FBX environment and prop models. House models in use (all in `CrimeScene_HouseShell`): `CrimeSceneHouseUpdate2026` + `CrimeSceneHouseNoStair` (walls, floors, stairs), `OutsideHouseFix` (roof), `HouseShell` (garage, window trims) |
| `Art/Materials/` | Materials and textures for environment art |
| `Art/Tablet/` | Tablet mesh and app icon sprites |
| `Art/Videos/` | In-game footage clips |
| `Art/UI/` | UI-related materials and assets |
| `Audio/` | Ambient sound effects |
| `Prefabs/Player/` | Player rig prefab (`XR Rig Rigidbody`, lives in `CrimeScene_Main`) |
| `Prefabs/Tablet/` | Tablet prefabs (`Tablet_manager`, `TabletRecall`, `Tablet`) |
| `Prefabs/SceneFlow/` | `SceneTeleportTrigger` prefab (front-door triggers in `CrimeScene_Gameplay`) |
| `Input/` | `InputSystem_Actions` (the project's Input System action asset) |
| `_Archive/` | Retired prototypes, unused art, and the pre-additive `CSHouse` / `CS_Outside` scenes (do not use in production) |

## Scene flow

```
MenuScene → TutorialScene / ExperienceScene
TutorialScene → MainMenu (skip) or back to MenuScene
CrimeScene_Main → loads every crime-scene scene additively (inside and outside are one world)
```

## Build settings

`CrimeScene_Main` is the first scene in the build, followed by every scene it loads additively (all enabled). The loader looks them up by name, so a crime-scene scene that is missing from the build list will not load.

Flow scenes are listed but disabled — they must stay in the build list so `SceneManager.LoadScene()` can find them by name.

## Additive crime scene

The house, the street and the gameplay layer are separate scenes so the modeler and the engineers can work at the same time without merging the same `.unity` file. All scenes share one world space: when they are loaded together they form one continuous environment (you can walk out of the front door).

| Scene | Owns |
|-------|------|
| `CrimeScene_Main` | Global systems only: `Systems/SceneLoader` (`CrimeSceneLoader`), `Systems/Lighting` (the sun), `Systems/FallRespawn` (`PlayerFallRespawn`), `XR/XR Rig Rigidbody`, `Tablet_manager`, `TabletRecall`. Main is the active scene, so lighting/RenderSettings live here |
| `CrimeScene_HouseShell` | Anything shared by several rooms: house structure mesh, roof, garage/window trims, floor, all doors |
| `CrimeScene_Outside` | Street, neighbouring houses, police props, outdoor ambience, `Outside/MapBoundary` (invisible map border — drag its corner handles in the Scene view to reshape it) |
| `Rooms/CrimeScene_<Room>` | Props that belong to one room: Entryway, LivingRoom, Kitchen, DownstairsHallway, DownstairsCoatCloset, UnderStairsCloset, DownstairsBathroom, StairsUpstairsHallway, MainBedroom, UpstairsWalkInCloset, Bedroom2, UpstairsBathroom, UpstairsCloset, UpstairsOffice. Each room's root empty sits in the middle of that room on its floor; a room with no props yet holds a `Placeholder Cube` (delete it once the room has a real object) |
| `CrimeScene_Gameplay` | Gameplay-only objects: `Objectives`, `GlobalTriggers` (front-door teleports), `SpawnPoints`, `Narration`, `InvestigationSystems`, `UI` |

Room map (see the floor plan in `References/`; upstairs floor is at y ≈ 2.54). Ground floor — Entryway (front door / porch), Living Room (+x, +z), Kitchen + pantry (+x, −z), Downstairs Bathroom (half bath, back corner by the pantry), Downstairs Hallway (back hall), Downstairs Coat Closet (narrow closet off the back hall), Under Stairs Closet (door from the hall, under the upper stair flight). Upstairs — Stairs + Upstairs Hallway, Bedroom 2 (−x, −z), Upstairs Walk-In Closet (−x, +z, door into the main bedroom), Main Bedroom (+z), Upstairs Closet (the hall closet), Upstairs Bathroom (between the office and main bedroom), Upstairs Office (+x, −z, the room with the safe).

**Where does a new object go?**
- Used by more than one room (walls, doors, windows, floors) → `CrimeScene_HouseShell`.
- Belongs to one room → that room's scene, under the room's root object.
- Gameplay-only and not part of a prop (triggers, objectives, spawn points) → `CrimeScene_Gameplay`.
- Global systems, the XR rig, scene loading → `CrimeScene_Main`. Keep room content out of Main.

**Working with the scenes**
- Opening `CrimeScene_Main` (double-click or **Open All For Editing**) opens every crime-scene scene with it. To work on just one room, use **Tools ▸ Crime Scene ▸ Additive Scenes ▸ Open Main Only** and drag that room's scene into the Hierarchy, or untick **Auto-Open All Scenes With Main** (a per-user setting).
- Press Play with `CrimeScene_Main` open. The loader loads every other scene (and skips any you already have open). Playing a room scene on its own has no player.
- Save only the scene you changed. The Hierarchy marks modified scenes with `*`; don't save scenes you didn't mean to touch, that is what causes merge conflicts.
- Never drag a reference from one scene into a component in another scene — Unity cannot save cross-scene references and silently drops them. Use a prefab, a name/id lookup at runtime (see `SpawnPoint.Find`, `SceneTeleportTrigger.promptTextName`) or a manager in Main.
- To move an object to another scene, drag it onto that scene's header in the Hierarchy; its world position is kept.
- Keep `Tablet_manager` and `TabletRecall` as root objects in Main (they use `DontDestroyOnLoad`, which only works on roots).
- Before pushing, run **Tools ▸ Crime Scene ▸ Additive Scenes ▸ Validate**: it checks the build list, missing scripts and cross-scene references.
- A new crime-scene scene must be added to the `CrimeSceneLoader` list on `Systems/SceneLoader` in Main **and** to the Build Profiles scene list.

## Conventions

- Put new game scripts in the matching `Scripts/<Feature>/` subfolder.
- Put new 3D models in `Art/Environment/` and materials in `Art/Materials/`.
- Put new game scenes in `Scenes/Production/` or `Scenes/Flow/` as appropriate. Crime-scene content goes into the matching additive scene (see above), not a new standalone level.
- Do not add content to `_Archive/` unless retiring old work.
- Leave vendor packages (`BNG Framework`, `Samples/`, `TextMesh Pro/`) untouched.
- Our own prefabs go in `Prefabs/`, never inside the vendored `BNG Framework/` folder.
- Player-facing input uses the Input System (an `InputActionProperty`, or `Keyboard.current` / `Mouse.current` for desktop testing). Don't add new `Input.GetKey` calls. Active Input Handling stays on *Both* because BNG still reads legacy input internally.

## After moving assets

Unity tracks assets by GUID (`.meta` files), so references should survive moves as long as `.meta` files move with their assets. If something breaks after a reimport:

1. Select the asset in the Project window.
2. Use **Find References In Scene** or the missing-script inspector to locate broken links.
3. Re-assign any `[SerializeField]` references that show as **Missing**.
