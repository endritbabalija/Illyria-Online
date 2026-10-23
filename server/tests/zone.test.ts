// ZoneSim: the server-run zone with real Oak Valley data. Two players fighting the same wolf is
// the M4 "done when"; the rest checks movement and leap validation, skills, loot, death, shops,
// the safe town and saving.

var TICK = 0.1;

function newSim(zoneId?: string, seed?: number): ZoneSim {
    return new ZoneSim(testData(), zoneId || 'oak_valley', { rng: seededRng(seed || 7), nextItemId: counterIds('srv-') });
}

function saveAt(sim: ZoneSim, characterId: string, name: string, at?: Vec3): CharacterSave {
    var save = createCharacter(sim.data, characterId, name, counterIds(name + '-'), 0).save;
    if (at) save.location = { zone: sim.zone.id, x: at.x, y: at.y, z: at.z, yaw: 0 };
    return save;
}

function join(sim: ZoneSim, userId: string, characterId: string, at?: Vec3): SimPlayer {
    return sim.addPlayer(userId, 'session-' + userId, userId, saveAt(sim, characterId, 'Hero ' + userId, at));
}

function run(sim: ZoneSim, seconds: number, each?: () => void): void {
    var ticks = Math.round(seconds / TICK);
    for (var i = 0; i < ticks; i++) {
        if (each) each();
        sim.step(TICK);
    }
}

function runUntil(sim: ZoneSim, maxSeconds: number, done: () => boolean, each?: () => void): boolean {
    for (var t = 0; t < maxSeconds; t += TICK) {
        if (done()) return true;
        if (each) each();
        sim.step(TICK);
    }
    return done();
}

/** Every message sent so far, drained. */
function drain(sim: ZoneSim): OutMessage[] { return sim.takeMessages(); }

function messagesFor(messages: OutMessage[], userId: string, op: number): any[] {
    return messages.filter(function (m) { return m.op === op && (m.to === null || indexOf(m.to, userId) >= 0); }).map(function (m) { return m.data; });
}

function eventsOf(messages: OutMessage[], kind: string): any[] {
    var out: any[] = [];
    messages.forEach(function (m) { if (m.op === Op.Events) m.data.e.forEach(function (e: any) { if (e.k === kind) out.push(e); }); });
    return out;
}

function packMonsters(sim: ZoneSim, packId: string): SimMonster[] {
    return sim.monsters.filter(function (m) { return m.pack.id === packId; });
}

function lastResult(messages: OutMessage[], userId: string): any {
    var results = messagesFor(messages, userId, Op.Result);
    return results[results.length - 1];
}

/** Keeps both players parked next to a monster (they "walk" with it, within the speed limit). */
function standBy(sim: ZoneSim, p: SimPlayer, m: SimMonster, offsetX: number): void {
    var to = vec3(m.pos.x + offsetX, m.pos.y, m.pos.z);
    if (flatDistance(p.pos, to) > 0.4) {
        var dir = flatDirection(p.pos, to) as Vec2;
        var step = Math.min(0.5, flatDistance(p.pos, to));
        to = vec3(p.pos.x + dir.x * step, m.pos.y, p.pos.z + dir.z * step);
    }
    sim.handle(p.id, Op.Move, { x: to.x, y: to.y, z: to.z, yaw: 0 });
}

// ---- joining ----------------------------------------------------------------------------------

test('zone: Oak Valley opens with every pack at full strength', function () {
    var sim = newSim();
    eq(sim.monsters.length, 30);
    sim.zone.packs.forEach(function (pack) {
        packMonsters(sim, pack.id).forEach(function (m) {
            eq(m.data.id, pack.monster);
            ok(flatDistance(m.pos, vec3From(pack.position)) <= pack.radius + 1e-6, m.id + ' inside its pack circle');
            eq(m.hp, m.data.maxHp);
        });
    });
});

test('zone: two players see each other and the monsters', function () {
    var sim = newSim();
    join(sim, 'alice', 'kreshnik');
    var first = drain(sim);
    var welcomeA = messagesFor(first, 'alice', Op.Welcome)[0];
    eq(welcomeA.you, 'alice');
    eq(welcomeA.players.length, 1);
    eq(welcomeA.monsters.length, 30);
    eq(welcomeA.self.gold, 100);
    // New players start at the zone's respawn point (the Han).
    eq([welcomeA.players[0].x, welcomeA.players[0].z], [-16, -178]);

    join(sim, 'bora', 'zanas_chosen');
    sim.step(TICK);
    var second = drain(sim);
    var welcomeB = messagesFor(second, 'bora', Op.Welcome)[0];
    eq(welcomeB.players.map(function (p: any) { return p.id; }).sort(), ['alice', 'bora']);
    var joins = eventsOf(second, 'join').filter(function (j) { return j.player.id === 'bora'; });
    eq(joins.length, 1);
    eq(joins[0].player.id, 'bora');
    eq(joins[0].player.character, 'zanas_chosen');
    eq(joins[0].player.weapon, 'worn_sicas');
    ok(messagesFor(second, 'alice', Op.Events).length > 0, 'alice hears about bora');
    eq(messagesFor(second, 'alice', Op.Welcome).length, 0);

    // Alice moves; Bora gets it in a snapshot.
    sim.handle('alice', Op.Move, { x: -16, y: 0, z: -177.5, yaw: 0 });
    sim.step(TICK);
    var snaps = messagesFor(drain(sim), 'bora', Op.Snapshot);
    ok(snaps.some(function (s: any) { return s.p.some(function (p: any) { return p.id === 'alice' && p.z === -177.5; }); }), 'bora sees alice move');
});

// ---- the M4 "done when": two players fight the same monster -----------------------------------

test('zone: two players fight the same wolf; the server decides every hit and the kill', function () {
    var sim = newSim('oak_valley', 11);
    var wolf = packMonsters(sim, 'MeadowWestPack')[0];
    var a = join(sim, 'alice', 'kreshnik', vec3(wolf.pos.x - 1.5, wolf.pos.y, wolf.pos.z));
    var b = join(sim, 'bora', 'zanas_chosen', vec3(wolf.pos.x + 1.5, wolf.pos.y, wolf.pos.z));
    drain(sim);
    sim.handle('alice', Op.Target, { id: wolf.id, rid: 1 });
    sim.handle('bora', Op.Target, { id: wolf.id, rid: 2 });
    var messages: OutMessage[] = [];
    ok(runUntil(sim, 20, function () { return wolf.state === 'dead'; }, function () {
        standBy(sim, a, wolf, -1.2);
        standBy(sim, b, wolf, 1.2);
        messages = messages.concat(drain(sim));
    }), 'the wolf dies');
    messages = messages.concat(drain(sim));

    var hits = eventsOf(messages, 'hit').filter(function (h) { return h.dst === wolf.id; });
    ok(hits.some(function (h) { return h.src === 'alice'; }), 'alice hit it');
    ok(hits.some(function (h) { return h.src === 'bora'; }), 'bora hit it');
    var dealt = 0;
    hits.forEach(function (h) {
        if (h.miss) return;
        dealt += h.dmg;
        // Basic attacks only: each hit is within the attacker's weapon range after the wolf's defense (crits ×1.6).
        var weapon = getWeapon(sim.data, h.src === 'alice' ? 'sword' : 'sicas').attack;
        between(h.dmg, Math.max(1, weapon.minDamage - wolf.data.defense), Math.round(weapon.maxDamage * weapon.critMultiplier) - wolf.data.defense, 'hit by ' + h.src);
    });
    ok(dealt >= wolf.data.maxHp, 'damage dealt covers its HP');

    // Both clients got the same death event; the top damage dealer gets the kill.
    var deaths = eventsOf(messages, 'die').filter(function (d) { return d.id === wolf.id; });
    eq(deaths.length, 1);
    var credited = deaths[0].by;
    eq(credited, sim.creditFor(wolf));
    eq(sim.players[credited].save.kills, 1);
    var other = credited === 'alice' ? 'bora' : 'alice';
    eq(sim.players[other].save.kills, 0);
    var tally = wolf.damageBy;
    ok(tally[credited] >= tally[other]);

    // Loot is the credited player's alone.
    ok(messagesFor(messages, other, Op.Drops).length === 0 || messagesFor(messages, other, Op.Drops).every(function (d: any) { return d.add.length === 0; }));
});

test('zone: monsters bite back, players die, revive at the Han and the pack goes home', function () {
    var sim = newSim('oak_valley', 3);
    var pack = packMonsters(sim, 'WolfDenPack');
    var p = join(sim, 'alice', 'kreshnik', vec3From(pack[0].pack.position));
    p.hp = 30;
    drain(sim);
    var messages: OutMessage[] = [];
    ok(runUntil(sim, 30, function () { return p.dead; }, function () { messages = messages.concat(drain(sim)); }), 'the den wolves kill her');
    messages = messages.concat(drain(sim));
    ok(eventsOf(messages, 'hit').some(function (h) { return h.dst === 'alice' && !h.miss; }));
    eq(eventsOf(messages, 'die').filter(function (d) { return d.id === 'alice'; }).length, 1);
    sim.step(TICK);
    ok(pack.every(function (m) { return m.state !== 'chase'; }), 'with their target dead the wolves stop chasing and head home');

    run(sim, sim.data.combat.player.respawnDelay + TICK);
    ok(!p.dead);
    eq(p.hp, p.stats.maxHp);
    eq([p.pos.x, p.pos.z], [-16, -178]);
    var revive = drain(sim);
    eq(eventsOf(revive, 'revive').length, 1);
    eq(messagesFor(revive, 'alice', Op.Correction)[0].why, 'revive');

    run(sim, 10);
    pack.forEach(function (m) { eq(m.state, 'idle'); eq(m.hp, m.data.maxHp); });
});

test('zone: hitting one wolf pulls in its packmates nearby', function () {
    var sim = newSim('oak_valley', 5);
    var pack = packMonsters(sim, 'MeadowEastPack');
    var target = pack[0];
    // Bow range: shoot from outside every wolf's aggro range.
    var p = join(sim, 'alice', 'kreshnik', vec3(target.pos.x, target.pos.y, target.pos.z - 12));
    p.items.swapWeapon();
    sim.step(TICK);
    ok(pack.every(function (m) { return m.state === 'idle'; }), 'nobody noticed her yet');
    sim.handle('alice', Op.Target, { id: target.id });
    runUntil(sim, 5, function () { return target.state === 'chase'; });
    eq(target.state, 'chase');
    var radius = sim.data.zones.monsterAi.packAssistRadius;
    pack.forEach(function (m) {
        if (m !== target && flatDistance(m.pos, target.pos) <= radius) ok(m.state === 'chase' || m.state === 'dead', m.id + ' joins in');
    });
});

test('zone: a player on a ledge is out of the wolves\' reach', function () {
    var sim = newSim('oak_valley', 9);
    var ledge = sim.zone.ledges.filter(function (l) { return l.name === 'LeapStone Meadow West'; })[0];
    var top = ledgeLandingPoint(ledge, vec3(ledge.centre[0], 0, ledge.centre[2]), 0.8);
    var p = join(sim, 'alice', 'kreshnik', top);
    ok(p.onLedge === ledge, 'rejoins on the ledge');
    p.items.swapWeapon();
    var wolf = packMonsters(sim, 'MeadowWestPack')[0];
    sim.step(TICK);
    drain(sim);
    sim.handle('alice', Op.Target, { id: wolf.id });
    var messages: OutMessage[] = [];
    var chased = false;
    run(sim, 10, function () {
        if (wolf.state === 'chase') chased = true;
        messages = messages.concat(drain(sim));
    });
    messages = messages.concat(drain(sim));
    ok(chased, 'the wolf came for her');
    ok(eventsOf(messages, 'hit').some(function (h) { return h.src === 'alice' && h.dst === wolf.id; }), 'she shot it from the ledge');
    eq(eventsOf(messages, 'hit').filter(function (h) { return h.dst === 'alice'; }).length, 0, 'no bite reaches her');
    eq(p.hp, p.stats.maxHp);
});

// ---- skills -----------------------------------------------------------------------------------

test('zone: skill forms need book levels, cost Spirit and hit their shape', function () {
    var sim = newSim('oak_valley', 21);
    var pack = packMonsters(sim, 'BoarSpringHerd');
    var boar = pack[0];
    var p = join(sim, 'alice', 'kreshnik', vec3(boar.pos.x - 2, boar.pos.y, boar.pos.z));
    drain(sim);

    sim.handle('alice', Op.Cast, { form: 'eagle_strike', target: boar.id, rid: 1 });
    eq(lastResult(drain(sim), 'alice').error, 'locked', 'book level 0');
    sim.handle('alice', Op.Cast, { form: 'hawk_shot', target: boar.id, rid: 2 });
    eq(lastResult(drain(sim), 'alice').error, 'wrongWeapon', 'a bow form with a sword');

    for (var i = 0; i < 3; i++) sim.handle('alice', Op.Action, { a: 'levelBook', book: 'sword_book' });
    eq(p.skills.bookLevel('sword_book'), 3);
    eq(p.skills.progress.points, 37);
    drain(sim);

    var spirit = p.skills.spirit;
    sim.handle('alice', Op.Cast, { form: 'eagle_strike', target: boar.id, rid: 3 });
    var r = lastResult(drain(sim), 'alice');
    ok(r.ok, JSON.stringify(r));
    eq(r.hits, 1);
    eq(p.skills.spirit, spirit - 8);
    ok(boar.hp < boar.data.maxHp, 'the boar took the strike');

    // Pressed while Eagle Strike plays: queued, then fires when it ends.
    sim.handle('alice', Op.Cast, { form: 'mountain_sweep', rid: 4 });
    eq(lastResult(drain(sim), 'alice').queued, true);
    var messages: OutMessage[] = [];
    run(sim, 0.6, function () { messages = messages.concat(drain(sim)); });
    messages = messages.concat(drain(sim));
    var casts = eventsOf(messages, 'cast').filter(function (c) { return c.form === 'mountain_sweep'; });
    eq(casts.length, 1, 'the queued sweep went off');
    ok(casts[0].n >= 1, 'and hit the boar in front');

    sim.handle('alice', Op.Cast, { form: 'eagle_strike', target: boar.id, rid: 5 });
    eq(lastResult(drain(sim), 'alice').error, 'cooldown');
});

test('zone: Thunder Arrow stuns, and a stunned monster stops', function () {
    var sim = newSim('oak_valley', 4);
    var wolf = packMonsters(sim, 'MeadowWestPack')[0];
    var p = join(sim, 'alice', 'kreshnik', vec3(wolf.pos.x, wolf.pos.y, wolf.pos.z - 10));
    p.items.swapWeapon();
    for (var i = 0; i < 6; i++) p.skills.levelUp('bow_book');
    sim.step(TICK);
    drain(sim);
    sim.handle('alice', Op.Cast, { form: 'thunder_arrow', target: wolf.id });
    var r = lastResult(drain(sim), 'alice');
    ok(r.ok, JSON.stringify(r));
    ok(wolf.state === 'dead' || wolf.statuses.isDisabled(sim.now), 'stunned (chance 1, wolves have no stun resistance)');
    if (wolf.state === 'dead') return;
    var at = { x: wolf.pos.x, z: wolf.pos.z };
    run(sim, 1);
    eq({ x: wolf.pos.x, z: wolf.pos.z }, at, 'stood still while stunned');
});

// ---- movement and leaping ---------------------------------------------------------------------

test('zone: moves faster than the run speed are corrected', function () {
    var sim = newSim();
    var p = join(sim, 'alice', 'kreshnik');
    drain(sim);
    sim.handle('alice', Op.Move, { x: -16, y: 0, z: -168, yaw: 0 });
    var c = messagesFor(drain(sim), 'alice', Op.Correction)[0];
    eq(c.why, 'tooFast');
    eq([c.x, c.z], [-16, -178]);
    eq(p.pos.z, -178);
    // Walking at run speed is fine, message after message.
    for (var i = 1; i <= 20; i++) {
        sim.handle('alice', Op.Move, { x: -16, y: 0, z: -178 + i * 0.55, yaw: 0 });
        sim.step(TICK);
    }
    eq(messagesFor(drain(sim), 'alice', Op.Correction).length, 0);
    near(p.pos.z, -167, 1e-9);
});

test('zone: walking up onto a ledge is refused; leaping onto it is allowed', function () {
    var sim = newSim();
    var p = join(sim, 'alice', 'kreshnik', vec3(-21.5, 0, -182));
    var ledge = sim.zone.ledges.filter(function (l) { return l.name === 'LeapStone Low'; })[0];
    run(sim, 0.5);
    drain(sim);
    sim.handle('alice', Op.Move, { x: -23.5, y: ledgeTop(ledge), z: -182, yaw: -90 });
    eq(messagesFor(drain(sim), 'alice', Op.Correction)[0].why, 'ledge');

    sim.handle('alice', Op.Leap, { ledge: 'LeapStone Low', dir: [1, 0], rid: 1 });
    eq(lastResult(drain(sim), 'alice').error, 'notInAim', 'aiming away from it');
    sim.handle('alice', Op.Leap, { ledge: 'Kulla Roof', dir: [1, 0], rid: 2 });
    eq(lastResult(drain(sim), 'alice').error, 'tooFar');
    sim.handle('alice', Op.Leap, { ledge: 'Not A Ledge', dir: [-1, 0], rid: 3 });
    eq(lastResult(drain(sim), 'alice').error, 'notALedge');

    var spirit = p.skills.spirit;
    sim.handle('alice', Op.Leap, { ledge: 'LeapStone Low', dir: [-1, 0], rid: 4 });
    var r = lastResult(drain(sim), 'alice');
    ok(r.ok, JSON.stringify(r));
    eq(p.skills.spirit, spirit - sim.data.skills.drangueLeap.spiritCost);
    run(sim, sim.data.skills.drangueLeap.leapDuration + TICK);
    ok(p.onLedge === ledge);
    near(p.pos.y, ledgeTop(ledge), 1e-9);
    // Walking about on top is fine; stepping off drops you to the ground.
    sim.handle('alice', Op.Move, { x: p.pos.x - 0.3, y: p.pos.y, z: p.pos.z, yaw: 0 });
    eq(messagesFor(drain(sim), 'alice', Op.Correction).length, 0);
    sim.step(TICK);
    sim.handle('alice', Op.Leap, { ledge: 'LeapStone Low', dir: [-1, 0], rid: 5 });
    eq(lastResult(drain(sim), 'alice').error, 'cooldown');
});

test('zone: without a ledge the leap is a ground dash', function () {
    var sim = newSim();
    var p = join(sim, 'alice', 'kreshnik');
    drain(sim);
    sim.handle('alice', Op.Leap, { dir: [0, 1] });
    var r = lastResult(drain(sim), 'alice');
    ok(r.ok);
    eq(r.ledge, null);
    run(sim, 0.5);
    near(p.pos.z, -178 + sim.data.skills.drangueLeap.dashDistance, 1e-9);
    eq(p.onLedge, null);
});

// ---- loot, shop and saving --------------------------------------------------------------------

test('zone: loot drops for the killer, auto-loots within range and lands in the ledger', function () {
    var sim = newSim('oak_valley', 2);
    var wolf = packMonsters(sim, 'MeadowWestPack')[0];
    var p = join(sim, 'alice', 'kreshnik', vec3(wolf.pos.x - 1.5, wolf.pos.y, wolf.pos.z));
    sim.collectSaves('match-1', 0);
    drain(sim);
    sim.handle('alice', Op.Target, { id: wolf.id });
    var messages: OutMessage[] = [];
    // Kill wolves until one drops something.
    var dropped = runUntil(sim, 120, function () { return messagesFor(messages, 'alice', Op.Drops).some(function (d: any) { return d.add.length > 0; }); }, function () {
        if (!p.target || wolf.state === 'dead') {
            var next = sim.nearestMonster(p, 30);
            if (next) { wolf = next; sim.handle('alice', Op.Target, { id: wolf.id }); }
        }
        if (wolf.state !== 'dead') standBy(sim, p, wolf, -1.2);
        p.hp = p.stats.maxHp;
        messages = messages.concat(drain(sim));
    });
    ok(dropped, 'something dropped');
    var drop = messagesFor(messages, 'alice', Op.Drops).filter(function (d: any) { return d.add.length > 0; })[0].add[0];
    var gold = p.items.gold, bagCount = p.items.s.bag.filter(function (i) { return !!i; }).length;
    // Walk onto it: picked up after the pickup delay.
    var picked = runUntil(sim, 10, function () {
        return messages.some(function (m) { return m.op === Op.Drops && m.data.remove.some(function (r: any) { return r.id === drop.id && r.why === 'picked'; }); });
    }, function () {
        var to = vec3(drop.x, p.pos.y, drop.z);
        var dir = flatDirection(p.pos, to);
        if (dir) {
            var step = Math.min(0.5, flatDistance(p.pos, to));
            sim.handle('alice', Op.Move, { x: p.pos.x + dir.x * step, y: p.pos.y, z: p.pos.z + dir.z * step, yaw: 0 });
        }
        p.hp = p.stats.maxHp;
        messages = messages.concat(drain(sim));
    });
    ok(picked, 'auto-looted');
    if (drop.gold > 0) ok(p.items.gold >= gold + drop.gold);
    else ok(p.items.s.bag.filter(function (i) { return !!i; }).length >= bagCount || p.items.countOf(drop.item.def) > 0);

    var saves = sim.collectSaves('match-1', 10000);
    eq(saves.length, 1);
    var ledger = saves[0].ledger;
    ok(ledger.some(function (e) { return e.reason === 'loot: Grey Wolf' || e.reason.indexOf('loot: ') === 0; }), 'the pickup is in the ledger');
    eq(saves[0].save.session, { matchId: 'match-1', until: 10000 + sim.data.zones.server.sessionLockSeconds * 1000 });
    ok(saves[0].save.kills >= 1);
});

test('zone: shops trade only near their keeper', function () {
    var sim = newSim();
    var keeper = vec3From(sim.zone.shopkeepers[0].position);
    var p = join(sim, 'alice', 'kreshnik', vec3(keeper.x + 2, 0, keeper.z));
    drain(sim);
    sim.handle('alice', Op.Action, { a: 'buy', shop: 'han_keeper', item: 'hp_potion', count: 2, rid: 1 });
    var r = lastResult(drain(sim), 'alice');
    ok(r.ok, JSON.stringify(r));
    eq(p.items.gold, 80);
    eq(p.items.countOf('hp_potion'), 7);
    sim.step(TICK);
    eq(messagesFor(drain(sim), 'alice', Op.Self)[0].gold, 80, 'her private state follows');

    join(sim, 'bora', 'kreshnik');
    drain(sim);
    sim.handle('bora', Op.Action, { a: 'buy', shop: 'han_keeper', item: 'hp_potion', count: 1, rid: 2 });
    eq(lastResult(drain(sim), 'bora').error, 'noShop');
});

test('zone: gear actions update stats for the player and the weapon for everyone', function () {
    var sim = newSim();
    var p = join(sim, 'alice', 'kreshnik');
    join(sim, 'bora', 'kreshnik');
    drain(sim);
    sim.handle('alice', Op.Action, { a: 'swap', rid: 1 });
    sim.step(TICK);
    var messages = drain(sim);
    ok(lastResult(messages, 'alice').ok);
    eq(p.stats.weaponType, 'bow');
    var gear = eventsOf(messages, 'gear');
    eq(gear.length, 1);
    eq([gear[0].id, gear[0].weapon], ['alice', 'worn_bow']);
    var self = messagesFor(messages, 'alice', Op.Self);
    eq(self[self.length - 1].stats.attack.range, getWeapon(sim.data, 'bow').attack.range);
    eq(messagesFor(messages, 'bora', Op.Self).length, 0, 'private state stays private');

    sim.handle('alice', Op.Action, { a: 'unequip', slot: 'weapon', rid: 2 });
    eq(lastResult(drain(sim), 'alice').error, 'weaponRequired');
    sim.handle('alice', Op.Action, { a: 'nonsense', rid: 3 });
    eq(lastResult(drain(sim), 'alice').error, 'badRequest');
});

test('zone: the town is safe', function () {
    var sim = newSim('dardana');
    eq(sim.monsters.length, 0);
    join(sim, 'alice', 'kreshnik');
    drain(sim);
    sim.handle('alice', Op.Cast, { form: 'storm_of_blades', rid: 1 });
    eq(lastResult(drain(sim), 'alice').error, 'safeZone');
    sim.handle('alice', Op.Target, { nearest: true, rid: 2 });
    eq(lastResult(drain(sim), 'alice').error, 'noTarget');
});

test('zone: leaving returns a final save with the lock released and the spot kept', function () {
    var sim = newSim();
    var p = join(sim, 'alice', 'kreshnik');
    sim.handle('alice', Op.Move, { x: -16, y: 0, z: -177.6, yaw: 90 });
    sim.step(TICK);
    join(sim, 'bora', 'kreshnik');
    drain(sim);
    var request = sim.removePlayer('alice') as SaveRequest;
    eq(request.save.session, null);
    eq(request.save.location, { zone: 'oak_valley', x: -16, y: 0, z: -177.6, yaw: 90 });
    eq(request.save.hp, p.stats.maxHp);
    eq(eventsOf((sim.step(TICK), drain(sim)), 'leave'), [{ k: 'leave', id: 'alice' }]);
    eq(sim.playerCount(), 1);
    // Rejoining puts her back where she was.
    var again = sim.addPlayer('alice', 's2', 'alice', request.save);
    eq([again.pos.x, again.pos.z], [-16, -177.6]);
});

test('zone: bad messages are ignored', function () {
    var sim = newSim();
    var p = join(sim, 'alice', 'kreshnik');
    drain(sim);
    sim.handle('alice', Op.Move, { x: 'far', y: null, z: NaN });
    sim.handle('alice', Op.Move, null);
    sim.handle('alice', 999, { anything: true });
    sim.handle('nobody', Op.Move, { x: 0, y: 0, z: 0 });
    sim.handle('alice', Op.Target, { id: 'm999' });
    sim.handle('alice', Op.Cast, { form: 'no_such_form' });
    sim.handle('alice', Op.Action, { a: 'moveItem', from: -1, to: 'x' });
    sim.handle('alice', Op.Action, { a: 'buy', shop: 'han_keeper', item: 'hp_potion', count: -5 });
    sim.step(TICK);
    eq([p.pos.x, p.pos.z], [-16, -178]);
    eq(p.items.gold, 100);
});
