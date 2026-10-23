// The walkable ground of a zone, exported from Unity's baked NavMesh by the editor tool
// Wuxia → World → Export Walkable Grid (WalkableGridExporter.cs) into
// shared/gamedata/navgrid/<zone>.json: one ground height per cell, or nothing where nobody can
// walk (walls, the river, rocks, trunks, the slopes outside the valley). The server checks moves
// against it, paths monsters through it and puts them on the ground.

interface NavGridFile {
    version: number;
    zone: string;
    navMeshSha256: string;
    cellSize: number;
    originX: number;
    originZ: number;
    width: number;
    depth: number;
    encoding: string;
    heights: string;
}

var NAVGRID_BLOCKED = -32768;
var BASE64_CHARS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';

/** Base64 → bytes (no atob or Buffer in Nakama's JavaScript runtime). */
function base64Bytes(text: string): number[] {
    var lookup: { [ch: string]: number } = {};
    for (var i = 0; i < BASE64_CHARS.length; i++) lookup[BASE64_CHARS.charAt(i)] = i;
    var bytes: number[] = [];
    var buffer = 0, bits = 0;
    for (var c = 0; c < text.length; c++) {
        var value = lookup[text.charAt(c)];
        if (value === undefined) continue; // padding, line breaks
        buffer = (buffer << 6) | value;
        bits += 6;
        if (bits >= 8) {
            bits -= 8;
            bytes.push((buffer >> bits) & 0xff);
        }
    }
    return bytes;
}

class NavGrid {
    zone: string;
    cellSize: number;
    originX: number;
    originZ: number;
    width: number;
    depth: number;
    /** Ground height in metres per cell (row by row, Z outer), or NaN where nobody can walk. */
    heights: number[];

    constructor(file: NavGridFile) {
        if (file.encoding !== 'int16le-cm-base64') throw new Error('navgrid ' + file.zone + ": unknown encoding '" + file.encoding + "'");
        this.zone = file.zone;
        this.cellSize = file.cellSize;
        this.originX = file.originX;
        this.originZ = file.originZ;
        this.width = file.width;
        this.depth = file.depth;
        var bytes = base64Bytes(file.heights);
        if (bytes.length !== file.width * file.depth * 2) throw new Error('navgrid ' + file.zone + ': ' + bytes.length + ' bytes for ' + file.width + 'x' + file.depth + ' cells');
        this.heights = [];
        for (var i = 0; i < bytes.length; i += 2) {
            var v = bytes[i] | (bytes[i + 1] << 8);
            if (v >= 0x8000) v -= 0x10000;
            this.heights.push(v === NAVGRID_BLOCKED ? NaN : v / 100);
        }
    }

    cellX(x: number): number { return Math.floor((x - this.originX) / this.cellSize); }
    cellZ(z: number): number { return Math.floor((z - this.originZ) / this.cellSize); }

    private open(cx: number, cz: number): boolean {
        return cx >= 0 && cz >= 0 && cx < this.width && cz < this.depth && !isNaN(this.heights[cz * this.width + cx]);
    }

    private centre(cx: number, cz: number): Vec3 {
        return vec3(this.originX + (cx + 0.5) * this.cellSize, this.heights[cz * this.width + cx], this.originZ + (cz + 0.5) * this.cellSize);
    }

    /** The ground height of the cell under the point, or null if nobody can walk there. */
    heightAt(p: Vec2): number | null {
        var cx = this.cellX(p.x), cz = this.cellZ(p.z);
        return this.open(cx, cz) ? this.heights[cz * this.width + cx] : null;
    }

    isWalkable(p: Vec2): boolean { return this.heightAt(p) !== null; }

    /** The centre of the nearest walkable cell within `radius` (the point's own cell first), or null. */
    nearestWalkable(p: Vec2, radius: number): Vec3 | null {
        var cx = this.cellX(p.x), cz = this.cellZ(p.z);
        if (this.open(cx, cz)) return this.centre(cx, cz);
        var reach = Math.ceil(radius / this.cellSize);
        var best: Vec3 | null = null, bestSq = (radius + this.cellSize * 0.71) * (radius + this.cellSize * 0.71);
        for (var dz = -reach; dz <= reach; dz++)
            for (var dx = -reach; dx <= reach; dx++) {
                if (!this.open(cx + dx, cz + dz)) continue;
                var c = this.centre(cx + dx, cz + dz);
                var d = flatDistanceSq(c, p);
                if (d < bestSq) { best = c; bestSq = d; }
            }
        return best;
    }

    /** Ground height at or near a point: its own cell, else the nearest walkable cell within `radius`. */
    groundNear(p: Vec2, radius: number): number | null {
        var h = this.heightAt(p);
        if (h !== null) return h;
        var near = this.nearestWalkable(p, radius);
        return near ? near.y : null;
    }

    /** Can you walk straight from a to b without leaving walkable cells? */
    lineWalkable(a: Vec2, b: Vec2): boolean {
        var distance = flatDistance(a, b);
        var steps = Math.max(1, Math.ceil(distance / (this.cellSize * 0.25)));
        for (var i = 0; i <= steps; i++) {
            var t = i / steps;
            if (!this.isWalkable({ x: a.x + (b.x - a.x) * t, z: a.z + (b.z - a.z) * t })) return false;
        }
        return true;
    }

    /** The last walkable point on the straight line from a towards b (a itself if a isn't walkable). */
    walkableUntil(a: Vec2, b: Vec2): Vec2 {
        var distance = flatDistance(a, b);
        var steps = Math.max(1, Math.ceil(distance / (this.cellSize * 0.25)));
        var last: Vec2 = { x: a.x, z: a.z };
        for (var i = 1; i <= steps; i++) {
            var t = i / steps;
            var p = { x: a.x + (b.x - a.x) * t, z: a.z + (b.z - a.z) * t };
            if (!this.isWalkable(p)) break;
            last = p;
        }
        return last;
    }

    /**
     * A walkable route from `from` to `to` as waypoints (the last one is `to`, or the nearest
     * walkable spot to it), straightened where the ground allows; null if there is none within
     * `maxNodes` searched cells. A* over the cells, 8 directions, no cutting corners.
     */
    findPath(from: Vec2, to: Vec2, maxNodes: number): Vec2[] | null {
        var start = this.nearestWalkable(from, 1.5), goal = this.nearestWalkable(to, 2);
        if (!start || !goal) return null;
        var w = this.width;
        var sx = this.cellX(start.x), sz = this.cellZ(start.z), gx = this.cellX(goal.x), gz = this.cellZ(goal.z);
        var startIndex = sz * w + sx, goalIndex = gz * w + gx;
        var end: Vec2 = this.isWalkable(to) ? { x: to.x, z: to.z } : { x: goal.x, z: goal.z };
        if (startIndex === goalIndex) return [end];

        var g: { [cell: number]: number } = {};
        var came: { [cell: number]: number } = {};
        var closed: { [cell: number]: boolean } = {};
        var heap: number[] = [], heapF: number[] = [];
        function h(cell: number): number {
            var dx = Math.abs(cell % w - gx), dz = Math.abs(Math.floor(cell / w) - gz);
            return Math.max(dx, dz) + 0.41421356 * Math.min(dx, dz);
        }
        function push(cell: number, f: number) {
            heap.push(cell); heapF.push(f);
            var i = heap.length - 1;
            while (i > 0) {
                var parent = (i - 1) >> 1;
                if (heapF[parent] <= heapF[i]) break;
                var tc = heap[i]; heap[i] = heap[parent]; heap[parent] = tc;
                var tf = heapF[i]; heapF[i] = heapF[parent]; heapF[parent] = tf;
                i = parent;
            }
        }
        function pop(): number {
            var top = heap[0];
            var lastCell = heap.pop() as number, lastF = heapF.pop() as number;
            if (heap.length > 0) {
                heap[0] = lastCell; heapF[0] = lastF;
                var i = 0;
                for (;;) {
                    var l = i * 2 + 1, r = l + 1, m = i;
                    if (l < heap.length && heapF[l] < heapF[m]) m = l;
                    if (r < heap.length && heapF[r] < heapF[m]) m = r;
                    if (m === i) break;
                    var tc = heap[i]; heap[i] = heap[m]; heap[m] = tc;
                    var tf = heapF[i]; heapF[i] = heapF[m]; heapF[m] = tf;
                    i = m;
                }
            }
            return top;
        }

        g[startIndex] = 0;
        push(startIndex, h(startIndex));
        var expanded = 0, found = false;
        while (heap.length > 0) {
            var cell = pop();
            if (closed[cell]) continue;
            if (cell === goalIndex) { found = true; break; }
            closed[cell] = true;
            if (++expanded > maxNodes) return null;
            var cx = cell % w, cz = Math.floor(cell / w);
            for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++) {
                    if (dx === 0 && dz === 0) continue;
                    var nx = cx + dx, nz = cz + dz;
                    if (!this.open(nx, nz)) continue;
                    if (dx !== 0 && dz !== 0 && (!this.open(cx + dx, cz) || !this.open(cx, cz + dz))) continue;
                    var next = nz * w + nx;
                    if (closed[next]) continue;
                    var cost = g[cell] + (dx !== 0 && dz !== 0 ? 1.41421356 : 1);
                    if (g[next] !== undefined && g[next] <= cost) continue;
                    g[next] = cost;
                    came[next] = cell;
                    push(next, cost + h(next));
                }
        }
        if (!found) return null;

        var cells: Vec2[] = [];
        for (var c = goalIndex; c !== startIndex; c = came[c]) cells.push(this.centre(c % w, Math.floor(c / w)));
        cells.reverse();
        cells[cells.length - 1] = end;
        // String-pull: skip every waypoint the one before can see past.
        var path: Vec2[] = [];
        var anchor: Vec2 = from;
        var i = 0;
        while (i < cells.length) {
            var far = i;
            for (var j = cells.length - 1; j > i; j--)
                if (this.lineWalkable(anchor, cells[j])) { far = j; break; }
            path.push(cells[far]);
            anchor = cells[far];
            i = far + 1;
        }
        return path;
    }
}
