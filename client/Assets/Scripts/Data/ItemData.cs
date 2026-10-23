using System;
using UnityEngine;

namespace Wuxia.Data
{
    // Mirrors shared/gamedata/items.json. Field names match the JSON keys exactly so the Nakama
    // server can read the same file in M4.

    /// <summary>The 6 gear slots. <see cref="None"/> is for items that can't be worn (potions).</summary>
    public enum EquipSlot { None = -1, Weapon, Helm, Armor, Boots, Ring, Amulet }

    /// <summary>Offline stand-ins until levels (M7) and saves (M4) exist.</summary>
    [Serializable]
    public class OfflineItemData
    {
        public string note;
        /// <summary>Every character counts as this level for item level requirements until M7.</summary>
        public int playerLevel = 1;
    }

    [Serializable]
    public class InventoryRules
    {
        /// <summary>Bag slots; a stack of potions takes one slot.</summary>
        public int capacity;
        /// <summary>Item ids on the 2 potion quick slots by default.</summary>
        public string[] quickSlots;
    }

    [Serializable]
    public class LootRules
    {
        /// <summary>Gold and items within this many metres (ground plane) are picked up on their own.</summary>
        public float autoLootRadius;
        /// <summary>Seconds a drop stays on the ground before it vanishes.</summary>
        public float groundTime;
        /// <summary>Seconds after dropping before auto-loot may take it, so the drop is seen first.</summary>
        public float pickupDelay;
    }

    [Serializable]
    public class StatCaps
    {
        /// <summary>Highest total dodge from base and gear.</summary>
        public float dodge;
        /// <summary>Highest poison or paralysis resistance from base and gear.</summary>
        public float resist;
    }

    [Serializable]
    public class RarityData
    {
        public string id;
        public string name;
        /// <summary>Name colour, "#RRGGBB".</summary>
        public string color;
        /// <summary>Random bonus lines rolled onto an item of this rarity.</summary>
        public int bonusLines;
        /// <summary>Relative chance for a gear drop to roll this rarity; 0 = never from plain drops.</summary>
        public float dropWeight;
        /// <summary>NPC sell price = item sell price × this, rounded down.</summary>
        public float sellMultiplier = 1f;
    }

    /// <summary>One entry of the bonus-line pool: which stat, its value range, and which slots can roll it.</summary>
    [Serializable]
    public class BonusLineData
    {
        /// <summary>One of <see cref="StatIds"/>.</summary>
        public string stat;
        public float min;
        public float max;
        /// <summary>Relative chance among the lines a slot can roll.</summary>
        public float weight = 1f;
        /// <summary>Slot names (weapon, helm, armor, boots, ring, amulet) that can roll this line.</summary>
        public string[] slots;
    }

    /// <summary>
    /// Stats an item (or a bonus line, or the sum of all worn gear) adds. Damage is flat, accuracy,
    /// dodge, skill damage and resistances are fractions (0.02 = 2%), skill defense is flat and is
    /// added to defense against skill forms. A weapon's min/max damage replaces its weapon type's
    /// basic-attack damage from combat.json.
    /// </summary>
    [Serializable]
    public class ItemStats
    {
        public int minDamage;
        public int maxDamage;
        public int defense;
        public int hp;
        public int spirit;
        public float accuracy;
        public float dodge;
        public float skillDamage;
        public int skillDefense;
        public float poisonResist;
        public float paralysisResist;

        public void Add(ItemStats other)
        {
            if (other == null) return;
            minDamage += other.minDamage;
            maxDamage += other.maxDamage;
            defense += other.defense;
            hp += other.hp;
            spirit += other.spirit;
            accuracy += other.accuracy;
            dodge += other.dodge;
            skillDamage += other.skillDamage;
            skillDefense += other.skillDefense;
            poisonResist += other.poisonResist;
            paralysisResist += other.paralysisResist;
        }

        /// <summary>Adds one bonus-line value; "damage" raises both ends of the damage range.</summary>
        public void Add(string stat, float value)
        {
            var whole = (int)Math.Round(value);
            switch (stat)
            {
                case StatIds.Damage: minDamage += whole; maxDamage += whole; break;
                case StatIds.Defense: defense += whole; break;
                case StatIds.Hp: hp += whole; break;
                case StatIds.Spirit: spirit += whole; break;
                case StatIds.Accuracy: accuracy += value; break;
                case StatIds.Dodge: dodge += value; break;
                case StatIds.SkillDamage: skillDamage += value; break;
                case StatIds.SkillDefense: skillDefense += whole; break;
                case StatIds.PoisonResist: poisonResist += value; break;
                case StatIds.ParalysisResist: paralysisResist += value; break;
                default: throw new ArgumentException($"Unknown stat '{stat}'");
            }
        }
    }

    /// <summary>Stat names used by bonus lines.</summary>
    public static class StatIds
    {
        public const string Damage = "damage";
        public const string Defense = "defense";
        public const string Hp = "hp";
        public const string Spirit = "spirit";
        public const string Accuracy = "accuracy";
        public const string Dodge = "dodge";
        public const string SkillDamage = "skillDamage";
        public const string SkillDefense = "skillDefense";
        public const string PoisonResist = "poisonResist";
        public const string ParalysisResist = "paralysisResist";

        public static readonly string[] All =
            { Damage, Defense, Hp, Spirit, Accuracy, Dodge, SkillDamage, SkillDefense, PoisonResist, ParalysisResist };

        /// <summary>Whole-number stats; the rest are fractions shown as percentages.</summary>
        public static bool IsWhole(string stat) =>
            stat == Damage || stat == Defense || stat == Hp || stat == Spirit || stat == SkillDefense;
    }

    /// <summary>What drinking a potion does. Potions sharing a <c>cooldownGroup</c> share one cooldown.</summary>
    [Serializable]
    public class ItemUseData
    {
        public int hp;
        public int spirit;
        public string cooldownGroup;
        public float cooldown;
    }

    [Serializable]
    public class ItemDef
    {
        public string id;
        public string name;
        /// <summary>weapon, helm, armor, boots, ring or amulet; empty for items that can't be worn.</summary>
        public string slot;
        /// <summary>Weapons only: a weapon id from combat.json (sword, bow, sicas, staff).</summary>
        public string weaponType;
        /// <summary>Character ids that can wear it; empty = everyone.</summary>
        public string[] characters;
        public int levelRequired;
        /// <summary>1 for gear; potions stack up to this many in one bag slot.</summary>
        public int stackSize = 1;
        /// <summary>What an NPC shop charges; 0 = not sold by NPCs.</summary>
        public int buyPrice;
        /// <summary>What an NPC pays for one (before the rarity multiplier).</summary>
        public int sellPrice;
        /// <summary>The Unity side maps this to a prefab (e.g. "IronSword").</summary>
        public string modelKey;
        public ItemStats stats;
        /// <summary>Set on potions (hp or spirit &gt; 0).</summary>
        public ItemUseData use;

        public EquipSlot Slot => ParseSlot(slot);
        public bool IsGear => Slot != EquipSlot.None;
        public bool IsWeapon => Slot == EquipSlot.Weapon;
        public bool IsStackable => stackSize > 1;
        public bool IsUsable => use != null && (use.hp > 0 || use.spirit > 0);

        public bool AllowsCharacter(string characterId) =>
            characters == null || characters.Length == 0 || Array.IndexOf(characters, characterId) >= 0;

        public static EquipSlot ParseSlot(string value)
        {
            switch (value)
            {
                case null:
                case "": return EquipSlot.None;
                case "weapon": return EquipSlot.Weapon;
                case "helm": return EquipSlot.Helm;
                case "armor": return EquipSlot.Armor;
                case "boots": return EquipSlot.Boots;
                case "ring": return EquipSlot.Ring;
                case "amulet": return EquipSlot.Amulet;
                default: throw new FormatException($"Unknown gear slot '{value}'");
            }
        }
    }

    [Serializable]
    public class ItemCount
    {
        public string item;
        public int count = 1;
    }

    /// <summary>What a character starts with (and gets back when the dev button switches character).</summary>
    [Serializable]
    public class StarterKitData
    {
        public string character;
        public int gold;
        /// <summary>Worn from the start, one per slot.</summary>
        public string[] equip;
        public ItemCount[] bag;
    }

    [Serializable]
    public class ShopData
    {
        public string id;
        public string name;
        /// <summary>Item ids on sale, at each item's <c>buyPrice</c>.</summary>
        public string[] items;
    }

    /// <summary>One independent roll on a loot table.</summary>
    [Serializable]
    public class LootDropData
    {
        public string item;
        /// <summary>0–1 chance this entry drops at all.</summary>
        public float chance;
        public int min = 1;
        public int max = 1;
    }

    [Serializable]
    public class LootTableData
    {
        public string id;
        /// <summary>Monster id from combat.json that rolls this table when it dies.</summary>
        public string monster;
        public string note;
        public float goldChance;
        public int goldMin;
        public int goldMax;
        public LootDropData[] drops;
    }

    [Serializable]
    public class ItemsData
    {
        public int version;
        public OfflineItemData offline;
        public InventoryRules inventory;
        public LootRules loot;
        public StatCaps statCaps;
        public RarityData[] rarities;
        public BonusLineData[] bonusLines;
        public ItemDef[] items;
        public StarterKitData[] starterKits;
        public ShopData[] shops;
        public LootTableData[] lootTables;

        public const string CommonRarity = "common";

        public ItemDef GetItem(string id)
        {
            var item = FindItem(id);
            return item ?? throw new ArgumentException($"No item '{id}' in item data");
        }

        public ItemDef FindItem(string id)
        {
            foreach (var i in items)
                if (i.id == id) return i;
            return null;
        }

        public RarityData GetRarity(string id)
        {
            foreach (var r in rarities)
                if (r.id == id) return r;
            throw new ArgumentException($"No rarity '{id}' in item data");
        }

        public ShopData GetShop(string id)
        {
            foreach (var s in shops)
                if (s.id == id) return s;
            throw new ArgumentException($"No shop '{id}' in item data");
        }

        public StarterKitData GetStarterKit(string characterId)
        {
            foreach (var k in starterKits)
                if (k.character == characterId) return k;
            throw new ArgumentException($"No starter kit for character '{characterId}' in item data");
        }

        /// <summary>The loot table a monster rolls when it dies, or null if it drops nothing.</summary>
        public LootTableData FindLootTable(string monsterId)
        {
            foreach (var t in lootTables)
                if (t.monster == monsterId) return t;
            return null;
        }

        public static ItemsData FromJson(string json)
        {
            var data = JsonUtility.FromJson<ItemsData>(json);
            if (data?.inventory == null || data.loot == null || data.rarities == null || data.items == null
                || data.starterKits == null || data.shops == null || data.lootTables == null)
                throw new FormatException("items.json is missing 'inventory', 'loot', 'rarities', 'items', 'starterKits', 'shops' or 'lootTables'");
            if (data.offline == null) data.offline = new OfflineItemData();
            if (data.statCaps == null) data.statCaps = new StatCaps { dodge = 1f, resist = 1f };
            if (data.bonusLines == null) data.bonusLines = new BonusLineData[0];
            if (data.inventory.quickSlots == null) data.inventory.quickSlots = new string[0];
            foreach (var item in data.items)
            {
                if (item.stats == null) item.stats = new ItemStats();
                if (item.characters == null) item.characters = new string[0];
                // JsonUtility fills a missing nested object with zeros; only potions keep a use block.
                if (item.use != null && item.use.hp <= 0 && item.use.spirit <= 0) item.use = null;
            }
            return data;
        }
    }
}
