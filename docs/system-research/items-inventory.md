# Hero Online — Items, Consumables & Inventory Management

Historical systems research · Original 2006 Hero Online, later Hero Plus, international releases and community documentation · 24 October 2026

> The source links and the interactive figures (the stack-limit callout in §3, the mobile inventory mock-up in §15 and the item-foundation diagram in §16) didn't come through when this was pasted in; their text is kept below.

## Executive summary

Yes, this was an important missing piece of our research. We previously examined what equipment players acquired, how they upgraded it and how they traded it. We hadn't properly examined the rules governing everything they carried, consumed, stored, transferred or lost.

Several important findings emerged.

**First,** original Hero Online had both inventory-space and weight considerations. The older official interface guide describes weight-limited inventory and stackable consumables, while a June 2006 firsthand preview specifically reports 120 shared bank slots.

**Second,** the item ecosystem was already extensive in 2006. In addition to normal potions, a contemporary cash-shop listing describes reservoir-style healing items, stat-reset consumables, temporary attribute buffs, resurrection items, teleport scrolls, pet conveniences and storage expansions.

**Third,** items had different kinds of restrictions. By May 2026, the official game explicitly distinguished whether an item could be traded, stored, consigned or used in Soul Transfer. These were separate properties rather than one universal bound/unbound flag.

**Fourth,** inventory convenience became a major concern in later Hero Plus. In May 2026, Netgame raised the general maximum stack quantity to 9,999, while later patches separately enabled stacking for individual item categories.

For our Unity game, the underlying lesson is that the inventory needs to be a shared foundation for combat, blacksmithing, loot, pets, crafting, trading and quests. These systems should not each invent their own interpretation of what an item is.

---

## 1. Historical mechanics summary

| Mechanic | Recovered finding | Confidence / era |
| --- | --- | --- |
| Inventory | Carries equipment, consumables and other items | Official guide, undated |
| Carry weight | Explicit weight limit | Official guide, undated |
| Consumable stacking | Consumables described as stored in hundreds | Official guide, undated |
| Shared personal bank | 120 slots, shared among characters | Firsthand, June 2006 |
| Account character limit | Three characters in a June 2006 preview | Contemporary preview |
| Equipment requirements | Stat and weapon compatibility, sometimes gender-specific | Contemporary, 2006 |
| Quickslot pages | Four pages | Official guide, undated |
| Recovery | Health and Chi potions, meditation and recovery items | Contemporary/official |
| Internal injury | Separate condition curable through meditation or consumables | Contemporary/official |
| Reservoir potions | 200K–1M recovery capacity examples | 2006 community cash-shop listing |
| Attribute buffs | +15 STR/DEX/INT for 48 hours connected | 2006 community listing |
| Stat resets | Complete and partial attribute resets | 2006 community listing |
| Teleport consumables | Safe-zone destination and return functionality | 2006 community listing |
| Item pickup automation | Offensive-pet auto-loot convenience | 2006 community listing |
| Revival consumables | XP recovery and other revival effects | 2006 community listing |
| Storage expansions | Time-limited premium convenience | 2006 community listing |
| Item trade/storage flags | Separate transfer restrictions | Official, 2026 |
| General stack maximum | 9,999 | Official, May 2026 |
| Storage and inventory gold caps | 9,999 billion in detailed notice | Official, May 2026 |
| Original exact inventory slot count | Not verified | Unknown |
| Original weight formula | Not recovered | Unknown |
| Original potion cooldowns | Not recovered | Unknown |
| Original item-loss probability on death | Not established | Unknown |

**Chronology matters:** The official beginner guide contains references to later cash equipment, expanded slots and pet gear. It preserves an older-looking interface, but its entire content cannot be assumed to describe the original July 2006 release unchanged.

---

## 2. Complete item taxonomy

I couldn't recover an official 2006 database listing every item ID and category. However, the sources establish a fairly broad functional taxonomy.

| Category | Examples | Historical evidence |
| --- | --- | --- |
| Weapons | Swords, spears, bows and other weapon families | Original equipment guides |
| Armor | Helmets, body armor, boots, masks | Official inventory interface |
| Accessories | Rings, protective and stat-related items | Official equipment information |
| Enhancement resources | Gems, strengthening stones and associated charms | 2006 previews and cash-shop guide |
| Recovery consumables | Health and Chi potions | Official UI and 2006 community list |
| Pet consumables | Pet food, restoration and revival | Official guide and community list |
| Buff consumables | Temporary STR, DEX and INT increases | Community, August 2006 |
| Travel items | Destination and return scrolls | Community, July–August 2006 |
| Revival items | Self/party revival and XP recovery | Community, 2006 |
| Skill books | Martial techniques, mastery and resets | Original game documentation |
| Quest items | Non-tradable/non-discardable mission objects | Official glossary |
| Crafting materials | Ores, monster materials, recipes and powders | Older crafting guides |
| Random containers | Fortune Boxes and later event boxes | 2006 firsthand and Hero Plus |
| Cosmetic items | Costumes, masks and additional appearance gear | 2006 cash-shop listing |
| Utility items | Inventory/storage expansion, auto-loot, respecs | 2006 community listing |
| Event currencies/items | Seasonal tokens, tickets and temporary rewards | Later Hero Plus updates |

The crucial distinction is between an item's purpose, its storage representation and its transfer permissions.

A potion, a quest document and a +8 sword might all be inventory objects, but their lifecycle rules are fundamentally different.

---

## 3. Inventory size, weight and stacking

### A. Inventory weight

The Netgame interface guide states that the inventory had a weight limit and displayed its current weight near the bottom.

*Official guide, undated*

The game's attribute descriptions associate Strength with carrying capacity, making inventory burden another consideration in character builds.

However, the surviving documentation doesn't establish:

- The starting maximum weight.
- Weight increase per STR.
- Exact weight of each consumable or equipment class.
- Whether an overweight character could still move.
- Whether excessive weight reduced movement speed.
- Whether items could be looted while overweight.
- Whether pet carrying capacity increased effective player weight limits.

Do not assume a linear weight formula.

### B. Item slots

We have two different pieces of information:

- The 2006 preview gives 120 slots for the shared bank.
- The older interface guide documents the inventory but does not give a reliable base backpack slot count in the retrieved text.

These should not be confused.

The existence of 120 bank slots does not establish 120 personal backpack slots.

### C. Stacking in earlier versions

The official interface guide says consumables could be stored in quantities of hundreds.

This establishes stacking, but doesn't prove an exact maximum for every consumable or for ores, books, gems and quest materials.

### D. Stack expansion in May 2026

*Official, 19 May 2026*

Netgame announced a **general maximum stack quantity of 9,999** per eligible stackable item after the update.

This was not a statement that every object in the game became stackable.

Subsequent July and August 2026 updates specifically added stacking support for individual containers and consumables.

### Why that matters

A global maximum stack size and whether an item may stack at all are independent rules.

An equipment item with its own rolled bonuses must remain an individual instance even if ordinary ore can stack to 9,999.

The May 2026 update also added larger-number input controls and improved wealth readability, confirming that large item and currency quantities had become a user-interface concern.

---

## 4. Banking and shared storage

This is among the strongest original-era findings.

A June 2006 firsthand preview reports:

- 120 bank slots.
- Shared bank access across the account's characters.
- The ability to transfer equipment and gold indirectly through bank storage.
- Three available character slots per account in that release description.

*Contemporary firsthand / preview evidence, 2006*

### What account sharing enabled

Imagine one character specializing in swords and another in bows.

If the sword character found equipment suitable for the bow character, they could store it in the shared bank and retrieve it after switching characters.

This is important because early equipment had character and stat requirements.

**Inference:** Shared storage could make off-build loot more useful instead of forcing players to sell every incompatible item.

### Later storage expansion

A community cash-shop listing dated August 2006 describes a storage-expansion item lasting 30 days. After expiry, items could be removed but new deposits were blocked.

That is a reasonably player-friendly expiration mechanism: access changes without immediately destroying stored items.

The official 2026 site still lists timed storage and bag expansions, including 30-day and 100-day examples.

### Account-bank rules still missing

I couldn't verify:

- Original deposit and withdrawal fees.
- Whether bound items were accepted.
- Whether quest objects could be stored.
- Whether storage slots had a separate weight limit.
- Whether every character on the account shared the same bank after server transfers.
- Whether the 120-slot capacity remained unchanged after updates.
- Whether item storage permissions could vary by character class or faction.

### Shared bank versus guild bank

Our earlier guild research identified institutional House storage with separate monthly costs and withdrawal permissions.

That is a different system from the 120-slot account bank.

For your Unity game, personal inventory, account storage, guild treasury and marketplace escrow should have distinct ownership rules even if they use the same underlying item data structures.

---

## 5. Recovery items, potions and internal injuries

Original Hero Online had three separate recovery concerns: Health, Chi and Internal Injury.

The Netgame interface guide describes HP and Chi meters, plus a separate Internal Injury meter. Accumulating Internal Injury could reduce attack/defense and restrict skill use; meditation could recover it.

### A. Ordinary recovery consumables

The official quickslot description identifies Health Potions, Chi Potions and Pet Food as frequently used consumables.

The original design distinguished resource recovery from fighting effectiveness:

| Recovery need | Mechanic | Historical status |
| --- | --- | --- |
| Restore HP | Health potion | Officially documented |
| Restore Chi | Chi potion | Officially documented |
| Recover Internal Injury | Meditation and special consumables | Official/community-documented |
| Restore pet resources | Food and pet recovery items | Official/community-documented |
| Recover lost XP on death | Specialized revival consumable | Community-documented, 2006 |

A July 2006 contemporary preview also criticized slow Chi regeneration, noting that limited Chi made sustained skill use difficult during monster grinding.

That gives recovery items a meaningful gameplay role: Chi availability affected how often martial techniques could be used.

### B. Reservoir-style recovery items

One of the most useful discoveries is the August 11, 2006 community cash-shop catalogue. It describes consumables that stored a large quantity of HP or Chi restoration instead of functioning as a single ordinary potion.

*Community-documented, August 2006*

| Recovery item description | Listed reservoir capacity |
| --- | --- |
| Combined Health + Chi | 1,000,000 |
| Health | 350,000 |
| Chi | 200,000 |
| Larger Health variant | 400,000 |
| Larger Chi variant | 400,000 |
| Pet Health + Chi | 350,000 |

The guide says these items did not use ordinary inventory weight or space and that they could be assigned to quickslots, with a minimum consumption of 200 recovery units.

These numbers are reservoir capacities, not necessarily HP instantly restored per click.

The guide is community-transcribed, and I couldn't verify the original retail prices or whether every variant was available at the same time.

### Why reservoir items mattered

A player could potentially avoid carrying numerous small potion stacks by relying on one substantial recovery source.

For your game, this suggests a valuable mobile-friendly concept: a reusable supply item with a finite charge reservoir.

However, I'd provide it through ordinary gameplay rather than make efficient HP/Chi recovery primarily a premium convenience.

### C. Temporary attribute buffs

The same August 2006 catalogue provides exceptionally clear examples.

| Buff | Reported effect | Duration |
| --- | --- | --- |
| Strength elixir | +15 STR | 48 connected hours |
| Dexterity elixir | +15 DEX | 48 connected hours |
| Intelligence liquor | +15 INT | 48 connected hours |

*Community-documented, 2006*

The important phrase is *connected hours*.

According to the descriptions, the countdown applied while connected to the game, rather than necessarily expiring 48 hours after activation.

The guide also says these effects could not be disabled during their active duration.

### D. Healing versus meditation

Meditation was available as an ordinary action and could restore HP, Chi and injury-related status, according to the official interface guide.

That gave players a non-item recovery method, though the precise recovery rate and interruption rules are unverified.

**Design implication:** Consumables should speed up or support combat recovery, while a free baseline recovery mechanic prevents the economy from turning every attempt at farming into a potion tax.

---

## 6. Revival items and death penalties

The original documentation distinguishes character death, experience loss and item-related losses, but the evidence is uneven.

### Confirmed death behavior

The Netgame interface guide says that reaching zero HP causes death, followed by a choice between reviving at the fallen location or returning to town.

A July 2006 community cash-shop guide describes a self-revival book with 20 uses, recovering 50% of lost XP.

An August 2006 catalogue also describes effects reducing death-related experience loss.

### Other resurrection consumables

The July 2006 community listing describes:

| Item function | Reported charges |
| --- | --- |
| Revive another player | 20 |
| Revive entire party | 10 |
| Revive self with partial XP recovery | 20 |

*Community-documented, 2006*

These descriptions do not establish complete level restrictions, revival distance or whether the item worked during every PvP encounter.

### Did players drop equipment when they died?

I could not verify a universal original Hero Online death-item-loss rule.

The historical sources reviewed establish XP loss and revival mechanics, but not a reliable formula for dropping backpack items or equipped gear on death.

This distinction matters:

- A sword destroyed by a failed blacksmith attempt is confirmed enhancement risk.
- An XP penalty from death is documented in historical descriptions.
- The automatic loss or theft of a sword upon death is not established by the evidence reviewed.

Do not import death-drop rules from games such as Lineage II or similarly named MMORPGs. They are separate games with different mechanics.

For your game, I'd keep PvE death separate from item ownership and avoid equipment loss on ordinary death.

---

## 7. Travel scrolls, utility items and special consumables

The July 2006 community catalogue documents scrolls capable of transporting characters to safe regions and later returning them to their original location.

### Historical travel-item examples

| Item | Reported effect | Listed uses |
| --- | --- | --- |
| General town portal scroll | Travel to supported safe regions and return | 40 |
| Dragon Castle portal | Travel to town and return | 35 |
| Reed Forest portal | Travel to that region and return | 35 |
| West Weald portal | Travel to that region and return | 35 |

*Community-documented, July 2006*

The later Netgame cash-item catalogue independently describes a teleportation scroll allowing travel to eligible safe zones and a return to the original location, with 40 uses. It also notes that some zones restrict teleportation.

*Official catalogue; publication date uncertain*

### What remains unverified

- Whether travel worked during combat.
- Cast time and interruption behavior.
- Whether consuming the initial trip also consumed the return use.
- Exact original NPC versus scroll travel costs.
- Whether quest areas disallowed returns.
- Whether all destination unlocks were account-wide.
- Whether PvP zones prohibited selected scrolls.

For your game, I'd make the rules clear: a travel item should tell the player its destination, return behavior, charges and any combat restrictions before use.

### Item-based automated looting

Another particularly interesting 2006 item taught a character to collect loot through an offensive companion. The pet—not a riding mount—could automatically deliver dropped items into inventory while the effect remained active.

That links items, pets and farming convenience.

**Design implication:** Automatic collection should be part of normal mobile usability where appropriate, not a premium restriction on basic interactions.

---

## 8. Stat resets, skill resets and special-use books

Our Classes and Martial Arts research covered the progression consequences, but the item implementation deserves its own analysis.

The August 2006 catalogue describes separate consumables for:

- Resetting every allocated attribute point.
- Refunding ten points from STR.
- Refunding ten points from DEX.
- Refunding ten points from INT.
- Resetting all martial skill allocations.
- Resetting individual skill points.
- Resetting first- and second-profession choices.

*Community-documented, August 2006*

This is a meaningful distinction: character build choices were stored as different kinds of progression state, and items could mutate specific parts of that state.

These weren't ordinary buffs with a duration. They were permanent account/character state changes activated by consuming an item.

That creates stricter implementation requirements:

1. Check eligibility.
2. Show the exact change.
3. Validate the character's current state.
4. Consume the item.
5. Apply the reset.
6. Save the new state exactly once.

If reset items can be traded, they also become valuable market commodities.

For your Unity implementation, such items should execute predefined, validated effects—not contain arbitrary script names or code.

---

## 9. Cosmetic equipment and premium gear

Hero Online also supported equipment that affected appearance separately from ordinary armor.

The official interface guide describes Expanded Equipment slots for a helmet, armor, boots, mask and House Costume. It also describes swapping which equipped appearance was shown.

The August 2006 catalogue shows special decorative equipment layered over normal items.

But these weren't necessarily cosmetic-only.

Some listed pieces granted additional HP, Chi recovery, resistances or defense.

### Historical examples

| Item type | Listed bonus |
| --- | --- |
| Decorative helmet | +30 HP, +20% HP recovery |
| Decorative mask | +150 Chi, +50% Chi recovery |
| Temporary outfit helmet | +60 HP, +10 paralysis resistance |
| Temporary outfit body armor | +75 HP, +15 confusion resistance |
| Temporary outfit boots | +40 HP, +2% normal defense |

Some decorative items in the catalogue lasted 168 hours, while complete special outfits were advertised for three weeks.

*Community-documented, August 2006*

This meant premium-looking clothing could also increase combat effectiveness.

For your game, I'd separate:

- **Equipment:** determines combat statistics.
- **Appearance:** determines visual presentation.

Combining combat buffs with purchased decorative outfits makes it harder to keep PvP and equipment progression fair.

---

## 10. Item binding, storage and trading permissions

Later Hero Plus documentation shows a more detailed system than a simple tradable/non-tradable flag.

In May 2026, Netgame updated item tooltips to explicitly identify restrictions.

| Tooltip field | Meaning |
| --- | --- |
| Trade X | Cannot trade directly with another player |
| Store X | Cannot put in supported storage |
| Consign X | Cannot list through consignment |
| Soul X | Cannot transfer through Soul Transfer |

*Official, May 2026*

An October 2026 Halloween event provides an interesting example: its temporary reward items were non-tradable, could be stored, could not be consigned and could not be transferred through Soul Transfer. They were also scheduled for deletion at a later maintenance.

### Why a single isBound flag is insufficient

An item may be:

- Non-tradable but storable.
- Tradable directly but barred from consignment.
- Transferable between characters but not through a special transfer mechanism.
- Time-limited but otherwise usable.
- Eligible for dismantling but not ordinary sale.

For your game, these should be clear capabilities rather than an accumulation of exceptions hidden in individual UI screens.

### Quest items

The older Netgame glossary says quest items cannot normally be traded or discarded, although a tavern clerk can help remove obsolete quest objects.

That's a special lifecycle rule, distinct from a marketplace restriction.

I would track purely narrative quest evidence in quest state rather than filling the backpack with objects the player can neither use nor discard.

### Expiring event items

Hero Plus maintenance notices repeatedly list event items scheduled for automatic deletion. The August 2026 notice even included a correction to expiration information displayed in item tooltips.

This is evidence that lifecycle communication became important enough to patch.

For your game, distinguish between a temporary active effect, a temporarily usable item and an item that actually disappears. They should never share an ambiguous countdown.

---

## 11. Later Hero Plus: 2026 gold limits and quantity management

The detailed May 19, 2026 notice documents:

| Mechanic | Official limit |
| --- | --- |
| Inventory gold | 9,999 billion |
| Storage gold | 9,999 billion |
| Direct trade gold | 50 billion |
| Gold transferred through mail | 50 billion |
| Personal-shop trade | 50 billion |
| Maximum consignment price | 990 billion |
| General eligible-item stack maximum | 9,999 |

A shorter maintenance summary describes the inventory gold cap differently, at 999.9 billion. This conflict should be preserved, not concealed. The detailed announcement above is more explicit.

### What it tells us

These caps don't prove actual historical item prices or gold inflation rates.

They do show that very large numerical quantities affected usability, prompting Netgame to add billion-unit input controls and clearer currency displays.

**Design inference:** Currency denomination, stack limits and search/transfer interfaces need to be designed together.

For your mobile game, I would not adopt enormous balances from Hero Plus. Start with an economy whose everyday prices fit comfortably on a phone screen.

---

## 12. Item economy and monetization

The strongest historical sources show several item-related economic mechanisms.

| Activity | Effect on economy |
| --- | --- |
| Monster drops | Creates consumables, equipment and materials |
| NPC potion purchase | Removes gold |
| Healing potion use | Removes item supply |
| Paid storage expansion | Adds convenience, not necessarily in-game gold supply |
| Premium stat buff | Gives temporary character power |
| Paid respec | Changes character progression choices |
| Teleport consumable | Reduces travel cost in time |
| Fortune Box opening | Removes gold, creates a randomized item |
| Enhancement | Consumes stones, gold and potentially equipment |
| Player trading | Redistributes existing gold and items |
| Storage | Preserves item value across characters |
| Timed event deletion | Removes previously created item supply |

The sources document the underlying item mechanics; the economic classifications are analytical.

### Premium convenience versus competitive advantages

Already in August 2006, community documentation described premium-style advantages such as:

- Efficient recovery reservoirs.
- Temporary primary-stat boosts.
- Enhancement-success modifiers.
- Extra storage space.
- Pet auto-looting.
- Experience and gold acquisition bonuses.
- Reduced XP losses on death.

These affect gameplay differently.

Storage expansion makes inventory management easier. But enhancement modifiers and bonus attributes can change combat performance and wealth accumulation.

For your game, I'd separate quality-of-life features from power progression, particularly if you eventually have competitive PvP and tradeable rare equipment.

### Community reactions

A June 2006 player preview explicitly recommended preserving low-level loot in shared storage because equipment was expensive and could be useful to another character.

A July 2006 launch reviewer observed that abundant ordinary equipment often went directly to NPC merchants or the account bank, while enhancement gems, pets and Fortune Boxes had greater apparent interest in personal stalls.

Those are firsthand observations, not statistical market data.

The April–May 2026 developer notices cite player feedback as motivation for increased stack quantities, improved item interfaces and gold-limit adjustments.

This supports the conclusion that item management convenience remained important years later.

I could not recover reliable original-era survey data measuring satisfaction with weight limits, bank capacity, consumable prices or premium inventory items.

---

## 13. Version comparison

| System | 2006 evidence | Later Hero Plus | Global/private-server status |
| --- | --- | --- | --- |
| Shared account bank | 120 slots reported | Storage still supported | Separate rules unverified |
| Carry weight | Original-era attribute associations; older official guide | Weight shown in interface | Actual formulas unavailable |
| Potion stacks | General stacking described | Maximum 9,999 for eligible items in 2026 | Caps unverified |
| Reservoir recovery items | Community catalogue, 2006 | Multiple advanced recovery products | Parity unverified |
| Attribute buff items | +15 temporary stats reported in 2006 | Many further temporary buffs | Parity unverified |
| Teleport/return scrolls | Community documented, 2006 | Official item catalogue confirms functionality | Destination lists may differ |
| Revival items | Community documented, 2006 | Later protection/revival variants | Current death rules differ by mode |
| Storage expansion | 30-day premium item reported | Timed expansions still listed in 2026 | No separate verified tables |
| Item binding | Quest items restricted; other rules unclear | Separate trade, storage, consignment and Soul restrictions | Unverified |
| Event-item expiry | No universal 2006 rule recovered | Explicit timed deletions in 2026 | Unverified |
| Cosmetic stat bonuses | Community documented, 2006 | Expanded cosmetic/gear systems | Unverified |

### Hero Online Global and The Hero Global

I did not recover authenticated, release-specific inventories or complete item tables for either named version.

It would be incorrect to transfer Netgame's May 2026 stack limits, prices or binding flags to those versions without verification.

### Private servers

I couldn't substantiate a complete private-server item database with a reliably identified client build and independently checked item effects.

Even where private-server packages advertise full item tables, their content may be altered.

A private server's 200-slot backpack or modified potion limit, for example, would not demonstrate the original 2006 setting unless tied to verifiable original data.

### Important unresolved historical details

- Original personal backpack capacity.
- Exact weight formula and overweight consequences.
- Full original equipment-slot count.
- NPC potion prices across levels.
- Potion effect sizes and cooldowns.
- Whether potion use had a global cooldown.
- Bank deposit and withdrawal charges.
- Exact death penalties by zone.
- Whether equipment or inventory objects could drop on death.
- Full original cash-shop price table.
- Original item-binding matrix.
- Maximum carried stacks for each 2006 item category.
- Whether unclaimed loot expired after a fixed interval.
- Exact original auto-loot radius.
- Item duplication/exploit protections.
- Inventory and storage rules on every regional edition.

I would not fill these gaps with values borrowed from unrelated MMORPGs.

---

## 14. Implications for our Unity mobile MMO-lite

I would make one shared item model with different item behaviors, rather than separately designing the inventory for each gameplay system.

That is especially important because we already have a relatively sophisticated blacksmith specification and expect boss gear to contain rolled attributes and enhancement history.

### What I'd keep, change or avoid

| Historical feature | Recommendation | Why |
| --- | --- | --- |
| Shared account storage | Keep eventually | Useful across characters |
| Clear item categories | Keep | Essential for small screens |
| Consumable stacking | Keep | Reduces clutter |
| Carry-weight system | Avoid initially | Duplicates slot-capacity restrictions |
| Quick-access potions | Keep | Combat usability |
| Free meditation/recovery | Keep concept | Reduces mandatory potion spending |
| Health/Chi reservoirs | Consider later | Convenient for longer adventures |
| Internal Injury consumables | Only if condition remains | Must connect to actual combat |
| Teleport and return items | Keep later | Supports world exploration |
| Inventory expansion | Earnable first | Avoid unnecessary monetization pressure |
| Account bank | Keep | Supports alternate characters |
| Cosmetic overlays | Keep | Visual personalization |
| Paid cosmetic stat bonuses | Avoid | Complicates competitive fairness |
| Quest items taking permanent bag space | Avoid | Inventory clutter |
| Arbitrary timed item deletion | Avoid | Poor ownership experience |
| Death-related equipment loss | Avoid | Adds risk beyond the blacksmith |
| Explicit item permissions | Keep | Needed for secure trading |
| 9,999 material stacks | Not necessary initially | Should depend on actual resource volumes |

---

## 15. Proposed inventory v0.1

These are new design starting points, not original Hero Online values.

| Parameter | Proposal |
| --- | --- |
| Backpack capacity | 40 slots |
| Account storage | 80 slots when needed |
| Weight | None |
| Ordinary material stack | 999 |
| Recovery consumable stack | 99 |
| Unique equipment | One item per slot |
| Quest-only evidence | Stored in quest journal |
| Quickslots | 4 configurable combat shortcuts |
| Auto-loot | Baseline convenience |
| Pickup overflow | Secure claim queue for important rewards |
| Equipment drops | Persistent individual instances |
| Sorting | By type, rarity and level |
| Filters | Equipment, Consumables, Materials, Books, Special |
| Transfer restrictions | Explicit capabilities |
| Gold | Integer balance stored separately |

I would not implement account storage immediately if the prototype only supports one character.

### Example mobile inventory organization

*(The original was an illustrative interface mock-up, not connected to Unity. Its contents:)*

**Inventory — 8 / 40 slots — All Items**

| Item | Quantity |
| --- | --- |
| Highland Spear +3 | |
| Bronze Helmet | |
| Health Potion | ×24 |
| Chi Potion | ×18 |
| Iron Ore | ×67 |
| Rough Ruby Stone | ×9 |
| Mountain Spear Book | |
| Return Scroll | ×3 |
| Gold | 12,450 |

The design priority isn't making the backpack attractive. It's ensuring players can locate, understand and use items during short sessions.

---

## 16. One item foundation for every system

1. **Item definitions** — what the item is allowed to do.
2. **Owned items and quantities** — who owns them, where they are and their instance data.
3. Used by **Combat**, **Blacksmith**, **Crafting**, **Marketplace**, **Quests** and **Pets**.

### Minimum item data

An item definition should describe:

- Item ID, name, description, icon and category.
- Stackability and maximum stack.
- Whether the item is usable, wearable, sellable, storable or transferable.
- Relevant equipment restrictions.
- Possible use effects.
- NPC value where applicable.
- Expiration policy, if any.

A unique equipment instance should additionally have its own identity, rolled properties, rarity and complete ordered enhancement history.

A stack of ordinary potions generally doesn't need a unique identity for each bottle.

### Item ownership states

These need careful separation:

| State | Meaning |
| --- | --- |
| Backpack | Character can directly use the item |
| Equipped | Character is wearing/wielding it |
| Account storage | Accessible through the account bank |
| Guild storage | Belongs to guild treasury rules |
| Marketplace escrow | Temporarily held for a listing |
| Mail/claim storage | Awaiting delivery or retrieval |
| Consumed/destroyed | Item no longer exists |

One unique sword must never exist in two states simultaneously.

That protects the blacksmith, the trading economy and character persistence from duplication errors.

---

## 17. Two important cross-system discoveries

### A. Fortune Draws could fail because the inventory was full

The official Netgame Lot System guide says the character needed more than five empty inventory slots before using a draw.

It also documents counter resets after character death, disconnects, server switching, an hour without kills and reaching 10,000 kills.

*Official guide, undated*

This adds detail missing from our earlier Events research.

It also reinforces the value of inventory design: a reward mechanic can become frustrating when the player needs to reorganize the backpack before claiming it.

For your game, unclaimed Fortune Draws should persist, and a full bag should send rewards to secure temporary claim storage.

### B. Inventory friction was part of the game's monetization

Already in 2006, players were offered storage expansions and recovery items that avoided normal inventory-space or weight restrictions.

That means convenience was not merely a later Hero Plus development.

But the fact that it existed in the original game doesn't mean we need to reproduce it.

For your game, inventory capacity should create meaningful choices about loot, not pressure players into paying to avoid routine inconvenience.

---

## 18. Unity implementation recommendations

I'd ask Claude Code to inspect your current project in assessment-only mode before implementing any inventory architecture.

Specifically, it should determine:

- Whether item definitions already use ScriptableObjects or another data format.
- How inventory quantities and unique items are serialized.
- Where equipment stats are calculated.
- Whether blacksmith enhancement history is already attached to persistent item instances.
- How consumables modify player attributes and resources.
- Whether loot, quests and NPC shops already use a shared inventory API.
- How the game handles full inventory, saving and loading.
- Whether item ownership can eventually support an authoritative multiplayer backend.

The first vertical slice should cover one stackable potion, one unique sword, one material stack and one quest reward, all using the same inventory and save system.

Then test splitting/merging stacks, equipping, consuming, upgrading, storing and reclaiming items without duplication or data loss.

I wouldn't build marketplace escrow, account-bank sharing or guild storage before those fundamental behaviors work.

---

## 19. Key research sources

| Source | Evidence |
| --- | --- |
| June 2006 firsthand preview | 120 shared bank slots, gender-dependent equipment and early inventory advice |
| June 2006 preview summary | Account bank, shared gold/equipment, three characters |
| Official Netgame interface guide | Weight, quickslots, equipment, pet food and recovery |
| Official item terminology | Quest-item restrictions and general progression concepts |
| August 2006 item catalogue | Potion reservoirs, stat buffs, cosmetics, resets and enhancement charms |
| July 2006 cash-item catalogue | Revival, teleportation, auto-loot and storage expansion |
| Official Netgame cash-item catalogue | Teleport scrolls, transfer items and enhancement modifiers |
| Official Lot System guide | Empty-slot rule and kill-counter resets |
| 19 May 2026 gold and item overhaul | Stack limits, currency limits and transfer flags |
| July 2026 stack changes | Specific items newly allowed to stack |
| August 2026 maintenance | Item expiry communication and stack changes |
| July 2026 consumable/reward catalogue | Timed bag expansions, combat buffs and utility items |
| Official October 2026 event notices | Contemporary item restrictions, temporary rewards and consumables |

---

## Final assessment

Items and inventory are the connective tissue of the entire game. They're not merely another feature to research alongside combat, pets or crafting.

The original Hero Online combined weight-limited inventory, a generous shared bank, stackable recovery items and a surprisingly broad selection of special-purpose consumables. Later Hero Plus added more complex item-transfer permissions, huge stack limits and numerous timed products.

For your Illyrian/Dardanian game, I'd preserve the satisfaction of discovering, collecting, improving and trading valuable objects while simplifying the inventory management around them.

The architecture decisions I'd prioritize are persistent item identity, legal ownership transfers, clear item permissions, safe consumption and a single inventory interface shared by every progression system.

One final recommendation: now that we've completed the original 12 research categories plus this 13th items-and-inventory study, the next task shouldn't be to implement all thirteen systems. It should be to reconcile them into a single dependency map, define the character and item foundations, and identify the smallest coherent playable vertical slice of your Unity game.
