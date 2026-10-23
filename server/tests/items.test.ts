// Items, gold and characters, mirroring the client's EquipmentTests, InventoryShopTests and LootTests.

function newBelongings(data: GameData, characterId?: string): Belongings {
    var b = new Belongings(data, new ItemFactory(data, counterIds('item-')), characterId || 'kreshnik', emptyBelongings(data));
    b.giveStarterKit();
    b.drainLedger();
    return b;
}

function bagDefs(b: Belongings): string[] {
    return b.s.bag.filter(function (i) { return !!i; }).map(function (i) { return (i as ItemInstance).def + 'x' + (i as ItemInstance).count; });
}

var FULL_VITALS: Vitals = {
    isDead: function () { return false; }, hp: function () { return 100; }, maxHp: function () { return 100; },
    spirit: function () { return 100; }, maxSpirit: function () { return 100; },
    restoreHp: function () { }, restoreSpirit: function () { },
};

function vitalsOf(hp: number, maxHp: number, spirit: number, maxSpirit: number, dead?: boolean) {
    var v = { hp: hp, spirit: spirit };
    var vitals: Vitals = {
        isDead: function () { return !!dead; }, hp: function () { return v.hp; }, maxHp: function () { return maxHp; },
        spirit: function () { return v.spirit; }, maxSpirit: function () { return maxSpirit; },
        restoreHp: function (a) { v.hp += a; }, restoreSpirit: function (a) { v.spirit += a; },
    };
    return { v: v, vitals: vitals };
}

test('items: the starter kit matches items.json and is all in the ledger', function () {
    var data = testData();
    var created = createCharacter(data, 'kreshnik', 'Gjergj', counterIds(), 0);
    var b = created.save.belongings;
    eq(b.gold, 100);
    eq((b.equipment['weapon'] as ItemInstance).def, 'worn_sword');
    eq(b.bag.filter(function (i) { return !!i; }).map(function (i) { return (i as ItemInstance).def + 'x' + (i as ItemInstance).count; }),
       ['worn_bowx1', 'hp_potionx5', 'spirit_potionx3']);
    eq(b.quickSlots, ['hp_potion', 'spirit_potion']);
    eq(created.ledger.map(function (e) { return e.kind + ':' + (e.defId || e.gold); }),
       ['goldIn:100', 'itemIn:worn_sword', 'itemIn:worn_bow', 'itemIn:hp_potion', 'itemIn:spirit_potion']);
    // Every item has its own id.
    var ids = created.ledger.filter(function (e) { return !!e.itemId; }).map(function (e) { return e.itemId; });
    eq(ids.length, 4);
});

test('items: starting stats come from combat.json (Worn Sword = the old sword numbers)', function () {
    var data = testData();
    var s = newBelongings(data).computeStats();
    eq(s.weaponType, 'sword');
    eq([s.attack.minDamage, s.attack.maxDamage], [14, 22]);
    eq(s.maxHp, data.combat.player.maxHp);
    eq(s.defense, data.combat.player.defense);
});

test('items: equipping the Iron Sword raises the basic attack and accuracy', function () {
    var data = testData();
    var b = newBelongings(data);
    var sword = b.factory.create('iron_sword');
    ok(b.tryAdd(sword));
    eq(b.equip(sword.id), 'none');
    var s = b.computeStats();
    eq([s.attack.minDamage, s.attack.maxDamage], [21, 31]);
    near(s.attack.accuracy, 0.94, 1e-5);
    // The Worn Sword went to the Iron Sword's bag slot.
    ok(bagDefs(b).indexOf('worn_swordx1') >= 0);
});

test('items: bonus lines add up and dodge is capped', function () {
    var data = testData();
    var b = newBelongings(data);
    var vest = b.factory.create('padded_vest', 1, 'rare', [{ stat: 'hp', value: 20 }, { stat: 'dodge', value: 0.6 }]);
    b.tryAdd(vest);
    eq(b.equip(vest.id), 'none');
    var s = b.computeStats();
    eq(s.maxHp, data.combat.player.maxHp + 25 + 20);
    eq(s.dodge, f32(data.items.statCaps.dodge));
});

test('items: weapons are locked to characters and levels are enforced', function () {
    var data = testData();
    var b = newBelongings(data, 'zanas_chosen');
    var sword = b.factory.create('iron_sword');
    b.tryAdd(sword);
    eq(b.equip(sword.id), 'wrongCharacter');
    var amulet = b.factory.create('wolf_fang_amulet');
    b.tryAdd(amulet);
    eq(b.equip(amulet.id), 'levelTooLow');
    eq(b.unequip('weapon'), 'weaponRequired');
});

test('items: swap wields the other weapon type, last used first', function () {
    var data = testData();
    var b = newBelongings(data);
    eq(b.swapWeapon(), 'none');
    eq(b.computeStats().weaponType, 'bow');
    var worn = b.findSwapWeapon() as ItemInstance;
    eq(worn.def, 'worn_sword');
    var iron = b.factory.create('iron_sword');
    b.tryAdd(iron);
    // The Worn Sword was swapped out last, so it comes back even though the Iron Sword hits harder.
    eq((b.findSwapWeapon() as ItemInstance).id, worn.id);
});

test('items: bag capacity, stacking and all-or-nothing adds', function () {
    var data = testData();
    var b = newBelongings(data);
    var capacity = data.items.inventory.capacity;
    var potion = getItem(data, 'hp_potion');
    eq(b.freeSlots(), capacity - 3);
    // Potions top up the existing stack first.
    ok(b.tryAdd(b.factory.create('hp_potion', 10)));
    eq(b.countOf('hp_potion'), 15);
    eq(b.freeSlots(), capacity - 3);
    while (b.freeSlots() > 0) ok(b.tryAdd(b.factory.create('cloth_hood')));
    ok(!b.tryAdd(b.factory.create('cloth_hood')), 'full bag refuses gear');
    ok(b.tryAdd(b.factory.create('hp_potion', potion.stackSize - 15)), 'still room on the stack');
    ok(!b.tryAdd(b.factory.create('hp_potion', 1)));
});

test('items: shop buys are atomic and priced from items.json', function () {
    var data = testData();
    var b = newBelongings(data);
    eq(b.buy('han_keeper', 'hp_potion', 5), 'none');
    eq(b.gold, 100 - 5 * getItem(data, 'hp_potion').buyPrice);
    eq(b.countOf('hp_potion'), 10);
    var ledger = b.drainLedger();
    eq(ledger.map(function (e) { return e.kind; }), ['goldOut', 'itemIn']);
    eq(ledger[0].reason, 'shop buy: Han Keeper');
    eq(b.buy('han_keeper', 'padded_vest', 1), 'notEnoughGold');
    eq(b.buy('han_keeper', 'iron_sword', 1), 'notSold');
    eq(b.buy('han_keeper', 'hp_potion', 0), 'badCount');
    b.addGold(1000, 'test');
    b.drainLedger();
    var before = b.gold;
    while (b.freeSlots() > 0) b.tryAdd(b.factory.create('cloth_hood'));
    eq(b.buy('han_keeper', 'leather_boots', 1), 'bagFull');
    eq(b.gold, before, 'no gold taken');
    eq(b.drainLedger(), []);
});

test('items: selling pays sell price × rarity, never worn items', function () {
    var data = testData();
    var b = newBelongings(data);
    var hood = b.factory.create('cloth_hood', 1, 'fine', [{ stat: 'hp', value: 12 }]);
    b.tryAdd(hood);
    eq(b.sell(hood.id, 1), 'none');
    eq(b.gold, 100 + Math.floor(12 * 1.5));
    eq(b.sell((b.weapon as ItemInstance).id, 1), 'equipped');
    var potions = b.findFirst('hp_potion') as ItemInstance;
    eq(b.sell(potions.id, 2), 'none');
    eq(b.countOf('hp_potion'), 3);
    eq(b.sell(potions.id, 9), 'notFound');
});

test('items: potions restore up to max, share a cooldown and are refused at full', function () {
    var data = testData();
    var b = newBelongings(data);
    var v = vitalsOf(50, 220, 100, 100);
    eq(b.useQuickSlot(0, 0, v.vitals), 'none');
    eq(v.v.hp, 130);
    eq(b.countOf('hp_potion'), 4);
    eq(b.useQuickSlot(0, 1, v.vitals), 'cooldown');
    eq(b.useQuickSlot(0, 8, v.vitals), 'none');
    eq(v.v.hp, 210);
    eq(b.useQuickSlot(1, 0, v.vitals), 'alreadyFull', 'Spirit is full');
    eq(b.useQuickSlot(0, 100, FULL_VITALS), 'alreadyFull');
    eq(b.useQuickSlot(0, 100, vitalsOf(0, 220, 0, 100, true).vitals), 'dead');
    eq(b.drainLedger().map(function (e) { return e.kind + ':' + e.reason; }), ['itemOut:potion', 'itemOut:potion']);
});

test('items: quick slots swap when a potion moves to the other slot', function () {
    var b = newBelongings(testData());
    ok(b.assignQuickSlot(0, 'spirit_potion'));
    eq(b.s.quickSlots, ['spirit_potion', 'hp_potion']);
    ok(!b.assignQuickSlot(0, 'worn_bow'));
    ok(!b.assignQuickSlot(2, 'hp_potion'));
});

test('items: bag moves move, merge and swap', function () {
    var data = testData();
    var b = newBelongings(data);
    // bag: 0 worn_bow, 1 hp x5, 2 spirit x3
    ok(b.move(0, 10));
    eq((b.s.bag[10] as ItemInstance).def, 'worn_bow');
    ok(b.move(1, 2));
    eq((b.s.bag[2] as ItemInstance).def, 'hp_potion', 'different items swap');
    var extra = b.factory.create('hp_potion', 3);
    b.s.bag[5] = extra;
    ok(b.move(5, 2));
    eq((b.s.bag[2] as ItemInstance).count, 8, 'same potion merges');
    eq(b.s.bag[5], null);
    ok(!b.move(7, 8), 'empty source');
    ok(!b.move(0, 99));
});

test('loot: wolf table odds over many rolls (seeded)', function () {
    var data = testData();
    var factory = new ItemFactory(data, counterIds());
    var table = findLootTable(data, 'wolf') as LootTableData;
    var rng = seededRng(99);
    var n = 20000, goldDrops = 0, goldTotal = 0, potions = 0, swords = 0;
    var rarities: { [id: string]: number } = {}, gear = 0;
    for (var i = 0; i < n; i++) {
        rollLoot(data, table, factory, rng).forEach(function (d) {
            if (!d.item) { goldDrops++; goldTotal += d.gold; between(d.gold, table.goldMin, table.goldMax); return; }
            var def = getItem(data, d.item.def);
            if (d.item.def === 'hp_potion') potions++;
            if (d.item.def === 'iron_sword') swords++;
            if (isGear(def)) {
                gear++;
                rarities[d.item.rarity] = (rarities[d.item.rarity] || 0) + 1;
                eq(d.item.lines.length, getRarity(data, d.item.rarity).bonusLines);
            } else eq(d.item.rarity, 'common');
        });
    }
    near(goldDrops / n, table.goldChance, 0.01, 'gold chance');
    near(goldTotal / goldDrops, (table.goldMin + table.goldMax) / 2, 0.15, 'average gold');
    near(potions / n, 0.25, 0.01, 'healing draught chance');
    near(swords / n, 0.02, 0.004, 'iron sword chance');
    near((rarities['common'] || 0) / gear, 0.70, 0.03, 'common share');
    near((rarities['fine'] || 0) / gear, 0.24, 0.03, 'fine share');
    near((rarities['rare'] || 0) / gear, 0.06, 0.02, 'rare share');
});

test('loot: bonus lines only come from the slot\'s pool, each stat once', function () {
    var data = testData();
    var factory = new ItemFactory(data, counterIds());
    var rng = seededRng(5);
    for (var i = 0; i < 500; i++) {
        var lines = factory.rollBonusLines(getItem(data, 'padded_vest'), 2, rng);
        eq(lines.length, 2);
        ok(lines[0].stat !== lines[1].stat);
        lines.forEach(function (l) {
            var entry = data.items.bonusLines.filter(function (e) { return e.stat === l.stat; })[0];
            ok(indexOf(entry.slots, 'armor') >= 0, l.stat + ' rolls on armor');
            between(l.value, entry.min, entry.max, l.stat);
            eq(l.value, isWholeStat(l.stat) ? Math.round(l.value) : roundTo(l.value, 3), 'stored rounded');
        });
    }
});

test('characters: names are checked; saves survive a JSON round trip', function () {
    eq(cleanCharacterName('  Gjergj   Elez  '), 'Gjergj Elez');
    eq(cleanCharacterName('x'), null);
    eq(cleanCharacterName('<script>'), null);
    eq(cleanCharacterName(42), null);
    var data = testData();
    var created = createCharacter(data, 'zanas_chosen', 'Tanusha', counterIds(), 123);
    var loaded = upgradeCharacterSave(data, JSON.parse(JSON.stringify(created.save)));
    eq(loaded, created.save);
    eq(summarizeCharacter(data, loaded, 0).weaponType, 'sicas');
});

test('characters: an item removed from items.json is dropped from old saves', function () {
    var data = testData();
    var save: any = createCharacter(data, 'kreshnik', 'Gjergj', counterIds(), 0).save;
    save.belongings.bag[4] = { id: 'x', def: 'deleted_item', rarity: 'common', count: 1, lines: [] };
    delete save.session;
    var loaded = upgradeCharacterSave(data, save);
    eq(loaded.belongings.bag[4], null);
    eq(loaded.session, null);
});

test('characters: the session lock holds for other matches until it runs out', function () {
    var save = createCharacter(testData(), 'kreshnik', 'Gjergj', counterIds(), 0).save;
    ok(!isSessionLocked(save, 'a', 1000));
    save.session = { matchId: 'a', until: 5000 };
    ok(!isSessionLocked(save, 'a', 1000), 'its own match');
    ok(isSessionLocked(save, 'b', 1000));
    ok(!isSessionLocked(save, 'b', 6000), 'expired');
});
