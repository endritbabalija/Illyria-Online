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
| `character_create` | `{characterId, name}` | `{character: summary}`; 409 if the account already has that character, 400 for a bad name (2–20 letters, digits, spaces, `'` or `-`) |
| `character_load` | `{characterId}` | `{character, online, stats}`: the full save (gold, bag, gear, quick slots, skills, kills, last location, HP/Spirit) and its computed stats |
| `character_delete` | `{characterId}` | `{}`; refused while the character is in a zone |
| `zone_list` | `{zone}` | `{zone, channels: [{matchId, channel, players, cap}]}` |
| `zone_join` | `{zone, channel?}` | `{matchId, zone, channel}`: the lowest channel with room (or the one asked for), opening a new one when all are full |

`summary` = `{characterId, name, weaponType, gold, kills, zone, online}`. One save per character
kind per account (`kreshnik`, `zanas_chosen`). New characters get their `starterKits` entry from
`items.json` and, while `skills.json` `dev.learnAllBooks` is on, every book their weapons use plus
`dev.startingSkillPoints`.

Zones (`shared/gamedata/zones.json`): `dardana` (the town: safe, no monsters, cap 100;
placeholder layout until the town scene exists) and `oak_valley` (cap 50).

## 3. Join a zone

`socket.JoinMatchAsync(matchId, new Dictionary<string,string> {{"characterId", "kreshnik"}})`.
The join is refused with a message when the channel is full, the character doesn't exist, or the
character is still held by another zone (its previous match hasn't saved yet, or crashed less than
`server.sessionLockSeconds` ago): wait a moment and retry. On success the first message is
**Welcome**.

While joined, the character's save is locked to that match; the match saves changes about once a
second (`server.saveInterval`) together with their ledger rows, and once more on leave.

## 4. Match messages

Every payload is UTF-8 JSON (`socket.SendMatchStateAsync(matchId, opCode, json)`).

### Client → server

| Op | Name | Payload | Notes |
| --- | --- | --- | --- |
| 1 | Move | `{x, y, z, yaw}` | Send ~10×/s while moving. Checked against run speed (`combat.json` `player.moveSpeed` × `movement.speedTolerance`), slope, and ledges (ledge tops are reached by Leap only). A refused move gets a **Correction**. Ignored while dead or mid-leap. |
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
| 106 | Correction | one | `{x, y, z, why}`: put the player here (`tooFast`, `tooSteep`, `ledge`, `disabled`, `revive`) |
| 107 | Notice | one | `{code, text?}`: `target` `{id}` (target changed or dropped), `skillPoint`, `bagFull` |

- `player` = `{id (user id), name, character, weapon (item def), x, y, z, yaw, hp, maxHp, sp, maxSp, dead, st: [{id, left}]}`
- `monster` = `{id, monster (combat.json id), pack, x, y, z, yaw, hp, maxHp, state (idle/chase/return/dead), target, st}`. Monsters move in straight lines on the ground plane; `y` is their pack's ground height (or the chased player's), so place them on the NavMesh/terrain locally.
- `drop` = `{id, x, y, z, gold, item: {id, def, rarity, count, lines} | null, left}`. Loot belongs to the player credited with the kill (most damage dealt; the killing blow breaks ties) and only they see it; it's picked up automatically within `loot.autoLootRadius` after `loot.pickupDelay`.
- `self` = `{characterId, name, gold, bag, equipment, quickSlots, stats, skills: {books, points, meterKills}, kills, cooldowns, potionCooldowns}`

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
- `movement`: the move checks above.
- `monsterAi`: the values `MonsterAI`/`MonsterSpawner` keep as serialized fields (corpse time, pack assist radius, wander pauses, personal space, chase wind-up), plus a body radius so a pack spreads around a player.
- `playerRules`: cast queue timeout, weapon draw time after a swap, shop ranges, loot scatter.

Not checked by the server yet: walls and the river (it has no collision geometry; moves are
checked for speed, slope and ledges only) and whether a ledge top is clear (the client's raycast).
