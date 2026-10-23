using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wuxia.Data;
using Wuxia.Items;

namespace Wuxia.UI
{
    /// <summary>Player-facing item colours and wording shared by the bag, shop, loot text and ground drops.</summary>
    public static class ItemText
    {
        public const string Good = "#5EE07A";
        public const string Bad = "#FF6A5C";
        public const string Muted = "#9AA3B5";
        public const string GoldHex = "#FFD24A";

        static readonly Dictionary<string, Color> rarityColors = new Dictionary<string, Color>();
        static readonly Dictionary<string, string> rarityHex = new Dictionary<string, string>();

        // Stats shown in this order everywhere (damage is shown as a range first).
        static readonly string[] ListedStats =
        {
            StatIds.Defense, StatIds.Hp, StatIds.Spirit, StatIds.Accuracy, StatIds.Dodge,
            StatIds.SkillDamage, StatIds.SkillDefense, StatIds.PoisonResist, StatIds.ParalysisResist,
        };

        /// <summary>The rarity's colour from items.json (white when missing or unparseable).</summary>
        public static Color RarityColor(RarityData rarity)
        {
            if (rarity == null || string.IsNullOrEmpty(rarity.color)) return Color.white;
            if (!rarityColors.TryGetValue(rarity.color, out var color))
            {
                if (!ColorUtility.TryParseHtmlString(rarity.color, out color)) color = Color.white;
                rarityColors[rarity.color] = color;
            }
            return color;
        }

        /// <summary>"#RRGGBB" for rich text.</summary>
        public static string RarityHex(RarityData rarity)
        {
            var key = rarity != null && !string.IsNullOrEmpty(rarity.color) ? rarity.color : "#FFFFFF";
            if (!rarityHex.TryGetValue(key, out var hex))
            {
                hex = "#" + ColorUtility.ToHtmlStringRGB(RarityColor(rarity));
                rarityHex[key] = hex;
            }
            return hex;
        }

        /// <summary>Tile colour by kind, standing in for icon art: weapons steel, armour leather, jewellery violet, potions by what they restore.</summary>
        public static Color TileColor(ItemDef def)
        {
            if (def.IsUsable) return def.use.hp > 0 ? new Color(0.55f, 0.13f, 0.12f) : new Color(0.14f, 0.32f, 0.62f);
            switch (def.Slot)
            {
                case EquipSlot.Weapon: return new Color(0.30f, 0.34f, 0.40f);
                case EquipSlot.Ring:
                case EquipSlot.Amulet: return new Color(0.36f, 0.24f, 0.46f);
                case EquipSlot.None: return new Color(0.25f, 0.25f, 0.25f);
                default: return new Color(0.40f, 0.29f, 0.18f);
            }
        }

        public static string SlotName(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Weapon: return "Weapon";
                case EquipSlot.Helm: return "Helm";
                case EquipSlot.Armor: return "Armor";
                case EquipSlot.Boots: return "Boots";
                case EquipSlot.Ring: return "Ring";
                case EquipSlot.Amulet: return "Amulet";
                default: return "";
            }
        }

        /// <summary>"Sword", "Helm", "Potion".</summary>
        public static string KindName(ItemDef def)
        {
            if (def.IsWeapon)
            {
                var weapon = GameData.Combat.FindWeapon(def.weaponType);
                return weapon != null ? weapon.name : "Weapon";
            }
            if (def.IsGear) return SlotName(def.Slot);
            return def.IsUsable ? "Potion" : "Item";
        }

        /// <summary>The item's own stats, one line each ("Damage 21–31", "+2% Accuracy").</summary>
        public static void AppendStats(StringBuilder sb, ItemStats s)
        {
            if (s.minDamage != 0 || s.maxDamage != 0) sb.Append("Damage ").Append(s.minDamage).Append('–').Append(s.maxDamage).Append('\n');
            foreach (var stat in ListedStats)
            {
                var v = Get(s, stat);
                if (Mathf.Abs(v) > 0.0001f) sb.Append(Signed(stat, v)).Append('\n');
            }
        }

        /// <summary>
        /// What changes if <paramref name="candidate"/> replaces <paramref name="worn"/> (null = empty
        /// slot): green gains, red losses. Appends "No change" when equal.
        /// </summary>
        public static void AppendDelta(StringBuilder sb, ItemStats candidate, ItemStats worn)
        {
            var any = false;
            var dMin = candidate.minDamage - (worn != null ? worn.minDamage : 0);
            var dMax = candidate.maxDamage - (worn != null ? worn.maxDamage : 0);
            if (dMin != 0 || dMax != 0)
            {
                sb.Append("Damage ").Append(Colored(SignedInt(dMin), dMin)).Append(" / ").Append(Colored(SignedInt(dMax), dMax)).Append('\n');
                any = true;
            }
            foreach (var stat in ListedStats)
            {
                var d = Get(candidate, stat) - (worn != null ? Get(worn, stat) : 0f);
                if (Mathf.Abs(d) < 0.0001f) continue;
                sb.Append(Colored(Signed(stat, d), d)).Append('\n');
                any = true;
            }
            if (!any) sb.Append("<color=").Append(Muted).Append(">No change</color>\n");
        }

        /// <summary>A one-line summary for shop rows: "Damage 14–22", "+1 Defense  +10 HP", "Restores 80 HP".</summary>
        public static string Summary(ItemDef def)
        {
            if (def.IsUsable)
            {
                if (def.use.hp > 0 && def.use.spirit > 0) return $"Restores {def.use.hp} HP and {def.use.spirit} Spirit";
                return def.use.hp > 0 ? $"Restores {def.use.hp} HP" : $"Restores {def.use.spirit} Spirit";
            }
            var sb = new StringBuilder();
            AppendStats(sb, def.stats);
            return sb.ToString().TrimEnd('\n').Replace("\n", "   ");
        }

        /// <summary>Who can use it, "" when anyone can.</summary>
        public static string CharacterNames(ItemDef def)
        {
            if (def.characters == null || def.characters.Length == 0) return "";
            var sb = new StringBuilder();
            foreach (var id in def.characters)
            {
                if (sb.Length > 0) sb.Append(", ");
                var c = GameData.Combat.FindCharacter(id);
                sb.Append(c != null ? c.name : id);
            }
            return sb.ToString();
        }

        /// <summary>Whether this character may wield/wear it, ignoring level.</summary>
        public static bool CharacterCanUse(ItemDef def, CharacterData character)
        {
            if (!def.AllowsCharacter(character.id)) return false;
            return !def.IsWeapon || System.Array.IndexOf(character.weapons, def.weaponType) >= 0;
        }

        public static string Describe(ShopBlock block)
        {
            switch (block)
            {
                case ShopBlock.NotEnoughGold: return "Not enough gold";
                case ShopBlock.BagFull: return "Your bag is full";
                case ShopBlock.Equipped: return "Take it off before selling it";
                case ShopBlock.NotSold: return "Not sold here";
                case ShopBlock.NotFound: return "That item isn't in your bag";
                case ShopBlock.GoldLimit: return "You can't carry any more gold";
                case ShopBlock.BadCount: return "Can't trade that many";
                default: return "";
            }
        }

        public static string Colored(string text, float sign) =>
            sign > 0f ? $"<color={Good}>{text}</color>" : sign < 0f ? $"<color={Bad}>{text}</color>" : text;

        static string SignedInt(int v) => v > 0 ? "+" + v : v.ToString();

        /// <summary>"+2 Defense", "-1.5% Dodge".</summary>
        public static string Signed(string stat, float v)
        {
            if (StatIds.IsWhole(stat))
            {
                var whole = Mathf.RoundToInt(v);
                return $"{(whole > 0 ? "+" : "")}{whole} {StatText.Name(stat)}";
            }
            return $"{(v > 0f ? "+" : "")}{System.Math.Round(v * 100f, 1)}% {StatText.Name(stat)}";
        }

        /// <summary>"12%" for a 0–1 fraction.</summary>
        public static string Percent(float v) => System.Math.Round(v * 100f, 1) + "%";

        static float Get(ItemStats s, string stat)
        {
            switch (stat)
            {
                case StatIds.Defense: return s.defense;
                case StatIds.Hp: return s.hp;
                case StatIds.Spirit: return s.spirit;
                case StatIds.Accuracy: return s.accuracy;
                case StatIds.Dodge: return s.dodge;
                case StatIds.SkillDamage: return s.skillDamage;
                case StatIds.SkillDefense: return s.skillDefense;
                case StatIds.PoisonResist: return s.poisonResist;
                case StatIds.ParalysisResist: return s.paralysisResist;
                default: return 0f;
            }
        }
    }
}
