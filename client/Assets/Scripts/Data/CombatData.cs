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
        /// <summary>Base chance to hit, 0–1, before the defender's dodge.</summary>
        public float accuracy;
        public float critChance;
        public float critMultiplier;
    }

    [Serializable]
    public class PlayerData
    {
        public string name;
        public int maxHp;
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
        public AttackData attack;
    }

    [Serializable]
    public class CombatData
    {
        public int version;
        public PlayerData player;
        public MonsterData[] monsters;

        public MonsterData GetMonster(string id)
        {
            foreach (var m in monsters)
                if (m.id == id) return m;
            throw new ArgumentException($"No monster '{id}' in combat data");
        }

        public static CombatData FromJson(string json)
        {
            var data = JsonUtility.FromJson<CombatData>(json);
            if (data?.player?.attack == null || data.monsters == null)
                throw new FormatException("combat.json is missing 'player', 'player.attack' or 'monsters'");
            return data;
        }
    }

    /// <summary>Game data loaded once per session; set by <see cref="World.ZoneSession"/> in Awake.</summary>
    public static class GameData
    {
        static CombatData combat;

        public static CombatData Combat =>
            combat ?? throw new InvalidOperationException("GameData.Combat used before ZoneSession loaded it");

        public static void Load(TextAsset combatJson) => combat = CombatData.FromJson(combatJson.text);
    }
}
