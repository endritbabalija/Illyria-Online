using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wuxia.Data;
using Wuxia.Items;

namespace Wuxia.Tests
{
    /// <summary>Loot table rolls, rarity odds and bonus lines, with a seeded RNG.</summary>
    public class LootTests
    {
        const int Rolls = 20000;

        static ItemsData data;

        [OneTimeSetUp]
        public void LoadData() => data = ItemDataTests.Load();

        /// <summary>Allowed gap between an observed rate and its odds: 4 standard deviations, at least 0.005.</summary>
        static double Tolerance(double p, int n) => System.Math.Max(0.005, 4 * System.Math.Sqrt(p * (1 - p) / n));

        [Test]
        public void WolfLoot_MatchesItsTable()
        {
            var table = data.FindLootTable("wolf");
            var factory = new ItemFactory(data);
            var rng = new System.Random(1234);
            var goldDrops = 0;
            var seen = table.drops.ToDictionary(d => d.item, d => 0);
            var ids = new HashSet<string>();

            for (var i = 0; i < Rolls; i++)
            {
                var perEntry = new HashSet<string>();
                foreach (var drop in LootRoller.Roll(table, factory, rng))
                {
                    if (drop.IsGold)
                    {
                        goldDrops++;
                        Assert.That(drop.Gold, Is.InRange(table.goldMin, table.goldMax));
                        continue;
                    }
                    Assert.That(ids.Add(drop.Item.Id), "every dropped item has its own id");
                    var entry = table.drops.First(d => d.item == drop.Item.DefId);
                    Assert.That(drop.Item.Count, Is.InRange(entry.min, entry.max));
                    perEntry.Add(drop.Item.DefId);
                }
                foreach (var id in perEntry) seen[id]++;
            }

            Assert.That((double)goldDrops / Rolls, Is.EqualTo(table.goldChance).Within(Tolerance(table.goldChance, Rolls)), "gold");
            foreach (var entry in table.drops)
                Assert.That((double)seen[entry.item] / Rolls, Is.EqualTo(entry.chance).Within(Tolerance(entry.chance, Rolls)), entry.item);
        }

        [Test]
        public void IronSword_DropsFromWolves_AtItsTableChance()
        {
            var table = data.FindLootTable("wolf");
            var chance = table.drops.First(d => d.item == "iron_sword").chance;
            Assert.That(chance, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), "the wolf table must be able to drop the Iron Sword");

            var factory = new ItemFactory(data);
            var rng = new System.Random(42);
            var swords = 0;
            for (var i = 0; i < Rolls; i++)
                if (LootRoller.Roll(table, factory, rng).Any(d => !d.IsGold && d.Item.DefId == "iron_sword")) swords++;
            Assert.That((double)swords / Rolls, Is.EqualTo(chance).Within(Tolerance(chance, Rolls)));
        }

        [Test]
        public void GearDrops_RollRarities_ByWeight_WithThatManyBonusLines()
        {
            var factory = new ItemFactory(data);
            var rng = new System.Random(99);
            var counts = data.rarities.ToDictionary(r => r.id, r => 0);
            var total = data.rarities.Sum(r => r.dropWeight);

            foreach (var slotItem in new[] { "iron_sword", "cloth_hood", "padded_vest", "leather_boots", "copper_ring", "shepherds_amulet" })
            {
                var def = data.GetItem(slotItem);
                for (var i = 0; i < Rolls / 6; i++)
                {
                    var item = factory.CreateDrop(slotItem, 1, rng);
                    counts[item.Rarity.id]++;
                    Assert.That(item.BonusLines.Count, Is.EqualTo(item.Rarity.bonusLines), $"{slotItem} {item.Rarity.id}");
                    Assert.That(item.BonusLines.Select(l => l.Stat).Distinct().Count(), Is.EqualTo(item.BonusLines.Count), "no stat twice");
                    foreach (var line in item.BonusLines)
                    {
                        var pool = data.bonusLines.First(b => b.stat == line.Stat);
                        Assert.That(pool.slots, Does.Contain(def.slot), $"{line.Stat} can't roll on {def.slot}");
                        Assert.That(line.Value, Is.InRange(pool.min - 1e-3f, pool.max + 1e-3f), line.Stat);
                    }
                }
            }

            var n = Rolls / 6 * 6;
            foreach (var r in data.rarities)
            {
                var p = r.dropWeight / total;
                Assert.That((double)counts[r.id] / n, Is.EqualTo(p).Within(Tolerance(p, n)), r.id);
            }
        }

        [Test]
        public void PotionDrops_AreAlwaysCommon_AndStack()
        {
            var factory = new ItemFactory(data);
            var rng = new System.Random(7);
            for (var i = 0; i < 200; i++)
            {
                var potion = factory.CreateDrop("hp_potion", 2, rng);
                Assert.That(potion.Rarity.id, Is.EqualTo(ItemsData.CommonRarity));
                Assert.That(potion.BonusLines, Is.Empty);
                Assert.That(potion.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void SameSeed_SameLoot()
        {
            var table = data.FindLootTable("wolf");
            string Run(int seed)
            {
                var rng = new System.Random(seed);
                var factory = new ItemFactory(data);
                return string.Join(",", Enumerable.Range(0, 200).SelectMany(_ => LootRoller.Roll(table, factory, rng)).Select(d => d.ToString()));
            }
            Assert.That(Run(42), Is.EqualTo(Run(42)));
        }

        [Test]
        public void MonsterWithoutATable_DropsNothing()
        {
            Assert.That(LootRoller.RollFor("no_such_monster", new ItemFactory(data), new System.Random(1)), Is.Empty);
        }
    }
}
