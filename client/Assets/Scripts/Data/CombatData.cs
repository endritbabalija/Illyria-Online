using System;
using UnityEngine;

namespace Wuxia.Data
{
    // Mirrors shared/gamedata/combat.json (the com.wuxia.gamedata package). Field names match the
    // JSON keys exactly so the Nakama server can read the same file in M4.

    [Serializable]
    public class AttackData
    {
        public int minDamage;
        public int maxDamage;
        /// <summary>Seconds between basic attacks.</summary>
        public float interval;
        /// <summary>Reach in metres, measured centre to centre on the ground plane.</summary>
        public float range;
        /// <summary>Largest height difference the attack can still reach (a wolf can't bite up onto a ledge).</summary>
        public float reachHeight;
        /// <summary>Base chance to hit, 0–1, before the defender's dodge.</summary>
        public float accuracy;
        public float critChance;
        public float critMultiplier;
    }

    /// <summary>Status resistances, 0–1; each one cuts the chance of that status landing.</summary>
    [Serializable]
    public class ResistData
    {
        public float poison;
        public float paralysis;
        public float stun;

        public float Get(string key)
        {
            switch (key)
            {
                case "poison": return poison;
                case "paralysis": return paralysis;
                case "stun": return stun;
                default: throw new ArgumentException($"Unknown resistance '{key}'");
            }
        }
    }

    [Serializable]
    public class PlayerData
    {
        public int maxHp;
        public int maxSpirit;
        /// <summary>Spirit is slow to come back, so skills are for packs, bosses and PvP.</summary>
        public float spiritRegenPerSecond;
        public int defense;
        public float dodge;
        public float moveSpeed;
        public float turnSpeed;
        /// <summary>The Attack button locks the nearest monster within this range.</summary>
        public float lockRange;
        /// <summary>A locked target further away than this is dropped.</summary>
        public float loseTargetRange;
        /// <summary>Seconds without taking damage before HP starts regenerating.</summary>
        public float regenDelay;
        public float regenPerSecond;
        public float respawnDelay;
        public ResistData resist;
    }

    [Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        /// <summary>Weapon ids this character can wield, in swap order.</summary>
        public string[] weapons;
    }

    [Serializable]
    public class WeaponData
    {
        public string id;
        public string name;
        /// <summary>The weapon's basic attack.</summary>
        public AttackData attack;
    }

    [Serializable]
    public class MonsterData
    {
        public string id;
        public string name;
        public int level;
        public int maxHp;
        public int defense;
        public float dodge;
        public float wanderSpeed;
        public float chaseSpeed;
        public float aggroRange;
        /// <summary>Gives up the chase once this far from its spawn point.</summary>
        public float leashRange;
        public float wanderRadius;
        public float respawnDelay;
        public ResistData resist;
        public AttackData attack;
    }

    [Serializable]
    public class CombatData
    {
        public int version;
        public PlayerData player;
        public CharacterData[] characters;
        public WeaponData[] weapons;
        public MonsterData[] monsters;

        public CharacterData GetCharacter(string id)
        {
            foreach (var c in characters)
                if (c.id == id) return c;
            throw new ArgumentException($"No character '{id}' in combat data");
        }

        public WeaponData GetWeapon(string id)
        {
            foreach (var w in weapons)
                if (w.id == id) return w;
            throw new ArgumentException($"No weapon '{id}' in combat data");
        }

        /// <summary>The character with this id, or null.</summary>
        public CharacterData FindCharacter(string id)
        {
            foreach (var c in characters)
                if (c.id == id) return c;
            return null;
        }

        /// <summary>The weapon type with this id, or null.</summary>
        public WeaponData FindWeapon(string id)
        {
            foreach (var w in weapons)
                if (w.id == id) return w;
            return null;
        }

        public MonsterData GetMonster(string id)
        {
            foreach (var m in monsters)
                if (m.id == id) return m;
            throw new ArgumentException($"No monster '{id}' in combat data");
        }

        public static CombatData FromJson(string json)
        {
            var data = JsonUtility.FromJson<CombatData>(json);
            if (data?.player == null || data.characters == null || data.weapons == null || data.monsters == null)
                throw new FormatException("combat.json is missing 'player', 'characters', 'weapons' or 'monsters'");
            return data;
        }
    }

    /// <summary>Game data loaded once per session; set by <see cref="World.ZoneSession"/> in Awake.</summary>
    public static class GameData
    {
        static CombatData combat;
        static SkillData skills;
        static ItemsData items;

        public static CombatData Combat =>
            combat ?? throw new InvalidOperationException("GameData.Combat used before ZoneSession loaded it");

        public static SkillData Skills =>
            skills ?? throw new InvalidOperationException("GameData.Skills used before ZoneSession loaded it");

        public static ItemsData Items =>
            items ?? throw new InvalidOperationException("GameData.Items used before ZoneSession loaded it");

        public static void Load(TextAsset combatJson, TextAsset skillsJson, TextAsset itemsJson)
        {
            if (itemsJson == null) throw new InvalidOperationException("ZoneSession has no items.json assigned");
            combat = CombatData.FromJson(combatJson.text);
            skills = SkillData.FromJson(skillsJson.text);
            items = ItemsData.FromJson(itemsJson.text);
        }
    }
}
