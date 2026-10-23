// A small test runner: tests compile into one script with src/ (same global scope as the module)
// and run under Node. `npm test` builds build-test/tests.js and runs it.

declare function require(name: string): any;
declare var process: any;
declare var __dirname: string;
declare var console: { log(...args: any[]): void };

interface TestCase { name: string; fn: () => void; }

var TESTS: TestCase[] = [];

function test(name: string, fn: () => void): void { TESTS.push({ name: name, fn: fn }); }

function fail(message: string): never { throw new Error(message); }

function ok(condition: any, message?: string): void { if (!condition) fail(message || 'expected true'); }

function eq(actual: any, expected: any, message?: string): void {
    var a = JSON.stringify(actual), e = JSON.stringify(expected);
    if (a !== e) fail((message ? message + ': ' : '') + 'expected ' + e + ', got ' + a);
}

function near(actual: number, expected: number, tolerance: number, message?: string): void {
    if (!(Math.abs(actual - expected) <= tolerance)) fail((message ? message + ': ' : '') + 'expected ' + expected + ' ± ' + tolerance + ', got ' + actual);
}

function between(actual: number, min: number, max: number, message?: string): void {
    if (!(actual >= min && actual <= max)) fail((message ? message + ': ' : '') + 'expected ' + min + '..' + max + ', got ' + actual);
}

function throwsError(fn: () => void, message?: string): void {
    try { fn(); } catch (e) { return; }
    fail(message || 'expected an error');
}

var testDataCache: GameData | null = null;

/** The real shared/gamedata files. Each call gets a fresh copy, so tests may edit it. */
function testData(): GameData {
    if (!testDataCache) {
        var fs = require('fs'), path = require('path');
        var dir = path.join(__dirname, '..', '..', 'shared', 'gamedata');
        testDataCache = parseGameData(function (file) { return fs.readFileSync(path.join(dir, file), 'utf8'); });
    }
    // The grids are read-only and large, so copies share them.
    var cached = testDataCache as GameData;
    var grids = cached.navGrids;
    cached.navGrids = {};
    var copy = deepCopy(cached);
    cached.navGrids = grids;
    copy.navGrids = grids;
    return copy;
}

/** An RNG that returns the given doubles from next() and the given values from int(), in order. */
function fixedRng(doubles: number[], ints?: number[]): Rng {
    var d = doubles.slice(), n = (ints || []).slice();
    return {
        next: function () { if (d.length === 0) fail('fixedRng ran out of doubles'); return d.shift() as number; },
        int: function (min: number, maxExclusive: number) {
            if (n.length === 0) fail('fixedRng ran out of ints');
            var v = n.shift() as number;
            if (v < min || v >= maxExclusive) fail('fixedRng int ' + v + ' outside ' + min + '..' + (maxExclusive - 1));
            return v;
        },
    };
}

function counterIds(prefix?: string): () => string {
    var n = 0;
    return function () { return (prefix || 'id-') + (++n); };
}

function runTests(): void {
    var filter = process.argv[2];
    var passed = 0, failed = 0;
    TESTS.forEach(function (t) {
        if (filter && t.name.indexOf(filter) < 0) return;
        try {
            t.fn();
            passed++;
        } catch (e) {
            failed++;
            console.log('FAIL ' + t.name + '\n     ' + (e && (e as any).stack ? (e as any).stack.split('\n').slice(0, 3).join('\n     ') : e));
        }
    });
    console.log(passed + ' passed, ' + failed + ' failed');
    process.exitCode = failed > 0 ? 1 : 0;
}
