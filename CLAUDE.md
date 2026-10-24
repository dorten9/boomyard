# BoomYard – working notes for Claude

The spec is the artifact "BoomYard Game Spec" (v2.0). Sister project: Overlane (same code-first Unity setup and CI).

## Branches
- `main` is the default branch and the only one that deploys (Web build → Vercel project `boomyard`).
- Work on `claude/*` branches; `check.yml` builds them without deploying.

## Code
- Everything is built from code: no scenes, prefabs or materials in the repo beyond the empty boot scene the editor creates.
- `Scripts/Core` is pure C# (no UnityEngine): keep game rules there so `Tools/SimTest` can test them.
- Game data (items, objects, arenas) lives in `Core/Catalog.cs`, with the spec's numbers.
- The game is English only. UI text goes through `Loc`.
- No external assets: models (`MeshKit`, `Props`, `ArenaBuilder`), textures (`TextureLib`), sound and music (`Sfx`) are all generated. Keep it that way (the owner's call).
- The fonts have no ↑ ◆ ★ ▶ glyphs: use the generated sprites in `UIKit`.
- Rendering is URP, created from code by `Editor/PipelineSetup.cs` into `Assets/BoomYard/Generated` (gitignored). Runtime materials copy the template materials there so their shader variants are in builds; a new keyword combination needs a new template.
- Lighting is golden hour (`Rendering.Sunset`); the owner found night too dark.

## Checks before pushing
- Look at the CI screenshots (`screenshots` branch) after visual changes.
- `Tools/SimTest/run.sh` – core rules (needs Mono).
- Compile check of the scripts (mcs against the Unity reference DLLs).
