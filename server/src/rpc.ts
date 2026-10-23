// RPCs (client: session.rpc / client.rpc). Players log in with Nakama's own device authentication;
// these manage their characters and find a zone channel to join. Everything that happens inside a
// zone (combat, loot, items, skills) goes through the zone match instead, so it can't race it.
//
// Nakama looks handlers up by their global function names, so each one is a top-level function.

function rpcError(code: nkruntime.Codes, message: string): nkruntime.Error {
    return { message: message, code: code } as nkruntime.Error;
}

function rpcInput(payload: string): any {
    if (!payload) return {};
    try {
        var parsed = JSON.parse(payload);
        return parsed && typeof parsed === 'object' ? parsed : {};
    } catch (e) {
        throw rpcError(nkruntime.Codes.INVALID_ARGUMENT, 'payload must be JSON');
    }
}

function rpcUser(ctx: nkruntime.Context): string {
    if (!ctx.userId) throw rpcError(nkruntime.Codes.UNAUTHENTICATED, 'log in first');
    return ctx.userId;
}

function rpcCharacterId(data: GameData, value: any): string {
    if (typeof value !== 'string' || !findBy(data.combat.characters, value))
        throw rpcError(nkruntime.Codes.INVALID_ARGUMENT, 'characterId must be one of: ' + data.combat.characters.map(function (c) { return c.id; }).join(', '));
    return value;
}

/** {} → {characters: [summary], available: [{id, name, weapons}]} */
function rpcCharacterList(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    var userId = rpcUser(ctx);
    var data = loadGameData(nk);
    var now = Date.now();
    var saves = listCharacterSaves(nk, data, userId);
    return JSON.stringify({
        characters: saves.map(function (s) { return summarizeCharacter(data, s, now); }),
        available: data.combat.characters.map(function (c) { return { id: c.id, name: c.name, weapons: c.weapons }; }),
    });
}

/** {characterId, name} → {character: summary}. One character of each kind per account; names are unique (any case). */
function rpcCharacterCreate(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    var userId = rpcUser(ctx);
    var data = loadGameData(nk);
    var input = rpcInput(payload);
    var characterId = rpcCharacterId(data, input.characterId);
    var name = cleanCharacterName(input.name);
    if (!name) throw rpcError(nkruntime.Codes.INVALID_ARGUMENT, 'name must be 2-20 letters, digits, spaces, apostrophes or hyphens');
    var alreadyHave = function () { return rpcError(nkruntime.Codes.ALREADY_EXISTS, 'you already have ' + getCharacter(data, characterId).name); };
    var nameTaken = function () { return rpcError(nkruntime.Codes.ALREADY_EXISTS, 'the name ' + name + ' is taken'); };
    if (readCharacter(nk, data, userId, characterId)) throw alreadyHave();
    if (isCharacterNameTaken(nk, name as string)) throw nameTaken();
    var created = createCharacter(data, characterId, name, function () { return nk.uuidv4(); }, Date.now());
    try {
        createCharacterSave(nk, userId, created.save, created.ledger);
    } catch (e) {
        // Lost a race: the character or the name was created a moment ago.
        throw readCharacter(nk, data, userId, characterId) ? alreadyHave() : nameTaken();
    }
    created.ledger.forEach(function (entry) { logger.debug('%s %s', userId, describeLedger(data, entry)); });
    logger.info('Character created: %s (%s) for %s', name, characterId, userId);
    return JSON.stringify({ character: summarizeCharacter(data, created.save, Date.now()) });
}

/** {characterId} → {character: the full save (minus the session lock), stats} */
function rpcCharacterLoad(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    var userId = rpcUser(ctx);
    var data = loadGameData(nk);
    var characterId = rpcCharacterId(data, rpcInput(payload).characterId);
    var stored = readCharacter(nk, data, userId, characterId);
    if (!stored) throw rpcError(nkruntime.Codes.NOT_FOUND, 'no such character');
    var save = stored.save;
    var belongings = new Belongings(data, new ItemFactory(data, function () { return ''; }), save.characterId, save.belongings);
    var online = isHeldElsewhere(nk, save, null, Date.now());
    var view: any = deepCopy(save);
    delete view.session;
    return JSON.stringify({ character: view, online: online, stats: belongings.computeStats() });
}

/** {characterId} → {}. Refused while the character is in a zone. */
function rpcCharacterDelete(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    var userId = rpcUser(ctx);
    var data = loadGameData(nk);
    var characterId = rpcCharacterId(data, rpcInput(payload).characterId);
    var stored = readCharacter(nk, data, userId, characterId);
    if (!stored) throw rpcError(nkruntime.Codes.NOT_FOUND, 'no such character');
    if (isHeldElsewhere(nk, stored.save, null, Date.now())) throw rpcError(nkruntime.Codes.FAILED_PRECONDITION, 'leave the zone first');
    try {
        deleteCharacter(nk, userId, stored);
    } catch (e) {
        throw rpcError(nkruntime.Codes.ABORTED, 'the character changed meanwhile; try again');
    }
    logger.info('Character deleted: %s (%s) for %s', stored.save.name, characterId, userId);
    return JSON.stringify({});
}

interface ZoneLabel { zone: string; channel: number; cap: number; players: number; }

/** Nakama indexes match labels in batches, so a channel opened a moment ago isn't listed yet; remember it here. */
var RECENT_CHANNELS_SECONDS = 10;

function recentChannelsKey(zoneId: string): string { return 'zone-channels:' + zoneId; }

function zoneChannels(nk: nkruntime.Nakama, zoneId: string, cap: number): { matchId: string; label: ZoneLabel }[] {
    var matches = nk.matchList(100, true, null, null, null, '+label.zone:' + zoneId) || [];
    var out: { matchId: string; label: ZoneLabel }[] = [];
    var listed: { [matchId: string]: boolean } = {};
    matches.forEach(function (m) {
        try {
            var label = JSON.parse(m.label) as ZoneLabel;
            if (label.zone !== zoneId) return;
            out.push({ matchId: m.matchId, label: label });
            listed[m.matchId] = true;
        } catch (e) { }
    });
    var recent: { matchId: string; channel: number }[] = JSON.parse(nk.localcacheGet(recentChannelsKey(zoneId)) || '[]');
    recent.forEach(function (r) {
        if (!listed[r.matchId]) out.push({ matchId: r.matchId, label: { zone: zoneId, channel: r.channel, cap: cap, players: 0 } });
    });
    out.sort(function (a, b) { return a.label.channel - b.label.channel; });
    return out;
}

function rememberChannel(nk: nkruntime.Nakama, zoneId: string, matchId: string, channel: number): void {
    var key = recentChannelsKey(zoneId);
    var recent: { matchId: string; channel: number }[] = JSON.parse(nk.localcacheGet(key) || '[]');
    recent.push({ matchId: matchId, channel: channel });
    nk.localcachePut(key, JSON.stringify(recent), RECENT_CHANNELS_SECONDS);
}

/** {zone} → {zone, channels: [{matchId, channel, players, cap}]} */
function rpcZoneList(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    rpcUser(ctx);
    var data = loadGameData(nk);
    var zoneId = rpcInput(payload).zone;
    if (typeof zoneId !== 'string' || !findBy(data.zones.zones, zoneId)) throw rpcError(nkruntime.Codes.INVALID_ARGUMENT, 'unknown zone');
    return JSON.stringify({
        zone: zoneId,
        channels: zoneChannels(nk, zoneId, getZone(data, zoneId).channelCap).map(function (c) { return { matchId: c.matchId, channel: c.label.channel, players: c.label.players, cap: c.label.cap }; }),
    });
}

/**
 * {zone, channel?} → {matchId, zone, channel}: the lowest channel with room (or the one asked
 * for), opening a new channel when all are full. Then join it over the socket with metadata
 * {characterId}.
 */
function rpcZoneJoin(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    rpcUser(ctx);
    var data = loadGameData(nk);
    var input = rpcInput(payload);
    var zoneId = input.zone;
    if (typeof zoneId !== 'string' || !findBy(data.zones.zones, zoneId)) throw rpcError(nkruntime.Codes.INVALID_ARGUMENT, 'unknown zone');
    var zone = getZone(data, zoneId);
    var channels = zoneChannels(nk, zoneId, zone.channelCap);
    var used: { [n: number]: boolean } = {};
    channels.forEach(function (c) { used[c.label.channel] = true; });
    for (var i = 0; i < channels.length; i++) {
        var c = channels[i];
        if (input.channel !== undefined && c.label.channel !== input.channel) continue;
        if (c.label.players < zone.channelCap) return JSON.stringify({ matchId: c.matchId, zone: zoneId, channel: c.label.channel });
    }
    if (input.channel !== undefined && used[input.channel]) throw rpcError(nkruntime.Codes.RESOURCE_EXHAUSTED, 'that channel is full');
    var channel = 1;
    while (used[channel]) channel++;
    var matchId = nk.matchCreate('zone', { zone: zoneId, channel: channel });
    rememberChannel(nk, zoneId, matchId, channel);
    logger.info('Opened %s channel %d: %s', zone.name, channel, matchId);
    return JSON.stringify({ matchId: matchId, zone: zoneId, channel: channel });
}
