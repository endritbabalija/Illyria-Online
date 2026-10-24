# Hero Online: Martial Arts & Skill Books

Historical systems research | Original Hero Online (2006), Hero Plus, community documentation and mobile adaptation | 24 October 2026

> The source links and the two interactive figures (the mastery example in §11 and the control layout in §13) didn't come through when this was pasted in; their text is kept below.

## Executive summary

The most interesting part of Hero Online's combat progression wasn't simply unlocking stronger attacks. It was that a martial arts book represented an entire group of techniques that a player could progressively master.

That is the mechanic I would seriously consider preserving in your mobile wuxia MMO-lite.

The surviving documentation reveals a surprisingly structured system:

- Skill books gave access to martial arts disciplines, usually containing three to five individual techniques, called Forms.
- Skill points improved mastery, with a documented 12-rank system for many original skills.
- Weapon proficiency, class, character level and Intelligence requirements restricted which books a character could learn.
- Chi consumption increased as skills were upgraded, introducing a resource-management tradeoff.
- Individual techniques had different targeting behavior, including single-target melee, area attacks, piercing attacks and scatter attacks.
- Books entered the economy through NPCs, quests, monster drops and player trading.

These foundations are documented by Netgame's skill guide and contemporary 2006 developer material.

### Research findings at a glance

| Mechanic | Original findings | Confidence |
| --- | --- | --- |
| Skills organized into books | Usually 3–5 forms per book | Documented |
| Mastery levels | 12 ranks for ordinary skills | Documented |
| Book equip slots | 10 slots described in 2006 | Developer-reported |
| Skill XP | Separate progression from character XP | Developer-reported |
| Skill-point costs | Increasing, with exact tables surviving | Documented |
| Chi costs | Increase with mastery; exact tables survive | Documented |
| Book sources | NPCs, quests, drops, trading | Developer-reported |
| First class choice | Level 10 | Documented |
| Second profession | Level 50 | Documented |
| Higher-level books | Level 25, 40, 50, 60, 75 milestones reported | Mixed |
| Combat damage formula | Not completely recoverable | Unverified |
| Skill-book economy | Trading and expensive rare-book acquisition | Community-reported |
| Later Divine/Fierce systems | Expanded specializations and powerful effects | Documented |

A key limitation: Netgame's current website retains old guide pages beside newer Hero Plus systems, but many historical pages are undated. Where a description is from the website rather than a clearly dated 2006 account, I'll identify it as an older-style Netgame guide instead of assigning it an unsupported release date.

---

## 1. How the original skill-book system worked

### A book was a collection of techniques

The core hierarchy was:

- **Martial Arts Book** — a discipline associated with a weapon or specialization
  - **Form 1** — unique attack
  - **Form 2** — unique attack
  - **Form 3** — unique attack
  - **Skill mastery levels** — spend skill points to improve and unlock techniques

The official glossary says books generally contained three to five forms. Each had its own animation, damage and effect characteristics.

A 2006 announcement further described players as having ten book slots, enabling them to choose which collections of techniques to equip.

**Design significance:** A book gives progression more identity than a conventional isolated skill unlock. Finding a new book means discovering a fighting discipline, rather than merely receiving another icon for the ability bar.

### Concrete original example: sword techniques

Netgame's historical-style skill catalog contains the following progression:

| Book | Character level | Example techniques |
| --- | --- | --- |
| Combination Sword, Vol. 1 | 10 | Vigorous Strike, Elegant Strike, Burning Strike |
| Combination Sword, Vol. 2 | 25 | Swing of Infinity, Earthquake Stomp, The Seventh Shadow |
| Tae-Geuk Sword | 40 | Tender Stab, Spin Slash, Rapid Slash, Spirit of Heaven |

These are historical names for analysis only. Your game should have its own original martial disciplines, lore, effects and animations.

Notice that different forms belong to one book, yet perform different actions. In the catalog, The Seventh Shadow is categorized as a melee area attack, Spin Slash as a ranged piercing attack, and Spirit of Heaven as a scatter attack.

This confirms that the book system was not just different damage numbers attached to identical attacks.

---

## 2. Skill mastery, progression and exact costs

This is one of the strongest areas of the research because official numerical tables still exist.

### Twelve mastery ranks

*Documented — Netgame skill catalog*

The catalog lists skill mastery from rank 1 through rank 12. The skill-point requirements rise sharply at higher ranks.

For the first sword-technique volume, the costs are:

| Mastery rank | Points for next rank | Total invested |
| --- | --- | --- |
| 1 | 1 | 1 |
| 2 | 1 | 2 |
| 3 | 1 | 3 |
| 4 | 2 | 5 |
| 5 | 3 | 8 |
| 6 | 4 | 12 |
| 7 | 6 | 18 |
| 8 | 9 | 27 |
| 9 | 14 | 41 |
| 10 | 21 | 62 |
| 11 | 31 | 93 |
| 12 | 47 | 140 |

The difference between early and late mastery is enormous. Reaching rank 6 requires 12 total points, while achieving rank 12 requires 140.

That is almost 12 times the investment, for only six additional ranks.

**Inference:** The curve strongly incentivizes players to consider whether to specialize deeply or distribute their points across multiple disciplines.

### Different book categories cost different amounts

A surviving player-compiled guide lists the following totals to achieve rank 12:

| Book category | Total skill points for rank 12 |
| --- | --- |
| First profession / passive / Volume 1 | 140 |
| Volume 2 | 210 |
| Wind Drift / Tae-Geuk / Master | 315 |
| Second profession | 473 |
| Divine | 680 |

*Community-reported, partly cross-checked*

The first-volume, second-volume and Tae-Geuk values agree with the official sword tables I inspected. The other category totals come from the fan guide and have not all been independently verified against official entries.

This means Hero Online did not simply apply one universal cost curve to every skill category.

### Skill points and character progression

In a June 2006 interview, the designer described skill points as coming from a separate skill-experience meter. A July 2006 hands-on preview, meanwhile, describes points being earned through character progression and invested in books.

A community guide gives these example total skill-point amounts:

| Character level | Reported total skill points |
| --- | --- |
| 10 | 4 |
| 20 | 22 |
| 30 | 56 |
| 40 | 121 |
| 50 | 241 |
| 60 | 459 |

**Source disagreement:** The surviving material isn't precise enough to establish whether every point was awarded by ordinary character leveling, a distinct skill-XP curve, or an interconnected process that early reviewers simplified. The total-point table is community-reported, not an official formula.

For your project, that distinction is important enough to make deliberately rather than inherit accidentally.

---

## 3. Chi consumption and combat resource scaling

Hero Online had a particularly unusual skill-design tradeoff: higher mastery could increase Chi consumption.

*Documented*

Official tables give the following values for selected sword techniques.

| Technique | Chi at rank 1 | Chi at rank 6 | Chi at rank 12 |
| --- | --- | --- | --- |
| Vigorous Strike | 11 | 26 | 44 |
| Elegant Strike | 14 | 34 | 58 |
| Burning Strike | 17 | 47 | 83 |
| The Seventh Shadow | 60 | 105 | 159 |
| Spin Slash | 72 | 97 | 127 |
| Spirit of Heaven | 150 | 200 | 260 |

The Chi values form arithmetic sequences within these examples. For instance, Vigorous Strike increases by three Chi for each additional rank.

However, I could not verify the corresponding damage numbers for these sword skills. Therefore, I cannot determine whether the damage-per-Chi efficiency increased or decreased with mastery.

### The actual resource system

The official interface guide describes Chi as necessary for performing learned techniques. It also includes an Internal Injury meter. Accumulated injuries could reduce attack and defense and restrict skill use; meditation helped recovery.

So combat had at least three interacting systems:

- Health determined survival.
- Chi constrained technique usage.
- Internal Injury could weaken the character and restrict actions.

**Design implication:** Chi was not necessarily just an ordinary mana bar. The surrounding mechanics could make recovering between fights relevant.

For your mobile game, I'd investigate the gameplay value of meditation separately from its downtime cost. A recovery mechanic can reinforce the martial arts fantasy, but requiring players to sit idle after every fight would be a poor fit for short mobile sessions.

---

## 4. Skill-book acquisition and unlock requirements

This is another area where the surviving documentation is unusually specific.

The table below comes from a historical fan guide. It is community-reported and may reflect the version the author played, rather than every Hero Online release. Some level requirements, notably levels 10, 25 and 40, are independently supported by Netgame's sword catalog.

| Book family | Required character level | Additional requirement | Acquisition |
| --- | --- | --- | --- |
| First profession / passive | 10 | 15 INT | Profession trainer |
| Weapon Volume 1 | 10 | 15 INT | Dragon Castle bookstore |
| Weapon Volume 2 | 25 | 30 INT | Dragon Castle bookstore |
| Wind Drift | 30 | 35 INT | Dragon Castle bookstore |
| Tae-Geuk | 40 | 50 INT | Quest or Southern Plains bookstore |
| Second profession | 50 | No INT requirement reported | Promotion quest |
| Master skills | 60 | No INT requirement reported | Antique Chest |
| Divine skills | 75 | Crafting requirement | Crafted book |

The fan guide specifies that opening the Antique Chest for Master skills cost 20 million in-game currency and that Divine book crafting required a level-83 character. These are valuable historical leads, but I did not find independent official confirmation for those exact requirements.

### A second historical source confirms the workflow

An August 2006 Turkish community discussion explains that players purchased a book from the Dragon Castle bookstore, right-clicked it from inventory, and placed it into the skill interface. As skill points accumulated, players upgraded the book's skills. The commenter stressed buying books appropriate for the character and weapon.

*Community-reported, August 2006*

This corroborates the basic learn-and-upgrade workflow without relying on later Hero Plus material.

### Player economy

The June 2006 developer description expressly lists trading with other players as a means of acquiring books.

The expensive chest and crafting requirements reported in the community guide indicate ways rare skills could consume substantial resources, although the actual historical market prices and profitability of book trading remain unverified.

**My interpretation:** The strongest part of this system is that learning a technique could be an adventure in itself. Some arts were bought from teachers, others earned through promotions, and rarer disciplines came through difficult acquisition routes.

For your game, books shouldn't all be generic loot with different rarity colors.

---

## 5. Weapon styles, professions and build identity

### Original professions

In 2006, the game offered four professions at level 10: Warrior, Hunter, Physician and Assassin. Each profession had a different collection of skills and specializations.

A contemporary community guide describes these roles:

| Profession | Skill identity | Evidence |
| --- | --- | --- |
| Warrior | Offensive and defensive buffs; improved survivability | Community, 2006 |
| Hunter | Animal taming, pet-related abilities and mining | Community, 2006 |
| Physician | Healing, status curing and defensive abilities | Community, 2006 |
| Assassin | Stealth, stealth detection, accuracy and evasion | Community, 2006 |

A 2006 developer overview additionally explains that class progression opened more powerful class-specific skills, and that players could specialize in aggressive or passive techniques.

This is important because professions were not exclusively traditional combat roles. The Hunter had economic and utility capabilities beyond attacking monsters.

### Weapons provided another layer of identity

The official skill catalog organizes normal techniques into common, male-character and female-character weapon sets, including separate sword and rod categories.

This indicates that techniques depended on more than just the chosen profession.

For example, a character's profession could supply buffs or utility skills while their weapon discipline determined much of their active attack set.

**Inference:** This separation may have created more build combinations than a rigid class system in which every Warrior always receives the same four attacks.

### Promotion milestones

The developer described multiple profession tiers in 2006, and a 2009 review confirms another major promotion at level 50.

The progression created natural objectives beyond simply gaining a level:

- **Early character progression** — basic attacks and exploration before selecting a profession.
- **Level 10: First profession** — new class-related books and weapon techniques.
- **Level 25–40: Expanded disciplines** — additional weapon forms, higher requirements and movement-related skills.
- **Level 50: Second profession** — promotion quest and new class specialization.
- **Level 60–75: Advanced mastery** — rare-book acquisition and Divine techniques, according to historical community documentation.

The exact rewards and requirements varied across book families. This should not be interpreted as a complete historical unlock schedule.

---

## 6. Combat mechanics: what we know versus what remains missing

The original combat system distinguished between several attack forms.

The official sword catalog contains these attack categories:

| Attack classification | Official example | What is established |
| --- | --- | --- |
| Melee Single Attack | Vigorous Strike | Single-target melee designation |
| Melee Area Attack | The Seventh Shadow | Area-targeting melee designation |
| Range Pierce Attack | Spin Slash | Piercing ranged designation |
| Melee Scatter Attack | Spirit of Heaven | Scatter attack designation |

This is useful evidence of different targeting patterns, but it does not provide their detailed hitboxes, falloff rules or target limits.

### The damage formula problem

The original's complete combat damage formula could not be authenticated from the reviewed material.

Specifically, I could not confidently determine:

- Whether skill damage multiplied physical attack, martial attack or another underlying attack rating.
- How equipment strengthening interacted with skill power.
- Whether normal defense and skill defense were calculated separately for every attack.
- Whether PvP had separate damage multipliers.
- Exact attack-speed and animation-cancel rules.
- Whether individual weapon techniques shared cooldown groups.
- How accuracy determined hits for different technique categories.
- Whether critical hits applied equally to every skill.

It would be misleading to fill these gaps with a conventional MMORPG damage formula.

### Attributes and damage relationships

A contemporary 2006 community guide reports that Dexterity affected hit chance, evasion and defense, while Intelligence affected maximum Chi, skill damage and defense against skills.

*Community-reported*

The official glossary independently confirms Strength, Dexterity and Intelligence as distributable character attributes.

That suggests an important distinction between ordinary weapon progression and martial-technique progression, although it does not prove an exact numerical division between the two.

### A later Hero Plus example with actual effect values

The official February 2023 Fierce Skill guide provides more concrete numbers.

*Fierce Skills — Hero Plus, February 2023*

| Skill | Documented effect by tier |
| --- | --- |
| Fierce Flame | Damage described as approximately 10, 30, 60 or 90 times the relevant level/stat expression, depending on tier |
| Fierce Blue Flame | Pulls up to 10 enemies; ranges 16, 19, 21 and 24 metres |
| Fierce Shield | Invulnerability durations 11, 14, 17 and 21 seconds |

Official 2023 figures. The wording for Fierce Flame does not identify every variable clearly enough to reconstruct an exact damage formula.

These are powerful effects. An invulnerability ability lasting up to 21 seconds, for example, would have an enormous effect on modern PvP unless its cooldown, availability and counterplay were carefully restricted.

These are later Hero Plus mechanics, not evidence of how basic sword combat was balanced in 2006.

---

## 7. Hero Plus and later-version differences

The central book-and-form idea survived, but later releases added significantly more layers.

### Version comparison

| System | Early Hero Online evidence | Later Hero Plus evidence |
| --- | --- | --- |
| Standard martial books | Confirmed in 2006 | Retained in official guide |
| Weapon-specific techniques | Confirmed | Retained |
| Profession skills | Confirmed | Retained and expanded |
| Twelve-rank mastery | Official older-style guide | Still described in general documentation |
| Divine skill trees | Historical higher-level system | Separate official interface |
| House/guild skills | Not independently established for initial beta | Officially documented |
| Fierce skills | Not established for 2006 | Official 2023 guide |
| Darkness Arts books | Not established for 2006 | Official 2025 maintenance notes |
| Full universal damage formula | Unknown | Unknown |

### Divine skill specialization

The Netgame skill-interface guide distinguishes ordinary skills from Divine Skills.

The Divine section describes enhancements affecting Chi consumption, skill attack, range, delays, health and other special properties. The page also states that certain Divine abilities depend on the character's equipable weapons.

This represents a movement beyond simply unlocking another attack animation: later progression could modify the behavior and performance of techniques.

### House skills

The same official guide says that House Skills are learned by the House Master and taught to House members.

*Documented in Netgame guide*

This is an interesting social progression mechanic. A martial discipline could be associated with organizational membership rather than solely personal leveling or random drops.

However, I could not determine the original acquisition costs, whether leaving a House removed a taught skill, or how its full permission system worked.

### 2025 Darkness Arts update

An official July 2025 Hero Plus update standardized the naming of Darkness Arts martial books between NPC shops and player inventories.

This establishes that newer martial-book categories continued to exist and be maintained, but the notice doesn't provide full unlocking rules or combat formulas.

### Hero Online Global and The Hero Global

I could not independently authenticate distinct skill tables or mechanical changes specifically for releases named Hero Online Global or The Hero Global.

Therefore, it would be incorrect to attribute Netgame's Hero Plus mastery costs, Divine mechanics or 2025 content to those versions without additional evidence.

For the moment, they remain unverified comparison targets rather than confirmed mechanical variants.

---

## 8. Private servers and international community documentation

The useful discovery here came from Japanese Hero Online documentation, rather than an authenticated private-server database.

A Japanese wiki, last modified in March 2020, contains profession-specific martial arts tables with starting values and per-training-rank changes.

For example, its second-tier martial profession page describes techniques that increase Strength, defense or HP, with separate entries for starting effect and additional effect per training.

One entry reports a Strength increase of 20 and a duration increase of 120 seconds per training increment. Another describes a defense-related bonus of 1,000.

*Community-documented — Japanese version*

This is valuable because it shows that some martial skills progressed through increased duration or defensive effects rather than simply increased direct damage.

However, those numerical effects must not be blindly merged with Netgame's English-version data. Localization, server balance and update dates may differ.

### Private-server research gaps

I did not find a sufficiently reliable private-server skill database documenting an entire ruleset with a verifiable server version and date.

In particular, I could not verify:

- Private-server-specific mastery multipliers.
- Rebalanced skill-point totals.
- Custom skill-drop tables.
- Rewritten combat formulas.
- Custom profession restrictions.
- Official comparisons between private-server and Netgame behavior.

A private-server advertisement claiming to offer original-style skills would not, by itself, establish the underlying mechanics.

---

## 9. Player economy, monetization and social effects

### Books could circulate between players

*Developer-reported, 2006*

The 2006 developer material explicitly included trading with other players among the ways to acquire skill books.

This gives rare techniques potential marketplace value beyond their direct combat utility.

But no reliable historical transaction dataset was found for comparing common-book prices against rare-book prices.

### Acquisition could become a gold sink

*Community-reported*

The fan guide's account of an expensive Antique Chest for Master skills suggests that advanced book acquisition could involve significant gold spending.

If the chest price went to an NPC or system, that would directly remove currency from circulation. If rare books were instead purchased from other players, those transactions would mainly transfer gold.

Those two effects should be distinguished in your eventual economy simulation.

### Reset items and monetization

A July 2006 player-posted cash-shop item list describes a book that reset primary and secondary class selections, along with class skills.

*Community-reported*

The source suggests that changing a committed build could involve a premium item. It does not establish the final launch price or whether equivalent resets were earnable through ordinary gameplay.

This could create a frustration point: expensive mastery choices become especially punishing if players must pay to correct an ineffective build.

### What players liked

The early developer material strongly emphasized visually distinctive martial arts attacks. A 2006 MMORPG.com preview also praised the fighting presentation and compared its mixture of action and loot progression to familiar action RPGs.

These sources support the importance of the fantasy of performing martial arts.

They do not prove that players consistently preferred the precise skill-point formulas.

### What players complained about

Two historically documented criticisms stand out.

**Poor onboarding:** A July 2006 preview criticized the weak manual and lack of clear in-game guidance. The reviewer described players being confused about what to do, and another early account specifically warned that players needed enough Intelligence to use initial skill books.

**Repetitive progression:** The 2009 MMORPG.com review describes the early game as grind-heavy, with players needing time and external guidance to discover its depth.

These are contemporary reviewer observations, not representative satisfaction statistics.

I also could not verify a credible breakdown of how many players enjoyed the skill-book system versus conventional skill trees.

---

## 10. Implications for our mobile wuxia MMO-lite

Here's where I think the research becomes valuable for your actual game.

Our objective shouldn't be to recreate every original system. It's to preserve the feeling of learning martial arts, discovering rare techniques and becoming progressively more powerful.

The question is how to achieve that in a game played on a touchscreen, often in 10–20-minute sessions.

### What should we keep?

| Original Hero Online mechanic | My recommendation | Reason |
| --- | --- | --- |
| Martial arts books containing multiple forms | Keep | Strong identity, satisfying acquisition |
| Weapon-specific techniques | Keep | Makes weapon choice meaningful |
| Skill mastery ranks | Keep, simplify | Long-term progression |
| Separate skill points | Keep initially | Supports meaningful allocation decisions |
| Chi resource | Keep | Adds combat resource management |
| Class-specific abilities | Keep | Build diversity |
| Rare books from drops and quests | Keep | Exploration and economy |
| Skill-book trading | Consider for later | Player-driven economy |
| Intelligence requirement for nearly every book | Change | Can force undesirable attribute builds |
| Ten equipped books | Reduce substantially | Too many abilities for a mobile screen |
| Premium build resets | Avoid | Discourages experimentation |
| Extremely high late mastery costs | Rebalance | Risk of repetitive grinding |
| Long recovery downtime | Reduce | Poor mobile-session pacing |

---

## 11. My proposed skill system

I'd call this the **Martial Arts Library** internally for design purposes, but the final game can use terminology grounded in its own setting.

The architecture is inspired by the book-and-form relationship, not a direct recreation.

### A. Each book contains three techniques

For the first playable version, I'd use:

- One basic offensive technique.
- One specialized combat technique.
- One advanced technique, unlocked later through mastery.

For example, an original fictional spear discipline could have:

**The Mountain Spear** — *illustrative original discipline · Spear · 3 forms*

- **Form I — Piercing Thrust:** quick single-target damage. Accessible immediately.
- **Form II — Sweeping Arc:** wide attack that hits multiple nearby enemies.
- **Form III — Eagle's Descent:** high-impact finishing technique unlocked through mastery.

These are new design examples, not abilities documented in Hero Online.

The important point is that a book should feel like a complete combat style. Players shouldn't have to collect three unrelated objects to perform three moves from the same martial discipline.

### B. Six mastery ranks for the first version

I wouldn't start with twelve ranks. I'd use six, with stronger differences between them.

An example of how that could feel (the original was an interactive figure; this is its rank 1 state):

| Martial Mastery, rank 1/6 | |
| --- | --- |
| Skill power | 100% |
| Chi cost | 20 |
| Total points | 1 |

| Technique progression | Unlocks at |
| --- | --- |
| Form I — Basic technique | Unlocked |
| Form II — Specialized technique | Rank 3 |
| Form III — Advanced technique | Rank 5 |

This is a conceptual balancing example only. The displayed power, Chi and point costs are proposals.

The advantage is that every rank can offer a visible increase while unlocking new combat behavior at milestone ranks.

I would also avoid assuming that all forms in a book must deal the same percentage of damage. A defensive or mobility technique should have its own advancement logic.

---

## 12. Should skill books level up separately from players?

This is one of the biggest decisions we should make before implementation.

There are three viable options:

| Model | How it works | My assessment |
| --- | --- | --- |
| Character-level points | Leveling grants points to spend on books | Simple and predictable |
| Skill-specific XP | Using techniques gradually improves that discipline | Immersive but encourages repetition |
| Hybrid | Character progression grants points; mastery milestones also require practice | Strongest concept, more complexity |

For your playable prototype, I would start with character-earned skill points, not a separate use-based XP system.

That reduces implementation and balancing complexity.

Later, we could add specific mastery trials: for example, defeat an elite opponent with a spear to unlock a higher form. This captures the fiction of martial training without rewarding players for repeatedly spamming the same button against weak enemies.

---

## 13. How many abilities should players equip on mobile?

I would start with four active combat abilities plus a basic attack and dodge, and treat the book collection as the source of those abilities.

*(The original included a conceptual combat control layout diagram. Final control placement should be validated using the project's actual mobile combat camera and UI.)*

A character might know 15 techniques across different books, but only equip four active techniques for immediate combat.

That preserves collection depth without requiring players to navigate a crowded hotbar during a boss fight.

It also creates room for distinct loadouts, such as PvE farming, bosses and PvP.

---

## 14. Connect martial arts progression to the blacksmith

This is particularly important because we've already designed equipment enhancement around distinct stones:

- Ruby: damage.
- Jade: defense.
- Amber: accuracy.
- Onyx: dodge.
- Sapphire: skill damage.

That means the martial arts system cannot be balanced independently.

Consider two players carrying an identical +8 weapon.

One heavily invested in Ruby enhancements, while the other invested in Sapphire.

If both ordinary attacks and martial skills use exactly the same damage modifiers, those choices may become redundant.

I'd like the eventual combat architecture to distinguish:

| Stat | Primary role |
| --- | --- |
| Damage | Ordinary attacks and applicable weapon contributions |
| Skill Damage | Additional martial-technique output |
| Accuracy | Chance to land applicable attacks |
| Dodge | Defensive avoidance |
| Defense | Damage mitigation |
| Chi | Technique availability and resource management |

These are design intentions, not reconstructed Hero Online formulas.

We should determine precisely how each stat enters combat calculations before Claude implements the skill system. Otherwise the blacksmith and martial arts systems could create unintended damage multipliers.

---

## 15. What I'd build first in Unity

Since your game is already a playable prototype, I wouldn't hand Claude a document instructing it to create an entire martial arts framework from scratch.

I'd first have it assess how attacks, equipment and character stats are implemented.

Then I'd aim for a small vertical slice:

1. **One collectible martial arts book** — three original techniques associated with one existing weapon category.
2. **Six mastery ranks** — configurable point costs, damage and Chi consumption. Unlock the second and third forms at progression milestones.
3. **Three distinct combat behaviors** — single-target attack, area attack and a high-impact advanced technique.
4. **One mobile skill interface** — learn the book, spend mastery points, equip unlocked forms, preview Chi costs and test abilities in combat.
5. **Combat and balance tests** — test damage, cooldown behavior, Chi sustainability, animations and interactions with enhanced equipment.

This would let us validate the essential feeling of the original book-and-form system without committing to dozens of skills, classes and progression trees.

---

## 16. Most valuable source documents

These are the sources I'd retain for future architecture and game-design decisions.

| Source | Why it matters |
| --- | --- |
| Official Netgame skill catalog | Exact skill names, mastery costs, Chi tables and attack categories |
| Official terminology guide | 3–5 forms, mastery ranks and attribute definitions |
| Official skill interface guide | Skill learning, passive skills, Divine specialization and House Skills |
| June 2006 developer interview | Original profession progression and development context |
| August 2006 game description | Book slots, class progression, book sources and technique categories |
| Historical fan skill-point guide | Level requirements, rare-book acquisition and category-specific mastery costs |
| Japanese second-profession skill wiki | Numerical starting bonuses and training increments |
| Hero Plus 2023 Fierce Skill guide | Later skill tiers, range, area effects and immunity durations |
| Hero Plus 2025 update | Later Darkness Arts book maintenance |
| July 2006 hands-on preview | Gameplay experience, Chi use, class and skill progression |
| 2009 Hero Online review | Historical criticism of onboarding and repetitive leveling |

---

## Final assessment

I see three defining qualities of Hero Online's martial arts system:

**First:** techniques were discovered, not simply granted automatically. Books, trainers, quests and player trading helped martial arts feel like things that existed in the world.

**Second:** martial disciplines developed over time. A book wasn't just an item that unlocked one attack. It represented multiple forms and a substantial mastery investment.

**Third:** progression was connected to character building. Weapon choice, profession, stats, Chi and skill points all influenced what a character could learn and use.

Those features are supported by contemporary developer statements, historical player accounts and Netgame's official skill documentation.

For your game, I'd preserve the book-and-form structure, progressive mastery and rare technique discovery. I'd simplify the hotbar, avoid punishing early stat requirements and resist copying the original's steep grinding curves.

The next design decision I would prioritize is how martial techniques calculate damage and interact with weapon attacks. That decision affects skill mastery, Chi costs, class balance, equipment bonuses and our already-selected Balanced blacksmith.

The research gives us a strong historical structural foundation, but not a sufficiently verified combat formula to implement unchanged. That part needs to be designed and tested as an original system.
