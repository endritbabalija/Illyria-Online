using System;
using Wuxia.Data;

namespace Wuxia.Combat
{
    /// <summary>
    /// Skill-form damage and status rolls, free of Unity types for the move to the server in M4.
    /// Forms always hit and never crit: damage = roll(min..max) × book-level bonus × (1 + skill damage)
    /// − (defense + skill defense), at least 1.
    /// </summary>
    public static class SkillMath
    {
        /// <summary>Damage multiplier for a book at <paramref name="bookLevel"/>: 1 at level 1, +perLevel per level above.</summary>
        public static float LevelMultiplier(int bookLevel, float perLevel) => 1f + perLevel * Math.Max(0, bookLevel - 1);

        /// <param name="defense">The target's defense plus its skill defense.</param>
        /// <param name="damageBonus">The caster's skill damage from gear, as a fraction (0.05 = +5%).</param>
        public static int RollDamage(FormData form, int bookLevel, float perLevel, int defense, Random rng, float damageBonus = 0f)
        {
            float raw = rng.Next(form.minDamage, form.maxDamage + 1);
            raw *= LevelMultiplier(bookLevel, perLevel) * (1f + Math.Max(0f, damageBonus));
            return Math.Max(1, (int)Math.Round(raw) - defense);
        }

        /// <summary>Chance after resistance: chance × (1 − resist), clamped to 0–1.</summary>
        public static float StatusChance(float chance, float resist) =>
            Math.Clamp(chance * (1f - Math.Clamp(resist, 0f, 1f)), 0f, 1f);

        public static bool StatusLands(float chance, float resist, Random rng) =>
            rng.NextDouble() < StatusChance(chance, resist);
    }
}
