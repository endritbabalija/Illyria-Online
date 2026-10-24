# Kreshnik — Vertical Slice Design Doc

Oct 22, 2026 · @endrit

## Summary and pillars

The slice is a small, complete version of the game, set in an original world inspired by Illyrian and Albanian legends: 2 characters, levels 1–30, a shared town, 2 hunting zones, 1 dungeon and 1 faction war, built so a handful of testers can hunt, upgrade, trade and fight on their phones. It proves the four things that made Hero Online special before any money is spent on content. Research behind it: [Hero Online — Game Research](https://claude.ai/artifact/Cb578Efqb8DdBP3qALdAbx).

**Pillars** — every feature should serve at least one:

1. **Legendary-hero feel.** Flashy skill forms and Drangue Leaps over walls and rooftops. Combat should look good even when the numbers are simple.
2. **Everything is tradeable.** Gear, stones, skill books and gold move freely between players. NPC shops sell only basics, so the market is how you get strong.
3. **Risky upgrades.** Any failed upgrade destroys the gear. That keeps demand for stones and spare gear alive and gives the economy its sink.
4. **Faction wars.** Two banners meet in scheduled battles, so PvP has a calendar and a reason.

## Title and story

The story is set; only the title is open. I'd go with **Kreshnik**: it's the word for the heroes of the Albanian frontier songs, it's short, and every player becomes one. Run a trademark and app-store name check before committing.

| Title | Why it works |
| --- | --- |
| **Kreshnik** | Names what the player becomes; unique and easy to search |
| **Land of Eagles** | The well-known nickname for Albania, so it sets the place at once for Albanian players, though it sounds less unique in English |
| **Besa** | The sacred oath at the heart of the story; one strong word, but hard to search for |

**Story**

Long ago the mountain clans swore a besa, a sacred oath, and with the zanas' help they chained the kulshedra beneath the mountains. Now the springs are running dry: the kulshedra is waking and drinking them. The clans split over what to do, and the zanas choose new heroes, the players.

- **Eagle Banner:** keep the besa and seal the kulshedra again with the zanas' help.
- **Wolf Banner:** hunt the kulshedra and take its power for the clans.
- **Starting town:** Dardana, a stone-tower town built around an old bazaar, named after Dardania, the ancient name of the region.

The world is fictional. It borrows creatures and ideas from folklore, not real history, and every character is original.

**Words players will meet**

| Word | Meaning in the game |
| --- | --- |
| Kreshnik | A hero; the top rank in the slice |
| Zana | Mountain spirits who give heroes their strength (the Spirit resource) |
| Drangue | Winged warrior of folklore who fights the kulshedra; the source of the Drangue Leap |
| Kulshedra | The many-headed serpent who causes drought; the main enemy |
| Bolla | A serpent that grows into a kulshedra; a common monster |
| Besa | A sacred oath; breaking it split the clans |
| Kulla | A stone tower house; the towers fought over in war |

The rest of this doc uses Kreshnik as the working title.

## Decisions locked

| Decision | Choice | What it means for the build |
| --- | --- | --- |
| Scope | MMO-lite | A shared town hub plus separate zone instances ("channels") with a player cap each; no seamless open world |
| Look | Full 3D, stylised and hand-painted | Third-person camera. Characters, monsters, weapons and items are generated with Tripo AI from concept images, cleaned up in Blender to a per-asset spec and budget (`docs/art/assets.md`); animations from Mixamo and the Quaternius library; effects from Cartoon FX |
| Engine | Unity, with an MCP bridge so Claude Code can drive the editor | C# client; scenes wired through MCP, not by hand |
| Server | Nakama | Accounts, chat, groups, storage and server-authoritative matches, with server logic in TypeScript |
| Setting | Original world inspired by Illyrian and Albanian legends | Own story, names and characters; no Hero Online names, art or text |
| Money | Decided later | No cash shop or token in the slice, but the economy keeps a slot for an earnable currency (see Economy) |
| Platform | Mobile first | Proposed: Android for early tests, iOS after (open question) |

## Characters and jobs in the slice

The slice has 2 fixed characters, each with 2 weapons, and 2 of the 4 eventual jobs. As in the original, the character sets your weapons and the job sets your role, so 2 characters × 2 jobs already gives 4 distinct kits.

| Character | Weapons | Fantasy | Skill forms in the slice (shape) |
| --- | --- | --- | --- |
| **The Kreshnik** (M) | Sword · bow | A frontier warrior out of the old mountain songs; strikes close or picks off from range | Sword: Eagle Strike (single), Mountain Sweep (cone), Storm of Blades (circle). Bow: Hawk Shot (single), Rain of Arrows (circle at target), Thunder Arrow (single, stun). Level 25: Frontier Volley (cone) |
| **The Zana's Chosen** (F) | Twin sicas · staff | Raised by the mountain zanas; fights with the curved Illyrian daggers (sica) and a staff that carries the zanas' strength | Sicas: Swift Cut (single), Crescent Fan (cone), Whirlwind (circle). Staff: Mountain Strike (single, heavy), Spring Wave (cone), Zana's Ring (circle). Level 25: Wrath of the Zanas (circle, knockdown) |

- **Stats:** Strength, Dexterity and Intelligence, as in the original. Strength drives HP and damage, Dexterity accuracy, dodge and defense, Intelligence Spirit and skill power.
- **Proposed rule:** skills are **not** split by gender in the slice. It halves the animation work; split later if wanted.

**Jobs (chosen at level 10)**

| Job | Active skills | Passive | Specialisations (after the slice) |
| --- | --- | --- | --- |
| **Guardian** | Iron Besa: attack and defense buff on self or an ally · Shield Roar: pull nearby monsters onto you | Bear's Vigor: +max HP | Vanguard (offense) · Bulwark (defense) |
| **Herbalist** | Mending Touch: heal · Antidote: cure poison and paralysis · Viper Needle: poison a target | Owl's Wisdom: +max Spirit, +skill defense | Healer (group support) · Poisoner (curses) |

The two other jobs come after the slice: **Beastmaster** (tames eagles and wolves, mines ore; the economy's richest role) and **Shadow** (stealth and assassination).

## Combat, skills and movement

Combat is target-lock, which suits a phone and stays fair on shaky mobile connections. You pick a target and the server decides every hit; the client plays the animation at once so it still feels instant.

**Controls**

- Left thumb: virtual joystick. Right side: 4 skill buttons, 1 Drangue Leap button, 2 potion slots.
- Tap a monster or player to lock on; auto-attack starts on the locked target.
- Swipe the empty screen to turn the camera.
- Auto-loot picks up gold and items near you.

**Attacks**

- **Basic attacks** cost nothing and can miss or crit.
- **Skill forms** cost Spirit, always hit, and differ in damage, range, cooldown and shape: single target, cone in front, or circle. One form can be queued while another plays.
- **Spirit** is the strength the zanas lend you. It regenerates slowly, so skills are for bosses, packs and PvP, and basic attacks carry everyday hunting.
- **Status effects:** poison (damage over time), paralysis (can't act for a moment) and stun, each with its own resistance on gear.

**Skill books and points**

- Skill books are items, bought from the Book Store or other players. Using one teaches you its forms.
- Skill points come from a separate meter filled by kills, not from levels.
- You spend points on a book's level (1–12). Forms unlock at book levels, e.g. the 2nd at level 3 and the 3rd at level 6, and higher levels make every form in the book stronger.
- Costs climb steeply, as in the original (1, 1, 1, 2, 3, 4, 6, 9, 14, 21, 31, 47 = 140 points to max a starter book), so players choose what to max.

**Drangue Leap (movement skill)**

- Named after the winged warriors of folklore. A book learned at level 20: dash forward and leap onto marked ledges, walls and tower roofs.
- Costs Spirit; disabled while carrying a war banner.
- The server checks every landing spot (allowed surface, maximum distance), so it can't be used to teleport.

**Death and parties**

- Death costs 3% of the current level's EXP and never drops you a level (proposed). Revive at the Han (the inn), or on the spot with a revive item.
- Parties hold up to 4 players; EXP is split by damage dealt.

## World map

&#91;embedded content: slice world map · town, 2 zones, dungeon, war map\]

The slice world is one shared town and four instanced areas. Every trip starts in Dardana, and the Han (inn) in each zone teleports you back for a gold fee.

| Area | Levels | Type | What's there |
| --- | --- | --- | --- |
| **Dardana** | All | Shared town, safe, proposed cap of 100 per channel | Old Bazaar stalls and market board, Blacksmith, Book Store, the Han (potions, teleports), Clan Hall, job trainers, Eagle and Wolf Banner recruiters, arena |
| **Oak Valley** | 1–12 | Open zone, channels of up to 50 | Wolves, boars, bandits; the first errands |
| **Serpent Marsh** | 12–25 | Open zone, channels of up to 50 | Vipers, bollas, oath-breaker raiders; Drangue Leap ledges as shortcuts; Zana Silver starts dropping |
| **Kulshedra's Cave** | 20–30 | Dungeon, one copy per party of 1–4 | 3 chambers and the Young Kulshedra boss; drops the Kulshedra Hunter set, Warding Charms and Zana Silver |
| **Kulla War map** | 20+ | Scheduled battlefield | Three stone towers (kulla) to hold, banner spawn points |

**Oak Valley layout** (south to north, about a 70-second run gate to gate; mood boards in `docs/art/oak-valley/`): the south gate to Dardana with the zone's Han beside it; the meadow (Lv 1–4) with a shepherd and his flock; the oak forest (Lv 5–8) around a dried spring; a river crossed only by one stone bridge; then the upper valley (Lv 8–12) with a bandit camp at a ruined tower on a hill (Lv 8–11) and a wolf den under the east slopes (Lv 11–12); the north pass leads on to Serpent Marsh (Lv 12–25). Each part shows its name and level band when you walk in.

## Progression and quests

The slice runs from level 1 to 30, roughly the first week of play. Levels are shown as ranks, an original take on Hero Online's Kyu/Dan: Novice (1–9), Warrior (10–19), Champion (20–29) and Kreshnik (30). So "Warrior 4" means level 14.

| Level | Unlocks |
| --- | --- |
| 1 | Start in Dardana with a basic version of both weapons |
| 5 | First skill point |
| 10 | Choose a job; weapon books; trading, stalls and the market |
| 15 | Main story, Chapter 1 |
| 20 | Drangue Leap; choose the Eagle or Wolf Banner; Kulshedra's Cave; war map |
| 25 | Level-25 weapon book; found a clan |
| 30 | Slice cap and the Kreshnik rank |

**Quests**

- **Errands:** about one per level early on, from Dardana's townsfolk, e.g. carrying salt to a shepherd or a boar hunt. Small gold, potions or EXP.
- **Main story, Chapter 1 — The First Spring:** 8–10 steps through both zones, ending in Kulshedra's Cave, where the Young Kulshedra guards the first stolen spring. It rewards a strong weapon, like the original's demon chain.
- **Daily bounties:** kill or collect tasks for gold and skill points; they give returning players a reason to log in.
- **Quest tracker** always on screen, and gated zone exits warn you about higher-level areas. The original lacked both.

**Grind rewards**

- **Fortune Draw:** every 100 kills of level-appropriate monsters without dying earns a free draw from a prize table (gold, items, a short EXP buff). The streak resets on death, logout or an hour without kills. A premium draw for gold is an extra gold sink.
- **Fortune boxes:** rare monster drops opened by paying earned gold.
- **Published odds:** the odds for both are shown in game, and they are never opened with bought currency, which keeps them clear of loot-box rules.

## Items and the blacksmith

Gear comes from drops, the dungeon and other players; NPC shops sell only starter gear and consumables. Upgrading at the blacksmith is risky, and that risk drives demand for stones, spare gear and protection charms.

**Gear**

- **6 slots:** weapon, helm, armor, boots, ring, amulet.
- **4 rarities:** Common (white), Fine (green, 1 bonus line), Rare (blue, 2 bonus lines) and Heroic (gold, part of a set).
- **Stats:** damage range, defense, HP, Spirit, accuracy, dodge, skill damage, skill defense, and poison and paralysis resistance.
- **Restrictions:** weapons are locked to the characters who can use them, and gear has a level requirement. Unusable drops are still worth selling or dismantling.
- **Dungeon set:** the Kulshedra Hunter set (helm, armor, boots) gives a bonus when 2 or 3 pieces are worn.

**Upgrade stones**

| Stone | Adds |
| --- | --- |
| Ruby | Attack damage |
| Jade | Defense |
| Amber | Accuracy |
| Onyx | Dodge |
| Sapphire | Skill damage |

Each comes as Rough (tier 1, from drops) or Polished (tier 2, combined from 3 Rough).

**Blacksmith actions**

| Action | What it does |
| --- | --- |
| Strengthen | +1 to +8. Put in the item and 1–3 stones of one type; more stones raise the chance, and the stone type sets the stat gained. Costs a gold fee that grows with item level |
| Dismantle | Breaks an item into materials, with a chance of stones |
| Extract | Takes an item down one level (+5 → +4) for a chance to recover a stone |
| Combine | 3 Rough stones → 1 Polished stone |

**Strengthening odds** (proposed starting values, to tune in testing)

| Target | 1 stone | 3 stones |
| --- | --- | --- |
| +1 | 95% | 99% |
| +2 | 85% | 95% |
| +3 | 75% | 88% |
| +4 | 60% | 75% |
| +5 | 45% | 60% |
| +6 | 35% | 48% |
| +7 | 25% | 36% |
| +8 | 15% | 24% |

- **Same risk as the original:** any failed attempt, from +1 to +8, destroys both the item and the stones used. Early upgrades feel safe only because the odds are high (95–99% at +1).
- **Warding Charm:** stops a destruction and drops the item one level instead. It drops from the dungeon boss, Fortune Draws and war rewards, and it is tradeable, so it becomes one of the market's most valuable goods.
- **Visible power:** +3 gear glows silver, +5 gold, +7 gets a flame aura, so upgraded gear shows in town.
- **Server-side rolls:** every roll happens on the server, is logged, and shows its odds before you confirm.

## Economy and trading

Almost everything earned can be traded, and every gold or item movement is recorded on the server. Gold flows in from hunting and out through fees and failed upgrades, and a separate currency keeps the later money decision open.

**Currencies**

| Currency | Earned from | Tradeable | Used for |
| --- | --- | --- | --- |
| Gold | Monster drops, quests, bounties, selling to NPCs, war rewards | Yes | Everything: shops, fees, trades |
| Zana Silver | A rare separate drop in harder zones and from the dungeon boss | No (account-bound in the slice) | The Silversmith's Warding Charms and cosmetics |

Zana Silver is the open slot for the money decision. Later it can become a premium currency or an on-chain token, the way Gold Dust feeds DRACO in The Hero Global. Because it is separate from gold, a token's price never sets the price of gold.

**Ways to trade** (all unlock at level 10)

- **Trade window:** both players add items and gold and both confirm. Any change resets both confirmations, and the server swaps everything in one step.
- **Stalls:** in the Old Bazaar in Dardana, list up to 6 items to sell, or post buy orders. A stall stays open while you're offline for 24 hours, paid with a stall license.
- **Market board:** a searchable list of everything in stalls, plus direct listings, so nobody has to walk stall to stall on a phone. A listing fee is paid up front and a sales tax on each sale.
- **Mail:** delivers sold items and gold.

Not tradeable in the slice: quest items, Zana Silver, and rewards marked account-bound.

**Gold sinks** (proposed values, to tune with an economy simulation)

| Sink | Cost |
| --- | --- |
| Clan founding | 1,000,000 |
| Premium Fortune Draw | 50,000 |
| Fortune box opening | 500–20,000 by box tier |
| Stall license, 24 hours | 5,000 |
| Teleport between town and zones | 200–1,000 by distance |
| Market listing fee + sales tax | 1% + 5% |
| Strengthening fee | Grows with item level |
| Potions, revive items, NPC skill books | Ongoing |

**Keeping it healthy**

- **A ledger:** every item has a unique ID and every gold or item change is a logged transaction, so duplicates and exploits can be traced.
- **Item lock:** PIN-lock up to 10 items so they can't be traded, dropped or sold by mistake or by a thief.
- **Economy dashboard:** an admin view of total gold in the game and daily gold in versus gold out.
- **No gold or tradeable items for real money in the slice.** In the original, tradeable cash items pushed prices to 500 billion gold.

## Social, factions and PvP

PvP in the slice is opt-in and scheduled: duels and an arena in town, plus a twice-weekly war between the two banners. Open-world player killing, karma and blood-feud bounties come after the slice, once there are enough players to make them fair.

**Clans and banners**

- **Clans (guilds):** founded at level 25 for 1,000,000 gold at the Clan Hall in Dardana, with up to 30 members and clan chat.
- **Banners (factions):** at level 20 every player picks the Eagle Banner or the Wolf Banner, and a clan belongs to its founder's banner. The choice is permanent in the slice.

**PvP modes**

| Mode | Where | Rules | Stakes |
| --- | --- | --- | --- |
| Duel | Anywhere in town | Challenge another player; first to zero HP loses | None |
| Arena | Ring in Dardana | 1v1, queued | Ranking only |
| **Kulla War** | War map, level 20+ | Eagles vs Wolves, up to 20 per side, 15 minutes. Hold 3 stone towers for points per second; kills add points | War coins for both sides, more for the winners |

- **Schedule:** the Kulla War runs twice a week at fixed times (proposed: Tuesday and Saturday evening, Central European Time), with a 10-minute join window.
- **War coins** are spent at the banner quartermaster on stones and Warding Charms, which feeds the war back into the economy.
- **Hunting zones are PvE-only** in the slice.

## Technical architecture

&#91;embedded content: slice architecture · client, server, database, shared data\]

The phone sends what the player wants to do (move, use a skill on a target, upgrade an item, buy a listing); the server checks it, decides the result and sends it back. Nothing that affects gold, items or combat outcomes is decided on the phone.

- **Client:** Unity 6 with the mobile render pipeline, in C#. It plays animations and effects immediately for feel, then corrects to the server's result.
- **Server:** Nakama with server logic in TypeScript. The town and every zone channel run as server-controlled matches (proposed 10 updates a second); blacksmith, trade and market actions are single calls that succeed or fail as a whole. Groups handle clans, and chat channels cover town, clan and banner.
- **Database:** PostgreSQL behind Nakama stores characters, inventories and the gold and item ledger.
- **Shared game data:** one set of JSON files defines skills, items, monsters, drop tables and upgrade odds. Both client and server load them, so numbers never drift between what players see and what the server does.
- **Dev setup:** Claude Code edits the C# and TypeScript, drives scenes and prefabs through the Unity MCP bridge, and runs Nakama locally with Docker. Git with LFS holds the art.
- **Hosting for the closed test:** one small cloud server is enough; scale later.

## Build milestones for Claude Code

Ten milestones, each small enough for one focused stretch of Claude Code sessions. Give Claude Code the milestone's goal and its "done when"; it reads this doc and the repo through CLAUDE.md, so prompts stay short.

| # | Milestone | Goal | Done when |
| --- | --- | --- | --- |
| M0 | Setup | Unity 6 mobile project, Git with LFS, Unity MCP bridge, Nakama running locally in Docker, CLAUDE.md pointing at this doc | Claude Code creates a scene object through MCP, and the client logs into local Nakama |
| M1 | Offline core | One greybox zone, joystick movement, camera, monsters with simple AI, target lock, basic attacks, death and respawn | You can kill 20 wolves on an Android phone at a steady frame rate |
| M2 | Skills | Spirit, skill books and points, both characters' weapon books with single, cone and circle forms, poison, paralysis and stun, Drangue Leap | Each form hits exactly the targets its shape covers; Drangue Leap lands only on allowed ledges |
| M3 | Loot and inventory | Gold and item drops, auto-loot, inventory, 6 gear slots, NPC shop, potions | Equipping a dropped sword changes your stats |
| M4 | Online | Nakama login and saves, the shared town hub, zone channels as server-run matches, combat resolved on the server | Two phones see each other in town and fight the same monster |
| M5 | Blacksmith | Stones, strengthening with the odds table, dismantle, extract, combine, Warding Charm; rolls on the server, logged | 1,000 simulated upgrades match the odds table within 2% |
| M6 | Trading | Trade window, stalls with offline selling, buy orders, market board with fees and tax, mail, item lock, ledger | Duplication tests pass and every trade appears in the ledger |
| M7 | Content | Levels 1–30, jobs, errands, Chapter 1, bounties, second zone, Kulshedra's Cave and its boss, Fortune Draws and boxes | A new player can reach level 30 in about a week of play |
| M8 | Banners and war | Banner choice, clans, duels, arena, Kulla War | A 10-vs-10 test war runs 15 minutes without players seeing different results |
| M9 | Polish and test build | Art pass (every greybox replaced through the asset pipeline), effects and sound, performance on a mid-range Android phone, closed test through Google Play | 10 testers play for a full week |

Order matters: M1–M3 run offline so the game feels good before any server work, and the economy (M5–M6) is built on the server from day one.

## Out of scope and open questions

**Not in the slice** (planned for later): pets and mounts (eagles and wolves are natural first picks); the Beastmaster and Shadow jobs; level-50 specialisations; gender-split skills; open-world PvP with karma and blood-feud bounties; castles and territory; crafting beyond the blacksmith; rebirth; any cash shop, token or NFTs.

**Open questions**

- [ ] Pick the title: Kreshnik, Land of Eagles or Besa
- [ ] Auto-hunt: in the slice or not? Asian mobile MMOs usually have it, and it shapes zone and reward design
- [ ] Android first and iOS after?
- [ ] Player cap per zone channel (proposed 50) and per war (proposed 20 per side)
- [x] Art style: stylised, hand-painted (`docs/art/assets.md` → Art style)
- [ ] The Tripo plan's licence terms for a commercial game
- [ ] Where to host Nakama (provider and region near your players)
- [ ] Money model: cash shop, token or both; decide before M9
