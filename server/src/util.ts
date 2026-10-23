// Small helpers shared by the rule files. Everything in src/ compiles into one global scope
// (no import/export), so names here are visible everywhere.

/** Random numbers in the shape the client's System.Random calls use. */
interface Rng {
    /** [0, 1), like System.Random.NextDouble. */
    next(): number;
    /** Whole number in [min, maxExclusive), like System.Random.Next(min, max). */
    int(min: number, maxExclusive: number): number;
}

/** Math.random for live matches. */
function mathRng(): Rng {
    return {
        next: function () { return Math.random(); },
        int: function (min: number, maxExclusive: number) {
            return maxExclusive <= min ? min : min + Math.floor(Math.random() * (maxExclusive - min));
        },
    };
}

/** Seeded RNG (mulberry32) for tests and repeatable simulations. */
function seededRng(seed: number): Rng {
    var s = seed >>> 0;
    var next = function () {
        s = (s + 0x6d2b79f5) >>> 0;
        var t = s;
        t = Math.imul(t ^ (t >>> 15), t | 1);
        t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
    return {
        next: next,
        int: function (min: number, maxExclusive: number) {
            return maxExclusive <= min ? min : min + Math.floor(next() * (maxExclusive - min));
        },
    };
}

/**
 * The client's rules use C# floats; f32 rounds a JS double to the same single-precision value so
 * multiplications land on the same side of .5 (e.g. 25 × 1.7f = 42.5000012 → 43, not 42).
 */
var f32 = Math.fround;

/** C#'s Math.Round: halves go to the even neighbour (22.5 → 22, 23.5 → 24). */
function roundHalfEven(value: number): number {
    var floor = Math.floor(value);
    var diff = value - floor;
    if (diff > 0.5) return floor + 1;
    if (diff < 0.5) return floor;
    return floor % 2 === 0 ? floor : floor + 1;
}

/** C#'s Math.Round(value, digits) for bonus-line values (0.001 steps). */
function roundTo(value: number, digits: number): number {
    var scale = Math.pow(10, digits);
    return roundHalfEven(value * scale) / scale;
}

function clamp(value: number, min: number, max: number): number {
    return value < min ? min : value > max ? max : value;
}

/** A world point, Y up (Unity axes). */
interface Vec3 { x: number; y: number; z: number; }

/** A ground-plane point or direction: x and z of a world point. */
interface Vec2 { x: number; z: number; }

function vec3(x: number, y: number, z: number): Vec3 {
    return { x: x, y: y, z: z };
}

function vec3From(a: number[]): Vec3 {
    return { x: a[0], y: a[1], z: a[2] };
}

function flatDistance(a: Vec2, b: Vec2): number {
    var dx = a.x - b.x, dz = a.z - b.z;
    return Math.sqrt(dx * dx + dz * dz);
}

function flatDistanceSq(a: Vec2, b: Vec2): number {
    var dx = a.x - b.x, dz = a.z - b.z;
    return dx * dx + dz * dz;
}

/** Unit ground-plane direction from a to b, or null when they're on top of each other. */
function flatDirection(from: Vec2, to: Vec2): Vec2 | null {
    var dx = to.x - from.x, dz = to.z - from.z;
    var length = Math.sqrt(dx * dx + dz * dz);
    return length < 1e-6 ? null : { x: dx / length, z: dz / length };
}

/** Unity yaw in degrees (0 = +Z, 90 = +X) to a ground-plane direction. */
function yawToDirection(yawDegrees: number): Vec2 {
    var r = yawDegrees * Math.PI / 180;
    return { x: Math.sin(r), z: Math.cos(r) };
}

function directionToYaw(dir: Vec2): number {
    return Math.atan2(dir.x, dir.z) * 180 / Math.PI;
}

/** Angle in degrees between two ground-plane directions (needn't be normalised). */
function flatAngle(a: Vec2, b: Vec2): number {
    var la = Math.sqrt(a.x * a.x + a.z * a.z), lb = Math.sqrt(b.x * b.x + b.z * b.z);
    if (la < 1e-6 || lb < 1e-6) return 0;
    var cos = clamp((a.x * b.x + a.z * b.z) / (la * lb), -1, 1);
    return Math.acos(cos) * 180 / Math.PI;
}

/** Rounds for the wire: positions to cm, so snapshots stay small. */
function q2(value: number): number {
    return Math.round(value * 100) / 100;
}

function isFiniteNumber(value: any): value is number {
    return typeof value === 'number' && isFinite(value);
}

function keysOf(obj: { [key: string]: any }): string[] {
    var keys: string[] = [];
    for (var k in obj) if (Object.prototype.hasOwnProperty.call(obj, k)) keys.push(k);
    return keys;
}

function indexOf<T>(list: T[] | null | undefined, value: T): number {
    if (!list) return -1;
    for (var i = 0; i < list.length; i++) if (list[i] === value) return i;
    return -1;
}

function deepCopy<T>(value: T): T {
    return JSON.parse(JSON.stringify(value));
}
