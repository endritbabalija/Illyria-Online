using NUnit.Framework;
using Wuxia.Combat;
using Wuxia.Data;

namespace Wuxia.Tests
{
    public class SkillStateTests
    {
        static SkillData data;
        static BookData Sword => data.GetBook("sword_book");

        [OneTimeSetUp]
        public void LoadData() => data = SkillDataTests.Load();

        static SkillState NewState(int points = 0)
        {
            var state = new SkillState(data, 100, 2f);
            state.AddSkillPoints(points);
            return state;
        }

        [Test]
        public void LearnedBook_StartsAtLevel0_WithEveryFormLocked()
        {
            var state = NewState();
            Assert.That(state.BookLevel("sword_book"), Is.EqualTo(-1));
            Assert.That(state.Learn("sword_book"));
            Assert.That(state.Learn("sword_book"), Is.False, "learning twice does nothing");
            Assert.That(state.BookLevel("sword_book"), Is.Zero);
            Assert.That(state.CheckCast(Sword, Sword.forms[0], 0f), Is.EqualTo(CastBlock.Locked));
        }

        [Test]
        public void Forms_UnlockAtLevels1_3_6()
        {
            var state = NewState(points: 140);
            state.Learn("sword_book");
            var forms = Sword.forms;
            for (var level = 1; level <= 6; level++)
            {
                Assert.That(state.LevelUp("sword_book"));
                Assert.That(state.IsUnlocked(Sword, forms[0]), Is.True, $"level {level}");
                Assert.That(state.IsUnlocked(Sword, forms[1]), Is.EqualTo(level >= 3), $"level {level}");
                Assert.That(state.IsUnlocked(Sword, forms[2]), Is.EqualTo(level >= 6), $"level {level}");
            }
        }

        [Test]
        public void MaxingABook_Costs140Points()
        {
            var state = NewState(points: 140);
            state.Learn("sword_book");
            for (var i = 0; i < 12; i++) Assert.That(state.LevelUp("sword_book"), $"level {i + 1}");
            Assert.That(state.BookLevel("sword_book"), Is.EqualTo(12));
            Assert.That(state.SkillPoints, Is.Zero);
            Assert.That(state.CheckLevelUp("sword_book", out _), Is.EqualTo(LevelUpBlock.MaxLevel));
        }

        [Test]
        public void LevelUp_NeedsEnoughPoints()
        {
            var state = NewState(points: 3);
            state.Learn("sword_book");
            for (var i = 0; i < 3; i++) state.LevelUp("sword_book");
            Assert.That(state.CheckLevelUp("sword_book", out var cost), Is.EqualTo(LevelUpBlock.NotEnoughPoints));
            Assert.That(cost, Is.EqualTo(2));
            Assert.That(state.LevelUp("sword_book"), Is.False);
            Assert.That(state.CheckLevelUp("bow_book", out _), Is.EqualTo(LevelUpBlock.NotLearned));
        }

        [Test]
        public void KillMeter_PaysAPoint_EveryKillsPerPoint()
        {
            var state = NewState();
            var per = data.skillPoints.killsPerPoint;
            for (var i = 1; i < per; i++) Assert.That(state.AddKill(), Is.False);
            Assert.That(state.AddKill(), Is.True);
            Assert.That(state.SkillPoints, Is.EqualTo(1));
            Assert.That(state.MeterKills, Is.Zero);
        }

        [Test]
        public void Casting_SpendsSpirit_AndStartsTheCooldown()
        {
            var state = NewState(points: 1);
            state.Learn("sword_book");
            state.LevelUp("sword_book");
            var form = Sword.forms[0];

            Assert.That(state.CheckCast(Sword, form, 0f), Is.EqualTo(CastBlock.None));
            state.CommitCast(form, 10f);
            Assert.That(state.Spirit, Is.EqualTo(100 - form.spiritCost));
            Assert.That(state.CheckCast(Sword, form, 10f + form.cooldown - 0.01f), Is.EqualTo(CastBlock.Cooldown));
            Assert.That(state.CheckCast(Sword, form, 10f + form.cooldown), Is.EqualTo(CastBlock.None));
        }

        [Test]
        public void Casting_NeedsSpirit_WhichRegeneratesSlowly()
        {
            var state = NewState(points: 1);
            state.Learn("sword_book");
            state.LevelUp("sword_book");
            var form = Sword.forms[0];
            var now = 0f;
            while (state.CheckCast(Sword, form, now) == CastBlock.None)
            {
                state.CommitCast(form, now);
                now += form.cooldown;
            }
            Assert.That(state.CheckCast(Sword, form, now), Is.EqualTo(CastBlock.NoSpirit));
            state.Regenerate(form.spiritCost / state.SpiritRegenPerSecond);
            Assert.That(state.CheckCast(Sword, form, now), Is.EqualTo(CastBlock.None));
            state.Regenerate(1000f);
            Assert.That(state.Spirit, Is.EqualTo(state.MaxSpirit), "regen stops at max");
        }

        [Test]
        public void Leap_NeedsTheBook_SpiritAndCooldown()
        {
            var state = NewState();
            var leap = data.drangueLeap;
            Assert.That(state.CheckLeap(0f), Is.EqualTo(CastBlock.NotLearned));
            state.Learn(leap.bookId);
            Assert.That(state.CheckLeap(0f), Is.EqualTo(CastBlock.None));
            state.CommitLeap(0f);
            Assert.That(state.CheckLeap(leap.cooldown - 0.01f), Is.EqualTo(CastBlock.Cooldown));
            Assert.That(state.Spirit, Is.EqualTo(100 - leap.spiritCost));
        }

        [Test]
        public void SkillDamage_GrowsWithBookLevel_AndAlwaysDealsAtLeast1()
        {
            var form = Sword.forms[0];
            var rng = new System.Random(5);
            var perLevel = data.skillPoints.damagePerBookLevel;
            for (var i = 0; i < 500; i++)
            {
                Assert.That(SkillMath.RollDamage(form, 1, perLevel, 0, rng), Is.InRange(form.minDamage, form.maxDamage));
                var max = SkillMath.RollDamage(form, 12, perLevel, 0, rng);
                Assert.That(max, Is.InRange((int)(form.minDamage * (1 + 11 * perLevel)) - 1, (int)(form.maxDamage * (1 + 11 * perLevel)) + 1));
                Assert.That(SkillMath.RollDamage(form, 1, perLevel, 10000, rng), Is.EqualTo(1));
            }
        }
    }
}
