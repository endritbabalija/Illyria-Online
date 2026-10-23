using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    /// <summary>One rolled bonus line on a Fine or Rare item, e.g. hp +18 or dodge +0.02.</summary>
    public struct BonusLine
    {
        /// <summary>One of <see cref="StatIds"/>.</summary>
        public string Stat;
        public float Value;

        public BonusLine(string stat, float value)
        {
            Stat = stat;
            Value = value;
        }

        /// <summary>"+18 HP", "+2% Dodge".</summary>
        public override string ToString() => StatText.Format(Stat, Value);
    }

    /// <summary>
    /// One real item: a definition from items.json plus what was rolled for this copy (rarity, bonus
    /// lines) and a unique id, so every copy can be traced through the gold and item ledger. A potion
    /// stack is one instance with a <see cref="Count"/>. Pure C#; the server owns these from M4.
    /// </summary>
    public sealed class ItemInstance
    {
        readonly List<BonusLine> bonusLines;

        public string Id { get; }
        public ItemDef Def { get; }
        public string DefId => Def.id;
        public string Name => Def.name;
        public RarityData Rarity { get; }
        public IReadOnlyList<BonusLine> BonusLines => bonusLines;
        /// <summary>1 for gear; how many are in the stack for potions.</summary>
        public int Count { get; internal set; }

        public ItemInstance(string id, ItemDef def, RarityData rarity, int count, IEnumerable<BonusLine> lines = null)
        {
            Id = id;
            Def = def;
            Rarity = rarity;
            Count = count;
            bonusLines = lines != null ? new List<BonusLine>(lines) : new List<BonusLine>();
        }

        /// <summary>The definition's stats plus every bonus line.</summary>
        public ItemStats TotalStats()
        {
            var total = new ItemStats();
            total.Add(Def.stats);
            foreach (var line in bonusLines) total.Add(line.Stat, line.Value);
            return total;
        }

        /// <summary>What an NPC pays for one of these: sell price × the rarity's multiplier, rounded down.</summary>
        public int SellPriceEach => (int)(Def.sellPrice * (Rarity != null ? Rarity.sellMultiplier : 1f));

        public override string ToString() =>
            $"{Name}{(Count > 1 ? " ×" + Count : "")}{(Rarity != null && Rarity.id != ItemsData.CommonRarity ? " (" + Rarity.name + ")" : "")} #{Id}";
    }

    /// <summary>Player-facing stat names and values.</summary>
    public static class StatText
    {
        public static string Name(string stat)
        {
            switch (stat)
            {
                case StatIds.Damage: return "Damage";
                case StatIds.Defense: return "Defense";
                case StatIds.Hp: return "HP";
                case StatIds.Spirit: return "Spirit";
                case StatIds.Accuracy: return "Accuracy";
                case StatIds.Dodge: return "Dodge";
                case StatIds.SkillDamage: return "Skill damage";
                case StatIds.SkillDefense: return "Skill defense";
                case StatIds.PoisonResist: return "Poison resistance";
                case StatIds.ParalysisResist: return "Paralysis resistance";
                default: return stat;
            }
        }

        public static string Format(string stat, float value) =>
            StatIds.IsWhole(stat)
                ? $"+{(int)System.Math.Round(value)} {Name(stat)}"
                : $"+{System.Math.Round(value * 100f, 1)}% {Name(stat)}";
    }
}
