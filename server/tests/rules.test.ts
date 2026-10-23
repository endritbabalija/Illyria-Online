// The combat and skill rules against the numbers in shared/gamedata, mirroring the client's
// EditMode tests (CombatMathTests, SkillShapesTests, SkillStateTests, StatusSetTests, DrangueLeapTests).

// ---- game data --------------------------------------------------------------------------------

test('gamedata: all four files load and every reference resolves', function () {
    var data = testData();
    eq(validateGameData(data), []);
    ok(data.combat.monsters.length >= 1);
    ok(findBy(data.zones.zones, 'oak_valley'));
    ok(findBy(data.zones.zones, 'dardana'));
});

test('gamedata: a broken reference is reported', function () {
    var data = testData();
    data.zones.zones[1].packs[0].monster = 'dragon';
    data.items.lootTables[0].drops[0].item = 'excalibur';
    var problems = validateGameData(data);
    eq(problems.length, 2);
});

test('gamedata: weapon items match their combat.json weapon type', function () {
    var data = testData();
    data.items.items.filter(isWeaponDef).forEach(function (item) {
        ok(findBy(data.combat.weapons, item.weaponType), item.id);
        item.characters.forEach(function (c) { ok(indexOf(getCharacter(data, c).weapons, item.weaponType) >= 0, item.id + ' for ' + c); });
    });
    // The Worn weapons carry the old combat.json numbers.
    eq([getItem(data, 'worn_sword').stats.minDamage, getItem(data, 'worn_sword').stats.maxDamage],
       [getWeapon(data, 'sword').attack.minDamage, getWeapon(data, 'sword').attack.maxDamage]);
});

test('gamedata: Oak Valley packs match the scene (7 packs, 30 monsters)', function () {
    var zone = getZone(testData(), 'oak_valley');
    eq(zone.packs.length, 7);
    eq(zone.packs.reduce(function (n, p) { return n + p.count; }, 0), 30);
    eq(zone.ledges.length, 8);
});

// ---- CombatMath -------------------------------------------------------------------------------

function attackOf(overrides: Partial<AttackData>): AttackData {
    var a: AttackData = { minDamage: 10, maxDamage: 20, interval: 1, range: 2, reachHeight: 2, accuracy: 0.9, critChance: 0.1, critMultiplier: 2 };
    for (var k in overrides) (a as any)[k] = (overrides as any)[k];
    return a;
}

test('combat: hit chance is accuracy - dodge, clamped to 5-99%', function () {
    near(hitChance(0.9, 0.1), 0.8, 1e-5);
    eq(hitChance(0.1, 0.5), MIN_HIT_CHANCE);
    eq(hitChance(1.5, 0), MAX_HIT_CHANCE);
});

test('combat: a basic attack rolls hit, crit, then damage minus defense', function () {
    var a = attackOf({ accuracy: 0.9, critChance: 0.1, critMultiplier: 2 });
    // hit roll 0.5 < 0.85, crit roll 0.05 < 0.1, damage 14 × 2 − 3
    eq(rollBasicAttack(a, 3, 0.05, fixedRng([0.5, 0.05], [14])), { hit: true, crit: true, damage: 25, kind: 'basic' });
    eq(rollBasicAttack(a, 3, 0.05, fixedRng([0.5, 0.5], [14])), { hit: true, crit: false, damage: 11, kind: 'basic' });
    // a roll at or above the hit chance misses, and nothing else is rolled
    eq(rollBasicAttack(a, 3, 0.05, fixedRng([0.86])), { hit: false, crit: false, damage: 0, kind: 'basic' });
});

test('combat: defense never takes a hit below 1', function () {
    eq(rollBasicAttack(attackOf({}), 1000, 0, fixedRng([0, 0.9], [10])).damage, 1);
});

test('combat: crit damage rounds like C# floats (half to even, single precision)', function () {
    // Staff crit ×1.7f: 25 × 1.7f rounds to exactly 42.5 in single precision, and C# rounds halves to even.
    var staff = getWeapon(testData(), 'staff').attack;
    eq(rollBasicAttack(staff, 0, 0, fixedRng([0, 0], [25])).damage, 42);
    eq(rollBasicAttack(staff, 0, 0, fixedRng([0, 0], [26])).damage, 44);
    // An exact half goes to the even neighbour: 13 × 1.5 = 19.5 → 20, 15 × 1.5 = 22.5 → 22.
    var wolf = attackOf({ critChance: 1, critMultiplier: 1.5, minDamage: 5, maxDamage: 20 });
    eq(rollBasicAttack(wolf, 0, 0, fixedRng([0, 0], [13])).damage, 20);
    eq(rollBasicAttack(wolf, 0, 0, fixedRng([0, 0], [15])).damage, 22);
});

test('combat: hit and crit rates match over many rolls', function () {
    var rng = seededRng(1234);
    var a = attackOf({ accuracy: 0.85, critChance: 0.2 });
    var n = 20000, hits = 0, crits = 0;
    for (var i = 0; i < n; i++) {
        var r = rollBasicAttack(a, 0, 0.05, rng);
        if (r.hit) { hits++; between(r.damage, 10, 40); }
        if (r.crit) crits++;
    }
    near(hits / n, 0.8, 0.015, 'hit rate');
    near(crits / hits, 0.2, 0.015, 'crit rate among hits');
});

test('combat: the Kreshnik vs a Grey Wolf uses the real numbers', function () {
    var data = testData();
    var wolf = getMonster(data, 'wolf');
    var sword = getWeapon(data, 'sword').attack;
    near(hitChance(sword.accuracy, wolf.dodge), 0.84, 1e-5);
    // min roll, no crit: 14 − 1 defense
    eq(rollBasicAttack(sword, wolf.defense, wolf.dodge, fixedRng([0, 0.99], [14])).damage, 13);
    // max roll, crit: 22 × 1.6 = 35.2 → 35, − 1
    eq(rollBasicAttack(sword, wolf.defense, wolf.dodge, fixedRng([0, 0], [22])).damage, 34);
});

// ---- SkillMath --------------------------------------------------------------------------------

test('skills: damage grows 8% per book level above 1 and skill damage adds on top', function () {
    var data = testData();
    var form = (findForm(data, 'eagle_strike') as { form: FormData }).form;
    var per = data.skills.skillPoints.damagePerBookLevel;
    near(levelMultiplier(1, per), 1, 1e-6);
    near(levelMultiplier(6, per), 1.4, 1e-6);
    eq(rollSkillDamage(form, 1, per, 0, fixedRng([], [40]), 0), 40);
    // 40 × 1.4 = 56, − 5 defense
    eq(rollSkillDamage(form, 6, per, 5, fixedRng([], [40]), 0), 51);
    // 40 × 1.4 × 1.05 = 58.8 → 59
    eq(rollSkillDamage(form, 6, per, 0, fixedRng([], [40]), 0.05), 59);
    eq(rollSkillDamage(form, 1, per, 999, fixedRng([], [40]), 0), 1);
});

test('skills: status chance is chance × (1 − resistance)', function () {
    near(statusChance(0.6, 0), 0.6, 1e-6);
    near(statusChance(0.6, 0.5), 0.3, 1e-6);
    eq(statusChance(0.6, 2), 0);
    ok(statusLands(0.5, 0, fixedRng([0.49])));
    ok(!statusLands(0.5, 0, fixedRng([0.5])));
});

// ---- SkillShapes ------------------------------------------------------------------------------

var REACH = 2;

/** A point `distance` from the origin at `angle` degrees clockwise from +Z (Unity forward). */
function at(distance: number, angle: number, height?: number): Vec3 {
    var r = angle * Math.PI / 180;
    return vec3(Math.sin(r) * distance, height || 0, Math.cos(r) * distance);
}

function select(form: FormData, targets: Vec3[], locked?: number, facing?: Vec2): number[] {
    return selectTargets(form, vec3(0, 0, 0), facing || { x: 0, z: 1 }, targets, locked === undefined ? -1 : locked, REACH);
}

function formOf(shape: SkillShape, range: number, radius: number, angle: number): FormData {
    return {
        id: 'f', name: 'f', unlockLevel: 1, shape: shape, range: range, radius: radius, angle: angle, spiritCost: 0, cooldown: 0,
        castTime: 0, minDamage: 1, maxDamage: 1, status: { id: '', chance: 0, duration: 0, tickDamage: 0 },
    };
}

test('shapes: single hits only the locked target, in range', function () {
    var form = formOf('single', 3, 0, 0);
    eq(select(form, [at(2, 0), at(1, 90)], 0), [0]);
    eq(select(form, [at(2, 0), at(1, 90)], -1), []);
    eq(select(form, [at(3.01, 0)], 0), []);
});

test('shapes: cone hits exactly the wedge and follows the facing', function () {
    var form = formOf('cone', 4, 0, 90);
    eq(select(form, [at(3, 0), at(3, 44), at(3, 46), at(3, -44), at(3, 180), at(4.1, 0)]), [0, 1, 3]);
    eq(select(form, [at(3, 90), at(3, 0)], -1, { x: 1, z: 0 }), [0]);
});

test('shapes: circle and circle-at-target', function () {
    eq(select(formOf('circle', 0, 4, 0), [at(3.9, 0), at(3.9, 180), at(4.1, 90)]), [0, 1]);
    var rain = formOf('circleAtTarget', 10, 3, 0);
    var centre = at(8, 0);
    eq(select(rain, [centre, vec3(centre.x + 2.9, 0, centre.z), vec3(centre.x + 3.1, 0, centre.z), at(1, 0)], 0), [0, 1]);
    eq(select(rain, [at(10.1, 0)], 0), [], 'needs the target in range');
});

test('shapes: targets too far above or below are out of reach', function () {
    var form = formOf('circle', 0, 5, 0);
    eq(select(form, [at(2, 0, REACH), at(2, 90, -REACH - 0.01), at(2, 180, REACH + 0.01)]), [0]);
});

test('shapes: every form in skills.json respects its own edges', function () {
    var data = testData();
    data.skills.books.forEach(function (book) {
        book.forms.forEach(function (form) {
            var what = book.id + '/' + form.id;
            switch (form.shape) {
                case 'single':
                    eq(select(form, [at(form.range - 0.01, 30)], 0).length, 1, what);
                    eq(select(form, [at(form.range + 0.01, 30)], 0).length, 0, what);
                    break;
                case 'cone':
                    var edge = form.angle / 2;
                    eq(select(form, [at(form.range - 0.01, edge - 0.5)]).length, 1, what);
                    eq(select(form, [at(form.range - 0.01, edge + 0.5)]).length, 0, what);
                    eq(select(form, [at(form.range + 0.01, 0)]).length, 0, what);
                    break;
                case 'circle':
                    eq(select(form, [at(form.radius - 0.01, 200)]).length, 1, what);
                    eq(select(form, [at(form.radius + 0.01, 200)]).length, 0, what);
                    break;
                case 'circleAtTarget':
                    var c = at(form.range - 0.01, 0);
                    eq(select(form, [c, vec3(c.x + form.radius - 0.01, 0, c.z), vec3(c.x + form.radius + 0.01, 0, c.z)], 0), [0, 1], what);
                    break;
            }
        });
    });
});

test('shapes: the server tolerance only widens the locked-target reach', function () {
    var form = formOf('single', 3, 0, 0);
    eq(selectTargets(form, vec3(0, 0, 0), { x: 0, z: 1 }, [at(3.5, 0)], 0, REACH, 0.75), [0]);
    eq(selectTargets(formOf('circle', 0, 3, 0), vec3(0, 0, 0), { x: 0, z: 1 }, [at(3.5, 0)], -1, REACH, 0.75), []);
});

// ---- Drangue Leap -----------------------------------------------------------------------------

test('leap: only marked ledges, within distance, rise and drop', function () {
    var rules = testData().skills.drangueLeap;
    var from = vec3(0, 0, 0);
    eq(checkLeap(rules, from, vec3(0, 3, 6), false), 'notALedge');
    eq(checkLeap(rules, from, vec3(0, 3, 6), true), 'ok');
    eq(checkLeap(rules, from, vec3(0, 3, rules.maxDistance + 0.01), true), 'tooFar');
    eq(checkLeap(rules, from, vec3(0, rules.maxRise + 0.01, 5), true), 'tooHigh');
    eq(checkLeap(rules, from, vec3(0, -rules.maxDrop - 0.01, 5), true), 'tooLow');
});

test('leap: landing points stay inset on top of a turned ledge', function () {
    var zone = getZone(testData(), 'oak_valley');
    var ledge = zone.ledges.filter(function (l) { return l.name === 'LeapStone Meadow West'; })[0];
    var from = vec3(-40, 0.5, -129);
    var p = ledgeLandingPoint(ledge, from, 0.8);
    near(p.y, ledgeTop(ledge), 1e-6);
    ok(overLedge(ledge, p, 0), 'on the ledge');
    var local = ledgeLocal(ledge, p);
    ok(Math.abs(local.x) <= ledge.size[0] / 2 - 0.8 + 1e-6 && Math.abs(local.z) <= ledge.size[2] / 2 - 0.8 + 1e-6, 'inset from the edges');
    ok(!overLedge(ledge, from, 0));
});

// ---- StatusSet --------------------------------------------------------------------------------

test('statuses: refresh rather than stack; poison ticks until it ends', function () {
    var data = testData();
    var poison = getStatus(data, 'poison');
    var stun = getStatus(data, 'stun');
    var s = new StatusSet();
    s.apply(poison, 3, 5, 'p1', 0);
    s.apply(poison, 2, 4, 'p2', 1);
    eq(s.active.length, 1);
    var total = 0;
    for (var t = 0.5; t <= 4; t += 0.5) total += s.update(t).damage;
    eq(total, 15, '3 ticks of the higher damage');
    eq(s.active.length, 0);
    s.apply(stun, 2.5, 0, 'p1', 10);
    ok(s.isDisabled(11));
    ok(!s.isDisabled(12.5));
});

// ---- SkillState -------------------------------------------------------------------------------

test('skill state: dev start knows every book of the character (and the leap) with 40 points', function () {
    var data = testData();
    var k = startingSkills(data, 'kreshnik');
    eq(keysOf(k.books).sort(), ['bow_book', 'drangue_leap', 'frontier_volley_book', 'sword_book']);
    eq(k.points, 40);
    var z = startingSkills(data, 'zanas_chosen');
    eq(keysOf(z.books).sort(), ['drangue_leap', 'sicas_book', 'staff_book', 'wrath_of_the_zanas_book']);
});

test('skill state: book levels cost the table, forms unlock at 1, 3 and 6', function () {
    var data = testData();
    var state = new SkillState(data, { books: { sword_book: 0 }, points: 5, meterKills: 0 }, 100);
    var book = getBook(data, 'sword_book');
    eq(state.checkCast(book, book.forms[0], 0), 'locked');
    eq(state.levelUp('sword_book'), 'none');
    eq(state.levelUp('sword_book'), 'none');
    eq(state.levelUp('sword_book'), 'none');
    eq(state.bookLevel('sword_book'), 3);
    eq(state.progress.points, 2);
    eq(state.checkCast(book, book.forms[1], 0), 'none');
    eq(state.checkCast(book, book.forms[2], 0), 'locked');
    eq(state.levelUp('sword_book'), 'none'); // level 4 costs 2
    eq(state.levelUp('sword_book'), 'notEnoughPoints');
    eq(state.levelUp('bow_book'), 'notLearned');
});

test('skill state: Spirit, cooldowns and the kill meter', function () {
    var data = testData();
    var state = new SkillState(data, { books: { sword_book: 1 }, points: 0, meterKills: 0 }, 20);
    var book = getBook(data, 'sword_book');
    var strike = book.forms[0];
    eq(state.checkCast(book, strike, 0), 'none');
    state.commitCast(strike, 0);
    eq(state.spirit, 12);
    eq(state.checkCast(book, strike, 1), 'cooldown');
    eq(state.checkCast(book, strike, strike.cooldown), 'none');
    state.commitCast(strike, 10);
    eq(state.checkCast(book, strike, 20), 'noSpirit');
    state.regenerate(2);
    eq(state.spirit, 4 + 2 * data.combat.player.spiritRegenPerSecond);
    for (var i = 0; i < data.skills.skillPoints.killsPerPoint - 1; i++) ok(!state.addKill());
    ok(state.addKill());
    eq(state.progress.points, 1);
    eq(state.progress.meterKills, 0);
});
