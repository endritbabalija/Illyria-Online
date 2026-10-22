using NUnit.Framework;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.Tests
{
    public class CombatMathTests
    {
        static AttackData Attack(float accuracy = 0.9f, float critChance = 0.1f) => new AttackData
        {
            minDamage = 10,
            maxDamage = 20,
            interval = 1f,
            range = 2f,
            accuracy = accuracy,
            critChance = critChance,
            critMultiplier = 2f,
        };

        [Test]
        public void HitChance_IsAccuracyMinusDodge_Clamped()
        {
            Assert.That(CombatMath.HitChance(0.9f, 0.1f), Is.EqualTo(0.8f).Within(1e-5));
            Assert.That(CombatMath.HitChance(0.1f, 0.5f), Is.EqualTo(CombatMath.MinHitChance));
            Assert.That(CombatMath.HitChance(1.5f, 0f), Is.EqualTo(CombatMath.MaxHitChance));
        }

        [Test]
        public void Rolls_MatchHitAndCritRates()
        {
            var rng = new System.Random(1234);
            var attack = Attack(accuracy: 0.85f, critChance: 0.2f);
            const int n = 20000;
            int hits = 0, crits = 0;
            for (var i = 0; i < n; i++)
            {
                var r = CombatMath.RollBasicAttack(attack, 0, 0.05f, rng);
                if (r.Hit) hits++;
                if (r.Crit) crits++;
            }
            Assert.That(hits / (float)n, Is.EqualTo(0.80f).Within(0.02f));
            Assert.That(crits / (float)hits, Is.EqualTo(0.20f).Within(0.02f));
        }

        [Test]
        public void Damage_StaysInRange_AndDefenseNeverZeroesAHit()
        {
            var rng = new System.Random(42);
            var attack = Attack(accuracy: 1f, critChance: 0f);
            for (var i = 0; i < 2000; i++)
            {
                var r = CombatMath.RollBasicAttack(attack, 5, 0f, rng);
                if (r.Hit) Assert.That(r.Damage, Is.InRange(5, 15));

                var tanky = CombatMath.RollBasicAttack(attack, 1000, 0f, rng);
                if (tanky.Hit) Assert.That(tanky.Damage, Is.EqualTo(1));
            }
        }

        [Test]
        public void Crit_MultipliesDamage()
        {
            var rng = new System.Random(7);
            var attack = Attack(accuracy: 1f, critChance: 1f);
            for (var i = 0; i < 500; i++)
            {
                var r = CombatMath.RollBasicAttack(attack, 0, 0f, rng);
                if (!r.Hit) continue; // even perfect accuracy is capped at MaxHitChance
                Assert.That(r.Crit);
                Assert.That(r.Damage, Is.InRange(20, 40));
            }
        }

        [Test]
        public void Miss_DealsNoDamage()
        {
            var rng = new System.Random(3);
            var attack = Attack(accuracy: 0f);
            var misses = 0;
            for (var i = 0; i < 1000; i++)
            {
                var r = CombatMath.RollBasicAttack(attack, 0, 1f, rng);
                if (!r.Hit) { misses++; Assert.That(r.Damage, Is.Zero); }
            }
            Assert.That(misses, Is.GreaterThan(900));
        }
    }
}
