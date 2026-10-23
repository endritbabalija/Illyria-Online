using NUnit.Framework;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.Tests
{
    public class StatusSetTests
    {
        static readonly StatusData Poison = new StatusData { id = "poison", name = "Poisoned", tickInterval = 1f, resist = "poison" };
        static readonly StatusData Stun = new StatusData { id = "stun", name = "Stunned", disables = true, resist = "stun" };
        static readonly StatusData Paralysis = new StatusData { id = "paralysis", name = "Paralysed", disables = true, resist = "paralysis" };

        [Test]
        public void Poison_TicksOncePerInterval_ForItsDuration()
        {
            var set = new StatusSet();
            var source = new object();
            set.Apply(Poison, 6f, 5, source, 0f);

            var total = 0;
            for (var t = 0.1f; t <= 10f; t += 0.1f)
            {
                total += set.Update(t, out var from);
                if (from != null) Assert.That(from, Is.SameAs(source));
            }
            Assert.That(total, Is.EqualTo(30), "6 ticks of 5");
            Assert.That(set.Count, Is.Zero, "expired");
            Assert.That(set.IsDisabled(1f), Is.False, "poison doesn't stop you acting");
        }

        [Test]
        public void Poison_CatchesUpOnALongFrame()
        {
            var set = new StatusSet();
            set.Apply(Poison, 6f, 5, null, 0f);
            Assert.That(set.Update(3.5f, out _), Is.EqualTo(15));
        }

        [Test]
        public void StunAndParalysis_Disable_UntilTheyEnd()
        {
            var set = new StatusSet();
            set.Apply(Stun, 2f, 0, null, 0f);
            Assert.That(set.IsDisabled(1.99f));
            Assert.That(set.IsDisabled(2f), Is.False);

            set.Apply(Paralysis, 1f, 0, null, 5f);
            Assert.That(set.IsDisabled(5.5f));
            set.Update(6f, out _);
            Assert.That(set.Count, Is.Zero);
        }

        [Test]
        public void Reapplying_Refreshes_InsteadOfStacking()
        {
            var set = new StatusSet();
            set.Apply(Stun, 2f, 0, null, 0f);
            set.Apply(Stun, 2f, 0, null, 1.5f);
            Assert.That(set.Count, Is.EqualTo(1));
            Assert.That(set.IsDisabled(3.4f), "refreshed to 3.5");

            set.Apply(Stun, 0.5f, 0, null, 2f);
            Assert.That(set.IsDisabled(3.4f), "a shorter stun never cuts a longer one short");
        }

        [Test]
        public void Resistance_CutsTheChance()
        {
            Assert.That(SkillMath.StatusChance(0.6f, 0f), Is.EqualTo(0.6f).Within(1e-5));
            Assert.That(SkillMath.StatusChance(0.6f, 0.5f), Is.EqualTo(0.3f).Within(1e-5));
            Assert.That(SkillMath.StatusChance(1f, 1f), Is.Zero);

            var rng = new System.Random(11);
            var landed = 0;
            for (var i = 0; i < 20000; i++) if (SkillMath.StatusLands(0.6f, 0.5f, rng)) landed++;
            Assert.That(landed / 20000f, Is.EqualTo(0.3f).Within(0.015f));
        }
    }
}
