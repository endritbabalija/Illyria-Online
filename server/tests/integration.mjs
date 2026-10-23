// End-to-end check against a running Nakama (docker-compose.test.yml): two devices log in, make
// characters, join the same Oak Valley channel, see each other, walk to the same Grey Wolf and kill
// it together; then their saves and ledger rows are read back.
//
//   docker compose -f server/docker-compose.test.yml up -d --build --wait
//   npm --prefix server run test:integration      (NAKAMA_URL defaults to http://127.0.0.1:17350)
//   docker compose -f server/docker-compose.test.yml down
//
// Node 22+ (global fetch and WebSocket), no dependencies.

const BASE = process.env.NAKAMA_URL || 'http://127.0.0.1:17350';
const SERVER_KEY = process.env.NAKAMA_SERVER_KEY || 'defaultkey';
const Op = { Move: 1, Target: 2, Cast: 3, Leap: 4, Action: 5, Welcome: 100, Snapshot: 101, Events: 102, Self: 103, Drops: 104, Result: 105, Correction: 106, Notice: 107 };

let failures = 0;
function check(condition, what) {
    console.log((condition ? '  ok   ' : '  FAIL ') + what);
    if (!condition) failures++;
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function http(path, { method = 'POST', token, body, basic } = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers.Authorization = 'Bearer ' + token;
    if (basic) headers.Authorization = 'Basic ' + Buffer.from(basic + ':').toString('base64');
    const res = await fetch(BASE + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await res.text();
    const json = text ? JSON.parse(text) : {};
    if (!res.ok) { const e = new Error(`${path}: ${res.status} ${json.message || text}`); e.status = res.status; e.code = json.code; throw e; }
    return json;
}

async function login(deviceId) {
    const session = await http('/v2/account/authenticate/device?create=true', { basic: SERVER_KEY, body: { id: deviceId } });
    return session.token;
}

/** RPCs with a session take the payload as a JSON string in the body. */
async function rpc(token, id, payload) {
    const res = await http('/v2/rpc/' + id, { token, body: JSON.stringify(payload || {}) });
    return res.payload ? JSON.parse(res.payload) : {};
}

/** A player connected over the realtime socket, keeping its own view of the zone from the server's messages. */
class Client {
    constructor(name, token) {
        this.name = name;
        this.token = token;
        this.cid = 0;
        this.pending = new Map();
        this.messages = [];
        this.players = new Map();
        this.monsters = new Map();
        this.events = [];
        this.drops = new Map();
        this.dropsSeen = [];
        this.dropsPicked = [];
        this.results = [];
        this.self = null;
        this.you = null;
    }

    connect() {
        return new Promise((resolve, reject) => {
            this.ws = new WebSocket(BASE.replace(/^http/, 'ws') + '/ws?lang=en&status=true&token=' + encodeURIComponent(this.token));
            this.ws.onopen = () => resolve();
            this.ws.onerror = (e) => reject(new Error(this.name + ' socket error ' + (e.message || '')));
            this.ws.onmessage = (event) => this.onMessage(JSON.parse(event.data));
        });
    }

    request(envelope) {
        const cid = String(++this.cid);
        return new Promise((resolve, reject) => {
            this.pending.set(cid, { resolve, reject });
            this.ws.send(JSON.stringify({ cid, ...envelope }));
        });
    }

    onMessage(m) {
        if (m.cid && this.pending.has(m.cid)) {
            const p = this.pending.get(m.cid);
            this.pending.delete(m.cid);
            if (m.error) p.reject(new Error(m.error.message || JSON.stringify(m.error)));
            else p.resolve(m);
            return;
        }
        if (!m.match_data) return;
        const op = Number(m.match_data.op_code);
        const data = JSON.parse(Buffer.from(m.match_data.data || '', 'base64').toString('utf8') || 'null');
        this.messages.push({ op, data });
        switch (op) {
            case Op.Welcome:
                this.you = data.you;
                this.self = data.self;
                data.players.forEach((p) => this.players.set(p.id, p));
                data.monsters.forEach((x) => this.monsters.set(x.id, x));
                break;
            case Op.Snapshot:
                data.p.forEach((p) => this.players.set(p.id, { ...this.players.get(p.id), ...p }));
                data.m.forEach((x) => this.monsters.set(x.id, { ...this.monsters.get(x.id), ...x }));
                break;
            case Op.Events:
                data.e.forEach((e) => {
                    this.events.push(e);
                    if (e.k === 'join') this.players.set(e.player.id, e.player);
                    if (e.k === 'leave') this.players.delete(e.id);
                    if (e.k === 'spawn') this.monsters.set(e.monster.id, e.monster);
                    if (e.k === 'despawn') this.monsters.delete(e.id);
                });
                break;
            case Op.Self: this.self = data; break;
            case Op.Drops:
                data.add.forEach((d) => { this.drops.set(d.id, d); this.dropsSeen.push(d); });
                data.remove.forEach((r) => { this.drops.delete(r.id); if (r.why === 'picked') this.dropsPicked.push(r.id); });
                break;
            case Op.Result: this.results.push(data); break;
            case Op.Correction:
                if (this.me()) Object.assign(this.me(), { x: data.x, y: data.y, z: data.z });
                break;
        }
    }

    send(op, payload) {
        this.ws.send(JSON.stringify({ match_data_send: { match_id: this.matchId, op_code: op, data: Buffer.from(JSON.stringify(payload)).toString('base64') } }));
    }

    async join(matchId, characterId) {
        this.matchId = matchId;
        await this.request({ match_join: { match_id: matchId, metadata: { characterId } } });
        await waitFor(() => this.you !== null, 5000, this.name + ' welcome');
    }

    me() { return this.players.get(this.you); }

    /** One step (≤0.5 m, i.e. 5 m/s at 10 Hz) towards a point; true once within `within` metres. */
    stepToward(x, z, within) {
        const me = this.me();
        const dx = x - me.x, dz = z - me.z, d = Math.hypot(dx, dz);
        if (d <= within) return true;
        const s = Math.min(0.5, d - within * 0.5);
        me.x += (dx / d) * s;
        me.z += (dz / d) * s;
        this.send(Op.Move, { x: me.x, y: me.y, z: me.z, yaw: Math.atan2(dx, dz) * 180 / Math.PI });
        return false;
    }

    close() { try { this.ws.close(); } catch { } }
}

async function waitFor(fn, ms, what) {
    const end = Date.now() + ms;
    while (Date.now() < end) {
        if (fn()) return true;
        await sleep(20);
    }
    throw new Error('timed out waiting for ' + what);
}

async function main() {
    console.log('Nakama at ' + BASE);
    const run = Date.now().toString(36);

    // ---- device login and characters --------------------------------------------------------
    const tokenA = await login('wuxia-it-a-' + run);
    const tokenB = await login('wuxia-it-b-' + run);
    check(!!tokenA && !!tokenB, 'two devices log in');

    const listA = await rpc(tokenA, 'character_list');
    check(listA.characters.length === 0 && listA.available.length === 2, 'a new account has no characters and 2 to choose from');
    const createdA = await rpc(tokenA, 'character_create', { characterId: 'kreshnik', name: 'Alba' });
    check(createdA.character.gold === 100 && createdA.character.weaponType === 'sword', 'Alba the Kreshnik starts with 100 gold and a sword');
    await rpc(tokenB, 'character_create', { characterId: 'zanas_chosen', name: 'Bora' });
    let duplicate = null;
    try { await rpc(tokenA, 'character_create', { characterId: 'kreshnik', name: 'Again' }); } catch (e) { duplicate = e; }
    check(duplicate && duplicate.status === 409, 'a second Kreshnik on the same account is refused (409)');
    let badName = null;
    try { await rpc(tokenA, 'character_create', { characterId: 'zanas_chosen', name: '<b>' }); } catch (e) { badName = e; }
    check(badName && badName.status === 400, 'a bad name is refused (400)');

    // ---- the same channel ---------------------------------------------------------------------
    const joinA = await rpc(tokenA, 'zone_join', { zone: 'oak_valley' });
    const joinB = await rpc(tokenB, 'zone_join', { zone: 'oak_valley' });
    check(joinA.matchId === joinB.matchId, 'both get the same Oak Valley channel (' + joinA.channel + ')');

    const a = new Client('Alba', tokenA), b = new Client('Bora', tokenB);
    await a.connect();
    await b.connect();
    await a.join(joinA.matchId, 'kreshnik');
    await b.join(joinB.matchId, 'zanas_chosen');
    await waitFor(() => a.players.has(b.you) && b.players.has(a.you), 3000, 'seeing each other');
    check(a.players.has(b.you) && b.players.has(a.you), 'they see each other');
    // 30 in a fresh channel; a reused one may have a wolf or two waiting to respawn.
    check(a.monsters.size >= 25 && a.monsters.size <= 30, 'Alba sees the Oak Valley monsters (' + a.monsters.size + ')');
    let online = await rpc(tokenA, 'character_load', { characterId: 'kreshnik' });
    check(online.online === true, 'the save is locked to the match while she plays');
    let refused = null;
    try { await rpc(tokenA, 'character_delete', { characterId: 'kreshnik' }); } catch (e) { refused = e; }
    check(refused && refused.status === 400, 'deleting a character in a zone is refused');

    // ---- walk to the meadow west pack together ----------------------------------------------
    const packCentre = { x: -42, z: -136 };
    for (let i = 0; i < 200; i++) {
        const doneA = a.stepToward(packCentre.x - 1, packCentre.z - 7, 0.5);
        const doneB = b.stepToward(packCentre.x + 1, packCentre.z - 7, 0.5);
        if (doneA && doneB) break;
        await sleep(100);
    }
    const corrections = a.messages.concat(b.messages).filter((m) => m.op === Op.Correction).length;
    check(corrections === 0, 'walking at run speed is never corrected');
    await waitFor(() => Math.abs(b.players.get(a.you).z - a.me().z) < 0.01, 2000, 'Bora seeing Alba arrive');
    check(Math.hypot(b.players.get(a.you).x - a.me().x, b.players.get(a.you).z - a.me().z) < 0.05, "Bora sees Alba where she is");

    // ---- fight the same wolf -----------------------------------------------------------------
    const alive = [...a.monsters.values()].filter((m) => m.pack === 'MeadowWestPack' && m.state !== 'dead');
    alive.sort((m1, m2) => Math.hypot(m1.x - a.me().x, m1.z - a.me().z) - Math.hypot(m2.x - a.me().x, m2.z - a.me().z));
    const wolf = alive[0];
    const goldBefore = { [a.you]: a.self.gold, [b.you]: b.self.gold };
    a.send(Op.Target, { id: wolf.id, rid: 't1' });
    b.send(Op.Target, { id: wolf.id, rid: 't2' });
    let dead = false;
    for (let i = 0; i < 300 && !dead; i++) {
        const w = a.monsters.get(wolf.id);
        if (w) {
            a.stepToward(w.x - 1, w.z, 1.2);
            b.stepToward(w.x + 1, w.z, 1.2);
        }
        dead = a.events.some((e) => e.k === 'die' && e.id === wolf.id);
        await sleep(100);
    }
    await sleep(300);
    check(dead, 'the wolf dies');
    const hitsSeenByA = a.events.filter((e) => e.k === 'hit' && e.dst === wolf.id);
    const hitsSeenByB = b.events.filter((e) => e.k === 'hit' && e.dst === wolf.id);
    check(hitsSeenByA.some((h) => h.src === a.you) && hitsSeenByA.some((h) => h.src === b.you), 'both players hit the same wolf');
    check(JSON.stringify(hitsSeenByA) === JSON.stringify(hitsSeenByB), 'both clients got the same hits from the server (' + hitsSeenByA.length + ')');
    const death = a.events.find((e) => e.k === 'die' && e.id === wolf.id);
    const deathB = b.events.find((e) => e.k === 'die' && e.id === wolf.id);
    check(deathB && deathB.by === death.by, 'and the same kill credit');
    const killer = death.by === a.you ? a : b;
    console.log('       kill credited to ' + killer.name);
    check(killer.self.kills === 1, killer.name + ' has 1 kill');

    // ---- loot: only the credited player gets drops, auto-looted within range ---------------
    const other = killer === a ? b : a;
    check(other.dropsSeen.length === 0, 'the other player gets no loot');
    await sleep(500);
    for (let i = 0; i < 80 && killer.drops.size > 0; i++) {
        const next = [...killer.drops.values()][0];
        killer.stepToward(next.x, next.z, 0.3);
        await sleep(100);
    }
    await sleep(300);
    const drops = killer.dropsSeen;
    if (drops.length > 0) {
        check(drops.every((d) => killer.dropsPicked.includes(d.id)), 'auto-loot picked up all ' + drops.length + ' drop(s)');
        const goldDropped = drops.reduce((n, d) => n + d.gold, 0);
        check(killer.self.gold === goldBefore[killer.you] + goldDropped, 'gold went from ' + goldBefore[killer.you] + ' to ' + killer.self.gold);
    } else console.log('       (nothing dropped this time)');

    // ---- an item action round trip -----------------------------------------------------------
    a.send(Op.Action, { a: 'swap', rid: 'swap1' });
    await waitFor(() => a.results.some((r) => r.rid === 'swap1'), 2000, 'swap result');
    check(a.results.find((r) => r.rid === 'swap1').ok && a.self.stats.weaponType === 'bow', 'Alba swaps to her bow');
    await waitFor(() => b.events.some((e) => e.k === 'gear' && e.id === a.you), 2000, 'gear event');
    check(b.events.some((e) => e.k === 'gear' && e.id === a.you && e.weapon === 'worn_bow'), 'Bora sees the bow');

    // ---- leave, then read the saves back -----------------------------------------------------
    const where = { ...a.me() };
    await a.request({ match_leave: { match_id: a.matchId } });
    await b.request({ match_leave: { match_id: b.matchId } });
    a.close();
    b.close();
    await sleep(500);
    const savedA = await rpc(tokenA, 'character_load', { characterId: 'kreshnik' });
    check(savedA.online === false, 'leaving releases the lock');
    check(savedA.character.location.zone === 'oak_valley' && Math.abs(savedA.character.location.x - where.x) < 0.6 && Math.abs(savedA.character.location.z - where.z) < 0.6,
        'her spot in Oak Valley is saved');
    check(savedA.stats.weaponType === 'bow', 'and her bow is still in hand');
    const savedKiller = killer === a ? savedA : await rpc(tokenB, 'character_load', { characterId: 'zanas_chosen' });
    check(savedKiller.character.kills === 1, 'the kill is saved');

    const ledger = await http('/v2/storage/ledger?limit=100&user_id=' + (killer === a ? a.you : b.you), { method: 'GET', token: killer.token });
    const rows = (ledger.objects || []).map((o) => JSON.parse(o.value));
    check(rows.filter((r) => r.reason === 'starter kit').length === 5, 'the starter kit is in the ledger (5 rows)');
    if (drops.length > 0) check(rows.some((r) => r.reason === 'loot: Grey Wolf'), 'the loot is in the ledger');

    // ---- rejoin puts you back --------------------------------------------------------------
    const again = new Client('Alba', tokenA);
    await again.connect();
    const rejoin = await rpc(tokenA, 'zone_join', { zone: 'oak_valley' });
    await again.join(rejoin.matchId, 'kreshnik');
    check(Math.abs(again.me().x - where.x) < 0.6 && Math.abs(again.me().z - where.z) < 0.6, 'rejoining puts her back where she left');
    again.close();

    console.log(failures === 0 ? 'integration: all checks passed' : 'integration: ' + failures + ' check(s) failed');
    process.exitCode = failures === 0 ? 0 : 1;
}

main().catch((e) => { console.error(e); process.exitCode = 1; });
