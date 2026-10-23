// Nakama loads build/index.js and calls the global InitModule once at startup.
function InitModule(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, initializer: nkruntime.Initializer) {
    // Fail at startup, not at the first login, if the shared game data is missing or inconsistent.
    var data = loadGameData(nk);

    initializer.registerRpc('character_list', rpcCharacterList);
    initializer.registerRpc('character_create', rpcCharacterCreate);
    initializer.registerRpc('character_load', rpcCharacterLoad);
    initializer.registerRpc('character_delete', rpcCharacterDelete);
    initializer.registerRpc('zone_list', rpcZoneList);
    initializer.registerRpc('zone_join', rpcZoneJoin);

    initializer.registerMatch<ZoneMatchState>('zone', {
        matchInit: matchInit,
        matchJoinAttempt: matchJoinAttempt,
        matchJoin: matchJoin,
        matchLeave: matchLeave,
        matchLoop: matchLoop,
        matchTerminate: matchTerminate,
        matchSignal: matchSignal,
    });

    logger.info('wuxia module loaded: %d zones, %d monsters, %d items', data.zones.zones.length, data.combat.monsters.length, data.items.items.length);
}
