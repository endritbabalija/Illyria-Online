using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wuxia.Data;

namespace Wuxia.Tests
{
    /// <summary>Sanity checks on the shared combat.json so a bad edit fails here, not in Play mode.</summary>
    public class CombatDataTests
    {
        const string CombatJsonPath = "Packages/com.wuxia.gamedata/combat.json";

        static CombatData Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CombatJsonPath);
            Assert.That(asset, Is.Not.Null, $"{CombatJsonPath} not found");
            return CombatData.FromJson(asset.text);
        }

        static void AssertValid(AttackData a, string who)
        {
            Assert.That(a.minDamage, Is.GreaterThan(0), who);
            Assert.That(a.maxDamage, Is.GreaterThanOrEqualTo(a.minDamage), who);
            Assert.That(a.interval, Is.GreaterThan(0f), who);
            Assert.That(a.range, Is.GreaterThan(0f), who);
            Assert.That(a.accuracy, Is.InRange(0f, 1.5f), who);
            Assert.That(a.critChance, Is.InRange(0f, 1f), who);
            Assert.That(a.critMultiplier, Is.GreaterThanOrEqualTo(1f), who);
        }

        [Test]
        public void Player_IsValid()
        {
            var p = Load().player;
            Assert.That(p.maxHp, Is.GreaterThan(0));
            Assert.That(p.moveSpeed, Is.GreaterThan(0f));
            Assert.That(p.respawnDelay, Is.GreaterThanOrEqualTo(0f));
            AssertValid(p.attack, "player");
        }

        [Test]
        public void Wolf_IsValid_AndPlayerCanOutrunIt()
        {
            var data = Load();
            var wolf = data.GetMonster("wolf");
            Assert.That(wolf.maxHp, Is.GreaterThan(0));
            Assert.That(wolf.leashRange, Is.GreaterThan(wolf.aggroRange));
            Assert.That(wolf.chaseSpeed, Is.LessThan(data.player.moveSpeed), "the player must be able to escape a chase");
            AssertValid(wolf.attack, "wolf");
        }

        [Test]
        public void MonsterIds_AreUnique()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var m in Load().monsters)
                Assert.That(ids.Add(m.id), $"duplicate monster id '{m.id}'");
        }
    }
}
