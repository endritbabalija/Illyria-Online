// The saved character: one storage object per (account, character id) in the "characters"
// collection, written only by the server. Pure functions here; storage.ts does the reads and writes.

var CHARACTER_SAVE_VERSION = 1;
var CHARACTER_NAME_MIN = 2;
var CHARACTER_NAME_MAX = 20;

interface CharacterSave {
    version: number;
    /** kreshnik or zanas_chosen (combat.json characters). One save per character per account. */
    characterId: string;
    name: string;
    createdAt: number;
    belongings: BelongingsSave;
    skills: SkillProgress;
    /** Lifetime monster kills. */
    kills: number;
    /** Where the character was when last saved; a rejoin to the same zone puts them back there. */
    location: { zone: string; x: number; y: number; z: number; yaw: number } | null;
    /** HP and Spirit at the last save (full after a revive); clamped to the current maxima on load. */
    hp: number | null;
    spirit: number | null;
    /**
     * The match that owns this character right now and until when (epoch ms). A second match
     * refuses the character until the owner lets go or the lock runs out, so a character can't be
     * played (and its items duplicated) in two places at once.
     */
    session: { matchId: string; until: number } | null;
}

/** What character_list shows. */
interface CharacterSummary {
    characterId: string;
    name: string;
    weaponType: string;
    gold: number;
    kills: number;
    zone: string | null;
    online: boolean;
}

/** Trims and checks a display name: 2–20 letters, digits, spaces, apostrophes or hyphens. */
function cleanCharacterName(name: any): string | null {
    if (typeof name !== 'string') return null;
    var trimmed = name.replace(/\s+/g, ' ').replace(/^ | $/g, '');
    if (trimmed.length < CHARACTER_NAME_MIN || trimmed.length > CHARACTER_NAME_MAX) return null;
    return /^[A-Za-z0-9 '\-]+$/.test(trimmed) ? trimmed : null;
}

/** A new character with its starter kit. The ledger holds the kit's gold and item entries. */
function createCharacter(data: GameData, characterId: string, name: string, nextId: () => string, now: number): { save: CharacterSave; ledger: LedgerEntry[] } {
    getCharacter(data, characterId);
    var belongings = new Belongings(data, new ItemFactory(data, nextId), characterId, emptyBelongings(data));
    belongings.giveStarterKit();
    var stats = belongings.computeStats();
    var save: CharacterSave = {
        version: CHARACTER_SAVE_VERSION,
        characterId: characterId,
        name: name,
        createdAt: now,
        belongings: belongings.s,
        skills: startingSkills(data, characterId),
        kills: 0,
        location: null,
        hp: stats.maxHp,
        spirit: stats.maxSpirit,
        session: null,
    };
    return { save: save, ledger: belongings.drainLedger() };
}

/**
 * Checks a loaded save still makes sense against the current game data and fills fields added
 * since it was written. Throws on a save that can't be played (unknown character).
 */
function upgradeCharacterSave(data: GameData, raw: any): CharacterSave {
    var save = raw as CharacterSave;
    getCharacter(data, save.characterId);
    if (!save.skills) save.skills = { books: {}, points: 0, meterKills: 0 };
    if (!save.skills.books) save.skills.books = {};
    if (save.kills === undefined) save.kills = 0;
    if (save.location === undefined) save.location = null;
    if (save.hp === undefined) save.hp = null;
    if (save.spirit === undefined) save.spirit = null;
    if (save.session === undefined) save.session = null;
    // Items whose definition was removed from items.json would break every rule; drop them loudly.
    var b = save.belongings;
    for (var i = 0; i < b.bag.length; i++) {
        var it = b.bag[i];
        if (it && !findBy(data.items.items, it.def)) b.bag[i] = null;
    }
    for (var slot in b.equipment) {
        var worn = b.equipment[slot];
        if (worn && !findBy(data.items.items, worn.def)) b.equipment[slot] = null;
    }
    for (var book in save.skills.books) if (!findBy(data.skills.books, book)) delete save.skills.books[book];
    save.version = CHARACTER_SAVE_VERSION;
    return save;
}

function isSessionLocked(save: CharacterSave, matchId: string | null, nowMs: number): boolean {
    return !!save.session && save.session.matchId !== matchId && save.session.until > nowMs;
}

function summarizeCharacter(data: GameData, save: CharacterSave, nowMs: number): CharacterSummary {
    var belongings = new Belongings(data, new ItemFactory(data, function () { return ''; }), save.characterId, save.belongings);
    return {
        characterId: save.characterId,
        name: save.name,
        weaponType: belongings.computeStats().weaponType,
        gold: save.belongings.gold,
        kills: save.kills,
        zone: save.location ? save.location.zone : null,
        online: isSessionLocked(save, null, nowMs),
    };
}
