# Hero Online: Pets & Mounts System

Historical research report · Original 2006 Hero Online, later Hero Plus, international community documentation · 24 October 2026

> The source links and the interactive figures (the pet types in §1, the evolution families in §4, the Divine crafting loop in §10, the companion cards in §13B, the evolution stages in §13D and the behavior modes in §13E) didn't come through when this was pasted in; their text is kept below. The formulas were written in LaTeX and are shown here as plain text.

## Executive summary

Hero Online's original pet system was much more than a cosmetic companion feature. Pets could fight, protect their owners, be ridden, evolve visually, gain experience, require feeding and loyalty management, and circulate through player trading.

One particularly important detail: only Hunters could tame wild animals, but other professions could purchase and use them. That gave one profession an economic role without excluding everyone else from the system.

I found three especially valuable historical sources:

- A May 2006 developer journal describing the intended system.
- A July 2006 player-tested pet-leveling guide containing unusually precise measurements.
- An official Netgame pet guide preserving species, forms, HP values, feeding and AI behaviors.

Together, they allow us to reconstruct much of the original design, although not its complete combat or taming formulas.

The most important distinction is between the original pet-companion model and the later Hero Plus systems, particularly Divine Pet Ataraxia (2023) and Guardian Pets (August 2026). These introduce additional crafting, probability, stat bonuses and sockets that should not be attributed to the 2006 release.

### Original-system summary

| Mechanic | Original Hero Online evidence | Confidence |
| --- | --- | --- |
| Pet categories | Combat companions and mounts | Documented, 2006 |
| Taming | Hunter profession only | Developer-documented |
| Pet ownership | All professions could purchase/use pets | Developer-documented |
| Initial taming condition | Target reduced to about 10% HP | Reviewer-reported, 2006 |
| Pet progression | Individual leveling and visual evolution | Official guide |
| Experience frequency | One XP calculation every 3 minutes | Player-tested, 2006 |
| Mount-level requirement | Current pet level × 10 XP | Player-reported formula |
| Hunger decay | 1 point/minute | Player-tested, 2006 |
| Loyalty | Begins declining below 70 hunger | Player-tested, 2006 |
| Loyalty reaching zero | Pet leaves its master | Official guide |
| Combat AI | Normal, Combat, Defend, Avoid | Official guide |
| Pet death | Revival consumable available | Community-documented |
| Trading | Pets could be bought, sold and traded | Developer-documented |
| Pet food | Different foods for different pet types | Official guide |
| Visual progression | Multiple species-specific forms | Official guide |
| Exact taming chance | Unknown | Not verified |
| Complete pet damage formula | Unknown | Not verified |
| Mount-speed formula | Unknown | Not verified |

---

## 1. Original pet types: combat companions versus mounts

| Type | Role |
| --- | --- |
| **Combat pets** | Follow the player, attack enemies, defend their master and respond to AI behavior settings |
| **Mounts** | Provide transportation, with certain mounted-combat behavior reported in early sources |

The May 2006 developer journal described two broad categories: mountable and non-mountable pets. The former transported players, while the latter provided combat support through configurable AI.

The original official introduction also says mounts could absorb damage received by their riders.

### A significant historical disagreement: mounted combat

Here the sources conflict.

- **Developer description, May 2006:** Players could fight while riding certain mounts, although the mounts themselves did not attack independently.
- **Player guide, July 2006:** The author explicitly states that skills could not be used while mounted, while still describing gaining mount experience through fighting.
- **Contemporary beta review:** The reviewer describes pets that players could ride into battle.

These statements could reflect differences between ordinary attacks and martial skills, different mount categories, or changes between development intentions and live behavior.

There is insufficient evidence to resolve that precisely.

**Design implication:** For your game, mounted combat should be an explicit design decision. Do not assume Hero Online allowed the full martial-arts moveset while mounted.

---

## 2. Acquiring pets and taming wild animals

### The Hunter profession's special role

*Documented, May 2006*

Hunters could use taming skills to acquire wild animals. Other professions could obtain pets through purchases and trades. NPCs also sold certain pets, including horses.

This is a surprisingly strong piece of profession design.

The Hunter wasn't simply another damage-dealing class. It had access to a method of creating valuable companion assets.

### The reported taming procedure

A July 2006 hands-on guide tells players to reduce an animal to approximately 10% HP before attempting to tame it.

*Contemporary reviewer-reported*

The surviving source doesn't establish whether 10% was an exact engine threshold, a suggested strategy or an approximate observation.

I could not verify:

- The exact taming success probability.
- Whether target level affected the probability.
- Whether skill mastery improved taming success.
- Whether repeated failures increased success chances.
- Whether a failed attempt made the creature hostile.
- Whether the required HP threshold varied by species.
- Whether consumable bait was required.
- The number of simultaneous pets a Hunter could tame or carry.

Those should remain open historical questions.

### Where pets could be acquired

The older official guide preserves several acquisition locations.

| Pet family | Source/location |
| --- | --- |
| Black Wolf | Hunter taming, Highlands |
| Dark Bear | Hunter taming, Highlands |
| Red Hawk | Hunter taming, Southern Plains |
| Great Tiger | Hunter taming, Southern Plains |
| Brown Horse | Stable Keeper NPC |

This created a geographic element to companion progression. Better or different animals required visiting different areas.

**Inference:** That geographical restriction could create travel, hunting and trading opportunities, especially if not all characters could easily obtain pets themselves.

---

## 3. Pet leveling: the actual 2006 mechanics

This is the most unusually detailed community evidence I found.

A player guide published on 30 July 2006, credited to Zengiar and Caestupid, describes direct testing of pet leveling.

### Experience was evaluated in three-minute intervals

The guide reports that pets received XP at approximately three-minute intervals.

When a pet was simply present or mounted without combat, it received one experience point per interval.

Combat activity could increase that reward.

For combat pets, the relevant activity was attacks performed by the pet.

For mounts, the relevant activity was attacks performed by the rider.

### Reported progression formula

The guide states:

```
XP(next level) = 10 × L
```

where L is the mount's current level.

This was specifically described for class mounts, not established as a universal formula for every combat pet.

If the formula applies literally, the next-level requirements would be:

| Current mount level | XP to next level |
| --- | --- |
| 10 | 100 |
| 20 | 200 |
| 30 | 300 |
| 40 | 400 |
| 50 | 500 |
| 80 | 800 |

These numbers are calculated from the community-reported formula. They are not independently verified official values.

### Why the formula matters

At one XP every three minutes, a level-20 class mount would theoretically require 600 minutes, or ten hours, to earn 200 XP through passive presence alone.

That excludes combat bonuses and any other effects.

This helps explain why players developed unusual optimization strategies.

### The pet-leveling exploit pattern

The 2006 guide advises players to use weak weapons against durable enemies to land as many attacks as possible during an XP interval.

Another suggestion involved attacking creatures that could repeatedly heal, allowing players to generate attacks without rapidly killing the target.

This is a crucial economic and gameplay lesson.

**Inference:** If pet XP is rewarded primarily for attack count rather than meaningful combat contribution, players are encouraged to maximize harmless repeated attacks instead of completing normal content.

For your mobile MMO-lite, that could lead directly to unattended grinding, autoclicking and bot-like behavior.

### Additional player-reported findings

The same guide reports:

- Dismissing or unmounting the pet reset its current XP timing interval.
- Moving too far away could teleport the combat pet back and reset the interval.
- Pet leveling did not depend directly on monster level.
- Party membership did not increase pet leveling speed.
- Feeding alone did not grant XP.
- Pets could not lose already-earned levels through death.

These were community experiments, not an official test suite. Their authors also expressed some uncertainty about specific hunger and XP interactions.

A later guide from May 2007 and a 2008 repost repeat much of this information, but their close wording suggests they may be derivative rather than independent confirmations.

---

## 4. Pet evolution and visual progression

*Official Netgame documentation, undated historical guide*

Pets changed forms as they leveled, becoming visually different and receiving higher stats.

The older guide preserves multiple complete evolution families.

| Family | Forms | Reported evolution milestones |
| --- | --- | --- |
| Wolf | Black Wolf → Blood Wolf → Blue Wolf | around level 46 and level 81–82 |
| Bear | Dark Bear → Black Bear → Red Bear | level 58 and level 81 |
| Hawk | Red Hawk → Black Hawk → White Hawk | level 55 and level 82 |
| Tiger | Great Tiger → Baek-Du Tiger → White Tiger | level 61 and level 81 |
| Horse | Brown Horse → White Horse → Black Horse | level 50 and level 84 |

The Netgame guide has a few inconsistencies—for example, one wolf description says level 82 while the summary table says level 81, and a horse entry mislabels an evolved horse as a wolf.

For that reason, the exact milestones should be treated as official-guide values with internal inconsistencies, not a perfectly verified evolution database.

### Documented pet HP progression

The guide also provides concrete HP values.

| Family | First form HP | Second form HP | Third form HP |
| --- | --- | --- | --- |
| Wolf | 246 | 1,632 | 3,540 |
| Bear | 918 | 1,560 | 2,580 |
| Hawk | 1,344 | 2,230 | 2,960 |
| Tiger | 1,860 | 2,650 | 3,400 |
| Horse | 552 | 1,830 | 3,520 |

These are listed values for particular evolved forms, not a per-level HP formula.

We cannot conclude that HP rose linearly between forms, or that pet levels alone determined all HP gains.

### Why evolution was compelling

In the official pet overview, pets progressed through Beginner, Normal, Master and Divine classifications. High-tier pets could acquire special abilities.

An owner could therefore keep a recognizable companion for a long time while watching it transform.

**Inference:** That can create stronger attachment than replacing one pet with a different higher-rarity pet every few levels.

For your game, I would strongly prefer evolving an existing companion over forcing players to discard it repeatedly.

---

## 5. Feeding, hunger, loyalty and abandonment

Hero Online had a persistent pet-care layer.

### Official documentation

The Netgame guide says pets needed food, that prolonged hunger reduced loyalty, and that a pet would leave its master when loyalty reached zero. It also confirms that different pet species required different foods available from taverns.

### Historical hunger numbers

The July 2006 community guide reports:

| Mechanic | Value |
| --- | --- |
| Hunger decrease | 1 point per minute |
| Loyalty decline starts | Below 70 hunger |
| Loyalty increase | Reported during hunger movement from 100 toward 92 |
| Pet abandonment | Loyalty reaches zero; confirmed by official guide |

The guide does not provide a trustworthy universal formula for loyalty loss per minute, how much each food restores or whether all pets share the same hunger rate.

### Gameplay consequences

At one hunger point per minute, a pet could move from 100 to 70 hunger within 30 minutes, assuming continuous depletion.

That is a relatively short period for an MMO session.

For a mobile game, I'd question this mechanic heavily.

Food can reinforce the feeling of caring for a companion, but a pet permanently abandoning the player because they forgot to feed it is especially problematic when players may be interrupted by real-life obligations.

**My recommendation:** Preserve feeding as a positive bond/recovery mechanic, but avoid permanent pet loss through hunger.

---

## 6. Combat AI: four documented behavior modes

The official system included four commands.

| Mode | Officially documented behavior | Mobile design interpretation |
| --- | --- | --- |
| Normal | Protect owner and independently engage enemies | Balanced |
| Combat | Initiate combat with nearby monsters | Aggressive |
| Defend | Attack only to protect itself or its owner | Defensive |
| Avoid | Avoid initiating combat and flee if attacked | Passive |

The distinction between Normal and Defend was meaningful in theory: one could proactively engage, while the other waited for a threat.

### Historical UI

The community guide describes a pet command toolbar and notes that teleporting a distant pet back to the player could reset its command state.

That suggests pet-following and AI state management were visible gameplay systems rather than entirely hidden behaviors.

### What isn't documented

The official guide does not establish the actual AI implementation:

- Detection radius.
- Attack-target priority.
- Leash distance.
- Threat generation.
- Whether a pet could interrupt player-selected targets.
- Behavior when multiple monsters attacked.
- Pathfinding recovery rules.
- Whether the mode changed pet damage or only decisions.
- Whether commands affected PvP targeting.

### Community criticism

A July 2006 forum participant criticized pets for being weak against the much higher-level monsters players were commonly fighting. The same participant felt pets were mainly decorative, despite having selectable behavior modes.

Another participant in the discussion strongly defended the pet system and valued its presentation and long-term potential.

These are opposing player opinions, not proof of broad community consensus.

The criticism does highlight something important: AI options aren't valuable if the companion is too weak to contribute meaningfully to the fight.

---

## 7. Pet death, revival and survival

*Community-documented, July 2006*

The 2006 leveling guide describes reviving dead pets using a consumable called Dead Spirit's Incense, available from a stable NPC near Dragon Castle or through player markets.

The same guide says pets could not lose their levels through repeated deaths.

That suggests a distinction between death and permanent loss, unlike the separate loyalty-abandonment system.

### What is unresolved

I could not verify:

- The exact revival fee.
- Whether the consumable was always required.
- Whether dead pets lost loyalty or experience.
- Whether revival restored full HP.
- Whether cooldowns or location restrictions applied.
- Whether mounts and combat pets used identical revival rules.

For your game, I would make pet defeat temporary, with revival available through ordinary gameplay.

---

## 8. Mounts, speed and class-specific animals

*Community-reported, July 2006*

The historical leveling guide assigns particular mounts to professions:

| Profession | Reported class mount |
| --- | --- |
| Warrior | Lion |
| Assassin | Wolf |
| Hunter | Reindeer |
| Physician | Tiger |

It also says the horse mount was available to every class.

This provides a form of visual class identity, even for players who otherwise use similar weapons.

### The movement-speed problem

A July 2006 critic argued that the player's running skill could become faster than some level-30 mounts.

I could not authenticate the precise movement-speed multipliers of horses, advanced mounts or running techniques.

However, if the complaint reflected normal gameplay, it illustrates a poor incentive: players might spend time and gold developing a mount that fails to improve everyday travel.

**Design implication:** Mounts should retain a clear gameplay role even after players unlock high-level movement skills.

### Riding and combat restrictions

As noted earlier, the developer described combat while mounted, but the 2006 player guide reported that martial skills couldn't be used while riding.

We should treat the exact restrictions as unresolved.

For your Unity game, I'd initially make mounting a travel state, with unmounting required for the full combat moveset. Mounted combat could later become its own specialized feature if there's a compelling design reason.

---

## 9. Pet economy, trading and profession value

The original system supported multiple economic activities.

### Hunter taming as a profession economy

*Developer-documented, May 2006*

Only Hunters could tame pets, while any player could buy or trade them.

That made taming a specialized production activity.

A Hunter could theoretically spend time finding, weakening and capturing desirable animals, then trade them to players whose professions lacked that capability.

This is an economic inference from the documented rules, not verified historical profitability.

### Pet consumables as gold sinks

Food was sold by NPCs, and the historical community guide identifies NPC-purchased revival consumables.

When players bought these supplies directly from NPCs, the transactions could remove gold from the economy.

Trading the same items through player shops would redistribute gold instead.

### Pet-related premium features

A July 2006 player-posted cash-shop list described a temporary item that enabled offensive pets to collect loot automatically for their owners.

*Community-reported, 2006*

This is an important early monetization example.

It wasn't simply buying a prettier pet. It offered a gameplay convenience that could improve farming efficiency.

But the exact price, duration and availability across official servers could not be confirmed.

### Pets as inventory helpers

A 2009 review reported that pets could serve as additional storage through interactions with their owners.

*Reviewer-reported, 2009*

This could help explain some of the appeal of the system beyond combat and traveling.

However, I couldn't verify the number of pet inventory slots, weight limits or whether all pet categories supported storage.

### Economic research gaps

I found no reliable historical datasets for:

- Average prices of captured pets.
- Pet prices by evolution stage.
- Demand for rare mounts.
- Food spending per hour.
- Revival-item transaction volume.
- Pet trading taxes.
- Whether pets retained XP when sold.
- Whether capture quality influenced resale value.

Those mechanics may have existed in some form, but their precise effects remain unverified.

---

## 10. Later Hero Plus: Divine Pet Ataraxia (2023)

A major later feature was officially documented on 17 May 2023.

This system allowed players who had completed their third profession advancement to combine existing high-level pets into Divine Pet cases.

### The crafting loop

1. **Existing pets** — a level-100 pet and additional material pets.
2. **Exchange for pet statues** — different statues based on species and level.
3. **Combine at blacksmith** — 1,200,000 gold fee and required statues.
4. **Receive a randomized pet case** — chance of a Divine pet or a level-99 alternative.

The official guide lists recipes for multiple species and confirms the 1.2-million-gold combination fee.

It also states that the resulting case can yield the desired Divine pet or a level-99 pet; the exact probability was not documented in the text I recovered.

**Design consequence:** Later Hero Plus converted older companions into resources for a new progression layer, while adding significant gold and material costs.

That's effective as an economic sink, but potentially destructive to emotional attachment if players must consume companions they've raised for a long time.

For your game, I would avoid pet-sacrifice crafting at launch.

---

## 11. The August 2026 Guardian Pet system

*Officially documented, 18 August 2026*

This is the newest important expansion I verified.

Unlike traditional companions, Guardian Pets do not fight or receive attacks. They instead grant character attributes and special effects.

### Main and supporting slots

The system allows one primary Guardian Pet and up to three supporting slots.

| Slot | Applied pet stat value |
| --- | --- |
| Main pet | 100% |
| Supporting pet 1 | 50% |
| Supporting pet 2 | 50% |
| Supporting pet 3 | 50% |

The guide prohibits equipping duplicates of the same Guardian kind and prevents Guardian Pet effects from being combined with regular combat/mount pet equipment bonuses.

That creates a new form of build customization through pets.

### Guardian grades

| Guardian grade | Guardian socket slots |
| --- | --- |
| Awakened | 1 |
| Blessed | 2 |
| Exalted | 3 |
| Divine | 4 |

Higher grades have stronger base stats and different appearances.

### Example actual pet stats

The August 2026 official catalog documents a Yang Rat Guardian with these bonuses:

- Maximum HP +1,000.
- Minimum Arts Attack +150.
- Maximum Arts Attack +150.
- Arts Attack +3%.
- Wind +10.
- Monster Aggro +3.

This illustrates the breadth of the later system, but it also introduces considerable stacking complexity alongside character attributes, gear, skill books and blacksmith enhancements.

### Guardian enhancement fees

The official August 2026 guide documents separate enhancement fees:

| Guardian grade | Gold fee |
| --- | --- |
| Awakened | 300,000 |
| Blessed | 1,000,000 |
| Exalted | 5,000,000 |
| Divine | 30,000,000 |

Guardian Socket Enhancement allows randomized stat improvements, and additional consumables can help retain previous values, improve rolls or lock selected stats.

This is effectively another equipment-like optimization system layered onto companions.

**My interpretation:** It's useful for late-game build complexity, but it moves far away from the original fantasy of taming and raising an animal that fights beside you.

---

## 12. Hero Online Global, The Hero Global and private servers

I searched for independent documentation of these named versions, but couldn't verify a complete version-specific pet rulebook.

| Version | What can be established |
| --- | --- |
| Original Hero Online, 2006 | Strong contemporary evidence for taming, pet XP, feeding, AI, mounts and trading |
| Older Netgame guide | Species, forms, HP, levels and acquisition locations |
| Hero Plus, 2023 | Divine Pet Ataraxia combinations and associated gold fees |
| Hero Plus, 2026 | Guardian Pets, stat bonuses, supporting slots and socket enhancement |
| Hero Online Global | No independently verified pet-specific formula or rate table recovered |
| The Hero Global | No independently verified pet-specific formula or rate table recovered |
| Private servers | No authenticated, complete server-specific taming/XP/loot rule table recovered |

I also did not find reliable evidence for a universal pet-capture percentage across official and private servers.

A private server's advertised pet-taming multiplier would not establish the original game's underlying probability.

### Historical numbers we can and cannot trust

| Mechanic | Numerical evidence | Status |
| --- | --- | --- |
| Passive pet XP | 1 per 3 minutes | Community-tested, 2006 |
| Class mount XP to next level | Current pet level × 10 | Community-reported |
| Hunger loss | 1 point/minute | Community-tested |
| Loyalty decline threshold | Below 70 hunger | Community-reported |
| Taming HP threshold | Approximately 10% target HP | Reviewer-reported |
| Pet evolution HP | Several exact forms documented | Official, undated |
| Pet capture chance | No verified percentage | Unknown |
| Pet attack formula | No verified equation | Unknown |
| Pet defense formula | No verified equation | Unknown |
| Mount movement speed | No verified numerical values | Unknown |
| Pet death penalty | Level loss reportedly absent | Community-reported |
| Pet marketplace prices | No reliable historical data | Unknown |
| 2023 Divine crafting fee | 1.2 million gold | Official |
| 2023 Divine result probability | Not recovered | Unknown |
| 2026 Guardian enhancement fees | 300K–30M gold | Official |

---

## 13. Implications for our mobile wuxia MMO-lite

I think the original pet system contains three genuinely valuable ideas worth preserving:

- Pets are living companions that develop alongside the player.
- Pets have observable behavioral roles rather than existing only as passive stats.
- Taming gives a profession an economic function and encourages exploration.

The things I would avoid are punishing hunger management, leveling based on repeated attack counts, companions that become useless against appropriate enemies, and excessive additional enhancement subsystems.

### A. What to keep and what to change

| Hero Online mechanic | Recommendation | Reason |
| --- | --- | --- |
| Combat pets and mounts | Keep | Clear, distinct purposes |
| Wild taming | Keep | Exploration, discovery, economy |
| Hunter taming specialization | Adapt | Creates profession identity |
| Three-stage evolution | Keep | Long-term attachment and visible progression |
| Four AI modes | Simplify initially | Mobile-friendly controls |
| Pet XP tied to attack count | Replace | Encourages artificial grinding |
| Food and loyalty | Simplify | Avoid repetitive maintenance |
| Permanent pet abandonment | Avoid | Punishes interrupted play |
| Death and revival | Keep, low-friction | Adds stakes without permanent loss |
| Pet trading | Consider later | Potential player economy |
| Mounted combat | Delay | Animation, control and balance complexity |
| Guardian Pet sockets | Avoid for v0.1 | Duplicates existing gear progression |
| Pet-sacrifice crafting | Avoid initially | Can undermine pet attachment |
| Pet auto-looting | Consider as utility | Useful mobile convenience, not paid-only |

### B. Proposed companion system

I would begin with one active companion slot and three companion archetypes, rather than launching a full pet collection system.

| Companion | Role |
| --- | --- |
| **Wolf** | Offensive companion. Focuses on attacking the player's target |
| **Bear** | Defensive companion. Can draw limited enemy attention |
| **Eagle** | Utility companion. Scouting, target marking or limited ranged support |

Illustrative original design directions, not a reconstruction of Hero Online's specific pet appearances.

Each companion would have its own identity and purpose. They shouldn't all be interchangeable sources of extra damage.

### C. Suggested leveling model

Rather than granting XP for every attack, I'd reward pets through completed meaningful encounters.

A proposed formula for testing:

```
XP(pet) = XP(eligible encounter) × 0.20
```

This is a starting design number, not a Hero Online formula.

For example, if an eligible encounter rewards 100 character XP, the active pet earns 20 XP.

The system should count completed encounters, not number of attacks.

Potential safeguards:

- No pet XP from enemies far below the intended level.
- No extra XP for hitting the same target hundreds of times.
- Party XP calculated from legitimate encounter participation.
- No need for a pet to deliver the final hit.
- A summoned defensive or utility pet can earn progress without attacking.
- No XP gains for repeatedly damaging invulnerable or permanently healing targets.

This encourages players to actually play the game rather than exploit the leveling timer.

### D. Suggested pet evolution

I'd test three visually distinct forms.

**Illustrative Wolf Companion progression**

| Stage | Levels |
| --- | --- |
| Stage I | 1–19 |
| Stage II | 20–39 |
| Stage III | 40+ |

These are proposed milestones for a level-50 character progression model, not historical evolution thresholds.

I would make evolution permanent and guaranteed once its conditions are met.

No random chance of losing the companion or its progression.

A companion that has traveled with the player for dozens of hours should feel increasingly important, not disposable.

### E. Suggested companion AI

For the first version, I'd reduce the four original commands to three player-facing modes.

**Companion Behavior** — mobile UI interaction concept: **Assist** · **Defend** · **Passive**

- **Assist:** the companion prioritizes the enemy currently targeted by the player and protects them when attacked.

*(The original figure switched between the three modes; only Assist's description came through.)*

These modes can map to existing combat targets and threat events.

I would not initially build a complicated autonomous hunting AI that roams around attacking monsters without player involvement.

That could interfere with mobile combat, pull unwanted enemies or generate excessive pathfinding work.

### F. Feeding and loyalty

My recommendation is to replace permanent hunger-based abandonment with a bond system.

Feeding could:

- Restore companion HP after difficult encounters.
- Increase a non-punitive bond meter.
- Unlock cosmetic behaviors or animations.
- Contribute to evolution quests or training.
- Provide a modest, time-limited utility benefit.

Bond should not continuously decay while the player is offline.

I would not allow players to permanently lose a pet because they neglected feeding for a few hours.

That preserves the emotional relationship while eliminating maintenance anxiety.

### G. Mount progression

I would keep mounts separate from combat pets, because their fundamental jobs differ.

For the first mobile implementation:

- One active travel mount.
- Mounting only outside combat.
- Automatic dismount when entering combat.
- Clear travel-speed bonus.
- No special combat moves while mounted.
- No food-related disappearance.
- Mount visual growth or customization later.

I wouldn't introduce mounted combat until normal player movement, animations and mounted camera behavior are well polished.

### H. Taming and profession design

The original Hunter-only taming system created an interesting economy, but it also carried a risk of making one profession mandatory for obtaining desirable pets without using trading.

For your game, I'd consider two alternative designs:

- **Exclusive taming profession:** Only a dedicated Beastmaster/Hunter can capture wild companions. Other players purchase or trade them.
- **Universal basic taming with specialist advantages:** Everyone can capture ordinary creatures, while specialized characters have better opportunities to capture rare species or access unique companion abilities.

For a mobile MMO-lite with an uncertain number of simultaneous players, I lean toward the second model. It doesn't depend on a healthy player economy existing from day one.

We should choose based on the actual class system we finalize.

---

## 14. Unity implementation implications

Before building companions, Claude Code should inspect how your current prototype handles:

- Enemy AI.
- Character targeting.
- NavMesh movement and navigation.
- Health and damage.
- Aggro/threat management.
- Animations.
- Save-game persistence.
- Equipment and item instances.
- Multiplayer authority, if present.

The first companion should reuse those systems wherever possible.

### Minimum vertical slice

I'd build one wolf that can be acquired, summoned, dismissed, commanded, leveled, temporarily defeated and saved.

It needs:

- Persistent unique pet ID.
- Pet species and evolution stage.
- Pet level and XP.
- Current HP.
- AI mode.
- Owner association.
- Combat stats.
- Summon/unsummon state.
- Save-compatible progression data.

Only after that works should we implement additional species, mounts and trading.

This avoids building an unnecessarily elaborate pet framework before we even know whether the companion feels good in combat.

---

## 15. Key sources

| Source | What it provides |
| --- | --- |
| May 2006 developer journal | Original design: types, taming, trading, mounted combat and AI |
| July 2006 firsthand preview | Taming HP advice, early White Tiger sightings and acquisition |
| July 2006 pet-leveling guide | XP timing, hunger, loyalty, class mounts, revival and player-tested formulas |
| Official Netgame pet guide | AI modes, food, pet forms, HP and evolution levels |
| Official system overview | Evolution classes, special abilities and mount functions |
| July 2006 player debate | Praise and criticism of actual pet utility |
| May 2023 Divine Pet Ataraxia guide | Pet combinations, requirements, recipes and gold fee |
| August 2026 Guardian Pet System | Passive guardian effects, slot system and progression |
| August 2026 Guardian stats | Numerical bonus values by Guardian pet |
| August 2026 Guardian socket system | Material requirements, rerolls, locking and enhancement costs |

---

## Final assessment

The original Hero Online pet system had several features that were ahead of the simple cosmetic-companion approach: class-based taming, independent development, multiple behavioral modes, evolution, mounts and an active player marketplace. Those features are supported by contemporary developer material, player guides and official documentation.

But players also identified practical problems: combat pets could feel ineffective against higher-level enemies, movement skills could outperform mounts, and pet leveling rewarded repetitive attack-count optimization.

Later Hero Plus addressed long-term progression through additional pet tiers, crafting and Guardian stat systems, but at the cost of adding another major layer of character optimization.

For your original mobile wuxia MMO-lite, I'd preserve taming, recognizable companions, combat behavior and visual evolution. I'd replace timer-based attack grinding with encounter XP, remove permanent hunger abandonment and initially avoid pet sockets or sacrificing raised companions.

The most important design question is whether companions are primarily fighters, utility helpers or build-stat equipment. I would make combat companionship the foundation, with mount travel as a separate system. This preserves the emotional appeal of raising a creature alongside your character without turning pets into yet another blacksmith.
