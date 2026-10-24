# Hero Online — Combat Mechanics & Formulas

Historical research report · Original Hero Online (2006), later Hero Plus, international and community documentation · 24 October 2026

> The source links and the interactive figures (the three gauges in §8, the formula sandbox in §14B, the economy loop in §12 and the calculation pipeline in §16) didn't come through when this was pasted in; their text is kept below. The formulas were written in LaTeX and are shown here as plain text.

## Executive summary

The biggest finding is that we can reconstruct Hero Online's combat systems with reasonable confidence, but not its complete original damage formula.

The historical sources clearly distinguish normal attacks, martial techniques, weapon-dependent attacks, Chi expenditure, physical and martial defenses, accuracy, dodge, critical hits, status effects and Internal Injury. However, they don't establish exactly how all these variables were combined into final damage in the 2006 game.

There is an important exception: Netgame published explicit formulas for a special PvP mode in December 2019. These are valuable evidence of how the developers approached combat normalization, but they are not the universal Hero Online damage formulas, and must not be attributed to 2006.

Our research now supports five major conclusions:

- **Normal attacks and martial attacks were separate combat paths.** Different statistics and weapon techniques mattered.
- **STR, DEX and INT affected multiple derived statistics**, but surviving original-era coefficients are estimates reported by players—not authenticated formulas.
- **Chi was a significant combat constraint.** A contemporary 2006 reviewer specifically criticized slow Chi recovery because it discouraged frequent martial-skill use during monster grinding.
- **Internal Injury was a third combat resource**, beyond HP and Chi, which could weaken offensive and defensive performance or restrict techniques.
- **Later Hero Plus sometimes overrode ordinary combat stats with mode-specific calculations.** That gives us a useful example of PvP normalization without requiring us to copy its precise values.

My recommendation for your Illyrian/Dardanian mobile game is to preserve the distinction between ordinary attacks and powerful weapon disciplines, but design one transparent calculation pipeline before adding more equipment bonuses, class passives or blacksmith modifiers.

---

## 1. Historical combat-mechanics summary

| Mechanic | Verified or reported finding | Evidence quality |
| --- | --- | --- |
| Normal attacks | Distinct from martial techniques | Official, undated |
| Martial Arts attacks | Weapon-associated forms; single-target and area attacks | Official, undated |
| Primary attributes | STR, DEX, INT | Official and 2006 contemporary |
| Attribute coefficients | Approximate player-measured values | Community, June 2006 |
| Weapon base damage | Minimum/maximum damage values | Older equipment catalogues |
| Attack vs Arts Attack | Separately displayed statistics | Official |
| Defense vs Arts Defense | Separately displayed statistics | Official |
| Accuracy/dodge | Combat-relevant ratings | Official and community |
| Critical attacks | Critical rating; exact original chance formula unknown | Community |
| Attack speed | Weapon/build-dependent | Community, 2006 |
| Chi costs | Per-technique, per-rank values documented | Official skill tables |
| Skill mastery | Up to 12 mastery levels | Official skill tables |
| Internal Injury | Reduces attack/defense, may restrict skill use | Official |
| Status effects | Paralysis, confusion, poison and other effects | Official |
| Buff/debuff abilities | Profession and martial techniques | Official |
| PvP scaling | Explicit mode-specific rules exist in 2019 | Official, 2019 |
| Universal 2006 damage equation | Not recovered | Unknown |
| Universal 2006 hit/crit equations | Not recovered | Unknown |
| Exact 2006 attack interval formula | Not recovered | Unknown |

---

## 2. Original combat: normal attacks versus martial arts

The game's original combat was primarily point-and-click. Players targeted enemies and used ordinary attacks or activated martial techniques from their skill interface. A 2006 firsthand review compares the experience with Diablo-style combat, including frequent potion use.

The official skill catalogue distinguishes attack categories such as:

- Melee, single target.
- Melee, area effect.
- Ranged attacks.
- Techniques with different Chi requirements.

### Why normal attacks mattered

Unlike an action game where the basic attack simply exists to build energy for a special ability, normal attacking was a meaningful part of Hero Online's sustained damage.

The July 2006 GameSpy preview reports that players frequently fought large numbers of monsters but lacked sufficient Chi to use martial techniques continuously.

That created a recurring decision: expend Chi for powerful skills or continue with ordinary attacks.

### What we cannot reconstruct

The documentation does not establish whether the original combat server evaluated all ordinary attacks using the same timing and hit-resolution path.

It also doesn't fully describe:

- Attack animation cancellation.
- Whether attacks had separate wind-up and recovery phases.
- Whether skill casting interrupted auto-attacks.
- Multi-hit damage calculations.
- Whether ranged attacks checked line of sight.
- Whether the client displayed estimated or authoritative damage.
- Input buffering and target-switching rules.

These matter for recreating the feel of combat, but the surviving numerical documentation isn't sufficient to reconstruct them exactly.

---

## 3. STR, DEX and INT: original 2006 stat coefficients

This is probably the most important community evidence.

A Vietnamese player guide published on 28 June 2006 provides detailed observations.

*Community-reported, June 2006*

| +1 attribute | Reported effects |
| --- | --- |
| STR | +1 normal attack, +1 skill attack, +3 HP, extra carry capacity |
| DEX | +1 physical defense, +1 skill defense, +2 critical rating, +0.5 dodge rating |
| INT | +3 Chi, +0.5 skill attack, +2 skill defense |

### The author's warning is essential

The author explicitly stated that these figures were approximately 70% accurate, not exact.

They observed cases where one point of STR appeared to add two points of skill attack for one character but only one for another.

Therefore, the table should be used as evidence of the reported direction of stat scaling, not as an official engine equation.

### Example: 20 attribute points

Using the community estimates mechanically:

| Allocation | Nominal result |
| --- | --- |
| +20 STR | +20 normal attack, +20 Arts attack, +60 HP |
| +20 DEX | +20 physical defense, +20 Arts defense, +40 critical rating, +10 dodge rating |
| +20 INT | +60 Chi, +10 Arts attack, +40 Arts defense |

These are calculations from uncertain coefficients, not independently verified character-stat changes.

### Are critical and dodge ratings percentages?

Not reliably established.

The guide lists rating changes but doesn't authenticate how those ratings translate to actual probabilities.

For example, +2 critical rating must not be interpreted automatically as +2 percentage points of critical chance.

That is one of the largest sources of potential error when copying community combat formulas.

---

## 4. Character builds and practical combat balance

A July 2006 player discussion gives several firsthand build observations.

One player reported that DEX/INT could be strong in PvP but weak for ordinary PvE attacks at low levels.

The same player switched from a bow to a one-handed sword because the bow's attack speed felt too slow.

*Community-reported, July 2006*

An October 2007 build guide describes STR/INT as a common approach, with strong skill damage but weaker normal-defense performance. It describes other builds such as STR/DEX and DEX/INT as having different advantages.

### Why this matters

The apparent value of a stat wasn't determined only by its character-sheet benefits.

It also depended on:

- Weapon speed.
- Available martial skills.
- Chi capacity.
- Armor bonuses.
- PvE enemy attributes.
- PvP opponent behavior.

**Inference:** A character could have impressive skill damage but still level inefficiently if normal attacks were weak and Chi recovery was slow.

That is a meaningful historical lesson for your game: build comparisons must consider an entire combat session, not merely one ability's damage.

---

## 5. Martial Arts formulas: what we can verify

The official Netgame skill catalogue provides unusually useful numerical data, but it is important to understand what those numbers measure.

For each martial technique, it can identify the required character level, weapon discipline, attack type, mastery rank and Chi consumption.

### Example: three sword techniques

| Mastery rank | Vigorous Strike Chi | Elegant Strike Chi | Burning Strike Chi |
| --- | --- | --- | --- |
| 1 | 11 | 14 | 17 |
| 3 | 17 | 22 | 29 |
| 6 | 26 | 34 | 47 |
| 9 | 35 | 46 | 65 |
| 12 | 44 | 58 | 83 |

*Official skill table; source page undated.*

Each technique's documented Chi requirement rises in a regular sequence:

```
C_Vigorous(r) = 11 + 3(r − 1)
C_Elegant(r)  = 14 + 4(r − 1)
C_Burning(r)  = 17 + 6(r − 1)
```

where r is mastery rank 1–12.

These equations are exact mathematical descriptions of the published tables, not evidence of how the original game computed every skill's costs internally.

### The crucial missing variable: actual skill damage

The catalogue does not supply a reliable formula for how Burning Strike converts a character's Arts Attack, weapon damage and target Arts Defense into final HP loss.

We can verify how much Chi the skill consumes. We cannot calculate its exact damage-per-Chi efficiency from that data alone.

The same limitation applies to cooldowns, animation times, armor penetration, hit counts and critical interactions.

### Skill-point costs were nonlinear

For the first combination-sword book, the published mastery-point costs are:

1, 1, 1, 2, 3, 4, 6, 9, 14, 21, 31, 47

That totals 140 skill points across 12 ranks.

The later ranks were far more expensive than the initial ones. It meant maximum mastery was a significant investment, although we can't infer exact hours to reach it without the skill XP economy.

**Design implication:** Your proposed six-rank martial arts progression can preserve meaningful specialization while being substantially easier to tune than twelve ranks with escalating point costs.

---

## 6. Critical hits, accuracy and dodge

This is where we need to be particularly careful about not inventing the original formulas.

### What the original-era evidence establishes

Players discussed Critical, Dodge and accuracy-related benefits as important properties of character builds and equipment. The June 2006 Vietnamese guide reports DEX increasing critical and dodge ratings.

But no verified general equation was recovered for:

```
P(hit)      = f(Accuracy, Dodge, Levels)
P(critical) = g(Critical rating, Target, Level)
```

Nor could I establish whether all ordinary attacks and martial arts attacks shared the same critical-hit rules.

### A verified special-case formula: Diamond Castle, December 2019

An official guide dated 18 December 2019 provides the following mode-specific rules:

| Combat statistic | Diamond Castle calculation |
| --- | --- |
| Normal Attack rating | Character level × 9 |
| Arts Attack rating | Character level × 25 + weapon minimum damage + weapon maximum damage |
| Dodge | Character level ÷ 10 |
| Hit | 100 − opponent's Dodge |
| Critical chance | 5% |
| Critical damage | Normal attack × 1.5 |
| Defense | 0 |
| Arts Defense | 0 |
| Attack-speed bonus | 0, representing normal attack speed |

*Official, December 2019 — Diamond Castle only*

For example, a level-80 character with a weapon showing 40–60 damage would have:

```
ATK      = 80 × 9                 = 720
Arts ATK = (80 × 25) + 40 + 60    = 2100
Dodge    = 80 / 10                = 8
```

Against an opponent with Dodge 8, the stated hit calculation produces 92.

These are Diamond Castle ratings or rules, not necessarily final damage to HP.

### An important textual ambiguity

The guide labels the formula *Hit point = 100 − Opponent's Dodge*.

Given its placement near Dodge and Critical, it appears to describe a hit-related value rather than HP, but the translation is imprecise.

Even if interpreted as a hit percentage, the source does not document how values are clamped or rounded.

I would not extrapolate it beyond this particular PvP mode.

### Damage normalization in that mode

The guide also states that ordinary attack/defense equipment buffs are mostly ignored, that only selected HP effects remain and that pets cannot be used.

It imposes additional rules on castle structures, including fixed configured damage values for ordinary and Arts attacks.

This is especially relevant to the question we raised in our PvP research: how to keep a faction battle from being completely dominated by players with highly enhanced equipment.

Hero Plus provides a historical example of aggressively normalizing combat statistics for a special competitive mode.

It does not prove that the same normalization applied to duels, ordinary PvP or boss fights.

---

## 7. Attack speed, movement and range

A July 2006 player explicitly criticized bow attack speed relative to a one-handed sword, which contributed to their decision to change weapon builds.

*Community-reported, July 2006*

That establishes a meaningful difference in how weapons felt to at least one player.

But the original attack intervals are unknown.

I couldn't verify whether the game used a linear, capped or breakpoint-based transformation from Attack Speed rating to attacks per second.

### Later Hero Plus evidence

A 2 April 2024 Netgame correction identifies a Frozen Guild attack-speed consumable that should grant Attack Speed +100 for two hours.

The same update separately listed Accuracy +200 for two hours and Arts Attack +3,000 for two hours.

*Official, April 2024*

This proves these ratings were used in later buffs, but it does not establish that +100 Attack Speed meant 100% faster attacks, or any particular reduction in animation duration.

### Movement and combat

The 2006 developer and preview material emphasize unusually acrobatic movement skills, including fast running and crossing obstacles. These abilities were intended to matter in PvP and against challenging encounters.

**Design implication:** Movement is part of combat balance, not just travel. Effective range, pursuit, retreat and the ability to evade attacks all interact with weapon damage.

For your game, I would measure:

- Attacks per second for each weapon family.
- Damage per completed attack animation.
- Time required to close distance.
- How often attacks miss moving targets.
- Ability recovery and vulnerability windows.
- Chi spent per minute of sustained combat.

I would not copy undocumented historical attack-speed breakpoints.

---

## 8. Internal Injury: the overlooked third resource

This is a potentially distinctive mechanic we haven't fully incorporated into our own combat design.

The official Netgame interface guide describes three character gauges:

| Gauge | Role |
| --- | --- |
| **HP — survival** | Losing all HP causes character death |
| **Chi — technique resource** | Required for martial skills |
| **Internal Injury — accumulated condition** | Could reduce attack and defense and restrict martial skills |

### What we can verify

Internal Injury accumulated as a consequence of receiving physical damage. The official guide says its accumulation could weaken offensive and defensive statistics and interfere with skill use.

Meditation could recover the condition.

### What remains unknown

- Whether every successful hit increased injury.
- Injury gained per damage point.
- Whether damage type changed injury gain.
- Thresholds for penalties.
- Exact attack/defense reductions.
- Conditions under which a martial skill became unavailable.
- Whether injury affected monster AI or player movement.
- Whether injury persisted through death and logout.
- Healing rate per second.
- Potion effects on injury.

These gaps are too large to reconstruct the historical system numerically.

### Would I add it to your game?

Not immediately.

I like the original idea, but a third persistent combat meter adds UI and balance complexity.

First, establish whether the existing Unity prototype's normal combat and Chi systems are fun. Then consider Internal Injury as an advanced status mechanic introduced through a profession or region.

An important rule: if injury makes a player weaker, and weakness causes them to take more injury, the system can create a negative spiral. Recovery and combat escape need to remain practical.

---

## 9. Status effects, resistances and cleansing

The official first-promotion guide documents a Physician skill that can paralyze targets, heal allies, cure abnormalities and clear blocked energy.

The second-promotion guide is more detailed.

### Officially documented abilities

| Effect | Class or ability example |
| --- | --- |
| Healing | Physician, Surgeon |
| Healing over time | Surgeon |
| Poison cleansing | Surgeon |
| Paralysis cleansing | Surgeon |
| Confusion cleansing | Surgeon |
| Poison application | Combat Medic |
| Paralysis application | Combat Medic |
| Attack damage reduction | Champion, Ranger |
| Defense reduction | Musa |
| Movement-speed reduction | Shinobi |
| Accuracy/dodge reduction | Shinobi |
| Stealth | Assassin, Slayer |
| Stealth detection | Assassin |
| HP draining | Assassin |

*Official class guides, undated; not all features established for 2006 launch*

The guide therefore supports at least three different categories of effects:

- **Crowd control:** paralysis and movement interference.
- **Damage over time or abnormal conditions:** poison.
- **Attribute modification:** attack, defense, accuracy, dodge and movement-speed changes.

### Resistances

Later Hero Plus item documentation explicitly lists separate poison, confusion and paralysis defense ratings. A December 2023 item specification, for example, includes Poison/Confusion/Paralysis DEF +25.

The problem is that rating 25 is not necessarily 25% resistance.

I could not recover exact formulas for applying those defenses to status chance, duration or intensity.

### Status-effect stacking

No complete authenticated original rules were recovered for:

- Whether poison stacks.
- Whether multiple paralysis effects refresh duration.
- Whether crowd control diminishes on repeated application.
- Immunity following a completed paralysis.
- Whether bosses resist or ignore particular effects.
- Whether PvP applies shorter durations.
- Whether cleansing removes all effects or one effect at a time.

For your game, these rules need explicit definitions, particularly if you introduce PvP.

---

## 10. PvE versus PvP: what actually changed?

The historical evidence supports different combat contexts, but not one universal list of modifiers.

| Context | Evidence | What can be concluded |
| --- | --- | --- |
| Ordinary PvE | 2006 previews and old guides | Normal attacks, martial skills, Chi and potions mattered |
| Open-world PvP | 2006 previews and PvP guides | Equipment, levels, skills and movement had competitive roles |
| Faction battles | Older Netgame warfare guides | Special objectives and combat rules |
| Diamond Castle | December 2019 official guide | Explicit stat normalization and special damage rules |
| Dark Night regions | October 2025 official guide | Level-derived defensive stats and equipment restrictions |

The original PvP designers intended player ability to matter alongside character progression. A July 2006 preview presents the example of a skilled level-50 player beating a poorly played level-60 opponent, while usually losing to a level-70 opponent.

That is a statement of design intention, not a measured win-rate calculation.

The 2019 Diamond Castle rules provide a particularly concrete implementation of reduced gear dependence, while the 2025 Dark Night guide specifies a different regional normalization rule:

```
Defense = min(2 × Level, 500)
```

This is restricted to that later region, where armor effects and several resistances are disabled.

**The lesson for our game:** A common combat engine can support several rule configurations. We don't need separate damage systems for PvE, duels and faction battles, but the context-specific modifiers must be explicit.

---

## 11. Community feedback: what combat felt like

The original-era feedback exposes three recurring tensions.

- **Visual excitement versus repetitive combat:** The developers emphasized elaborate martial-arts animations and movement, but reviewers complained that ordinary monster grinding often became repetitive.
- **Powerful abilities versus limited Chi:** A 2006 preview specifically criticized slow Chi regeneration for reducing the practical use of martial arts during frequent encounters.
- **Character-build freedom versus balance:** Contemporary players experimented with different allocations and weapons, sometimes recreating characters after discovering that a build performed poorly in PvE or PvP.

The sources do not establish a representative consensus about the game's best weapon or profession. They demonstrate that these considerations mattered to engaged players.

I did not find reliable original-era numerical PvP combat logs, documented tournament win-rate distributions or independently validated damage experiments sufficient to reconstruct the complete system.

---

## 12. Player economy and combat formulas

The damage system determines how valuable many of the items we've already researched actually are.

Consider three of our five planned enhancement-stone effects:

| Stone effect | Required combat dependency |
| --- | --- |
| Ruby — Damage | Normal and/or physical damage formula |
| Amber — Accuracy | Hit probability calculation |
| Onyx — Dodge | Opponent hit probability calculation |
| Sapphire — Skill Damage | Martial attack formula |
| Jade — Defense | Incoming damage mitigation |

These five effects must not be balanced independently.

For example, if enemies rarely miss, Amber's accuracy bonus has limited practical value.

If defense reduces incoming damage too effectively, Jade could outperform additional HP and even offensive upgrades.

If martial abilities dominate damage, Sapphire could become much more desirable than Ruby.

These are mathematical design risks, not historical claims about the exact relative value of Hero Online stones.

### Combat efficiency also controls the economy

A stronger weapon increases farming speed, which can increase equipment drops, stone supply and gold income.

That creates a feedback loop:

Better attributes, skills or equipment → higher effective combat output → faster monster and boss farming → more XP, gold and materials per hour → (back to better equipment)

A blacksmith simulation that assumes every player kills the same number of monsters per hour will miss this relationship.

Our integrated economy model needs to incorporate actual combat efficiency.

---

## 13. Version comparison

| Mechanic | 2006 evidence | Later Hero Plus evidence | Global/private-server evidence |
| --- | --- | --- | --- |
| Normal and martial attacks | Confirmed | Retained in published system guides | Distinct formulas unverified |
| STR/DEX/INT | Confirmed | Additional advanced stats and buffs | Version-specific coefficients unknown |
| Original attribute coefficients | Player estimates | No matching global formula authenticated | Unknown |
| Weapon attack-speed differences | Player-reported | Attack-speed buffs documented | Exact mappings unknown |
| Chi costs | Original mechanic | Detailed 12-rank skill tables | Version parity unverified |
| Internal Injury | Original mechanic described | Official guides retain description | Exact formulas unknown |
| Accuracy and dodge | Community and equipment descriptions | Explicit Diamond Castle exception | Universal rates unknown |
| Critical hits | Community descriptions | 5% and ×1.5 in Diamond Castle | Universal rates unknown |
| Status effects | Profession documentation | Extensive later effects | PvP duration formulas unknown |
| Combat normalization | Not established for 2006 | Verified in 2019 and 2025 modes | No authenticated comparison |
| Pet combat | Developer-documented | Prohibited in Diamond Castle | Server-dependent details unknown |

### Hero Online Global and The Hero Global

I could not verify a complete independently published combat formula set for either named version.

Although Netgame guides are useful, they don't establish that other regional services use precisely the same stat coefficients or PvP modifications.

### Private-server documentation

No authenticated private-server source or database was recovered with a complete, version-controlled combat algorithm that could be compared directly with the 2006 client/server.

I would not use an emulator's unverified constants as evidence of original damage, critical or evasion behavior.

---

## 14. Implications for our Illyrian/Dardanian Unity game

Now we can make a concrete design proposal.

My recommendation is to preserve the original game's normal attacks, martial disciplines, character-build customization and weapon identity, while creating our own consistent damage mathematics.

### A. The smallest useful combat model

Start with a single formula family for ordinary physical damage and martial damage.

The formulas below are original proposed test formulas, not Hero Online formulas.

**Proposed normal damage**

```
A        = Random(WeaponMin, WeaponMax) + 2 × STR
D_normal = max(1, A × 100 / (100 + Defense))
```

**Proposed martial damage**

```
M         = (Random(WeaponMin, WeaponMax) + 2 × STR + INT) × SkillMultiplier
D_martial = max(1, M × 100 / (100 + ArtsDefense))
```

These equations separate ordinary and martial defense, but share the same diminishing-return mitigation function.

There is a problem worth acknowledging: high Defense values eventually produce diminishing absolute increases in protection, while the effective HP contribution can still grow substantially.

We must test mitigation against healing, boss time-to-kill, PvP and equipment bonuses before finalizing it.

**Proposed hit formula**

```
P(hit) = clamp(0.90 + 0.002 × (Accuracy − Dodge), 0.10, 0.98)
```

The 90% baseline, 0.002 coefficient and bounds are illustrative.

They are not a reconstruction of Hero Online.

**Proposed critical hits**

```
P(crit) = clamp(0.05 + 0.001 × CritRating, 0.05, 0.30)
D_crit  = 1.5 × D_normal
```

Again, the proposed 1.5 multiplier happens to resemble the documented 2019 Diamond Castle rule, but that does not establish it as the original game's universal critical multiplier.

I'd initially apply critical hits only to ordinary attacks. Whether martial techniques can crit is a separate rule to decide through testing.

### B. Try different stat values

*(The original was an interactive calculator illustrating how a simplified damage model behaves when STR, INT, weapon damage and target defense change. Its example state:)*

**Combat formula sandbox** — *design proposal*

| Input | Value |
| --- | --- |
| Strength | 20 |
| Intelligence | 15 |
| Average weapon damage | 40 |
| Enemy Defense / Arts Defense | 50 |

| Result | Value |
| --- | --- |
| Normal damage | 53.3 |
| Martial damage | 95.0 |

Uses average weapon damage and a proposed 1.5× martial multiplier. No misses, crits, resource costs, cooldowns, rounding or status effects are included.

The calculator is a starting point for discussion, not a finalized combat design.

Before adopting these numbers, we must decide how your existing weapon and enhancement formulas interact with them. Otherwise we risk applying bonus damage twice.

---

## 15. Recommended rules for combat v0.1

| Mechanic | Initial proposal |
| --- | --- |
| Normal attack | Weapon-dependent auto/basic attack |
| Martial techniques | Weapon-specific active skills |
| Chi | Consumed by techniques, regenerated slowly |
| Weapon attack speed | Defined by weapon family |
| Hit probability | Base 90%, modified by accuracy/dodge |
| Critical hits | Start at 5% with 1.5× damage |
| Defensive mitigation | Diminishing returns |
| Status effects | Initially poison and a short paralysis |
| Cleansing | Available to support-oriented characters |
| Internal Injury | Defer until core combat is validated |
| PvP formula | Reuse combat pipeline with configurable normalization |
| Boss combat | Reuse pipeline, add enemy-specific resistances and behavior |
| Pet combat | Use the same damage and status interfaces with pet-specific stats |

All numbers are proposed starting settings.

### What makes the combat feel like its inspiration?

Not the exact damage multipliers.

It is the combination of:

- Distinct weapon styles.
- Flashy but readable martial techniques.
- Resource-based decisions.
- Meaningful character builds.
- Dangerous enemies worth farming.
- Equipment that changes how effectively the character fights.

For your game, our original spear discipline should feel different from a sword discipline before we give either one twenty numerical stat modifiers.

---

## 16. Unity architecture and dependencies

I would insist on an assessment-only pass before coding.

Claude should first determine how the playable prototype currently handles character attributes, weapon attacks, skills, health, Chi, enemy defenses, status effects, equipment bonuses, damage events and saves.

### The calculation pipeline

1. **Character attributes** — base stats, profession passives, temporary buffs.
2. **Equipment and blacksmith bonuses** — weapon ranges, rarity, ordered enhancement history and stones.
3. **Effective combat statistics** — normal attack, Arts attack, accuracy, dodge, defenses, crit.
4. **Attack resolution** — hit → crit → damage → status → HP/Chi changes.
5. **Combat outcome events** — death, loot, XP, quests, pet contribution, PvP rewards.

These are logical stages. They don't imply Claude must introduce five separate managers.

### Critical dependencies

- **Blacksmith:** Each stone's effect must feed into an appropriate derived stat exactly once.
- **Equipment:** Weapon rarity and rolled attributes must not accidentally multiply the same attack bonus several times.
- **Martial Arts:** Skill mastery controls technique parameters, not unrelated copies of player stats.
- **Classes:** Profession passives must be applied consistently and safely removed on respec.
- **Pets:** Companion attacks should use compatible damage handling while retaining their distinct combat role.
- **Bosses:** Special resistances should be encounter data, not hardcoded exceptions scattered through scripts.
- **PvP:** Normalization should be an explicit ruleset, not modifications to the player's saved equipment or permanent attributes.
- **Economy:** Damage per minute influences monster kills per hour, XP, gold, stone supply and equipment production.

---

## 17. Validation before implementation

I'd have Claude inspect and test the following before changing code:

| Test | Why |
| --- | --- |
| Add 10 STR | Confirm one predictable damage increase |
| Equip a stronger weapon | Verify base weapon damage is included once |
| Equip and remove +3 enhanced gear | Verify exact restoration of original stats |
| Increase target Defense | Confirm expected mitigation |
| Increase Accuracy | Confirm hit probability improves |
| Increase Dodge | Confirm incoming hit probability decreases |
| Level a martial technique | Verify both effect and Chi cost change as designed |
| Apply poison/paralysis | Confirm durations and cleansing |
| Add a pet attack | Confirm damage attribution and kill credit |
| Enter PvP normalization mode | Ensure original equipment isn't modified |
| Save and reload | Verify persistent and temporary stats remain correct |
| Simulate 10,000 combat encounters | Measure damage, misses, crits and kill times |

The simulation count is a recommendation, not a claim that we've already run it.

I would specifically ask Claude to output a combat-stat dependency graph, showing which scripts and data structures currently contribute to final attack, defense and accuracy.

That will expose double-counting and architectural conflicts before implementation.

---

## 18. Primary research sources

| Source | Evidence |
| --- | --- |
| May 2006 developer journal | Original martial-arts combat concept, weapons, skill books and movement |
| June 2006 developer interview | Class choices, weapon proficiency and skill progression |
| July 2006 GameSpy preview | Combat pacing, Chi limitations and intended level/skill balance |
| July 2006 firsthand preview | Click-to-attack combat, potions and higher-level monster fights |
| June 2006 Vietnamese stat guide | Approximate STR/DEX/INT coefficients with explicit uncertainty |
| July 2006 community stat discussion | Weapon speed and different PvE/PvP builds |
| October 2007 build guide | Class and weapon build strategies |
| Official skill tables | Technique types, mastery ranks and exact Chi costs |
| Official combat interface guide | HP, Chi, Internal Injury and character-stat display |
| Official terminology guide | Martial skills and Internal Injury |
| Official first-profession abilities | Healing, stealth, paralysis and class passives |
| Official second-profession abilities | Poisons, debuffs, cleansing, movement and support |
| December 2019 Diamond Castle formulas | Mode-specific ATK, Arts ATK, dodge, crit and normalization |
| April 2024 stat-buff correction | Attack speed, accuracy and Arts Attack item bonuses |
| October 2025 Dark Night regional combat rules | Level-based defense normalization and equipment restrictions |

---

## Final assessment

The strongest part of Hero Online's combat design was how many systems contributed to a character's fighting style: weapon choice, personal attributes, martial books, profession skills, Chi, equipment and enhancement.

Its biggest historical weaknesses appear in the gap between exciting martial animations and repetitive sustained combat, limited Chi regeneration, and the possibility that a seemingly reasonable build would perform poorly in practice.

The numerical evidence is uneven. We have reliable Chi-cost tables and explicit formulas for particular later PvP regions, but we do not have a verified universal 2006 damage equation.

For your Illyrian/Dardanian Unity game, I wouldn't try to reverse-engineer missing coefficients from community guesses.

I'd establish one original, explainable combat model, test it against your selected blacksmith and martial-arts mechanics, and measure how it influences farming speed and progression.

That is now the most important foundation for bringing all our researched gameplay systems together into a coherent game.
