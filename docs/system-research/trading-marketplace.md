# Hero Online: Trading & Marketplace System

Historical research report · 2006 Hero Online, Hero Plus, The Hero Global and private-server documentation · 24 October 2026

> The source links and the interactive figures (the material economy in §4, the Merchant Quarter in §12, the fee calculator in §14 and the blacksmith flow in §15) didn't come through when this was pasted in; their text is kept below.

## Executive summary

Hero Online had something modern mobile MMORPGs often struggle to reproduce: trading was both an economic activity and a visible part of the game world.

Players could turn their characters into merchants, advertise items with custom shop banners, sell equipment and enhancement stones, and open shops that purchased materials from other players.

The market wasn't just a screen showing item prices. It was a physical destination where players congregated.

However, the trading system also evolved. By September 2015, Hero Plus had three documented trading methods: direct trading, personal stalls and offline consignment. In May 2026, Netgame substantially increased transaction limits and revised its trading interface.

### Six major findings

- **Personal shops were available from level 10 in 2006.** Players could set prices, advertise items and stand in town as merchants.
- **The older official shop interface supported buying as well as selling.** That meant a player could advertise demand for materials, not merely list existing stock.
- **Original-era demand concentrated on certain goods.** A July 2006 reviewer particularly noticed enhancement gems, pets and Fortune Boxes in player stalls.
- **Offline consignment was confirmed by 2015.** Players could list items for seven days and sell them while logged out.
- **The later marketplace had a percentage-based fee.** In May 2026, Netgame officially specified a 1% consignment commission and removed the previous commission cap.
- **The economy suffered from weak ordinary-item demand.** Contemporary reviewers complained that most drops were too uninteresting to bother selling.

The opportunity for your mobile MMO-lite is to preserve the feeling of visiting a busy marketplace, while making buying and selling practical for players who only have short sessions.

---

## 1. Original and later trading systems compared

| Feature | 2006 Hero Online | Hero Plus, 2015 | Hero Plus, 2026 |
| --- | --- | --- | --- |
| Direct item/gold trading | Supported in older official guide; exact 2006 UI version uncertain | Confirmed | Confirmed |
| Player-operated stalls | Confirmed at level 10 | Confirmed | Confirmed |
| Stall capacity | Six slots in undated official guide | Not reverified | Not reverified |
| Buying stalls | Present in undated official guide | Confirmed | Still included in personal-shop system |
| Custom merchant signs | Confirmed | Confirmed visually | No removal documented |
| Offline consignment | Not verified in 2006 | Confirmed | Confirmed |
| Consignment duration | Unknown | Seven days | Current duration unverified |
| Consignment slots | Unknown | Ten per character | Current capacity unverified |
| Market commissions | Unknown | Buying-stall 1% reported | Consignment 1% official |
| Maximum consignment price | Unknown | Unknown | 990 billion gold |
| Direct trade gold limit | Unknown | Unknown | 50 billion gold |
| Trading restrictions | Item-dependent behavior incompletely documented | Tradeable-item restrictions | Explicit Trade/Consign/Bound properties |

**Important version distinction:** The six-slot official beginner guide is undated. It documents an older interface, but we cannot prove that the same maximum remained unchanged throughout the game's history.

---

## 2. Player-to-player direct trading

*Documented in Netgame guide*

The older official client guide describes direct trading through another player's interaction menu.

The sequence was simple:

1. Player A requests a trade with Player B.
2. Player B accepts the request.
3. Both players place items and/or gold into the trading window.
4. Both players approve the final exchange.
5. The game completes the trade.

This workflow is described in both the Netgame interface guide and a detailed September 2015 Turkish community tutorial.

### Inventory restrictions

The 2015 guide explicitly advises players to ensure they have sufficient inventory space before trading.

It also describes the exchange window as accepting eligible materials and gold.

*Community-documented, 2015*

What remains uncertain is the exact validation behavior when inventory space becomes insufficient at the moment both players confirm.

### Anti-scam protections

I could verify mutual approval, but not the complete historical safeguards.

Specifically, I could not establish whether changing an item or gold amount automatically revoked the other player's confirmation, whether item tooltips locked during final acceptance, or whether high-value exchanges required a second confirmation.

Those details are essential for an original multiplayer implementation.

**Design implication:** Your system should never simply copy an old two-window trade interaction without modern transaction security.

---

## 3. Personal shops: becoming a merchant

*Documented, July 2006*

This was arguably Hero Online's most memorable trading feature.

Players who reached level 10 could activate a personal shop. Their character became a stationary merchant with a visible banner advertising their goods.

A contemporary example was a merchant advertising inexpensive wolves.

The older Netgame guide describes opening the shop from the character's interaction menu, selecting sale mode and entering prices for items. It explicitly lists a maximum of six items in that interface.

### Selling-stall workflow

1. Select your character and open the shop option.
2. Choose sale mode.
3. Add eligible items from inventory.
4. Set each item's price.
5. Enter an optional shop sign.
6. Open the shop for other players.

### The social value

A player entering a populated market could browse other characters, read shop signs, inspect items and see who was selling what.

A May 2009 reviewer described a marketplace packed with merchants, while noting that even the busy area ran acceptably on their computer.

*Contemporary reviewer observation*

The most interesting aspect is the combination of player presence and commerce.

A valuable merchant wasn't only a menu entry. They existed in the same shared world as everyone else.

**Inference:** That physical presence could support repeated visits, recognizable traders, social interaction and the feeling that the game's economy was alive.

But the reviewer observations do not establish that shop owners earned consistently good profits.

---

## 4. Buying stalls: players could advertise demand

This is an important mechanic that I'd preserve in some form.

A conventional marketplace primarily asks players to list items they want to sell.

Hero Online's older official guide also included a Purchase Store.

That reversed the transaction:

1. A player wanted a particular material.
2. They advertised a purchase price.
3. Another player could supply the item.
4. The item was sold directly into the buyer's demand.

The September 2015 Turkish guide describes creating a buying stall for an item represented in the player's inventory, selecting an amount and price, and opening the stall. It reports a 1% commission for buying stalls.

*Community-documented, 2015*

The exact timing and accounting of the commission remain uncertain.

### Why this mattered

Imagine three different players:

- A monster farmer has 40 Rough stones.
- A blacksmith-focused player wants 100 Rough stones.
- A PvP player wants an enhanced sword.

Without buying orders, the farmer must search for somebody already offering to buy stones—or list the stones and hope someone notices.

With a buying stall, the blacksmith can announce demand directly.

**Example player-material economy**

| Player | Role |
| --- | --- |
| Farmer | Produces materials |
| Crafter | Buys stones |
| Fighter | Buys upgraded gear |

Materials, equipment and gold circulate between different player activities.

This is an economic illustration, not a measured reconstruction of actual 2006 trading volumes.

This is one of the most useful ideas for your game because you already plan multiple enhancement stone types.

Buying orders can help materials find buyers without every player needing to operate a shop continuously.

---

## 5. Offline consignment: the major convenience upgrade

*Community-documented, 23 September 2015*

By September 2015, Hero Plus supported an NPC-operated consignment marketplace that functioned while sellers were offline. The Turkish guide describes three simultaneous trading methods: direct trade, personal shops and consignment.

This was a substantial improvement over relying entirely on merchant characters standing in town.

### The 2015 consignment process

The guide describes the following steps:

1. Speak with the consignment NPC.
2. Open the consignment interface.
3. Browse categorized items and compare prices.
4. Select an inventory item to sell.
5. Enter the asking price.
6. Review the displayed commission.
7. Confirm the listing.
8. Receive status notices after a sale or listing expiration.

The same guide specifies the following rules:

| Property | September 2015 value |
| --- | --- |
| Maximum listed items | 10 per character |
| Listing duration | Up to 7 days |
| Offline selling | Yes |
| Search/browsing | Item categories and visible prices |
| Listing cancellation | Yes |
| Return on cancellation | Item returned to inventory |
| Result notifications | Displayed when logging in |
| Consignment commission | Displayed in interface; exact 2015 rate not established |

This is stronger evidence than simply knowing a marketplace existed. It gives us an actual, fairly complete merchant workflow.

### What happened after listing expiry?

The guide states that items remain on sale for at most seven days and that players are notified of sold or unsold listings. It does not completely specify the persistence and item-retrieval policy for expired listings.

Therefore, I cannot confirm whether expired items automatically moved to inventory, had to be reclaimed from the NPC, or entered a separate storage interface.

### Later market relocation

A March 2026 official notice confirms that the consignment NPC was moved to the Market Place and removed from Dragon Castle. Another notice instructed players to concentrate personal shops in the Market Place.

This reveals a consistent design objective: concentrating trading activity in a common destination.

**Inference:** A central marketplace can reinforce social presence even after offline buying and selling become possible.

For a mobile game, that suggests a useful hybrid: market access that is convenient, but a town hub that still feels socially active.

---

## 6. Marketplace fees, gold limits and inflation

This area has unusually strong documentation from May 2026.

### Hero Plus: 19 May 2026 update

*Officially documented*

Netgame published a gold and item system overhaul on 19 May 2026. It expanded gold capacities, improved large-number input, changed item stacking and adjusted consignment limits and fees.

| Mechanic | Documented 2026 value |
| --- | --- |
| Maximum item stack | 9,999 |
| Direct player-to-player gold trade | 50 billion |
| Gold transfer by mail | 50 billion |
| Personal-shop gold transaction | 50 billion |
| Maximum consignment listing price | 990 billion |
| Consignment commission | 1% |
| Previous maximum commission cap | 50 million, removed |
| Inventory/storage gold limit | 9,999 billion, per detailed notice |

The accompanying shorter maintenance announcement describes an inventory gold maximum of 999.9 billion, which differs from the detailed notice's 9,999 billion. I would use the detailed May 19 notice for the table while flagging the contradiction.

### Commission calculation

The 2026 announcement defines commission as 1% of the listed item price.

For illustrative listing prices:

| Asking price | 1% commission | Seller proceeds if deducted from sale |
| --- | --- | --- |
| 100,000 gold | 1,000 | 99,000 |
| 1,000,000 gold | 10,000 | 990,000 |
| 10,000,000 gold | 100,000 | 9,900,000 |
| 100,000,000 gold | 1,000,000 | 99,000,000 |
| 1,000,000,000 gold | 10,000,000 | 990,000,000 |

The arithmetic is straightforward, but the source does not explicitly establish when the commission is charged.

It may be collected on listing, on successful sale, or through another combination of charges.

Therefore, the seller-proceeds column is conditional, not a verified Hero Plus settlement rule.

### What does this tell us about the economy?

*Inference*

Extremely high gold-storage and listing limits suggest that the economy had accumulated very large nominal prices or balances.

However, maximum allowed prices are not the same as actual market prices.

We cannot conclude that an ordinary weapon cost billions of gold or estimate a historical inflation rate from limits alone.

The May 2026 announcement also describes the update as a response to player surveys, indicating that currency caps and trading convenience were important enough to warrant official changes.

---

## 7. What players bought and sold

The original trading economy is better documented qualitatively than numerically.

### Original-era market demand

A July 2006 firsthand reviewer observed that player shops frequently advertised three kinds of goods:

- Enhancement gems.
- Pets.
- Fortune Boxes.

The reviewer also observed that equipment and gold drops were common, with many players selling ordinary items to NPC merchants instead of other players.

*Contemporary firsthand observation*

This aligns with the blacksmith and pet research.

Gems helped players improve gear, pets had independent progression value, and Fortune Boxes offered randomized rewards.

Those goods could remain interesting even when ordinary dropped weapons became obsolete.

### Skill books

A May 2007 Turkish player discussion describes players purchasing advanced skill books from the marketplace, including books obtained from expensive chests.

*Community-reported, 2007*

That suggests advanced martial arts had an active player-to-player acquisition path.

### Weak demand for ordinary equipment

A July 2006 player complained that they sold almost everything to NPCs because ordinary dropped items weren't worth the effort of operating a stall.

This is valuable criticism because it points to an important economic weakness:

**An elaborate trading UI does not create a valuable economy if the goods being traded are interchangeable and uninteresting.**

The original system could support buying, selling and visible shops, yet many ordinary equipment drops apparently didn't generate much motivation to trade.

### Player-to-player trading versus gold sinks

Consider an enhanced sword sold for 20,000 gold.

If another player purchases it directly, the buyer loses 20,000 and the seller receives 20,000. Assuming no fee, total gold in circulation doesn't change.

If the marketplace collects a 1% commission, 200 gold leaves circulation.

But if a player pays 5,000 gold to an NPC blacksmith, that entire 5,000 is removed.

This gives us three different mechanisms:

| Mechanism | Economic function |
| --- | --- |
| Monster gold drops | Currency creation |
| Player-to-player sales | Currency redistribution |
| Market commission | Currency sink |
| NPC blacksmith fees | Currency sink |
| Item enhancement failure | Equipment/material destruction |
| Dismantling | Equipment destruction plus material creation |

**Inference:** Hero Online's economy is interesting precisely because enhancement, farming, pet acquisition and trading could generate different kinds of demand and resource flows.

For our game, this is why we should eventually simulate the blacksmith and marketplace together.

---

## 8. Cash shop involvement and paid trading convenience

A community-posted Hero Online cash-shop listing from August 2006 described a temporary item that expanded Sale/Purchase Store capacity and changed the stall's visible sign.

Its advertised duration was 72 hours, counting only while the character was connected.

*Community-documented, August 2006*

The listing said that a shop already open could continue after the item's duration expired, but opening a new shop required the benefit to be active.

This suggests that merchant convenience or visibility could be monetized very early in the game's history.

However, I couldn't verify the exact extra slot count or the item's complete pricing across regions.

### Why this matters

Shop capacity affects economic participation.

If one player can list six items while a paying player can list significantly more, the paying player gains a convenience and potentially a commercial advantage.

That doesn't automatically mean the economy was pay-to-win, because the impact depends on demand and listing constraints.

But for your mobile game, I would avoid monetizing transaction capacity or market-search access at launch.

---

## 9. Community feedback: what worked and what frustrated players

The evidence is mixed, rather than uniformly positive.

### What seemed to work

**A lively physical marketplace.** The 2009 reviewer describes a heavily populated trading area full of merchants.

**Useful specialist goods.** The 2006 reviewer noticed a market for enhancement gems and pets, both of which had gameplay uses beyond equipment level requirements.

**Player convenience improved later.** By 2015, offline consignment meant that selling no longer required keeping a merchant character online, according to the Turkish player guide.

### What players disliked

**Low-value drops.** One contemporary player considered ordinary loot too repetitive and preferred immediate NPC sales.

**Grinding and automation.** The same 2006 discussion complained about bots and repetitive grinding, both of which could affect the supply side of a player economy.

**Inventory-theft and phishing concerns.** In November 2006, Hero Online publicly warned players that unofficial websites advertising cheats, gold and rare items were being used to steal account credentials and inventories.

That last issue is not evidence of a defect in the in-game trade-window implementation. It is evidence of security problems surrounding a valuable virtual-item economy.

### What the evidence cannot tell us

I could not reconstruct a reliable history of:

- Material price changes over several years.
- Average sales per merchant.
- Market concentration among wealthy players.
- Exact prevalence of price manipulation.
- Trade-related item duplication exploits.
- Percentage of players using personal shops versus direct trades.
- Whether selling rare gear was more profitable than enhancing it.
- How much currency bots introduced into the economy.

The player observations establish experiences and problems, not statistically representative market data.

---

## 10. Hero Online Global, The Hero Global and private servers

### The Hero Global

The site's The Hero Global NPC directory documents merchants, bankers, blacksmiths, bookstores, taverns and other trade-related NPC roles.

That confirms the existence of trade-oriented NPC infrastructure on that site, but I could not independently verify current player-market rules, commissions or a version-specific consignment interface.

It would be incorrect to claim that its trading limits match Hero Plus's May 2026 limits.

### Hero Online Global

I did not recover a separately authenticated trading guide for a release specifically identified as Hero Online Global.

Its trading fees, market limits and item-binding behavior remain unverified.

### Private servers

A May 2020 RaGEZONE discussion documents community efforts to distribute a Hero Online/Hero Plus emulator and database associated with the Dragon Legend private server.

Subsequent discussion describes the emulator as incomplete relative to official Hero Plus and containing various customized systems.

That establishes a private-server development ecosystem, but not an authenticated trading ruleset.

I did not find independently verified private-server data for stall capacities, consignment fees, marketplace taxes, trade limits, listing restrictions or the value of any currency used.

These are open research gaps.

---

## 11. Implications for our mobile MMO-lite

We've now researched several interlocking parts of Hero Online: blacksmith, loot, martial arts, classes, pets and trading.

The marketplace connects all of them.

My recommendation is to use a hybrid marketplace: a physical merchant district for nostalgia, combined with offline listings and buying orders designed for short mobile sessions.

### What to adopt, modify and avoid

| Historical mechanic | Decision | Reason |
| --- | --- | --- |
| Physical marketplace with merchants | Keep | Social identity and nostalgia |
| Custom shop banners | Keep | Gives traders presence and personality |
| Direct player trades | Keep later | Useful social interaction but needs secure transactions |
| Selling stalls | Adapt | Shouldn't require staying logged in |
| Buying stalls | Keep | Strong demand-discovery mechanic |
| Offline consignment | Keep | Essential for mobile convenience |
| Seven-day listings | Good starting point | Predictable turnover |
| Item-category browsing | Keep | Helps discovery |
| One-percent tax | Test as starting value | Simple transaction sink |
| Paid stall expansion | Avoid | Can distort trading competition |
| Hard-to-read large gold amounts | Avoid | Mobile readability |
| Tradeable unique equipment | Keep selectively | Makes rare loot socially valuable |
| Universal tradability | Avoid | Can undermine progression rewards |
| Auction bidding | Not needed initially | Additional complexity and exploit risks |

---

## 12. Proposed market design

**The Merchant Quarter** — *original concept; conceptual direction for your game's physical market district. Original artwork inspiration, not Hero Online assets.*

| Part | What it does |
| --- | --- |
| Personal Stall | Character and sign visible in the market |
| Market Board | Search and buy from offline listings |
| Buy Orders | Request materials at a chosen unit price |
| Direct Trade | Negotiate and exchange with another player |

There should be one underlying marketplace service, not separate item economies for the visible stalls and market board.

A physical stall would simply be a social presentation of the owner's listings.

That prevents two distinct trading systems from competing with one another.

---

## 13. Suggested starting marketplace rules

These are original design proposals, not recovered Hero Online values.

| Parameter | Initial recommendation |
| --- | --- |
| Market unlock | Level 10 |
| Seller listings | 8 active listings |
| Buyer orders | 4 active orders |
| Listing duration | 7 days |
| Sale commission | 2%, charged only on sale |
| Listing fee | None initially |
| Minimum price | 1 gold |
| Currency | Existing in-game gold |
| Offline buying/selling | Yes |
| Listing cancellation | Allowed |
| Expired item recovery | Guaranteed |
| Search filters | Category, rarity, enhancement level, price |
| P2P barter | Future phase |
| Real-money trading | Not supported in-game |

### Why 2% rather than copying 1%?

The original 2026 Hero Plus commission is documented at 1%, but our gold generation, average transaction prices and blacksmith fees will be completely different.

A 2% test fee is large enough to create a measurable sink while remaining relatively small compared with item prices.

If the simulation shows that the market removes too much currency or discourages low-value material sales, reduce it.

A minimum-tax rule might eventually be useful, but I wouldn't add one until we understand the transaction-size distribution.

---

## 14. Interactive market-fee calculator

*(The original was an interactive calculator: an asking price, a commission of 1%, 2% or 5%, and the result. Its example state:)*

| | Gold |
| --- | --- |
| Asking price | 10,000 |
| Commission | 2% |
| Seller receives | 9,800 |
| Gold removed | 200 |

Assumes commission is deducted after a successful sale, rounded down to whole gold. Proposed system behavior, not a verified Hero Online settlement rule.

---

## 15. How the marketplace connects to the blacksmith

This is the relationship I'd design carefully before implementation.

1. **Monster drops and gathering** — weapons, Rough stones, crafting resources.
2. **Player marketplace** — farmers sell materials; blacksmiths purchase and process them.
3. **Enhance, Combine, Extract, Dismantle** — generate, transform and consume resources.
4. **Tradeable improved equipment** — buyers purchase valuable finished items; seller receives gold.

I would not automatically make every material tradeable.

The initial economy needs explicit rules for:

- Rough and Polished enhancement stones.
- Normal and rare equipment.
- Upgraded equipment.
- Warding Charms.
- Boss rewards.
- Pets and mounts.
- Skill books.
- Quest rewards.

For example, freely tradeable Warding Charms could become one of the highest-demand commodities in the game, because they reduce the risk of destroying enhanced equipment.

That may be desirable for a player economy, but only if their acquisition rate, usage rate and availability are balanced.

---

## 16. Preventing economic exploits

For a multiplayer game, the trading service must be authoritative.

I would give Claude these requirements:

- **Item integrity:** Every unique item has a persistent identifier. A listed item cannot simultaneously exist in inventory, equipment and marketplace escrow.
- **Atomic purchases:** Item ownership and gold transfer occur together, or neither occurs.
- **Order funding:** Buy orders reserve sufficient gold when created, preventing buyers from spending the same funds elsewhere.
- **Safe cancellation:** Canceling a listing returns the exact item instance, including enhancement history and rolled properties.
- **Trade confirmations:** Both players must approve the exact final state. Any modification invalidates previous approval.
- **Quantity and rounding:** Stack splitting, partial fills, taxes and order quantities must use integer arithmetic with explicitly defined rounding.
- **Audit logs:** Every listing, sale, cancellation, transfer and fee has a transaction record.
- **Anti-abuse:** Apply server-side rate limits and monitor suspicious transfers, without assuming every unusually valuable transaction is fraudulent.
- **Player protection:** Never delete an expired listing's item simply because the recipient's inventory is full; retain it in secure claim storage.

These aren't historical Hero Online mechanics. They're technical requirements I recommend for your original game.

---

## 17. Implementation scope for Unity

I wouldn't build everything at once.

Since your prototype already has equipment and an evolving blacksmith design, start with the smallest useful market:

- **Phase 1 — Offline marketplace:** Players can list eligible items, search listings, buy items, cancel listings and collect proceeds.
- **Phase 2 — Buy orders:** Players can post requests for stackable materials and other supported commodities.
- **Phase 3 — Physical market stalls:** Display the player's listings through a merchant character or stall in the town hub.
- **Phase 4 — Direct trading:** Add secure player-to-player barter and item-for-gold transactions once multiplayer interactions are established.

Before any implementation, Claude should inspect how your Unity project represents inventory, unique item instances, player ownership, currency, persistence and networking.

If the game currently has local-only progression, don't build a fake client-authoritative marketplace. Design the contracts and defer real trading until a trusted shared backend exists.

---

## 18. What we still need to measure

Our blacksmith simulation did not have a real marketplace.

The next economy simulation should examine the following relationships:

| Metric | What it tells us |
| --- | --- |
| Rough-stone average sell price | Whether farming materials is worthwhile |
| Polished-stone price relative to Rough | Whether combining is economically rational |
| Warding Charm market value | Price of protecting high-level equipment |
| Equipment replacement cost | Actual risk of destructive +6–+8 attempts |
| Item listing sell-through rate | Whether the market has enough buyers |
| Average time to sale | Whether listings are useful in short mobile sessions |
| Active buyers versus sellers | Marketplace liquidity |
| Market tax versus blacksmith gold fees | Relative gold-sink effectiveness |
| Wealth concentration | Whether trading enriches a very small group |
| Price volatility | Whether players can plan their upgrades |

We can't responsibly invent these figures before simulating supply, demand and player behavior.

---

## 19. Most valuable sources

| Source | What it confirms |
| --- | --- |
| July 2006 Hero Online hands-on | Level-10 merchant stalls, banners and popular trade goods |
| Netgame Personal Shop guide | Sale/purchase shop modes and six item slots |
| Netgame UI and direct trading guide | Direct trade window and player-shop interactions |
| September 2015 Turkish marketplace guide | Direct trade, buy/sell stalls, offline consignment, seven-day listings and ten listing slots |
| May 2009 player-experience review | Busy trading district and market activity |
| July 2006 community debate | Complaints about loot value, grinding and shop effort |
| May 2026 official gold/item overhaul | Consignment commission, gold limits, stack capacity and tooltip rules |
| March 2026 market relocation | Market Place consolidation |
| August 2006 cash-shop item list | Temporary merchant expansion and special signage |
| November 2006 account-security warning | Historical external item/gold scam concerns |
| The Hero Global NPC directory | Vendor and storage NPC roles |
| Private-server emulator discussion | Unverified emulator/data research lead |

---

## Final assessment

Hero Online's trading system had three particularly worthwhile characteristics.

**It made merchants visible.** Players could turn their characters into shops and create an identifiable commercial district.

**It supported both supply and demand.** Players could operate selling stalls and buying stalls, allowing specialists to gather materials while other players purchased them.

**It eventually embraced offline convenience.** The 2015 consignment system was an important improvement over requiring players to remain online to sell items.

The biggest weakness wasn't necessarily the trade interface. Contemporary players complained that much of the ordinary loot wasn't valuable enough to trade in the first place.

For your game, I'd preserve the social merchant district but build one secure offline marketplace beneath it. Let players farm stones, tame companions, discover skill books, forge valuable equipment and trade those goods without needing to stay online for hours.

The next essential piece isn't another shop mechanic. It's an integrated economy model connecting loot drops, blacksmith destruction, character progression and trading. That's what will determine whether your marketplace feels alive or becomes a collection of unsold listings.
