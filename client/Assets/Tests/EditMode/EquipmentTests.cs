using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wuxia.Data;
using Wuxia.Items;

namespace Wuxia.Tests
{
    /// <summary>Starter kits, equip/unequip rules, weapon swap and the stat calculator.</summary>
    public class EquipmentTests
    {
        static ItemsData data;
        static CombatData combat;

        [OneTimeSetUp]
        public void LoadData()
        {
            data = ItemDataTests.Load();
            combat = CombatDataTests.Load();
        }

        /// <summary>A fresh character with its starter kit, plus a ledger recording every change.</summary>
        public static PlayerItems NewPlayer(string characterId, List<LedgerEntry> ledger = null)
        {
            var d = data ?? ItemDataTests.Load();
            var c = combat ?? CombatDataTests.Load();
            return new PlayerItems(d, c, new ItemFactory(d), characterId, ledger != null ? ledger.Add : (System.Action<LedgerEntry>)null);
        }

        static ItemInstance GiveDrop(PlayerItems items, string defId)
        {
            var item = items.Factory.Create(defId);
            Assert.That(items.PickUp(LootDrop.OfItem(item), "test"), Is.EqualTo(PickupBlock.None));
            return item;
        }

        [Test]
        public void StarterKit_WieldsOneWeapon_CarriesTheOther()
        {
            var k = NewPlayer("kreshnik");
            Assert.That(k.Equipment.Weapon.DefId, Is.EqualTo("worn_sword"));
            Assert.That(k.Bag.CountOf("worn_bow"), Is.EqualTo(1));
            Assert.That(k.Gold, Is.EqualTo(data.GetStarterKit("kreshnik").gold));
            Assert.That(k.QuickSlot(0), Is.EqualTo(data.inventory.quickSlots[0]));

            var z = NewPlayer("zanas_chosen");
            Assert.That(z.Equipment.Weapon.DefId, Is.EqualTo("worn_sicas"));
            Assert.That(z.Bag.CountOf("worn_staff"), Is.EqualTo(1));
        }

        [Test]
        public void StarterStats_MatchCombatJson()
        {
            // The Worn Sword is the M1/M2 sword, so the starter kit keeps the old balance.
            var stats = NewPlayer("kreshnik").ComputeStats();
            var player = combat.player;
            var sword = combat.GetWeapon("sword").attack;
            Assert.That(stats.WeaponType, Is.EqualTo("sword"));
            Assert.That(stats.MaxHp, Is.EqualTo(player.maxHp));
            Assert.That(stats.MaxSpirit, Is.EqualTo(player.maxSpirit));
            Assert.That(stats.Defense, Is.EqualTo(player.defense));
            Assert.That(stats.Dodge, Is.EqualTo(player.dodge).Within(1e-5));
            Assert.That(stats.Attack.minDamage, Is.EqualTo(sword.minDamage));
            Assert.That(stats.Attack.maxDamage, Is.EqualTo(sword.maxDamage));
            Assert.That(stats.Attack.range, Is.EqualTo(sword.range));
            Assert.That(stats.Attack.interval, Is.EqualTo(sword.interval));
        }

        [Test]
        public void EquippingADroppedIronSword_RaisesTheBasicAttackDamage()
        {
            // M3 "done when": equipping a dropped sword changes your stats.
            var items = NewPlayer("kreshnik");
            var before = items.ComputeStats().Attack;
            var iron = GiveDrop(items, "iron_sword");

            Assert.That(items.Equip(iron.Id), Is.EqualTo(EquipBlock.None));
            var after = items.ComputeStats().Attack;
            var def = data.GetItem("iron_sword");
            Assert.That(after.minDamage, Is.EqualTo(def.stats.minDamage));
            Assert.That(after.maxDamage, Is.EqualTo(def.stats.maxDamage));
            Assert.That(after.minDamage, Is.GreaterThan(before.minDamage));
            Assert.That(after.maxDamage, Is.GreaterThan(before.maxDamage));
            Assert.That(after.accuracy, Is.EqualTo(before.accuracy + def.stats.accuracy).Within(1e-5));
            Assert.That(after.range, Is.EqualTo(before.range), "range and speed stay per weapon type");

            // And the rolled damage really uses the new range.
            var rng = new System.Random(3);
            for (var i = 0; i < 300; i++)
            {
                var hit = Combat.CombatMath.RollBasicAttack(after, 0, 0f, rng);
                if (hit.Hit && !hit.Crit) Assert.That(hit.Damage, Is.InRange(after.minDamage, after.maxDamage));
            }
        }

        [Test]
        public void Equip_SwapsTheOldItemIntoTheSameBagSlot()
        {
            var items = NewPlayer("kreshnik");
            var worn = items.Equipment.Weapon;
            var iron = GiveDrop(items, "iron_sword");
            var index = items.Bag.IndexOf(iron.Id);
            var free = items.Bag.FreeSlots;

            items.Equip(iron.Id);
            Assert.That(items.Equipment.Weapon, Is.SameAs(iron));
            Assert.That(items.Bag.Get(index), Is.SameAs(worn));
            Assert.That(items.Bag.FreeSlots, Is.EqualTo(free));
        }

        [Test]
        public void Equip_ChecksSlotCharacterAndLevel()
        {
            var zana = NewPlayer("zanas_chosen");
            var sword = GiveDrop(zana, "iron_sword");
            Assert.That(zana.Equip(sword.Id), Is.EqualTo(EquipBlock.WrongCharacter));
            Assert.That(zana.Equip(zana.Bag.FindFirst("hp_potion").Id), Is.EqualTo(EquipBlock.NotGear));
            Assert.That(zana.Equip("no-such-id"), Is.EqualTo(EquipBlock.NotFound));

            var amulet = GiveDrop(zana, "wolf_fang_amulet");
            Assert.That(data.GetItem("wolf_fang_amulet").levelRequired, Is.GreaterThan(zana.PlayerLevel));
            Assert.That(zana.Equip(amulet.Id), Is.EqualTo(EquipBlock.LevelTooLow));
            zana.PlayerLevel = data.GetItem("wolf_fang_amulet").levelRequired;
            Assert.That(zana.Equip(amulet.Id), Is.EqualTo(EquipBlock.None));
            Assert.That(zana.Equipment.Get(EquipSlot.Amulet), Is.SameAs(amulet));
        }

        [Test]
        public void PlayerLevel_ComesFromItemsJson()
        {
            Assert.That(NewPlayer("kreshnik").PlayerLevel, Is.EqualTo(data.offline.playerLevel));
        }

        [Test]
        public void Unequip_GoesToTheBag_UnlessItIsFull_AndTheWeaponStays()
        {
            var items = NewPlayer("kreshnik");
            var vest = GiveDrop(items, "padded_vest");
            items.Equip(vest.Id);
            Assert.That(items.Unequip(EquipSlot.Weapon), Is.EqualTo(EquipBlock.WeaponRequired));
            Assert.That(items.Unequip(EquipSlot.Helm), Is.EqualTo(EquipBlock.NotFound));

            while (!items.Bag.IsFull) GiveDrop(items, "cloth_hood");
            Assert.That(items.Unequip(EquipSlot.Armor), Is.EqualTo(EquipBlock.BagFull));
            Assert.That(items.Equipment.Get(EquipSlot.Armor), Is.SameAs(vest), "a failed unequip changes nothing");

            items.Sell(items.Bag.FindFirst("cloth_hood").Id);
            Assert.That(items.Unequip(EquipSlot.Armor), Is.EqualTo(EquipBlock.None));
            Assert.That(items.Equipment.Get(EquipSlot.Armor), Is.Null);
            Assert.That(items.Bag.Find(vest.Id), Is.SameAs(vest));
        }

        [Test]
        public void Gear_AddsToBaseStats()
        {
            var items = NewPlayer("kreshnik");
            var before = items.ComputeStats();
            foreach (var id in new[] { "cloth_hood", "padded_vest", "leather_boots", "copper_ring", "shepherds_amulet" })
                Assert.That(items.Equip(GiveDrop(items, id).Id), Is.EqualTo(EquipBlock.None), id);

            var gear = new ItemStats();
            foreach (var id in new[] { "cloth_hood", "padded_vest", "leather_boots", "copper_ring", "shepherds_amulet" })
                gear.Add(data.GetItem(id).stats);
            var after = items.ComputeStats();
            Assert.That(after.MaxHp, Is.EqualTo(before.MaxHp + gear.hp));
            Assert.That(after.MaxSpirit, Is.EqualTo(before.MaxSpirit + gear.spirit));
            Assert.That(after.Defense, Is.EqualTo(before.Defense + gear.defense));
            Assert.That(after.Dodge, Is.EqualTo(before.Dodge + gear.dodge).Within(1e-5));
            Assert.That(after.Attack.accuracy, Is.EqualTo(before.Attack.accuracy + gear.accuracy).Within(1e-5));
            Assert.That(after.Resist.poison, Is.EqualTo(before.Resist.poison + gear.poisonResist).Within(1e-5));
            Assert.That(after.Attack.minDamage, Is.EqualTo(before.Attack.minDamage), "armour doesn't touch weapon damage");
        }

        [Test]
        public void BonusLines_AddToTheItemsStats()
        {
            var factory = new ItemFactory(data);
            var rare = data.GetRarity("rare");
            var sword = factory.Create(data.GetItem("iron_sword"), 1, rare,
                new[] { new BonusLine(StatIds.Damage, 3), new BonusLine(StatIds.SkillDamage, 0.05f) });
            var ring = factory.Create(data.GetItem("copper_ring"), 1, data.GetRarity("fine"),
                new[] { new BonusLine(StatIds.Hp, 20) });

            var stats = StatCalculator.Compute(combat, data.statCaps, "kreshnik", sword, new[] { sword, ring });
            var def = data.GetItem("iron_sword");
            Assert.That(stats.Attack.minDamage, Is.EqualTo(def.stats.minDamage + 3), "a damage line raises both ends");
            Assert.That(stats.Attack.maxDamage, Is.EqualTo(def.stats.maxDamage + 3));
            Assert.That(stats.SkillDamage, Is.EqualTo(0.05f).Within(1e-5));
            Assert.That(stats.MaxHp, Is.EqualTo(combat.player.maxHp + 20));
            Assert.That(sword.SellPriceEach, Is.EqualTo((int)(def.sellPrice * rare.sellMultiplier)));
        }

        [Test]
        public void Dodge_AndResistances_AreCapped()
        {
            var factory = new ItemFactory(data);
            var boots = factory.Create(data.GetItem("leather_boots"), 1, data.GetRarity("rare"),
                new[] { new BonusLine(StatIds.Dodge, 5f), new BonusLine(StatIds.PoisonResist, 5f) });
            var stats = StatCalculator.Compute(combat, data.statCaps, "kreshnik", null, new[] { boots });
            Assert.That(stats.Dodge, Is.EqualTo(data.statCaps.dodge));
            Assert.That(stats.Resist.poison, Is.EqualTo(data.statCaps.resist));
            Assert.That(stats.WeaponType, Is.EqualTo("sword"), "with no weapon the first weapon type's base attack stands");
            Assert.That(stats.Attack.minDamage, Is.EqualTo(combat.GetWeapon("sword").attack.minDamage));
        }

        [Test]
        public void Swap_WieldsTheOtherWeaponType_AndBack()
        {
            var items = NewPlayer("kreshnik");
            var sword = items.Equipment.Weapon;
            Assert.That(items.SwapWeapon(), Is.EqualTo(EquipBlock.None));
            Assert.That(items.Equipment.Weapon.DefId, Is.EqualTo("worn_bow"));
            var stats = items.ComputeStats();
            Assert.That(stats.WeaponType, Is.EqualTo("bow"));
            Assert.That(stats.Attack.range, Is.EqualTo(combat.GetWeapon("bow").attack.range));
            Assert.That(stats.Attack.minDamage, Is.EqualTo(data.GetItem("worn_bow").stats.minDamage));

            Assert.That(items.SwapWeapon(), Is.EqualTo(EquipBlock.None));
            Assert.That(items.Equipment.Weapon, Is.SameAs(sword));
        }

        [Test]
        public void Swap_PrefersTheLastUsedWeapon_ThenTheBest()
        {
            var items = NewPlayer("kreshnik");
            var worn = items.Equipment.Weapon;
            var iron = GiveDrop(items, "iron_sword");

            items.SwapWeapon(); // to the bow; the Worn Sword was the last sword used
            Assert.That(items.FindSwapWeapon(), Is.SameAs(worn));

            Assert.That(items.Sell(worn.Id), Is.EqualTo(ShopBlock.None));
            Assert.That(items.FindSwapWeapon(), Is.SameAs(iron), "with the last one gone, the best sword");
            items.SwapWeapon();
            Assert.That(items.Equipment.Weapon, Is.SameAs(iron));
        }

        [Test]
        public void Swap_FailsWithoutAnotherWeaponType()
        {
            var items = NewPlayer("kreshnik");
            items.Sell(items.Bag.FindFirst("worn_bow").Id);
            GiveDrop(items, "iron_sword"); // same type as the wielded one: not a swap target
            Assert.That(items.SwapWeapon(), Is.EqualTo(EquipBlock.NoOtherWeapon));
            Assert.That(items.Equipment.Weapon.DefId, Is.EqualTo("worn_sword"));
        }

        [Test]
        public void ResetToStarterKit_SwitchesCharacter_AndClearsEverything()
        {
            var ledger = new List<LedgerEntry>();
            var items = NewPlayer("kreshnik", ledger);
            items.Equip(GiveDrop(items, "iron_sword").Id);
            items.AddGold(500, "test");

            var changed = new List<EquipSlot>();
            items.EquipmentChanged += changed.Add;
            items.ResetToStarterKit("zanas_chosen");
            Assert.That(items.CharacterId, Is.EqualTo("zanas_chosen"));
            Assert.That(items.Equipment.Weapon.DefId, Is.EqualTo("worn_sicas"));
            Assert.That(items.Bag.CountOf("iron_sword") + items.Bag.CountOf("worn_sword"), Is.Zero);
            Assert.That(items.Gold, Is.EqualTo(data.GetStarterKit("zanas_chosen").gold));
            Assert.That(items.ComputeStats().WeaponType, Is.EqualTo("sicas"));
            Assert.That(changed, Does.Contain(EquipSlot.Weapon));
            Assert.That(ledger.Any(e => e.Kind == LedgerKind.ItemOut && e.DefId == "iron_sword" && e.Reason == LedgerReasons.Reset));
        }

        [Test]
        public void EveryItemInstance_HasAUniqueId()
        {
            var factory = new ItemFactory(data);
            var ids = new HashSet<string>();
            for (var i = 0; i < 1000; i++) Assert.That(ids.Add(factory.Create("cloth_hood").Id));
        }
    }
}
