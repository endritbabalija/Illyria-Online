using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Items
{
    /// <summary>A character's final combat numbers: base stats from combat.json plus everything worn.</summary>
    public sealed class CharacterStats
    {
        public string CharacterId;
        /// <summary>Weapon id from combat.json (sword, bow, sicas, staff) the basic attack comes from.</summary>
        public string WeaponType;
        public int MaxHp;
        public int MaxSpirit;
        public int Defense;
        public float Dodge;
        /// <summary>Poison and paralysis include gear; stun has no gear stat yet.</summary>
        public ResistData Resist;
        /// <summary>
        /// The basic attack the player fights with: the weapon type's attack (range, interval, crit…)
        /// with its damage replaced by the weapon item's, plus damage and accuracy from all gear.
        /// </summary>
        public AttackData Attack;
        /// <summary>Extra skill-form damage as a fraction (0.05 = +5%).</summary>
        public float SkillDamage;
        /// <summary>Flat reduction of incoming skill-form damage, on top of <see cref="Defense"/>.</summary>
        public int SkillDefense;
        /// <summary>The sum of all worn items' stats, for a stats panel.</summary>
        public ItemStats Gear;
    }

    /// <summary>
    /// Character base stats (combat.json) + worn gear (items.json, bonus lines included) → final
    /// stats. Pure C#, so the server computes the same numbers in M4.
    /// </summary>
    public static class StatCalculator
    {
        /// <param name="weapon">The worn weapon, or null to use the base attack of the character's first weapon type.</param>
        /// <param name="worn">Everything worn, the weapon included.</param>
        public static CharacterStats Compute(CombatData combat, StatCaps caps, string characterId, ItemInstance weapon, IEnumerable<ItemInstance> worn)
        {
            var character = combat.GetCharacter(characterId);
            var player = combat.player;
            var weaponType = weapon != null ? weapon.Def.weaponType : character.weapons[0];
            var baseAttack = combat.GetWeapon(weaponType).attack;

            var gear = new ItemStats();
            foreach (var item in worn) gear.Add(item.TotalStats());

            // A weapon item's damage replaces the weapon type's base damage; without one the base stands.
            var minDamage = gear.minDamage + (weapon != null ? 0 : baseAttack.minDamage);
            var maxDamage = gear.maxDamage + (weapon != null ? 0 : baseAttack.maxDamage);
            minDamage = Math.Max(1, minDamage);
            maxDamage = Math.Max(minDamage, maxDamage);

            var baseResist = player.resist ?? new ResistData();
            return new CharacterStats
            {
                CharacterId = characterId,
                WeaponType = weaponType,
                MaxHp = Math.Max(1, player.maxHp + gear.hp),
                MaxSpirit = Math.Max(0, player.maxSpirit + gear.spirit),
                Defense = Math.Max(0, player.defense + gear.defense),
                Dodge = Clamp(player.dodge + gear.dodge, caps.dodge),
                Resist = new ResistData
                {
                    poison = Clamp(baseResist.poison + gear.poisonResist, caps.resist),
                    paralysis = Clamp(baseResist.paralysis + gear.paralysisResist, caps.resist),
                    stun = baseResist.stun,
                },
                Attack = new AttackData
                {
                    minDamage = minDamage,
                    maxDamage = maxDamage,
                    interval = baseAttack.interval,
                    range = baseAttack.range,
                    reachHeight = baseAttack.reachHeight,
                    accuracy = baseAttack.accuracy + gear.accuracy,
                    critChance = baseAttack.critChance,
                    critMultiplier = baseAttack.critMultiplier,
                },
                SkillDamage = Math.Max(0f, gear.skillDamage),
                SkillDefense = Math.Max(0, gear.skillDefense),
                Gear = gear,
            };
        }

        public static CharacterStats Compute(CombatData combat, ItemsData items, string characterId, Equipment equipment) =>
            Compute(combat, items.statCaps, characterId, equipment.Weapon, equipment.Items);

        static float Clamp(float value, float cap) => Math.Max(0f, Math.Min(value, cap));
    }
}
