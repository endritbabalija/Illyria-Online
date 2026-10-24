# System research

Detailed system specs, written from research into Hero Online's systems and mechanics, one file per system. They're **proposals**: `docs/design.md` stays the source of truth. When a spec here is agreed, its decisions move into `design.md`; until then, where the two disagree, flag the conflict and ask rather than picking one.

All names, visuals, formulas and implementations are original; Hero Online is only the reference for how the systems feel.

| File | System | Status |
| --- | --- | --- |
| [blacksmith.md](blacksmith.md) | Enhance, dismantle, extract, combine, Warding Charm | v0.1 draft; differs from design.md (see below) |
| [martial-arts.md](martial-arts.md) | Martial arts books, forms, mastery, Chi | research; much of it is already built (see below) |
| [equipment-loot.md](equipment-loot.md) | Equipment, rarity, random properties, drops, crafting | research; the basics are built (see below) |
| [classes-progression.md](classes-progression.md) | Characters, attributes, professions, levels and ranks | research; design.md already follows its structure (see below) |
| [pets-mounts.md](pets-mounts.md) | Companions, taming, evolution, mounts | research; after the slice in design.md (see below) |
| [trading-marketplace.md](trading-marketplace.md) | Direct trade, stalls, buy orders, offline market, fees | research; design.md plans most of it for M6 (see below) |
| [pvp-factions.md](pvp-factions.md) | Duels, faction wars, karma, bounties, Honor | research; design.md plans duels and the banner war for M8 (see below) |
| [crafting-gathering.md](crafting-gathering.md) | Gathering, mining, recipes, contested mines | research; mostly after the slice in design.md (see below) |
| [bosses-loot.md](bosses-loot.md) | Named elites, bosses, world bosses, boss loot | research; the slice's one boss is in M7 (see below) |
| [guilds-social.md](guilds-social.md) | Parties, guilds, mentors, chat, guild prestige | research; design.md plans parties, clans and chat (see below) |
| [quests-world.md](quests-world.md) | Quests, tutorials through quests, regions, exploration | research; design.md plans errands and Chapter 1 for M7 (see below) |
| [events-daily.md](events-daily.md) | Kill-count draws, Fortune Boxes, daily activities, events | research; design.md plans Fortune Draws and boxes for M7 (see below) |
| [items-inventory.md](items-inventory.md) | Item model, stacking, storage, consumables, permissions | research; the bag and items are built (M3) (see below) |
| [combat-formulas.md](combat-formulas.md) | Damage, hit, crit, Chi, statuses, PvP normalisation | research; the combat maths is built (M1–M2) and differs (see below) |

All twelve planned research areas are in, plus a thirteenth on items and inventory and a fourteenth on combat formulas. The next step the research itself recommends is a cross-system review: how the twelve fit together, which balance assumptions contradict each other, what needs real multiplayer, and what belongs in the playable slice. The comparisons below are the starting list of conflicts.

## Open conflicts with design.md

**Blacksmith v0.1**
- Failure: design.md destroys the item on **any** failed attempt (+1 to +8); v0.1 uses three brackets (+1–3 keep the level, +4–5 lose a level, only +6–8 destroy).
- Odds: different tables (design.md: +1 at 95% with 1 stone; v0.1: 100%), and v0.1 adds a 2-stone column.
- Warding Charm: in design.md it stops a destruction at any level; in v0.1 it's only offered at +6–8 and is kept on a success.
- Fees: design.md grows the fee with item level; v0.1 has a flat fee per target level.
- Rarities: the game has Common, Fine, Rare and Heroic; v0.1's dismantle table uses Common, Uncommon, Rare, Epic and Legendary.

**Martial arts research vs what's built (M2, `shared/gamedata/skills.json`)**

Already in the game: books of forms per weapon (Frontier Sword, Hawk Bow, Zana Sicas, Spring Staff, plus the level-25 Frontier Volley and Wrath of the Zanas), 12 book levels on Hero Online's exact 140-point curve, forms unlocking at book levels 1, 3 and 6, skill points from a separate kill meter (5 kills a point), Spirit as the Chi resource, single/cone/circle shapes, level requirements but no Intelligence requirement. Basic attacks and forms already use separate damage formulas (`CombatMath` uses weapon damage, crit and accuracy against dodge; `SkillMath` uses form damage, book level and skill damage against defense and skill defense), which answers most of §14.

Where the research proposes something different:
- 6 mastery ranks instead of 12, with forms at ranks 1, 3 and 5 (game: 12 levels, forms at 1, 3, 6).
- Skill points from character levels (game: from kills; character levels arrive in M7).
- Chi cost rising with mastery, as in the original (game: each form's Spirit cost is fixed).
- 4 equipped techniques chosen from every known book, plus a dodge (game: the buttons show the current weapon's forms; Drangue Leap is the movement button).

**Equipment and loot research vs what's built (M3, `shared/gamedata/items.json`)**

Already in the game: item definitions separate from item instances (every instance has a unique id), weapons with min–max damage locked to characters and weapon types, real level requirements and no attribute requirements, 6 gear slots, rarity rolled per gear drop (Common 70 / Fine 24 / Rare 6 by weight) with that many distinct random bonus lines (0 / 1 / 2) from a per-slot pool, per-monster loot tables, server-run drops and a ledger row for every change.

Where the research proposes something different:
- Rarities: Common, Uncommon, Rare, Epic, Legendary (3 active in the prototype) with a base-stat multiplier (1.00–1.20×) (game: Common, Fine, Rare, plus Heroic for sets in design.md; rarity adds bonus lines but doesn't scale base stats).
- Drop volume: ~4 gear drops per 100 regular kills (game: a Grey Wolf drops gear ~21 times per 100 kills, adding up its seven gear entries).
- Elite and boss loot sources and one rare named boss weapon (game: no elites or bosses until Kulshedra's Cave in M7).
- Scarce equipment surviving +6–8 failures (e.g. a damaged, restorable item): an open question for the blacksmith, not decided anywhere yet.

**Classes and progression research vs design.md (most of it comes in M7)**

Already decided in design.md: the character sets the weapons and the job sets the role, as in the original (2 characters × 2 jobs in the slice); jobs at level 10 (Guardian and Herbalist, with Beastmaster and Shadow after the slice, the counterparts of Warrior, Physician, Hunter and Assassin); level-50 specialisations after the slice; Strength, Dexterity and Intelligence with broad roles; ranks every ten levels (Novice, Warrior, Champion, Kreshnik: an original take on Dan/Kyu); death costs 3% of the level's EXP. Built so far: one stat path (`StatCalculator`: combat.json base plus everything worn), and the player is level 1 (`offline.playerLevel`) until M7; no attributes, EXP or jobs exist in code yet.

Where the research proposes something different:
- Level cap 50 with milestones at 10, 20, 30, 40 and 50 (design.md: the slice runs 1–30).
- 3 attribute points per level, free respec early and earnable later (design.md names the attributes but not the points or resets).
- Attributes kept to narrower roles than the original, so they don't overlap the blacksmith stones (still to decide, together with the stones and gear bonuses).

**Pets and mounts research vs design.md**

Nothing is built, and design.md puts pets and mounts after the slice ("eagles and wolves are natural first picks"), with taming tied to the Beastmaster job (tames eagles and wolves, mines ore), the counterpart of the Hunter. The research's proposals only need deciding when that work starts:
- Who tames: Beastmaster only (design.md's direction, and the original's) or everyone with Beastmaster advantages (the research's preference, so pets don't depend on a busy market).
- Companions as fighters first (wolf offence, bear defence, eagle utility), one active, 3 AI modes (Assist, Defend, Passive), 3 evolution stages, XP from finished encounters rather than attack counts, a bond meter instead of hunger and abandonment.
- Mounts separate from companions: travel only, dismount in combat.

**Trading research vs design.md (M6)**

Already planned in design.md: a trade window where any change resets both confirmations and the server swaps everything in one step; stalls in the Old Bazaar with up to 6 items or buy orders; a searchable market board over every stall plus direct listings; mail for sold items; trading from level 10; a ledger, item lock and no real-money trading. Not built yet.

Where the research proposes something different:
- Offline listings for 7 days at no cost (design.md: a stall stays open offline for 24 hours with a 5,000-gold licence).
- 2% tax only on a sale and no listing fee (design.md: 1% listing fee up front plus 5% sales tax).
- 8 listings and 4 buy orders per player (design.md: 6 stall slots).

**PvP research vs design.md (M8)**

Already planned in design.md: PvP is opt-in and scheduled (duels and an arena in Dardana, a twice-weekly war between the Eagle and Wolf banners on the Kulla War map with three towers, war coins spent at the banner quartermaster); open-world player killing, karma and blood-feud bounties come after the slice. Not built yet.

Where the research proposes something different:
- A first faction battle as a small 5v5 over 3 shrines (8 minutes, score from held objectives, not kills) before a large war (design.md: the Kulla War, tested 10 v 10).
- Bounties redesigned: one payout per contract, only in a bounty zone and on players who opted in or committed a crime, 10% fee, anti-collusion rules.
- Winning gives a 15–25% gathering bonus for a while, never exclusive access to materials everyone needs.
- Soft normalisation of gear in faction battles, so +8 gear doesn't decide every fight.
- No EXP loss in PvP (design.md: death costs 3% EXP; it doesn't say whether that applies to PvP deaths).

**Crafting and gathering research vs design.md**

design.md keeps crafting beyond the blacksmith out of the slice, ties mining to the Beastmaster job (after the slice), and has Rough stones come from drops. Nothing is built. The research's proposals, for when that work starts:
- Gathering open to everyone (ore, herbs, crystals), with job advantages on top, rather than mining as one job's skill.
- Monster drops stay the main source of Rough stones; crystal nodes are a smaller, targeted second source.
- A handful of recipes (two consumables, refined metal, basic gear) sharing the blacksmith's Combine instead of a second crafting system.
- Contested mines as a temporary bonus for the winning banner, never sole access.

**Bosses research vs design.md and the game**

design.md has one boss in the slice: the Young Kulshedra at the end of Kulshedra's Cave (M7), dropping the Kulshedra Hunter set, Warding Charms and Zana Silver; parties of up to 4. In the game today there are no elites or bosses (the Den Wolves are the hardest pack), and each kill's loot goes to the player who dealt the most damage.

Where the research proposes something different:
- Three tiers: named elites in the open zones (solo, 45–90 s, one readable attack), regional bosses (1–3 players) and world bosses (4–8, scheduled windows), rather than one dungeon boss.
- Personal loot for everyone who took part, judged by contribution (healing and defending count), not one owner by top damage (the rule the game uses now).
- Guaranteed boss fragments that add up to the rare item (e.g. 30 for a named weapon), next to a small chance of the item itself.
- Some boss gear dropping already enhanced (+0–3 from regional bosses, up to +5 rarely from world bosses), with a real enhancement history.

**Guilds and social research vs design.md**

Already planned in design.md: parties of up to 4 (the research's number too), clans through Nakama groups with a Clan Hall in Dardana, chat channels for town, clan and banner, and banner membership. Nothing is built yet.

Where the research proposes something different:
- Party EXP split equally with a +10% group bonus (design.md: EXP split by damage dealt).
- Personal loot in parties (the game today gives each kill's loot to the top damage dealer).
- Clan founding at level 20 for about 5,000 gold, 30 members, leader/officer/member roles, one weekly shared objective, no clan storage at first (design.md: founding costs 1,000,000 gold).
- Mentor–disciple pairs later: one mentor, up to two disciples, a catch-up bonus for the disciple.

**Quests and world research vs design.md and the game**

Already in design.md and the game: one shared town (Dardana) with connected zones (Oak Valley, Serpent Marsh, Kulshedra's Cave), the zone's Han to return to, areas that show their name and level band, about one errand per level from Dardana's townsfolk, Chapter 1 of the story and bounties in M7, and a job chosen at level 10. Oak Valley already has the shepherd, the dried spring and the bandit camp as landmarks a quest can use. No quests are built yet.

Where the research proposes something different or more specific:
- Quests that teach each system by doing it (e.g. "enhance a sword twice" at the blacksmith), with a level 1–20 plan that walks through combat, gathering, the blacksmith, skill books, the job, a named elite and a companion.
- Exploration objectives on objects in the world (trees, campfires, chests, weapon racks), not only kill counts.
- The level-10 job and later promotions as short multi-step quest chains, without long back-and-forth runs.
- Quest items tracked in the quest journal instead of bag slots.

**Events and daily research vs design.md (M7)**

Already planned in design.md: a Fortune Draw (a free draw from a prize table of gold, items and a short EXP buff), Fortune boxes opened for 500–20,000 gold by tier, a premium draw paid in gold as a sink, odds shown in game, and nothing opened with bought currency. Nothing is built yet.

Where the research proposes something different:
- A draw every 50 qualifying kills, as in the original, that survives logout and death (design.md: every 100 kills without dying; the streak resets on death, logout or an hour without kills).
- Every draw gives something useful (no empty draw), claimed after combat so it never interrupts a fight; a test table includes a 12% Rough stone.
- Three optional activities (a daily hunt, a daily expedition, a weekly challenge) and no login streaks or hourly online rewards.
- Events reuse the draw and existing content (e.g. event tokens added to the draw) instead of new currencies each time.

Note: the items-and-inventory research found the official Lot System guide, which says the original counter did reset on death, disconnects, changing server and an hour without kills, and needed more than five free bag slots. So design.md's reset rules are the faithful ones; the events research's "keep progress" is a proposed change.

**Items and inventory research vs the game (M3)**

Already built: item definitions separate from item instances (each with a unique id) and one place where gold and items change (`PlayerItems`, with a ledger row per change), a 30-slot bag with all-or-nothing adds, 6 gear slots, potions that stack and share a cooldown per group, 2 potion quick slots, auto-loot within 3 m, no carry weight, no death drops, and server-owned items online. No account storage, quest items or travel items yet.

Where the research proposes something different:
- A 40-slot bag and 4 quick slots (game: 30 and 2).
- A secure claim queue when the bag is full (game: the drop stays on the ground and vanishes after 60 s).
- Separate item permissions (tradeable, storable, listable on the market, sellable) instead of one "bound" flag, before trading is built.
- Sorting and filters by type, rarity and level; quest items kept in the quest journal.
- Combat stats and looks kept apart: cosmetics never add stats.

**Combat formulas research vs the game (M1–M2, on the server since M4)**

Already built, in `Wuxia.Combat` and its server port: separate paths for basic attacks and skill forms; Spirit as Chi, spent by forms and regenerating; statuses (poison, paralysis, stun, knockdown) that refresh rather than stack, with chance × (1 − resistance); crits on basic attacks only; one stat path (`StatCalculator`). The formulas:
- Basic attack: hit chance = accuracy − dodge, clamped to 5–99%; damage = roll(min..max) × crit multiplier − defense, at least 1.
- Skill form: always hits, never crits; damage = roll × (1 + 8% × (book level − 1)) × (1 + skill damage) − (defense + skill defense), at least 1.

Where the research proposes something different:
- Defense as a percentage cut, damage × 100 / (100 + defense), with diminishing returns (game: defense is subtracted, so it matters a lot against weak hits and little against strong ones).
- Hit chance as a 90% base moved by the accuracy–dodge gap in rating points, clamped 10–98% (game: accuracy and dodge are already percentages, clamped 5–99%).
- Separate defense against skills, "Arts Defense", instead of adding skill defense to defense (game: forms are reduced by defense + skill defense).
- STR feeding damage and INT feeding skill damage, once attributes exist (game: no attributes yet).
- Skill forms' Spirit cost rising with book level (game: fixed per form), and Internal Injury left out for now (game: none).
- Explicit per-mode rule sets for PvP normalisation instead of editing saved gear.

This is the shared combat-power question the earlier files kept raising: the blacksmith stones, gear bonus lines and future attributes all plug into these formulas, so they should be settled before the blacksmith (M5) adds stone bonuses.
