// ZoneSim: one zone channel (the town or a hunting zone) as a pure simulation with time passed
// in. It owns the players' live state, the monsters and the loot, decides every hit, and hands
// back the messages to send and the saves to write. match.ts runs it inside a Nakama match;
// the tests run it directly. Positions use Unity axes (Y up).

type MonsterState = 'idle' | 'chase' | 'return' | 'dead';

interface OutMessage {
    op: number;
    data: any;
    /** User ids to send to; null = everyone in the match. */
    to: string[] | null;
}

interface QueuedCast { form: string; target: string | null; dir: Vec2 | null; at: number; }

interface LeapInProgress { to: Vec3; endAt: number; ledge: LedgeData | null; }

interface SimPlayer {
    id: string;
    sessionId: string;
    username: string;
    save: CharacterSave;
    items: Belongings;
    skills: SkillState;
    stats: CharacterStats;
    pos: Vec3;
    yaw: number;
    onLedge: LedgeData | null;
    /** Moves are checked against where the player was at the end of an earlier tick. */
    moveAnchor: { pos: Vec3; t: number };
    movedThisTick: boolean;
    hp: number;
    lastDamagedAt: number;
    regenCarry: number;
    statuses: StatusSet;
    dead: boolean;
    respawnAt: number;
    target: string | null;
    nextAttackAt: number;
    castEnd: number;
    queued: QueuedCast | null;
    leap: LeapInProgress | null;
    /** Drops already told "Your bag is full", so it shows once per drop. */
    bagFullTold: { [dropId: string]: boolean };
    itemsVersion: number;
    /** The worn weapon's item id when stats were last computed. */
    lastWeaponId: string | null;
    selfDirty: boolean;
    saveDirty: boolean;
    pendingLedger: LedgerEntry[];
    lastSaveAt: number;
    sent: string;
}

interface SimMonster {
    id: string;
    data: MonsterData;
    pack: PackData;
    home: Vec3;
    pos: Vec3;
    yaw: number;
    hp: number;
    statuses: StatusSet;
    state: MonsterState;
    target: string | null;
    dest: Vec3 | null;
    /** Waypoints around obstacles towards `pathFor` (grid zones only), recalculated now and then. */
    path: Vec2[] | null;
    pathFor: Vec2 | null;
    pathAt: number;
    nextWanderAt: number;
    nextAttackAt: number;
    despawnAt: number;
    despawned: boolean;
    respawnAt: number;
    /** Damage dealt by each player since this spawn; the top dealer gets the kill and the loot. */
    damageBy: { [playerId: string]: number };
    lastAttacker: string | null;
    sent: string;
}

interface SimDrop {
    id: string;
    owner: string;
    pos: Vec3;
    gold: number;
    item: ItemInstance | null;
    droppedAt: number;
    /** The monster's name, for the ledger ("loot: Grey Wolf"). */
    source: string;
}

interface SaveRequest {
    userId: string;
    characterId: string;
    save: CharacterSave;
    ledger: LedgerEntry[];
}

interface ZoneSimOptions {
    rng?: Rng;
    /** Unique ids for new item instances (nk.uuidv4 in a match). */
    nextItemId: () => string;
    log?: (message: string) => void;
}

class ZoneSim {
    data: GameData;
    zone: ZoneData;
    /** The zone's walkable ground, if it has one exported (the town doesn't yet). */
    grid: NavGrid | null;
    rng: Rng;
    factory: ItemFactory;
    log: (message: string) => void;
    /** Seconds since the match started. */
    now = 0;
    players: { [userId: string]: SimPlayer } = {};
    monsters: SimMonster[] = [];
    drops: SimDrop[] = [];
    events: any[] = [];
    out: OutMessage[] = [];
    private nextDropId = 1;

    constructor(data: GameData, zoneId: string, options: ZoneSimOptions) {
        this.data = data;
        this.zone = getZone(data, zoneId);
        this.grid = data.navGrids[zoneId] || null;
        this.rng = options.rng || mathRng();
        this.factory = new ItemFactory(data, options.nextItemId);
        this.log = options.log || function () { };
        this.spawnPacks();
    }

    playerCount(): number { return keysOf(this.players).length; }

    // ================================================================ players joining and leaving

    /** Adds a player with their loaded save, sends them the zone and tells everyone else. */
    addPlayer(userId: string, sessionId: string, username: string, saved: CharacterSave): SimPlayer {
        var items = new Belongings(this.data, this.factory, saved.characterId, saved.belongings);
        var stats = items.computeStats();
        var respawn = this.respawnPoint();
        var loc = saved.location;
        var alive = saved.hp === null || saved.hp > 0;
        var pos = loc && loc.zone === this.zone.id && alive ? vec3(loc.x, loc.y, loc.z) : respawn.pos;
        var yaw = loc && loc.zone === this.zone.id && alive ? loc.yaw : respawn.yaw;
        var hp = alive && saved.hp !== null ? Math.min(saved.hp, stats.maxHp) : stats.maxHp;
        var p: SimPlayer = {
            id: userId,
            sessionId: sessionId,
            username: username,
            save: saved,
            items: items,
            skills: new SkillState(this.data, saved.skills, stats.maxSpirit, alive && saved.spirit !== null ? saved.spirit : stats.maxSpirit),
            stats: stats,
            pos: pos,
            yaw: yaw,
            onLedge: null,
            moveAnchor: { pos: { x: pos.x, y: pos.y, z: pos.z }, t: this.now },
            movedThisTick: false,
            hp: hp,
            lastDamagedAt: -Infinity,
            regenCarry: 0,
            statuses: new StatusSet(),
            dead: false,
            respawnAt: 0,
            target: null,
            nextAttackAt: 0,
            castEnd: 0,
            queued: null,
            leap: null,
            bagFullTold: {},
            itemsVersion: items.version,
            lastWeaponId: items.weapon ? items.weapon.id : null,
            selfDirty: false,
            saveDirty: true,
            pendingLedger: [],
            lastSaveAt: -Infinity,
            sent: '',
        };
        // Standing on a ledge when last saved: put them back on it.
        for (var i = 0; i < this.zone.ledges.length; i++) {
            var ledge = this.zone.ledges[i];
            if (overLedge(ledge, pos, 0) && Math.abs(pos.y - ledgeTop(ledge)) < 0.5) p.onLedge = ledge;
        }
        this.players[userId] = p;
        p.sent = this.playerSignature(p);

        this.send(Op.Welcome, {
            you: userId,
            zone: this.zone.id,
            t: q2(this.now),
            self: this.selfState(p),
            players: this.allPlayers().map(this.publicPlayer, this),
            monsters: this.monsters.filter(function (m) { return !m.despawned; }).map(this.publicMonster, this),
            drops: [],
        }, [userId]);
        this.event({ k: 'join', player: this.publicPlayer(p) });
        this.log('[Zone] ' + saved.name + ' (' + saved.characterId + ') joined ' + this.zone.name);
        return p;
    }

    /** Removes a player and returns their final save (session lock released). */
    removePlayer(userId: string): SaveRequest | null {
        var p = this.players[userId];
        if (!p) return null;
        for (var i = 0; i < this.monsters.length; i++)
            if (this.monsters[i].target === userId && this.monsters[i].state === 'chase') this.startReturn(this.monsters[i]);
        this.drops = this.drops.filter(function (d) { return d.owner !== userId; });
        var request = this.saveRequest(p, null, 0);
        delete this.players[userId];
        this.event({ k: 'leave', id: userId });
        this.log('[Zone] ' + p.save.name + ' left ' + this.zone.name);
        return request;
    }

    allPlayers(): SimPlayer[] {
        var list: SimPlayer[] = [];
        for (var id in this.players) list.push(this.players[id]);
        return list;
    }

    private respawnPoint(): { pos: Vec3; yaw: number } {
        return { pos: vec3From(this.zone.respawn.position), yaw: this.zone.respawn.yaw };
    }

    // ================================================================ incoming messages

    handle(userId: string, op: number, payload: any): void {
        var p = this.players[userId];
        if (!p || !payload || typeof payload !== 'object') return;
        switch (op) {
            case Op.Move: this.onMove(p, payload); break;
            case Op.Target: this.onTarget(p, payload); break;
            case Op.Cast: this.onCast(p, payload); break;
            case Op.Leap: this.onLeap(p, payload); break;
            case Op.Action: this.onAction(p, payload); break;
        }
    }

    private onMove(p: SimPlayer, msg: any): void {
        if (!isFiniteNumber(msg.x) || !isFiniteNumber(msg.y) || !isFiniteNumber(msg.z)) return;
        if (p.dead || p.leap) return;
        var to = vec3(msg.x, msg.y, msg.z);
        var why = this.checkMove(p, to);
        if (why) {
            this.send(Op.Correction, { x: q2(p.pos.x), y: q2(p.pos.y), z: q2(p.pos.z), why: why }, [p.id]);
            return;
        }
        if (p.onLedge && (!overLedge(p.onLedge, to, 0.3) || to.y < ledgeTop(p.onLedge) - 1)) p.onLedge = null;
        p.pos = to;
        if (isFiniteNumber(msg.yaw)) p.yaw = msg.yaw;
        p.movedThisTick = true;
    }

    /** Null if the move is allowed, else why not. */
    checkMove(p: SimPlayer, to: Vec3): string | null {
        var rules = this.data.zones.movement;
        var from = p.moveAnchor.pos;
        var horizontal = flatDistance(from, to);
        if (p.statuses.isDisabled(this.now) && horizontal > rules.distanceSlack) return 'disabled';
        var elapsed = Math.min(Math.max(0, this.now - p.moveAnchor.t), rules.maxGapSeconds);
        var allowed = this.data.combat.player.moveSpeed * rules.speedTolerance * elapsed + rules.distanceSlack;
        if (horizontal > allowed) return 'tooFast';
        if (to.y - from.y > horizontal * rules.maxSlope + rules.stepUp) return 'tooSteep';
        // Ledges are reached by Drangue Leap only, never by walking up them.
        for (var i = 0; i < this.zone.ledges.length; i++) {
            var ledge = this.zone.ledges[i];
            if (ledge === p.onLedge) continue;
            var bottom = ledge.centre[1] - ledge.size[1] * 0.5;
            if (overLedge(ledge, to, 0) && to.y > bottom + rules.ledgeClimbHeight) return 'ledge';
        }
        // Walls, the river and anything else off the NavMesh: only ground the zone's grid calls
        // walkable. A ledge top (reached by leaping) is above the grid, so standing on one is exempt.
        var grid = this.grid;
        if (grid && !(p.onLedge && overLedge(p.onLedge, to, 0.3))) {
            var ground = grid.groundNear(to, rules.gridSlack);
            if (ground === null) return 'blocked';
            if (to.y < ground - rules.heightTolerance) return 'height';
            // Above the ground only while coming down (stepping off a ledge).
            if (to.y > ground + rules.heightTolerance && to.y > from.y + 0.05) return 'height';
        }
        return null;
    }

    private onTarget(p: SimPlayer, msg: any): void {
        if (p.dead) return this.result(p, 'target', msg.rid, 'dead');
        if (msg.nearest) {
            var current = p.target ? this.findMonster(p.target) : null;
            var picked = current && this.canTarget(p, current) ? current : this.nearestMonster(p, this.data.combat.player.lockRange);
            if (!picked) return this.result(p, 'target', msg.rid, 'noTarget');
            this.setTarget(p, picked.id);
            return this.result(p, 'target', msg.rid, null, { id: picked.id });
        }
        if (msg.id === null || msg.id === undefined || msg.id === '') {
            this.setTarget(p, null);
            return this.result(p, 'target', msg.rid, null, { id: null });
        }
        var m = this.findMonster(String(msg.id));
        if (!m || !this.canTarget(p, m)) return this.result(p, 'target', msg.rid, 'noTarget');
        this.setTarget(p, m.id);
        this.result(p, 'target', msg.rid, null, { id: m.id });
    }

    private canTarget(p: SimPlayer, m: SimMonster): boolean {
        return m.state !== 'dead' && flatDistance(p.pos, m.pos) <= this.data.combat.player.loseTargetRange;
    }

    private setTarget(p: SimPlayer, id: string | null): void {
        if (p.target === id) return;
        p.target = id;
        this.send(Op.Notice, { code: 'target', id: id }, [p.id]);
    }

    nearestMonster(p: SimPlayer, range: number): SimMonster | null {
        var best: SimMonster | null = null, bestDistance = range;
        for (var i = 0; i < this.monsters.length; i++) {
            var m = this.monsters[i];
            if (m.state === 'dead') continue;
            var d = flatDistance(p.pos, m.pos);
            if (d > bestDistance) continue;
            bestDistance = d;
            best = m;
        }
        return best;
    }

    findMonster(id: string): SimMonster | null {
        for (var i = 0; i < this.monsters.length; i++) if (this.monsters[i].id === id) return this.monsters[i];
        return null;
    }

    // ================================================================ skill forms

    private onCast(p: SimPlayer, msg: any): void {
        var dir = parseDir(msg.dir);
        var cast: QueuedCast = { form: String(msg.form), target: msg.target ? String(msg.target) : null, dir: dir, at: this.now };
        var block = this.castBlock(p, cast);
        if (block) return this.result(p, 'cast', msg.rid, block);
        if (this.now < p.castEnd) {
            // Pressing while a form plays replaces whatever was queued.
            p.queued = cast;
            return this.result(p, 'cast', msg.rid, null, { queued: true });
        }
        this.fireCast(p, cast, msg.rid);
    }

    /** Why a cast can't start now (null = it can). */
    private castBlock(p: SimPlayer, cast: QueuedCast): string | null {
        if (this.zone.safe) return 'safeZone';
        if (p.dead) return 'dead';
        if (p.statuses.isDisabled(this.now)) return 'disabled';
        if (p.leap) return 'busy';
        var found = findForm(this.data, cast.form);
        if (!found) return 'badRequest';
        if (found.book.weapon !== p.stats.weaponType) return 'wrongWeapon';
        var block = p.skills.checkCast(found.book, found.form, this.now);
        if (block !== 'none') return block;
        return null;
    }

    private fireCast(p: SimPlayer, cast: QueuedCast, rid: any): boolean {
        var found = findForm(this.data, cast.form) as { book: BookData; form: FormData };
        var form = found.form;
        var tolerance = this.data.zones.server.rangeTolerance;
        var reach = p.stats.attack.reachHeight;
        var target: SimMonster | null = null;
        if (needsTarget(form)) {
            var targetId = cast.target || p.target;
            target = targetId ? this.findMonster(targetId) : null;
            if (!target || target.state === 'dead') {
                this.result(p, 'cast', rid, 'noTarget');
                return false;
            }
            if (!formInReach(form, p.pos, target.pos, reach, tolerance)) {
                this.result(p, 'cast', rid, 'outOfRange');
                return false;
            }
            this.setTarget(p, target.id);
        } else if (cast.target) {
            var aimed = this.findMonster(cast.target);
            if (aimed && aimed.state !== 'dead') target = aimed;
        }

        var facing: Vec2 = target ? (flatDirection(p.pos, target.pos) || yawToDirection(p.yaw)) : (cast.dir || yawToDirection(p.yaw));
        p.yaw = directionToYaw(facing);
        p.skills.commitCast(form, this.now);
        p.castEnd = this.now + form.castTime;
        p.nextAttackAt = Math.max(p.nextAttackAt, p.castEnd);

        var candidates: SimMonster[] = this.monsters.filter(function (m) { return m.state !== 'dead'; });
        var lockedIndex = target ? indexOf(candidates, target) : -1;
        var hits = selectTargets(form, p.pos, facing, candidates.map(function (m) { return m.pos; }), lockedIndex, reach, tolerance);
        var level = p.skills.bookLevel(found.book.id);
        this.event({ k: 'cast', src: p.id, form: form.id, tgt: target ? target.id : null, dir: [q2(facing.x), q2(facing.z)], n: hits.length });
        for (var i = 0; i < hits.length; i++) this.skillHit(p, candidates[hits[i]], form, level);
        this.result(p, 'cast', rid, null, { form: form.id, hits: hits.length });
        p.selfDirty = true;
        return true;
    }

    private skillHit(p: SimPlayer, m: SimMonster, form: FormData, bookLevel: number): void {
        var perLevel = this.data.skills.skillPoints.damagePerBookLevel;
        var damage = rollSkillDamage(form, bookLevel, perLevel, m.data.defense, this.rng, p.stats.skillDamage);
        this.damageMonster(m, p.id, { hit: true, crit: false, damage: damage, kind: 'skill' });
        var status = form.status;
        if (status && status.id && m.state !== 'dead') {
            var data = getStatus(this.data, status.id);
            if (statusLands(status.chance, resistFor(m.data.resist, data.resist), this.rng)) {
                m.statuses.apply(data, status.duration, status.tickDamage, p.id, this.now);
                this.event({ k: 'status', id: m.id, s: data.id, dur: status.duration });
            }
        }
    }

    private processQueue(p: SimPlayer): void {
        var q = p.queued;
        if (!q || this.now < p.castEnd || p.dead) return;
        if (this.now - q.at > this.data.zones.playerRules.castQueueTimeout) { p.queued = null; return; }
        if (p.statuses.isDisabled(this.now) || p.leap) return;
        var block = this.castBlock(p, q);
        if (block) {
            p.queued = null;
            return this.result(p, 'cast', null, block);
        }
        var found = findForm(this.data, q.form) as { book: BookData; form: FormData };
        if (needsTarget(found.form)) {
            var target = this.findMonster(q.target || p.target || '');
            if (!target || target.state === 'dead') { p.queued = null; return this.result(p, 'cast', null, 'noTarget'); }
            // Still running in: wait (the timeout drops it).
            if (!formInReach(found.form, p.pos, target.pos, p.stats.attack.reachHeight, this.data.zones.server.rangeTolerance)) return;
        }
        p.queued = null;
        this.fireCast(p, q, null);
    }

    // ================================================================ Drangue Leap

    private onLeap(p: SimPlayer, msg: any): void {
        if (p.dead) return this.result(p, 'leap', msg.rid, 'dead');
        if (p.statuses.isDisabled(this.now)) return this.result(p, 'leap', msg.rid, 'disabled');
        if (p.leap) return this.result(p, 'leap', msg.rid, 'busy');
        var block = p.skills.checkLeap(this.now);
        if (block !== 'none') return this.result(p, 'leap', msg.rid, block);
        var rules = this.data.skills.drangueLeap;
        var aim = parseDir(msg.dir) || yawToDirection(p.yaw);
        var to: Vec3, ledge: LedgeData | null = null, duration: number;

        if (msg.ledge) {
            ledge = null;
            for (var i = 0; i < this.zone.ledges.length; i++) if (this.zone.ledges[i].name === msg.ledge) ledge = this.zone.ledges[i];
            if (!ledge) return this.result(p, 'leap', msg.rid, 'notALedge');
            to = ledgeLandingPoint(ledge, p.pos, rules.landingInset);
            var distance = flatDistance(p.pos, to);
            if (distance < 1 && Math.abs(to.y - p.pos.y) < 0.5) return this.result(p, 'leap', msg.rid, 'alreadyThere');
            var offset = { x: to.x - p.pos.x, z: to.z - p.pos.z };
            if (distance >= 0.001 && flatAngle(aim, offset) > rules.aimAngle * 0.5) return this.result(p, 'leap', msg.rid, 'notInAim');
            var check = checkLeap(rules, p.pos, to, true);
            if (check !== 'ok') return this.result(p, 'leap', msg.rid, check);
            duration = rules.leapDuration;
        } else {
            to = vec3(p.pos.x + aim.x * rules.dashDistance, p.pos.y, p.pos.z + aim.z * rules.dashDistance);
            // A dash stops at the first wall in its way.
            if (this.grid && !p.onLedge) {
                var stop = this.grid.walkableUntil(p.pos, to);
                var ground = this.grid.groundNear(stop, 1);
                to = vec3(stop.x, ground !== null ? ground : p.pos.y, stop.z);
            }
            duration = rules.dashDuration;
        }

        p.skills.commitLeap(this.now);
        p.leap = { to: to, endAt: this.now + duration, ledge: ledge };
        p.queued = null;
        p.yaw = directionToYaw(aim);
        this.event({ k: 'leap', id: p.id, from: [q2(p.pos.x), q2(p.pos.y), q2(p.pos.z)], to: [q2(to.x), q2(to.y), q2(to.z)], dur: duration, ledge: ledge ? ledge.name : null });
        this.result(p, 'leap', msg.rid, null, { to: [q2(to.x), q2(to.y), q2(to.z)], ledge: ledge ? ledge.name : null });
        if (ledge) this.log('[Leap] ' + p.save.name + ' landing on ' + ledge.name);
        p.selfDirty = true;
    }

    private finishLeap(p: SimPlayer): void {
        if (!p.leap || this.now < p.leap.endAt) return;
        p.pos = p.leap.to;
        p.onLedge = p.leap.ledge;
        p.leap = null;
        p.moveAnchor = { pos: { x: p.pos.x, y: p.pos.y, z: p.pos.z }, t: this.now };
    }

    // ================================================================ item and book actions

    private onAction(p: SimPlayer, msg: any): void {
        var a = String(msg.a || '');
        var rid = msg.rid;
        var items = p.items;
        var error: string | null = null;
        var extra: any = undefined;
        var block: string;
        switch (a) {
            case 'equip':
                block = items.equip(String(msg.item));
                if (block !== 'none') error = block;
                break;
            case 'unequip':
                block = items.unequip(String(msg.slot));
                if (block !== 'none') error = block;
                break;
            case 'swap':
                if (p.dead) { error = 'dead'; break; }
                block = items.swapWeapon();
                if (block !== 'none') error = block;
                break;
            case 'moveItem':
                if (!items.move(Number(msg.from), Number(msg.to))) error = 'badSlot';
                break;
            case 'quickSlot':
                if (!items.assignQuickSlot(Number(msg.slot), String(msg.item))) error = 'badSlot';
                break;
            case 'use':
                block = items.use(String(msg.item), this.now, this.vitals(p));
                if (block !== 'none') error = block;
                break;
            case 'quickUse':
                block = items.useQuickSlot(Number(msg.slot), this.now, this.vitals(p));
                if (block !== 'none') error = block;
                break;
            case 'buy':
                var keeper = this.shopkeeperNear(p, String(msg.shop));
                if (!keeper) { error = 'noShop'; break; }
                block = items.buy(keeper.shop, String(msg.item), msg.count === undefined ? 1 : Number(msg.count));
                if (block !== 'none') error = block;
                break;
            case 'sell':
                if (!this.shopkeeperNear(p, null)) { error = 'noShop'; break; }
                block = items.sell(String(msg.item), msg.count === undefined ? 1 : Number(msg.count));
                if (block !== 'none') error = block;
                break;
            case 'levelBook':
                block = p.skills.levelUp(String(msg.book));
                if (block !== 'none') error = block;
                else {
                    extra = { book: String(msg.book), level: p.skills.bookLevel(String(msg.book)) };
                    p.saveDirty = true;
                    p.selfDirty = true;
                }
                break;
            default:
                error = 'badRequest';
        }
        this.afterItemsChanged(p);
        this.result(p, a, rid, error, extra);
    }

    /** A shopkeeper of that shop (any shop for null) within the shop's leave range. */
    private shopkeeperNear(p: SimPlayer, shopId: string | null): ShopkeeperData | null {
        var range = this.data.zones.playerRules.shopLeaveRange;
        for (var i = 0; i < this.zone.shopkeepers.length; i++) {
            var k = this.zone.shopkeepers[i];
            if (shopId !== null && k.shop !== shopId) continue;
            if (flatDistance(p.pos, vec3From(k.position)) <= range) return k;
        }
        return null;
    }

    private vitals(p: SimPlayer): Vitals {
        return {
            isDead: function () { return p.dead; },
            hp: function () { return p.hp; },
            maxHp: function () { return p.stats.maxHp; },
            spirit: function () { return p.skills.spirit; },
            maxSpirit: function () { return p.skills.maxSpirit; },
            restoreHp: function (amount: number) { p.hp = Math.min(p.stats.maxHp, p.hp + amount); },
            restoreSpirit: function (amount: number) { p.skills.addSpirit(amount); },
        };
    }

    /** Recomputes stats after gear changed, moves new ledger entries to the save queue. */
    private afterItemsChanged(p: SimPlayer): void {
        var ledger = p.items.drainLedger();
        for (var i = 0; i < ledger.length; i++) {
            p.pendingLedger.push(ledger[i]);
            this.log(describeLedger(this.data, ledger[i]));
        }
        if (p.items.version === p.itemsVersion) return;
        p.itemsVersion = p.items.version;
        p.selfDirty = true;
        p.saveDirty = true;
        var oldWeapon = p.stats.weaponType;
        p.stats = p.items.computeStats();
        if (p.hp > p.stats.maxHp) p.hp = p.stats.maxHp;
        p.skills.setMaxSpirit(p.stats.maxSpirit);
        var weapon = p.items.weapon;
        if (weapon && p.lastWeaponId !== weapon.id) {
            // A short draw time so swapping can't be used to skip the attack interval.
            p.nextAttackAt = Math.max(p.nextAttackAt, this.now + this.data.zones.playerRules.weaponDrawTime);
            this.event({ k: 'gear', id: p.id, weapon: weapon.def, weaponType: p.stats.weaponType });
            if (oldWeapon !== p.stats.weaponType) p.queued = null;
            p.lastWeaponId = weapon.id;
        }
    }

    // ================================================================ the tick

    /** Advances the zone by dt seconds. */
    step(dt: number): void {
        this.now += dt;
        var players = this.allPlayers();
        for (var i = 0; i < players.length; i++) this.updatePlayer(players[i], dt);
        for (var m = 0; m < this.monsters.length; m++) this.updateMonster(this.monsters[m], dt);
        this.separateMonsters();
        this.updateDrops();
        for (i = 0; i < players.length; i++) {
            var p = players[i];
            if (!this.players[p.id]) continue;
            this.afterItemsChanged(p);
            if (p.movedThisTick) {
                p.moveAnchor = { pos: { x: p.pos.x, y: p.pos.y, z: p.pos.z }, t: this.now };
                p.movedThisTick = false;
            }
        }
        this.flush();
    }

    private updatePlayer(p: SimPlayer, dt: number): void {
        var combat = this.data.combat.player;
        if (p.dead) {
            if (this.now >= p.respawnAt) this.revive(p);
            return;
        }
        this.finishLeap(p);

        var tick = p.statuses.update(this.now);
        if (tick.damage > 0) this.damagePlayer(p, tick.source, { hit: true, crit: false, damage: tick.damage, kind: 'poison' });
        if (p.dead) return;

        if (p.target) {
            var t = this.findMonster(p.target);
            if (!t || t.state === 'dead' || flatDistance(p.pos, t.pos) > combat.loseTargetRange) this.setTarget(p, null);
        }

        this.processQueue(p);

        if (p.target && !p.leap && !p.statuses.isDisabled(this.now) && this.now >= p.nextAttackAt && this.now >= p.castEnd) {
            var target = this.findMonster(p.target) as SimMonster;
            var a = p.stats.attack;
            if (flatDistance(p.pos, target.pos) <= a.range + this.data.zones.server.rangeTolerance && Math.abs(target.pos.y - p.pos.y) <= a.reachHeight) {
                p.nextAttackAt = this.now + a.interval;
                var dir = flatDirection(p.pos, target.pos);
                if (dir) p.yaw = directionToYaw(dir);
                this.damageMonster(target, p.id, rollBasicAttack(a, target.data.defense, target.data.dodge, this.rng));
            }
        }

        if (this.now - p.lastDamagedAt >= combat.regenDelay && p.hp < p.stats.maxHp) {
            p.regenCarry += combat.regenPerSecond * dt;
            var whole = Math.floor(p.regenCarry);
            if (whole > 0) {
                p.regenCarry -= whole;
                p.hp = Math.min(p.stats.maxHp, p.hp + whole);
            }
        }
        p.skills.regenerate(dt);
    }

    private damagePlayer(p: SimPlayer, sourceId: string | null, result: AttackResult): void {
        if (p.dead) return;
        if (result.hit) {
            p.hp = Math.max(0, p.hp - result.damage);
            p.lastDamagedAt = this.now;
        }
        this.event({ k: 'hit', src: sourceId, dst: p.id, dmg: result.damage, crit: result.crit, miss: !result.hit, kind: result.kind });
        if (p.hp > 0) return;
        p.dead = true;
        p.respawnAt = this.now + this.data.combat.player.respawnDelay;
        p.statuses.clear();
        p.queued = null;
        p.leap = null;
        p.regenCarry = 0;
        this.setTarget(p, null);
        var killer = sourceId ? this.findMonster(sourceId) : null;
        this.event({ k: 'die', id: p.id, by: sourceId });
        this.log('[Combat] ' + p.save.name + ' was defeated by ' + (killer ? killer.data.name : 'something'));
        p.saveDirty = true;
    }

    private revive(p: SimPlayer): void {
        var spot = this.respawnPoint();
        p.dead = false;
        p.pos = spot.pos;
        p.yaw = spot.yaw;
        p.onLedge = null;
        p.hp = p.stats.maxHp;
        p.skills.restoreSpirit();
        p.statuses.clear();
        p.lastDamagedAt = -Infinity;
        p.nextAttackAt = 0;
        p.castEnd = 0;
        p.moveAnchor = { pos: { x: spot.pos.x, y: spot.pos.y, z: spot.pos.z }, t: this.now };
        this.event({ k: 'revive', id: p.id, at: [spot.pos.x, spot.pos.y, spot.pos.z] });
        this.send(Op.Correction, { x: spot.pos.x, y: spot.pos.y, z: spot.pos.z, why: 'revive' }, [p.id]);
        this.log('[Combat] ' + p.save.name + ' revived at ' + this.zone.name + "'s respawn point");
        p.saveDirty = true;
    }

    // ================================================================ monsters

    private spawnPacks(): void {
        var n = 0;
        for (var i = 0; i < this.zone.packs.length; i++) {
            var pack = this.zone.packs[i];
            var data = getMonster(this.data, pack.monster);
            for (var c = 0; c < pack.count; c++) {
                var m: SimMonster = {
                    id: 'm' + (++n),
                    data: data,
                    pack: pack,
                    home: vec3From(pack.position),
                    pos: vec3From(pack.position),
                    yaw: 0,
                    hp: data.maxHp,
                    statuses: new StatusSet(),
                    state: 'idle',
                    target: null,
                    dest: null,
                    path: null,
                    pathFor: null,
                    pathAt: 0,
                    nextWanderAt: 0,
                    nextAttackAt: 0,
                    despawnAt: 0,
                    despawned: false,
                    respawnAt: 0,
                    damageBy: {},
                    lastAttacker: null,
                    sent: '',
                };
                this.monsters.push(m);
                this.placeMonster(m);
            }
        }
    }

    /** (Re)spawns at a free spot in its pack's circle: full HP, idle. */
    private placeMonster(m: SimMonster): void {
        var centre = vec3From(m.pack.position);
        var spot = this.freeSpot(centre, m.pack.radius, 10, m, true)
            || (this.grid && this.grid.nearestWalkable(centre, m.pack.radius)) || centre;
        m.home = spot;
        m.pos = { x: spot.x, y: spot.y, z: spot.z };
        m.yaw = this.rng.next() * 360;
        m.hp = m.data.maxHp;
        m.statuses.clear();
        m.state = 'idle';
        m.target = null;
        m.dest = null;
        m.path = null;
        m.despawned = false;
        m.damageBy = {};
        m.lastAttacker = null;
        m.nextWanderAt = this.now + this.rng.next() * this.data.zones.monsterAi.wanderPauseMax;
    }

    /**
     * A random walkable point within `radius` of `centre`, on the ground, that no other monster
     * stands on or walks to; failing that (spawning only, `fallback`), any walkable one found.
     */
    private freeSpot(centre: Vec3, radius: number, attempts: number, except: SimMonster, fallback?: boolean): Vec3 | null {
        var spare: Vec3 | null = null;
        for (var i = 0; i < attempts; i++) {
            var angle = this.rng.next() * Math.PI * 2, r = Math.sqrt(this.rng.next()) * radius;
            var point = vec3(centre.x + Math.cos(angle) * r, centre.y, centre.z + Math.sin(angle) * r);
            if (this.grid) {
                var ground = this.grid.heightAt(point);
                if (ground === null) continue;
                point.y = ground;
            }
            if (this.isFree(point, this.data.zones.monsterAi.personalSpace, except)) return point;
            if (!spare) spare = point;
        }
        return fallback ? spare : null;
    }

    private isFree(point: Vec3, space: number, except: SimMonster): boolean {
        var spaceSq = space * space;
        for (var i = 0; i < this.monsters.length; i++) {
            var o = this.monsters[i];
            if (o === except || o.state === 'dead') continue;
            if (flatDistanceSq(o.pos, point) < spaceSq) return false;
            if (o.dest && flatDistanceSq(o.dest, point) < spaceSq) return false;
        }
        return true;
    }

    private updateMonster(m: SimMonster, dt: number): void {
        var ai = this.data.zones.monsterAi;
        if (m.state === 'dead') {
            if (!m.despawned && this.now >= m.despawnAt) {
                m.despawned = true;
                m.respawnAt = this.now + m.data.respawnDelay;
                this.event({ k: 'despawn', id: m.id });
            } else if (m.despawned && this.now >= m.respawnAt) {
                this.placeMonster(m);
                m.sent = '';
                this.event({ k: 'spawn', monster: this.publicMonster(m) });
            }
            return;
        }

        var tick = m.statuses.update(this.now);
        if (tick.damage > 0) {
            this.damageMonster(m, tick.source, { hit: true, crit: false, damage: tick.damage, kind: 'poison' });
            if ((m.state as MonsterState) === 'dead') return;
        }
        if (m.statuses.isDisabled(this.now)) return;

        switch (m.state) {
            case 'idle': {
                var victim = this.nearestPlayer(m.pos, m.data.aggroRange);
                if (victim) { this.startChase(m, victim.id); break; }
                if (m.dest) {
                    if (this.moveToward(m, m.dest, m.data.wanderSpeed, dt, 0) !== 'moving') m.dest = null;
                } else if (this.now >= m.nextWanderAt) {
                    m.nextWanderAt = this.now + ai.wanderPauseMin + this.rng.next() * (ai.wanderPauseMax - ai.wanderPauseMin);
                    // A few tries for a free spot; if the pack crowds every one, stay put until the next pause.
                    m.dest = this.freeSpot(m.home, m.data.wanderRadius, 4, m);
                }
                break;
            }
            case 'chase': {
                var target = m.target ? this.players[m.target] : null;
                if (!target || target.dead || this.zone.safe || flatDistance(m.pos, m.home) > m.data.leashRange) {
                    this.startReturn(m);
                    break;
                }
                // Without a grid, walk at the height of a player on the ground; one on a ledge stays above, out of reach.
                if (!this.grid && !target.onLedge) m.pos.y = target.pos.y;
                var a = m.data.attack;
                var inReach = flatDistance(m.pos, target.pos) <= a.range && Math.abs(target.pos.y - m.pos.y) <= a.reachHeight;
                if (inReach) {
                    var face = flatDirection(m.pos, target.pos);
                    if (face) m.yaw = directionToYaw(face);
                    if (this.now >= m.nextAttackAt) {
                        m.nextAttackAt = this.now + a.interval;
                        this.damagePlayer(target, m.id, rollBasicAttack(a, target.stats.defense, target.stats.dodge, this.rng));
                    }
                } else if (this.moveToward(m, target.pos, m.data.chaseSpeed, dt, a.range * ai.stoppingFraction) === 'blocked') {
                    // No way round to them: give up.
                    this.startReturn(m);
                }
                break;
            }
            case 'return': {
                var home = this.moveToward(m, m.home, m.data.chaseSpeed, dt, 0);
                if (home !== 'moving' || flatDistance(m.pos, m.home) <= ai.homeArriveDistance) {
                    // Arrived, or (never expected) no way home: either way, back at home.
                    m.pos = { x: m.home.x, y: m.home.y, z: m.home.z };
                    m.path = null;
                    m.hp = m.data.maxHp;
                    m.statuses.clear();
                    m.damageBy = {};
                    m.lastAttacker = null;
                    this.enterIdle(m);
                }
                break;
            }
        }
    }

    /**
     * Walks towards a point: straight where the ground allows, else along a grid path around
     * obstacles (recalculated every repathInterval while the point moves). 'arrived' once within
     * `stopAt`, 'blocked' when there's no way there.
     */
    private moveToward(m: SimMonster, point: Vec3, speed: number, dt: number, stopAt: number): 'arrived' | 'moving' | 'blocked' {
        if (flatDistance(m.pos, point) <= stopAt + 1e-3) { m.path = null; return 'arrived'; }
        var waypoint: Vec2 = point;
        var final = true;
        var grid = this.grid;
        if (grid && !grid.lineWalkable(m.pos, point)) {
            var ai = this.data.zones.monsterAi;
            var moved = !m.pathFor || flatDistance(m.pathFor, point) > 1;
            if (!m.path || (moved && this.now - m.pathAt >= ai.repathInterval)) {
                m.path = grid.findPath(m.pos, point, ai.pathMaxNodes);
                m.pathFor = { x: point.x, z: point.z };
                m.pathAt = this.now;
                if (!m.path) return 'blocked';
            }
            while (m.path.length > 1 && flatDistance(m.pos, m.path[0]) < 0.05) m.path.shift();
            final = m.path.length === 1;
            waypoint = final ? point : m.path[0];
        } else m.path = null;

        var stop = final ? stopAt : 0;
        var distance = flatDistance(m.pos, waypoint);
        var dir = flatDirection(m.pos, waypoint);
        if (dir) {
            var stepLength = Math.min(speed * dt, Math.max(0, distance - stop));
            m.pos.x += dir.x * stepLength;
            m.pos.z += dir.z * stepLength;
            m.yaw = directionToYaw(dir);
            if (!final && stepLength >= distance - 1e-3 && m.path) m.path.shift();
        }
        this.settle(m);
        return final && flatDistance(m.pos, point) <= stopAt + 1e-3 ? 'arrived' : 'moving';
    }

    /** Puts a monster on the grid's ground where it stands. */
    private settle(m: SimMonster): void {
        if (!this.grid) return;
        var ground = this.grid.groundNear(m.pos, 1.5);
        if (ground !== null) m.pos.y = ground;
    }

    /** Stopped agents never push apart, so nudge overlapping monsters away from each other. */
    private separateMonsters(): void {
        var minGap = this.data.zones.monsterAi.bodyRadius * 2;
        var list = this.monsters;
        for (var i = 0; i < list.length; i++) {
            var a = list[i];
            if (a.state === 'dead' || a.statuses.isDisabled(this.now)) continue;
            for (var j = i + 1; j < list.length; j++) {
                var b = list[j];
                if (b.state === 'dead' || b.statuses.isDisabled(this.now)) continue;
                var d = flatDistance(a.pos, b.pos);
                if (d >= minGap) continue;
                var dir = flatDirection(b.pos, a.pos) || { x: 1, z: 0 };
                var push = (minGap - d) * 0.5;
                this.nudge(a, dir.x * push, dir.z * push);
                this.nudge(b, -dir.x * push, -dir.z * push);
            }
        }
    }

    /** Shifts a monster on the ground plane unless that would put it off walkable ground. */
    private nudge(m: SimMonster, dx: number, dz: number): void {
        var to = { x: m.pos.x + dx, z: m.pos.z + dz };
        if (this.grid && !this.grid.isWalkable(to)) return;
        m.pos.x = to.x;
        m.pos.z = to.z;
        this.settle(m);
    }

    private nearestPlayer(from: Vec3, range: number): SimPlayer | null {
        if (this.zone.safe) return null;
        var best: SimPlayer | null = null, bestDistance = range;
        for (var id in this.players) {
            var p = this.players[id];
            if (p.dead) continue;
            var d = flatDistance(from, p.pos);
            if (d > bestDistance) continue;
            bestDistance = d;
            best = p;
        }
        return best;
    }

    private enterIdle(m: SimMonster): void {
        m.state = 'idle';
        m.target = null;
        m.dest = null;
        m.nextWanderAt = this.now + this.rng.next() * this.data.zones.monsterAi.wanderPauseMax;
    }

    private startChase(m: SimMonster, playerId: string): void {
        if (m.state === 'chase' && m.target === playerId) return;
        m.state = 'chase';
        m.target = playerId;
        m.dest = null;
        m.path = null;
        // A short wind-up so a monster doesn't bite the instant it reaches you.
        m.nextAttackAt = Math.max(m.nextAttackAt, this.now + this.data.zones.monsterAi.chaseWindup);
    }

    private startReturn(m: SimMonster): void {
        m.state = 'return';
        m.target = null;
        m.dest = null;
        m.path = null;
    }

    private damageMonster(m: SimMonster, attackerId: string | null, result: AttackResult): void {
        if (m.state === 'dead') return;
        var before = m.hp;
        if (result.hit) m.hp = Math.max(0, m.hp - result.damage);
        if (attackerId && this.players[attackerId]) {
            m.lastAttacker = attackerId;
            m.damageBy[attackerId] = (m.damageBy[attackerId] || 0) + (before - m.hp);
        }
        this.event({ k: 'hit', src: attackerId, dst: m.id, dmg: result.damage, crit: result.crit, miss: !result.hit, kind: result.kind });
        if (m.hp <= 0) return this.killMonster(m);

        var attacker = attackerId ? this.players[attackerId] : null;
        if (m.state !== 'idle' || !attacker || attacker.dead) return;
        this.startChase(m, attacker.id);
        // Packmates nearby join in.
        var radius = this.data.zones.monsterAi.packAssistRadius;
        for (var i = 0; i < this.monsters.length; i++) {
            var other = this.monsters[i];
            if (other !== m && other.pack === m.pack && other.state === 'idle' && flatDistance(other.pos, m.pos) <= radius)
                this.startChase(other, attacker.id);
        }
    }

    private killMonster(m: SimMonster): void {
        m.state = 'dead';
        m.statuses.clear();
        m.target = null;
        m.dest = null;
        m.despawnAt = this.now + this.data.zones.monsterAi.corpseTime;
        var creditId = this.creditFor(m);
        this.event({ k: 'die', id: m.id, by: creditId });
        var p = creditId ? this.players[creditId] : null;
        if (!p) return;
        p.save.kills++;
        p.saveDirty = true;
        p.selfDirty = true;
        this.log('[Combat] ' + m.data.name + ' slain in ' + this.zone.name + ' by ' + p.save.name + ' (' + p.save.kills + ' total)');
        if (p.skills.addKill()) this.send(Op.Notice, { code: 'skillPoint', text: 'Skill point earned' }, [p.id]);
        this.dropLoot(m, p);
    }

    /** The player who dealt the most damage since the spawn (the killing blow breaks ties). */
    creditFor(m: SimMonster): string | null {
        var best: string | null = null, bestDamage = 0;
        for (var id in m.damageBy) {
            if (!this.players[id]) continue;
            var dealt = m.damageBy[id];
            if (dealt > bestDamage || (dealt === bestDamage && id === m.lastAttacker)) { best = id; bestDamage = dealt; }
        }
        return best;
    }

    // ================================================================ loot

    /** Rolls the monster's loot table and scatters the drops around the corpse, for the credited player only. */
    private dropLoot(m: SimMonster, p: SimPlayer): void {
        var rules = this.data.zones.playerRules;
        var drops = rollLoot(this.data, findLootTable(this.data, m.data.id), this.factory, this.rng);
        if (drops.length === 0) return;
        var added: any[] = [];
        var start = this.rng.next() * 360, stepAngle = 360 / drops.length;
        var names: string[] = [];
        for (var i = 0; i < drops.length; i++) {
            var angle = (start + stepAngle * i) * Math.PI / 180;
            var r = rules.lootScatterMin + this.rng.next() * (rules.lootScatterMax - rules.lootScatterMin);
            var drop: SimDrop = {
                id: 'd' + (this.nextDropId++),
                owner: p.id,
                pos: this.dropSpot(m.pos, angle, r),
                gold: drops[i].gold,
                item: drops[i].item,
                droppedAt: this.now,
                source: m.data.name,
            };
            this.drops.push(drop);
            added.push(this.publicDrop(drop));
            names.push(drop.item ? describeItem(this.data, drop.item) : drop.gold + ' gold');
        }
        this.send(Op.Drops, { add: added, remove: [] }, [p.id]);
        this.log('[Loot] ' + m.data.name + ' dropped ' + names.join(', '));
    }

    /** Where a drop lands: around the corpse, on walkable ground (the corpse's spot if that's all there is). */
    private dropSpot(at: Vec3, angle: number, r: number): Vec3 {
        var spot = vec3(at.x + Math.sin(angle) * r, at.y, at.z + Math.cos(angle) * r);
        if (!this.grid) return spot;
        var ground = this.grid.heightAt(spot);
        return ground === null ? vec3(at.x, at.y, at.z) : vec3(spot.x, ground, spot.z);
    }

    private updateDrops(): void {
        var loot = this.data.items.loot;
        var radiusSq = loot.autoLootRadius * loot.autoLootRadius;
        var removed: { [owner: string]: any[] } = {};
        var keep: SimDrop[] = [];
        for (var i = 0; i < this.drops.length; i++) {
            var d = this.drops[i];
            var owner = this.players[d.owner];
            var why: string | null = null;
            if (!owner) why = 'gone';
            else if (this.now - d.droppedAt >= loot.groundTime) why = 'expired';
            else if (!owner.dead && this.now - d.droppedAt >= loot.pickupDelay && flatDistanceSq(owner.pos, d.pos) <= radiusSq) {
                var block = owner.items.pickUp({ gold: d.gold, item: d.item }, d.source);
                if (block === 'none') why = 'picked';
                else if (!owner.bagFullTold[d.id]) {
                    owner.bagFullTold[d.id] = true;
                    this.send(Op.Notice, { code: 'bagFull', text: ErrorText.bagFull }, [owner.id]);
                }
            }
            if (why) {
                if (owner) (removed[owner.id] = removed[owner.id] || []).push({ id: d.id, why: why });
                if (owner) delete owner.bagFullTold[d.id];
            } else keep.push(d);
        }
        this.drops = keep;
        for (var id in removed) this.send(Op.Drops, { add: [], remove: removed[id] }, [id]);
    }

    // ================================================================ saving

    /**
     * Saves due now: players with changes (after saveInterval) or due a lock refresh. The session
     * lock in each save runs until `nowMs + sessionLockSeconds`.
     */
    collectSaves(matchId: string, nowMs: number): SaveRequest[] {
        var server = this.data.zones.server;
        var due: SaveRequest[] = [];
        for (var id in this.players) {
            var p = this.players[id];
            var since = this.now - p.lastSaveAt;
            var changed = p.saveDirty || p.pendingLedger.length > 0;
            if ((changed && since >= server.saveInterval) || since >= server.lockRefreshInterval) {
                due.push(this.saveRequest(p, matchId, nowMs + server.sessionLockSeconds * 1000));
                p.lastSaveAt = this.now;
                p.saveDirty = false;
            }
        }
        return due;
    }

    private saveRequest(p: SimPlayer, matchId: string | null, until: number): SaveRequest {
        var save = p.save;
        save.belongings = p.items.s;
        save.skills = p.skills.progress;
        save.hp = p.dead ? 0 : p.hp;
        save.spirit = p.dead ? 0 : Math.floor(p.skills.spirit);
        var at = p.leap ? p.leap.to : p.pos;
        save.location = { zone: this.zone.id, x: q2(at.x), y: q2(at.y), z: q2(at.z), yaw: Math.round(p.yaw) };
        save.session = matchId ? { matchId: matchId, until: until } : null;
        var ledger = p.pendingLedger;
        p.pendingLedger = [];
        return { userId: p.id, characterId: save.characterId, save: deepCopy(save), ledger: ledger };
    }

    // ================================================================ outgoing

    private send(op: number, data: any, to: string[] | null): void { this.out.push({ op: op, data: data, to: to }); }

    private event(e: any): void { this.events.push(e); }

    private result(p: SimPlayer, a: string, rid: any, error: string | null, extra?: any): void {
        var msg: any = { a: a, ok: !error };
        if (rid !== undefined && rid !== null) msg.rid = rid;
        if (error) { msg.error = error; msg.text = ErrorText[error] || error; }
        if (extra) for (var k in extra) msg[k] = extra[k];
        this.send(Op.Result, msg, [p.id]);
    }

    /** Events, then the snapshot of whatever changed, then each player's private state if it changed. */
    private flush(): void {
        if (this.events.length > 0) {
            this.send(Op.Events, { t: q2(this.now), e: this.events }, null);
            this.events = [];
        }
        var players: any[] = [], monsters: any[] = [];
        for (var id in this.players) {
            var p = this.players[id];
            var sig = this.playerSignature(p);
            if (sig !== p.sent) { p.sent = sig; players.push(this.publicPlayer(p)); }
        }
        for (var i = 0; i < this.monsters.length; i++) {
            var m = this.monsters[i];
            if (m.despawned) continue;
            var msig = this.monsterSignature(m);
            if (msig !== m.sent) { m.sent = msig; monsters.push(this.publicMonster(m)); }
        }
        if (players.length > 0 || monsters.length > 0) this.send(Op.Snapshot, { t: q2(this.now), p: players, m: monsters }, null);
        for (id in this.players) {
            var self = this.players[id];
            if (!self.selfDirty) continue;
            self.selfDirty = false;
            this.send(Op.Self, this.selfState(self), [self.id]);
        }
    }

    /** Hands over everything queued for sending since the last call. */
    takeMessages(): OutMessage[] {
        var out = this.out;
        this.out = [];
        return out;
    }

    private playerSignature(p: SimPlayer): string {
        var pos = p.leap ? p.leap.to : p.pos;
        return [q2(pos.x), q2(pos.y), q2(pos.z), Math.round(p.yaw), p.hp, p.stats.maxHp, Math.floor(p.skills.spirit), p.skills.maxSpirit, p.dead, p.statuses.active.length].join('|');
    }

    private monsterSignature(m: SimMonster): string {
        return [q2(m.pos.x), q2(m.pos.y), q2(m.pos.z), Math.round(m.yaw), m.hp, m.state, m.target, m.statuses.active.length].join('|');
    }

    publicPlayer(p: SimPlayer): any {
        var pos = p.leap ? p.leap.to : p.pos;
        return {
            id: p.id,
            name: p.save.name,
            character: p.save.characterId,
            weapon: p.items.weapon ? p.items.weapon.def : null,
            x: q2(pos.x), y: q2(pos.y), z: q2(pos.z), yaw: Math.round(p.yaw),
            hp: p.hp, maxHp: p.stats.maxHp,
            sp: Math.floor(p.skills.spirit), maxSp: p.skills.maxSpirit,
            dead: p.dead,
            st: p.statuses.describe(this.now),
        };
    }

    publicMonster(m: SimMonster): any {
        return {
            id: m.id,
            monster: m.data.id,
            pack: m.pack.id,
            x: q2(m.pos.x), y: q2(m.pos.y), z: q2(m.pos.z), yaw: Math.round(m.yaw),
            hp: m.hp, maxHp: m.data.maxHp,
            state: m.state,
            target: m.target,
            st: m.statuses.describe(this.now),
        };
    }

    private publicDrop(d: SimDrop): any {
        return {
            id: d.id,
            x: q2(d.pos.x), y: q2(d.pos.y), z: q2(d.pos.z),
            gold: d.gold,
            item: d.item ? { id: d.item.id, def: d.item.def, rarity: d.item.rarity, count: d.item.count, lines: d.item.lines } : null,
            left: q2(this.data.items.loot.groundTime - (this.now - d.droppedAt)),
        };
    }

    selfState(p: SimPlayer): any {
        var s = p.stats;
        var cooldowns: { [key: string]: number } = {};
        for (var key in p.skills.readyAt) {
            var left = p.skills.cooldownLeft(key, this.now);
            if (left > 0) cooldowns[key] = q2(left);
        }
        var potionCooldowns: { [group: string]: number } = {};
        for (var g in p.items.cooldownEnds) {
            var pl = p.items.cooldownEnds[g] - this.now;
            if (pl > 0) potionCooldowns[g] = q2(pl);
        }
        return {
            characterId: p.save.characterId,
            name: p.save.name,
            gold: p.items.gold,
            bag: p.items.s.bag,
            equipment: p.items.s.equipment,
            quickSlots: p.items.s.quickSlots,
            stats: {
                weaponType: s.weaponType, maxHp: s.maxHp, maxSpirit: s.maxSpirit, defense: s.defense, dodge: s.dodge,
                resist: s.resist, attack: s.attack, skillDamage: s.skillDamage, skillDefense: s.skillDefense,
            },
            skills: p.skills.progress,
            kills: p.save.kills,
            cooldowns: cooldowns,
            potionCooldowns: potionCooldowns,
        };
    }
}

function parseDir(value: any): Vec2 | null {
    if (!value || !isFiniteNumber(value[0]) || !isFiniteNumber(value[1])) return null;
    var length = Math.sqrt(value[0] * value[0] + value[1] * value[1]);
    return length < 1e-6 ? null : { x: value[0] / length, z: value[1] / length };
}
