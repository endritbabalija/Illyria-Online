// Items and gold, ported from the client's pure C# (Wuxia.Items: ItemInstance, ItemFactory,
// Inventory, Equipment, Wallet, StatCalculator, LootRoller, PlayerItems). On the server these are
// the real thing: every change goes through Belongings and becomes a ledger row.

/** One rolled bonus line on a Fine or Rare item, e.g. hp +18 or dodge +0.02. */
interface BonusLine { stat: string; value: number; }

/** One real item. Saved as-is in the character document. */
interface ItemInstance {
    /** Unique id (a UUID on the server), traced through the ledger. */
    id: string;
    /** Item id from items.json. */
    def: string;
    rarity: string;
    /** 1 for gear; the stack size for potions. */
    count: number;
    lines: BonusLine[];
}

function emptyStats(): ItemStats {
    return {
        minDamage: 0, maxDamage: 0, defense: 0, hp: 0, spirit: 0, accuracy: 0, dodge: 0,
        skillDamage: 0, skillDefense: 0, poisonResist: 0, paralysisResist: 0,
    };
}

var FLOAT_STATS = ['accuracy', 'dodge', 'skillDamage', 'poisonResist', 'paralysisResist'];

function addStats(total: ItemStats, other: Partial<ItemStats> | null | undefined): void {
    if (!other) return;
    for (var k in other) {
        if (!other.hasOwnProperty(k)) continue;
        var key = k as keyof ItemStats;
        var v = (other as any)[key] || 0;
        total[key] = indexOf(FLOAT_STATS, key) >= 0 ? f32(total[key] + f32(v)) : total[key] + v;
    }
}

/** Adds one bonus-line value; "damage" raises both ends of the damage range. */
function addStatLine(total: ItemStats, stat: string, value: number): void {
    var whole = roundHalfEven(value);
    switch (stat) {
        case 'damage': total.minDamage += whole; total.maxDamage += whole; break;
        case 'defense': total.defense += whole; break;
        case 'hp': total.hp += whole; break;
        case 'spirit': total.spirit += whole; break;
        case 'accuracy': total.accuracy = f32(total.accuracy + f32(value)); break;
        case 'dodge': total.dodge = f32(total.dodge + f32(value)); break;
        case 'skillDamage': total.skillDamage = f32(total.skillDamage + f32(value)); break;
        case 'skillDefense': total.skillDefense += whole; break;
        case 'poisonResist': total.poisonResist = f32(total.poisonResist + f32(value)); break;
        case 'paralysisResist': total.paralysisResist = f32(total.paralysisResist + f32(value)); break;
        default: throw new Error("Unknown stat '" + stat + "'");
    }
}

/** The definition's stats plus every bonus line. */
function itemTotalStats(data: GameData, item: ItemInstance): ItemStats {
    var total = emptyStats();
    addStats(total, getItem(data, item.def).stats);
    for (var i = 0; i < item.lines.length; i++) addStatLine(total, item.lines[i].stat, item.lines[i].value);
    return total;
}

/** What an NPC pays for one: sell price × the rarity's multiplier, rounded down. */
function sellPriceEach(data: GameData, item: ItemInstance): number {
    var def = getItem(data, item.def);
    var rarity = findBy(data.items.rarities, item.rarity);
    return Math.floor(f32(def.sellPrice * f32(rarity ? rarity.sellMultiplier : 1)));
}

function describeItem(data: GameData, item: ItemInstance): string {
    var def = getItem(data, item.def);
    var rarity = findBy(data.items.rarities, item.rarity);
    return def.name + (item.count > 1 ? ' x' + item.count : '') + (rarity && rarity.id !== 'common' ? ' (' + rarity.name + ')' : '') + ' #' + item.id;
}

// ---- ItemFactory ------------------------------------------------------------------------------

/** Makes item instances with unique ids and rolls rarity and bonus lines for drops. */
class ItemFactory {
    data: GameData;
    nextId: () => string;

    constructor(data: GameData, nextId: () => string) {
        this.data = data;
        this.nextId = nextId;
    }

    /** A plain Common copy (shop stock, starter kits). */
    create(defId: string, count?: number, rarity?: string, lines?: BonusLine[]): ItemInstance {
        var def = getItem(this.data, defId);
        var n = count === undefined ? 1 : count;
        if (n < 1 || n > Math.max(1, def.stackSize)) throw new Error(defId + ': count ' + n + ' outside 1..' + def.stackSize);
        return { id: this.nextId(), def: def.id, rarity: rarity || 'common', count: n, lines: lines ? lines.slice() : [] };
    }

    /** A dropped copy: gear rolls a rarity and that many bonus lines; stackables are always Common. */
    createDrop(defId: string, count: number, rng: Rng): ItemInstance {
        var def = getItem(this.data, defId);
        if (!isGear(def)) return this.create(defId, count);
        var rarity = this.rollRarity(rng);
        return this.create(defId, 1, rarity.id, this.rollBonusLines(def, rarity.bonusLines, rng));
    }

    /** Picks a rarity by dropWeight. */
    rollRarity(rng: Rng): RarityData {
        var rarities = this.data.items.rarities;
        var total = f32(0);
        rarities.forEach(function (r) { total = f32(total + Math.max(0, r.dropWeight)); });
        if (total <= 0) return getRarity(this.data, 'common');
        var roll = f32(f32(rng.next()) * total);
        for (var i = 0; i < rarities.length; i++) {
            var w = Math.max(0, rarities[i].dropWeight);
            if (w <= 0) continue;
            if (roll < w) return rarities[i];
            roll = f32(roll - w);
        }
        for (var j = rarities.length - 1; j >= 0; j--) if (rarities[j].dropWeight > 0) return rarities[j];
        return getRarity(this.data, 'common');
    }

    /** Up to `count` lines from the pool entries allowing the item's slot, weighted, each stat at most once. */
    rollBonusLines(def: ItemDef, count: number, rng: Rng): BonusLine[] {
        var lines: BonusLine[] = [];
        if (count <= 0 || !isGear(def)) return lines;
        var pool = this.data.items.bonusLines.filter(function (e) { return e.weight > 0 && indexOf(e.slots, def.slot) >= 0; });
        while (lines.length < count && pool.length > 0) {
            var total = f32(0);
            pool.forEach(function (e) { total = f32(total + e.weight); });
            var roll = f32(f32(rng.next()) * total);
            var pick = pool.length - 1;
            for (var i = 0; i < pool.length; i++) {
                if (roll < pool[i].weight) { pick = i; break; }
                roll = f32(roll - pool[i].weight);
            }
            var line = pool.splice(pick, 1)[0];
            var value = f32(line.min + f32(f32(rng.next()) * f32(line.max - line.min)));
            // Stored as the rounded decimal (0.012); sums widen it to a single like the client's float.
            value = isWholeStat(line.stat) ? roundHalfEven(value) : roundTo(value, 3);
            lines.push({ stat: line.stat, value: value });
        }
        return lines;
    }
}

// ---- Loot -------------------------------------------------------------------------------------

/** One pile on the ground: gold or one item (a gear piece or a potion stack). */
interface LootDrop { gold: number; item: ItemInstance | null; }

/** Rolls a loot table: gold (goldChance, goldMin..goldMax), then each entry independently. */
function rollLoot(data: GameData, table: LootTableData | null, factory: ItemFactory, rng: Rng): LootDrop[] {
    var drops: LootDrop[] = [];
    if (!table) return drops;
    if (table.goldMax > 0 && rng.next() < f32(table.goldChance))
        drops.push({ gold: rng.int(Math.max(1, table.goldMin), Math.max(1, table.goldMax) + 1), item: null });
    if (!table.drops) return drops;
    for (var i = 0; i < table.drops.length; i++) {
        var entry = table.drops[i];
        if (rng.next() >= f32(entry.chance)) continue;
        var def = getItem(data, entry.item);
        var min = entry.min === undefined ? 1 : entry.min, max = entry.max === undefined ? 1 : entry.max;
        var count = rng.int(Math.max(1, min), Math.max(min, max) + 1);
        if (isStackable(def)) {
            while (count > 0) {
                var pile = Math.min(count, def.stackSize);
                drops.push({ gold: 0, item: factory.createDrop(def.id, pile, rng) });
                count -= pile;
            }
        } else {
            for (var n = 0; n < count; n++) drops.push({ gold: 0, item: factory.createDrop(def.id, 1, rng) });
        }
    }
    return drops;
}

// ---- Stats ------------------------------------------------------------------------------------

/** A character's final combat numbers: combat.json base plus everything worn. */
interface CharacterStats {
    characterId: string;
    /** The weapon type (combat.json) the basic attack comes from. */
    weaponType: string;
    maxHp: number;
    maxSpirit: number;
    defense: number;
    dodge: number;
    resist: ResistData;
    attack: AttackData;
    skillDamage: number;
    skillDefense: number;
    gear: ItemStats;
}

/**
 * StatCalculator.Compute: the weapon item's damage replaces its type's base damage (range,
 * interval and crit stay per type); other gear adds; dodge and resistances are capped.
 */
function computeStats(data: GameData, characterId: string, weapon: ItemInstance | null, worn: ItemInstance[]): CharacterStats {
    var character = getCharacter(data, characterId);
    var player = data.combat.player;
    var caps = data.items.statCaps;
    var weaponType = weapon ? getItem(data, weapon.def).weaponType : character.weapons[0];
    var base = getWeapon(data, weaponType).attack;

    var gear = emptyStats();
    for (var i = 0; i < worn.length; i++) addStats(gear, itemTotalStats(data, worn[i]));

    var minDamage = Math.max(1, gear.minDamage + (weapon ? 0 : base.minDamage));
    var maxDamage = Math.max(minDamage, gear.maxDamage + (weapon ? 0 : base.maxDamage));
    var baseResist = player.resist || { poison: 0, paralysis: 0, stun: 0 };
    function cap(value: number, limit: number) { return Math.max(0, Math.min(value, limit)); }

    return {
        characterId: characterId,
        weaponType: weaponType,
        maxHp: Math.max(1, player.maxHp + gear.hp),
        maxSpirit: Math.max(0, player.maxSpirit + gear.spirit),
        defense: Math.max(0, player.defense + gear.defense),
        dodge: cap(f32(f32(player.dodge) + gear.dodge), f32(caps.dodge)),
        resist: {
            poison: cap(f32(f32(baseResist.poison) + gear.poisonResist), f32(caps.resist)),
            paralysis: cap(f32(f32(baseResist.paralysis) + gear.paralysisResist), f32(caps.resist)),
            stun: baseResist.stun,
        },
        attack: {
            minDamage: minDamage,
            maxDamage: maxDamage,
            interval: base.interval,
            range: base.range,
            reachHeight: base.reachHeight,
            accuracy: f32(f32(base.accuracy) + gear.accuracy),
            critChance: base.critChance,
            critMultiplier: base.critMultiplier,
        },
        skillDamage: Math.max(0, gear.skillDamage),
        skillDefense: Math.max(0, gear.skillDefense),
        gear: gear,
    };
}

// ---- Belongings (PlayerItems) -----------------------------------------------------------------

type EquipBlock = 'none' | 'notFound' | 'notGear' | 'wrongCharacter' | 'levelTooLow' | 'bagFull' | 'weaponRequired' | 'noOtherWeapon';
type PickupBlock = 'none' | 'bagFull';
type ShopBlock = 'none' | 'notSold' | 'badCount' | 'notEnoughGold' | 'bagFull' | 'notFound' | 'equipped' | 'goldLimit';
type UseBlock = 'none' | 'dead' | 'noneLeft' | 'notUsable' | 'cooldown' | 'alreadyFull';
type LedgerKind = 'goldIn' | 'goldOut' | 'itemIn' | 'itemOut';

var QUICK_SLOT_COUNT = 2;
var MAX_GOLD = 2147483647;

/** One gold or item change. The server writes each as a ledger row with the character save. */
interface LedgerEntry {
    kind: LedgerKind;
    reason: string;
    gold: number;
    goldAfter: number;
    itemId: string | null;
    defId: string | null;
    rarity: string | null;
    count: number;
}

var LedgerReasons = {
    loot: 'loot',
    buy: 'shop buy',
    sell: 'shop sell',
    potion: 'potion',
    starterKit: 'starter kit',
};

/** Whoever drinks a potion: HP and Spirit to top up. */
interface Vitals {
    isDead(): boolean;
    hp(): number;
    maxHp(): number;
    spirit(): number;
    maxSpirit(): number;
    restoreHp(amount: number): void;
    restoreSpirit(amount: number): void;
}

/** What's saved of a character's belongings. */
interface BelongingsSave {
    gold: number;
    bag: (ItemInstance | null)[];
    equipment: { [slot: string]: ItemInstance | null };
    quickSlots: (string | null)[];
    /** Weapon type → id of the weapon of that type last swapped out, for Swap. */
    lastWeapon: { [weaponType: string]: string };
}

function emptyBelongings(data: GameData): BelongingsSave {
    var bag: (ItemInstance | null)[] = [];
    for (var i = 0; i < data.items.inventory.capacity; i++) bag.push(null);
    var equipment: { [slot: string]: ItemInstance | null } = {};
    GEAR_SLOTS.forEach(function (s) { equipment[s] = null; });
    return { gold: 0, bag: bag, equipment: equipment, quickSlots: [null, null], lastWeapon: {} };
}

/**
 * Everything one character owns and wears: gold, the bag, the 6 gear slots, the 2 potion quick
 * slots and potion cooldowns. The one place gold and items change: every change appends to
 * `ledger` (drained by the caller and written next to the save). Time passed in.
 */
class Belongings {
    data: GameData;
    factory: ItemFactory;
    characterId: string;
    /** Level used for item requirements: items.json offline.playerLevel until levels exist (M7). */
    playerLevel: number;
    s: BelongingsSave;
    cooldownEnds: { [group: string]: number } = {};
    ledger: LedgerEntry[] = [];
    /** Bumped on every change, so the match knows to resend and save. */
    version = 0;

    constructor(data: GameData, factory: ItemFactory, characterId: string, save: BelongingsSave) {
        this.data = data;
        this.factory = factory;
        this.characterId = characterId;
        this.playerLevel = data.items.offline.playerLevel;
        this.s = save;
        // A save from before a capacity change keeps its items and gets the new size.
        while (this.s.bag.length < data.items.inventory.capacity) this.s.bag.push(null);
        GEAR_SLOTS.forEach(function (slot) { if (save.equipment[slot] === undefined) save.equipment[slot] = null; });
        while (this.s.quickSlots.length < QUICK_SLOT_COUNT) this.s.quickSlots.push(null);
        if (!this.s.lastWeapon) this.s.lastWeapon = {};
    }

    get gold(): number { return this.s.gold; }
    get weapon(): ItemInstance | null { return this.s.equipment['weapon']; }

    worn(): ItemInstance[] {
        var out: ItemInstance[] = [];
        var eq = this.s.equipment;
        GEAR_SLOTS.forEach(function (slot) { if (eq[slot]) out.push(eq[slot] as ItemInstance); });
        return out;
    }

    computeStats(): CharacterStats { return computeStats(this.data, this.characterId, this.weapon, this.worn()); }

    def(item: ItemInstance): ItemDef { return getItem(this.data, item.def); }

    // ---- bag (Inventory) ----

    bagIndexOf(itemId: string): number {
        for (var i = 0; i < this.s.bag.length; i++) { var it = this.s.bag[i]; if (it && it.id === itemId) return i; }
        return -1;
    }

    bagFind(itemId: string): ItemInstance | null {
        var i = this.bagIndexOf(itemId);
        return i >= 0 ? this.s.bag[i] : null;
    }

    countOf(defId: string): number {
        var n = 0;
        this.s.bag.forEach(function (it) { if (it && it.def === defId) n += it.count; });
        return n;
    }

    findFirst(defId: string): ItemInstance | null {
        for (var i = 0; i < this.s.bag.length; i++) { var it = this.s.bag[i]; if (it && it.def === defId) return it; }
        return null;
    }

    freeSlots(): number {
        var n = 0;
        this.s.bag.forEach(function (it) { if (!it) n++; });
        return n;
    }

    canAdd(def: ItemDef, count: number): boolean {
        if (count <= 0) return false;
        var stackSize = Math.max(1, def.stackSize), room = 0;
        this.s.bag.forEach(function (it) {
            if (!it) room += stackSize;
            else if (stackSize > 1 && it.def === def.id) room += stackSize - it.count;
        });
        return room >= count;
    }

    /** All or nothing; potions top up stacks first. The instance's count may drop to 0 if fully merged. */
    tryAdd(item: ItemInstance): boolean {
        if (!item || item.count <= 0 || !this.canAdd(this.def(item), item.count)) return false;
        this.addUnchecked(item);
        return true;
    }

    /** Adds every item or none (shop buys). */
    tryAddAll(items: ItemInstance[]): boolean {
        var savedBag = deepCopy(this.s.bag);
        var savedCounts = items.map(function (i) { return i.count; });
        for (var i = 0; i < items.length; i++) {
            if (items[i].count > 0 && this.canAdd(this.def(items[i]), items[i].count)) {
                this.addUnchecked(items[i]);
                continue;
            }
            this.s.bag = savedBag;
            items.forEach(function (it, n) { it.count = savedCounts[n]; });
            return false;
        }
        return true;
    }

    private addUnchecked(item: ItemInstance): void {
        var stackSize = Math.max(1, this.def(item).stackSize);
        var bag = this.s.bag, i: number;
        if (stackSize > 1)
            for (i = 0; i < bag.length && item.count > 0; i++) {
                var s = bag[i];
                if (!s || s === item || s.def !== item.def || s.count >= stackSize) continue;
                var moved = Math.min(item.count, stackSize - s.count);
                s.count += moved;
                item.count -= moved;
            }
        if (item.count <= 0) return;
        for (i = 0; i < bag.length; i++)
            if (!bag[i]) { bag[i] = item; return; }
        throw new Error('Bag ran out of room after canAdd passed');
    }

    /** Takes `count` from a bag stack; returns a snapshot of what was taken, or null. */
    private take(itemId: string, count: number): ItemInstance | null {
        var index = this.bagIndexOf(itemId);
        if (index < 0 || count <= 0) return null;
        var item = this.s.bag[index] as ItemInstance;
        if (item.count < count) return null;
        item.count -= count;
        if (item.count === 0) this.s.bag[index] = null;
        var taken = deepCopy(item);
        taken.count = count;
        return taken;
    }

    /** Bag move: empty slot moves it, same potion merges (as much as fits), anything else swaps. */
    move(from: number, to: number): boolean {
        var bag = this.s.bag;
        if (!(from >= 0 && from < bag.length && to >= 0 && to < bag.length) || from === to || from % 1 !== 0 || to % 1 !== 0) return false;
        var a = bag[from];
        if (!a) return false;
        var b = bag[to];
        var stack = b ? this.def(b).stackSize : 1;
        if (b && stack > 1 && b.def === a.def && b.count < stack) {
            var moved = Math.min(a.count, stack - b.count);
            b.count += moved;
            a.count -= moved;
            if (a.count === 0) bag[from] = null;
        } else {
            bag[to] = a;
            bag[from] = b;
        }
        this.bump();
        return true;
    }

    // ---- gear ----

    checkWearable(item: ItemInstance | null): EquipBlock {
        if (!item) return 'notFound';
        var def = this.def(item);
        if (!isGear(def)) return 'notGear';
        if (!allowsCharacter(def, this.characterId)) return 'wrongCharacter';
        if (isWeaponDef(def) && indexOf(getCharacter(this.data, this.characterId).weapons, def.weaponType) < 0) return 'wrongCharacter';
        if (def.levelRequired > this.playerLevel) return 'levelTooLow';
        return 'none';
    }

    /** Wears an item from the bag; whatever was in its slot takes the item's bag slot. */
    equip(itemId: string): EquipBlock {
        var index = this.bagIndexOf(itemId);
        if (index < 0) return 'notFound';
        var item = this.s.bag[index] as ItemInstance;
        var block = this.checkWearable(item);
        if (block !== 'none') return block;
        var def = this.def(item);
        var old = this.s.equipment[def.slot];
        this.s.equipment[def.slot] = item;
        this.s.bag[index] = old;
        if (old && isWeaponDef(this.def(old))) this.s.lastWeapon[this.def(old).weaponType] = old.id;
        this.bump();
        return 'none';
    }

    /** Takes a worn item off into the bag. The weapon slot can't be emptied. */
    unequip(slot: string): EquipBlock {
        if (indexOf(GEAR_SLOTS as string[], slot) < 0) return 'notFound';
        var item = this.s.equipment[slot];
        if (!item) return 'notFound';
        if (slot === 'weapon') return 'weaponRequired';
        if (!this.canAdd(this.def(item), item.count)) return 'bagFull';
        this.s.equipment[slot] = null;
        this.tryAdd(item);
        this.bump();
        return 'none';
    }

    /** The character's next weapon type with a usable weapon in the bag: the one last swapped out, else the best average damage. */
    findSwapWeapon(): ItemInstance | null {
        var types = getCharacter(this.data, this.characterId).weapons;
        var current = this.weapon;
        var currentType = current ? this.def(current).weaponType : null;
        var start = currentType ? indexOf(types, currentType) : -1;
        for (var step = 1; step <= types.length; step++) {
            var type = types[((start + step) % types.length + types.length) % types.length];
            if (currentType && type === currentType) continue;
            var best: ItemInstance | null = null, bestScore = -Infinity;
            for (var i = 0; i < this.s.bag.length; i++) {
                var item = this.s.bag[i];
                if (!item) continue;
                var def = this.def(item);
                if (!isWeaponDef(def) || def.weaponType !== type || this.checkWearable(item) !== 'none') continue;
                if (this.s.lastWeapon[type] === item.id) return item;
                var st = itemTotalStats(this.data, item);
                var score = (st.minDamage + st.maxDamage) * 0.5;
                if (score > bestScore) { best = item; bestScore = score; }
            }
            if (best) return best;
        }
        return null;
    }

    swapWeapon(): EquipBlock {
        var next = this.findSwapWeapon();
        return next ? this.equip(next.id) : 'noOtherWeapon';
    }

    // ---- loot and gold ----

    checkPickUp(drop: LootDrop): PickupBlock {
        if (!drop.item) return this.s.gold + drop.gold <= MAX_GOLD ? 'none' : 'bagFull';
        return this.canAdd(this.def(drop.item), drop.item.count) ? 'none' : 'bagFull';
    }

    /** Auto-loot. bagFull leaves the drop untouched. */
    pickUp(drop: LootDrop, source: string): PickupBlock {
        var block = this.checkPickUp(drop);
        if (block !== 'none') return block;
        var reason = reasonWith(LedgerReasons.loot, source);
        if (!drop.item) {
            this.s.gold += drop.gold;
            this.record('goldIn', reason, drop.gold);
        } else {
            var count = drop.item.count;
            var snapshot = deepCopy(drop.item);
            this.tryAdd(drop.item);
            this.record('itemIn', reason, 0, snapshot, count);
        }
        this.bump();
        return 'none';
    }

    addGold(amount: number, reason: string): boolean {
        if (amount === 0) return true;
        if (amount < 0 || this.s.gold + amount > MAX_GOLD) return false;
        this.s.gold += amount;
        this.record('goldIn', reason, amount);
        this.bump();
        return true;
    }

    // ---- shop ----

    checkBuy(shop: ShopData, defId: string, count: number): { block: ShopBlock; cost: number } {
        if (!(count >= 1) || count % 1 !== 0) return { block: 'badCount', cost: 0 };
        var def = findBy(this.data.items.items, defId);
        if (!def || def.buyPrice <= 0 || indexOf(shop.items, defId) < 0) return { block: 'notSold', cost: 0 };
        var cost = def.buyPrice * count;
        if (cost > MAX_GOLD) return { block: 'badCount', cost: 0 };
        if (cost > this.s.gold) return { block: 'notEnoughGold', cost: cost };
        if (!this.canAdd(def, count)) return { block: 'bagFull', cost: cost };
        return { block: 'none', cost: cost };
    }

    /** Atomic: gold and bag both change or neither does. */
    buy(shopId: string, defId: string, count: number): ShopBlock {
        var shop = findBy(this.data.items.shops, shopId);
        if (!shop) return 'notSold';
        var check = this.checkBuy(shop, defId, count);
        if (check.block !== 'none') return check.block;
        var def = getItem(this.data, defId);
        var bought: ItemInstance[] = [];
        for (var left = count; left > 0;) {
            var n = Math.min(left, Math.max(1, def.stackSize));
            bought.push(this.factory.create(def.id, n));
            left -= n;
        }
        var snapshots = deepCopy(bought);
        if (!this.tryAddAll(bought)) return 'bagFull';
        this.s.gold -= check.cost;
        var reason = reasonWith(LedgerReasons.buy, shop.name);
        this.record('goldOut', reason, check.cost);
        for (var i = 0; i < snapshots.length; i++) this.record('itemIn', reason, 0, snapshots[i], snapshots[i].count);
        this.bump();
        return 'none';
    }

    checkSell(itemId: string, count: number): { block: ShopBlock; value: number } {
        if (!(count >= 1) || count % 1 !== 0) return { block: 'badCount', value: 0 };
        var eq = this.s.equipment;
        if (GEAR_SLOTS.some(function (s) { return !!eq[s] && (eq[s] as ItemInstance).id === itemId; })) return { block: 'equipped', value: 0 };
        var item = this.bagFind(itemId);
        if (!item || item.count < count) return { block: 'notFound', value: 0 };
        var value = sellPriceEach(this.data, item) * count;
        if (this.s.gold + value > MAX_GOLD) return { block: 'goldLimit', value: 0 };
        return { block: 'none', value: value };
    }

    /** Sells from the bag to an NPC shop. Worn items can't be sold. */
    sell(itemId: string, count: number): ShopBlock {
        var check = this.checkSell(itemId, count);
        if (check.block !== 'none') return check.block;
        this.s.gold += check.value;
        var taken = this.take(itemId, count) as ItemInstance;
        this.record('itemOut', LedgerReasons.sell, 0, taken, count);
        this.record('goldIn', LedgerReasons.sell, check.value);
        this.bump();
        return 'none';
    }

    // ---- potions ----

    /** Puts a usable item (by id) on a quick slot; if it's on the other slot the two trade places. */
    assignQuickSlot(index: number, defId: string): boolean {
        if (!(index >= 0 && index < QUICK_SLOT_COUNT) || index % 1 !== 0 || !defId) return false;
        var def = findBy(this.data.items.items, defId);
        if (!def || !isUsable(def)) return false;
        var q = this.s.quickSlots;
        if (q[index] === defId) return true;
        var other = indexOf(q, defId);
        if (other >= 0) q[other] = q[index];
        q[index] = defId;
        this.bump();
        return true;
    }

    private cooldownKey(def: ItemDef): string {
        return def.use && def.use.cooldownGroup ? def.use.cooldownGroup : def.id;
    }

    cooldownLeft(defId: string, now: number): number {
        var def = findBy(this.data.items.items, defId);
        if (!def || !def.use) return 0;
        var end = this.cooldownEnds[this.cooldownKey(def)];
        return end === undefined ? 0 : Math.max(0, end - now);
    }

    checkUse(def: ItemDef | null, now: number, vitals: Vitals): UseBlock {
        if (vitals.isDead()) return 'dead';
        if (!def || !isUsable(def)) return 'notUsable';
        if (this.countOf(def.id) <= 0) return 'noneLeft';
        if (this.cooldownLeft(def.id, now) > 0) return 'cooldown';
        var use = def.use as ItemUseData;
        var hpNeeded = use.hp > 0 && vitals.hp() < vitals.maxHp();
        var spiritNeeded = use.spirit > 0 && vitals.spirit() < vitals.maxSpirit();
        return hpNeeded || spiritNeeded ? 'none' : 'alreadyFull';
    }

    useQuickSlot(index: number, now: number, vitals: Vitals): UseBlock {
        var defId = index >= 0 && index < QUICK_SLOT_COUNT ? this.s.quickSlots[index] : null;
        return defId ? this.use(defId, now, vitals) : 'notUsable';
    }

    /** Drinks one: restores HP/Spirit up to max and starts its cooldown group. */
    use(defId: string, now: number, vitals: Vitals): UseBlock {
        var def = findBy(this.data.items.items, defId);
        var block = this.checkUse(def, now, vitals);
        if (block !== 'none') return block;
        var use = (def as ItemDef).use as ItemUseData;
        var hp = Math.max(0, Math.min(use.hp, vitals.maxHp() - vitals.hp()));
        var spirit = Math.max(0, Math.min(use.spirit, Math.ceil(vitals.maxSpirit() - vitals.spirit())));
        var stack = this.findFirst(defId) as ItemInstance;
        var taken = this.take(stack.id, 1) as ItemInstance;
        this.record('itemOut', LedgerReasons.potion, 0, taken, 1);
        if (hp > 0) vitals.restoreHp(hp);
        if (spirit > 0) vitals.restoreSpirit(spirit);
        this.cooldownEnds[this.cooldownKey(def as ItemDef)] = now + use.cooldown;
        this.bump();
        return 'none';
    }

    // ---- starter kit ----

    /** Fills an empty character with its starter kit (gold, worn gear, bag items, quick slots). */
    giveStarterKit(): void {
        var kit = getStarterKit(this.data, this.characterId);
        if (kit.gold > 0) this.addGold(kit.gold, LedgerReasons.starterKit);
        var self = this;
        (kit.equip || []).forEach(function (id) {
            var item = self.factory.create(id);
            var block = self.checkWearable(item);
            if (block !== 'none') throw new Error('Starter kit for ' + self.characterId + ": can't wear " + id + ' (' + block + ')');
            self.s.equipment[self.def(item).slot] = item;
            self.record('itemIn', LedgerReasons.starterKit, 0, item, item.count);
        });
        (kit.bag || []).forEach(function (entry) {
            var item = self.factory.create(entry.item, entry.count);
            var snapshot = deepCopy(item);
            if (!self.tryAdd(item)) throw new Error('Starter kit for ' + self.characterId + " doesn't fit the bag");
            self.record('itemIn', LedgerReasons.starterKit, 0, snapshot, snapshot.count);
        });
        var defaults = this.data.items.inventory.quickSlots;
        for (var i = 0; i < QUICK_SLOT_COUNT; i++) this.s.quickSlots[i] = i < defaults.length ? defaults[i] : null;
        this.bump();
    }

    /** Hands over the ledger entries recorded since the last call. */
    drainLedger(): LedgerEntry[] {
        var out = this.ledger;
        this.ledger = [];
        return out;
    }

    private record(kind: LedgerKind, reason: string, gold: number, item?: ItemInstance, count?: number): void {
        this.ledger.push({
            kind: kind,
            reason: reason,
            gold: gold,
            goldAfter: this.s.gold,
            itemId: item ? item.id : null,
            defId: item ? item.def : null,
            rarity: item ? item.rarity : null,
            count: count || 0,
        });
    }

    private bump(): void { this.version++; }
}

function reasonWith(reason: string, detail: string | null | undefined): string {
    return detail ? reason + ': ' + detail : reason;
}

function describeLedger(data: GameData, e: LedgerEntry): string {
    switch (e.kind) {
        case 'goldIn': return '[Gold] +' + e.gold + ' (' + e.reason + ') -> ' + e.goldAfter;
        case 'goldOut': return '[Gold] -' + e.gold + ' (' + e.reason + ') -> ' + e.goldAfter;
        default:
            var def = e.defId ? findBy(data.items.items, e.defId) : null;
            return '[Loot] ' + (e.kind === 'itemIn' ? '+' : '-') + e.count + ' ' + (def ? def.name : e.defId) + ' [' + e.rarity + '] #' + e.itemId + ' (' + e.reason + ')';
    }
}
