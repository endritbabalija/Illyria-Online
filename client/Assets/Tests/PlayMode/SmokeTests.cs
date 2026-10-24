using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wuxia.Monsters;
using Wuxia.Net;
using Wuxia.Player;
using Wuxia.UI;
using Wuxia.World;
using Object = UnityEngine.Object;

namespace Wuxia.Tests
{
    /// <summary>
    /// Smoke tests: the real Oak Valley scene in Play mode, driven like a player would. Any error or
    /// exception logged meanwhile fails the test (the test framework's LogAssert), so a pass also means
    /// a clean Console. Offline: kill a Grey Wolf and auto-loot its drops. Online (needs the dev server;
    /// skipped without one): the smoke tests' own account locks a monster and the server's hit lands.
    /// </summary>
    public class SmokeTests
    {
        const string Scene = "OakValley";
        const string WolfId = "wolf";

        static float Now => Time.realtimeSinceStartup;

        [TearDown]
        public void TearDown()
        {
            NetZone.ConnectOnStartOverride = null;
            NetZone.DeviceIdSuffixOverride = null;
        }

        [UnityTest]
        public IEnumerator Offline_KillAWolf_ItsLootIsPickedUp()
        {
            NetZone.ConnectOnStartOverride = false;
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return WaitFor(() => MonsterAI.Alive.Count > 0, 10f, "the spawners");
            var player = Object.FindFirstObjectByType<PlayerCombat>();
            var loot = Object.FindFirstObjectByType<GroundLoot>();
            var session = ZoneSession.Instance;
            // Keeps the pack from making the test flaky: this checks the loop, not the balance.
            Action keepAlive = () => { if (player.Combatant.HpFraction < 0.5f) player.Combatant.RestoreFull(); };

            // Gold drops 90% of the time and items rarely, so a wolf can drop nothing: try up to three.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var wolf = Nearest(player.transform.position, WolfId);
                Assert.IsNotNull(wolf, "No Grey Wolf alive");
                var kills = session.Kills;
                var to = wolf.transform.position - player.transform.position;
                to.y = 0f;
                player.Mover.Teleport(wolf.transform.position - to.normalized * 2f, Quaternion.LookRotation(to));
                player.Engage(wolf.Combatant);
                yield return WaitFor(() => wolf.Combatant.IsDead, 30f, $"{wolf.name} to die", keepAlive);
                Assert.AreEqual(kills + 1, session.Kills, "The kill wasn't counted");

                yield return null;
                if (loot.ActiveCount == 0) continue;
                var gold = player.Items.Gold;
                var version = player.Items.Version;
                player.Mover.Teleport(wolf.transform.position, player.transform.rotation);
                yield return WaitFor(() => loot.ActiveCount == 0, 10f, "auto-loot to pick up the drops", keepAlive);
                Assert.IsTrue(player.Items.Gold > gold || player.Items.Version != version, "The drops vanished without reaching the wallet or the bag");
                yield break;
            }
            Assert.Inconclusive("Three wolves in a row dropped nothing");
        }

        [UnityTest]
        public IEnumerator Online_LockAMonster_TheServerHitsIt()
        {
            NetZone.ConnectOnStartOverride = true;
            NetZone.DeviceIdSuffixOverride = "smoketest";
            yield return SceneManager.LoadSceneAsync(Scene);
            var select = Object.FindFirstObjectByType<CharacterSelect>(FindObjectsInactive.Include);
            yield return WaitFor(() => select.IsOpen || !NetZone.WillConnect, 20f, "the server or the offline fallback");
            if (!select.IsOpen) Assert.Ignore("No Nakama server: docker compose -f server/docker-compose.yml up -d --build");

            // The name only matters the first time; afterwards the account's Kreshnik is played.
            select.Pick("kreshnik", "Smoke Kreshnik");
            yield return WaitFor(() => NetZone.Instance.InMatch, 30f, "joining an Oak Valley channel");
            var player = Object.FindFirstObjectByType<PlayerCombat>();

            // The account's hero keeps its saved spot, HP and potions between runs, so it may die (and
            // revive at the Han) on the way; keep trying until a hit lands.
            var until = Now + 120f;
            var nextPotion = 0f;
            while (true)
            {
                if (Now > until) Assert.Fail("No server hit on a locked monster within 120 s");
                var monster = player.IsDead ? null : Nearest(player.transform.position);
                if (monster == null)
                {
                    yield return null;
                    continue;
                }
                if (Flat(player.transform.position, monster.transform.position) > 10f)
                {
                    player.Mover.RunTo(monster.transform.position, 8f, null);
                    var runUntil = Now + 20f;
                    while (Now < runUntil && !player.IsDead && !monster.Combatant.IsDead
                           && Flat(player.transform.position, monster.transform.position) > 10f)
                        yield return null;
                    continue;
                }

                var hp = monster.Combatant.Hp;
                player.Engage(monster.Combatant);
                var lockUntil = Now + 15f;
                while (Now < lockUntil && !player.IsDead && player.Target == monster.Combatant && monster.Combatant.Hp >= hp)
                {
                    if (player.Combatant.HpFraction < 0.45f && Now >= nextPotion)
                    {
                        player.UseQuickSlot(0);
                        nextPotion = Now + 1f;
                    }
                    yield return null;
                }
                if (monster.Combatant.Hp < hp)
                {
                    Debug.Log($"[Smoke] Server hit {monster.name}: {hp} → {monster.Combatant.Hp} HP");
                    yield break;
                }
            }
        }

        /// <summary>Waits (real time, so a slow unfocused Editor still gets its seconds) until <paramref name="done"/>.</summary>
        static IEnumerator WaitFor(Func<bool> done, float seconds, string what, Action eachFrame = null)
        {
            var until = Now + seconds;
            while (!done())
            {
                if (Now > until) Assert.Fail($"Timed out after {seconds:0} s waiting for {what}");
                eachFrame?.Invoke();
                yield return null;
            }
        }

        static MonsterAI Nearest(Vector3 from, string monsterId = null)
        {
            MonsterAI best = null;
            var bestDistance = float.MaxValue;
            foreach (var m in MonsterAI.Alive)
            {
                if (monsterId != null && m.MonsterId != monsterId) continue;
                var d = Flat(from, m.transform.position);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = m;
            }
            return best;
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
