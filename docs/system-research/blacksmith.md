# Blacksmith System — Design Specification v0.1

- **Project:** Original Mobile Wuxia MMO-lite
- **Engine:** Unity
- **Status:** Design draft for review and implementation planning
- **Inspiration:** Classic Korean MMORPG equipment enhancement systems, particularly Hero Online. All names, visuals, formulas and implementations must be original.

---

## 1. System Overview

The Blacksmith is a progression system that allows players to improve equipment, customize equipment attributes, recover resources and manage enhancement risk.

Its purpose is to create a rewarding long-term equipment progression loop with meaningful resource consumption and decisions.

The system consists of four core actions:

1. **Enhance** — Upgrade equipment from +0 to +8.
2. **Dismantle** — Break equipment into crafting materials and potentially recover enhancement stones.
3. **Extract** — Sacrifice one enhancement level for a chance to recover a stone.
4. **Combine** — Convert lower-tier stones into higher-tier stones.

A fifth system, **Enhancement Transfer**, is reserved for a later development phase.

### Core Design Principles

- Early enhancements should be approachable.
- Higher enhancement levels should carry meaningful risks.
- Equipment and stones should both retain economic value.
- Players should understand exactly what they risk before committing.
- The blacksmith must not be a mandatory cash-shop mechanic.
- Equipment customization should support different combat builds.
- All random outcomes must use a controlled and testable probability system.
- The implementation must support future balancing through configurable data.
- The first implementation should be small, modular and compatible with the existing Unity project.

---

## 2. Equipment Enhancement

### 2.1 Enhancement Levels

All eligible equipment begins at +0.

The maximum enhancement level is +8.

Each successful enhancement increases the equipment level by one and adds a stat bonus determined by the enhancement stone used.

Example:

- Warrior's Blade +0
- Warrior's Blade +1
- Warrior's Blade +2
- …
- Warrior's Blade +8

Enhancement cannot exceed +8.

### 2.2 Eligible Equipment

The system should initially support combat equipment already present in the project.

Possible categories include:

- Weapons
- Armor
- Helmets
- Boots
- Accessories

**Implementation instruction:** Inspect the current equipment architecture before determining which slots should initially support enhancement. Do not introduce new equipment categories unnecessarily.

Equipment requires a unique instance identifier so that two copies of the same base item can have different enhancement histories.

### 2.3 Enhancement Stone Slots

Each enhancement attempt accepts:

- One equipment item.
- One to three enhancement stones.
- Optional Warding Charm.
- Gold payment.

All stones in one attempt must have the same type and tier.

The stone type determines which attribute is enhanced.

The number of stones determines success probability.

Using more stones must not multiply the stat gained from that enhancement level.

### 2.4 Enhancement Stone Types

| Stone | Attribute |
| --- | --- |
| Ruby | Damage |
| Jade | Defense |
| Amber | Accuracy |
| Onyx | Dodge |
| Sapphire | Skill Damage |

Stone types should be independently configurable.

Every eligible item category must have a defined compatibility mapping. For example, damage stones should not automatically enhance every armor piece without explicit game-design approval.

A compatibility system should determine which stone types can be used on each item.

### 2.5 Stone Tiers

There are two initial stone tiers:

**Rough**

- Obtained primarily through normal gameplay and drops.
- Can be used for enhancement.
- Three Rough stones of the same type can be combined into one Polished stone.

**Polished**

- Created through combining or earned from appropriately difficult content.
- Provides a stronger stat enhancement than Rough.
- Uses the same general success-rate table initially.

**Initial tier modifier**

- Rough: 100% of the listed stat bonus.
- Polished: 150% of the listed stat bonus.

The modifier applies to the stat bonus gained from the successful level, not the item's entire existing enhancement bonus.

The success rate depends on stone count, not tier, in v0.1.

This prevents Polished stones from simultaneously increasing both success probability and stat gain.

**Balancing note:** The exact 150% modifier is provisional and should be tested.

---

## 3. Enhancement Stat Scaling

Use an increasing, non-compounding enhancement curve.

Every successfully enhanced level contributes its own bonus to one chosen attribute.

### Starting Stat Curve

| Level achieved | Bonus added |
| --- | --- |
| +1 | 2% |
| +2 | 2% |
| +3 | 3% |
| +4 | 3% |
| +5 | 4% |
| +6 | 4% |
| +7 | 5% |
| +8 | 7% |

The bonus is calculated against an appropriate unenhanced item stat, not against the already enhanced total.

When all eight enhancements use Rough stones targeting the same stat, the cumulative contribution is 30% of the relevant base stat.

### Example

A sword has 100 base damage.

The player successfully enhances it to +3 using Rough Ruby stones at every level.

- +1: +2 damage.
- +2: +2 damage.
- +3: +3 damage.

Total damage becomes 107, excluding other modifiers.

If the +3 enhancement instead uses Polished Ruby, its level bonus becomes 4.5 damage before the game's stat-rounding rules.

### Mixed Enhancements

Each enhancement level must remember:

- The level achieved.
- Stone type used.
- Stone tier used.
- Attribute affected.
- Resulting bonus contribution.

Example enhancement history:

| Level | Stone | Bonus |
| --- | --- | --- |
| +1 | Rough Ruby | Damage +2% |
| +2 | Rough Ruby | Damage +2% |
| +3 | Polished Sapphire | Skill Damage +4.5% |
| +4 | Rough Amber | Accuracy enhancement |
| +5 | Polished Ruby | Damage +6% |

Stats must be recalculated from enhancement history rather than permanently modifying base item values.

**Important:** Accuracy and Dodge must integrate with the existing combat-stat formulas. Do not interpret the percentages as unrestricted increases in final hit/dodge probability. If a compatible rating formula does not exist, flag it before implementation.

---

## 4. Enhancement Success Rates

The probability of success depends on:

1. Target enhancement level.
2. Number of stones used.

### Initial Probability Table

| Target level | 1 stone | 2 stones | 3 stones |
| --- | --- | --- | --- |
| +1 | 100% | 100% | 100% |
| +2 | 95% | 97% | 99% |
| +3 | 88% | 91% | 94% |
| +4 | 78% | 82% | 86% |
| +5 | 65% | 70% | 75% |
| +6 | 50% | 56% | 62% |
| +7 | 32% | 39% | 46% |
| +8 | 15% | 20% | 24% |

These are starting design values, not historical Hero Online odds.

All probability values must be configurable.

One roll determines whether an enhancement succeeds.

If the attempt succeeds, increase the enhancement level by one.

If it fails, apply the failure rules below.

---

## 5. Balanced Failure System

The chosen failure model has three risk brackets.

### Bracket A: Early Enhancement

Target levels +1 through +3

- On success: item gains one level.
- On failure: item retains its current level.
- Item is never destroyed.
- Stones are consumed.
- Gold is consumed.

For +1, the starting success chance is 100%, so normal attempts cannot fail.

### Bracket B: Intermediate Enhancement

Target levels +4 and +5

- On success: item gains one level.
- On failure: item loses one enhancement level.
- Item is not destroyed.
- Stones are consumed.
- Gold is consumed.

Example:

A +4 weapon attempts to become +5.

If enhancement fails, it becomes +3.

The enhancement record corresponding to the lost level is removed.

### Bracket C: High-Risk Enhancement

Target levels +6 through +8

- On success: item gains one level.
- On unprotected failure: equipment is destroyed.
- Stones are consumed.
- Gold is consumed.

Example:

A +6 weapon attempts to become +7.

If the attempt fails without protection, the entire weapon is destroyed, including its enhancement history.

The item must be removed safely from equipment, inventory and persistence systems.

Equipment destruction must never result in duplicated, orphaned or partially saved items.

---

## 6. Warding Charm

The Warding Charm is a consumable protection item.

### Behavior

When a charm is applied to an eligible enhancement attempt:

**Success**

- Equipment gains one enhancement level.
- Enhancement stones are consumed.
- Gold is consumed.
- Warding Charm is not consumed.

**Failure**

- Equipment survives.
- Equipment loses one enhancement level.
- Enhancement stones are consumed.
- Gold is consumed.
- Warding Charm is consumed.

Example:

A +7 weapon attempts to become +8 using a Warding Charm.

If it fails:

- Weapon becomes +6.
- Weapon survives.
- Warding Charm disappears.

The last enhancement record is removed.

### Eligibility

The Warding Charm can initially be used for target levels +6, +7 and +8.

It is unnecessary in the early safe brackets and should not be offered there.

### Acquisition

Possible sources:

- Limited progression rewards.
- Weekly gameplay objectives.
- Difficult PvE encounters.
- Limited event rewards.
- Future player trading if the economy supports it.

Initial economic test target:

- One introductory charm from progression.
- Approximately two to four additional charms weekly for an active player.

Supply must be evaluated against the number of high-risk enhancement attempts, not only calendar time.

Do not implement a premium-shop-only acquisition path.

Do not introduce stronger protection items in v0.1.

---

## 7. Gold Costs

Enhancement consumes gold on every attempt, regardless of success or failure.

### Initial Gold Fee Table

| Target level | Gold fee |
| --- | --- |
| +1 | 50 |
| +2 | 100 |
| +3 | 200 |
| +4 | 400 |
| +5 | 800 |
| +6 | 1,500 |
| +7 | 3,000 |
| +8 | 5,000 |

Fees do not initially scale with item rarity or base item level.

This should remain configurable for future economic balancing.

Gold and enhancement materials must be checked before the attempt is initiated.

All resources must be deducted as part of the same successful state transaction that resolves the attempt.

---

## 8. Dismantle

Dismantling destroys unwanted equipment and returns crafting resources.

### Inputs

- One eligible equipment item.
- Gold fee.

### Outputs

- Guaranteed base crafting materials.
- Additional crafting materials based on item rarity.
- Chance of receiving an enhancement stone.

### Initial Rules

Dismantling succeeds 100% of the time.

Equipment is permanently destroyed.

The player receives the calculated rewards.

Suggested initial fees:

- Common equipment: 50 gold.
- Rare equipment: 200 gold.

Other rarity fees should be configurable.

### Stone Recovery

Initial proposed probabilities:

| Item rarity | Chance to receive a Rough stone |
| --- | --- |
| Common | 5% |
| Uncommon | 10% |
| Rare | 20% |
| Epic | 35% |
| Legendary | 50% |

These probabilities are placeholders and must be validated against actual drop rates.

The stone type should be determined by a configurable reward table.

The presence of previous enhancements should not automatically duplicate all invested stones.

If enhanced equipment is dismantled, its enhancement history is permanently destroyed unless explicitly accounted for by the dismantling reward configuration.

Equipment currently worn by the player must require an additional confirmation before dismantling.

---

## 9. Extract

Extraction sacrifices enhancement progress for a chance to recover a stone.

### Requirements

- Equipment must be +1 or higher.
- Player must pay gold.
- The item must be eligible for extraction.

### Behavior

An extraction attempt removes exactly one enhancement level.

This occurs regardless of whether a stone is recovered.

The removed enhancement record determines the stone type eligible for recovery.

Example:

An item contains the following history:

- +1 Ruby
- +2 Jade
- +3 Sapphire
- +4 Ruby

Extraction from +4 removes the Ruby enhancement at level +4.

The item becomes +3.

It has a chance to return one Ruby stone.

It cannot randomly return an unrelated stone type.

### Initial Recovery Odds

- 50% chance to recover one stone.
- 50% chance to recover nothing.

The recovered stone retains its original type and tier.

Extraction never destroys the equipment.

### Extraction Fees

Initial rule:

Extraction costs 30% of the enhancement fee associated with the level being removed, rounded to a whole gold amount.

Example:

Removing +5 costs 30% of the +5 enhancement fee.

800 × 30% = 240 gold.

No Warding Charm can be applied to extraction.

---

## 10. Combine

Combination converts Rough stones into Polished stones.

### Inputs

- Three Rough stones.
- All three must have the same type.
- 150 gold.

### Output

- One Polished stone of the matching type.

Examples:

- 3 Rough Ruby → 1 Polished Ruby.
- 3 Rough Jade → 1 Polished Jade.

### Rules

- Combination success rate: 100%.
- No destruction risk beyond consuming the required input stones.
- Warding Charm cannot be used.
- Polished stones cannot be combined further in v0.1.
- Mixed stone types cannot be combined.

This is intentionally simple to avoid introducing another random failure system alongside equipment enhancement.

---

## 11. Equipment Appearance and Visual Effects

Enhancement level affects equipment presentation.

### Visual Milestones

| Enhancement | Visual treatment |
| --- | --- |
| +0 to +1 | Normal appearance |
| +2 | Subtle metallic shimmer |
| +3 to +4 | Silver glow |
| +5 to +6 | Gold glow |
| +7 to +8 | Flame-like aura |

At +8, the flame aura may receive a distinctive additional visual detail.

Visual effects should:

- Be recognizable at mobile screen sizes.
- Avoid excessive particles.
- Preserve the underlying equipment silhouette.
- Be configurable by equipment type.
- Support reduced-effects settings for performance.
- Avoid obscuring combat animations.

The equipment name and enhancement level should also be visible in the inventory and character equipment panel.

Enhancement visuals should use original game assets and a consistent art direction.

---

## 12. Blacksmith User Interface

The Blacksmith should have four primary sections:

**Enhance | Dismantle | Extract | Combine**

### Enhance Screen

Display:

1. Selected equipment.
2. Current enhancement level.
3. Current equipment stats.
4. Preview of stats after successful enhancement.
5. Stone type selection.
6. Stone tier selection.
7. Number of stones (1–3).
8. Exact success probability.
9. Gold cost.
10. Failure consequence.
11. Optional Warding Charm for high-risk levels.
12. Confirm enhancement button.

The player must never have to guess what a failure will do.

### Failure Warnings

For safe attempts, display:

> "Failure consumes stones and gold. Equipment stays at its current level."

For intermediate attempts:

> "Failure removes one enhancement level."

For high-risk unprotected attempts:

> "Failure permanently destroys this equipment."

For protected attempts:

> "Failure consumes your Warding Charm and removes one enhancement level. Equipment survives."

These are functional examples, not final game copy.

### Confirmation Rules

- Safe upgrades: direct confirmation.
- Intermediate upgrades: standard confirmation.
- Destructive upgrades: explicit warning and confirmation.
- Dismantling valuable or equipped items: explicit confirmation.

Do not use misleading progress bars or invented probability animations.

The visual animation must reflect a result already determined by the gameplay logic.

### Result Screen

On success, show:

- Previous level.
- New level.
- Attribute gained.
- Updated relevant stats.

On failure, show:

- Failure outcome.
- Stones consumed.
- Gold consumed.
- Protection used, if applicable.
- Updated enhancement level or destroyed equipment state.

### Upgrade History

Players should be able to inspect the enhancement contributions of an item.

Example:

Warrior's Blade +5

- +1 Rough Ruby
- +2 Rough Ruby
- +3 Polished Sapphire
- +4 Rough Jade
- +5 Polished Ruby

The interface should also show aggregated enhancement bonuses.

---

## 13. Economy and Trading

The blacksmith creates multiple resource sinks.

### Gold Sinks

- Enhancement fees.
- Combining fees.
- Extraction fees.
- Dismantling fees.

### Material Sinks

- Enhancement stone consumption.
- Equipment destruction.
- Combining three stones into one.
- Failed extraction recovery.

### Equipment Sinks

- Destructive high-level enhancement failures.
- Dismantling.

Initial tradeability is a design decision that must be reconciled with the existing economy.

If the game supports trading:

- Ordinary Rough stones should generally be tradeable.
- Polished stones may be tradeable.
- Enhanced equipment may be tradeable where appropriate.
- Warding Charm trading should be tested carefully.
- Quest-granted protection items may need account binding.

Do not implement trading solely to support the blacksmith if the current game does not already have it.

The system should avoid creating unlimited gold or materials through repeated extraction, combining and dismantling.

---

## 14. Technical Architecture Requirements

Claude Code: inspect the existing Unity project before designing the implementation.

Determine how the following are already handled:

- Item definitions and item instances.
- Inventory quantities and stacking.
- Equipment slots.
- Character stats and combat calculations.
- Currency.
- Saving/loading.
- Network authority, if multiplayer functionality already exists.
- Random-number generation.
- UI screens and navigation.

Reuse existing services whenever possible.

Do not create a second inventory, currency or stat framework.

### Suggested Data Responsibilities

**Blacksmith Configuration**

Contains:

- Maximum enhancement level.
- Probability table.
- Fee table.
- Stone definitions.
- Item compatibility rules.
- Enhancement bonus curve.
- Visual milestone definitions.
- Extraction probabilities.
- Dismantling reward tables.
- Combination recipes.

Use Unity ScriptableObjects for static designer-authored configuration if compatible with the existing project.

**Equipment Enhancement State**

Persist per equipment instance:

- Current enhancement level.
- Ordered enhancement history.
- Stone types and tiers used.
- Any necessary version identifier for save migrations.

Enhancement level should always agree with the number of active enhancement records.

**Blacksmith Logic**

Centralize:

- Validation.
- Cost calculation.
- Success probability.
- Random outcome resolution.
- Resource consumption.
- Failure consequences.
- Result generation.

Keep the logic independent of animations and UI.

### Persistence and Multiplayer

Random outcomes and resource consumption must be authoritative.

For an actual multiplayer environment, enhancement rolls and equipment destruction should be resolved by a trusted server, not a client-controlled Unity random function.

Changes must be atomic and protected against duplicate or repeated requests.

A request must not be capable of granting rewards twice or consuming the same equipment multiple times.

For a local prototype, implement clean service boundaries that can later move to server authority without rewriting the blacksmith rules.

---

## 15. Edge Cases

The implementation must explicitly handle:

- Attempting to enhance +8 equipment.
- Missing equipment.
- Insufficient gold.
- Insufficient stones.
- Mixed stone types.
- Mixed stone tiers.
- Incompatible stones.
- Invalid enhancement history.
- Repeated button clicks.
- Inventory changes during a confirmation screen.
- Disconnects or crashes during an attempt.
- Item destruction while equipped.
- Stat recalculation after losing a level.
- Stat recalculation after destroying an item.
- Extraction of +1 to +0.
- Combining with insufficient inventory space.
- Awarding extraction or dismantling rewards with a full inventory.
- Loading old save files without enhancement data.
- Reusing an already processed enhancement request.
- Stale UI previews after equipment or inventory changes.

---

## 16. Testing and Balancing

Before finalizing gameplay values, the blacksmith must support automated simulations.

Test scenarios should include:

- 10,000 simulated players.
- 30 simulated days.
- Different stone consumption strategies.
- Players using one, two or three stones.
- Players who use protection.
- Players who never use protection.
- Casual and dedicated progression patterns.
- Multiple equipment slots.
- Scarce and abundant stone supply.
- Rare equipment with high replacement costs.

Measure:

- Percentage reaching each enhancement level.
- Average time to +8.
- Total enhancement attempts.
- Equipment destroyed.
- Charms consumed.
- Stones consumed.
- Gold removed.
- Average gold cost of achieving +8.
- Material demand by stone type and tier.
- Distribution of losses, not only averages.

The existing Balanced versus Hardcore simulation should be used as a reference point, not treated as production-validated player behavior.

---

## 17. Future Features — Not in v0.1

Do not implement these yet:

- Enhancement transfer between items.
- Socket creation.
- Random item options.
- More than two stone tiers.
- Additional protection-item categories.
- Paid success-rate boosters.
- Enhancement pity systems.
- Server-wide upgrade announcements.
- Guild-based forging bonuses.
- Special enhancement events.
- Player-to-player material auctions.

These can be considered after the core blacksmith has been tested.

---

## 18. Implementation Scope and Workflow

Claude Code should not immediately implement the entire specification.

First, inspect the current project and report:

1. Which existing systems the blacksmith can integrate with.
2. Which required capabilities are missing.
3. Conflicts between this specification and existing game architecture.
4. Any unclear stat formulas or equipment compatibility rules.
5. The smallest playable implementation that can be completed without unnecessary architecture.
6. A phased implementation plan with testing criteria.

Recommended sequence:

**Phase 1 — Enhancement core**

- Stone definitions.
- Equipment enhancement history.
- Success-rate calculation.
- Balanced failure model.
- Gold and stone consumption.
- Warding Charm.
- Automated tests.

**Phase 2 — Enhancement interface**

- Equipment selection.
- Stone configuration.
- Success preview.
- Risk warnings.
- Enhancement results.
- Basic visual feedback.

**Phase 3 — Resource processing**

- Dismantle.
- Extract.
- Combine.
- Inventory integration.

**Phase 4 — Polish and validation**

- Equipment glow effects.
- Sound and animation.
- Persistence and multiplayer validation.
- Economy simulations.
- Performance and usability testing.

### Definition of Done

The initial blacksmith is complete when a player can:

- Select eligible equipment.
- See its current enhancement level and enhancement history.
- Choose an eligible stone type, tier and quantity.
- Preview the success chance, stat gain and failure consequence.
- Spend the required stones and gold.
- Resolve enhancement with the correct probability.
- Experience the appropriate Balanced failure consequence.
- Use a Warding Charm on high-risk attempts.
- Dismantle eligible equipment.
- Extract the last enhancement for a chance at material recovery.
- Combine three matching Rough stones.
- Save, load and reconnect without losing or duplicating equipment state.
- See equipment stats update correctly after every operation.

**Final instruction to Claude Code:** Treat this document as the proposed game design, not as permission to replace existing systems. Inspect the Unity project first, identify ambiguities and integration risks, and present your recommended implementation plan before writing production code.
