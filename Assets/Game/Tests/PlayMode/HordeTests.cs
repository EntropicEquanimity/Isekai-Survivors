using System;
using System.Collections;
using System.Linq;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The horde in the real Region scene.</summary>
    public class HordePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            SceneManager.LoadScene("Region");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawner_fills_the_horde_and_abilities_find_members_through_the_hash()
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            HordeDirector director = HordeDirector.Current;
            yield return TestWait.Until(() => director.Count >= spawner.Count, "the horde fills up", 10f);
            Assert.IsInstanceOf<HordeWorld>(AbilityWorld.Current, "the director installs its world");

            yield return null;   // one LateUpdate to rebuild the hash
            HordeMember any = Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).First();
            var found = new System.Collections.Generic.List<CombatUnit>();
            Vector3 p = any.transform.position;
            AbilityWorld.Current.FindUnits(new Blondie.Core.Float3(p.x, p.y, 0f), 0.5f, true, found);
            Assert.That(found, Has.Member(any.Unit), "a member is found at its own position without a collider");
        }

        [UnityTest]
        public IEnumerator Mortals_that_reach_a_target_hurt_it()
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            spawner.enabled = false;
            var character = Object.FindAnyObjectByType<CharacterControl>();
            character.enabled = false;
            character.GetComponent<AutoCaster>().enabled = false;   // nothing kills the test enemies

            HordeTarget mainBase = HordeTarget.All.First(t => t.name.StartsWith("Main Base"));
            float max = mainBase.Damageable.Unit.Health;
            for (int i = 0; i < 3; i++)
                Pools.Spawn(spawner.Prefab, mainBase.transform.position + new Vector3(i - 1f, -2.5f, 0f), Quaternion.identity);

            yield return TestWait.Until(() => mainBase.Damageable.Unit.Health < max, "the base takes contact damage", 10f);
        }

    }
}
