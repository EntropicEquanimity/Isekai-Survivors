using System.Collections;
using System.Linq;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    public class UnitAnimationPlayModeTests
    {
        private RegionGrid _grid;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region is ready");
            _grid = RegionGrid.Current;
            _jack = Player.Character;
            TestDebug.Toggle("God mode", true);
        }

        private Vector3 FreeSpotNear(Vector3 from, float distance)
        {
            for (int i = 0; i < 64; i++)
            {
                float angle = i * 0.61f;
                Vector3 spot = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (distance + i * 0.05f);
                if (_grid.IsWalkable(spot)) return spot;
            }

            Assert.Fail("no free ground near " + from);
            return from;
        }

        private HordeMember SpawnSoldier()
        {
            HordeSpawner spawner = Object.FindAnyObjectByType<HordeSpawner>();
            Assert.IsNotNull(spawner, "horde spawner exists");
            Vector3 at = FreeSpotNear(_jack.transform.position, 28f);
            HordeMember member = Pools.Spawn(spawner.Prefab, at, Quaternion.identity).GetComponent<HordeMember>();
            Assert.IsNotNull(member, "a soldier spawns");
            member.SetRoaming(true);
            return member;
        }

        [UnityTest]
        public IEnumerator A_walking_unit_cycles_walk_frames()
        {
            HordeMember member = SpawnSoldier();
            SpriteRenderer renderer = member.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);

            Sprite first = renderer.sprite;
            bool changed = false;
            float end = Time.realtimeSinceStartup + 1.1f;
            while (Time.realtimeSinceStartup < end)
            {
                member.transform.position += Vector3.right * 0.08f;
                yield return null;
                if (renderer.sprite != first) changed = true;
            }

            Assert.IsTrue(changed, "the walk flipbook changes while moving");
        }

        [UnityTest]
        public IEnumerator A_standing_unit_cycles_idle_frames()
        {
            HordeMember member = SpawnSoldier();
            SpriteRenderer renderer = member.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);

            yield return new WaitForSeconds(0.2f);
            Sprite first = renderer.sprite;
            bool changed = false;
            float end = Time.realtimeSinceStartup + 1.3f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (renderer.sprite != first) changed = true;
            }

            Assert.IsTrue(changed, "the idle flipbook changes while standing still");
        }

        [UnityTest]
        public IEnumerator A_dead_unit_plays_a_death_clip_before_the_corpse_disappears()
        {
            HordeMember member = SpawnSoldier();
            member.Damageable.Kill();

            yield return TestWait.Until(() => Object.FindObjectsByType<UnitAnimationCorpse>(FindObjectsInactive.Exclude).Any(c => c.IsPlaying), "death corpse appears", 2f);
            UnitAnimationCorpse corpse = Object.FindObjectsByType<UnitAnimationCorpse>(FindObjectsInactive.Exclude).First(c => c.IsPlaying);
            Sprite first = corpse.CurrentSprite;

            yield return TestWait.Until(() => corpse != null && corpse.IsPlaying && corpse.CurrentSprite != null && corpse.CurrentSprite != first, "death frame advances", 2f);
            yield return TestWait.Until(() => corpse == null || !corpse.IsPlaying, "corpse finishes", 3f);
        }
    }
}
