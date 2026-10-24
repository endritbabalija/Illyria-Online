# Hero Online: PvP, Factions, Karma & Bounty

Historical systems research · Original 2006 Hero Online, later Hero Plus, The Hero Global and community documentation · 24 October 2026

> The source links and the interactive figures (the four PvP modes in §1, the bounty flow in §2, the faction-war loop in §4, the dominance cycle in §9, the roadmap in §12B, the arena layout in §12C, the bounty calculator in §12D and the architecture in §13) didn't come through when this was pasted in; their text is kept below.

## Executive summary

Hero Online's PvP was not one system. It was a collection of interconnected systems that gave fighting other players different purposes and consequences.

Some PvP was consensual and consequence-free. Some revolved around collecting a bounty. Some was tied to guild rivalries and reputation. Large-scale faction warfare offered rewards, prestige and—in the original design—control over valuable gathering areas.

The biggest discovery is that Japan's September 2006 update documented a real player-funded bounty economy, with a 25% registration fee, multiple bounty tiers, repeatable bounty contributions and restricted areas where targets could be attacked.

That's considerably more interesting than simply adding a PvP arena.

### Six important findings

- **PvP was optional in the original design.** A 2006 developer interview described dedicated PvP zones, duels, faction warfare and bounty targets while emphasizing that players could enjoy PvE without participating in PvP.
- **Bounties created legal targets outside normal PvP areas.** The September 2006 Japanese implementation permitted attacks on wanted characters across designated regions.
- **Karma could have practical consequences.** Negative-alignment characters could lose access to ordinary NPC services and face forced exile or imprisonment.
- **Faction warfare had economic consequences.** In a June 2006 interview, the developer described faction battles every six hours for exclusive access to valuable mining resources.
- **Later faction wars developed several objectives.** Official Netgame guides describe leader assassination, flag carrying, territory capture and castle control.
- **Later Hero Plus added an explicit Honor economy.** An August 2020 guide explains level-difference penalties, ranking, Honor restoration through gold payments and premium items that reduced XP losses.

**My initial design conclusion:** Your game could get a lot of mileage from three PvP layers—duels, faction battles and a carefully controlled bounty system—without immediately implementing unrestricted open-world killing.

### Historical mechanics summary

| System | Historical finding | Version | Confidence |
| --- | --- | --- | --- |
| Duels | Consensual PvP without ordinary retaliation consequences | 2006 | Developer-documented |
| Duel level requirement | Level 40 reported | July 2006 | Community-reported |
| Dedicated PvP zone | Mudo Island | 2006 | Developer-documented |
| Bounty hunting | Targets attackable outside designated safe areas | 2006 | Developer-documented |
| Bounty registration fee | 25% of posted bounty | Japan, Sept. 2006 | Published update rules |
| Bounty tiers | Low, ordinary, high | Japan, Sept. 2006 | Published update rules |
| Karma | Good and evil actions change alignment | Japan, Sept. 2006 | Published update rules |
| PK level protection | Characters below 2甲子 protected in Japanese rules | Japan, 2006 | Published, conversion uncertain |
| Bad-karma consequences | NPC restrictions, possible prison transfer | Japan, 2006 | Published update rules |
| Great Mine war interval | Every 6 hours | 2006 developer design | Developer-reported |
| Faction affiliation | Linked to House/guild membership | Older Netgame guide | Documented |
| House creation | Level 40 and 10M gold | Older Netgame guide | Documented |
| Faction war scoring | Guard 1, commander 5, player 10 | Later Netgame guide | Documented |
| Faction war rewards | 20 coins winner, 10 loser, plus chests | Later Netgame guide | Documented |
| Honor restoration | Restore up to 1,000 Honor for a level-dependent fee | Hero Plus 2020 | Documented |
| PvP damage formula | Not recovered | All versions | Unknown |
| Original bounty payout formula | Varies by bounty tier, exact table unavailable | Japan, 2006 | Partially documented |

The Japanese September update is especially important: the rules announced in August and the rules actually published for the September 19 implementation differ in several details, including eligible bounty regions.

---

## 1. Original 2006 PvP architecture

The clearest contemporary account comes from an August 2006 interview with lead designer Jack Stanley.

He described four distinct ways players could engage in PvP:

| Mode | Description | Evidence |
| --- | --- | --- |
| **Duels** | Mutual combat challenges without normal open-world consequences | Documented in August 2006 |
| **Dedicated PvP territory** | Mudo Island provided an area for players who wanted unrestricted competitive encounters | Developer description, 2006 |
| **Bounty hunting** | A posted bounty could make a player attackable in otherwise protected parts of the world | Developer description, 2006 |
| **Faction warfare** | Larger competitive encounters involving rival factions and collective objectives | Developer description, 2006 |

### Duel requirements

A July 2006 player discussion reports that players needed to reach level 40 to duel in an arena near the marketplace. The same player explains that PvP-designated areas did not require a duel request.

*Community-reported, July 2006*

The level-40 figure is not independently established by the developer interview, so I would not treat it as an invariant rule across all early servers.

### What this design achieved

*Inference*

The multiple modes allowed the game to offer combat at different levels of risk.

Players could test builds through duels, participate in organized wars or pursue individual conflicts through the bounty system.

That distinction remains useful today. A player who enjoys fighting other players should not necessarily be required to accept unrestricted attacks while completing ordinary quests.

---

## 2. The original bounty system: unusually detailed evidence

This is the standout discovery of the research.

On 25 August 2006, Japanese gaming publication 4Gamer published MGame Japan's planned September bounty rules.

On 19 September 2006, it published the implementation update with revised conditions.

### How a bounty worked

1. **Player registers a bounty** — select a target and commit gold.
2. **Registration fee charged** — 25% of bounty amount.
3. **Target becomes wanted** — eligible players can attack in permitted regions.
4. **Hunter defeats target** — payout awarded under the bounty's rules.

### Documented bounty rules

*Published Japanese implementation rules, September 2006*

- Players selected another character and registered a bounty amount.
- Bounty registration required an additional 25% fee.
- A bounty could not be canceled after registration.
- Players could contribute additional bounty money to an existing target.
- Bounties were classified into low, ordinary and high categories.
- The amount influenced how many contracted kills were supported and how much was paid for each successful kill.
- Bounty status made a target legally attackable in eligible regions.

### Example economic calculation

This example uses the documented 25% registration fee, not a reconstructed historical bounty amount.

| Posted bounty | Registration fee | Total gold paid |
| --- | --- | --- |
| 10,000 | 2,500 | 12,500 |
| 50,000 | 12,500 | 62,500 |
| 100,000 | 25,000 | 125,000 |
| 1,000,000 | 250,000 | 1,250,000 |

The registration fee is a direct potential gold sink. The bounty principal is funded by players and transferred through payouts, although unused-principal handling is not established.

### Important: repeated payouts

The Japanese rules explicitly state that bounty tiers changed the number of requested kills and payout per kill.

This means the system was not necessarily one bounty, one death, one payout.

A single bounty could support multiple executions depending on its tier and amount.

Unfortunately, I could not recover the exact allocation or payout table.

### Regional restrictions changed before release

The August announcement specified a list of eligible regions and exceptions.

The September implementation expanded the geographic wording to nearly every area except identified cities, markets and faction-war locations.

This is an actual source/version disagreement, not something to average or reconcile by guessing.

For the September Japanese implementation, the final published rules should take precedence over the August preview.

### An abuse problem the historical record doesn't resolve

The published rules don't explain whether the target's friends could repeatedly claim a bounty through coordinated defeats, whether rewards were restricted by account relationships or whether the same two players could repeatedly participate.

**Inference:** Any player-funded bounty system that pays on confirmed kills must account for collusion, alternate accounts and intentional deaths.

This is especially important for your project if gold and rare crafting materials can be traded.

---

## 3. Karma, criminality and reputation

The 2006 Japanese implementation distinguished between initiating unlawful attacks and defending against them.

### Offensive and defensive PK

*Documented, September 2006*

The update defined two states:

- **Aggressive PK:** A player intentionally initiates an attack.
- **Defensive PK:** A player is attacked and enters a defensive state automatically.

Actions were classified as good or evil and changed an alignment score. Character titles could change as alignment moved across thresholds.

The official English-language UI guide independently describes automatic self-defense modes that protect players from penalties after being attacked.

### Bad karma had real consequences

The Japanese release announcement documented two especially important penalties:

- Criminal characters could be denied access to ordinary inns, storage NPCs and blacksmiths outside a dedicated criminal settlement.
- If defeated by a good-aligned player, a criminal could have a chance of being transported to a prison-like area requiring actions to escape.

These consequences affected the player's everyday progression and economic activities.

A criminal might have gold and equipment but lose convenient access to the same crafting and storage infrastructure used by ordinary players.

### Criminal settlements

*Documented in Japanese release material*

The criminal settlement could be entered by negatively aligned characters, including through certain travel NPCs or respawn options.

This effectively created an alternative location for a subset of players.

**Inference:** A criminal settlement has the potential to become its own social hub. But the historical documentation does not prove that such a community actually developed or that its economy was healthy.

### What remains unknown

| Karma question | Finding |
| --- | --- |
| Initial karma score | Not verified |
| Points awarded for self-defense | Exact amount unknown |
| Points lost for killing innocents | Exact amount unknown |
| Alignment thresholds | Exact values not recovered |
| Duration of criminal status | Unknown |
| Passive karma recovery | Unknown |
| Prison transfer probability | Unknown |
| Escape quest requirements | Not recovered |
| NPC prices in criminal settlements | Unknown |
| Whether karma modified item drops | Not established |

**A crucial design distinction:** Japanese alignment/karma and later Hero Plus Honor/Fame should not automatically be treated as the same numerical variable.

---

## 4. Original faction warfare and resource control

This is the clearest economic connection in the original PvP design.

### The Great Mine

*Developer-documented, June 2006*

Lead designer Jack Stanley described a contested resource area that became available for faction warfare every six hours.

The winning faction gained exclusive access to its rare crafting materials until the next battle.

1. **Scheduled faction conflict** — two rival factions contest a resource location.
2. **Territory ownership** — winning faction receives temporary access.
3. **Rare crafting resources** — resources enter equipment production and enhancement.
4. **Player economy** — material demand, crafting, trading and preparation for the next conflict.

This is a particularly strong form of systemic design: players fight because territory produces economically valuable resources, not merely to increase a PvP counter.

**Evidence limitation:** The developer interview establishes the intended rules in June 2006. I did not independently verify the full live production history, actual winning rewards or the specific quantities mined during each six-hour ownership window.

### How players joined factions

The older official Netgame guide describes two opposing factions, Shao and Zhuang, with affiliation chosen when creating or joining a House (guild).

It states that the choice could not be changed afterward.

The same guide states:

- House creation required level 40.
- Creating a House cost 10 million gold.
- Houses could participate in wars and accumulate fame.

These are official-guide rules, but the page is undated. They should not be automatically assigned to every 2006 server.

### The economic implications

*Inference*

If one faction controls rare material access, it can potentially affect the blacksmith economy through supply.

This creates several possible outcomes:

- Rare material prices rise when access is limited.
- Players organize to challenge a dominant faction.
- Guild membership gains economic value.
- Traders specialize in buying resources during periods of abundance.

However, those are plausible market responses, not measured historical Hero Online price effects.

An exclusive resource monopoly can also become self-reinforcing: the strongest faction receives the best materials, which help it remain strongest.

That is one of the biggest risks to avoid in your mobile implementation.

---

## 5. Later Hero Plus: structured faction wars

The official Netgame PvP guide describes several distinct faction-war formats.

*Officially documented, guide publication date unclear*

### A. Faction War — attack the enemy leader

This is a base-assault mode.

Players advance toward the enemy faction's leader while fighting guards, commanders and opposing players.

The objective combines score accumulation with a potential instant victory through killing the faction leader.

| Target | Points |
| --- | --- |
| Enemy guard | 1 |
| Enemy commander | 5 |
| Enemy player | 10 |
| Enemy faction leader | Immediate victory |

**Rewards**

| Result | Official reward |
| --- | --- |
| Winning faction participant | 1 special chest + 20 Battle Coins |
| Losing faction participant | 1 special chest + 10 Battle Coins |

This is an interesting design because both teams receive something. Even a losing participant has a reason to finish the match.

But the guide does not verify how much participation was necessary to qualify.

### B. Great War — control five objectives

Another documented mode asks factions to occupy five statues distributed around a battlefield.

The guide describes:

- Capturing statues by standing near them.
- Colored indicators showing controlling faction.
- Points changing as ownership changes.
- Defeated players respawning at their faction's safe location.
- Rewards awarded to winning and losing participants.

The guide states that factions begin with 10,000 points, while losing a faction leader costs 1,000 points.

| Mechanic | Value |
| --- | --- |
| Control objectives | 5 |
| Starting faction points | 10,000 |
| Leader-loss penalty | 1,000 |
| Winning reward | 2 gem boxes + 20 Battle Coins |
| Losing reward | 1 gem box + 10 Battle Coins |

### C. Flag Kingdom — capture and return

The official guide describes a mode in which destroying a statue drops a flag.

Players then carry it toward their own base. If a carrier dies, the flag drops at that position.

One particularly interesting restriction: Wind Drift could not be used while carrying a flag.

*Officially documented*

That is a deliberate movement-balance rule.

Without it, players with the fastest martial mobility skills could potentially dominate flag objectives.

### D. Diamond Castle — guild-held temples

The official guide describes five capturable temples with experience bonuses for the owning House.

It specifies:

- First captured temple: 20% XP bonus.
- Each additional consecutive temple: +10% XP.
- Temple post respawn: one hour.

This is an especially important link between PvP and PvE progression.

### E. Golden Castle — control and mining privileges

The later official guide describes a more involved castle-war process involving gates, defense towers, sealed stones, House leaders and control duration.

The controlling faction could gain access to mining-related privileges and special NPC support.

Another documented feature was a temporary randomly assigned combat buff that doubled HP, defense and offense for its recipient.

**Design observation:** Later Hero Plus moved well beyond deathmatch PvP. Its battlefield modes gave different roles to frontline fighters, defenders, objective runners, guild leaders and players escorting important targets.

---

## 6. Scheduled events and level brackets

The official Netgame warfare guide describes two level brackets:

- Level 40–100.
- Level 101–199.

For some formats, it lists multiple scheduled occurrences per week or day, with a ten-minute registration window before the event starts.

The Hero Global warfare page also lists separate daily schedules for its lower and upper level brackets.

These should be treated as documented published schedules, not a guarantee that those event times are still active in October 2026.

For your game, the underlying principle is more valuable than the exact timetable: concentrate competitive activity into predictable moments so that enough players participate together.

A mobile MMO-lite may not have sufficient concurrency to support several different PvP modes running continuously.

---

## 7. Honor, Fame and PvP penalties in Hero Plus

The clearest later numerical reference is Netgame's 14 August 2020 Honor Points guide.

### The 20-level difference rule

The guide explicitly discourages attacking players more than 20 levels below your character.

It describes the following outcome:

- The higher-level attacker receives the maximum Honor loss applicable to that encounter.
- The attacker receives no XP reward.
- The lower-level victim still suffers normal death-related XP and Honor losses.

*Documented, Hero Plus 2020*

This is an important criticism of the design: the system punishes the attacker, but the low-level victim can still lose progress despite being severely outmatched.

### Honor restoration

The guide describes restoring Honor through NPCs when a character's Honor falls below 1,000.

It gives concrete gold costs:

| Character level | Example gold fee to restore 1,000 Honor |
| --- | --- |
| 30 | 5,540 |
| 50 | 22,202 |
| 80 | 142,474 |
| 90 | 258,208 |

The fees rise substantially, but I couldn't authenticate the formula.

We cannot infer whether the price is quadratic, exponential or determined by a lookup table from these four observations alone.

### Fame and rewards

The 2020 guide says characters at level 50 or higher could gain Honor through combat against opposing-faction players and through Great War participation.

It also describes Honor losses from dying to an opposing faction and attacking the same faction.

However, the same page contains an apparent inconsistency in its name-color explanations: its color legend doesn't align cleanly with the preceding text.

I would not implement a color-to-Honor rule based on that particular table without another corroborating source.

### Premium mitigation of XP loss

*Officially documented, Hero Plus 2020*

Certain premium consumables reduced XP loss when their holder died, without reducing the XP the opponent could receive from the kill.

This creates a monetization concern for your design.

A paying participant could face a smaller death penalty than a non-paying participant.

It doesn't necessarily increase their damage directly, but it changes the long-term cost of participating in risky PvP.

### Bounty revival in later Hero Plus

A 2020 Hero Plus announcement described bringing bounty-related features back, alongside a paid bounty/protection system intended to protect players from unwanted killing.

The announcement is evidence that the mechanics were revisited. It does not provide complete modern bounty fees, payout amounts or anti-abuse rules.

---

## 8. PvP combat mechanics and equipment interactions

### Intended role of player skill

A July 2006 GameSpy preview reports the developer's intention that player execution should matter alongside level and gear.

Its example was that a skilled level-50 character could defeat a less skilled level-60 character, while a level-70 opponent would generally be too powerful for the same level-50 player.

*Contemporary preview of design intention*

This does not establish measured win probabilities.

But it gives us a clear intended relationship:

**Character level + martial skill + equipment + player decisions all mattered.**

### Equipment and blacksmith

The old Netgame PvP guide doesn't provide a complete formula connecting enhanced equipment with PvP damage.

The official 2020 guide even uses vague wording about there being no specific PvP damage formula, which is insufficient to prove ordinary PvE damage calculations were reused unchanged.

I could not verify:

- Exact PvP damage reduction multipliers.
- Whether +8 weapons had PvP-specific scaling.
- Hit and dodge formulas against players.
- Critical-damage limits.
- PvP-specific skill cooldowns.
- Whether healing was reduced in PvP.
- Crowd-control diminishing returns.
- Pet damage multipliers against players.
- Equipment degradation after a PvP death.

### Pets and mounts

The June 2006 lead-designer interview described mounted pets absorbing damage until the pet was defeated. The developer framed that as relevant to both PvE and PvP tactics.

That creates a potential strategic choice: attack the mount, force a dismount or use different abilities.

However, exact pet-health targeting and mounted-combat behavior remain uncertain, as our previous pet research also found.

### Faction combat and professions

The documented classes suggest different potential contributions:

- **Warriors:** frontline damage and defensive buffs.
- **Physicians:** healing and cleansing.
- **Assassins:** stealth and evasive combat.
- **Hunters:** utility and companion-related abilities.

These roles are grounded in official class descriptions, but I did not recover reliable statistics showing how effective each profession was in faction wars.

**Inference:** Structured objective modes offer more room for class utility than a pure kill-count competition.

For your mobile game, that is an important reason to prioritize objective-based PvP.

---

## 9. PvP economy and social consequences

### What created value?

| PvP action | Economic effect | Historical evidence |
| --- | --- | --- |
| Posting a bounty | Player-funded reward plus registration fee | Japanese 2006 rules |
| Defeating bounty target | Transfers committed bounty to eligible hunter | Japanese 2006 rules |
| Restoring Honor | Gold paid to NPC | Hero Plus 2020 |
| Faction war participation | Reward chests and battle currency | Netgame war guide |
| Territory control | Access to rare crafting resources | June 2006 developer interview |
| Temple ownership | Experience-rate advantages | Later Netgame guide |
| Guild creation | Gold entry cost | Older Netgame guide |

### The potentially strongest economic loop

Territory control is the mechanic I find most interesting.

It can connect PvP directly to the blacksmith without simply handing out more gold for player kills.

For example, the winning faction might gain increased access to a particular enhancement material.

Players who prefer PvE could then buy those materials in the marketplace.

That would create a relationship between combat specialists, resource gatherers, blacksmiths and traders.

This is my proposed economic interpretation, not proof that original Hero Online markets behaved exactly that way.

### Major design risk: dominance snowball

If winning a faction battle grants exclusive access to increasingly powerful resources, the winning faction may become progressively harder to defeat.

That can create an unhealthy feedback loop:

Faction wins territory → faction gains scarce materials → faction members improve equipment → faction becomes more powerful → (repeat)

*A possible reinforcing cycle, not a measured historical outcome*

I would give winning factions a measurable economic advantage, but avoid exclusive control over materials essential for basic player progression.

### Bounty fraud

Player-funded bounty systems can also create collusion incentives.

Two players might cooperate to collect bounties rather than genuinely compete.

The original documented system does not provide sufficient information about how this was prevented.

For your game, bounty rules must be designed around the possibility of collusion from the outset.

---

## 10. Community feedback and usability

### Player opinion in July 2006

A contemporary MMORPG.com discussion contains opposing views.

One player rated PvP positively but complained that players generally needed to ask others to duel.

Another clarified that only consensual duel situations required a request, while entering dedicated PvP areas allowed other players to attack freely.

The second player also referenced karma penalties for attacking much weaker opponents.

*Community-reported, July 2006*

Another player gave PvP a favorable score but criticized other systems, including grinding, class identity and equipment drops.

This is a tiny, self-selected sample. It cannot establish average player satisfaction or a universal PvP preference.

### Later review observations

A Hero Plus overview lists dedicated PvP servers, duels and faction wars as prominent features, while criticizing repetitive gameplay and the dated interface.

Again, this is reviewer assessment rather than usage data.

### Original PvP UI

The old official Netgame guide documents four explicit PvP hotkeys:

| Function | Historical key |
| --- | --- |
| Bounty target mode | Left Ctrl + M |
| PvP self-defense | Left Ctrl + P |
| Guild vendetta attack | Left Ctrl + W |
| Guild vendetta defense | Left Ctrl + C |

This reveals a highly stateful interface: several kinds of PvP had different permission and penalty rules.

That may have been understandable to experienced PC players, but it's unnecessarily complicated for a touchscreen.

### UI mistakes to avoid

I would avoid exposing four separate combat-permission toggles on mobile.

Instead, the interface should automatically determine what combat rules apply based on:

- Current zone.
- Whether the player has accepted a duel.
- Event participation.
- Bounty eligibility.
- Whether the other player attacked first.
- Faction relationships.

The player should always be able to inspect why another character is hostile and what penalties might apply.

For example, if an attack would create criminal status, the game should warn the player before the attack, not merely punish them afterward.

---

## 11. Version comparison and unresolved evidence

| Feature | 2006 international evidence | Japan, Sept. 2006 | Hero Plus, 2020+ | The Hero Global |
| --- | --- | --- | --- | --- |
| Consensual duels | Confirmed | Not separately researched | Retained as game feature | Not independently verified |
| Open PvP region | Mudo Island | Specific PK areas | PK zones/channels | Not independently verified |
| Player bounty | Developer-confirmed | Detailed live rules | Revisited in 2020 | Unverified |
| Bounty fee | Unknown | 25% | Current fee unverified | Unverified |
| Alignment/karma | Community-described | Detailed penalties | Separate Honor guide | Unverified |
| Criminal settlement | Not verified for English launch | Documented | Current operation unverified | Unverified |
| Faction conflicts | Confirmed | Scheduled wars | Multiple battle formats | Great War documented |
| Territory-resource ownership | Developer-described | Not linked to same mechanism | Castle/mining privileges | Unverified |
| Honor restoration | Unknown | Separate alignment system | Documented | Unverified |
| PvP XP mitigation items | Unclear | Not established | Documented | Unverified |

### The Hero Global

The Hero Global website publishes a Great War guide with the same five-objective format, two level brackets, 10,000 starting points and Battle Coin rewards.

That provides evidence of a publicly documented faction-war ruleset.

However, I could not independently establish its current live status, complete bounty rules, version-specific karma coefficients or whether its data simply reproduces Netgame documentation.

### Hero Online Global

I could not recover a reliable, independently versioned PvP ruleset specifically attributable to a release named Hero Online Global.

Its full PvP rules remain unverified.

### Private servers

I did not find a private-server-specific PvP ruleset that could be authenticated well enough to compare damage, Honor, karma or bounty formulas against official versions.

Unverified emulator defaults or customized server rates should not be treated as original Hero Online mechanics.

### Most important research gaps

Exact historical figures remain unavailable for:

- Bounty tiers' minimum and maximum amounts.
- Original bounty payout frequency.
- Original karma-point thresholds.
- Good/evil alignment gain/loss formulas.
- Prison transfer probabilities.
- Complete player-kill XP rewards.
- PvP damage and mitigation formulas.
- Faction-war match durations across every mode.
- Actual territory production rates.
- Original guild-war declaration costs.
- Historical material-price response to faction wars.
- PvP participation and win-rate statistics.

The official 2023 PvP/Fame page exists, but its substantive guide content is image-based and was not fully recoverable as text. I therefore haven't treated it as a complete verified numerical source.

---

## 12. Implications for our design

We now have enough historical evidence to distinguish three design goals:

- **Combat competition:** Give players exciting opportunities to test character builds against one another.
- **Social conflict:** Give guilds and factions reasons to organize, compete and develop rivalries.
- **Economic stakes:** Give competitive activity meaningful consequences without undermining the PvE experience.

For your Unity-based mobile MMO-lite, I would not attempt to reproduce the full collection of Hero Plus battlegrounds, criminal settlements, flags, castle sieges and Honor systems at launch.

Instead, I would use a staged PvP roadmap.

### A. What to keep, change and avoid

| Original feature | Recommendation | Reason |
| --- | --- | --- |
| Consensual duels | Keep | Simple testing ground for combat |
| Dedicated PvP zones | Keep later | Clear opt-in risk |
| Two rival factions | Keep | Strong identity and social conflict |
| Faction battles | Keep | Collective endgame activity |
| Player-funded bounties | Keep later, redesigned | Distinctive but abuse-prone |
| Karma and criminal reputation | Simplify | Interesting consequences, complex implementation |
| Bad-karma NPC restrictions | Avoid initially | Could block essential progression |
| Prison/exile | Defer | Requires additional world and quest systems |
| Territory-resource rewards | Keep, limited | Connects PvP to blacksmith economy |
| Guild vendetta system | Defer | Needs mature guild/networking infrastructure |
| XP loss on PvP death | Avoid | Excessively punishing for mobile |
| Premium death protection | Avoid | Creates unequal costs of participation |
| Rewards for losing participants | Keep | Encourages match completion |
| Level-bracket matchmaking | Keep | Reduces power differences |
| Large-scale always-on warfare | Avoid initially | High concurrency requirement |

### B. Suggested PvP roadmap

- **Phase 1 — Duel Arena:** one-versus-one combat using existing player abilities and equipment. No items or XP lost.
- **Phase 2 — Faction Skirmish:** small team battle over capture objectives, with participation rewards and faction identity.
- **Phase 3 — Contested Resource Territory:** scheduled battles where the winner gains temporary access to bonus crafting resources.
- **Phase 4 — Bounty Contracts:** a player-funded hunting system with eligibility restrictions, fraud defenses and a dedicated rules UI.
- **Phase 5 — Reputation and Guild Rivalries:** more persistent consequences, criminal status and optional guild conflicts after player behavior has been observed.

The order matters.

There is little value in designing an elaborate bounty economy before we know whether the basic multiplayer combat feels responsive and balanced.

### C. Proposed first faction battle

I would begin with a compact five-versus-five, three-objective match, not a huge open-world siege.

**Three-Shrine Skirmish** — *original proposal. (The original had an illustrative top-down arena layout: three neutral capture points create multiple tactical decisions.)*

**Proposed starting rules**

| Parameter | Proposed value |
| --- | --- |
| Players | 5v5 |
| Match duration | 8 minutes |
| Entry level | 20 |
| Capture objectives | 3 |
| Time to capture neutral objective | 8 seconds |
| Capture scoring | 1 point/second per owned objective |
| Victory | Highest score at time limit |
| Early victory | First faction reaching 600 points |
| Respawn | 8 seconds |
| XP/item loss | None |
| Winner reward | 20 faction tokens |
| Loser reward | 10 faction tokens, subject to participation |

These are initial playtest values, not Hero Online statistics.

I would not initially award points for individual kills. Kills should help teams gain map control, not replace the match objective.

This gives tanks, support-oriented characters and mobile players with less precise aim something useful to do.

### Participation safeguards

A player should need meaningful activity to receive rewards: capturing objectives, defending teammates, dealing damage, healing or otherwise contributing.

The implementation should not rely solely on kills or total damage, because that can unfairly disadvantage support roles.

### D. Proposed bounty economy

Bounties have the greatest potential to create player stories, but also the greatest abuse risk.

I'd retain the core idea—players fund contracts against other players—but substantially change how they work.

**Suggested starting rules**

| Parameter | Proposed value |
| --- | --- |
| Bounty unlock | Level 30 |
| Minimum bounty | 2,000 gold |
| Maximum bounty | 50,000 gold |
| Posting fee | 10% of bounty |
| Contract duration | 48 hours |
| Payout condition | One qualifying defeat |
| Repeat payouts on same contract | Not allowed |
| Eligible environment | Dedicated bounty-enabled zone |
| Contract target | Player must be bounty-eligible through opt-in PvP or a defined criminal action |
| Same-account claims | Forbidden |
| Bounty target's ordinary gear | Never looted |
| XP loss on defeat | None |

Again, these are proposed values.

I would not permit arbitrary bounty posting to make a non-consenting PvE player vulnerable across the entire map.

That's something the original design experimented with, but it can become harassment when adapted to persistent mobile play.

**Financial example** (the original was an interactive bounty calculator)

| | Gold |
| --- | --- |
| Posted bounty | 10,000 |
| Posting fee (10%) | 1,000 |
| Total deposited | 11,000 |
| Hunter payout | 10,000 |

The bounty principal is held in escrow. The fee is removed from circulation. Expired contracts should refund unclaimed principal under an explicitly defined policy.

### Anti-collusion requirements

A simple same-account restriction is insufficient.

Players may control multiple accounts or cooperate with friends to trigger fraudulent payouts.

The eventual system should consider:

- Restrictions on repeated claimant-target pairings.
- Cooldowns after a qualifying defeat.
- Server-side combat participation validation.
- Suspicious payout-pattern detection.
- Limits on high-value contracts involving closely associated accounts.
- Clear rules for disconnected or surrendered targets.
- Escrow and atomic settlement.

No anti-collusion mechanism is perfect, so bounty rewards shouldn't become a primary unlimited source of newly created gold.

Ideally, the bounty principal is funded by other players, which makes payout a transfer rather than money creation.

### E. Resource-control rewards

This is where I would connect PvP to our Balanced blacksmith.

Rather than granting the winning faction exclusive control over essential enhancement stones, I'd give it temporary access to bonus resource opportunities.

For example:

| Reward | Proposed treatment |
| --- | --- |
| Rough stones | Available through ordinary PvE to everyone |
| Polished-stone materials | Slightly more accessible to winners |
| Rare crafting components | Additional contested resource nodes |
| Warding Charms | Not exclusively gated by PvP |
| Gold | Limited participation rewards |
| Cosmetic faction rewards | Primarily earned through faction participation |

This ensures PvP has economic consequences without making non-PvP players dependent on the winning faction for every upgrade.

The key balancing metric would be how much faster a winning faction can obtain valuable materials than a nonparticipant—not merely the number of resource nodes.

I would initially test a 15–25% bonus to eligible gathering yield for a limited ownership window, rather than complete resource exclusivity.

That percentage is a design proposal, not a researched historical value.

### F. PvP and equipment power

Before implementing competitive matchmaking, we need to establish how much of a player's combat power comes from gear enhancement versus level and martial skill.

Your current blacksmith can provide up to +8 enhancement and a potentially substantial stat increase.

That creates a risk: a player with highly enhanced equipment might overwhelm an equally leveled player who hasn't succeeded in forging.

I would test three approaches:

| Model | Equipment treatment | Intended use |
| --- | --- | --- |
| Full-power PvP | All equipment bonuses apply | High-stakes open-world conflict |
| Soft normalization | Gear matters, but extreme differences are compressed | Standard faction battles |
| Equalized PvP | Equipment and base stats are standardized | Skill-focused competitive duels |

I recommend soft normalization for your first faction mode.

It preserves the satisfaction of improving gear while giving players with less successful enhancement histories a chance to contribute.

The exact normalization formula should be decided only after the existing Unity combat architecture has been inspected.

---

## 13. Unity architecture considerations

This research highlights a risk of overengineering.

The game doesn't need separate combat implementations for duels, faction skirmishes and bounty encounters.

It needs one combat engine with explicit rules about who may damage whom and what happens afterward.

A sensible separation would be:

1. **Existing Combat System** — attacks, skills, damage, defense and death.
2. **PvP Eligibility Rules** — zone, duel, faction, bounty and protected-player checks.
3. **Match or Encounter Resolution** — victory, defeat, ownership, karma and result attribution.
4. **Rewards and Persistence** — gold, tokens, bounty escrow, reputation and saved state.

The architecture assessment should establish whether the prototype already supports networked authoritative combat. If not, build the first duel as a local test of combat balancing or a controlled multiplayer prototype, but don't implement a player-funded bounty economy that relies on client-controlled results.

### Specific implementation safeguards

- The server must determine whether an attack is legal before damage is applied.
- Every combat outcome must be attributed consistently.
- Bounty and faction rewards must be processed once.
- Disconnects must not duplicate payouts.
- Respawns must preserve equipment and inventory correctly.
- Death in a duel must not trigger ordinary PvE death penalties.
- The UI must show faction, target hostility and PvP state clearly.
- A player should not accidentally become criminal by tapping the wrong character on a mobile screen.

I would require Claude Code to produce an architecture assessment before proposing any production implementation.

---

## 14. Final conclusions

The original 2006 design had three strengths that I'd preserve.

**PvP existed at several risk levels.** Players could duel, enter dedicated PvP territories, fight in faction wars or pursue wanted characters. That gave different types of competitive players a place in the same game.

**Bounty hunting made personal rivalries economically meaningful.** Players could commit their own gold to a contract, and other players had a financial incentive to resolve the conflict. The 2006 Japanese rules confirm both the bounty and its 25% posting fee.

**Faction warfare connected combat to the wider world.** The original developer envisioned contested mines producing valuable crafting resources, while later Hero Plus expanded warfare through territorial objectives, currency rewards and progression bonuses.

The features I would avoid copying unchanged are punitive XP losses for outmatched victims, paid protection from death penalties, unrestricted player targeting through arbitrary bounties and permanent resource monopolies.

### Research quality and limitations

The strongest historical findings come from two August/September 2006 Japanese reports containing publisher-supplied rules, a June 2006 developer interview and official Netgame warfare guides.

The main unresolved gaps are numerical: complete karma formulas, full bounty payout schedules, PvP damage scaling, actual participation rates and historical market effects. I also couldn't verify separate complete mechanics for Hero Online Global or authenticated private-server variants.

The older Netgame guides establish published mechanics but are not reliable evidence that every listed war mode existed at launch.

### Primary source library

| Source | Most useful evidence |
| --- | --- |
| August 2006 developer Q&A | Duels, Mudo Island, bounty targets, optional PvP and faction wars |
| June 2006 lead-designer interview | Great Mine, six-hour control battles and faction mining |
| August 2006 Japanese bounty and karma announcement | Early bounty structure, 25% fee, criminal penalties and proposed regional rules |
| September 2006 Japanese implementation | Live-update bounty eligibility, defensive PK, karma and peaceful-channel rules |
| Official Netgame PvP interface guide | Bounty, self-defense and guild-war controls |
| Older official Netgame system overview | Houses, faction affiliation, PK penalties and channel rules |
| Official Netgame warfare guide | Flag Kingdom, Faction War, Great War, Diamond Castle and Golden Castle |
| August 2020 Honor Points guide | Level-difference penalties, restoration fees, ranking and XP-loss mitigation |
| November 2023 PvP/Fame guide | Later official guide, with image-based content not fully recovered |
| The Hero Global Great War guide | Published objective and reward rules |
| July 2006 player discussion | Community feedback about duels, karma and PvP experience |
| July 2006 GameSpy preview | Intended relationship between level, gear and skill |

My strongest recommendation is to treat faction warfare and bounty hunting as separate design projects. Build small-scale faction combat first, then use that validated combat and reward infrastructure to support bounties later. It captures the competitive identity of Hero Online without making your early Unity development dependent on an entire open-world criminal justice system.
