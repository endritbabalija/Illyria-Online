using System;
using System.Collections.Generic;
using Wuxia.Data;

namespace Wuxia.Combat
{
    /// <summary>
    /// Active status effects on one combatant (poison, paralysis, stun, knockdown). One instance per
    /// status: landing it again refreshes the duration rather than stacking. Pure C# with time passed in.
    /// </summary>
    public sealed class StatusSet
    {
        struct Active
        {
            public StatusData Data;
            public float EndTime;
            public float NextTick;
            public int TickDamage;
            public object Source;
        }

        readonly List<Active> active = new List<Active>();

        /// <summary>Bumped whenever a status starts or ends, so UI knows to refresh.</summary>
        public int Version { get; private set; }
        public int Count => active.Count;

        public StatusData Get(int index) => active[index].Data;

        public float Remaining(int index, float now) => Math.Max(0f, active[index].EndTime - now);

        public bool Has(string id, float now)
        {
            foreach (var a in active)
                if (a.Data.id == id && a.EndTime > now) return true;
            return false;
        }

        /// <summary>True while any disabling status (paralysis, stun, knockdown) is running.</summary>
        public bool IsDisabled(float now)
        {
            foreach (var a in active)
                if (a.Data.disables && a.EndTime > now) return true;
            return false;
        }

        /// <param name="source">Whoever applied it; damage ticks are credited to them.</param>
        public void Apply(StatusData data, float duration, int tickDamage, object source, float now)
        {
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i].Data.id != data.id) continue;
                var refreshed = active[i];
                refreshed.EndTime = Math.Max(refreshed.EndTime, now + duration);
                refreshed.TickDamage = Math.Max(refreshed.TickDamage, tickDamage);
                refreshed.Source = source;
                active[i] = refreshed;
                return;
            }

            active.Add(new Active
            {
                Data = data,
                EndTime = now + duration,
                NextTick = data.tickInterval > 0f ? now + data.tickInterval : float.PositiveInfinity,
                TickDamage = tickDamage,
                Source = source,
            });
            Version++;
        }

        /// <summary>
        /// Advances to <paramref name="now"/>: returns damage from every tick that came due (a tick
        /// landing exactly as the status ends still counts) and drops expired statuses.
        /// </summary>
        /// <param name="source">Who applied the status that dealt the damage, if any.</param>
        public int Update(float now, out object source)
        {
            source = null;
            var damage = 0;
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var a = active[i];
                if (a.Data.tickInterval > 0f)
                {
                    while (a.NextTick <= now && a.NextTick <= a.EndTime)
                    {
                        damage += a.TickDamage;
                        source = a.Source;
                        a.NextTick += a.Data.tickInterval;
                    }
                    active[i] = a;
                }

                if (a.EndTime <= now)
                {
                    active.RemoveAt(i);
                    Version++;
                }
            }
            return damage;
        }

        public void Clear()
        {
            if (active.Count == 0) return;
            active.Clear();
            Version++;
        }
    }
}
