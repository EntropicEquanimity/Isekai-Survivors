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
    /// <summary>Roaming groups of the wilds in the real Region scene.</summary>
    public class RoamingPlayModeTests
    {
        private CharacterControl _character;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => Object.FindAnyObjectByType<RegionWilds>().GroupCount > 0, "the wilds fill in", 3f);
            _character = Object.FindAnyObjectByType<CharacterControl>();
            _character.enabled = false;
            _character.GetComponent<AutoCaster>().enabled = false;   // the tests do the killing
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Blondie.Roguelike.Drafts.RunDrafts.ClearQueue();
            Time.timeScale = 1f;
            yield return null;
        }

        private static HordeMember[] Roamers() =>
            Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsRoaming && m.IsAlive).ToArray();

        [UnityTest]
        public IEnumerator Wild_groups_roam_in_place_until_a_target_comes_near()
        {
            HordeMember[] roamers = Roamers();
            Assert.That(roamers.Length, Is.GreaterThan(0), "the wilds have roaming groups");
            HordeMember one = roamers[0];
            Vector3 start = one.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(one.IsRoaming);
            Assert.Less(Vector3.Distance(start, one.transform.position), 0.01f, "roamers stand still");

            _character.transform.position = one.transform.position + new Vector3(3f, 0f, 0f);
            yield return TestWait.Until(() => !one.IsRoaming, "a target in aggro range wakes it", 2f);
        }

        [UnityTest]
        public IEnumerator Hurting_a_roamer_wakes_the_group_around_it()
        {
            HordeMember hurt = Roamers()[0];
            HordeMember[] neighbours = Roamers().Where(m => m != hurt && Vector3.Distance(m.transform.position, hurt.transform.position) < 3f).ToArray();
            Assume.That(neighbours.Length, Is.GreaterThan(0));
            hurt.Unit.TakeDamage(1f);
            yield return null;
            Assert.IsFalse(hurt.IsRoaming);
            Assert.IsTrue(neighbours.All(n => !n.IsRoaming), "its neighbours wake too");
        }
    }
}
