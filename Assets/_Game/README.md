# Crime Scene — Game Assets

All custom Crime Scene content lives under `Assets/_Game/`. Third-party packages (BNG Framework, XR Samples, TextMesh Pro) stay in their original locations.

## Folder layout

| Folder | Contents |
|--------|----------|
| `Scenes/Production/` | Shipped levels (`CSHouse`, `CS_Outside`) |
| `Scenes/Flow/` | Menus, tutorial, and experience flow scenes |
| `Scenes/Dev/` | Test and prototype scenes only |
| `Scripts/Core/` | Player rig setup, XR recenter, ambient audio |
| `Scripts/SceneFlow/` | Scene loading, teleport triggers, tablet persistence |
| `Scripts/Tablet/` | Tablet UI panels, clock, notebook, recall |
| `Scripts/Interaction/` | Raycast grab/drop interactables |
| `Scripts/Settings/` | Player settings persistence |
| `Scripts/Dev/` | Editor/desktop-only movement helpers |
| `Art/Environment/` | FBX environment and prop models |
| `Art/Materials/` | Materials and textures for environment art |
| `Art/Tablet/` | Tablet mesh and app icon sprites |
| `Art/Videos/` | In-game footage clips |
| `Art/UI/` | UI-related materials and assets |
| `Audio/` | Ambient sound effects |
| `Prefabs/Player/` | Customized player rig prefabs |
| `Data/TeleportDestinations/` | Scene teleport destination assets |
| `Input/` | Input System action assets |
| `_Archive/` | Retired prototypes (do not use in production) |

## Scene flow

```
MenuScene → TutorialScene / ExperienceScene
TutorialScene → MainMenu (skip) or back to MenuScene
CSHouse ↔ CS_Outside (in-scene teleport)
```

## Build settings

Production scenes enabled in build:
- `_Game/Scenes/Production/CSHouse`
- `_Game/Scenes/Production/CS_Outside`

Flow scenes are listed but disabled — they must stay in the build list so `SceneManager.LoadScene()` can find them by name.

## Conventions

- Put new game scripts in the matching `Scripts/<Feature>/` subfolder.
- Put new 3D models in `Art/Environment/` and materials in `Art/Materials/`.
- Put new game scenes in `Scenes/Production/` or `Scenes/Flow/` as appropriate.
- Do not add content to `_Archive/` unless retiring old work.
- Leave vendor packages (`BNG Framework`, `Samples/`, `TextMesh Pro/`) untouched.

## After moving assets

Unity tracks assets by GUID (`.meta` files), so references should survive moves as long as `.meta` files move with their assets. If something breaks after a reimport:

1. Select the asset in the Project window.
2. Use **Find References In Scene** or the missing-script inspector to locate broken links.
3. Re-assign any `[SerializeField]` references that show as **Missing**.
