# Crime Scene (VR)

Unity **6000.4.9f1** VR crime-scene investigation game for Quest-class headsets (URP, OpenXR, BNG VR Interaction Framework).

## Opening the project

Open **this folder** (the repo root) in Unity Hub with editor version `6000.4.9f1`. This is the only Unity project in the repo.

## What belongs in the repo root

Only the standard Unity project folders and repo config:

| Path | Purpose |
|------|---------|
| `Assets/` | All game content. Custom work goes in `Assets/_Game/` (see `Assets/_Game/README.md`). |
| `Packages/` | Package manifest |
| `ProjectSettings/` | Project settings |
| `docs/` | Design/research documents (not imported by Unity) |
| `.github/` | Pull request templates |
| `.gitignore`, `.gitattributes`, `.vsconfig` | Git / LFS / Visual Studio config |
| `README.md`, `CLAUDE.md` | Project docs |

**Do not** put scripts, scenes, builds, or other files in the root. Scripts go in `Assets/_Game/Scripts/<Feature>/`. Builds go in `Build/` or `Builds/` (git-ignored). Unity-generated folders like `Library/`, `Temp/`, `Logs/`, `UserSettings/` and any `*_DoNotShip` / `*_ButDontShipItWithYourGame` build folders are git-ignored and must never be committed.
