using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    /// <summary>One pile on the ground: either gold or one item (a gear piece or a potion stack).</summary>
    public sealed class LootDrop
    {
        public int Gold { get; }
        public ItemInstance Item { get; }
        public bool IsGold => Item == null;
        /// <summary>Set once picked up, so the same drop can never be taken twice.</summary>
        public bool Taken { get; internal set; }

        LootDrop(int gold, ItemInstance item)
        {
            Gold = gold;
            Item = item;
        }

        public static LootDrop OfGold(int gold)
        {
            if (gold <= 0) throw new ArgumentOutOfRangeException(nameof(gold));
            return new LootDrop(gold, null);
        }

        public static LootDrop OfItem(ItemInstance item) => new LootDrop(0, item ?? throw new ArgumentNullException(nameof(item)));

        public override string ToString() => IsGold ? $"{Gold} gold" : Item.ToString();
    }

    /// <summary>Rolls a monster's loot table: gold (goldChance, goldMin..goldMax), then each entry independently.</summary>
    public static class LootRoller
    {
        public static List<LootDrop> Roll(LootTableData table, ItemFactory factory, Random rng)
        {
            var drops = new List<LootDrop>();
            if (table == null) return drops;

            if (table.goldMax > 0 && rng.NextDouble() < table.goldChance)
            {
                var gold = rng.Next(Math.Max(1, table.goldMin), Math.Max(1, table.goldMax) + 1);
                drops.Add(LootDrop.OfGold(gold));
            }

            if (table.drops == null) return drops;
            foreach (var entry in table.drops)
            {
                if (rng.NextDouble() >= entry.chance) continue;
                var def = factory.Data.GetItem(entry.item);
                var count = rng.Next(Math.Max(1, entry.min), Math.Max(entry.min, entry.max) + 1);
                if (def.IsStackable)
                {
                    // Split anything over a stack (unusual for a monster drop) into several piles.
                    while (count > 0)
                    {
                        var pile = Math.Min(count, def.stackSize);
                        drops.Add(LootDrop.OfItem(factory.CreateDrop(def.id, pile, rng)));
                        count -= pile;
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++) drops.Add(LootDrop.OfItem(factory.CreateDrop(def.id, 1, rng)));
                }
            }
            return drops;
        }

        /// <summary>Rolls the table for a monster id from combat.json; empty when it has none.</summary>
        public static List<LootDrop> RollFor(string monsterId, ItemFactory factory, Random rng) =>
            Roll(factory.Data.FindLootTable(monsterId), factory, rng);
    }
}
