# Wuxia MMO — Claude Code guide

A mobile-first 3D wuxia MMO-lite, currently being built as a **vertical slice**: 2 characters, levels 1–30, a shared town hub, 2 hunting zones, 1 dungeon, 1 faction war. Unity 6 client (C#), Nakama server (TypeScript runtime) on PostgreSQL.

**`docs/design.md` is the source of truth.** It holds the pillars, locked decisions, game systems, architecture and the build milestones (M0–M9) with their "done when" criteria. Read the relevant section before starting a milestone. If code and the doc disagree, the doc wins — or flag the conflict and ask; don't silently diverge. Names (Nine Peaks, Lantern Ford, …) are placeholders.

## Folder layout

```
docs/design.md        Design doc — source of truth
client/               Unity 6 project (URP, Android target)
  Assets/Scripts/     Game C# code (namespace Wuxia.*)
  Packages/manifest.json  UPM deps: Nakama client, MCP for Unity
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
- Main scene: `Assets/Scenes/SampleScene.unity`. `M0_Cube` in it is the M0 MCP test object; delete it freely.
- `Assets/Scripts/Net/NakamaLoginTest.cs` — M0 smoke test; on Play it device-authenticates against `127.0.0.1:7350` and logs `[Nakama] Logged in as …` to the Console. It sits on the `NakamaLoginTest` GameObject in the main scene. Start the server first.
- Headless compile check (Editor must be closed for this project): `"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" -batchmode -quit -projectPath client -logFile -` and look for `error CS`.
- On a physical phone, `127.0.0.1` is the phone itself: set the host to the dev PC's LAN IP (and allow HTTP in Player settings until we move to TLS).

### Unity MCP bridge (Claude Code ↔ Unity Editor)

- Package: MCP for Unity (CoplayDev, `com.coplaydev.unity-mcp`, pinned in `client/Packages/manifest.json`). Its Python server runs through `uvx` (needs `uv` installed).
- Transport: HTTP at `http://127.0.0.1:8080/mcp`, registered in `.mcp.json` as `UnityMCP`.
- To connect: open `client/` in Unity — the package auto-starts the server (EditorPref `MCPForUnity.AutoStartOnLoad`, set on this machine; on a new machine tick auto-start in **Window → MCP for Unity** or click **Start Server**). Then start Claude Code from the repo root. Check with `/mcp`.
- Ready signals: Unity Console shows `MCP-FOR-UNITY: Session connected`; `curl http://127.0.0.1:8080/mcp` answers (406 is fine). The bridge only exists while the Editor is open.
- On each Editor start the package also writes a matching local-scope `UnityMCP` entry for this repo into `~/.claude.json`. That's expected and harmless (same URL).
- Don't use the Unity window's **Configure** button for Claude Code: it removes `UnityMCP` from every scope (including this repo's `.mcp.json`) and re-adds it at local scope.
- Per the design doc, scenes and prefabs are wired **through MCP**, not by hand-editing `.unity`/`.prefab` YAML.

## Conventions

- **Server-authoritative.** Anything that affects gold, items or combat outcomes is decided on the server; the client only predicts visuals. Upgrade rolls, trades and market actions are single atomic server calls, and every gold/item change is logged.
- **Shared game data** (skills, items, monsters, drop tables, odds) will live in one set of JSON files loaded by both client and server, so numbers never drift.
- **Git LFS** tracks binary assets (textures, models, audio, fonts, native libs) — see `.gitattributes`. Run `git lfs install` once per clone. Never commit `client/Library/`, `Temp/`, `Logs/`, `UserSettings/` or builds.
- Unity: keep `.meta` files with their assets; C# under `Assets/Scripts/`, namespace `Wuxia.<Area>`.
