// The "zone" match handler: one zone channel (town or hunting zone) run by the server at
// zones.json server.tickRate. It adapts Nakama's match calls to ZoneSim, writes the saves the
// sim asks for, and sends what it queues. Created by the zone_join RPC with params {zone, channel}.
//
// Nakama looks handlers up by their global function names, so each one is a top-level function.
// It also turns the match state into plain data between calls, so class instances would lose
// their methods (and globals are frozen after InitModule). The state therefore holds one closure
// that returns the live channel: a function survives the round trip, and the objects it closes
// over (in the match's own VM) are never converted.

/** Everything a running channel keeps between ticks. */
interface ZoneChannel {
    sim: ZoneSim;
    zoneId: string;
    channel: number;
    cap: number;
    /** Storage version of each joined player's character, for optimistic locking. */
    versions: { [userId: string]: string };
    presences: { [userId: string]: nkruntime.Presence };
    /** Characters read in matchJoinAttempt, waiting for matchJoin. */
    pending: { [userId: string]: StoredCharacter };
    /** Players removed from the sim without a save (a write conflict); their leave is ignored. */
    dropped: { [userId: string]: boolean };
    labelPlayers: number;
    emptySince: number | null;
}

interface ZoneMatchState { channel: () => ZoneChannel; }

function zoneChannel(state: ZoneMatchState, nk: nkruntime.Nakama, logger: nkruntime.Logger): ZoneChannel {
    var channel = state.channel();
    // The sim's callbacks use the nk/logger of the current call.
    channel.sim.factory.nextId = function () { return nk.uuidv4(); };
    channel.sim.log = function (message: string) { logger.debug('%s', message); };
    return channel;
}

function zoneLabel(channel: ZoneChannel): string {
    var label: ZoneLabel = { zone: channel.zoneId, channel: channel.channel, cap: channel.cap, players: channel.sim.playerCount() };
    return JSON.stringify(label);
}

function matchInit(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, params: { [key: string]: any }): { state: ZoneMatchState; tickRate: number; label: string } {
    var data = loadGameData(nk);
    var zone = getZone(data, String(params.zone));
    var channel: ZoneChannel = {
        sim: new ZoneSim(data, zone.id, { nextItemId: function () { return nk.uuidv4(); } }),
        zoneId: zone.id,
        channel: Number(params.channel) || 1,
        cap: zone.channelCap,
        versions: {},
        presences: {},
        pending: {},
        dropped: {},
        labelPlayers: 0,
        emptySince: Date.now(),
    };
    logger.info('%s channel %d started with %d monsters', zone.name, channel.channel, channel.sim.monsters.length);
    return { state: { channel: function () { return channel; } }, tickRate: data.zones.server.tickRate, label: zoneLabel(channel) };
}

function matchJoinAttempt(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher,
                          tick: number, state: ZoneMatchState, presence: nkruntime.Presence, metadata: { [key: string]: any }) {
    var channel = zoneChannel(state, nk, logger);
    var data = channel.sim.data;
    function reject(why: string) { return { state: state, accept: false, rejectMessage: why }; }
    var characterId = metadata && metadata.characterId;
    if (typeof characterId !== 'string' || !findBy(data.combat.characters, characterId)) return reject('pass {characterId} as join metadata');
    if (channel.sim.players[presence.userId] || channel.pending[presence.userId]) return reject('already in this zone');
    if (channel.sim.playerCount() + keysOf(channel.pending).length >= channel.cap) return reject('this channel is full');
    var stored = readCharacter(nk, data, presence.userId, characterId);
    if (!stored) return reject('no such character; create it first');
    if (isSessionLocked(stored.save, ctx.matchId || null, Date.now())) return reject('that character is still in another zone; try again in a moment');
    channel.pending[presence.userId] = stored;
    return { state: state, accept: true };
}

function matchJoin(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher,
                   tick: number, state: ZoneMatchState, presences: nkruntime.Presence[]) {
    var channel = zoneChannel(state, nk, logger);
    var lockSeconds = channel.sim.data.zones.server.sessionLockSeconds;
    presences.forEach(function (presence) {
        var stored = channel.pending[presence.userId];
        delete channel.pending[presence.userId];
        if (!stored) return;
        // Taking the session lock is a versioned write: if anything else wrote the character since
        // the join attempt read it, this fails and the player is sent away instead of playing a stale copy.
        stored.save.session = { matchId: ctx.matchId as string, until: Date.now() + lockSeconds * 1000 };
        try {
            channel.versions[presence.userId] = writeCharacter(nk, presence.userId, stored.save, stored.version, [], ctx.matchId || null);
        } catch (e) {
            logger.warn('Join of %s refused: character changed since the join attempt (%s)', presence.userId, String(e));
            dispatcher.matchKick([presence]);
            channel.dropped[presence.userId] = true;
            return;
        }
        channel.presences[presence.userId] = presence;
        channel.sim.addPlayer(presence.userId, presence.sessionId, presence.username, stored.save);
    });
    zoneFlush(channel, nk, logger, dispatcher, ctx.matchId as string);
    return { state: state };
}

function matchLeave(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher,
                    tick: number, state: ZoneMatchState, presences: nkruntime.Presence[]) {
    var channel = zoneChannel(state, nk, logger);
    presences.forEach(function (presence) {
        var userId = presence.userId;
        delete channel.pending[userId];
        delete channel.presences[userId];
        if (channel.dropped[userId]) { delete channel.dropped[userId]; return; }
        var request = channel.sim.removePlayer(userId);
        if (request) zoneWrite(channel, nk, logger, request, null);
        delete channel.versions[userId];
    });
    zoneFlush(channel, nk, logger, dispatcher, ctx.matchId as string);
    return { state: state };
}

function matchLoop(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher,
                   tick: number, state: ZoneMatchState, messages: nkruntime.MatchMessage[]) {
    var channel = zoneChannel(state, nk, logger);
    var sim = channel.sim;
    for (var i = 0; i < messages.length; i++) {
        var msg = messages[i];
        var payload: any;
        try { payload = JSON.parse(nk.binaryToString(msg.data)); } catch (e) { continue; }
        try {
            sim.handle(msg.sender.userId, msg.opCode, payload);
        } catch (e) {
            // A bad message must never take the channel down for everyone.
            logger.error('Op %d from %s failed: %s', msg.opCode, msg.sender.userId, String(e));
        }
    }
    sim.step(1 / sim.data.zones.server.tickRate);
    zoneFlush(channel, nk, logger, dispatcher, ctx.matchId as string);

    if (sim.playerCount() > 0 || keysOf(channel.pending).length > 0) channel.emptySince = null;
    else if (channel.emptySince === null) channel.emptySince = Date.now();
    else if (Date.now() - channel.emptySince > sim.data.zones.server.emptyMatchSeconds * 1000) {
        logger.info('%s channel %d closed (empty)', sim.zone.name, channel.channel);
        return null;
    }
    return { state: state };
}

function matchTerminate(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher,
                        tick: number, state: ZoneMatchState, graceSeconds: number) {
    var channel = zoneChannel(state, nk, logger);
    keysOf(channel.sim.players).forEach(function (userId) {
        var request = channel.sim.removePlayer(userId);
        if (request) zoneWrite(channel, nk, logger, request, null);
    });
    return { state: state };
}

function matchSignal(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher,
                     tick: number, state: ZoneMatchState, data: string) {
    return { state: state, data: zoneLabel(zoneChannel(state, nk, logger)) };
}

/** Writes one save; a conflict means someone else owns the character now, so the player is removed unsaved. */
function zoneWrite(channel: ZoneChannel, nk: nkruntime.Nakama, logger: nkruntime.Logger, request: SaveRequest, dispatcher: nkruntime.MatchDispatcher | null): boolean {
    var version = channel.versions[request.userId];
    if (!version) return false;
    try {
        channel.versions[request.userId] = writeCharacter(nk, request.userId, request.save, version, request.ledger, request.save.session ? request.save.session.matchId : null);
        return true;
    } catch (e) {
        logger.error('Save of %s (%s) failed, removing them unsaved: %s', request.userId, request.characterId, String(e));
        if (dispatcher && channel.presences[request.userId]) {
            dispatcher.matchKick([channel.presences[request.userId]]);
            channel.dropped[request.userId] = true;
        }
        channel.sim.removePlayer(request.userId);
        delete channel.versions[request.userId];
        return false;
    }
}

/** Writes due saves, sends queued messages and keeps the label's player count current. */
function zoneFlush(channel: ZoneChannel, nk: nkruntime.Nakama, logger: nkruntime.Logger, dispatcher: nkruntime.MatchDispatcher, matchId: string): void {
    channel.sim.collectSaves(matchId, Date.now()).forEach(function (request) { zoneWrite(channel, nk, logger, request, dispatcher); });
    channel.sim.takeMessages().forEach(function (m) {
        var to: nkruntime.Presence[] | null = null;
        if (m.to) {
            to = [];
            for (var i = 0; i < m.to.length; i++) if (channel.presences[m.to[i]]) to.push(channel.presences[m.to[i]]);
            if (to.length === 0) return;
        }
        dispatcher.broadcastMessage(m.op, JSON.stringify(m.data), to, null, true);
    });
    var players = channel.sim.playerCount();
    if (players !== channel.labelPlayers) {
        channel.labelPlayers = players;
        dispatcher.matchLabelUpdate(zoneLabel(channel));
    }
}
