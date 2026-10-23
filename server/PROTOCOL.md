# Server protocol (M4)

How the client talks to the Nakama module in `server/src/`. The server decides every hit, drop
and item change; the client sends what the player wants and plays animations right away, then
follows the server's results. Positions use Unity axes (Y up, metres). Op codes and error codes
live in `src/protocol.ts`.

## 1. Log in

Nakama device authentication (`client.AuthenticateDeviceAsync(deviceId)`), then open the socket.
`session.single_socket` and `single_match` are on: a second login drops the first socket, and
joining a zone leaves the previous one.

## 2. Characters (RPCs)

Payloads and answers are JSON. Errors come back as Nakama RPC errors (HTTP 400/404/409).

| RPC | Input | Answer |
| --- | --- | --- |
| `character_list` | `{}` | `{characters: [summary], available: [{id, name, weapons}]}` |
| `character_create` | `{characterId, name}` | `{character: summary}`; 409 if the account already has that character or another character holds the name (names are unique in any case), 400 for a bad name (2–20 letters, digits, spaces, `'` or `-`) |
| `character_load` | `{characterId}` | `{character, online, stats}`: the full save (gold, bag, gear, quick slots, skills, kills, last location, HP/Spirit) and its computed stats |
| `character_delete` | `{characterId}` | `{}`; refused while the character is in a zone |
| `zone_list` | `{zone}` | `{zone, channels: [{matchId, channel, players, cap}]}` |
| `zone_join` | `{zone, channel?}` | `{matchId, zone, channel}`: the lowest channel with room (or the one asked for), opening a new one when all are full |

`summary` = `{characterId, name, weaponType, gold, kills, zone, online}`. A name is claimed in
collection `character_names` (key: the name in lower case, owned by the system user, no client
access) in the same write as the character, and freed when the character is deleted. One save per character
kind per account (`kreshnik`, `zanas_chosen`). New characters get their `starterKits` entry from
`items.json` and, while `skills.json` `dev.learnAllBooks` is on, every book their weapons use plus
`dev.startingSkillPoints`.

Zones (`shared/gamedata/zones.json`): `dardana` (the town: safe, no monsters, cap 100;
placeholder layout until the town scene exists) and `oak_valley` (cap 50).

The Unity client shows a character select after logging in (`character_list`; Create calls
`character_create` with the typed name, then joins).

## 3. Join a zone

`socket.JoinMatchAsync(matchId, new Dictionary<string,string> {{"characterId", "kreshnik"}})`.
The join is refused with a message when the channel is full, the character doesn't exist, or the
character is still held by another zone (its previous match hasn't saved yet, or crashed less than
`server.sessionLockSeconds` ago): wait a moment and retry. On success the first message is
**Welcome**.

**Reconnecting.** When the socket drops, Nakama removes the presence from the match, which saves
the character and releases its lock. The client then logs in again (a new session only if the old
one expired), opens a new socket and joins a channel as the same character (`zone_join`, then the
join above, retrying while the lock is still held), backing off 1, 2, 4… up to 10 s between
attempts. The Welcome restores the saved position, HP, items and skills.

While joined, the character's save is locked to that match; the match saves changes about once a
second (`server.saveInterval`) together with their ledger rows, and once more on leave.

## 4. Match messages

Every payload is UTF-8 JSON (`socket.SendMatchStateAsync(matchId, opCode, json)`).

### Client → server

| Op | Name | Payload | Notes |
| --- | --- | --- | --- |
| 1 | Move | `{x, y, z, yaw}` | Send ~10×/s while moving. Checked against a movement budget (earned at `combat.json` `player.moveSpeed` × `movement.speedTolerance` per second of server time, banked up to `movement.maxGapSeconds` of it, spent by each move's length; `distanceSlack` may be borrowed), so moves may arrive in uneven bursts but can't outrun run speed on average; also slope, ledges (ledge tops are reached by Leap only) and the walkable grid. A refused move gets a **Correction**. Ignored while dead or mid-leap. |
| 2 | Target | `{id}` · `{id: null}` · `{nearest: true}` | Lock a monster (auto-attack starts whenever it's in weapon range), clear, or the Attack button (keep the current target or lock the nearest within `lockRange`). Answer: **Result** `{a: "target", id}`. |
| 3 | Cast | `{form, target?, dir?: [x, z], rid?}` | A skill form. Single and circle-at-target forms need a target in range (run in first). Pressed while a form plays, it's queued (`{queued: true}`) and fires when that one ends, if still possible within `playerRules.castQueueTimeout`. Cones face the target, else `dir`, else the player's yaw. |
| 4 | Leap | `{ledge?, dir?: [x, z], rid?}` | Drangue Leap onto the named ledge (`zones.json` ledge `name`), checked like `LeapRules` (distance, rise, drop) plus the aim wedge; or, without `ledge`, a ground dash of `dashDistance`. Answer: **Result** `{to: [x, y, z], ledge}`. |
| 5 | Action | `{a, rid?, …}` | Item and book actions, below. Answer: **Result**, then **Self** if anything changed. |

Actions (`a`):

| a | Fields | |
| --- | --- | --- |
| `equip` | `item` (instance id) | Wear from the bag; the old piece takes its bag slot |
| `unequip` | `slot` (`helm`, `armor`, …) | The weapon slot can't be emptied |
| `swap` | | Wield the other weapon type (last used, else best) |
| `moveItem` | `from`, `to` (bag indices) | Move, merge potions, or swap |
| `quickSlot` | `slot` (0/1), `item` (item def id) | Put a potion on a quick slot |
| `use` | `item` (def id) | Drink a potion |
| `quickUse` | `slot` | Drink the quick slot's potion |
| `buy` | `shop`, `item` (def id), `count` | Within `playerRules.shopLeaveRange` of that shop's keeper |
| `sell` | `item` (instance id), `count` | Near any shopkeeper; worn items can't be sold |
| `levelBook` | `book` | Spend skill points on a book level |

### Server → client

| Op | Name | To | Payload |
| --- | --- | --- | --- |
| 100 | Welcome | joiner | `{you, zone, t, self, players: [player], monsters: [monster], drops: []}` |
| 101 | Snapshot | all | `{t, p: [player], m: [monster]}`, only entities that changed this tick |
| 102 | Events | all | `{t, e: [event]}` |
| 103 | Self | one | Your private state (below), after it changes |
| 104 | Drops | one | `{add: [drop], remove: [{id, why}]}`; `why` = `picked`, `expired`, `gone` |
| 105 | Result | one | `{a, rid?, ok, error?, text?, …}`; `text` is the offline client's wording |
| 106 | Correction | one | `{x, y, z, why}`: put the player here (`tooFast`, `tooSteep`, `ledge`, `blocked` (off walkable ground: walls, the river, rocks), `height` (too far above or below the ground), `disabled`, `revive`) |
| 107 | Notice | one | `{code, text?}`: `target` `{id}` (target changed or dropped), `skillPoint`, `bagFull` |

- `player` = `{id (user id), name, character, weapon (item def), x, y, z, yaw, hp, maxHp, sp, maxSp, dead, st: [{id, left}]}`
- `monster` = `{id, monster (combat.json id), pack, x, y, z, yaw, hp, maxHp, state (idle/chase/return/dead), target, st}`. Monsters walk on the zone's walkable grid (straight where they can, round obstacles where they can't) and `y` is the grid's ground height, so they line up with the NavMesh to within a few centimetres; snap them onto it locally if needed.
- `drop` = `{id, x, y, z, gold, item: {id, def, rarity, count, lines} | null, left}`. Loot belongs to the player credited with the kill (most damage dealt; the killing blow breaks ties) and only they see it; it's picked up automatically within `loot.autoLootRadius` after `loot.pickupDelay`.
- `self` = `{characterId, name, gold, bag (array; empty slots are null), equipment: {weapon, helm, armor, boots, ring, amulet} (item or null), quickSlots, stats, skills: {books: [{id, level}], points, meterKills}, kills, cooldowns: [{id, left}] (form ids and `drangue_leap`), potionCooldowns: [{id, left}] (cooldown groups)}`. Lists rather than maps, so Unity's JsonUtility reads it.

Events (`k`):

| k | Fields | |
| --- | --- | --- |
| `join` / `leave` | `player` / `id` | Someone entered or left the channel |
| `hit` | `src, dst, dmg, crit, miss, kind` | Every resolved attack: `kind` = `basic`, `skill`, `poison` (ticks credited to whoever applied it) |
| `cast` | `src, form, tgt, dir, n` | A form went off and hit `n` targets |
| `status` | `id, s, dur` | A status landed |
| `die` | `id, by` | A player or monster died; for monsters `by` is the credited player |
| `revive` | `id, at` | A player revived at the zone's respawn point |
| `despawn` / `spawn` | `id` / `monster` | A corpse vanished / a monster (re)spawned |
| `leap` | `id, from, to, dur, ledge` | A Drangue Leap or dash started |
| `gear` | `id, weapon, weaponType` | A player's weapon changed (show the new model) |

## 5. Rules the server applies

The same as the offline client (`Wuxia.Combat`, `Wuxia.Items`), with the numbers from
`shared/gamedata`: basic attacks (hit = accuracy − dodge, 5–99%; damage × crit − defense, ≥1),
skill forms (always hit; book level and skill damage bonuses; shapes measured on the ground plane;
`reachHeight`), statuses (refresh, poison ticks), Spirit and cooldowns, Drangue Leap landing rules,
the bag, gear, shop, potions and loot tables. Float maths follows C# single precision and
round-half-to-even, so a predicted number matches the server's.

Server-only additions, all in `zones.json`:

- `server.rangeTolerance` (0.75 m): extra reach on attacks and single-target forms, for positions a tick apart.
- `movement`: the move checks above, including `gridSlack` (a move may end this close to walkable ground, since the NavMesh keeps its agent radius clear of walls) and `heightTolerance` (how far from the ground's height a player may stand, except while coming down).
- `monsterAi`: the values `MonsterAI`/`MonsterSpawner` keep as serialized fields (corpse time, pack assist radius, wander pauses, personal space, chase wind-up), plus a body radius so a pack spreads around a player.
- `monsterAi.repathInterval`, `monsterAi.pathMaxNodes`: how often a chasing monster re-plans its path, and how far one search may look before the target counts as unreachable (the monster then gives up and goes home).
- `playerRules`: cast queue timeout, weapon draw time after a swap, shop ranges, loot scatter.

**Walkable ground.** A zone with a `navGrid` in `zones.json` (Oak Valley) has its NavMesh, as
exported from Unity, on a 1 m grid of ground heights (`shared/gamedata/navgrid/<zone>.json`). Moves
must end on or next to walkable ground at its height, a dash stops at the first wall, and monsters
path round trunks, rocks, walls and the river (over the bridge). The town has no grid yet, so only
speed, slope and ledges are checked there. Whether a ledge top is clear isn't checked; the ledges
listed in `zones.json` are all clear.
