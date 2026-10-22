using System;
using Wuxia.Data;

namespace Wuxia.Combat
{
    public struct AttackResult
    {
        public bool Hit;
        public bool Crit;
        public int Damage;

        public static AttackResult Miss => new AttackResult();
    }

    /// <summary>
    /// Basic-attack rules, kept free of Unity types so the same formula can move to the server in M4
    /// (the client will then only predict visuals).
    /// </summary>
    public static class CombatMath
    {
        public const float MinHitChance = 0.05f;
        public const float MaxHitChance = 0.99f;

        public static float HitChance(float accuracy, float dodge) =>
            Math.Clamp(accuracy - dodge, MinHitChance, MaxHitChance);

        /// <summary>Basic attacks can miss or crit; a hit always deals at least 1 damage.</summary>
        public static AttackResult RollBasicAttack(AttackData attack, int defense, float dodge, Random rng)
        {
            if (rng.NextDouble() >= HitChance(attack.accuracy, dodge)) return AttackResult.Miss;

            var crit = rng.NextDouble() < attack.critChance;
            float raw = rng.Next(attack.minDamage, attack.maxDamage + 1);
            if (crit) raw *= attack.critMultiplier;

            return new AttackResult
            {
                Hit = true,
                Crit = crit,
                Damage = Math.Max(1, (int)Math.Round(raw) - defense),
            };
        }
    }
}
