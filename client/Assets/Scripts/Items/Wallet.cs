using System;

namespace Wuxia.Items
{
    /// <summary>
    /// A character's gold. Can't go negative or overflow. Change it through <see cref="PlayerItems"/>
    /// so every change is a ledger entry; this class only enforces the rules.
    /// </summary>
    public sealed class Wallet
    {
        public int Gold { get; private set; }

        public Wallet(int gold = 0)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            Gold = gold;
        }

        public bool CanAdd(int amount) => amount >= 0 && (long)Gold + amount <= int.MaxValue;

        public bool CanSpend(int amount) => amount >= 0 && amount <= Gold;

        /// <summary>False (no change) for a negative amount or if it would overflow.</summary>
        public bool Add(int amount)
        {
            if (!CanAdd(amount)) return false;
            Gold += amount;
            return true;
        }

        /// <summary>False (no change) for a negative amount or when there isn't enough gold.</summary>
        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount)) return false;
            Gold -= amount;
            return true;
        }
    }
}
