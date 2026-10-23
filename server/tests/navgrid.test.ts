// The walkable grid: decoding, straight lines and paths on a small hand-made grid, the real
// Oak Valley export (river, bridge, path length against Unity's NavMesh), and the zone using it.

/** A NavGridFile from rows of heights (row 0 = south, null = blocked), 1 m cells from (0, 0). */
function gridFile(zone: string, rows: (number | null)[][]): NavGridFile {
    var depth = rows.length, width = rows[0].length;
    var bytes: number[] = [];
    for (var z = 0; z < depth; z++)
        for (var x = 0; x < width; x++) {
            var h = rows[z][x];
            var v = h === null ? NAVGRID_BLOCKED : Math.round(h * 100);
            if (v < 0) v += 0x10000;
            bytes.push(v & 0xff, (v >> 8) & 0xff);
        }
    var Buffer = require('buffer').Buffer;
    return {
        version: 1, zone: zone, navMeshSha256: '', cellSize: 1, originX: 0, originZ: 0, width: width, depth: depth,
        encoding: 'int16le-cm-base64', heights: Buffer.from(bytes).toString('base64'),
    };
}

/** A size×size grid at height 0 with blocked cells where `blocked(x, z)` says so. */
function gridOf(zone: string, size: number, blocked: (x: number, z: number) => boolean): NavGridFile {
    var rows: (number | null)[][] = [];
    for (var z = 0; z < size; z++) {
        var row: (number | null)[] = [];
        for (var x = 0; x < size; x++) row.push(blocked(x, z) ? null : 0);
        rows.push(row);
    }
    return gridFile(zone, rows);
}

function pathLength(from: Vec2, path: Vec2[]): number {
    var length = 0, at = from;
    path.forEach(function (p) { length += flatDistance(at, p); at = p; });
    return length;
}

function distanceToSegment(p: Vec2, a: Vec2, b: Vec2): number {
    var dx = b.x - a.x, dz = b.z - a.z, lengthSq = dx * dx + dz * dz;
    var t = lengthSq === 0 ? 0 : clamp(((p.x - a.x) * dx + (p.z - a.z) * dz) / lengthSq, 0, 1);
    return flatDistance(p, { x: a.x + dx * t, z: a.z + dz * t });
}

/** testData() with an extra 20×20 m field zone on `grid`: one wolf pack at (3.5, 0, 3.5), respawn at (14.5, 0, 3.5): in bow range across the middle. */
function fieldData(grid: NavGridFile): GameData {
    var data = testData();
    data.zones.zones.push({
        id: grid.zone, name: 'Test Field', kind: 'field', safe: false, channelCap: 10,
        respawn: { position: [14.5, 0, 3.5], yaw: 0 }, shopkeepers: [], ledges: [], areas: [],
        packs: [{ id: 'TestPack', monster: 'wolf', position: [3.5, 0, 3.5], count: 1, radius: 0 }],
    });
    data.navGrids[grid.zone] = new NavGrid(grid);
    return data;
}

// ---- the grid on its own ----------------------------------------------------------------------

test('navgrid: heights decode, blocked cells read as null', function () {
    var grid = new NavGrid(gridFile('t', [[0, -0.7, null], [10.87, 2.5, 3]]));
    eq(grid.heightAt({ x: 0.5, z: 0.5 }), 0);
    eq(grid.heightAt({ x: 1.2, z: 0.9 }), -0.7);
    eq(grid.heightAt({ x: 2.5, z: 0.5 }), null);
    eq(grid.heightAt({ x: 0.5, z: 1.5 }), 10.87);
    eq(grid.heightAt({ x: -0.1, z: 0.5 }), null, 'outside the grid');
    eq(grid.groundNear({ x: 2.5, z: 0.5 }, 1), -0.7, 'nearest walkable cell');
    throwsError(function () { var f = gridFile('t', [[0]]); f.width = 2; new NavGrid(f); });
});

test('navgrid: straight lines stop at walls; paths go round them', function () {
    // A wall along x = 10 with a gap at the north end (z = 17..19).
    var grid = new NavGrid(gridOf('t', 20, function (x, z) { return x === 10 && z < 17; }));
    ok(!grid.lineWalkable({ x: 5, z: 3 }, { x: 15, z: 3 }));
    ok(grid.lineWalkable({ x: 5, z: 3 }, { x: 5, z: 18 }));
    var stop = grid.walkableUntil({ x: 5, z: 3 }, { x: 15, z: 3 });
    ok(stop.x < 10 && stop.x > 9, 'stops at the wall: ' + stop.x);

    var path = grid.findPath({ x: 5, z: 3 }, { x: 15, z: 3 }, 1000) as Vec2[];
    ok(path !== null, 'found a way round');
    eq(path[path.length - 1], { x: 15, z: 3 }, 'ends at the goal');
    ok(path.some(function (p) { return p.z >= 17; }), 'through the gap');
    var at: Vec2 = { x: 5, z: 3 };
    path.forEach(function (p) { ok(grid.lineWalkable(at, p), 'each leg is walkable'); at = p; });
    ok(path.length <= 4, 'straightened: ' + path.length + ' waypoints');

    eq(grid.findPath({ x: 5, z: 3 }, { x: 15, z: 3 }, 20), null, 'beyond the search budget');
    var closed = new NavGrid(gridOf('t', 20, function (x) { return x === 10; }));
    eq(closed.findPath({ x: 5, z: 3 }, { x: 15, z: 3 }, 100000), null, 'no way through');
});

// ---- the real Oak Valley export -------------------------------------------------------------

var BRIDGE: Vec2 = { x: 10, z: 54 + 9 * Math.sin(10 / 42 + 0.6) + 4 * Math.sin(10 / 17) };

test('navgrid: Oak Valley has its river, crossed only at the bridge', function () {
    var grid = testData().navGrids['oak_valley'];
    ok(grid, 'exported');
    ok(grid.isWalkable({ x: -16, z: -178 }), 'the Han respawn point');
    ok(grid.isWalkable(BRIDGE), 'the bridge deck');
    [-60, 0, 40, 80].forEach(function (x) {
        ok(!grid.lineWalkable({ x: x, z: 20 }, { x: x, z: 90 }), 'no wading across at x = ' + x);
    });
    ok(!grid.isWalkable({ x: 0, z: 300 }) && !grid.isWalkable({ x: 160, z: 0 }), 'nothing outside the valley');
});

test('navgrid: Han to wolf den goes over the bridge, about as long as Unity\'s path', function () {
    var grid = testData().navGrids['oak_valley'];
    var from = { x: -16, z: -178 }, to = { x: 80, z: 166 };
    var path = grid.findPath(from, to, 200000) as Vec2[];
    ok(path !== null);
    var nearBridge = Infinity, at: Vec2 = from;
    path.forEach(function (p) { nearBridge = Math.min(nearBridge, distanceToSegment(BRIDGE, at, p)); at = p; });
    ok(nearBridge < 3, 'crosses at the bridge (' + nearBridge.toFixed(1) + ' m from its centre)');
    // Unity's NavMesh path between the same spots is 386 m (measured when the NavMesh was rebaked).
    near(pathLength(from, path), 386, 386 * 0.06, 'path length');
});

// ---- the zone on a grid -----------------------------------------------------------------------

test('zone: moves into the river or up into the air are corrected', function () {
    var sim = newSim();
    var grid = sim.grid as NavGrid;
    var start = vec3(0, grid.heightAt({ x: 0, z: 40 }) as number, 40);
    var p = join(sim, 'alice', 'kreshnik', start);
    run(sim, 0.5);
    drain(sim);
    var why: string | null = null;
    for (var i = 0; i < 60 && !why; i++) {
        var to = { x: 0, z: p.pos.z + 0.5 };
        var ground = grid.groundNear(to, 1);
        sim.handle('alice', Op.Move, { x: to.x, y: ground === null ? p.pos.y : ground, z: to.z, yaw: 0 });
        sim.step(TICK);
        var c = messagesFor(drain(sim), 'alice', Op.Correction);
        if (c.length > 0) why = c[0].why;
    }
    eq(why, 'blocked');
    ok(grid.groundNear(p.pos, sim.data.zones.movement.gridSlack) !== null, 'she stayed on the bank');

    // Climbing into the air is 'tooSteep' before it's 'height'; sinking under the ground is 'height'.
    sim.handle('alice', Op.Move, { x: p.pos.x, y: p.pos.y + 3, z: p.pos.z - 0.3, yaw: 0 });
    eq(messagesFor(drain(sim), 'alice', Op.Correction)[0].why, 'tooSteep');
    sim.handle('alice', Op.Move, { x: p.pos.x, y: p.pos.y - 2, z: p.pos.z - 0.3, yaw: 0 });
    eq(messagesFor(drain(sim), 'alice', Op.Correction)[0].why, 'height');
});

test('zone: monsters stand on the grid\'s ground', function () {
    var sim = newSim();
    var grid = sim.grid as NavGrid;
    run(sim, 10);
    sim.monsters.forEach(function (m) {
        ok(grid.groundNear(m.pos, 1.5) !== null, m.id + ' on walkable ground');
        near(m.pos.y, grid.groundNear(m.pos, 1.5) as number, 1e-9, m.id + ' at ground height');
    });
});

test('zone: a monster walks round a wall to reach you', function () {
    var data = fieldData(gridOf('test_field', 20, function (x, z) { return x === 10 && z < 17; }));
    var sim = new ZoneSim(data, 'test_field', { rng: seededRng(3), nextItemId: counterIds() });
    var wolf = sim.monsters[0];
    var p = sim.addPlayer('alice', 's', 'alice', createCharacter(data, 'kreshnik', 'Alba', counterIds(), 0).save);
    p.items.swapWeapon();
    sim.step(TICK);
    sim.handle('alice', Op.Target, { id: wolf.id });
    var messages: OutMessage[] = [], maxZ = 0, offGrid = false;
    runUntil(sim, 20, function () { return eventsOf(messages, 'hit').some(function (h) { return h.dst === 'alice'; }); }, function () {
        // One arrow to provoke it, then let it come.
        if (wolf.state === 'chase' && p.target) sim.handle('alice', Op.Target, { id: null });
        if (!(sim.grid as NavGrid).isWalkable(wolf.pos)) offGrid = true;
        maxZ = Math.max(maxZ, wolf.pos.z);
        messages = messages.concat(drain(sim));
    });
    ok(eventsOf(messages, 'hit').some(function (h) { return h.dst === 'alice'; }), 'it got to her and bit');
    ok(maxZ >= 16.5, 'through the gap at the north end (max z ' + maxZ.toFixed(1) + ')');
    ok(!offGrid, 'never walked through the wall');
});

test('zone: a monster with no way to you gives up and goes home', function () {
    var data = fieldData(gridOf('test_field', 20, function (x) { return x === 10; }));
    var sim = new ZoneSim(data, 'test_field', { rng: seededRng(3), nextItemId: counterIds() });
    var wolf = sim.monsters[0];
    var p = sim.addPlayer('alice', 's', 'alice', createCharacter(data, 'kreshnik', 'Alba', counterIds(), 0).save);
    p.items.swapWeapon();
    sim.step(TICK);
    sim.handle('alice', Op.Target, { id: wolf.id });
    var states: { [s: string]: boolean } = {};
    var messages: OutMessage[] = [];
    run(sim, 4, function () {
        if (wolf.state === 'chase' && p.target) sim.handle('alice', Op.Target, { id: null });
        states[wolf.state] = true;
        messages = messages.concat(drain(sim));
    });
    ok(eventsOf(messages, 'hit').some(function (h) { return h.src === 'alice' && h.dst === wolf.id; }), 'it was provoked');
    ok(states['return'], 'and gave up');
    ok(!states['chase'], 'never kept chasing a target it cannot reach');
    ok(wolf.pos.x < 10, 'without crossing the wall');
    eq(eventsOf(messages, 'hit').filter(function (h) { return h.dst === 'alice'; }).length, 0);
});
