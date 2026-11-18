using System;
using System.Collections;
using System.Linq;
using Blondie.Abilities;
using Blondie.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Phase 5 in the real Region scene: roaming packs, Waves and capture.</summary>
    public class WavePlayModeTests
    {
        private CharacterControl _character;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            SceneManager.LoadScene("Region");
            yield return null;
            yield return null;
            yield return null;   // nests have spawned their packs
            _character = Object.FindAnyObjectByType<CharacterControl>();
            _character.enabled = false;
            _character.GetComponent<AutoCaster>().enabled = false;   // the tests do the killing
        }

        private static HordeMember[] Roamers() =>
            Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsRoaming && m.IsAlive).ToArray();

        [UnityTest]
        public IEnumerator Nest_packs_roam_in_place_until_a_target_comes_near()
        {
            HordeMember[] roamers = Roamers();
            Assert.That(roamers.Length, Is.GreaterThanOrEqualTo(3 * 25), "three nests, 25 each");
            HordeMember one = roamers[0];
            Vector3 start = one.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(one.IsRoaming);
            Assert.Less(Vector3.Distance(start, one.transform.position), 0.01f, "roamers stand still");

            _character.transform.position = one.transform.position + new Vector3(3f, 0f, 0f);
            yield return WaitFor(() => !one.IsRoaming, 2f, "a target in aggro range wakes it");
        }

        [UnityTest]
        public IEnumerator Hurting_a_roamer_wakes_the_pack_around_it()
        {
            HordeMember hurt = Roamers()[0];
            HordeMember[] neighbours = Roamers().Where(m => m != hurt && Vector3.Distance(m.transform.position, hurt.transform.position) < 3f).ToArray();
            Assume.That(neighbours.Length, Is.GreaterThan(0));
            hurt.Unit.TakeDamage(1f);
            yield return null;
            Assert.IsFalse(hurt.IsRoaming);
            Assert.IsTrue(neighbours.All(n => !n.IsRoaming), "its neighbours wake too");
        }

        [UnityTest]
        public IEnumerator A_due_Wave_spawns_attacking_enemies_at_the_edges()
        {
            var waves = Object.FindAnyObjectByType<WaveDirector>();
            HordeDirector horde = HordeDirector.Current;
            int before = horde.Attacking;
            waves.Skip(waves.SecondsUntilNext + 0.01f);
            yield return null;
            yield return null;
            Assert.AreEqual(1, waves.Released);
            Assert.Greater(horde.Attacking, before + 10, "the first Wave attacks at once");
        }

        [UnityTest]
        public IEnumerator The_Region_is_captured_when_every_nest_falls_and_the_final_Wave_is_beaten()
        {
            RegionGoal goal = RegionGoal.Current;
            var waves = Object.FindAnyObjectByType<WaveDirector>();
            foreach (EnemyNest nest in EnemyNest.All.ToArray()) nest.GetComponent<Damageable>().Unit.TakeDamage(100000f, DamageRules.TrueType);
            yield return null;
            Assert.IsTrue(goal.Capture.EnemyBasesCleared);
            Assert.IsFalse(goal.Capture.IsCaptured, "the final Wave hasn't come yet");

            waves.Skip(1000f);
            yield return WaitFor(() => goal.Capture.FinalWaveReleased, 2f, "the final Wave is released");
            yield return WaitFor(() => waves.PendingSpawns == 0, 3f, "every Wave's enemies have spawned");
            Assert.IsFalse(goal.Capture.IsCaptured);
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude))
                if (m.IsAlive && !m.IsRoaming) m.Unit.TakeDamage(100000f, DamageRules.TrueType);
            yield return WaitFor(() => goal.Capture.IsCaptured, 2f, "captured");
            yield return null;
            Assert.IsFalse(waves.enabled, "no more Waves after capture");
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds, string what)
        {
            float end = Time.time + seconds;
            while (!condition())
            {
                if (Time.time > end) Assert.Fail("Timed out: " + what);
                yield return null;
            }
        }
    }
}
