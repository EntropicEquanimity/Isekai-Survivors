using System;
using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.FlowFields;
using Blondie.PixelPerfect;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The phase 2 slice in the real Region scene: the Character fights by itself, mines, and the camera zooms.</summary>
    public class RegionSliceTests
    {
        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            SceneManager.LoadScene("Region");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Idle_Character_walks_to_and_kills_a_nearby_enemy()
        {
            // A small fight of our own: no horde, two enemies inside the leash.
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            spawner.enabled = false;
            Object.FindAnyObjectByType<FlowFieldCrowd>().Speed = 0f;   // they stand still, so the Character must walk
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude)) Pools.Despawn(m.gameObject);
            Damageable[] enemies =
            {
                Pools.Spawn(spawner.Prefab, new Vector3(-4.5f, 1.5f, 0f), Quaternion.identity).GetComponent<Damageable>(),
                Pools.Spawn(spawner.Prefab, new Vector3(-5f, 2f, 0f), Quaternion.identity).GetComponent<Damageable>(),
            };
            var control = Object.FindAnyObjectByType<CharacterControl>();
            Vector3 start = control.transform.position;

            yield return WaitFor(() => control.IsAutoFighting, 5f, "the Character takes over after the idle delay");
            yield return WaitFor(() => enemies.Any(e => e == null || !e.isActiveAndEnabled || e.Unit.IsDead), 20f, "an enemy dies");
            Assert.Greater(Vector3.Distance(start, control.transform.position), 0.5f, "it walked to reach the enemy");
        }

        [UnityTest]
        public IEnumerator Mining_a_node_fills_the_stockpile()
        {
            var miner = Object.FindAnyObjectByType<Miner>();
            miner.enabled = false;   // its Update would stop mining every frame: Interact isn't held in a test
            ResourceNode node = ResourceNode.All.First();
            int before = node.Remaining;
            Stockpile pile = RegionEconomy.Current.Stockpile;
            int start = pile.Get(node.Type);

            int mined = 0;
            for (int i = 0; i < 12; i++)
            {
                mined += miner.Mine(node, 0.25f);   // 3 s at MiningSpeed 2
                yield return null;
            }

            Assert.AreEqual(6, mined);
            Assert.AreEqual(start + 6, pile.Get(node.Type));
            Assert.AreEqual(before - 6, node.Remaining);
        }

        [UnityTest]
        public IEnumerator Zoom_changes_the_pixel_cameras_view_in_whole_steps()
        {
            var zoom = Object.FindAnyObjectByType<ScrollZoom>();
            PixelCamera cam = zoom.GetComponent<PixelCamera>();
            int startScale = zoom.Levels.EffectiveScale(cam.OutputSize);

            zoom.Zoom(+1);
            yield return null;
            Assert.AreEqual(zoom.Levels.ViewFor(cam.OutputSize), cam.ReferenceResolution);
            Assert.GreaterOrEqual(zoom.Levels.EffectiveScale(cam.OutputSize), startScale, "zooming in never shows more");

            zoom.Zoom(-2);
            yield return null;
            Assert.AreEqual(zoom.Levels.ViewFor(cam.OutputSize), cam.ReferenceResolution);
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
