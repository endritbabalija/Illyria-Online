using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    /// <summary>
    /// Makes item instances with unique ids and rolls rarity and bonus lines for drops. Offline the
    /// ids are "local-N" for the session; the server hands out real ones in M4.
    /// </summary>
    public sealed class ItemFactory
    {
        readonly ItemsData data;
        readonly string idPrefix;
        long nextId;

        public ItemsData Data => data;

        public ItemFactory(ItemsData data, string idPrefix = "local-")
        {
            this.data = data;
            this.idPrefix = idPrefix;
        }

        public string NextId() => idPrefix + (++nextId);

        /// <summary>A plain Common copy (shop stock, starter kits).</summary>
        public ItemInstance Create(string defId, int count = 1) => Create(data.GetItem(defId), count);

        public ItemInstance Create(ItemDef def, int count = 1) =>
            Create(def, count, data.GetRarity(ItemsData.CommonRarity), null);

        public ItemInstance Create(ItemDef def, int count, RarityData rarity, IEnumerable<BonusLine> lines)
        {
            if (count < 1 || count > Math.Max(1, def.stackSize))
                throw new ArgumentOutOfRangeException(nameof(count), $"{def.id}: count {count} outside 1..{def.stackSize}");
            return new ItemInstance(NextId(), def, rarity, count, lines);
        }

        /// <summary>A dropped copy: gear rolls a rarity and that rarity's bonus lines; stackables are always Common.</summary>
        public ItemInstance CreateDrop(string defId, int count, Random rng)
        {
            var def = data.GetItem(defId);
            if (!def.IsGear) return Create(def, count);
            var rarity = RollRarity(rng);
            return Create(def, 1, rarity, RollBonusLines(def, rarity.bonusLines, rng));
        }

        /// <summary>Picks a rarity by <c>dropWeight</c>.</summary>
        public RarityData RollRarity(Random rng)
        {
            var total = 0f;
            foreach (var r in data.rarities) total += Math.Max(0f, r.dropWeight);
            if (total <= 0f) return data.GetRarity(ItemsData.CommonRarity);

            var roll = (float)rng.NextDouble() * total;
            foreach (var r in data.rarities)
            {
                var w = Math.Max(0f, r.dropWeight);
                if (w <= 0f) continue;
                if (roll < w) return r;
                roll -= w;
            }
            // Float rounding at the very top end: the last rarity with any weight.
            for (var i = data.rarities.Length - 1; i >= 0; i--)
                if (data.rarities[i].dropWeight > 0f) return data.rarities[i];
            return data.GetRarity(ItemsData.CommonRarity);
        }

        /// <summary>
        /// Rolls up to <paramref name="count"/> lines from the pool entries that allow the item's slot,
        /// weighted, each stat at most once. Whole-number stats round to integers, fractions to 0.001.
        /// </summary>
        public List<BonusLine> RollBonusLines(ItemDef def, int count, Random rng)
        {
            var lines = new List<BonusLine>();
            if (count <= 0 || !def.IsGear) return lines;

            var pool = new List<BonusLineData>();
            foreach (var entry in data.bonusLines)
                if (entry.weight > 0f && entry.slots != null && Array.IndexOf(entry.slots, def.slot) >= 0)
                    pool.Add(entry);

            while (lines.Count < count && pool.Count > 0)
            {
                var total = 0f;
                foreach (var e in pool) total += e.weight;
                var roll = (float)rng.NextDouble() * total;
                var pick = pool.Count - 1;
                for (var i = 0; i < pool.Count; i++)
                {
                    if (roll < pool[i].weight) { pick = i; break; }
                    roll -= pool[i].weight;
                }

                var line = pool[pick];
                pool.RemoveAt(pick);
                var value = line.min + (float)rng.NextDouble() * (line.max - line.min);
                value = StatIds.IsWhole(line.stat) ? (float)Math.Round(value) : (float)Math.Round(value, 3);
                lines.Add(new BonusLine(line.stat, value));
            }
            return lines;
        }
    }
}
