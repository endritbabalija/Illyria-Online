using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wuxia.Data;
using Wuxia.Items;

namespace Wuxia.Tests
{
    /// <summary>Sanity checks on the shared items.json, cross-checked against combat.json.</summary>
    public class ItemDataTests
    {
        const string ItemsJsonPath = "Packages/com.wuxia.gamedata/items.json";

        public static ItemsData Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ItemsJsonPath);
            Assert.That(asset, Is.Not.Null, $"{ItemsJsonPath} not found");
            return ItemsData.FromJson(asset.text);
        }

        static ItemsData data;
        static CombatData combat;

        [OneTimeSetUp]
        public void LoadData()
        {
            data = Load();
            combat = CombatDataTests.Load();
        }

        static void AssertItemExists(string id, string where) =>
            Assert.That(data.FindItem(id), Is.Not.Null, $"{where} uses unknown item '{id}'");

        [Test]
        public void Ids_AreUnique()
        {
            var ids = new HashSet<string>();
            foreach (var i in data.items) Assert.That(ids.Add("item:" + i.id), $"duplicate item id '{i.id}'");
            foreach (var r in data.rarities) Assert.That(ids.Add("rarity:" + r.id), $"duplicate rarity id '{r.id}'");
            foreach (var s in data.shops) Assert.That(ids.Add("shop:" + s.id), $"duplicate shop id '{s.id}'");
            foreach (var t in data.lootTables) Assert.That(ids.Add("loot:" + t.id), $"duplicate loot table id '{t.id}'");
            foreach (var k in data.starterKits) Assert.That(ids.Add("kit:" + k.character), $"two starter kits for '{k.character}'");
        }

        [Test]
        public void EveryReferencedItem_Exists()
        {
            foreach (var t in data.lootTables)
            foreach (var d in t.drops) AssertItemExists(d.item, $"loot table {t.id}");
            foreach (var s in data.shops)
            foreach (var id in s.items) AssertItemExists(id, $"shop {s.id}");
            foreach (var id in data.inventory.quickSlots) AssertItemExists(id, "quickSlots");
            foreach (var k in data.starterKits)
            {
                foreach (var id in k.equip) AssertItemExists(id, $"starter kit {k.character}");
                foreach (var e in k.bag) AssertItemExists(e.item, $"starter kit {k.character}");
            }
        }

        [Test]
        public void Items_AreValid()
        {
            foreach (var i in data.items)
            {
                Assert.That(i.name, Is.Not.Empty, i.id);
                Assert.That(i.modelKey, Is.Not.Empty, i.id);
                Assert.DoesNotThrow(() => ItemDef.ParseSlot(i.slot), i.id);
                Assert.That(i.buyPrice, Is.GreaterThanOrEqualTo(0), $"{i.id} buy price");
                Assert.That(i.sellPrice, Is.GreaterThanOrEqualTo(0), $"{i.id} sell price");
                if (i.buyPrice > 0) Assert.That(i.sellPrice, Is.LessThan(i.buyPrice), $"{i.id} would sell for more than it costs");
                Assert.That(i.levelRequired, Is.GreaterThanOrEqualTo(1), i.id);
                foreach (var c in i.characters) Assert.DoesNotThrow(() => combat.GetCharacter(c), $"{i.id} names unknown character '{c}'");

                if (i.IsGear)
                {
                    Assert.That(i.stackSize, Is.EqualTo(1), $"{i.id}: gear doesn't stack");
                    Assert.That(i.use, Is.Null, $"{i.id}: gear can't be drunk");
                }
                else
                {
                    Assert.That(i.IsUsable, $"{i.id}: an item that can't be worn should be a potion");
                    Assert.That(i.stackSize, Is.GreaterThan(1), $"{i.id}: potions stack");
                    Assert.That(i.use.cooldown, Is.GreaterThanOrEqualTo(0f), i.id);
                }
            }
        }

        [Test]
        public void Weapons_MatchCombatJson_AndAreLockedToTheirCharacters()
        {
            foreach (var i in data.items)
            {
                if (!i.IsWeapon)
                {
                    Assert.That(i.weaponType, Is.Null.Or.Empty, $"{i.id} isn't a weapon");
                    continue;
                }
                Assert.DoesNotThrow(() => combat.GetWeapon(i.weaponType), $"{i.id}: unknown weapon type '{i.weaponType}'");
                Assert.That(i.characters, Is.Not.Empty, $"{i.id}: weapons are locked to the characters who can use them");
                foreach (var c in i.characters)
                    Assert.That(combat.GetCharacter(c).weapons, Does.Contain(i.weaponType), $"{c} can't wield {i.weaponType} ({i.id})");
                Assert.That(i.stats.minDamage, Is.GreaterThan(0), i.id);
                Assert.That(i.stats.maxDamage, Is.GreaterThanOrEqualTo(i.stats.minDamage), i.id);
            }
        }

        [Test]
        public void IronSword_IsAKreshnikSword_ClearlyBetterThanTheWornSword()
        {
            var iron = data.GetItem("iron_sword");
            var worn = data.GetItem("worn_sword");
            Assert.That(iron.weaponType, Is.EqualTo("sword"));
            Assert.That(iron.characters, Is.EqualTo(new[] { "kreshnik" }));
            Assert.That(iron.levelRequired, Is.EqualTo(1));
            Assert.That(iron.modelKey, Is.EqualTo("IronSword"));
            Assert.That(iron.stats.minDamage, Is.GreaterThan(worn.stats.minDamage));
            Assert.That(iron.stats.maxDamage, Is.GreaterThan(worn.stats.maxDamage));
        }

        [Test]
        public void Shops_SellOnlyStarterGearAndPotions()
        {
            foreach (var s in data.shops)
            foreach (var id in s.items)
            {
                var item = data.GetItem(id);
                Assert.That(item.buyPrice, Is.GreaterThan(0), $"{s.id} sells {id} without a price");
                Assert.That(item.levelRequired, Is.LessThanOrEqualTo(data.offline.playerLevel), $"{s.id}: {id} isn't starter gear");
            }
            Assert.That(data.shops.SelectMany(s => s.items), Does.Not.Contain("iron_sword"), "the Iron Sword is a drop");
        }

        [Test]
        public void QuickSlots_HoldTwoPotions()
        {
            Assert.That(data.inventory.quickSlots.Length, Is.EqualTo(PlayerItems.QuickSlotCount));
            foreach (var id in data.inventory.quickSlots) Assert.That(data.GetItem(id).IsUsable, id);
            Assert.That(data.inventory.capacity, Is.GreaterThan(0));
            Assert.That(data.loot.autoLootRadius, Is.GreaterThan(0f));
            Assert.That(data.loot.groundTime, Is.GreaterThan(0f));
            Assert.That(data.loot.pickupDelay, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void StarterKits_GiveEveryCharacterBothWeapons()
        {
            foreach (var c in combat.characters)
            {
                var kit = data.GetStarterKit(c.id);
                Assert.That(kit.gold, Is.GreaterThanOrEqualTo(0));
                var weapons = kit.equip.Concat(kit.bag.Select(e => e.item)).Select(data.GetItem).Where(i => i.IsWeapon).ToList();
                Assert.That(weapons.Select(w => w.weaponType), Is.EquivalentTo(c.weapons), $"{c.id} starts with a basic version of both weapons");
                Assert.That(kit.equip.Select(data.GetItem).Count(i => i.IsWeapon), Is.EqualTo(1), $"{c.id} wields exactly one weapon");
                foreach (var e in kit.bag) Assert.That(e.count, Is.InRange(1, data.GetItem(e.item).stackSize), $"{c.id}: {e.item}");
            }
        }

        [Test]
        public void Rarities_MatchTheDesignDoc()
        {
            Assert.That(data.GetRarity("common").bonusLines, Is.EqualTo(0));
            Assert.That(data.GetRarity("fine").bonusLines, Is.EqualTo(1));
            Assert.That(data.GetRarity("rare").bonusLines, Is.EqualTo(2));
            foreach (var r in data.rarities)
            {
                Assert.That(r.dropWeight, Is.GreaterThanOrEqualTo(0f), r.id);
                Assert.That(r.sellMultiplier, Is.GreaterThanOrEqualTo(1f), r.id);
                Assert.That(r.color, Does.Match("^#[0-9A-Fa-f]{6}$"), r.id);
            }
            Assert.That(data.rarities.Sum(r => r.dropWeight), Is.GreaterThan(0f));
        }

        [Test]
        public void BonusLinePool_IsValid_AndEverySlotCanRollARareItem()
        {
            var maxLines = data.rarities.Max(r => r.bonusLines);
            foreach (var line in data.bonusLines)
            {
                Assert.That(StatIds.All, Does.Contain(line.stat), $"unknown bonus stat '{line.stat}'");
                Assert.That(line.min, Is.GreaterThan(0f), line.stat);
                Assert.That(line.max, Is.GreaterThanOrEqualTo(line.min), line.stat);
                Assert.That(line.weight, Is.GreaterThan(0f), line.stat);
                foreach (var slot in line.slots)
                    Assert.That(ItemDef.ParseSlot(slot), Is.Not.EqualTo(EquipSlot.None), $"{line.stat}: bad slot '{slot}'");
            }
            foreach (var slot in new[] { "weapon", "helm", "armor", "boots", "ring", "amulet" })
                Assert.That(data.bonusLines.Count(l => l.slots.Contains(slot)), Is.GreaterThanOrEqualTo(maxLines),
                    $"{slot} needs at least {maxLines} different bonus lines");
        }

        [Test]
        public void LootTables_AreValid()
        {
            foreach (var t in data.lootTables)
            {
                Assert.DoesNotThrow(() => combat.GetMonster(t.monster), $"loot table {t.id}: unknown monster '{t.monster}'");
                Assert.That(t.goldChance, Is.InRange(0f, 1f), t.id);
                Assert.That(t.goldMin, Is.GreaterThanOrEqualTo(0), t.id);
                Assert.That(t.goldMax, Is.GreaterThanOrEqualTo(t.goldMin), t.id);
                foreach (var d in t.drops)
                {
                    Assert.That(d.chance, Is.InRange(0f, 1f), $"{t.id}/{d.item}");
                    Assert.That(d.min, Is.GreaterThanOrEqualTo(1), $"{t.id}/{d.item}");
                    Assert.That(d.max, Is.GreaterThanOrEqualTo(d.min), $"{t.id}/{d.item}");
                }
            }
            foreach (var m in combat.monsters) Assert.That(data.FindLootTable(m.id), Is.Not.Null, $"{m.id} drops loot");
        }
    }
}
