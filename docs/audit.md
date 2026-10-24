Integration report: AI asset and code pipeline in the Kreshnik (wuxia) Unity project

I only read files. Nothing in the repo was changed. The one thing I ran was the server unit tests, compiled into the scratchpad instead of server/build-test/.

1. Project inventory

┌────────────┬─────────────────────────────────────────────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────┐
│    Area    │                                                 Finding                                                 │                      Evidence                       │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Engine     │ Unity 6000.3.27f1 (6.3 LTS)                                                                             │ client/ProjectSettings/ProjectVersion.txt           │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Render     │ URP 17.3.0, Linear colour space, two tiers: Mobile_RPAsset (Android) and PC_RPAsset (Standalone)        │ manifest.json; ProjectSettings.asset:50;            │
│ pipeline   │                                                                                                         │ QualitySettings.asset:51,61,105,115                 │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Target     │ Android, IL2CPP, ARM64 only, min SDK 25, landscape. The bundle id is still the URP template's           │ ProjectSettings.asset:172-184,274,835               │
│            │ com.UnityTechnologies.com.unity.template.urpblank                                                       │                                                     │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Key        │ glTFast 6.20.0, Nakama 3.22.1, MCP for Unity v10.3.0, AI Navigation 2.0.16, Input System 1.20.1, Test   │ client/Packages/manifest.json                       │
│ packages   │ Framework, local com.wuxia.gamedata                                                                     │                                                     │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Scenes     │ OakValley.unity (build index 0), SampleScene.unity (the M0 login test)                                  │ EditorBuildSettings.asset                           │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│            │ One runtime assembly, Wuxia.asmdef: Combat, Data, Input, Items, Monsters, Net, Player, UI, World.       │                                                     │
│ Code       │ Editor tools in Wuxia.Editor (OakValleyBuilder, PlayerActionBuilder, ItemIconGenerator,                 │ client/Assets/Scripts/**                            │
│            │ WalkableGridExporter, AtmosphereShots)                                                                  │                                                     │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Server     │ Nakama TypeScript module plus Postgres in Docker; unit, integration and bot tests                       │ server/src, server/tests                            │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Shared     │ combat, skills, items and zones JSON, plus navgrid/oak_valley.json                                      │ shared/gamedata/                                    │
│ data       │                                                                                                         │                                                     │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Art in     │ 2 characters (glb, Humanoid avatars), 4 monsters (Wolf, DenWolf, Boar, Bandit), 2 weapon models,        │ Assets/Characters, Monsters, Items, JMO Assets,     │
│ game       │ Cartoon FX Free VFX, Mixamo and Quaternius animation clips                                              │ ThirdParty                                          │
├────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────┤
│ Art        │ GameAssets.blend, Boar.blend, original glbs, glb_extract_images.py, make_boar.py, make_bandit.py        │ art-source/                                         │
│ sources    │                                                                                                         │                                                     │
└────────────┴─────────────────────────────────────────────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────┘

Built so far: M0 to M4. Git history runs 03cab19 (M0) to 69dd091 (combat VFX).

What I could and couldn't confirm this session:

┌──────────────────┬──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│      Status      │                                                                         Item                                                                         │
├──────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ ✅ Confirmed     │ The server unit tests pass: 67 passed, 0 failed (rules, items, ZoneSim and walkable grid against the real JSON).                                     │
├──────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ ⚠️ Not confirmed │ Unity EditMode tests and Play mode. The Unity MCP bridge refused the connection (ECONNREFUSED), so the Editor is closed or the bridge isn't running. │
├──────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ ⚠️ Not confirmed │ The integration test against a live Nakama (needs Docker running).                                                                                   │
├──────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ ❌ Never done    │ An Android build: the SDK and NDK aren't installed (CLAUDE.md). M1's "done when" needs a phone, which you don't have yet.                            │
├──────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ ⚠️ Out of date   │ README.md still lists M4 as "Next" and calls the game "Illyria Online"; design.md uses "Kreshnik".                                                   │
└──────────────────┴──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

2. How the project works today

Importing 3D assets
- Every model comes in as a .glb through glTFast. All 7 game glbs use the glTFast ScriptedImporter. The exception is GreyWolf.glb, which uses DefaultImporter and isn't used.
- glTFast can't compress textures embedded in a glb. So textures are split out with art-source/glb_extract_images.py, which keeps the glb's GUID and fileIDs. Unity's texture importer then compresses them: 1024 px max for characters, 512 for items, ASTC 6×6 on Android and iOS. Example: Kreshnik_BaseColor.jpg.meta has maxTextureSize: 1024 and textureFormat: 50 for Android.
- The originals, with textures still embedded, are kept in art-source/.
- Monster recolours are done as JSON-only glb edits (make_bandit.py) or Blender scripts (make_boar.py).

Rigging and animation
- glTFast only creates Generic rigs. The Humanoid avatars (KreshnikAvatar.asset, ZanasChosenAvatar.asset) were built with AvatarBuilder.BuildHumanAvatar from a bone map, after posing a temporary copy into a T-pose. No script for this is in the repo: only CLAUDE.md describes the method, so it was done ad hoc.
- Player actions all come from one table, PlayerActions.asset. PlayerActionBuilder regenerates the action layers of Kreshnik.controller from it; both characters share that controller.
- Clips are Mixamo (X Bot, imported without skin) plus the Quaternius library, which needs a 180° root offset.
- Runtime code finds hands only through HumanBodyBones.RightHand and LeftHand (PlayerAppearance.cs:92-107). Any rig with a valid Humanoid avatar works without code changes.
- Monsters are Generic rigs. Each species gets a copy of Wolf.controller (parameters Gait, Playback, Attack, Dead). Attack and death clips are non-looping .anim copies, and walk and run clip speeds are measured per model.

Building gameplay features
- The rules are plain C# (Wuxia.Combat, Wuxia.Items) and are mirrored in server TypeScript (server/src/combat.ts, items.ts).
- Balance numbers live only in shared/gamedata/*.json.
- Scenes and prefabs are wired through MCP, never by editing the YAML by hand.
- Generated content (terrain, scatter, ledges, areas, spawners) comes from OakValleyBuilder.cs, which is about 1,960 lines of layout code.

3. Systems to keep

1. glb, glTFast and split-out textures. The hit flash depends on it: it writes glTFast's emissiveFactor and URP's _EmissionColor (CombatEffects.cs:59-60).
2. Humanoid retargeting plus PlayerActionSet and PlayerActionBuilder. New characters inherit every attack, skill, hit, death, leap and potion action for free.
3. ItemModelRegistry (ItemModels.asset), its grip rules (+Z blade; bows posed on the prefab root), and ItemIconGenerator.
4. OakValleyBuilder as the owner of Environment/Generated. A rebuild replaces it, so anything placed there by hand is lost.
5. The phone budget systems: ScatterBatcher (merges per cell and per material), the shared Foliage material with its palette texture, and the Wuxia/Foliage occluder-fade shader.
6. The chain after a world change: NavMesh bake → walkable grid export → zones.json → server image rebuild. NavGridTests fails if the grid is out of date.
7. Server authority and shared JSON, plus the server module rules: no import, file order set in tsconfig.
8. Git LFS rules (.gitattributes covers glb, fbx, blend, png and jpg) and a License.txt per model.

4. Pipeline integration map

1. World Model: world-building and layouts

- What already supports it: docs/art/oak-valley/*.png (six per-area mood boards that drive the AtmosphereProfile assets), the "Oak Valley layout" section of design.md, OakValleyBuilder.cs, ZoneArea, and zones.json.
- How its output gets in: layouts have to be turned into builder code (valley edges, pads, landmark spots, scatter rules) and into zones.json (packs, ledges, respawn, areas). Mood images go to docs/art/<zone>/ and are matched with Wuxia → World → Capture Atmosphere Shots.
- What's missing: there's no builder for any zone other than Oak Valley. Serpent Marsh, Dardana and the cave would each need one. Your memory notes also say Dardana waits on an art-direction decision.
- Compatibility risk: the project has no splat or point-cloud rendering. Any scene geometry from the World Model would have to be rebuilt as low-poly meshes or terrain. Treat its output as reference and layout, not as shipped geometry.

2. Claude: design and architecture

- What already supports it: design.md (the source of truth), CLAUDE.md, server/PROTOCOL.md.
- Conflicts to settle in the docs, which is your decision:
  - design.md "Decisions locked" says "Unity Asset Store characters, animations and effects", and M9 says "Art pass with store assets". That contradicts Tripo/Meshy.
  - The "Dev setup" section says "Claude Code edits the C# and TypeScript", and the milestones are titled "for Claude Code". That contradicts Codex doing the programming.
  - CLAUDE.md says the doc wins, so update these lines before the pipeline goes live.

3. Tripo AI / Meshy: asset generation

- First real sample: the untracked art-source/KreshnikBase.glb is a Tripo export (the glTF generator field says "Tripo"). Here it is next to the current in-game model:

┌────────────┬────────────────────────────────────────────────────────────────────────────┬──────────────────────────────────────────────┐
│            │                          KreshnikBase.glb (Tripo)                          │             In-game Kreshnik.glb             │
├────────────┼────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────┤
│ Triangles  │ 18,543                                                                     │ 31,076                                       │
├────────────┼────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────┤
│ Bones      │ 61: Unity-style names (Left_UpperArm, Chest, UpperChest), fingers and eyes │ 24 (Spine02, Spine01, headfront), no fingers │
├────────────┼────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────┤
│ Textures   │ 3 embedded: base colour, metallic-roughness (2048² PNG), normal            │ 1 base colour                                │
├────────────┼────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────┤
│ Material   │ doubleSided: true                                                          │ —                                            │
├────────────┼────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────┤
│ Extensions │ KHR_materials_volume, FB_ngon_encoding (both optional)                     │ —                                            │
├────────────┼────────────────────────────────────────────────────────────────────────────┼──────────────────────────────────────────────┤
│ Animations │ none                                                                       │ 1 clip, Armature|clip0|baselayer             │
└────────────┴────────────────────────────────────────────────────────────────────────────┴──────────────────────────────────────────────┘

  The current characters were exported from Blender glTF I/O 4.5. Their bone names and clip name look like Meshy's auto-rigger, but that's my inference, not something the files record.
- What's needed on the way in: keep everything glb, and store the originals in art-source/.
- What's missing:
  - No polygon, texture or bone budget is written down anywhere. Today's characters are 31k triangles, the wolf 1,962, the items 1.5–1.9k.
  - No convention for recording the generator and licence. The monsters have License.txt; generated assets need the same, with terms that depend on your plan.
- Compatibility risks:
  - As far as I know, Tripo and Meshy auto-rig mainly humanoids. Quadrupeds (wolves, boars, the Kulshedra) use Generic rigs here and may need rigging by hand in Blender. Check what your plans support.
  - FBX exports would bypass the glb workflow.

4. Blender: cleanup, retopology, materials, rig prep

- What already supports it: Blender 5.0 with the MCP addon, GameAssets.blend (one collection per model), the scripts above, and the axis rules in CLAUDE.md (Z-up → +Y, −Y → +Z, weapon grip at the origin).
- What to do to every Tripo/Meshy model before export:
  - Remove KHR_materials_volume.
  - Turn off doubleSided unless it's needed: it costs fill rate and shadows.
  - Decide whether to keep the eye and finger bones. Fingers will retarget, which changes how the hand holds the weapon.
  - Bake textures to ≤1024 (characters) or ≤512 (items).
  - Export a glb, then run glb_extract_images.py with one name per image, e.g. _BaseColor _MetallicRoughness _Normal.
- Texture import settings: metallic-roughness is data, so turn sRGB off. The normal map needs the Normal map texture type. Keep glTFast's own materials: they read glTF's packed channels (roughness in G, metal in B) correctly, while URP Lit expects a different packing.

5. Codex: gameplay programming

- What already supports it: clear assembly and namespace layout, pure-C# rules with EditMode tests, npm test, the integration test, and the bot.
- What's needed:
  - Codex reads AGENTS.md, and there isn't one. All the conventions are in CLAUDE.md, so Codex won't see them unless pointed there.
  - .mcp.json is Claude Code's format. Codex needs its own MCP config pointing at the same Unity bridge (http://127.0.0.1:8080/mcp). Without it, Codex can't wire scenes through MCP and would end up editing YAML, which the project forbids.
- Rules Codex must follow:
  - Server: no import/export, list files in dependency order in tsconfig, RPC and match handlers are top-level named functions, globals are frozen after load.
  - Never hard-code a balance number; it goes in the shared JSON.
  - Keep CombatData.cs, SkillData.cs and ItemData.cs in sync with the JSON.
  - The headless compile check needs the Editor closed.
- Risk: Claude and Codex driving the same Editor, bridge and working tree at once. Give each agent its own branch, and only one of them at a time should use the Editor.

6. Unity: integration, animation, testing, polish

- What already supports it: MCP wiring, the builders, EditMode tests, Device Simulator play-testing, and the atmosphere shots.
- Risk: Play mode on the Standalone target renders on the PC tier (Mobile is excluded for Standalone in QualitySettings.asset:61). Art judged in Play mode looks better than it will on a phone. Check it with Mobile_RPAsset, as CLAUDE.md describes.

5. Gaps and blockers

1. A new character model can't simply replace the old one. Kreshnik.prefab is a variant of Kreshnik.glb, it's nested in Player.prefab (m_SourcePrefab at line 1984), and Player.prefab's looks[].model points at Animator fileIDs inside it (lines 1368–1386). OakValley.unity also references it for remote players. A model with a different structure gets new glTFast sub-asset IDs, which breaks all of those references. Import it as a new asset, make a new variant, and rewire it through MCP.
2. The avatar builder isn't in the repo. Every new humanoid needs a Humanoid avatar made from a bone map. Tripo's names map almost one-to-one onto HumanBodyBones, but the tool itself has to be written first.
3. Weapons have to be refitted. The WeaponPivot and OffHandPivot poses and WornBow.prefab's root hand pose were fitted to the current rig's hands. The off-hand mirror rule assumes the left-hand axes are the right's with X flipped; that has to be checked on Tripo's rig.
4. Environment props have three constraints:
   - They must be placed by OakValleyBuilder (otherwise a rebuild deletes them).
   - Each new material adds a draw per scatter cell.
   - Leaves with alpha-clip or normal maps break the Wuxia/Foliage depth-pass assumptions.
   - Also, scatter is merged at runtime with CombineMeshes, and in player builds that may need readable meshes. Check this before putting glTFast-imported meshes into scatter.
5. Solid props affect the server. Anything solid (Obstacle layer 11) changes the NavMesh, which means a rebake, a grid export, a zones.json update and a server rebuild.
6. No toolchain or device: the Android SDK and NDK aren't installed and there's no phone, so the asset budgets can't be checked on hardware.
7. Documents contradict the pipeline (section 4, stage 2), and README.md is out of date.
8. The bundle id is still the template's. It must change before any store test (M9).

6. Recommended integration order

1. Docs and agent setup. No game risk, and it unblocks everything else.
   - Record the pipeline in design.md: the "Look" decision, "Dev setup" and the M9 wording.
   - Add asset budgets to CLAUDE.md.
   - Add an AGENTS.md that points Codex at CLAUDE.md and design.md, with its MCP config.
   - Run EditMode tests and npm test to get a green baseline.
2. Pilot with a static item, such as a generated weapon. It has no rig and no server impact; it only touches ItemModels.asset, a prefab and the icon atlas. It proves the whole path: generate → Blender → glb → split textures → importer settings → glTFast material → grip rules → icons → ground drop.
3. Character pilot with KreshnikBase.glb.
   - Blender cleanup (step 4 above).
   - Write and commit the avatar builder.
   - Make a new variant alongside the current one.
   - Refit the weapon pivots.
   - Check every PlayerActions state, then swap it into Player.prefab and the remote-player prefab.
4. Monsters. They depend on the Generic rig and controller template, per-model clip speeds and License.txt. They can reuse the Wolf template.
5. Environment props through OakValleyBuilder. This comes last because it triggers the NavMesh → grid → zones.json → server chain and has the strictest draw-call rules.
6. World Model for new zones. Use it to plan Serpent Marsh and the cave as builder code and zones.json. Dardana stays blocked until its art direction is decided.
7. Install the Android toolchain and test on a phone as soon as you can. Until then, every budget is an estimate from the Editor.



Plan: close the audit's findings before new milestones

Everything that runs today passes its tests. The open problems fall into three groups:
- the docs don't match your pipeline yet;
- some tooling the pipeline will need is missing;
- some checks have never been done (any build, mobile budgets, part of M4).

Below are the phases in dependency order. Each one ends in a check you can run.

Phase 0: Your decisions (they block phases 1 and 3)

1. How the design doc describes the pipeline. design.md currently says "Unity Asset Store characters" ("Decisions locked", M9) and "Claude Code edits the C# and TypeScript" (Dev setup). I'd replace those with your 6-stage pipeline.
2. Asset budgets. I propose the numbers below, based on what's in the project today:

┌──────────────────┬──────────────────────────────────────────┬─────────────────────────────┬──────────────┐
│      Asset       │                Triangles                 │          Textures           │    Other     │
├──────────────────┼──────────────────────────────────────────┼─────────────────────────────┼──────────────┤
│ Player character │ ≤20k, plus a ~5k LOD1 for remote players │ 1024                        │ ≤2 materials │
├──────────────────┼──────────────────────────────────────────┼─────────────────────────────┼──────────────┤
│ Monster          │ ≤5k                                      │ 512                         │ —            │
├──────────────────┼──────────────────────────────────────────┼─────────────────────────────┼──────────────┤
│ Weapon or item   │ ≤2k                                      │ 512                         │ —            │
├──────────────────┼──────────────────────────────────────────┼─────────────────────────────┼──────────────┤
│ Prop             │ ≤3k                                      │ shared atlas where possible │ —            │
└──────────────────┴──────────────────────────────────────────┴─────────────────────────────┴──────────────┘

- Why the LOD1: with the proposed 50 players per channel, today's 31k-triangle characters would put about 1.5M triangles on screen.
3. Bundle id. The project still uses the URP template's id. It depends on the title, which is still an open question. A placeholder such as com.<you>.kreshnik works for now.
4. M4's town. M4's "done when" says "two phones see each other in town", and Dardana doesn't exist yet. Do you want to build it before M5 (it could be the first full run of your pipeline), or close M4 without it and record that?

Phase 1: Docs and agent setup

- design.md: update the pipeline wording you approve in Phase 0.
- CLAUDE.md: add an AI asset intake section covering:
  - the budgets;
  - the Tripo/Meshy → Blender → glb checklist: strip KHR_materials_volume, turn off doubleSided, split textures, metallic-roughness with sRGB off, normal map as Normal type;
  - a provenance rule: a License.txt per model naming the tool, plan and date.
- AGENTS.md for Codex: point it at CLAUDE.md and design.md, and list the non-negotiables:
  - the server module rules;
  - balance numbers only in the shared JSON;
  - scenes changed through MCP, never by editing the YAML;
  - the test commands.
- Codex MCP config (your user config, outside the repo) pointing at http://127.0.0.1:8080/mcp.
- Done when: a fresh Codex session can read the OakValley hierarchy through MCP and run npm test.

Phase 2: Automated checks for assets

New EditMode tests, so every imported asset is checked automatically:
- Import rules:
  - every .glb under Characters, Monsters and Items uses glTFast;
  - every texture next to one has ASTC overrides for Android and iOS, a size within budget, and the right sRGB and normal-map settings.
- Budgets: triangles, bones and materials per category. The current 31k characters get a named exception until they're replaced.
- Provenance: every model folder has a License.txt. Today Characters/Kreshnik, Characters/ZanasChosen and Items/Weapons/* have none, so you'd write those first.
- Smoke test: a committed one-click version of the Play-mode check I ran (lock a wolf, kill it, loot, no errors), offline and online. You can re-run it after every asset swap.
- Done when: all of these pass, with only the listed exceptions.

Phase 3: Builds (nothing has ever been built)

1. Windows development build now. It proves the project builds outside the Editor. It also checks one specific risk: ScatterBatcher merges meshes at runtime, which may need readable meshes in a built player.
   - Done when the built game plays Oak Valley online with a clean Player.log.
2. Install the Android SDK, NDK and OpenJDK (Hub → Add modules). Set the bundle id, then build an APK. You don't need a phone to confirm it compiles with IL2CPP for ARM64 and that ASTC compression and shader stripping work.
3. Switch the Editor's build target to Android. Play mode then renders on the Mobile quality tier, so art is judged the way a phone shows it.
   - Done when the APK builds without errors and EditMode tests are still green on the Android target.

Phase 4: Tooling the pipeline needs

- Humanoid avatar builder as a committed editor tool. Today's avatars were built ad hoc.
  - Check it by rebuilding KreshnikAvatar from the current glb and confirming every action in PlayerActions plays the same.
- glb cleanup step for Tripo/Meshy exports: strip the unneeded extensions, turn off double-sided, split all textures with names (an extension of glb_extract_images.py).
- Texture importer presets for colour, data and normal maps.
- Done when: the avatar tool reproduces the current avatar, and art-source/KreshnikBase.glb comes out clean in one command.

Phase 5: Test run of the whole pipeline on low-risk assets

1. A generated weapon through Tripo/Meshy → Blender → ItemModels.asset → icon → ground drop. It has no rig and no effect on the server.
2. KreshnikBase.glb as a separate variant (KreshnikTripo.prefab):
   - Build its avatar and refit the weapon pivots.
   - Check that the left hand mirrors the right on Tripo's rig.
   - Play every action state with every weapon, then try it as a remote player.
   - Only swap it into Player.prefab once you approve it.
- Done when: both play in-game, the Phase 2 checks pass, and the smoke test is green.

Phase 6: Milestone loose ends

- Dardana, depending on your Phase 0 answer. It's the natural first job for the World Model stage.
- The untested M4 case: the connection dropping while the character select is open.
- M1's "on an Android phone at a steady frame rate": stays open until you buy a phone. Record it as deferred, not done.

Who does what: Phases 1–4 are code and docs that Claude or Codex can do. Phase 3's install and Phase 5's generation and Blender work need you. Each phase is its own commit on its own branch, and only one agent drives the Unity Editor at a time.



wait listen. what dont worry about the 3d models atm. Can we focus on the parts where we can resolve coding issues or whatever? like there were alot o fthing that u said needed to be fixed?

Fair point, and I should be straight about it: most of what I flagged is about bringing generated 3D models into the game. On the code side the audit found very little, since 171 tests pass and online play works. Let me make the short list of real code items concrete before proposing anything.

  Searched for 1 pattern

All 73 generated meshes are marked readable, so the build risk I raised for ScatterBatcher doesn't apply. Checking the reconnect path for the one untested M4 case:

  Read 1 file

You're right to push on this. Here's the honest picture: almost everything I flagged was about bringing generated 3D models into the game. On the code side the audit found very little wrong: 171 tests pass, and online play worked when I drove it. With the art items set aside, this is what's left.

One risk turned out not to be real: I'd said ScatterBatcher might fail in a built game because it merges meshes at runtime. I've now checked: all 73 generated meshes are marked readable, so that's fine.

Code issues worth fixing

1. Tapping a far wolf does nothing. This is a real bug, small but visible.
- A tap picks monsters up to 80 m away (PlayerCombat.cs:409).
- But the client drops any target beyond loseTargetRange, which is 30 m (combat.json:12, checked at PlayerCombat.cs:107).
- So tapping a wolf 30–80 m away locks it and drops it on the next frame. This is what happened to me in Play mode.
- My suggested fix: ignore taps on monsters farther than loseTargetRange, reading the number from the JSON rather than hard-coding it.

2. The untested M4 case: the connection drops while the character select is open.
- There is a code path for it (NetZone.cs:333-336), but it has never been tested.
- I'd test it by restarting Nakama while the select screen is up.
- While reading that code I also noticed Choose sets playing before the join succeeds (NetZone.cs:238). If a join fails, a later reconnect could rejoin as a character you didn't actually enter with. I'd confirm that in the same test before changing anything.

Checks that have never been done

3. A Windows development build. No build of any kind has ever been made. It's the cheapest way to catch Editor-only code leaking into the game or shader problems, and it needs nothing installed.

4. An automated smoke test. Today the "lock a wolf, kill it, loot it, no errors" check is something I drive by hand through MCP. Committing it as a one-click Editor command means you, or Codex, can re-run it after any change.

5. An Android build. It needs you to install the SDK, NDK and OpenJDK through Unity Hub, plus a bundle id; it still has the template's. Only then can I build an APK.

Workflow (no game code)

6. AGENTS.md for Codex, so it follows the same rules as CLAUDE.md, plus its MCP config for the Unity bridge.

Proposed order

1 and 2 → 3 → 4 → 6, then 5 once the SDK is installed. Each is a small separate commit. Should I start with item 1 using the fix above, or would you rather a far tap make the player run toward the wolf?