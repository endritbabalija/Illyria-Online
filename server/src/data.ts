// Types for shared/gamedata/*.json. Field names match the JSON keys and the client's mirrors in
// client/Assets/Scripts/Data (CombatData.cs, SkillData.cs, ItemData.cs); keep them in sync.

// ---- combat.json ------------------------------------------------------------------------------

interface AttackData {
    minDamage: number;
    maxDamage: number;
    interval: number;
    range: number;
    reachHeight: number;
    accuracy: number;
    critChance: number;
    critMultiplier: number;
}

interface ResistData { poison: number; paralysis: number; stun: number; }

interface PlayerData {
    maxHp: number;
    maxSpirit: number;
    spiritRegenPerSecond: number;
    defense: number;
    dodge: number;
    moveSpeed: number;
    turnSpeed: number;
    lockRange: number;
    loseTargetRange: number;
    regenDelay: number;
    regenPerSecond: number;
    respawnDelay: number;
    resist: ResistData;
}

interface CharacterData { id: string; name: string; weapons: string[]; }

interface WeaponData { id: string; name: string; attack: AttackData; }

interface MonsterData {
    id: string;
    name: string;
    level: number;
    maxHp: number;
    defense: number;
    dodge: number;
    wanderSpeed: number;
    chaseSpeed: number;
    aggroRange: number;
    leashRange: number;
    wanderRadius: number;
    respawnDelay: number;
    resist: ResistData;
    attack: AttackData;
}

interface CombatData {
    version: number;
    player: PlayerData;
    characters: CharacterData[];
    weapons: WeaponData[];
    monsters: MonsterData[];
}

// ---- skills.json ------------------------------------------------------------------------------

interface StatusData { id: string; name: string; disables: boolean; tickInterval: number; resist: string; }

interface StatusApplication { id: string; chance: number; duration: number; tickDamage: number; }

type SkillShape = 'single' | 'cone' | 'circle' | 'circleAtTarget';

interface FormData {
    id: string;
    name: string;
    unlockLevel: number;
    shape: SkillShape;
    range: number;
    radius: number;
    angle: number;
    spiritCost: number;
    cooldown: number;
    castTime: number;
    minDamage: number;
    maxDamage: number;
    status: StatusApplication;
}

interface BookData { id: string; name: string; weapon: string; requiredLevel: number; forms: FormData[]; }

interface LeapData {
    bookId: string;
    spiritCost: number;
    cooldown: number;
    maxDistance: number;
    maxRise: number;
    maxDrop: number;
    aimAngle: number;
    landingInset: number;
    leapDuration: number;
    arcHeight: number;
    dashDistance: number;
    dashDuration: number;
}

interface SkillData {
    version: number;
    skillPoints: { killsPerPoint: number; bookLevelCosts: number[]; damagePerBookLevel: number; };
    statuses: StatusData[];
    books: BookData[];
    drangueLeap: LeapData;
    dev: { learnAllBooks: boolean; startingSkillPoints: number; };
}

// ---- items.json -------------------------------------------------------------------------------

type GearSlot = 'weapon' | 'helm' | 'armor' | 'boots' | 'ring' | 'amulet';

var GEAR_SLOTS: GearSlot[] = ['weapon', 'helm', 'armor', 'boots', 'ring', 'amulet'];

interface ItemStats {
    minDamage: number;
    maxDamage: number;
    defense: number;
    hp: number;
    spirit: number;
    accuracy: number;
    dodge: number;
    skillDamage: number;
    skillDefense: number;
    poisonResist: number;
    paralysisResist: number;
}

interface ItemUseData { hp: number; spirit: number; cooldownGroup: string; cooldown: number; }

interface ItemDef {
    id: string;
    name: string;
    /** A gear slot, or '' for items that can't be worn. */
    slot: string;
    weaponType: string;
    characters: string[];
    levelRequired: number;
    stackSize: number;
    buyPrice: number;
    sellPrice: number;
    modelKey: string;
    stats: Partial<ItemStats>;
    use?: ItemUseData | null;
}

interface RarityData { id: string; name: string; color: string; bonusLines: number; dropWeight: number; sellMultiplier: number; }

interface BonusLineData { stat: string; min: number; max: number; weight: number; slots: string[]; }

interface StarterKitData { character: string; gold: number; equip: string[]; bag: { item: string; count: number }[]; }

interface ShopData { id: string; name: string; items: string[]; }

interface LootDropData { item: string; chance: number; min: number; max: number; }

interface LootTableData { id: string; monster: string; goldChance: number; goldMin: number; goldMax: number; drops: LootDropData[]; }

interface ItemsData {
    version: number;
    offline: { playerLevel: number };
    inventory: { capacity: number; quickSlots: string[] };
    loot: { autoLootRadius: number; groundTime: number; pickupDelay: number };
    statCaps: { dodge: number; resist: number };
    rarities: RarityData[];
    bonusLines: BonusLineData[];
    items: ItemDef[];
    starterKits: StarterKitData[];
    shops: ShopData[];
    lootTables: LootTableData[];
}

// ---- zones.json -------------------------------------------------------------------------------

interface LedgeData { name: string; centre: number[]; size: number[]; yaw: number; }

interface PackData { id: string; monster: string; position: number[]; count: number; radius: number; }

interface ShopkeeperData { id: string; shop: string; position: number[]; }

interface AreaData { name: string; minLevel: number; maxLevel: number; centre: number[]; size: number[]; priority: number; }

interface ZoneData {
    id: string;
    name: string;
    kind: 'town' | 'field';
    /** No combat at all (the town). */
    safe: boolean;
    channelCap: number;
    /** The walkable grid exported from the zone's NavMesh (path under shared/gamedata); none = no ground checks. */
    navGrid?: string;
    respawn: { position: number[]; yaw: number };
    shopkeepers: ShopkeeperData[];
    ledges: LedgeData[];
    packs: PackData[];
    areas: AreaData[];
}

interface ZonesData {
    version: number;
    server: {
        tickRate: number;
        /** Seconds between writes of changed characters (and their ledger rows). */
        saveInterval: number;
        /** A character joined to a match is locked to it this long without a refresh. */
        sessionLockSeconds: number;
        lockRefreshInterval: number;
        emptyMatchSeconds: number;
        /** Extra metres allowed on a player's reach checks, for positions that lag a little. */
        rangeTolerance: number;
    };
    movement: {
        speedTolerance: number;
        distanceSlack: number;
        maxGapSeconds: number;
        maxSlope: number;
        stepUp: number;
        ledgeClimbHeight: number;
        /** A move may end this far from a walkable cell (the NavMesh keeps its agent radius clear of walls). */
        gridSlack: number;
        /** How far a player may stand above or below the grid's ground height (except while falling). */
        heightTolerance: number;
    };
    monsterAi: {
        corpseTime: number;
        packAssistRadius: number;
        wanderPauseMin: number;
        wanderPauseMax: number;
        personalSpace: number;
        bodyRadius: number;
        chaseWindup: number;
        stoppingFraction: number;
        homeArriveDistance: number;
        /** Seconds between path recalculations while following a moving target. */
        repathInterval: number;
        /** Most grid cells one path search may visit; beyond that the target counts as unreachable. */
        pathMaxNodes: number;
    };
    playerRules: {
        castQueueTimeout: number;
        weaponDrawTime: number;
        shopInteractRange: number;
        shopLeaveRange: number;
        lootScatterMin: number;
        lootScatterMax: number;
    };
    zones: ZoneData[];
}

// ---- the bundle -------------------------------------------------------------------------------

interface GameData {
    combat: CombatData;
    skills: SkillData;
    items: ItemsData;
    zones: ZonesData;
    /** Walkable grids by zone id, for zones that have one. */
    navGrids: { [zoneId: string]: NavGrid };
}


function findBy<T extends { id: string }>(list: T[], id: string): T | null {
    for (var i = 0; i < list.length; i++) if (list[i].id === id) return list[i];
    return null;
}

function getBy<T extends { id: string }>(list: T[], id: string, what: string): T {
    var found = findBy(list, id);
    if (!found) throw new Error('No ' + what + " '" + id + "' in game data");
    return found;
}

function getCharacter(data: GameData, id: string): CharacterData { return getBy(data.combat.characters, id, 'character'); }
function getWeapon(data: GameData, id: string): WeaponData { return getBy(data.combat.weapons, id, 'weapon'); }
function getMonster(data: GameData, id: string): MonsterData { return getBy(data.combat.monsters, id, 'monster'); }
function getBook(data: GameData, id: string): BookData { return getBy(data.skills.books, id, 'book'); }
function getStatus(data: GameData, id: string): StatusData { return getBy(data.skills.statuses, id, 'status'); }
function getItem(data: GameData, id: string): ItemDef { return getBy(data.items.items, id, 'item'); }
function getRarity(data: GameData, id: string): RarityData { return getBy(data.items.rarities, id, 'rarity'); }
function getShop(data: GameData, id: string): ShopData { return getBy(data.items.shops, id, 'shop'); }
function getZone(data: GameData, id: string): ZoneData { return getBy(data.zones.zones, id, 'zone'); }

function getStarterKit(data: GameData, characterId: string): StarterKitData {
    var kits = data.items.starterKits;
    for (var i = 0; i < kits.length; i++) if (kits[i].character === characterId) return kits[i];
    throw new Error("No starter kit for character '" + characterId + "'");
}

function findLootTable(data: GameData, monsterId: string): LootTableData | null {
    var tables = data.items.lootTables;
    for (var i = 0; i < tables.length; i++) if (tables[i].monster === monsterId) return tables[i];
    return null;
}

/** The book a form belongs to and the form, or null. */
function findForm(data: GameData, formId: string): { book: BookData; form: FormData } | null {
    var books = data.skills.books;
    for (var b = 0; b < books.length; b++)
        for (var f = 0; f < books[b].forms.length; f++)
            if (books[b].forms[f].id === formId) return { book: books[b], form: books[b].forms[f] };
    return null;
}

/** Points to raise a book from `level` to the next; -1 at max level. */
function levelUpCost(data: GameData, level: number): number {
    var costs = data.skills.skillPoints.bookLevelCosts;
    return level >= 0 && level < costs.length ? costs[level] : -1;
}

function isGear(def: ItemDef): boolean { return !!def.slot; }
function isWeaponDef(def: ItemDef): boolean { return def.slot === 'weapon'; }
function isStackable(def: ItemDef): boolean { return def.stackSize > 1; }
function isUsable(def: ItemDef): boolean { return !!def.use && (def.use.hp > 0 || def.use.spirit > 0); }

function allowsCharacter(def: ItemDef, characterId: string): boolean {
    return !def.characters || def.characters.length === 0 || indexOf(def.characters, characterId) >= 0;
}

var WHOLE_STATS = ['damage', 'defense', 'hp', 'spirit', 'skillDefense'];
var ALL_STATS = ['damage', 'defense', 'hp', 'spirit', 'accuracy', 'dodge', 'skillDamage', 'skillDefense', 'poisonResist', 'paralysisResist'];

function isWholeStat(stat: string): boolean { return indexOf(WHOLE_STATS, stat) >= 0; }

/**
 * Parses the four JSON files and each zone's walkable grid (`read` takes a path relative to
 * shared/gamedata), fills defaults the way the client's FromJson does, and checks that every
 * cross-reference resolves. Throws with every problem listed.
 */
function parseGameData(read: (path: string) => string): GameData {
    var data: GameData = {
        combat: JSON.parse(read('combat.json')),
        skills: JSON.parse(read('skills.json')),
        items: JSON.parse(read('items.json')),
        zones: JSON.parse(read('zones.json')),
        navGrids: {},
    };
    data.zones.zones.forEach(function (z) {
        if (z.navGrid) data.navGrids[z.id] = new NavGrid(JSON.parse(read(z.navGrid)) as NavGridFile);
    });
    var items = data.items;
    if (!items.offline) items.offline = { playerLevel: 1 };
    if (!items.statCaps) items.statCaps = { dodge: 1, resist: 1 };
    if (!items.bonusLines) items.bonusLines = [];
    if (!items.inventory.quickSlots) items.inventory.quickSlots = [];
    for (var i = 0; i < items.items.length; i++) {
        var item = items.items[i];
        if (!item.stats) item.stats = {};
        if (!item.characters) item.characters = [];
        if (!item.slot) item.slot = '';
        if (!item.stackSize) item.stackSize = 1;
        if (item.use && item.use.hp <= 0 && item.use.spirit <= 0) item.use = null;
    }
    for (var r = 0; r < items.rarities.length; r++)
        if (items.rarities[r].sellMultiplier === undefined) items.rarities[r].sellMultiplier = 1;
    if (!data.skills.dev) data.skills.dev = { learnAllBooks: false, startingSkillPoints: 0 };

    var problems = validateGameData(data);
    if (problems.length > 0) throw new Error('Game data problems:\n- ' + problems.join('\n- '));
    return data;
}

function validateGameData(data: GameData): string[] {
    var problems: string[] = [];
    function need(ok: boolean, message: string) { if (!ok) problems.push(message); }
    function exists<T extends { id: string }>(list: T[], id: string, what: string, where: string) {
        need(findBy(list, id) !== null, where + ': unknown ' + what + " '" + id + "'");
    }

    var combat = data.combat, skills = data.skills, items = data.items;
    combat.characters.forEach(function (c) {
        c.weapons.forEach(function (w) { exists(combat.weapons, w, 'weapon', 'character ' + c.id); });
    });
    skills.books.forEach(function (b) {
        if (b.weapon) exists(combat.weapons, b.weapon, 'weapon', 'book ' + b.id);
        b.forms.forEach(function (f) {
            need(indexOf(['single', 'cone', 'circle', 'circleAtTarget'], f.shape) >= 0, 'form ' + f.id + ": unknown shape '" + f.shape + "'");
            if (f.status && f.status.id) exists(skills.statuses, f.status.id, 'status', 'form ' + f.id);
        });
    });
    skills.statuses.forEach(function (s) {
        need(indexOf(['poison', 'paralysis', 'stun'], s.resist) >= 0, 'status ' + s.id + ": unknown resistance '" + s.resist + "'");
    });
    exists(skills.books, skills.drangueLeap.bookId, 'book', 'drangueLeap');

    items.items.forEach(function (it) {
        if (it.slot) need(indexOf(GEAR_SLOTS as string[], it.slot) >= 0, 'item ' + it.id + ": unknown slot '" + it.slot + "'");
        if (it.slot === 'weapon') exists(combat.weapons, it.weaponType, 'weapon type', 'item ' + it.id);
        it.characters.forEach(function (c) { exists(combat.characters, c, 'character', 'item ' + it.id); });
    });
    items.bonusLines.forEach(function (l) { need(indexOf(ALL_STATS, l.stat) >= 0, "bonus line: unknown stat '" + l.stat + "'"); });
    exists(items.rarities, 'common', 'rarity', 'items.json');
    items.inventory.quickSlots.forEach(function (q) { exists(items.items, q, 'item', 'quickSlots'); });
    items.starterKits.forEach(function (k) {
        exists(combat.characters, k.character, 'character', 'starter kit');
        k.equip.forEach(function (e) { exists(items.items, e, 'item', 'starter kit ' + k.character); });
        k.bag.forEach(function (e) { exists(items.items, e.item, 'item', 'starter kit ' + k.character); });
    });
    combat.characters.forEach(function (c) {
        need(items.starterKits.some(function (k) { return k.character === c.id; }), 'character ' + c.id + ' has no starter kit');
    });
    items.shops.forEach(function (s) { s.items.forEach(function (i) { exists(items.items, i, 'item', 'shop ' + s.id); }); });
    items.lootTables.forEach(function (t) {
        exists(combat.monsters, t.monster, 'monster', 'loot table ' + t.id);
        t.drops.forEach(function (d) { exists(items.items, d.item, 'item', 'loot table ' + t.id); });
    });

    data.zones.zones.forEach(function (z) {
        z.packs.forEach(function (p) { exists(combat.monsters, p.monster, 'monster', 'zone ' + z.id + ' pack ' + p.id); });
        z.shopkeepers.forEach(function (s) { exists(items.shops, s.shop, 'shop', 'zone ' + z.id + ' shopkeeper ' + s.id); });
        need(!z.safe || z.packs.length === 0, 'zone ' + z.id + ' is safe but has monster packs');
        var grid = data.navGrids[z.id];
        if (!grid) return;
        need(grid.zone === z.id, 'zone ' + z.id + ': its navGrid is for ' + grid.zone);
        need(grid.nearestWalkable(vec3From(z.respawn.position), 1) !== null, 'zone ' + z.id + ': the respawn point is not on walkable ground');
        z.packs.forEach(function (p) {
            need(grid.nearestWalkable(vec3From(p.position), p.radius) !== null, 'zone ' + z.id + ' pack ' + p.id + ': no walkable ground in its circle');
        });
        z.shopkeepers.forEach(function (s) {
            need(grid.nearestWalkable(vec3From(s.position), data.zones.playerRules.shopInteractRange) !== null, 'zone ' + z.id + ' shopkeeper ' + s.id + ': no walkable ground in reach');
        });
    });
    return problems;
}
