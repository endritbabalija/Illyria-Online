// Nakama storage for characters and the gold and item ledger. Characters and their ledger rows are
// written in one storageWrite call, which Nakama runs as one transaction: a gold or item change and
// its ledger row are saved together or not at all.

var CHARACTERS = 'characters';
var LEDGER = 'ledger';

/** Owner can read, only the server writes. */
var OWNER_READ: nkruntime.ReadPermissionValues = 1;
var SERVER_WRITE: nkruntime.WritePermissionValues = 0;

/** Game data, parsed once per runtime VM (Nakama doesn't share globals between VMs). */
var gameDataCache: GameData | null = null;

function loadGameData(nk: nkruntime.Nakama): GameData {
    if (gameDataCache) return gameDataCache;
    gameDataCache = parseGameData(function (path) { return nk.fileRead('gamedata/' + path); });
    return gameDataCache;
}

interface StoredCharacter { save: CharacterSave; version: string; }

/**
 * Is the character live in another match? A lock whose match no longer exists (the server
 * restarted, or the match stopped without saving) holds nothing, even before it runs out.
 */
function isHeldElsewhere(nk: nkruntime.Nakama, save: CharacterSave, matchId: string | null, nowMs: number): boolean {
    if (!isSessionLocked(save, matchId, nowMs)) return false;
    try {
        return nk.matchGet((save.session as { matchId: string }).matchId) !== null;
    } catch (e) {
        return false;
    }
}

function readCharacter(nk: nkruntime.Nakama, data: GameData, userId: string, characterId: string): StoredCharacter | null {
    var objects = nk.storageRead([{ collection: CHARACTERS, key: characterId, userId: userId }]);
    if (!objects || objects.length === 0) return null;
    return { save: upgradeCharacterSave(data, objects[0].value), version: objects[0].version };
}

function listCharacterSaves(nk: nkruntime.Nakama, data: GameData, userId: string): CharacterSave[] {
    var list = nk.storageList(userId, CHARACTERS, 10);
    var saves: CharacterSave[] = [];
    (list.objects || []).forEach(function (o) {
        // A save for a character that no longer exists in combat.json is skipped, not fatal.
        try { saves.push(upgradeCharacterSave(data, o.value)); } catch (e) { }
    });
    return saves;
}

function ledgerWrite(userId: string, characterId: string, matchId: string | null, entry: LedgerEntry, nowMs: number, id: string): nkruntime.StorageWriteRequest {
    return {
        collection: LEDGER,
        key: id,
        userId: userId,
        value: {
            t: nowMs,
            character: characterId,
            match: matchId,
            kind: entry.kind,
            reason: entry.reason,
            gold: entry.gold,
            goldAfter: entry.goldAfter,
            itemId: entry.itemId,
            defId: entry.defId,
            rarity: entry.rarity,
            count: entry.count,
        },
        permissionRead: OWNER_READ,
        permissionWrite: SERVER_WRITE,
    };
}

/**
 * Writes a character and its ledger rows atomically.
 * @param version the version read (optimistic lock), '*' to create only if it doesn't exist yet.
 * @returns the new version. Throws if the version no longer matches (someone else wrote it).
 */
function writeCharacter(nk: nkruntime.Nakama, userId: string, save: CharacterSave, version: string, ledger: LedgerEntry[], matchId: string | null): string {
    var nowMs = Date.now();
    var writes: nkruntime.StorageWriteRequest[] = [{
        collection: CHARACTERS,
        key: save.characterId,
        userId: userId,
        value: save,
        version: version,
        permissionRead: OWNER_READ,
        permissionWrite: SERVER_WRITE,
    }];
    ledger.forEach(function (entry) { writes.push(ledgerWrite(userId, save.characterId, matchId, entry, nowMs, nk.uuidv4())); });
    var acks = nk.storageWrite(writes);
    return acks[0].version;
}

/** Deletes a character, writing ledger rows for everything it owned in the same transaction. */
function deleteCharacter(nk: nkruntime.Nakama, userId: string, stored: StoredCharacter): void {
    var nowMs = Date.now();
    var save = stored.save;
    var writes: nkruntime.StorageWriteRequest[] = [];
    var reason = 'character delete';
    function out(item: ItemInstance) {
        writes.push(ledgerWrite(userId, save.characterId, null, {
            kind: 'itemOut', reason: reason, gold: 0, goldAfter: save.belongings.gold,
            itemId: item.id, defId: item.def, rarity: item.rarity, count: item.count,
        }, nowMs, nk.uuidv4()));
    }
    save.belongings.bag.forEach(function (item) { if (item) out(item); });
    for (var slot in save.belongings.equipment) {
        var worn = save.belongings.equipment[slot];
        if (worn) out(worn);
    }
    if (save.belongings.gold > 0)
        writes.push(ledgerWrite(userId, save.characterId, null, {
            kind: 'goldOut', reason: reason, gold: save.belongings.gold, goldAfter: 0,
            itemId: null, defId: null, rarity: null, count: 0,
        }, nowMs, nk.uuidv4()));
    nk.multiUpdate(null, writes, [{ collection: CHARACTERS, key: save.characterId, userId: userId, version: stored.version }], null);
}
