// Combat rules, ported line for line from the client's pure C# (Wuxia.Combat: CombatMath,
// SkillMath, SkillShapes, LeapRules, StatusSet, SkillState) so the server decides exactly what the
// client predicts. Positions use Unity axes (Y up); shapes are measured on the ground plane (X, Z).

type HitKind = 'basic' | 'skill' | 'poison';

interface AttackResult {
    hit: boolean;
    crit: boolean;
    damage: number;
    kind: HitKind;
}

// ---- CombatMath: basic attacks ----------------------------------------------------------------

var MIN_HIT_CHANCE = f32(0.05);
var MAX_HIT_CHANCE = f32(0.99);

/** accuracy − dodge, clamped to 5–99%. */
function hitChance(accuracy: number, dodge: number): number {
    return clamp(f32(f32(accuracy) - f32(dodge)), MIN_HIT_CHANCE, MAX_HIT_CHANCE);
}

/** Basic attacks can miss or crit; a hit deals roll(min..max) × crit − defense, at least 1. */
function rollBasicAttack(attack: AttackData, defense: number, dodge: number, rng: Rng): AttackResult {
    if (rng.next() >= hitChance(attack.accuracy, dodge)) return { hit: false, crit: false, damage: 0, kind: 'basic' };
    var crit = rng.next() < f32(attack.critChance);
    var raw = rng.int(attack.minDamage, attack.maxDamage + 1);
    if (crit) raw = f32(raw * f32(attack.critMultiplier));
    return { hit: true, crit: crit, damage: Math.max(1, roundHalfEven(raw) - defense), kind: 'basic' };
}

// ---- SkillMath: forms always hit and never crit -----------------------------------------------

/** 1 at book level 1, +perLevel for every level above. */
function levelMultiplier(bookLevel: number, perLevel: number): number {
    return f32(1 + f32(f32(perLevel) * Math.max(0, bookLevel - 1)));
}

/**
 * roll(min..max) × book-level bonus × (1 + skill damage) − (defense + skill defense), at least 1.
 * @param defense the target's defense plus its skill defense.
 * @param damageBonus the caster's skill damage from gear (0.05 = +5%).
 */
function rollSkillDamage(form: FormData, bookLevel: number, perLevel: number, defense: number, rng: Rng, damageBonus: number): number {
    var raw = rng.int(form.minDamage, form.maxDamage + 1);
    raw = f32(raw * f32(levelMultiplier(bookLevel, perLevel) * f32(1 + Math.max(0, f32(damageBonus || 0)))));
    return Math.max(1, roundHalfEven(raw) - defense);
}

/** chance × (1 − resist), clamped to 0–1. */
function statusChance(chance: number, resist: number): number {
    return clamp(f32(f32(chance) * f32(1 - clamp(f32(resist), 0, 1))), 0, 1);
}

function statusLands(chance: number, resist: number, rng: Rng): boolean {
    return rng.next() < statusChance(chance, resist);
}

function resistFor(resist: ResistData, key: string): number {
    switch (key) {
        case 'poison': return resist.poison || 0;
        case 'paralysis': return resist.paralysis || 0;
        case 'stun': return resist.stun || 0;
        default: throw new Error("Unknown resistance '" + key + "'");
    }
}

// ---- SkillShapes ------------------------------------------------------------------------------

/** Slack so a target exactly on an edge stays inside despite float rounding. */
var SHAPE_EPSILON = 1e-4;

function withinHeight(caster: Vec3, target: Vec3, reachHeight: number): boolean {
    return Math.abs(target.y - caster.y) <= reachHeight + SHAPE_EPSILON;
}

/**
 * Can a single or circle-at-target form be aimed at `target` from here?
 * @param slack extra metres of reach (the server's tolerance for lagging positions; 0 = exact).
 */
function formInReach(form: FormData, caster: Vec3, target: Vec3, reachHeight: number, slack?: number): boolean {
    return flatDistance(caster, target) <= form.range + (slack || 0) + SHAPE_EPSILON && withinHeight(caster, target, reachHeight);
}

function inCircle(centre: Vec2, radius: number, point: Vec2): boolean {
    var r = radius + SHAPE_EPSILON;
    return flatDistanceSq(centre, point) <= r * r;
}

/** @param angleDegrees full width of the cone; 360 makes it a circle. */
function inCone(apex: Vec2, facing: Vec2, range: number, angleDegrees: number, point: Vec2): boolean {
    var ox = point.x - apex.x, oz = point.z - apex.z;
    var distance = Math.sqrt(ox * ox + oz * oz);
    if (distance > range + SHAPE_EPSILON) return false;
    if (distance < SHAPE_EPSILON) return true; // standing inside the caster
    var fl = Math.sqrt(facing.x * facing.x + facing.z * facing.z);
    var fx = fl > 1e-4 ? facing.x / fl : 0, fz = fl > 1e-4 ? facing.z / fl : 1;
    var halfAngle = angleDegrees * 0.5 * Math.PI / 180;
    return (ox / distance) * fx + (oz / distance) * fz >= Math.cos(halfAngle) - 1e-6;
}

/**
 * Indices of every candidate the form covers. Single = the locked target in range; cone = within
 * range and angle/2 of the facing; circle = within radius of the caster; circleAtTarget = within
 * radius of a locked target that is within range. Targets beyond reachHeight above or below are skipped.
 * @param lockedIndex index of the locked target in `candidates`, or -1.
 * @param slack extra reach for the locked-target check only (0 = exact client rules).
 */
function selectTargets(form: FormData, caster: Vec3, facing: Vec2, candidates: Vec3[], lockedIndex: number,
                       reachHeight: number, slack?: number): number[] {
    var hits: number[] = [];
    var i: number;
    switch (form.shape) {
        case 'single':
            if (lockedIndex >= 0 && formInReach(form, caster, candidates[lockedIndex], reachHeight, slack)) hits.push(lockedIndex);
            break;
        case 'circleAtTarget':
            if (lockedIndex < 0 || !formInReach(form, caster, candidates[lockedIndex], reachHeight, slack)) break;
            var centre = candidates[lockedIndex];
            for (i = 0; i < candidates.length; i++)
                if (withinHeight(caster, candidates[i], reachHeight) && inCircle(centre, form.radius, candidates[i])) hits.push(i);
            break;
        case 'circle':
            for (i = 0; i < candidates.length; i++)
                if (withinHeight(caster, candidates[i], reachHeight) && inCircle(caster, form.radius, candidates[i])) hits.push(i);
            break;
        case 'cone':
            for (i = 0; i < candidates.length; i++)
                if (withinHeight(caster, candidates[i], reachHeight) && inCone(caster, facing, form.range, form.angle, candidates[i])) hits.push(i);
            break;
    }
    return hits;
}

function needsTarget(form: FormData): boolean {
    return form.shape === 'single' || form.shape === 'circleAtTarget';
}

// ---- LeapRules and ledges ---------------------------------------------------------------------

type LeapCheck = 'ok' | 'notALedge' | 'tooFar' | 'tooHigh' | 'tooLow';

/** A landing must be a marked ledge, within maxDistance on the ground plane and within maxRise/maxDrop. */
function checkLeap(rules: LeapData, from: Vec3, to: Vec3, onMarkedLedge: boolean): LeapCheck {
    if (!onMarkedLedge) return 'notALedge';
    if (flatDistance(from, to) > rules.maxDistance + SHAPE_EPSILON) return 'tooFar';
    var rise = to.y - from.y;
    if (rise > rules.maxRise + SHAPE_EPSILON) return 'tooHigh';
    if (-rise > rules.maxDrop + SHAPE_EPSILON) return 'tooLow';
    return 'ok';
}

function ledgeTop(ledge: LedgeData): number {
    return ledge.centre[1] + ledge.size[1] * 0.5;
}

/** World point → the ledge's local ground-plane axes (it only turns around Y). */
function ledgeLocal(ledge: LedgeData, p: Vec2): Vec2 {
    var r = ledge.yaw * Math.PI / 180, c = Math.cos(r), s = Math.sin(r);
    var wx = p.x - ledge.centre[0], wz = p.z - ledge.centre[2];
    return { x: wx * c - wz * s, z: wx * s + wz * c };
}

function ledgeWorld(ledge: LedgeData, local: Vec2, y: number): Vec3 {
    var r = ledge.yaw * Math.PI / 180, c = Math.cos(r), s = Math.sin(r);
    return { x: ledge.centre[0] + local.x * c + local.z * s, y: y, z: ledge.centre[2] - local.x * s + local.z * c };
}

/** The point on top of the ledge nearest to `from`, kept `inset` inside its edges (LeapLedge.LandingPoint). */
function ledgeLandingPoint(ledge: LedgeData, from: Vec2, inset: number): Vec3 {
    var local = ledgeLocal(ledge, from);
    var spanX = Math.max(0, ledge.size[0] * 0.5 - inset), spanZ = Math.max(0, ledge.size[2] * 0.5 - inset);
    return ledgeWorld(ledge, { x: clamp(local.x, -spanX, spanX), z: clamp(local.z, -spanZ, spanZ) }, ledgeTop(ledge));
}

/** Is `p` over the ledge's footprint (grown by `margin`)? */
function overLedge(ledge: LedgeData, p: Vec2, margin: number): boolean {
    var local = ledgeLocal(ledge, p);
    return Math.abs(local.x) <= ledge.size[0] * 0.5 + margin && Math.abs(local.z) <= ledge.size[2] * 0.5 + margin;
}

// ---- StatusSet --------------------------------------------------------------------------------

interface ActiveStatus {
    data: StatusData;
    endTime: number;
    nextTick: number;
    tickDamage: number;
    /** Entity id of whoever applied it; poison damage is credited to them. */
    source: string | null;
}

/** Active statuses on one combatant. Landing one again refreshes it rather than stacking. Time passed in. */
class StatusSet {
    active: ActiveStatus[] = [];

    has(id: string, now: number): boolean {
        for (var i = 0; i < this.active.length; i++) if (this.active[i].data.id === id && this.active[i].endTime > now) return true;
        return false;
    }

    /** Paralysis, stun or knockdown is running: can't move, attack or use skills. */
    isDisabled(now: number): boolean {
        for (var i = 0; i < this.active.length; i++) if (this.active[i].data.disables && this.active[i].endTime > now) return true;
        return false;
    }

    apply(data: StatusData, duration: number, tickDamage: number, source: string | null, now: number): void {
        for (var i = 0; i < this.active.length; i++) {
            var a = this.active[i];
            if (a.data.id !== data.id) continue;
            a.endTime = Math.max(a.endTime, now + duration);
            a.tickDamage = Math.max(a.tickDamage, tickDamage);
            a.source = source;
            return;
        }
        this.active.push({
            data: data,
            endTime: now + duration,
            nextTick: data.tickInterval > 0 ? now + data.tickInterval : Infinity,
            tickDamage: tickDamage,
            source: source,
        });
    }

    /**
     * Advances to `now`: returns the damage of every tick that came due (one landing exactly as the
     * status ends still counts) and who it's credited to, and drops expired statuses.
     */
    update(now: number): { damage: number; source: string | null; expired: string[] } {
        var damage = 0, source: string | null = null, expired: string[] = [];
        for (var i = this.active.length - 1; i >= 0; i--) {
            var a = this.active[i];
            if (a.data.tickInterval > 0) {
                while (a.nextTick <= now && a.nextTick <= a.endTime) {
                    damage += a.tickDamage;
                    source = a.source;
                    a.nextTick += a.data.tickInterval;
                }
            }
            if (a.endTime <= now) {
                expired.push(a.data.id);
                this.active.splice(i, 1);
            }
        }
        return { damage: damage, source: source, expired: expired };
    }

    clear(): void { this.active = []; }

    /** [{id, left}] for the wire. */
    describe(now: number): { id: string; left: number }[] {
        var out: { id: string; left: number }[] = [];
        for (var i = 0; i < this.active.length; i++)
            if (this.active[i].endTime > now) out.push({ id: this.active[i].data.id, left: q2(this.active[i].endTime - now) });
        return out;
    }
}

// ---- SkillState -------------------------------------------------------------------------------

/** What's saved of a character's skills: learned books and their levels, points and the kill meter. */
interface SkillProgress {
    books: { [bookId: string]: number };
    points: number;
    meterKills: number;
}

type CastBlock = 'none' | 'notLearned' | 'locked' | 'cooldown' | 'noSpirit';
type LevelUpBlock = 'none' | 'notLearned' | 'maxLevel' | 'notEnoughPoints';

/** One character's skill progress plus Spirit and cooldowns (SkillState.cs). Time passed in. */
class SkillState {
    data: GameData;
    progress: SkillProgress;
    maxSpirit: number;
    spirit: number;
    regenPerSecond: number;
    readyAt: { [key: string]: number } = {};

    constructor(data: GameData, progress: SkillProgress, maxSpirit: number, spirit?: number) {
        this.data = data;
        this.progress = progress;
        this.maxSpirit = maxSpirit;
        this.spirit = spirit === undefined ? maxSpirit : Math.min(maxSpirit, Math.max(0, spirit));
        this.regenPerSecond = data.combat.player.spiritRegenPerSecond;
    }

    knows(bookId: string): boolean { return this.progress.books.hasOwnProperty(bookId); }

    /** The book's level, or -1 if it hasn't been learned. */
    bookLevel(bookId: string): number { return this.knows(bookId) ? this.progress.books[bookId] : -1; }

    /** Reading a book teaches it at level 0. False if already known. */
    learn(bookId: string): boolean {
        getBook(this.data, bookId);
        if (this.knows(bookId)) return false;
        this.progress.books[bookId] = 0;
        return true;
    }

    checkLevelUp(bookId: string): { block: LevelUpBlock; cost: number } {
        if (!this.knows(bookId)) return { block: 'notLearned', cost: -1 };
        var cost = levelUpCost(this.data, this.progress.books[bookId]);
        if (cost < 0) return { block: 'maxLevel', cost: cost };
        return { block: this.progress.points >= cost ? 'none' : 'notEnoughPoints', cost: cost };
    }

    levelUp(bookId: string): LevelUpBlock {
        var check = this.checkLevelUp(bookId);
        if (check.block !== 'none') return check.block;
        this.progress.points -= check.cost;
        this.progress.books[bookId]++;
        return 'none';
    }

    addSkillPoints(points: number): void { if (points > 0) this.progress.points += points; }

    /** Counts a kill on the meter; true when it fills and pays out a point. */
    addKill(): boolean {
        this.progress.meterKills++;
        if (this.progress.meterKills < this.data.skills.skillPoints.killsPerPoint) return false;
        this.progress.meterKills = 0;
        this.progress.points++;
        return true;
    }

    isUnlocked(book: BookData, form: FormData): boolean { return this.bookLevel(book.id) >= form.unlockLevel; }

    checkCast(book: BookData, form: FormData, now: number): CastBlock {
        if (!this.knows(book.id)) return 'notLearned';
        if (!this.isUnlocked(book, form)) return 'locked';
        if (this.cooldownLeft(form.id, now) > 0) return 'cooldown';
        return this.spirit >= form.spiritCost ? 'none' : 'noSpirit';
    }

    commitCast(form: FormData, now: number): void { this.spend(form.id, form.spiritCost, form.cooldown, now); }

    checkLeap(now: number): CastBlock {
        var leap = this.data.skills.drangueLeap;
        if (!this.knows(leap.bookId)) return 'notLearned';
        if (this.cooldownLeft(leap.bookId, now) > 0) return 'cooldown';
        return this.spirit >= leap.spiritCost ? 'none' : 'noSpirit';
    }

    commitLeap(now: number): void {
        var leap = this.data.skills.drangueLeap;
        this.spend(leap.bookId, leap.spiritCost, leap.cooldown, now);
    }

    /** Seconds until a form (by form id) or the leap (by its book id) is ready again. */
    cooldownLeft(key: string, now: number): number {
        return this.readyAt.hasOwnProperty(key) ? Math.max(0, this.readyAt[key] - now) : 0;
    }

    regenerate(seconds: number): void { this.spirit = Math.min(this.maxSpirit, this.spirit + this.regenPerSecond * seconds); }

    restoreSpirit(): void { this.spirit = this.maxSpirit; }

    addSpirit(amount: number): void { if (amount > 0) this.spirit = Math.min(this.maxSpirit, this.spirit + amount); }

    /** Gear changed max Spirit; current Spirit is kept but capped. */
    setMaxSpirit(maxSpirit: number): void {
        this.maxSpirit = Math.max(0, maxSpirit);
        this.spirit = Math.min(this.spirit, this.maxSpirit);
    }

    private spend(key: string, spirit: number, cooldown: number, now: number): void {
        this.spirit = Math.max(0, this.spirit - spirit);
        this.readyAt[key] = now + cooldown;
    }
}

/** New characters: every usable book learned (weaponless ones like the leap too) when skills.json dev.learnAllBooks is on. */
function startingSkills(data: GameData, characterId: string): SkillProgress {
    var progress: SkillProgress = { books: {}, points: 0, meterKills: 0 };
    var dev = data.skills.dev;
    var character = getCharacter(data, characterId);
    if (dev.learnAllBooks)
        data.skills.books.forEach(function (book) {
            if (!book.weapon || indexOf(character.weapons, book.weapon) >= 0) progress.books[book.id] = 0;
        });
    progress.points = Math.max(0, dev.startingSkillPoints || 0);
    return progress;
}
