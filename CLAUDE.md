# Wuxia MMO — Claude Code guide

A mobile-first 3D wuxia MMO-lite, currently being built as a **vertical slice**: 2 characters, levels 1–30, a shared town hub, 2 hunting zones, 1 dungeon, 1 faction war. Unity 6 client (C#), Nakama server (TypeScript runtime) on PostgreSQL.

**`docs/design.md` is the source of truth.** It holds the pillars, locked decisions, game systems, architecture and the build milestones (M0–M9) with their "done when" criteria. Read the relevant section before starting a milestone. If code and the doc disagree, the doc wins — or flag the conflict and ask; don't silently diverge. Names (Nine Peaks, Lantern Ford, …) are placeholders.

## Folder layout

```
docs/design.md        Design doc — source of truth
client/               Unity 6 project (URP, Android target)
  Assets/Scripts/     Game C# code (namespace Wuxia.*), one assembly: Wuxia.asmdef
  Assets/Tests/EditMode/  NUnit EditMode tests (Wuxia.Tests.EditMode.asmdef)
  Assets/Scenes/      OakValley.unity (M1 zone, first build scene), SampleScene.unity (M0 Nakama test)
  Assets/Prefabs/     Player, Wolf
  Assets/Art/Greybox/ Greybox materials and combined primitive meshes
  Packages/manifest.json  UPM deps: Nakama client, MCP for Unity, com.wuxia.gamedata (local, ../../shared/gamedata)
shared/gamedata/      Shared game data JSON (combat.json, …) as a local UPM package; the server reads the same files from M4
server/               Nakama server
  src/main.ts         TypeScript runtime module; InitModule entry point
  local.yml           Nakama config for local dev
  Dockerfile          Compiles the TS module and bakes it into the Nakama image
  docker-compose.yml  Nakama + PostgreSQL
.mcp.json             Claude Code → Unity MCP bridge registration
```

## How to run things

### Server (Nakama + Postgres)

```sh
docker compose -f server/docker-compose.yml up -d --build   # build module + start
docker compose -f server/docker-compose.yml logs -f nakama  # look for "wuxia module loaded"
docker compose -f server/docker-compose.yml down            # stop (add -v to wipe the database)
```

- Ports: `7350` HTTP/WebSocket API (clients use this), `7349` gRPC, `7351` developer console (http://127.0.0.1:7351, default login `admin` / `password`), `5432` Postgres.
- Server key is Nakama's default `defaultkey` — local dev only.
- After changing `server/src/**`, re-run the `up -d --build` command; the module is compiled inside the Docker build.
- Type-check locally without Docker: `cd server && npm install && npm run typecheck`.
- Module rules: `tsc` compiles everything into a single `build/index.js` (`outFile`, ES5 target). **No `import`/`export`** — files share one global scope, and Nakama calls the global `InitModule`. Add new files to `files` in `server/tsconfig.json`. Runtime types come from `nakama-runtime` (heroiclabs/nakama-common), pinned to the tag that matches the Nakama image version — bump both together.

### Client (Unity)

- Editor version: **Unity 6000.3.27f1** (Unity 6.3 LTS), installed at `C:\Program Files\Unity\Hub\Editor\6000.3.27f1`, with Android Build Support. Created from the Universal 3D (URP) template. Open `client/` via Unity Hub.
- Android SDK/NDK/OpenJDK are **not installed yet** (the editor and Android Build Support were installed from Unity's standalone installers, not through Hub). Add them before the first Android build (M1): Hub → Installs → 6000.3.27f1 → Add modules if Hub offers it, otherwise install them separately and point **Preferences → External Tools** at them.
- **No physical test device yet** (no Android phone, no iPhone/Mac). Test in the Editor with the **Device Simulator** (mid-range Android profile, touch input). On-device performance checks are deferred until an Android phone is bought; keep things mobile-friendly anyway (URP mobile quality tier, low draw calls). Keep code platform-neutral so iOS stays possible later.
- Main scene: `Assets/Scenes/OakValley.unity` (build index 0), the offline M1 hunting zone: player, 3 wolf dens of 5, the Han (respawn point), HUD.
- `Assets/Scenes/SampleScene.unity` keeps the M0 smoke test: `Assets/Scripts/Net/NakamaLoginTest.cs` on the `NakamaLoginTest` GameObject device-authenticates against `127.0.0.1:7350` on Play and logs `[Nakama] Logged in as …`. Start the server first. `M0_Cube` there is the M0 MCP test object; delete it freely.
- The Editor's active quality level is **Mobile** (same tier Android uses), so Play mode reflects phone settings.
- Player settings: landscape only (auto-rotates between the two landscape sides). `ZoneSession` sets `Application.targetFrameRate = 60`.

#### Play-testing Oak Valley (M1)

1. Open `Assets/Scenes/OakValley.unity`, switch the Game view tab to **Simulator** and pick a mid-range Android phone; rotate it to landscape.
2. Press Play. Controls (touch only, the mouse acts as one finger in the Simulator):
   - Touch and drag on the **left ~45%** of the screen: floating joystick (camera-relative).
   - Drag anywhere else: turn the camera.
   - **Tap a wolf** to lock on: you run to it and auto-attack. Or press **Attack** to lock the nearest wolf within 15 m. Moving with the joystick cancels the run-in but keeps the lock.
3. HUD: player HP (top-left), locked target (top-centre), `Wolves slain: N` (top-right). Floating numbers show damage, `N!` crits and `Miss`.
4. Dying shows a 5 s overlay, then you revive at the Han (south end). HP regenerates after 5 s without taking damage. Wolves respawn 10 s after their corpse fades.
5. The Console logs `[Combat] Grey Wolf slain in Oak Valley (N total)` per kill and `[Combat] … defeated / revived` on death. Any red error is a bug.
- In the plain Game view the mouse is turned into touches by `TouchSimulation` (Editor only); the Input System skips that while the Device Simulator is open, so there's no double input.

#### Game data and tests

- Combat numbers (player and monster HP, damage, accuracy, dodge, crit, ranges, speeds, aggro/leash, respawn timers) live in **`shared/gamedata/combat.json`**. `ZoneSession` loads it through a TextAsset reference to `Packages/com.wuxia.gamedata/combat.json`. Edit the JSON and re-enter Play; no code change needed. Field names are mirrored in `Assets/Scripts/Data/CombatData.cs`; keep them in sync.
- Basic-attack rules are in `Wuxia.Combat.CombatMath` (pure C#, no Unity types) so the same formula can move to the server in M4: hit chance = accuracy − dodge, clamped to 5–99%; damage = roll(min..max) × crit multiplier − defense, at least 1.
- EditMode tests: **Window → General → Test Runner → EditMode → Run All**, or through MCP `run_tests` (mode EditMode, assembly `Wuxia.Tests.EditMode`). They cover `CombatMath` and sanity-check `combat.json`.
- Layers: `Player` (8), `Monster` (9). Monster colliders sit on `Monster` for tap picking; the camera and NavMesh ignore both layers.
- Wolves use a NavMesh baked from `NavMesh/NavMeshSurface` in the scene (asset at `Assets/Scenes/OakValley/`). Rebake after moving level geometry (select `NavMesh` → **Bake**, or `NavMeshAssetManager.instance.StartBakingSurfaces` through MCP).
- Headless compile check (Editor must be closed for this project): `"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" -batchmode -quit -projectPath client -logFile -` and look for `error CS`.
- On a physical phone, `127.0.0.1` is the phone itself: set the host to the dev PC's LAN IP (and allow HTTP in Player settings until we move to TLS).

### Unity MCP bridge (Claude Code ↔ Unity Editor)

- Package: MCP for Unity (CoplayDev, `com.coplaydev.unity-mcp`, pinned in `client/Packages/manifest.json`). Its Python server runs through `uvx` (needs `uv` installed).
- Transport: HTTP at `http://127.0.0.1:8080/mcp`, registered in `.mcp.json` as `UnityMCP`.
- To connect: open `client/` in Unity — the package auto-starts the server (EditorPref `MCPForUnity.AutoStartOnLoad`, set on this machine; on a new machine tick auto-start in **Window → MCP for Unity** or click **Start Server**). Then start Claude Code from the repo root. Check with `/mcp`.
- Ready signals: Unity Console shows `MCP-FOR-UNITY: Session connected`; `curl http://127.0.0.1:8080/mcp` answers (406 is fine). The bridge only exists while the Editor is open.
- On each Editor start the package also writes a matching local-scope `UnityMCP` entry for this repo into `~/.claude.json`. That's expected and harmless (same URL).
- Don't use the Unity window's **Configure** button for Claude Code: it removes `UnityMCP` from every scope (including this repo's `.mcp.json`) and re-adds it at local scope.
- Per the design doc, scenes and prefabs are wired **through MCP**, not by hand-editing `.unity`/`.prefab` YAML. Bulk greybox work (scatter, prefab meshes, HUD layout) is easiest with `execute_code` (in-editor C#; its compiler is C# 6 with no `using UnityEngine.UI`, so fully qualify UI types and write `UnityEngine.Object`).
- MCP screenshots default to `Assets/Screenshots`; pass `output_folder: "Temp/Screenshots"` so they don't land in the project. A capture without a `camera` can show a cyan world when the Game view isn't focused; pass `camera: "Main Camera"` (that leaves out the overlay HUD).

## Conventions

- **Server-authoritative.** Anything that affects gold, items or combat outcomes is decided on the server; the client only predicts visuals. Upgrade rolls, trades and market actions are single atomic server calls, and every gold/item change is logged.
- **Shared game data** (skills, items, monsters, drop tables, odds) lives in one set of JSON files under `shared/gamedata/`, loaded by both client and server, so numbers never drift. Never hard-code a balance number in C# or TypeScript.
- **Git LFS** tracks binary assets (textures, models, audio, fonts, native libs) — see `.gitattributes`. Run `git lfs install` once per clone. Never commit `client/Library/`, `Temp/`, `Logs/`, `UserSettings/` or builds.
- Unity: keep `.meta` files with their assets; C# under `Assets/Scripts/`, namespace `Wuxia.<Area>`.
