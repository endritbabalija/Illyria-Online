# Hero Online: Classes & Character Progression

Historical system research · 2006 Hero Online, later Hero Plus, international community documentation · 24 October 2026

> The source links and the interactive figures (the progression structure in §1, the second-profession branches in §6, the timelines in §7 and §15, the PvP triangle in §12, the attribute prototype in §16 and the systems diagram in §18) didn't come through when this was pasted in; their text is kept below.

## Executive summary

Hero Online had a more flexible character-building system than its four class names suggest.

In the original release, players made several separate decisions:

- Choose one of four starting characters, each with different weapon proficiencies and starting attributes.
- Allocate Strength, Dexterity and Intelligence while leveling.
- Choose a profession at level 10.
- Build martial arts skills through books and skill points.
- Specialize further through a second profession at level 50.

Importantly, the starting character did not permanently determine the profession. A character associated with one role could choose a different profession at level 10. That was explicitly documented in 2006.

This created character combinations across three dimensions: weapon identity, profession and attribute distribution.

But it also created a serious problem: players could make important stat-allocation decisions long before understanding their consequences.

### Historical findings at a glance

| Mechanic | Findings | Confidence |
| --- | --- | --- |
| Starting characters | Four, with different weapon proficiencies | Documented, 2006 |
| Initial stat points | Four distributable points | Contemporary firsthand report |
| Stat points per level | Five | Contemporary firsthand report |
| Core attributes | STR, DEX, INT | Documented |
| First profession | Level 10; four choices | Documented |
| Second profession | Level 50; two paths per profession | Documented |
| Third profession | Later Divine progression | Official, undated |
| Fourth profession | Later Darkness progression | Official, 2020–26 |
| Initial level ceiling | 100 in original-era documentation | Documented |
| Level-cap expansion | 100 to 200 announced in Japan, July 2007 | Documented |
| Exact STR/DEX/INT formulas | Approximate player measurements survive | Community-reported |
| Exact XP requirement per level | Not reliably recovered | Unverified |
| Attribute resets | Reported by players; later job reset documented | Mixed |
| Class balance | Players reported strong build and weapon differences | Community-reported |

Sources: 2006 preview, 2020 official promotion guide, Japanese 2007 expansion announcement, historical stat discussion.

---

## 1. The original character creation system

### Four starting characters, not four permanent classes

The June 2006 developer journal names four starting characters:

- The Ruthless Blade
- Overseer of the Sky
- The Piercing Eye
- The Elegant Mystic

The characters had different strengths, weaknesses, initial attributes and weapon proficiencies.

A contemporary community guide records specific weapon combinations, although some English character names and gender associations differ between surviving sources.

### The underlying structure

1. **Choose starting character** — initial stats, appearance and compatible weapons.
2. **Develop attributes and weapon proficiency** — character levels 1–9.
3. **Select profession at level 10** — Warrior, Hunter, Physician or Assassin.
4. **Choose second specialization at level 50** — two branches linked to the selected profession.

The important historical distinction is that character identity and profession identity were separate.

**Design implication:** This separation offers more freedom than a rigid Warrior-only-uses-swords model, but requires strong onboarding. Players need to understand their weapon and profession options before spending potentially irreversible attribute points.

---

## 2. Primary attributes and actual formulas

This is one of the most useful sections for designing your combat and equipment systems.

### The three original attributes

A July 2006 hands-on preview described their broad functions:

| Attribute | Primary functions |
| --- | --- |
| Strength (STR) | Carry weight, HP and physical damage |
| Dexterity (DEX) | Defense and attack speed |
| Intelligence (INT) | Chi capacity and technique use |

That is a contemporary description, not a complete numerical specification.

### Community-measured attribute effects

Two independent-looking 2006 community guides provide nearly identical numerical estimates.

| Add 1 attribute point | Reported increase |
| --- | --- |
| +1 STR | +1 normal attack, +1 skill attack, +3 HP, additional carrying capacity |
| +1 DEX | +1 physical defense, +1 skill defense, +2 critical rating, +0.5 dodge rating |
| +1 INT | +3 Chi, +0.5 skill attack, +2 skill defense |

*Community-reported — 2006*

Sources: September 2006 Assassin build guide and June 2006 Vietnamese build discussion.

**Critical caveat:** The Vietnamese guide's author explicitly cautioned that these figures were not perfectly accurate. They observed that Strength sometimes appeared to increase skill attack differently across characters and estimated their table's accuracy at roughly 70%.

Consequently, these values are not verified engine formulas.

### What this means mathematically

If those estimates held for a specific character, investing 50 extra points in each attribute separately would produce the following nominal changes:

| Investment | Approximate result |
| --- | --- |
| 50 STR | +50 normal attack, +50 skill attack, +150 HP |
| 50 DEX | +50 normal defense, +50 skill defense, +100 critical rating, +25 dodge rating |
| 50 INT | +150 Chi, +25 skill attack, +100 skill defense |

These are mathematical extrapolations of player estimates, not independently observed character-screen results.

They also do not establish whether stats have diminishing returns, hidden breakpoints, character-specific multipliers or different PvP effects.

### Why these formulas mattered

STR contributed offensive power and survivability, DEX contributed defensive ratings, and INT contributed technique capacity and resistance.

So each attribute potentially affected several combat dimensions.

However, there was considerable potential for build imbalance. Intelligence, for example, reportedly improved both Chi and skill defense. If those two benefits scaled well together, players could strongly prefer INT-heavy builds.

The actual effectiveness depends on the missing damage, hit, dodge and defense formulas.

I would not transplant these coefficients directly into your Unity combat system.

---

## 3. Stat allocation and level progression

### Starting allocation

A July 2006 hands-on review reports:

- Four attribute points available when starting.
- Five additional attribute points gained per character level.

*Contemporary firsthand report*

Assuming four initial distributable points and five for every level gained, this produces the following cumulative allocation budget, excluding predetermined base stats and any bonus rewards:

| Character level | Calculated distributable points |
| --- | --- |
| 1 | 4 |
| 10 | 49 |
| 20 | 99 |
| 30 | 149 |
| 40 | 199 |
| 50 | 249 |
| 75 | 374 |
| 100 | 499 |

This is an extrapolation from the reported rule. It does not prove that all later versions awarded points identically, or that there were no special milestones or bonus points.

### Character XP versus skill XP

The 2006 lead-designer interview describes skill points as being earned through a separate skill-experience meter, while the hands-on preview discusses leveling and skill points together.

These statements are not necessarily contradictory, but the surviving descriptions don't fully explain their interaction.

What is established is that character attributes and martial arts mastery were distinct progression tracks.

The original experience requirement for every character level was not recovered. I therefore cannot provide an authentic XP curve or confidently calculate the number of monsters required for each level.

---

## 4. The Dan and Kyu leveling system

*Documented, 2006–09*

Instead of displaying progression solely as conventional level numbers, Hero Online used martial-arts ranking terminology.

The 2009 review explains the mapping with the example that level 84 appears as 8 Dan 4 Kyu.

| Ordinary level | Hero Online display |
| --- | --- |
| 1 | 0 Dan 1 Kyu, or simply 1 Kyu |
| 9 | 0 Dan 9 Kyu |
| 10 | 1 Dan 0 Kyu |
| 25 | 2 Dan 5 Kyu |
| 50 | 5 Dan 0 Kyu |
| 84 | 8 Dan 4 Kyu |
| 100 | 10 Dan 0 Kyu |

That gave level milestones a martial-arts identity without requiring an entirely different mathematical progression.

This is one of the original ideas I particularly like for your game.

**Design implication:** Your Illyrian/Dardanian-inspired game could use its own culturally grounded rank terminology, with a normal numerical level still available for clarity.

The important thing to preserve is a recognizable progression milestone every ten levels, not necessarily the names Dan and Kyu.

---

## 5. First profession at level 10

The old Netgame promotion guide describes four profession trainers in Dragon Castle, each associated with a distinct combat or utility role.

**Warrior — offensive and defensive enhancement**
Promotion grants abilities that improve attack and defense, alongside a health-increasing passive skill.
*Officially documented*

**Physician — healing and status effects**
The official guide lists healing, antidote, paralysis and cleansing forms. A supporting passive increases maximum Chi and skill defense.
*Officially documented*

**Assassin — stealth, detection and evasive combat**
Documented forms include hiding, detecting concealed enemies and stealing HP. A passive improves dodge and attack-related ratings.
*Officially documented*

**Hunter — animal taming and utility**
The official promotion guide includes a taming ability and defensive passive. Early developer documentation also identifies gathering-related specialization.
*Officially documented*

### Why these professions were unusual

The Warrior and Physician resemble conventional combat classes. The Hunter and Assassin introduced additional utility.

A Hunter wasn't only a ranged damage dealer. Taming animals could provide access to valuable companions, while gathering had economic relevance.

Similarly, stealth and detection created capabilities beyond damage-per-second comparisons.

**Inference:** These differences offered more opportunities for professions to become economically and socially meaningful than if all four were primarily damage dealers.

But whether each profession fulfilled that potential is a separate question.

---

## 6. Second profession at level 50

The second promotion is arguably the most important character-progression milestone in the historical system.

The older Netgame guide says that, after reaching 5 Dan, players completed a promotion quest chain, received a Secondary Promotional Approval Tablet and returned to a profession trainer to select a specialization.

### Second profession branches

| Profession | Branch | Representative effects |
| --- | --- | --- |
| Warrior | Champion | Offensive enhancements and weakening enemy attack |
| Warrior | Musa / Warrior | Defensive enhancements and weakening enemy defense |
| Physician | Surgeon | Group healing, cleansing and protective techniques |
| Physician | Medic | More combat-oriented specialization |
| Assassin | Slayer | |
| Assassin | Shinobi | |
| Hunter | Elite Dedic / Elite Medic | |
| Hunter | Elite Hunter | |

These are names and representative effects from older Netgame documentation and its 2020 promotion guide. Some English translations differ across pages, especially Musa/Warrior and Elite Dedic/Elite Medic.

The surviving official second-promotion table documents actual skill effects for these branches. For example, a Surgeon gained area healing and group status cleansing, while Champion and Musa emphasized different offensive and defensive support effects.

### What the branching system accomplished

A profession was not necessarily a complete fixed class at level 10. The second promotion allowed players to refine their role much later.

This creates a progression fantasy:

- Begin as a broadly capable martial character.
- Choose a profession.
- Learn its foundational techniques.
- Discover a specialized combat identity through a major quest.

**Inference:** That's more narratively satisfying than automatically adding another five abilities when a character reaches level 50.

### An important historical conflict

The May 2006 developer journal gave an Assassin → Slayer → Shinobi example, suggesting a sequential advancement. Later official guides clearly present Slayer and Shinobi as alternative second-promotion options.

These should be treated as different descriptions of the development plan and later implemented system, not combined into a single verified progression tree.

---

## 7. Third and fourth professions: how progression expanded

This is where the chronology matters most.

### The original level ceiling

A contemporary 2009 review describes ten Dan, equivalent to 100 levels, as the game's normal progression ceiling.

But Japan had already announced an extension beyond level 100 in July 2007. That means the 100-level limit cannot be described as universal across all regional versions as of 2009.

### July 2007: the Japanese cap expansion

*Documented, Japanese edition*

A Japanese announcement dated 13 July 2007 stated that an update scheduled for 26 July would raise the level cap from 100 to 200.

To pass the level-100 threshold, players had to meet several conditions:

- Reach 100% experience at the former level cap.
- Complete the second profession advancement.
- Finish Chapter 3 of the Twelve Heavenly Demons quest line.
- Complete a special advancement quest.

This is a significant finding: progression beyond the level cap was gated by demonstrated advancement, not XP alone.

However, the announcement establishes Japanese-version rules. It does not prove the English-language servers adopted the same system on the same date.

### Later Divine progression

A March 2021 Hero Plus guide describes becoming a Divine-class character through an Ataraxia quest.

It required level 100, a full experience meter and second-profession status. Successful completion awarded a Divine recasting item.

The official third-promotion guide lists four Divine profession identities:

| Divine profession | Main specialization |
| --- | --- |
| Warlord | HP, defense, offensive buffs and resilience |
| BeastLord | Defense, confusion resistance and specialized techniques |
| HolyHand | Healing, Chi and defensive support |
| ShadowLord | Stealth, mobility and related combat techniques |

Each also had access to several gathering-related skills in that guide.

That is a useful difference from the early system: gathering became less exclusive to a single basic profession in later content.

### Later Darkness progression

By March 2020, Netgame's official guide already listed fourth-profession branches for several class lines, including God of War, God of Death and God of Blade.

Later content clearly extends above level 200. An official April 2026 update describes fourth-profession passive martial books with level requirements from 201 to 300.

The following timeline is therefore supported:

- **2006: original progression framework** — four starting characters, four initial professions, advancement at levels 10 and 50, and a 100-level progression framework.
- **2007: Japanese expansion** — announced extension to level 200, with a special quest to progress beyond the former cap.
- **By 2020–21: Hero Plus advanced progression** — official documentation describes third- and fourth-profession identities and Divine advancement.
- **2026: fourth-profession content beyond 200** — passive-book requirements explicitly cover levels 201–300.

I could not establish the exact release date of every third- and fourth-profession system across all regions, nor a universal modern maximum level.

---

## 8. Resetting professions, attributes and skills

This is especially relevant because poorly allocated attributes could undermine an entire build.

### Original-era resets

A July 2006 player-posted cash-shop list describes an item capable of resetting both basic and secondary class selection, along with class skills.

*Community-reported, July 2006*

It suggests that correcting a profession decision could involve a premium consumable, but the source does not establish final retail pricing or all alternative acquisition paths.

### Later official reset items

By 2020, Netgame documented Transformation Paper, which returned a character to a no-profession state.

A September 2026 official anniversary notice distinguished three reset items:

| Item | Documented effect |
| --- | --- |
| Affliction Pill | Reset allocated character stats |
| Large Book of Arts | Reset martial arts skills |
| Transformation Paper | Reset profession and profession-related arts |

*Officially documented, 2026*

A March 2026 event also awarded Transformation Paper to participating players, proving it was not exclusively available through one purchase route in that event.

**The design lesson:** Attribute choices, martial skills and profession identity were separate enough to justify distinct reset mechanisms.

But I'd question whether your mobile game should charge players separately to correct each of those systems.

---

## 9. Character builds and the emerging meta

The strongest historical evidence comes from community discussions rather than official balance tables.

### Five reported build archetypes

An October 2007 player guide describes several common stat combinations.

| Build | Player-described strengths | Player-described weaknesses |
| --- | --- | --- |
| STR/INT | Strong technique damage and PvP capability | Vulnerable to ordinary attacks |
| STR/DEX | Strong basic attack, HP and normal defense | Difficult leveling and weaker skill defense |
| DEX/INT | Comfortable defensive leveling | Low HP and weak PvP performance |
| Pure STR | Large physical damage and HP | Low accuracy and defense |
| Hybrid | Broad capabilities | Lacked a dominant specialty |

The same guide proposes a very specific STR/INT allocation: 147 STR, 15 DEX and the remaining points in INT.

That is a player-recommended build, not an officially verified optimal configuration.

### Why build guides became important

The community discussion suggests several sources of complexity:

- Three attributes affecting multiple derived statistics.
- Different weapon requirements.
- Distinct skill and profession builds.
- Equipment helping compensate for attribute weaknesses.
- Differences between PvE efficiency and PvP survivability.

A July 2006 discussion even contains a player who claimed to be on their third character while testing builds, and said that accessories could fill gaps in a character's stats.

*Community-reported*

This shows that some players considered character optimization meaningful enough to restart characters and keep discoveries private.

It does not demonstrate that all players enjoyed doing so.

### An important balance problem

A 2006 critic argued that weapon choice had more gameplay impact than profession choice. The same player complained that leveling remained repetitive, and that the early class mechanics did little to improve that experience.

This is a particularly useful counterpoint to the developer's intended class diversity.

The existence of distinct class skills does not prove that every class feels distinct during everyday combat.

For your game, a Warrior, Hunter, Assassin and Physician shouldn't merely have different passive bonuses while spending most of their time using identical weapon attacks.

---

## 10. XP progression, death penalties and leveling speed

### Grinding

A July 2006 firsthand preview describes leveling largely through defeating monsters and completing straightforward quests.

A 2009 review similarly describes a progression experience where repetitive monster grinding often dominated the early game.

That establishes the general progression loop, but not the underlying XP function.

### Party experience

The July 2006 firsthand guide advises players that grouping increases XP acquisition.

However, it does not provide a party-size multiplier or a complete distribution formula.

### XP losses on death

A 2009 review describes experience penalties for death in some PvP contexts.

A July 2006 player-posted premium-item description also advertises a revival consumable capable of recovering 50% of lost experience.

These establish the existence of some XP-loss and recovery mechanics, but not a universal death-penalty formula across all servers.

### Progression speed modifiers

Later Hero Plus distributed experience-boost consumables through events. An April 2026 event offered timed EXP boosts advertised at 40%, 50% and 100%.

These are advertised item modifiers, not an independently verified measurement of final leveling speed.

I could not recover reliable official XP requirements for every level, monster XP formulas, party XP division percentages or a full list of level-difference penalties.

---

## 11. Class progression and the player economy

Character development affected the economy through more than equipment purchases.

### Skill books and profession resources

The 2006 developer journal describes promotion quests awarding skill books, while players could obtain and develop different martial disciplines over time.

**Inference:** This creates demand for class- and weapon-appropriate progression resources, although the exact market prices of profession-specific books are not recoverable.

### Pet taming

The Hunter profession could tame animals. The 2009 review states that other players could obtain pets through purchasing them, while Hunters could acquire them through taming.

This created a potential economic role for Hunters as pet suppliers.

The historical sources don't quantify how much profit Hunters earned through this activity.

### Mining and gathering

The original developer journal associates gathering capabilities with Hunters, while later Hero Plus documentation describes mining across multiple advanced professions.

This is a meaningful example of version evolution: a utility initially associated strongly with one class could later become available more broadly.

### Class and attribute reset items

The presence of reset consumables gave players a way to reconsider expensive long-term decisions. Some reset items appeared in premium or event contexts.

**Inference:** Reset restrictions could generate recurring demand for consumables, particularly after balance changes or when players moved from PvE to PvP.

But monetizing necessary build corrections could also discourage experimentation.

### Full character transfers

A January 2008 official notice introduced Soul Transfer, allowing character data such as stats, quests, skills and inventory to transfer to another character under specified restrictions. The initiating character reset to level 1 after use.

This was a significant account-progression feature, not merely an equipment trade.

It also demonstrates how later versions supported complex transfers of persistent character value.

I would not recommend replicating this in your initial mobile MMO-lite.

---

## 12. Social and PvP impact of character builds

The game's progression was closely connected to its competitive environment.

A July 2006 GameSpy preview, quoted by GameBanshee, describes the intended PvP balance: a skilled level-50 player could defeat an inadequately played level-60 opponent, while a level-70 opponent would generally be too powerful.

*Contemporary developer/game-preview account*

This is not a verified statistical measurement of match outcomes. It's evidence of the intended relationship between levels, equipment and player ability.

It suggests the designers wanted all three to matter:

- **Character progression** — levels and attributes.
- **Equipment** — items and enhancements.
- **Player execution** — skills and decisions.

That balance target is worth investigating for your game. In particular, whether a moderately leveled but skilled player can challenge someone with stronger enhanced equipment.

If gear enhancement completely determines PvP, your blacksmith may dominate the rest of the combat design.

---

## 13. Hero Online Global and private-server findings

### Hero Online Global / The Hero Global

I could not verify a complete, independently dated progression rulebook for releases operating under these specific names.

The Netgame Hero Plus website provides extensive official information, but it cannot automatically be used to establish the rules of every international server carrying a similar title.

I therefore cannot verify separate:

- Starting stat distributions.
- Class promotion trees.
- Level caps.
- Stat-point multipliers.
- Experience curves.
- Reset restrictions.
- PvP-specific class balance.

**Status:** insufficient independently verifiable evidence.

### Private servers

Private servers sometimes advertise customized progression rates, class mechanics and level caps, but I did not recover a version-identified ruleset with enough supporting documentation to compare its numerical progression reliably with 2006 Hero Online.

I would not use an unaudited private-server emulator or advertised XP multiplier as evidence of original mechanics.

### International community evidence

The June 2006 Vietnamese stat-build discussion is particularly useful because its author distinguishes observed behavior from uncertain formulas. It reports the same three attributes and similar approximate coefficients found in an English-language September 2006 build guide.

This is partial corroboration, not proof of exact engine calculations.

---

## 14. Implications for our mobile wuxia MMO-lite

Now the design decisions.

From everything we've researched so far—blacksmith, martial arts, equipment and class progression—I think we should preserve one fundamental characteristic of Hero Online:

**A character's identity should emerge from their weapon, martial disciplines, profession and personal build decisions—not from a single class-selection screen.**

However, I'd be much more careful about irreversible stat allocation.

### What I'd keep, simplify or avoid

| Original feature | Recommendation | Reason |
| --- | --- | --- |
| Starting character + later profession | Keep concept | Builds anticipation and personalization |
| Distinct weapon proficiencies | Keep | Connects class identity to martial arts |
| Three primary attributes | Keep provisionally | Readable foundation for combat |
| Manual attribute allocation | Keep, with guardrails | Meaningful build decisions |
| First profession milestone | Keep | Strong early progression event |
| Two second-profession branches | Keep later | Distinct specialization paths |
| Dan/Kyu rank hierarchy | Adapt terminology | Makes progression memorable |
| Class-specific utility abilities | Keep | More social roles than pure damage |
| High-level profession quests | Keep concept | Earned advancement |
| Multiple paid reset types | Avoid | Punishes experimentation |
| Extensive XP grinding | Reduce | Not ideal for mobile sessions |
| Third/fourth professions at launch | Avoid | Excessive early development scope |
| Separate class-limited crafting currencies | Avoid initially | Unnecessary economy complexity |

---

## 15. Proposed progression structure

For your game's initial long-term structure, I'd test a level cap of 50, with one major specialization milestone and several smaller martial progression milestones.

This is a proposed design, not a historical Hero Online formula.

**Character progression — proposed v0.1**

- **Levels 1–9 — The Apprentice:** learn basic attacks, obtain a first weapon discipline and experiment with starter abilities. Attribute assignment is forgiving and reversible.
- **Level 10 — First Profession:** choose one of four broad martial roles. Unlock the profession's first defining technique and passive ability.
- **Level 20 — Weapon Mastery:** earn a meaningful new technique from the equipped weapon discipline. Introduce more build diversity.
- **Level 30 — Martial Trial:** complete a class-specific challenge to unlock an advanced discipline, passive slot or another distinctive capability.
- **Level 40 — Advanced Training:** prepare for specialization through a substantial character-development milestone.
- **Level 50 — Second Profession:** complete a promotion quest and choose one of two permanent specializations, with an accessible but meaningful reset path.

The five milestone levels aren't intended to represent five separate architectural systems.

They can be implemented largely through existing quest, skill and character-progression capabilities if those already exist in Unity.

---

## 16. The attribute system I'd test first

I would retain STR, DEX and INT as recognizable internal stat concepts, while considering original terminology appropriate to your game's setting.

But I'd simplify the number of derived benefits each attribute controls.

**Attribute allocation prototype** (illustrative; experiment with 10 points above base stats. This is an interface concept, not the original Hero Online formula.)

| Attribute | Points | Role |
| --- | --- | --- |
| STR | 10 | Physical attack and HP |
| DEX | 10 | Accuracy, evasion and speed |
| INT | 10 | Chi and martial skill power |
| Remaining points | 10 | |

*(The figure also had a "Reset allocation" button.)*

These attribute roles are design suggestions. We would need to resolve the underlying attack, defense and martial skill formulas before adopting them.

One safeguard I'd insist on: no player should become unable to use their own class's basic techniques because they failed to allocate enough INT early on.

---

## 17. Progression economy: starting targets

We should not copy the original game's five attribute points per level without first knowing its full derived-stat formulas.

For a first 50-level design, I'd test:

| Progression resource | Proposed starting rule |
| --- | --- |
| Primary attributes | 3 allocatable points per level |
| Profession unlock | Level 10 |
| Advanced martial trial | Level 30 |
| Second profession | Level 50 |
| Initial respec | Free during early levels |
| Later attribute respec | Earnable through ordinary gameplay |
| Martial skill respec | Separate configuration, no premium requirement |
| Profession change | Special quest or earned consumable |
| XP progression | Data-driven curve, not decided yet |

A three-point-per-level award would create a budget of 147 earned points across level gains from 1 to 50, excluding starting allocations or milestone rewards.

That's not inherently balanced; it is simply an easy-to-reason-about starting budget.

The actual XP curve requires an independent progression simulation based on expected kills, quest rewards, playtime, party bonuses and level-difference rules.

I would avoid inventing XP values before those inputs are defined.

---

## 18. How classes should connect to the systems we've already researched

This is the most important technical consequence of the findings.

- **Character identity** — base attributes, weapon proficiency, chosen profession.
- **Martial Arts** — available techniques, mastery, Chi costs and class abilities.
- **Equipment & Loot** — weapon compatibility, requirements, rarity and item bonuses.
- **Blacksmith** — stone-derived bonuses, enhancement history and item destruction.
- **Companions** — taming and companion utility, with profession-related advantages.
- → **Final combat and progression state** — a consistent rules engine combines these systems.

There are three design conflicts to resolve before implementation.

**Attribute overlap:** Your blacksmith already grants damage, defense, accuracy, dodge and skill damage. If STR, DEX and INT also affect all those stats, we need clear stacking and scaling rules.

**Profession versus weapon identity:** A spear user should not automatically feel identical to another spear user with a different profession. Profession abilities need to change gameplay behavior, not just damage percentages.

**Build freedom versus character viability:** The game should permit unusual builds without allowing a beginner to unknowingly create a character incapable of basic progression.

---

## 19. What I'd ask Claude Code to assess

Before implementing class progression, Claude should inspect the existing Unity systems and answer:

- How are character levels, XP and attributes currently stored and saved?
- Is there one authoritative stat-calculation path, or are combat stats modified by several unrelated scripts?
- How are weapons, abilities and equipment requirements represented?
- Can profession-dependent capabilities be introduced without duplicating the combat system?
- How would attribute respecs, promotion quests and saved-character migrations work?
- What is the smallest playable vertical slice for one profession, one specialization and a complete level-up flow?

I would not tell Claude to build all four classes and eight second-profession branches immediately.

First, validate one class with its own active technique, passive, progression requirements and promotion choice. Then extend the same design to additional roles.

---

## 20. Key sources

These are the strongest sources I found for this research.

| Source | Research value |
| --- | --- |
| 2006 Hero Online developer introduction | Starting characters, professions, skill progression and original design intentions |
| June 2006 lead-designer interview | Original classes, character customization and skill-XP design |
| July 2006 hands-on preview | Starting stat points, level gains and class selection |
| Netgame first-promotion guide | Four starting professions and their abilities |
| Netgame second-promotion guide | Level-50 specialization requirements and branch skills |
| Netgame third-promotion guide | Divine professions and specialized abilities |
| 2020 official first/second profession guide | Updated progression tree and profession reset |
| 2021 Divine promotion guide | Divine advancement requirements |
| 2007 Japanese expansion announcement | Level-100-to-200 expansion and progression gate |
| June 2006 Vietnamese character-build guide | Estimated attribute coefficients and early build discussion |
| October 2007 build guide | Player-developed attribute strategies |
| July 2006 player reviews | Criticism of class differentiation and grinding |
| September 2026 anniversary rewards | Distinct stat, martial skill and profession reset items |

---

## Final assessment

I think Hero Online's character-progression design had three genuinely valuable ideas.

**First,** choosing a starting character was not the same as locking into a profession. This created more combinations of weapons, stats and abilities than its four class names suggested.

**Second,** professions developed through meaningful promotion milestones. Reaching level 50 wasn't merely another incremental power increase; it introduced a new specialization choice and quest progression.

**Third,** character builds could be deeply personal. Players experimented with STR/INT, DEX/INT and other allocations, weighed the tradeoffs between PvE and PvP, and used equipment to compensate for weaknesses.

But that flexibility came with problems: limited guidance, potentially ineffective builds, repetitive grinding and inconsistent class impact during ordinary combat. Those concerns appear repeatedly in contemporary reviews and player discussions.

For your game, I'd preserve flexible weapon choice, meaningful profession identity, manually allocated attributes and earned promotion milestones. I'd change the punitive parts: confusing initial allocation, harsh reset restrictions, excessive grinding and class differences that only become noticeable after dozens of hours.

The biggest unresolved decision is not how many classes to create. It's how character attributes, martial arts and equipment calculate final combat power together.

That shared foundation should be designed before Claude starts implementing three separate progression systems. Otherwise we'll risk the exact architectural overengineering you've been trying to avoid.
