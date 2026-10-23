# BoomYard – working notes for Claude

The spec is the artifact "BoomYard Game Spec" (v2.0). Sister project: Overlane (same code-first Unity setup and CI).

## Branches
- `main` is the default branch and the only one that deploys (Web build → Vercel project `boomyard`).
- Work on `claude/*` branches; `check.yml` builds them without deploying.

## Code
- Everything is built from code: no scenes, prefabs or materials in the repo beyond the empty boot scene the editor creates.
- `Scripts/Core` is pure C# (no UnityEngine): keep game rules there so `Tools/SimTest` can test them.
- Game data (items, objects, arenas) lives in `Core/Catalog.cs`, with the spec's numbers.
- UI text goes through `Loc` (Hebrew + English); Hebrew is shaped with `Rtl.Visual` because legacy uGUI Text has no bidi.
- The fonts have no ↑ ◆ ★ ▶ glyphs: use the generated sprites in `UIKit`.
- Rendering is the built-in pipeline with the Standard shader for now; URP is planned for M3.

## Checks before pushing
- `Tools/SimTest/run.sh` – core rules (needs Mono).
- Compile check of the scripts (mcs against the Unity reference DLLs).
