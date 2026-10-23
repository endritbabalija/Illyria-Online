using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Combat
{
    public enum CastBlock { None, NotLearned, Locked, Cooldown, NoSpirit }

    public enum LevelUpBlock { None, NotLearned, MaxLevel, NotEnoughPoints }

    /// <summary>
    /// One character's skill progress and resources: learned books and their levels (0–12), skill
    /// points and the kill meter that earns them, Spirit, and cooldowns. Pure C# with time passed in,
    /// so the server can own it in M4.
    /// </summary>
    public sealed class SkillState
    {
        readonly SkillData data;
        readonly Dictionary<string, int> bookLevels = new Dictionary<string, int>();
        readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();

        public int MaxSpirit { get; private set; }
        public float SpiritRegenPerSecond { get; }
        public float Spirit { get; private set; }
        public int SkillPoints { get; private set; }
        /// <summary>Kills counted towards the next skill point.</summary>
        public int MeterKills { get; private set; }
        public int KillsPerPoint => data.skillPoints.killsPerPoint;
        /// <summary>Bumped whenever books, levels or points change, so UI knows to refresh.</summary>
        public int Version { get; private set; }

        public SkillState(SkillData data, int maxSpirit, float spiritRegenPerSecond)
        {
            this.data = data;
            MaxSpirit = maxSpirit;
            SpiritRegenPerSecond = spiritRegenPerSecond;
            Spirit = maxSpirit;
        }

        public bool Knows(string bookId) => bookLevels.ContainsKey(bookId);

        /// <summary>The book's level, or -1 if it hasn't been learned.</summary>
        public int BookLevel(string bookId) => bookLevels.TryGetValue(bookId, out var level) ? level : -1;

        /// <summary>Reading a book teaches it at level 0; points then buy its levels. False if already known.</summary>
        public bool Learn(string bookId)
        {
            data.GetBook(bookId); // throws on an unknown id
            if (Knows(bookId)) return false;
            bookLevels[bookId] = 0;
            Version++;
            return true;
        }

        public LevelUpBlock CheckLevelUp(string bookId, out int cost)
        {
            cost = -1;
            if (!bookLevels.TryGetValue(bookId, out var level)) return LevelUpBlock.NotLearned;
            cost = data.LevelUpCost(level);
            if (cost < 0) return LevelUpBlock.MaxLevel;
            return SkillPoints >= cost ? LevelUpBlock.None : LevelUpBlock.NotEnoughPoints;
        }

        public bool LevelUp(string bookId)
        {
            if (CheckLevelUp(bookId, out var cost) != LevelUpBlock.None) return false;
            SkillPoints -= cost;
            bookLevels[bookId]++;
            Version++;
            return true;
        }

        public void AddSkillPoints(int points)
        {
            if (points <= 0) return;
            SkillPoints += points;
            Version++;
        }

        /// <summary>Counts a kill on the meter; returns true when it fills and pays out a point.</summary>
        public bool AddKill()
        {
            MeterKills++;
            Version++;
            if (MeterKills < KillsPerPoint) return false;
            MeterKills = 0;
            SkillPoints++;
            return true;
        }

        public bool IsUnlocked(BookData book, FormData form) => BookLevel(book.id) >= form.unlockLevel;

        public CastBlock CheckCast(BookData book, FormData form, float now)
        {
            if (!Knows(book.id)) return CastBlock.NotLearned;
            if (!IsUnlocked(book, form)) return CastBlock.Locked;
            if (CooldownLeft(form.id, now) > 0f) return CastBlock.Cooldown;
            return Spirit >= form.spiritCost ? CastBlock.None : CastBlock.NoSpirit;
        }

        /// <summary>Spends the form's Spirit and starts its cooldown. Call only after <see cref="CheckCast"/> passed.</summary>
        public void CommitCast(FormData form, float now) => Spend(form.id, form.spiritCost, form.cooldown, now);

        public CastBlock CheckLeap(float now)
        {
            var leap = data.drangueLeap;
            if (!Knows(leap.bookId)) return CastBlock.NotLearned;
            if (CooldownLeft(leap.bookId, now) > 0f) return CastBlock.Cooldown;
            return Spirit >= leap.spiritCost ? CastBlock.None : CastBlock.NoSpirit;
        }

        public void CommitLeap(float now)
        {
            var leap = data.drangueLeap;
            Spend(leap.bookId, leap.spiritCost, leap.cooldown, now);
        }

        /// <summary>Seconds until a form (by form id) or the leap (by its book id) is ready again.</summary>
        public float CooldownLeft(string key, float now) =>
            readyAt.TryGetValue(key, out var at) ? Math.Max(0f, at - now) : 0f;

        public void Regenerate(float seconds) => Spirit = Math.Min(MaxSpirit, Spirit + SpiritRegenPerSecond * seconds);

        public void RestoreSpirit() => Spirit = MaxSpirit;

        /// <summary>Adds Spirit (a potion), capped at max.</summary>
        public void AddSpirit(float amount)
        {
            if (amount > 0f) Spirit = Math.Min(MaxSpirit, Spirit + amount);
        }

        /// <summary>Gear changed max Spirit; current Spirit is kept but capped at the new max.</summary>
        public void SetMaxSpirit(int maxSpirit)
        {
            MaxSpirit = Math.Max(0, maxSpirit);
            Spirit = Math.Min(Spirit, MaxSpirit);
        }

        void Spend(string key, int spirit, float cooldown, float now)
        {
            Spirit = Math.Max(0f, Spirit - spirit);
            readyAt[key] = now + cooldown;
        }
    }
}
