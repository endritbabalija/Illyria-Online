# Hero Online: Guilds, Parties & Social Systems

Historical research report · 2006 Hero Online, later Hero Plus, international documentation · 24 October 2026

> The source links and the interactive figures (the mentorship hierarchy in §8, the social progression in §15 and the party-XP calculator in §16) didn't come through when this was pasted in; their text is kept below.

## Executive summary

Hero Online's most interesting social design wasn't simply allowing players to create guilds. It connected parties, guild membership, master–disciple relationships, special skills, resource control and shared progression.

The original game called guilds **Houses**. A House wasn't just a list of members: it had a visible identity, leadership, a faction affiliation and access to forms of cooperation that individual players lacked. Later documentation adds shared storage, mentorship bonuses, party synergy and competitive guild rewards.

Five findings stand out:

- **Parties supported six players**, with configurable XP distribution and four loot-allocation methods. The official guide also documents a 20-level restriction on ordinary party benefits.
- **Creating a House required level 40 and 10 million gold** in the older Netgame guide. Houses were faction-specific, and their names and emblems appeared above members' characters.
- **The master–disciple system added a social progression layer.** Guildmates could train together, receive proximity-based benefits and establish a Grand Master when the House reached the required size and leadership level.
- **House storage was a significant economic institution.** The guide documents monthly fees ranging from 50 million to 100 million gold, with 120–240 storage slots and restricted withdrawal permissions.
- **Later Hero Plus rewarded guild competition with lasting recognition**, including a monument honoring top guilds and a continuing 2026 guild-war schedule.

The main caution is chronology: contemporary 2006 reviews establish that parties and Houses existed, but the older official Netgame guides are undated. Their detailed mentorship, storage and synergy systems cannot all be confidently attributed to the original 2006 launch.

---

## 1. Historical mechanics summary

| Mechanic | Documented rule | Version / confidence |
| --- | --- | --- |
| Party capacity | 6 players | Official Netgame guide, undated |
| Party XP | Individual or equal-sharing mode | Official Netgame guide |
| Party loot | Individual, round-robin, random, master looter | Official Netgame guide |
| Level-gap restriction | Ordinary party benefits restricted when gap exceeds 20 levels | Official Netgame guide |
| Party creation in war zones | Restricted | Official Netgame guide |
| House creation level | 4 Dan / level 40 | Official older guide |
| House founding fee | 10,000,000 gold | Official older guide |
| House faction | Faction-specific membership | Official older guide |
| House appearance | Name and emblem displayed above character | Official older guide |
| Guild leadership | House Master controls membership | Official older guide |
| House storage | 120, 180 or 240 slots by rank | Official older guide |
| Storage lease | 50M, 80M or 100M gold/month | Official older guide |
| Storage withdrawals | Master or vice-master | Official older guide |
| Master–disciple | Shared-house relationship with training benefits | Official guide, undated |
| Disciples per master | Up to 6 in client guide | Official guide, undated |
| Grand Master | 15+ members and level-80+ leader | Official guide, undated |
| Party Sync Effect | Composition-based random bonuses | Official guide, undated |
| Public, faction and guild chat | Separate channels and shortcuts | Official client guide |
| Guild-war ranking monument | Top 5 guilds, first half of 2025 | Official update, July 2025 |
| Exact 2006 XP sharing formula | Unknown | Not verified |
| Original guild member cap | Unknown | Not verified |
| Guild-war rewards in June 2006 | Unknown | Not verified |

---

## 2. The original party system

### Contemporary 2006 evidence

A July 2006 MMORPG.com hands-on preview describes parties forming through invitations, sometimes with little conversation. Players often joined a group, fought together and left when finished.

The same review describes Houses emerging as the game's community organized through forums and open chat.

*Documented firsthand experience, July 2006*

This confirms that the basic social systems were operational during open beta.

A contemporary player in another July 2006 discussion confirmed a six-member limit and described XP distribution across party members.

*Community-reported, July 2006*

### Official party creation workflow

The surviving Netgame guide describes this interaction:

1. Select another character while holding Ctrl.
2. Request a party.
3. The recipient accepts or declines.
4. The party leader chooses the XP and loot rules.
5. Members appear in a party panel and on the minimap.

This was a relatively simple group-formation system by PC MMORPG standards.

The guide does not provide evidence of an automatic dungeon finder, queue-based matchmaking or persistent cross-server party finder.

### XP distribution

There were two modes:

| Mode | Officially described behavior |
| --- | --- |
| Individual | Members gain experience individually |
| Equal | Total experience is divided equally |

Crucially, the documentation does not provide the exact formula for calculating the total XP pool.

We cannot assume equal mode simply divides the solo XP reward by six, or that individual mode guarantees every member their normal solo XP.

A 2006 forum participant claimed that larger parties reduced each member's XP per kill, but that coordinated kills could compensate. They considered three-player farming groups especially practical.

That observation supports a gameplay tradeoff, not a verified equation.

### Party level-gap restriction

*Officially documented*

The party guide states that ordinary party benefits did not apply when members differed by more than 20 levels. It suggests the master–disciple system as an alternative for characters separated by larger level gaps.

This is a clever distinction: normal parties encouraged roughly comparable progression, while mentorship offered a different relationship for stronger and weaker characters.

But the source does not specify whether the restriction was calculated against party leader level, highest versus lowest member level, or some other internal criterion.

---

## 3. Party loot distribution

This system is especially relevant to our previous boss research.

The official guide lists four selectable loot modes.

| Mode | Behavior | Potential consequence |
| --- | --- | --- |
| Individual | Individual looting | Ownership implementation unclear |
| Round Robin | Players take turns | Predictable allocation order |
| Random | Loot permission randomly assigned | More variance |
| Master Looter | Leader collects all loot | Centralized control |

*Documented*

A separate passage says the default item-acquisition setting was free-for-all and advises leaders to change it if another distribution is desired.

This terminology is slightly inconsistent with the four named options, and it is not clear whether free-for-all was simply the default behavior of individual looting.

### Important trust problem

Giving a party leader complete control over rare loot is socially meaningful but can also create disputes.

The original documentation establishes the option, but it does not establish:

- Whether all party members saw the loot mode before joining.
- Whether changing the mode required notification.
- Whether boss loot used identical rules.
- Whether a leader could change loot ownership after a boss was engaged.
- Whether distribution disputes had an in-game resolution mechanism.

For your game, I would make loot mode changes visible and prohibit changes during active boss encounters.

For ordinary farming, personal loot would be substantially simpler.

---

## 4. Did party play actually improve progression?

The official guide argues that parties allow faster monster hunting and therefore faster experience gain.

But contemporary players disagreed about whether grouping was worthwhile.

A positive 2006 review argued that three-player parties could cover a farming area efficiently. Another player in the same discussion felt parties were largely unnecessary and could attract members who contributed little.

### A useful firsthand example

In July 2006, one player described power-leveling a friend's new character while fighting significantly stronger monsters.

They claimed they could bring the friend from level 1 to level 10 in about five minutes. The friend initially received much less XP, with the balance changing as levels increased.

*Community anecdote, not a verified XP formula*

The example suggests there was meaningful interaction between player level and shared XP rewards.

It also suggests parties could help friends catch up, which is useful for social retention.

However, it is not evidence that every class or server could reliably level a character from 1 to 10 in five minutes.

### Player feedback was sharply divided

A 2006 reviewer rated parties highly, while another rated them poorly and considered grouping unnecessary.

**Inference:** The value of grouping depended on practical factors such as monster density, party coordination, character level and whether players had friends to group with.

This is one area where nostalgia alone is not a reliable guide.

---

## 5. Houses: Hero Online's guild system

A House was a persistent social organization, distinct from a temporary party.

The official guide describes faction-specific Houses with leadership, membership management, visible emblems and their own communication channel.

### Creating a House

*Official Netgame guide, undated*

The documented requirements were:

| Requirement | Value |
| --- | --- |
| Character level | 40 (4 Dan) |
| Founding fee | 10,000,000 gold |
| NPC | House Manager in Dragon Castle |
| Faction | Selected during creation |
| Visible identity | House name, emblem and precept |
| Initial leadership | House Master |

The founder could choose a faction, emblem, introduction and notice. The historical interface specified a small emblem image size of 16 pixels.

### Guild membership and factions

A character affiliated with one faction could not join a House belonging to the opposing faction. The House Master could recruit and remove members.

This made guild choice more consequential than simply selecting a group-chat channel.

**Inference:** House membership could influence PvP alliances and access to faction-organized content.

The guide does not establish that every guild member was required to participate in PvP.

### Joining and leaving

The documented method involved selecting another player and issuing a House invitation. The invited player could accept it.

Members could leave through the House information panel.

I could not verify a universal minimum account age, membership cooldown or restriction on joining a different House after leaving.

### Guild leadership and permissions

The House Master had authority to recruit, expel and manage members. The extended House-management interface also allowed editing guild descriptions, visual identity and webpage-style sections, with separate permissions for reading, commenting, editing and deleting content.

This is a distinctive historical feature.

Hero Online treated the House partly as a persistent community space, not just a combat roster.

Guild members could have an identity and organized information outside the immediate party interface.

### One source inconsistency

The general client guide describes Houses consisting of two or more players, while the founding guide emphasizes one eligible player creating the organization through an NPC.

Those statements may describe creation versus ongoing membership requirements.

I couldn't confirm whether the game required a second member immediately or whether a newly founded House could temporarily contain just its creator.

---

## 6. House progression, fame and social prestige

The older official overview says Houses could build fame and that higher-fame Houses could receive benefits.

*Official guide, undated*

However, the exact original House-fame formula was not recovered.

I could not authenticate the number of fame points awarded for recruiting members, winning wars, defeating players or participating in other activities.

### House ranks

The storage guide identifies three institutional ranks:

- Family
- Clan
- House

These ranks were associated with different storage capacities and costs.

But it does not provide the precise numerical promotion thresholds between all three ranks.

Do not confuse these guild-organization ranks with the character's Dan/Kyu levels or master–disciple ranks.

### Visible prestige

Guild names and emblems appeared above characters, making affiliation noticeable in ordinary gameplay.

This is something I'd preserve.

A recognizable guild identity visible in town, during parties and at world bosses can make the world feel socially connected without requiring elaborate mechanics.

---

## 7. Shared House storage and its economy

This is one of the most concrete systems in the entire report.

*Official Netgame guide, undated*

The storage guide required an eligible House to have more than 15 members and meet a rank requirement. It charged a recurring monthly rental fee.

### Exact historical values

| House rank | Shared storage slots | Monthly lease |
| --- | --- | --- |
| Family | 120 | 50,000,000 gold |
| Clan | 180 | 80,000,000 gold |
| House | 240 | 100,000,000 gold |

### Permissions

All guild members could deposit items, but only the House Master and vice-master could withdraw them.

The guide also documents:

- A monthly lease renewal.
- A warning delivered ten days before the lease expired.
- Changes in rank-based benefits taking effect upon renewal.
- Storage becoming unavailable without a valid lease.
- Stored items being permanently lost if the House disbanded during its lease.

### Why this mattered economically

The rental fee was an ongoing gold sink. The guild effectively had a recurring operational expense.

A sufficiently organized guild would need to manage shared resources, authority and storage access.

However, the risk of losing every stored item when a guild disbanded is particularly severe.

For your game, I would not reproduce that behavior.

Items donated to a guild treasury can be valuable, and accidental or malicious disbanding shouldn't permanently erase them without a well-designed recovery policy.

### Historical uncertainties

I could not determine:

- Whether withdrawn items retained their original owner attribution.
- Whether guild storage accepted gold as well as items.
- Whether withdrawal limits could be configured.
- Whether there were public audit logs.
- Whether a vice-master could appoint other authorized officers.
- Whether unpaid rent erased items or only prevented access.
- The complete rank progression requirements.

Those details matter for a modern implementation.

---

## 8. Master–disciple relationships

This is arguably the most distinctive social mechanic worth studying.

Rather than treating a guild as one large anonymous group, Hero Online included closer training relationships.

The official guide says master–disciple relationships occurred between members of the same House. When master and disciple remained near one another, the disciple received hunting-related bonuses.

### Hierarchy

- **Grand Master** — House leader, once membership and level requirements are satisfied.
- **Master** — mentors one or more disciples.
- **Disciple** — receives proximity and training-related benefits.

### Exact requirements

The House guide defines Grand Master eligibility as:

- At least 15 House members.
- House leader at 8 Dan or above, corresponding to approximately level 80.

It also states that qualifying guild members receive a Divine Skill.

The general client guide describes a relationship containing one master and up to six disciples.

*Official documentation, undated*

The guides aren't sufficient to establish whether this full system was already operational in July 2006.

### Training behavior

Training resembled party creation, with a separate UI.

The master could initiate training and choose between random and ordered item acquisition. Leaving range disabled the corresponding proximity bonus.

The general client guide also describes masters teaching skills and experience-related benefits to disciples, but its wording explicitly warns of a forthcoming update.

Therefore, I would treat the full teaching-and-experience-transfer description as documented intended behavior, not proof that every feature operated exactly as written in all versions.

### Why this is a strong design idea

*Inference*

Mentorship solves a real social problem in persistent MMORPGs: established players and beginners are often too far apart in level to benefit from ordinary grouping.

By giving a veteran a reason to train with a newcomer, the game can turn progression inequality into a social opportunity.

But mentorship must reward genuine participation, not simply having high-level players carry inactive alternate accounts.

---

## 9. Party Sync Effect: rewarding group composition

The official guide describes a system called Sync Effect, providing extra benefits when players formed suitable parties.

*Official, undated*

### How it worked

- Characters received an individual bonus when connecting.
- Parties with at least two players could activate synergy effects.
- Parties of three or more had a chance to activate a stronger effect combining members' bonuses.
- Similar character levels improved the Sync Point rating.
- Members of the same House, or master–disciple partners, could potentially trigger the stronger effect with only two players.
- Bonuses could change when new party members joined.

One small but interesting number survives: disconnecting didn't immediately clear the assigned individual effect. The guide describes a roughly one-to-two-minute retention period before a new effect could be acquired.

### What is missing?

I couldn't recover a complete numerical table for Sync Point calculation, buff probabilities or bonus magnitudes.

The original guide provides qualitative relationships, not enough data to reproduce its internal algorithm.

### Why I find this mechanic interesting

Ordinary parties reward having more players.

The Sync system could reward who those players were: similar-level characters, guildmates and mentor–disciple partners.

That is a much more social form of progression.

However, I would avoid reproducing its random buff assignment unchanged. Players may repeatedly reform parties or reconnect to obtain an optimal effect if the reward system makes that worthwhile.

For a mobile game, predictable group bonuses tied to cooperation may work better than repeatedly rerolling temporary synergy.

---

## 10. Chat, friendships, messaging and social identity

Hero Online also had unusually varied communication features.

The official client guide lists distinct channels for normal chat, party, faction, House, whisper and server-wide shout.

| Channel | Original command prefix | Purpose |
| --- | --- | --- |
| Normal | % | Local/ordinary chat |
| Party | @ | Group communication |
| House | $ | Guild communication |
| Faction | ~ | Faction messages |
| Whisper | ! | Private messages |
| Shout | # | Wider announcements |

The game also included a Hero Messenger friend interface, and the client documentation refers to pigeon-based messages and notifications.

### The server-wide shout system

A July 2006 reviewer noted that public server-wide communication consumed Chi, limiting casual message spam. They also described players from different countries communicating in a shared chat channel.

*Contemporary firsthand observation*

This is an unusual design: a combat resource acted as a cost for broadcasting messages.

Later official interface documentation describes a purchasable tavern shout service.

The sources don't establish that every server used the same communication cost at every date.

### Social minigames

The Netgame client guide also documents the ability to challenge another player to Omok, a five-stone board game.

This shows that social interaction was not restricted to combat, trading and chat.

**Inference:** Small shared activities can help make towns feel inhabited. Players need things to do together besides maximizing combat efficiency.

For your mobile MMO-lite, a simple social activity could ultimately be more useful than another complex guild-stat upgrade.

---

## 11. Guild wars, territorial control and prestige

We covered the combat mechanics in our PvP research, so this section concentrates on their social effects.

Houses were connected to the game's opposing factions, allowing members to cooperate in organized warfare. The official introduction also says Houses could gain fame and benefits through their standing.

### Later Hero Plus: Diamond Castle

An official warfare guide documents a territory-control system with five temples. Holding temples provided experience bonuses, beginning at 20% for the first and increasing by 10 percentage points for successive controlled temples.

*Official, undated warfare guide*

This created a direct relationship between collective competition and character progression.

### 2024: dissatisfaction with concentrated rewards

The February 2024 Hero Plus roadmap explicitly discussed restructuring Diamond Castle rewards so more guilds could benefit, rather than concentrating rewards among a small number of clans.

That is particularly valuable developer evidence.

It doesn't prove how many guilds were participating, but it does establish that reward concentration was a recognized issue.

### 2025: guild monuments

In July 2025, Netgame announced a monument honoring the five highest-ranked guilds from January–June 2025 based on Frozen Guild War points.

This created a non-economic prestige reward.

It's a strong idea to consider for your own game: permanent or season-long recognition for coordinated success without simply giving the winners ever more powerful equipment.

### 2026: continuing guild wars

Official monthly results in May and June 2026 record different daily winning guilds, as well as rotating event coins that expired after a defined maintenance period.

A January 2026 roadmap also listed further guild-content development and additional guild rewards.

**Version warning:** Those notices establish 2026 content and plans, not the original 2006 guild-war reward structure.

---

## 12. Economic and social consequences

The historical guild system connected social organization to several types of economic value.

| Mechanic | Economic function | Evidence |
| --- | --- | --- |
| House founding | One-time NPC gold sink | 10M gold, official guide |
| Shared storage lease | Recurring gold sink | 50M–100M gold/month, official guide |
| Shared item storage | Resource pooling and redistribution | Official guide |
| Guild-organized farming | Potential higher farming efficiency | Official party guide |
| Mentor training | Progression benefits for linked players | Official guide |
| Castle control | XP/resource advantage | Official warfare guide |
| Guild-war prizes | Reward issuance and temporary currencies | Official Hero Plus announcements |
| Prestige monuments | Social recognition rather than direct combat power | July 2025 update |

### Guilds and the blacksmith

*Design inference*

In your game, guilds could become natural organizers of equipment progression.

A farmer collects Rough stones, another player specializes in boss hunting, and a third sells upgraded equipment.

Members could coordinate what materials they need through guild requests.

But a shared treasury should not be required for ordinary +1 to +8 progression. Solo players should still be able to access the Balanced blacksmith.

### Guilds and the marketplace

*Design inference*

Guildmates can trade directly, pool resources and help new members obtain equipment.

However, market liquidity becomes problematic if the richest guild controls scarce resource locations and trades most of its production internally.

The 2024 developer concern about reward concentration makes it worth testing how guild advantages affect the broader economy.

### Guild storage risk

The historical ability to lose all stored items upon disbanding is something I would deliberately reject.

For a modern multiplayer game, treasury access needs audit logs, ownership records, role-based withdrawal limits and a safe closure process.

---

## 13. What players liked and disliked

This is one of the better areas for contemporary player testimony, although the sample is still small.

### Positive feedback

A July 2006 community reviewer rated both the party and guild systems positively, praising group farming and describing the potential for a strong social community.

A 2009 reviewer described an active international community and said that party customization and loot sharing worked reasonably well, despite not being especially elaborate.

### Negative feedback

Another player in the July 2006 discussion said ordinary parties were often unnecessary and that some members contributed little.

A different player complained about difficulty finding random groups because they did not have an established guild and struggled to find players speaking the same language.

A contemporary hands-on reviewer also observed parties forming and disbanding with very little conversation.

### What this actually means

The sources don't justify saying either that Hero Online had excellent social systems or that grouping was fundamentally broken.

They suggest something more nuanced:

**The mechanics allowed cooperation, but they didn't always give strangers enough reason—or enough convenience—to cooperate meaningfully.**

For a mobile MMO-lite, this is probably the biggest lesson.

Simply adding a Party button and Guild menu does not create social gameplay.

### Research gaps in community reaction

I couldn't verify a representative set of Korean player discussions, original-version YouTube recordings with clear social-system rules, or Reddit threads sufficiently specific to quantify satisfaction.

No reliable participation percentages, guild retention rates or party-size distributions were recovered.

---

## 14. Version differences and unresolved documentation

| Feature | 2006 contemporary evidence | Undated Netgame guide | Hero Plus 2024–26 |
| --- | --- | --- | --- |
| Party invitations | Confirmed | Detailed UI workflow | No verified removal |
| Six-player limit | Player-reported | Officially documented | Current cap not reverified |
| XP sharing | Player-reported | Individual/equal options | Exact current formula unknown |
| Houses | Confirmed | Detailed creation and management | Guilds remain active |
| Faction-specific Houses | Not conclusively established from launch reporting | Confirmed | Later faction structure retained |
| House storage | Not verified at launch | Detailed lease and withdrawal rules | Present-day rules not reverified |
| Master–disciple | Not verified at launch | Detailed system | Current bonuses not reverified |
| Sync Effects | Not verified at launch | Detailed system | Current bonus table unknown |
| Guild-war rewards | Broad warfare concept | Territory rewards | Seasonal rankings and currencies |
| Guild monuments | Not established | Not found | Officially documented, 2025 |

### Hero Online Global and The Hero Global

I could not verify a separate, dated guild/party manual establishing distinct numerical rules for either named release.

An international website reproducing Netgame-style documentation would not by itself establish a unique server implementation.

### Private servers

I could not independently authenticate a private-server guild ruleset with complete membership caps, reward multipliers, party XP formulas or modified mentorship statistics.

Custom emulator configurations may exist, but they should not be confused with the original game.

### Missing formulas and values

The following remain genuinely unverified:

- Complete 2006 party XP formula.
- XP multiplier by party size.
- Exact individual-loot ownership formula.
- Guild maximum membership.
- Guild-fame gain and loss formulas.
- Complete guild promotion thresholds.
- Exact master–disciple XP transfer formula.
- Sync Point equation and buff percentages.
- Guild skill damage/stat bonuses at each level.
- Original guild-war rewards.
- House-disbandment and leadership succession edge cases.
- Original guild storage transaction limits.
- Social-system-specific monetization prices.

---

## 15. Implications for our design

For your original mobile MMO-lite, I would prioritize small-group cooperation and a persistent sense of belonging, rather than build a fully featured guild institution immediately.

The social structure should connect naturally to the systems we've already researched:

1. **Players form parties** — quest together, farm monsters and defeat regional bosses.
2. **Players build relationships** — friends, mentors and regular teammates.
3. **Persistent guild identity** — emblem, guild chat, shared objectives and prestige.
4. **Organized group activity** — world bosses, faction events and resource competition.

### What I would keep, modify or avoid

| Hero Online feature | Recommendation | Reason |
| --- | --- | --- |
| Six-person parties | Start with 4 | Better suited to a small initial population |
| Party XP sharing | Keep, simplify | Makes cooperation worthwhile |
| Four loot modes | Use personal loot initially | Fewer disputes and UI steps |
| Visible guild names/emblems | Keep | Strong social identity |
| Guild founding cost | Keep a modest fee | Creates a small economic commitment |
| Faction-linked guilds | Consider later | Should follow faction design |
| Master–disciple relationships | Keep later | Good way to help new players |
| Party Sync bonuses | Adapt | Reward real cooperation, not random rerolls |
| Shared guild storage | Defer | Security and item-ownership complexity |
| Guild territorial objectives | Keep later | Builds on PvP system |
| Guild prestige recognition | Keep | Status without runaway power |
| Paid or punitive guild upkeep | Avoid | Can discourage small communities |

---

## 16. Proposed party mechanics v0.1

These numbers are original starting proposals, not historical Hero Online mechanics.

| Parameter | Proposed value |
| --- | --- |
| Party size | 4 |
| Party invitation | Direct invitation plus simple nearby-player discovery |
| XP eligibility | Within encounter participation range |
| Shared XP | Equal shares of configurable party reward |
| Initial cooperation bonus | +10% total XP for 2+ participating players |
| Party level difference | No hard 20-level exclusion; use level-scaled rewards |
| Ordinary loot | Personal |
| Regional boss loot | Personal contribution-qualified |
| Party leader | Can invite, remove, mark targets and disband |
| Party rejoin | Allowed after disconnect if slot remains |
| AFK rewards | No eligibility without meaningful participation |

The +10% bonus is only a test parameter.

Party XP must be calculated carefully so that forming four inactive alternate characters doesn't become the optimal progression strategy.

### Example party-XP calculator

**Party XP prototype** — *proposal; the original was interactive (1–4 players). Its example state:*

| | |
| --- | --- |
| Participating players | 3 |
| Base encounter XP | 100 |
| Total party XP | 110 |
| XP per player | 36.7 |

Illustrates an equally divided total XP pool with a 10% group bonus. It assumes one encounter of fixed reward value and does not model faster group kill rates, level scaling or individual contribution.

One important consideration: equal splitting alone can make grouping less rewarding. In actual play, the faster encounter completion rate and access to harder enemies must offset the smaller XP share.

We should simulate XP per minute, not merely XP per kill.

---

## 17. Guilds: recommended first version

I would keep your game's early guild implementation deliberately focused.

### Proposed starting configuration

| Rule | Starting proposal |
| --- | --- |
| Guild creation level | 20 |
| Creation cost | 5,000 gold |
| Maximum members | 30 |
| Guild identity | Name, emblem, short description |
| Roles | Leader, Officer, Member |
| Guild chat | Yes |
| Guild invitations | Yes |
| Guild roster | Yes |
| Guild activity log | Basic |
| Guild storage | Not initially |
| Guild-level combat buffs | Not initially |
| Guild shared objectives | One weekly objective initially |
| Guild prestige | Seasonal rankings and cosmetic recognition |

The 5,000-gold founding fee needs adjustment against the real economy, especially since our earlier example assumed 4,000 gold earned daily.

I would not include large monthly upkeep costs at launch.

### First weekly objective

One example could be a guild-wide regional-boss challenge.

Members collectively earn points through eligible boss encounters. Reaching milestones grants modest individual rewards and guild recognition.

This could connect the guild system to content that already exists, instead of requiring an entirely new game mode.

Rewards should be participation-qualified, not simply issued to every account in a large guild.

---

## 18. Mentor–disciple: the social mechanic I'd save for a second phase

The original mentorship system is worth adopting conceptually because it gives veteran players a reason to support beginners.

I would test a simple structure:

- One mentor can support up to two active disciples.
- A disciple must be below a configurable progression threshold.
- Both earn small rewards for completing meaningful content together.
- The disciple receives an XP catch-up bonus.
- The mentor receives non-dominant progression or prestige rewards.
- Both can end the relationship without losing previously earned character skills.
- The system rejects repeated farming of new alternate accounts where feasible.

Unlike the historical rule, I would not remove accumulated XP because a mentor expels a disciple.

The goal is to encourage genuine help, not make beginners economically dependent on one powerful player.

---

## 19. Connecting social systems to the economy

The most promising integration isn't a complex guild bank.

It's coordinated goals that create reasons to use existing systems.

For example:

- Four players party to defeat a regional boss.
- They receive personal materials and equipment drops.
- One player sells spare stones in the marketplace.
- Another uses their materials in the Balanced blacksmith.
- Guildmates organize a faction event and earn recognition.

This creates social meaning from gameplay systems we already intend to build.

It is much safer than adding an unrestricted shared warehouse or guild-controlled equipment distribution before the backend supports secure ownership transfers.

---

## 20. Unity architecture and implementation order

Before Claude implements parties or guilds, it must determine whether your existing prototype supports authoritative multiplayer sessions.

A party system is not just a UI panel. It requires correct ownership and synchronization of player presence, invitations, party membership and encounter rewards.

### Suggested first vertical slice

1. Two test players can create and join a party.
2. The party roster and player positions update correctly.
3. Both participate in an ordinary enemy encounter.
4. The system calculates XP and personal loot correctly.
5. A player can disconnect, rejoin or leave without corrupting the party.
6. The party completes a regional-boss encounter with correct reward ownership.

Only then would I introduce guild creation, roles, chat and shared objectives.

The design should reuse the existing Unity character, combat, loot and progression systems.

Do not create an independent XP engine for parties.

### Essential safeguards

Every party and guild membership change needs authoritative validation. Every reward must be idempotent. Guild officers shouldn't be able to duplicate or erase item instances through concurrent operations. Chat needs reporting, blocking and moderation controls before broad public deployment.

These requirements are recommendations for your game, not descriptions of Hero Online's original server implementation.

---

## 21. Primary research sources

| Source | Most useful evidence |
| --- | --- |
| July 2006 firsthand preview | Original parties, international chat and emerging Houses |
| July 2006 community debate | Six-player parties, grouping efficiency, praise and complaints |
| July 2006 player discussion | Power-leveling, group finding and language barriers |
| Official Party System guide | XP modes, four loot modes, party limit, restrictions and UI |
| Official Master–Disciple party guide | Training relationships, proximity bonuses and ranks |
| Official Sync Effect guide | Party-composition bonuses and Sync Point rules |
| Official House guide | Creation level, cost, factions, management and emblems |
| Official House-management guide | Guild identity, announcements and community permissions |
| Official House Storage guide | Slots, monthly leases, withdrawal permissions and risks |
| Official Master–Disciple House guide | Master ranks and training benefits |
| Official social and interface guide | Chat commands, friends, communication and social actions |
| February 2024 roadmap | Developer concern about concentrated guild rewards |
| July 2025 guild monument announcement | Guild ranking and persistent recognition |
| June 2026 guild-war winners | Continuing guild competition and expiring event rewards |
| January 2026 roadmap | Further proposed guild content and rewards |

---

## Final assessment

The strongest part of Hero Online's social design was its progression from temporary cooperation to persistent relationships.

Parties handled immediate combat. Houses created a lasting identity. Master–disciple relationships helped bridge differences in character progression. Sync Effects gave cooperative play additional incentives. Faction and guild warfare connected those groups to the wider economy.

But the historical feedback also demonstrates weaknesses: grouping could feel unnecessary, finding compatible teammates was difficult, and some guild systems were expensive or punitive.

For your game, I'd start with **four-person parties, personal loot, simple guilds and one meaningful shared objective.**

Then add mentorship and territorial competition when the underlying multiplayer systems are reliable.

That will preserve the feeling of belonging to a martial community without forcing the first version of your Unity MMO-lite to support an entire old-school guild bureaucracy.
