# 3D assets — how every model is made

Every character, monster, weapon and item model goes through the same steps against a written spec, so models match each other and fit a phone. **Fill in an asset's spec card before generating it**, and check the result against the card before it goes into the game.

## The pipeline

1. **Concept image.** One clean image per asset, made to the style sheet below: plain light-grey background, even lighting, no strong shadows, the whole asset in frame. Characters stand in an **A-pose** (arms ~45° down, legs slightly apart), facing the camera, **holding no weapon**; weapons are separate items attached to the hand. Make a side view too when the generator accepts several images.
2. **Tripo AI, image to 3D.** Generate from the concept image, not from a text prompt alone. Ask for the card's face limit at generation time rather than decimating heavily later. Characters: use Tripo's humanoid auto-rig. Export **glb**.
3. **Original into `art-source/`.** Keep the raw Tripo export, untouched, as `art-source/<Name>Tripo.glb`, plus the concept image as `art-source/concepts/<Name>.png`.
4. **Blender cleanup** (`art-source/GameAssets.blend`, one collection per model):
   - scale to the card's height or length, apply transforms;
   - facing and pivot per the rules below;
   - reduce to the card's triangle budget, and remove hidden or interior geometry (Tripo meshes are often closed, double-layered shells);
   - one material where possible; remove `KHR_materials_volume` and other unused extensions; turn **double-sided off**;
   - textures baked or resized to the card's size;
   - export only that collection as `.glb`.
5. **Split the textures** with `python -I art-source/glb_extract_images.py in.glb <outdir>/<Name>.glb <Name>_BaseColor <Name>_Normal …` (one name per image, in the glb's image order), then set the texture importers (CLAUDE.md → Model textures): base colour sRGB, metallic-roughness **linear (sRGB off)**, normal map as **Normal map** type, mipmaps on, ASTC 6×6 on Android/iOS. Keep glTFast's own materials (they read glTF's packed channels; URP Lit expects a different packing).
6. **Into the game.** A new model is a **new asset next to the old one**, never a file overwrite: the prefabs reference sub-assets of the old glb by ID. Characters: build the Humanoid avatar, make a new prefab variant, refit the weapon pivots, play every `PlayerActions` state with every weapon, then swap it into `Player.prefab` and the remote-player prefab. Weapons and items: prefab + `ItemModels.asset` entry + regenerate icons.
7. **Provenance.** Every model folder gets a `License.txt`: the tool (Tripo AI, model version), your plan and its licence terms, the date, the concept image's source, and what was changed in Blender.

## Art style: stylised

Chosen 2026-10-24. Hand-painted, stylised fantasy (think World of Warcraft or Torchlight), in the warm light of the Oak Valley mood boards. Every concept image follows this sheet; when a result drifts from it, fix the concept image and regenerate rather than patching the model.

**Shapes**
- Big, simple, chunky shapes with soft bevelled edges. Silhouette first: a character should be recognisable as a black shadow.
- Heroic proportions: about 6.5 heads tall, broad shoulders on the men, hands, feet and weapons ~15% larger than real.
- Hair, beards and cloth are solid sculpted chunks, not strands. Cloth is short or stiff (vests, sashes, short skirts over trousers, wraps); no long capes.

**Colour and texture**
- Colour is painted on: soft light from above, darker towards the feet, light edge highlights on raised parts. No photo textures, no fine noise or grime, no tiny patterns (they vanish at phone size).
- Mostly matte. Metal is a painted colour (bronze, iron, silver) with a highlight, not a real reflective metal.
- Palette, from the mood boards: warm limestone cream, oak and walnut browns, olive and sage greens, undyed white wool, deep crimson wool, bronze and brass. Heroes wear the clean crimson and white; bandits wear dull maroon and dark leather (as the Bandit model does today), so friend and foe read apart at a glance.

**The world's look** (fictional, borrowing from Illyrian and Albanian folk dress and legend, never copying real flags or emblems)
- Mountain clans: white felt caps, white linen shirts, black or dark wool vests with braid trim, wide crimson sashes, leather shoes with turned-up toes, leather bracers and wraps.
- Old Illyrian echoes: bronze, curved sica daggers, round shields, cloak pins, spiral and sun-disc motifs.
- The zanas: silver, pale spring-green and white; their gifts may carry a faint glow (keep it small: one glowing accent, not a glowing outfit).

**Characters**
- **The Kreshnik:** a young frontier warrior, short dark beard, dark hair tied back, white felt cap, white shirt with rolled sleeves, black braided vest, crimson sash, leather bracers, dark trousers wrapped below the knee, turned-up leather shoes, a leather quiver on his back. Confident, broad, not bulky.
- **The Zana's Chosen:** a young woman raised by the mountain spirits, long hair in one thick braid, white and pale-green layered tunic with a short skirt over fitted trousers, silver spiral jewellery, leather wraps on the forearms and shins, a small glowing zana charm at the belt. Light and quick, not frail.

**Concept image template** (fill in the brackets; keep the rest word for word so every asset matches)

> Stylized hand-painted 3D game asset, World of Warcraft style, chunky simple shapes with soft bevelled edges, painted colours with soft top lighting, matte, warm Balkan mountain-folk fantasy. [Subject and details]. [Pose/view]. Plain light grey background, even studio lighting, no shadows on the ground, whole subject in frame, no text, no extra objects.

Pose/view: characters `full body, front view, A-pose with arms 45 degrees down, legs slightly apart, empty hands, neutral expression`; monsters `full body, side view, standing on all fours, mouth closed`; weapons and items `single object, side view, lying flat, centred`.

## Budgets

Measured in the game (triangles after cleanup). The old models over budget are listed under each card until they're replaced.

| Asset | Triangles | Textures | Materials | Rig |
| --- | --- | --- | --- | --- |
| Player character | ≤ 20k, plus a ~5k LOD for other players (later) | base colour 1024; normal 1024 and metallic-roughness 512 only if they visibly help | ≤ 2 | Humanoid, ≤ 65 bones |
| Humanoid NPC or monster | ≤ 8k | 512 | ≤ 2 | Humanoid or Generic |
| Animal monster | ≤ 5k | 512 | ≤ 2 | Generic |
| Weapon | ≤ 2k | 512 | 1 | none |
| Item (ground drop and icon only) | ≤ 1k | 256, or a shared atlas | 1 | none |
| Environment prop | ≤ 3k | shared atlas where possible | 1 | none; placed by `OakValleyBuilder` |

Why: up to 50 players share a channel. Today's 31k-triangle characters would put ~1.5M triangles on screen with a crowd, so a mid-range phone can't hold 60 fps.

## Rules for every model

- **Units are metres**; the model's real size goes on its card.
- **Axes**: Blender Z-up becomes Unity +Y, and Blender −Y becomes Unity +Z. Characters and monsters **face Blender −Y** (the front view) and stand with their **feet on the origin**.
- **Weapons**: in the glb, the middle of the grip at the origin and the blade or shaft pointing up (Blender +Z), the flat side facing the front view; the prefab's model child at rotation (90, 0, 0) then lays it along +Z under the hand, like the Iron Sword. Blender X comes into Unity mirrored (glTFast flips X), so a blade that bends towards Unity +X bends towards Blender −X. Bows are the exception (CLAUDE.md → Bows).
- **Static models are cleaned by `art-source/tripo_static.py`** (axes, grip origin, length, triangle budget, one matte base-colour material at the card's size); its command line goes in the model's `License.txt`, so the clean-up can be rerun from the raw export.
- **Names**: `PascalCase` file and collection names matching the item's `modelKey` or the prefab (`WornSicas`, `SicaLeft`, `Kreshnik`).
- **No loose cloth that needs physics** (long capes, ribbons, free hair strands): nothing simulates them, so they clip through legs during attacks. Keep cloth short or stiff.
- **Silhouette first.** On a phone a character is ~150 px tall; texture detail under a few pixels disappears. Big shapes and colour blocks read, small patterns don't.

## Spec cards

### Characters

| | The Kreshnik | The Zana's Chosen |
| --- | --- | --- |
| Who | Male frontier warrior out of the old mountain songs; close with the sword, ranged with the bow | Female, raised by the mountain zanas; twin sicas and a staff carrying their strength |
| Height | 1.70 m (same as today, so the camera, labels and effects stay tuned) | 1.70 m |
| Budget | ≤ 20k tris, base colour 1024, ≤ 2 materials | same |
| Rig | Tripo humanoid → Humanoid avatar | same |
| Look notes | *(from the style sheet)* | *(from the style sheet)* |
| Must work with | sword in the right hand, bow held at the side and drawn upright | a sica in each hand, staff in the right hand |
| Today | `Kreshnik.glb`, 31k tris, 24 bones, no fingers: **over budget** | `ZanasChosen.glb`, 31k tris: **over budget** |
| Started | `art-source/KreshnikBase.glb` (Tripo, 18.5k tris, 61 bones with fingers, 3 textures at 2048, 0.98 m tall). Made before this style sheet, so it's a technical test of Tripo's rig, not the final look: regenerate from a new concept image | — |

### Weapons

| Item (`modelKey`) | Length | Notes | Today |
| --- | --- | --- | --- |
| Worn Sword (`WornSword`) | ~0.95 m | starter, plain, nicked blade | greybox |
| Iron Sword (`IronSword`) | 1.0 m | a clear step up from the Worn Sword at a glance | Blender model, 1.5k tris ✓ |
| Worn Bow (`WornBow`) | 1.3 m | recurve, horn and wood, red wool cord | Blender model, 1.9k tris ✓ |
| Worn Sicas (`WornSicas`) | 0.6 m each | curved Illyrian daggers, one in each hand | Tripo, 1k tris ✓ (pilot) |
| Worn Staff (`WornStaff`) | ~1.7 m | wood with a carved head carrying the zanas' strength | greybox |

### Monsters

All of them get remade in Tripo too, so they match the characters. They use free Quaternius models with full animation sets today, so a replacement needs the model **and** its clips: idle, walk, run, attack and death. Use Tripo's rig and animation presets (quadruped for the wolves and boar, humanoid for the bandit); if a clip is missing there, we rig or animate it in Blender. Each one keeps the `Wolf.controller` template, and its walk and run speeds are measured again. The Den Wolf is generated separately (charcoal fur, amber eyes, heavier build) rather than recoloured, so it reads as a tougher animal.

| Monster | Size | Budget | Today |
| --- | --- | --- | --- |
| Grey Wolf | ~1.5 m long | ≤ 5k, 512 | Quaternius wolf, 2k tris ✓ |
| Den Wolf | 1.2 × the Grey Wolf | variant of the wolf | prefab variant ✓ |
| Wild Boar | ~1.4 m long | ≤ 5k, 512 | edited Quaternius pig, 0.7k tris ✓ |
| Bandit | 1.70 m | ≤ 8k, 512 | Quaternius adventurer, 7.3k tris ✓ |

### NPCs

| NPC | Height | Notes | Today |
| --- | --- | --- | --- |
| Han Keeper | ~1.70 m | innkeeper by the Han's door, apron | greybox |
| Old Shepherd | ~1.65 m | old man, crook, wool cloak (short) | greybox |
| Sheep | ~1.0 m long | flock in the pen; idle only | greybox |

### Items (bag icons and ground drops; not worn on the body)

Cloth Hood, Padded Vest, Leather Boots, Copper Ring, Shepherd's Amulet, Wolf Fang Amulet, Healing Draught, Zana's Dew. All greybox stand-ins today. Lowest priority: they're only ever seen as a 128 px icon or a small drop on the ground.

## Order

1. **Pilot: Worn Sicas.** A greybox weapon with no rig and no server impact, and the pair tests the off-hand mirror. It proves steps 1–7 end to end.
2. **The Kreshnik** from `KreshnikBase.glb` (or a fresh generation once the style is set), through the new avatar builder.
3. **The Zana's Chosen.**
4. **The other greybox weapons** (Worn Sword, Worn Staff).
5. **Monsters:** Grey Wolf first (the one players meet most), then Boar, Bandit, Den Wolf.
6. **NPCs**, then **items**.

## Pilot: Worn Sicas

One sica is generated and held in both hands, so the pair always matches.

**Concept image** (Tripo's text-to-image if your plan has it, or any image generator):

> Stylized hand-painted 3D game asset, World of Warcraft style, chunky simple shapes with soft bevelled edges, painted colours with soft top lighting, matte, warm Balkan mountain-folk fantasy. An old curved Illyrian sica dagger: a single-edged bronze-coloured blade curving forward like a shallow crescent, a few small nicks on the edge, a short plain crossguard, a grip wrapped in worn brown leather, a round bronze pommel. Simple and well used, a starter weapon. Single object, side view, lying flat, centred. Plain light grey background, even studio lighting, no shadows on the ground, whole subject in frame, no text, no extra objects.

**Tripo:** image to 3D from that picture, with texture on. If it offers a face or polygon limit, set it to about **1,000** (two daggers share the weapon budget of 2k). No rig. Export **glb**, and put it in `art-source/` as `WornSicaTripo.glb`, with the picture as `art-source/concepts/WornSica.png`.

**Card:** 0.6 m from pommel to tip (the greybox's length); 9 cm grip; the tip bends towards Unity +X like the greybox; ≤ 1,000 triangles; 1 material; base colour 512, no normal or metallic-roughness map (checked: the painted colour carries the detail).

**Done 2026-10-24:** Tripo gave 9,982 triangles and three 4096 maps (10.7 MB); `tripo_static.py` made it 1,000 triangles and one 512 map (39 KB glb + 47 KB texture). Both hands use the one model: the off-hand pivot mirrors the pose (`ItemModelRegistry` entry `offHandPrefab`).
