# Illyria Online

A mobile-first 3D MMO-lite rooted in Albanian folklore: kreshnik heroes out of the old mountain songs, the zanas who lend them strength, drangue leaps and the Kulshedra. Built as a vertical slice with a Unity 6 client (C#) and a Nakama server (TypeScript) on PostgreSQL.

The slice: 2 characters, levels 1–30, a shared town hub, 2 hunting zones, 1 dungeon and 1 faction war. The full design, locked decisions and milestones are in [`docs/design.md`](docs/design.md).

## Status

| Milestone | | |
| --- | --- | --- |
| M0 Setup | Unity project, Nakama in Docker, Unity MCP bridge | Done |
| M1 Offline core | Oak Valley zone, touch controls, wolves, target-lock combat, death and respawn | Done (on-device check pending) |
| M2 Skills | Spirit, skill books and points, sword/bow/sicas/staff forms, statuses, Drangue Leap | Done |
| M3 Loot and inventory | Gold and item drops, auto-loot, bag, 6 gear slots, shop NPC, potions | Done |
| M4 Online | Nakama login and saves, town hub, server-run zones and combat | Next |

M5–M9 (blacksmith, trading, content, banners and war, polish) follow; see the design doc.

## Repository layout

```
docs/design.md     Design doc, the source of truth
client/            Unity 6 project (URP, Android target)
shared/gamedata/   Game data JSON (combat, skills, items) shared by client and server
server/            Nakama server module and Docker setup
art-source/        Full-size original models
```

## Getting started

- **Git LFS** stores models, textures and audio: run `git lfs install` once before cloning.
- **Client:** open `client/` in Unity 6000.3.27f1 (Unity 6.3 LTS) with Android Build Support, open `Assets/Scenes/OakValley.unity` and press Play. Use the Device Simulator in landscape for touch controls.
- **Server:** `docker compose -f server/docker-compose.yml up -d --build` starts Nakama and PostgreSQL locally (developer console at http://127.0.0.1:7351). The keys and passwords in the repo are local-development defaults only.

[`CLAUDE.md`](CLAUDE.md) has the detailed developer guide: how to play-test each milestone, where game data lives, running tests and the Unity MCP bridge.

## Credits

- Animations: [Quaternius Universal Animation Library](https://quaternius.com) (CC0).
- Wolf: "Wolf" by Quaternius via [Poly Pizza](https://poly.pizza/m/P1gU3Qkr9r) (CC0).
