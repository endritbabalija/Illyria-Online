# Hero Online — Crafting, Gathering & Resource Control

Historical mechanics research | 2006 original, later Hero Plus, international community sources | 24 October 2026

> The source links and the interactive figures (the material sources in §1, the crafting loop in §5, the mine hierarchy and economy loop in §6, the resource cards in §14, the yield calculator in §15, the blacksmith loop in §16 and the vertical slice in §19) didn't come through when this was pasted in; their text is kept below.

## Executive summary

The most important finding is that Hero Online's resource economy was designed to connect several different types of gameplay, rather than treating crafting as an isolated profession.

Players could obtain materials from monsters, dismantled equipment, gathering activities and contested resource territories. Certain professions produced different consumables, encouraging trading between players. The 2006 lead designer also described a faction-controlled mine where winning a battle granted temporary exclusive access to rare resources.

I would distinguish three historical layers:

- **2006 design:** Equipment dismantling, weapon and armor crafting, resource gathering and faction-controlled mining were explicitly described by the developer. The surviving contemporary material does not establish that every planned feature was fully operating at launch.
- **Expanded Hero Online:** Official undated guides describe gathering, mining, fishing, deciphering, class-related production, equipment sets and recipes.
- **Later Hero Plus:** Official dated guides establish more elaborate crafting, profession-independent mining access, specialized material processing, enhancement inheritance and event-based crafting bonuses.

That distinction matters. Some of the most interesting modern systems were not necessarily part of the experience players remember from 2006.

### Historical system summary

| Mechanic | Finding | Version and confidence |
| --- | --- | --- |
| World resource gathering | Materials distributed throughout maps | 2006 developer-documented |
| Contested mine | War every six hours; winner controls access until next battle | 2006 developer-described design |
| Equipment dismantling | Break existing equipment into crafting components | 2006 design; later official guide |
| Mining | A skill-based resource activity | Older guides, officially documented |
| Gathering | Includes herbs and other resources | Older guide, officially documented |
| Fishing | Appears as a gathering skill in advanced class books | Later/undated official guide |
| Deciphering | Retrieves resources from chests and tombstones | Older official guide |
| Profession-based production | Different jobs could create different battle consumables | Older official guide |
| Class interdependence | Some materials had to be obtained through trading | Older official guide |
| Equipment crafting | Basic equipment and special monster materials used to create new items | Older official guide |
| Set crafting | Crafted equipment could grant cumulative set bonuses | Older official guide |
| Mining unlock | Level 50+, second profession for regular classes | Hero Plus guide, July 2020 |
| Failed dismantling | Item survives; part of cost lost | Older official guide |
| Crafting inheritance | Some recipes preserve enhancements and sockets; others reset them | Hero Plus, August 2025 |
| Exact original gathering odds | Not authenticated | Unknown |
| Exact 2006 crafting success rates | Not authenticated | Unknown |

The central theme is resource specialization. A monster farmer, miner, crafter, blacksmith and PvP participant could all supply different parts of the same equipment economy.

---

## 1. Original 2006 crafting and gathering design

### The four sources of crafting materials

In June 2006, Hero Online lead designer Jack Stanley described three major material-acquisition approaches: collecting resources throughout the world, dismantling existing weapons and obtaining especially valuable resources from the contested Great Mine. The broader item system also included monster drops.

| Source | What it gives |
| --- | --- |
| World gathering | Resources available through exploration |
| Monster farming | Equipment and special materials |
| Dismantling | Turn unwanted equipment into ingredients |
| Contested mining | Access valuable resources through faction warfare |

→ **Equipment and consumable crafting:** materials are transformed through recipes and NPC services.

**Confidence:** developer-documented 2006 design. The exact release-stage availability, yields and recipe odds are not established by the interview.

### Hunter's original economic role

A June 2006 newcomer guide characterizes the Hunter as particularly capable of earning money through taming and selling pets, and says mining becomes available later. The early developer description also associated Hunter skill development with either animal taming or mining.

*Community-reported / developer-described*

The community guide goes further, calling Hunter the wealthiest profession. That should be treated as an individual player's assessment, not measured class-income data.

This created a distinctive role: a profession could generate value outside direct combat.

However, it does not prove that mining was exclusive to Hunters throughout Hero Online's history. Later Hero Plus explicitly provided mining to other professions.

---

## 2. The actual gathering professions

The older Netgame crafting guide documents four resource activities across the expanded system.

| Activity | Documented purpose | Specific resources |
| --- | --- | --- |
| Mining | Gather mineral resources | Ores, rocks and upgrade-related materials |
| Gathering | Collect natural resources | Herbs and other natural ingredients |
| Fishing | Gathering skill in advanced martial books | Full original catch table unknown |
| Deciphering | Recover resources from special objects | Chests and tombstones |

The old guide describes herb collection, mining rocks, and using Deciphering on randomly generated environmental objects.

The official Divine profession guide independently includes Fishing, Mining, Gathering and Deciphering as named abilities available through advanced profession skill books.

*Official documentation; exact introduction dates unknown*

### Random world objects

One particularly interesting feature is the mention of randomly generated gatherable objects.

This suggests gathering was not necessarily limited to permanent mining spots repeatedly clicked forever.

However, the source does not establish whether random meant randomized spawn locations, randomized contents, randomized appearances or a combination.

For your game, it would be unsafe to reconstruct a node-respawn algorithm from that sentence.

### Gathering progression and mastery

The surviving official material establishes that gathering actions were represented as skills, but I could not recover a complete table showing:

- Mining-skill mastery levels.
- Gathering XP per successful harvest.
- Whether the activity awarded character XP.
- Gathering success chance by skill rank.
- Tool durability.
- Resource-node health.
- Whether gathering was interrupted by damage.
- Base collection duration.
- Skill-based double-yield probabilities.

These are gaps, not evidence that the mechanics did not exist.

---

## 3. Eligibility and unlocking gathering skills

A particularly useful official document appeared on 1 July 2020, titled *How Do I Get the Mining Skill?*

It explicitly says that regular character professions could obtain Mining after their second promotion, starting at level 50.

### Hero Plus, 2020: mining by profession

| Profession family | Earliest documented regular-character unlock |
| --- | --- |
| Warrior → Musa / Champion | Second profession, level 50+ |
| Hunter → Elite Medic / Elite Hunter | Second profession, level 50+ |
| Mercenary/Assassin → Slayer / Shinobi | Second profession, level 50+ |
| Healer/Physician → Surgeon / Medic | Second profession, level 50+ |

The guide also identifies the relevant Mining skill inside specific second-, third- and fourth-profession martial books.

Separately, it lists Beast-class mining abilities associated with their species-specific books.

### The historical difference

In early 2006 descriptions, gathering and mining were especially associated with the Hunter's economic identity. By 2020, mining was explicitly available across the main profession families.

This is a meaningful design evolution.

**Inference:** Broader gathering access reduces dependence on a particular class, but may weaken the economic uniqueness of professions that originally specialized in resource acquisition.

For a mobile MMO-lite, I would prefer broad access to basic gathering and reserve specialist advantages for selected classes.

It avoids the risk that a small early player population cannot supply essential crafting materials.

---

## 4. Dismantling and recovering ingredients

The older official Netgame guide documents an important blacksmith-adjacent process.

Its example dismantles an existing weapon into:

- A weapon-construction book.
- Dense Iron.
- Gem Powder.

The guide says that material quantities vary.

### Dismantling failure

*Official, undated*

Unlike enhancement failures that could destroy equipment, this documented dismantling system returned the item when dismantling failed, while forfeiting part of the paid cost.

The exact success chance and refunded-fee percentage are not stated.

### Extraction was different

The same guide describes extracting reinforcement materials from an already enhanced item.

Its example reduces a +6 weapon to +5, with a chance of recovering strengthening materials. Even a failed extraction reduces the enhancement level.

That distinction matters because we've already incorporated extraction into your Balanced blacksmith.

**Design implication:** Your Dismantle action should handle turning unwanted gear into ingredients, while Extract should reverse one selected piece of enhancement progress. They should remain distinct rather than become two buttons returning the same material types.

---

## 5. Recipes and class-produced consumables

The older Netgame crafting guide states that professions could create battle consumables using materials gathered through job skills.

It further specifies that some required materials originated from other professions, encouraging trading.

### The intended crafting loop

1. Specialist gathers a resource.
2. Other profession supplies an ingredient.
3. NPC processes the recipe.
4. Combat consumable enters the economy.

The intriguing part is the cross-profession dependency, not any one recipe.

But I could not verify a complete original 2006 recipe table with quantities and success rates for each profession.

I also could not establish whether production ingredients were exclusively locked to particular classes or whether players could purchase every component through normal trading.

### Community-documented materials from 2010

A September 2010 guide describes materials for crafting equipment above level 100.

This is community evidence about an expanded game, not a 2006 launch recipe.

| Activity | Examples documented in 2010 |
| --- | --- |
| Gathering | Ginseng, herbs, plant roots, colored fruit skins, cocoons |
| Mining | Silver/gold ore, bronze bars, coal, hard rock, meteorite-type materials |
| Deciphering | Recipes, weapon patterns, armor patterns and crafting books |
| Monster farming | Bones, animal skins, powders, processed ingredients |

The guide specifically distinguishes gathering, mining, deciphering and monster drops as different sources of the materials needed to make higher-level equipment.

*Community-reported, September 2010*

It also reports that some dropped resources could be collected only by particular advanced professions, but the underlying engine restrictions are not independently verified.

**What this suggests:** Resource acquisition was not necessarily a single universal loot table. Specialized activities generated different classes of ingredients, which could then converge in equipment crafting.

---

## 6. The Great Mine and Golden Castle: resource control

This is the most interesting historical feature for your game because it ties PvP directly to crafting.

### Original 2006 vision

The lead designer's June 2006 description was simple:

- A valuable mine exists in contested PvP territory.
- Factions fight for control every six hours.
- The winner gains exclusive access until the next war.
- The mine supplies unusually valuable resources for equipment crafting.

*Developer-documented design, 2006*

The interview does not specify the mine's complete loot table, exact battle duration or gathering rates.

### Hero Plus: Golden Castle

By June 2020, Netgame had published detailed Golden Castle instructions. The system contained three progressively more valuable mining floors.

*Official, June 2020*

**Golden Castle mining hierarchy**

- **Floor 1 — Basic resources:** common minerals, first-tier upgrade stones and lower socket materials.
- **Floor 2 — Improved resources:** ordinary minerals, first- and second-tier enhancement stones, improved socket minerals.
- **Floor 3 — Highest-value resources:** higher-tier crystals and crystal balls, premium socket minerals and special materials.

The official item descriptions distinguish the floors, but do not publish the complete chance of obtaining each item.

### Documented mining materials

| Floor | Resource categories |
| --- | --- |
| First | Gold, silver, coal, rock, ordinary enhancement stones, basic socket ores |
| Second | First-tier stones, second-tier crystals, intermediate/expert socket resources |
| Third | First-tier stones, second-tier crystals, third-tier crystal balls, premium socket resources |

Special materials such as Seven Star Stone, Pure Stone Liquid, Gold Sand and Purification Gem also appear in the mine's published item lists.

### Exact cooldowns

The official Golden Castle warfare guide provides two separate timing values:

| Timer | Value |
| --- | --- |
| Mine-node respawn (across the three floors) | 3 min |
| Harvest cooldown (documented harvest restriction) | 30 min |

The guide does not specify whether every harvest action within all mining areas shared exactly the same personal cooldown, nor whether bonuses could modify it.

### Winner and loser access

Here the official descriptions are not completely consistent.

The Golden Castle warfare guide says the winning faction receives substantially better mining access, including access to a special undiscovered mine. It also says the defeated faction can obtain a small amount of material.

An older warfare page uses stronger language about the victorious faction's exclusive access to a special mining zone.

These can coexist if ordinary mine floors are partially accessible to both sides while a bonus area remains exclusive.

However, the documents do not establish one perfectly consistent access matrix for every historical version.

**The safest conclusion:** Victory provided a meaningful resource advantage, including special-area access in some documented rules. It is not safe to assume every loser was excluded from every mining floor.

### The mine's role in the player economy

The June 2020 guide explicitly identifies mined resources as materials used for enhancement and sockets.

This makes the system economically connected to equipment progression rather than only producing generic crafting junk.

Faction warfare determines resource access → players mine valuable materials → materials enter marketplace or crafting → enhanced equipment improves character combat power

*Economic feedback-loop inference based on the documented systems, not a measured historical price cycle.*

This is powerful, but also potentially dangerous: successful factions may accumulate better resources and become even harder to defeat.

We should preserve the conflict over bonus resources while avoiding a permanent progression monopoly.

### Golden Castle special rewards

A later official guide states that the victorious controlling faction's House Master could receive 1 billion gold. It also describes access to special vendors and exchanges for mineral-related goods.

This is documented Hero Plus behavior, not an original 2006 reward amount.

It also constitutes a potential gold source, unlike blacksmith fees, which remove gold.

One billion gold going to a guild leader may create a very different incentive from distributing crafting materials among actual contributors.

The original rationale for making guild leaders personally control valuable rewards is not recoverable from the source.

---

## 7. Material processing: unrefined minerals

A July 2023 Hero Plus guide describes a specialized resource-processing mechanic.

Mining produced Unrefined Minerals associated with the three mine floors. These could yield a variety of upgrade materials.

### The documented quantity

The guide states that players could obtain 40 minerals from an Unrefined Mineral, and that the third-floor version had the highest likelihood of producing better materials.

*Official, July 2023*

This appears to be a bundled material-opening mechanic rather than one raw ore always converting into one fixed crafting ingredient.

The document does not authenticate the exact probability of each possible output.

### Why it matters

There are two separate reward moments:

- Acquiring the unrefined resource through gameplay.
- Processing or opening it to reveal its contents.

That can add excitement to gathering.

But it also adds probability layers to the player's economy. If the raw mineral is tradeable, its market price will reflect the expected value and rarity of possible contents.

**Inference:** Creating too many such containers can make prices harder for casual players to understand.

For our project, I would initially let mined resources grant visible, predictable materials. Rare bonus finds can exist without making every harvest an unidentified loot box.

---

## 8. Later equipment crafting: precise recipes and failure outcomes

The April 2025 Hero Plus Virago accessory guide provides a concrete example of advanced crafting.

### Verified recipe

One accessory recipe requires:

| Ingredient | Quantity |
| --- | --- |
| Previous-tier accessory | 1 |
| Demon material | 150 |
| Special accessory decoration | 1 |
| Dense-iron component | 250 |
| Condensed stone | 30 |

*Official, April 2025*

These are the game's documented quantities, not proposed values for your game.

### Crafting failure

The guide specifies a very important failure behavior.

On failure:

- The old accessory survives.
- The other required ingredients are consumed.

On success:

- The new accessory is produced.
- The prior identification options transfer.
- Enhancement level transfers.
- Socket properties transfer.

This is substantially more forgiving toward the principal equipment item than the high-level failure rules of the classic blacksmith.

**Design implication:** There may be good reason to distinguish high-risk enhancement from investment-preserving equipment evolution.

A player could risk enhancement for greater power but still carry their favorite weapon forward when crafting a higher-tier version.

### Important later-version complication

An official August 2025 guide explicitly states that not every crafting recipe preserves equipment enhancements.

Some recipes inherit level and socket data; others reset those properties. The UI marks which outcome applies.

This creates a usability requirement: the player must see precisely what happens to their existing equipment before crafting.

### Missing recipe details

The April 2025 guide does not publish the success percentage for that accessory recipe.

In June 2025, an official event announced temporarily increased crafting success rates for the same accessory family. That proves the probability could be altered through events, but not its normal baseline value.

---

## 9. Later resource recycling: Marble dismantling and crafting

The May 2023 official dismantling guide describes breaking Marbles into materials based on grade.

| Input grade | Materials returned |
| --- | --- |
| Normal | Marble Piece |
| Advanced | Marble Piece and Marble Energy |
| Rare or higher | Marble Piece, Marble Energy and Mystic Jewel |

Higher grades increased the potential quantity of materials. A special supporting stone increased the chance of receiving additional materials.

*Official, May 2023*

A later Marble Craft guide states that crafting a Marble requires at least ten Marble Pieces and gold, with costs depending on the target Marble.

This is not the same mechanic as original weapon dismantling.

It is a later resource-conversion system with separate progression and rarity rules.

### Design significance

This reveals how Hero Plus accumulated interconnected economic systems:

- Equipment becomes crafting materials.
- Marbles become Marble Pieces and other components.
- Pieces become higher-grade Marbles.
- Special consumables modify yields.
- Different crafting recipes change the value of different materials.

Each system adds potential item demand, but also inventory burden.

For a small mobile MMO-lite, I would avoid layering on these secondary material-conversion economies until the first crafting loop has been tested.

---

## 10. Economic and social effects

### A. Crafting could create demand for several kinds of players

The historical sources describe materials originating from distinct activities and professions, with trading used to obtain ingredients a character could not produce alone.

That provides a possible specialization economy:

| Player activity | Supply created | Likely consumer |
| --- | --- | --- |
| Monster farming | Equipment, skins, bones, special drops | Blacksmiths and equipment crafters |
| Mining | Ores, enhancement minerals, stone resources | Blacksmiths and traders |
| Herbal gathering | Plants and medicinal ingredients | Consumable crafters |
| Deciphering | Patterns, recipes and special components | Advanced equipment crafters |
| PvP resource control | Improved access to scarce minerals | Faction members and market buyers |

The supply categories are supported by the older official guide and 2010 community documentation. The buyer relationships are economic inferences, not reconstructed transaction records.

### B. Gold sinks versus material sinks

Not all crafting activity affects the economy in the same way.

- **Gold sinks:** NPC production fees, dismantling fees, extraction fees and certain equipment-crafting fees remove currency when charged by the game.
- **Material sinks:** Successful recipes and failed attempts consume ingredients. Combining materials changes inventory supply but does not necessarily remove an equivalent amount of economic value.
- **Equipment sinks:** Dismantling removes an equipment instance and creates components.
- **Gold sources:** Monster gold drops and documented faction-leader rewards introduce currency.

The Golden Castle guide's billion-gold reward is a particularly large nominal gold source, although its actual impact depends on the economy's size.

The nominal amount alone cannot establish whether it caused meaningful inflation.

### C. Rare ingredients and the marketplace

If a recipe requires hundreds of a particular component, the material can retain demand across many crafting attempts.

The 2025 Virago recipe, for example, consumes 150 demon pieces and 250 dense-iron pieces in one attempt.

That is structurally important: resource value comes from recipes that consume resources repeatedly.

However, a recipe requiring hundreds of items can also create tedious farming if drop rates are low and trading is inconvenient.

### D. Evidence about player satisfaction

The community evidence is significantly weaker here than the mechanical documentation.

A June 2006 beginner guide praises Hunters as profitable because of pet taming and eventual mining. This is one player's perception of class earning potential.

A separate July 2006 forum discussion strongly criticizes repetitive monster farming, low-value drops, crowding and bot activity. One participant nevertheless describes the enhancement system as one of the game's more interesting features.

These observations reveal a possible tension: resource acquisition can give grinding purpose, but it doesn't automatically make the underlying activity fun.

I did not find a representative Reddit, YouTube or Korean-language survey specifically evaluating Hero Online gathering and crafting.

**What players may have liked — inference**

The documented mechanics suggest possible appeal in obtaining rare resources, manufacturing useful items, supplying other professions and competing for economically valuable territory.

But I cannot honestly label those as demonstrated community consensus.

**What I would avoid — grounded design interpretation**

Avoid making the optimal playstyle endless clicking at a fixed rock, exclusively gating essential resources behind PvP, or requiring huge ingredient quantities simply to extend the grind.

Those recommendations follow from the documented system structure and contemporary complaints, rather than proving any particular feature caused player dissatisfaction.

---

## 11. User interface and quality-of-life findings

### Original and older Netgame UI

The older official guide places Dismantle and Extract within the blacksmith NPC interface. Players select an action, insert equipment into a designated slot and confirm.

The older equipment-crafting guide describes NPC-based production but does not provide a sufficiently complete text description of every recipe-interface field.

I could not verify whether the original crafting interface consistently showed:

- Exact success percentages.
- A preview of every possible output.
- Ingredient source locations.
- Missing-ingredient tracking.
- A warning before consuming rare equipment.
- A list of recipes the player was eligible to craft.
- A complete crafting history.

### Later Hero Plus improvements

The 2025 crafting-inheritance guide specifically explains that the interface indicated whether an existing item's enhancement and socket properties would transfer or reset.

The 2023 Marble guide also instructs players to inspect output materials and gold costs before dismantling.

Those are useful signs of more transparent resource processing.

**My recommendation:** Your crafting screen should go further by displaying every ingredient quantity, the gold cost, the actual success probability, and the exact failure consequence before committing.

For mobile, the interface should also offer a way to locate missing ingredients without requiring players to consult external wikis.

---

## 12. Version differences and unverified systems

### Version comparison

| Feature | 2006 Hero Online | Expanded official guides | Hero Plus 2020–26 |
| --- | --- | --- | --- |
| World resource gathering | Developer-described | Herbs, ores, special objects | Still supported |
| Mining specialization | Hunter-focused descriptions | Advanced gathering skills | Mining for multiple profession families |
| Equipment dismantling | Developer-described | Detailed outputs and failures | Expanded recycling systems |
| Contested mine | Six-hour faction war described | Castle territory system | Three-floor Golden Castle documented |
| Resource tiers by depth | Not verified | Not clearly dated | Explicit floor-dependent tables |
| Equipment-set crafting | Not established at launch | Documented | Expanded recipes |
| Crafting failure | Exact early rule unknown | Recipe-specific rules | Ingredients consumed, core equipment sometimes preserved |
| Enhancement inheritance | Not established | Not universally documented | Recipe-specific inheritance |
| Exact drop-rate tables | Unknown | Mostly unknown | Mostly unpublished |
| Special mining materials | Rare resources described | Further categories documented | Detailed minerals, stones and socket resources |

### Hero Online Global and The Hero Global

I couldn't independently authenticate a complete crafting/gathering manual under either specific release name.

Public Netgame Hero Plus guides should not automatically be attributed to similarly named international servers.

For both versions, I could not verify distinct numerical recipes, gathering cooldowns, resource requirements or mining drop percentages.

### Private servers

I located older Hero Online development discussions and community guide material, but no audited private-server crafting database with verified version provenance and complete formulas.

The September 2010 level-101+ crafting guide is useful historical community documentation, but it does not establish the rules of a particular private server.

Also, documentation for heRO on pandoraonline.net belongs to a Ragnarok Online private server, not the Korean wuxia MMORPG. Its mining figures must not be reused here.

### Exact unresolved numbers

| Question | Status |
| --- | --- |
| 2006 herb and ore gathering rates | Unknown |
| Original mining unlock level | Incomplete evidence |
| 2006 mining cooldown | Unknown |
| Original gatherable-object respawn | Unknown |
| Skill level versus collection quality | Unknown |
| Original equipment crafting chances | Unknown |
| Dismantling success probabilities | Unknown |
| 2025 Virago crafting baseline probability | Unknown |
| Exact Golden Castle material drop odds | Unknown |
| Exact PvP ownership effect on market prices | Unknown |
| Average resource yield per player-hour | Unknown |
| Complete fishing catch tables | Not recovered |
| Profession production recipes at 2006 launch | Not recovered |

The precise 3-minute node respawn and 30-minute harvest cooldown are documented for the later Golden Castle system, not original 2006 gathering generally.

---

## 13. Implications for our design

This is where I would make a clear decision.

For your game, gathering should primarily exist to support the blacksmith, equipment crafting and player economy—not become an entirely separate game of repetitive professions.

We already have meaningful progression systems planned:

- Equipment and loot.
- Balanced blacksmith, +0 to +8.
- Rough and Polished stones.
- Martial arts books and skill mastery.
- Pets and mounts.
- Player trading.
- Faction PvP.

The gathering system should connect these, rather than adding six new independent currencies and progression tracks.

### What to keep, change and avoid

| Hero Online feature | Recommendation | Reason |
| --- | --- | --- |
| Ore mining | Keep | Direct blacksmith material source |
| Herbal gathering | Keep | Useful consumable crafting |
| Equipment dismantling | Keep | Already part of your blacksmith design |
| Monster-derived crafting materials | Keep | Rewards combat farming |
| Deciphering treasure objects | Consider | Adds exploration variety |
| Fishing | Defer | Less essential to current combat loop |
| Class-based gathering advantages | Adapt | Preserve identity without locking out essential resources |
| Profession-dependent production | Simplify | Avoid low-population supply shortages |
| Faction-controlled mining | Keep later | Strong PvP/economy connection |
| Three-tier mine depth | Consider | Intuitive risk/reward progression |
| Equipment evolution recipes | Keep later | Protects investments in rare gear |
| Giant crafting recipes | Avoid initially | Unnecessary material inflation |
| Multiple crafting success boosters | Avoid | Duplicates blacksmith complexity |
| Secondary socket crafting | Defer | Too many overlapping systems |

---

## 14. A proposed starting gathering economy

I would begin with three basic resource categories.

| Resource | Used for |
| --- | --- |
| **Ore** | Equipment crafting, repairs and blacksmith-related production |
| **Herbs** | Healing, Chi restoration and utility consumables |
| **Crystals** | Enhancement stones and limited rare crafting recipes |

Illustrative resource directions for your game; not original Hero Online assets.

You can introduce different item subtypes later, but three broad categories are enough to validate the complete economic loop.

### Proposed acquisition methods

| Resource | Ordinary acquisition | Alternative |
| --- | --- | --- |
| Ore | Mining nodes | Dismantling gear |
| Herbs | Plants in outdoor areas | Enemy drops and quests |
| Rough stones | Monster drops | Crystal nodes and limited crafting |
| Rare components | Boss drops | Special gathering areas |
| Polished stones | Combine three Rough stones | Later rare rewards |

These proposals deliberately connect to systems we've already designed.

---

## 15. Initial gathering numbers to test

These are original design proposals, not Hero Online statistics.

| Parameter | Starting test |
| --- | --- |
| Basic gathering interaction | 3 seconds |
| Basic node yield | 2–4 resources |
| Basic node respawn | 90 seconds |
| Rare node respawn | 5 minutes |
| Basic gathering unlock | Early game |
| Resource carry limit | Existing inventory constraints |
| Gathering failure | None for ordinary nodes |
| Gathering tools | No durability system at launch |
| Contested node bonus yield | +20% |
| Contest ownership window | 2 hours |

These values should be revised after we know actual map population, travel time, equipment costs and desired gathering volume.

For example, a three-second interaction is reasonable only if moving between resources takes meaningful time. Otherwise players could accumulate hundreds of materials almost instantly.

### Test the gathering loop

**Gathering yield calculator** (proposal; the original was interactive)

| | |
| --- | --- |
| Nodes harvested per session | 20 |
| Average materials per node | 3 |
| Apply +20% contested territory bonus | off |
| Expected total materials | 60 |

Expected value only. Assumes every selected node is successfully harvested and does not model travel or competition.

For a session with 20 successful nodes and three materials per node, the baseline output is 60 resources. A 20% ownership bonus raises that expectation to 72.

The more important metric is materials per active minute, including movement, waiting and competing with other players.

---

## 16. Connecting gathering to our Balanced blacksmith

Here I would make one important adjustment to the economic design.

Your blacksmith's primary consumable is the enhancement stone. Players may use one, two or three stones per attempt, and they can combine three Rough stones into one Polished.

If crystal mining gives stones too frequently, the marketplace value of ordinary stones could collapse.

If it gives stones too rarely, the blacksmith becomes inaccessible to casual players.

Therefore, resource nodes should not simply produce an unlimited random assortment of enhancement stones.

### Proposed principle

**Monster combat remains the primary Rough-stone source. Crystal gathering provides a secondary, targeted source.**

For example, a crystal node might have a visible type so players can intentionally gather Ruby-related or Jade-related ingredients.

That gives gathering a purpose beyond generic resource accumulation and lets players pursue specific builds.

The exact probability and conversion recipes should be tested rather than finalized now.

### A more interesting economic loop

1. Fight monsters and gather resources.
2. Keep, craft or sell the materials.
3. Enhance equipment through the blacksmith.
4. Trade better items or use them in harder content.

I would postpone recipes that require ten unrelated material categories.

That keeps your economy easier to understand, tune and simulate.

---

## 17. Faction-controlled gathering without monopolies

I like Hero Online's Great Mine concept, but I wouldn't implement absolute exclusivity over essential resources.

Instead:

- All players can obtain ordinary ore and Rough stones through PvE.
- The victorious faction gains access to an optional bonus mine.
- The mine offers more efficient gathering and a small chance at rare crafting ingredients.
- Ownership resets regularly.
- Losing players retain access to alternative resource sources.
- Rewards favor actual participation rather than simply belonging to the largest faction.

For the first implementation, I would use a temporary bonus rather than ownership of the entire resource economy.

This captures the original fantasy of fighting for a mine without allowing the leading faction to starve competitors of equipment progression.

---

## 18. Crafting recipes worth building first

Instead of introducing a large recipe database, I recommend a minimal set of recipes tied to concrete gameplay needs.

| Recipe category | Example proposed inputs | Output |
| --- | --- | --- |
| Restoration consumable | 3 Herbs + 20 gold | Health consumable |
| Chi consumable | 3 Herbs + 1 Crystal Fragment + 40 gold | Chi consumable |
| Equipment material | 5 Ore + 100 gold | Refined metal |
| Equipment crafting | Refined metal + monster material + gold | Basic equipment item |
| Stone combination | 3 matching Rough stones + 150 gold | 1 Polished stone |

These are starting recipes, not historical Hero Online data.

The stone combination is already part of your blacksmith draft. It should remain one shared recipe/service, not be duplicated into a separate crafting system.

### What I would not implement yet

Fishing equipment, several gathering professions, herb quality tiers, tool enhancement, profession-specific crafting benches, dozens of intermediate materials and random success chances for ordinary consumables.

Those can create complexity without proving the core resource economy is enjoyable.

---

## 19. Unity architecture and implementation order

Before implementation, Claude Code should assess your existing Unity project.

The goal should be to reuse the same inventory, equipment and transaction infrastructure already planned for loot and the blacksmith.

### Minimum architecture

The system needs to represent:

- A resource definition: type, stackability, visual and inventory ID.
- A world resource node: type, current availability and respawn behavior.
- A gathering interaction: eligibility, distance, action duration and interruption rules.
- A loot or yield table: deterministic and probabilistic outputs.
- A recipe definition: inputs, output, gold fee and outcome behavior.
- An authoritative resource transaction: consume inputs and award outputs once.
- Persistence and server synchronization where multiplayer already exists.

I would avoid introducing a separate inventory or crafting-currency system.

### Minimal vertical slice

1. **One mine resource node** — player approaches, interacts, receives ore, and the node respawns.
2. **One herb resource node** — same interaction infrastructure, different resource output.
3. **One crafting recipe** — uses existing inventory and gold to make a useful consumable.
4. **Blacksmith integration** — ore/dismantling resources feed existing equipment progression.
5. **Economy validation** — track harvested, crafted, sold and destroyed resource quantities.

Then validate how the loop actually feels before building advanced crafting or contested territory.

---

## 20. Primary sources and evidence quality

| Source | Evidence and reliability |
| --- | --- |
| June 2006 lead designer interview | Primary historical source: materials, dismantling, Great Mine, six-hour control |
| Older Netgame crafting guide | Official, undated: job production, gathering, mining, deciphering, set equipment |
| Netgame dismantling/extraction guide | Official, undated: example materials, fees and failure behavior |
| June 2006 community guide | Contemporary player account: Hunter mining role and class economy |
| September 2010 crafting material guide | Community-reported detailed gathering and production ingredients |
| July 2020 mining skill guide | Official and dated: mining eligibility by profession |
| June 2020 Golden Castle guide | Official: three floors, materials, mining access, faction rewards |
| Official Golden Castle warfare rules | Official, undated: resource ownership, node respawn and harvest cooldown |
| July 2023 mineral and enhancement guide | Official: unrefined minerals, stone tiers and floor-dependent rewards |
| May 2023 Marble dismantling guide | Official: materials returned by Marble grade |
| April 2025 Virago crafting guide | Official: detailed recipes, failure rules and enhancement inheritance |
| August 2025 inheritance guide | Official: when crafting preserves or resets existing progression |
| January 2026 development roadmap | Official: planned material-drop and mining-reward improvements |
| July 2006 community feedback | Contemporary small-sample complaints about grind, drops and bots |

---

## Final assessment

The most distinctive feature of Hero Online's resource design wasn't mining itself.

It was the way resource acquisition connected different types of players.

A Hunter could specialize in acquiring valuable resources. PvE players produced equipment and monster materials. Crafters transformed those inputs. Traders moved goods between players. Factions fought for access to better gathering opportunities.

Later Hero Plus expanded that foundation into three-floor mines, specialized stone tiers, advanced recipes and progression inheritance. Those additions brought more depth, but also considerably more material and interface complexity.

For your mobile game, I'd preserve the connections between gathering, combat, trading and territory ownership, but drastically simplify the number of resource categories and production steps at launch.

The most important lesson for the Balanced blacksmith is that material supply determines whether forging feels rewarding or exhausting. If we build gathering without modeling stone consumption, equipment destruction and player trading together, we'll risk creating either a flooded marketplace or a progression bottleneck.

My preferred first version: ore and herbs in the world, enhancement materials from combat and limited crystal gathering, a handful of useful recipes, and contested resource bonuses added only after the basic PvP and marketplace infrastructure is validated.

That gives us the economic identity of classic Hero Online without turning your Unity prototype into a full-scale crafting simulator before the main combat and progression loops are established.
