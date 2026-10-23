// Shared by integration.mjs and bot.mjs: plain HTTP + WebSocket access to the Nakama module
// (Node 22+: global fetch and WebSocket, no dependencies).

export const BASE = process.env.NAKAMA_URL || 'http://127.0.0.1:17350';
export const SERVER_KEY = process.env.NAKAMA_SERVER_KEY || 'defaultkey';
export const Op = { Move: 1, Target: 2, Cast: 3, Leap: 4, Action: 5, Welcome: 100, Snapshot: 101, Events: 102, Self: 103, Drops: 104, Result: 105, Correction: 106, Notice: 107 };

export const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

export async function http(path, { method = 'POST', token, body, basic } = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers.Authorization = 'Bearer ' + token;
    if (basic) headers.Authorization = 'Basic ' + Buffer.from(basic + ':').toString('base64');
    const res = await fetch(BASE + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await res.text();
    const json = text ? JSON.parse(text) : {};
    if (!res.ok) { const e = new Error(`${path}: ${res.status} ${json.message || text}`); e.status = res.status; e.code = json.code; throw e; }
    return json;
}

export async function login(deviceId) {
    const session = await http('/v2/account/authenticate/device?create=true', { basic: SERVER_KEY, body: { id: deviceId } });
    return session.token;
}

/** RPCs with a session take the payload as a JSON string in the body. */
export async function rpc(token, id, payload) {
    const res = await http('/v2/rpc/' + id, { token, body: JSON.stringify(payload || {}) });
    return res.payload ? JSON.parse(res.payload) : {};
}

/** A player connected over the realtime socket, keeping its own view of the zone from the server's messages. */
export class Client {
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

export async function waitFor(fn, ms, what) {
    const end = Date.now() + ms;
    while (Date.now() < end) {
        if (fn()) return true;
        await sleep(20);
    }
    throw new Error('timed out waiting for ' + what);
}
