using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The visual effects: cues play pooled effects that return to the pool, nothing plays out of sight, the pool cap holds in a horde fight, hits flash, Jack's hits shake the camera, the fog edge is soft.</summary>
    public class VfxPlayModeTests
    {
        private VfxPlayer _vfx;
        private CharacterControl _jack;
        private CombatUnit _jackUnit;
        private Vector3 _jackAt;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            TestDebug.Toggle("God mode", true);
            RegionTestScene.ClearHorde();
            yield return TestWait.Frames(3);
            _vfx = VfxPlayer.Current;
            Assert.IsNotNull(_vfx, "the VFX player is a Region system");
            _jack = Player.Character;
            _jackUnit = _jack.GetComponent<Damageable>().Unit;
            _jackAt = _jack.transform.position;
            yield return Settle();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // Waits until every effect has played out.
        private IEnumerator Settle() => TestWait.Until(() => _vfx.ActiveCount == 0, "the effects are over", 6f);

        private Vector3 Unseen()
        {
            RegionGrid grid = RegionGrid.Current;
            for (int x = 0; x < grid.Width; x += 3)
            for (int y = 0; y < grid.Height; y += 3)
            {
                var cell = new Int2(x, y);
                if (!RegionVision.Current.CanSee(Side.Yours, cell)) return grid.CentreOf(cell);
            }
            Assert.Fail("some ground is unseen");
            return default;
        }

        private HordeMember SpawnMortal(Vector3 at) =>
            Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, at, Quaternion.identity).GetComponent<HordeMember>();

        // ---- Cues play pooled effects ----

        [UnityTest]
        public IEnumerator A_death_cue_plays_a_pooled_burst_that_returns_to_the_pool()
        {
            Feedback.Raise(Cue.Death, _jackAt);
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.DeathSplat), "the burst plays");
            VfxSprite burst = _vfx.Live.First();
            Assert.IsTrue(burst.gameObject.activeSelf);
            Assert.AreEqual(VfxKind.DeathSplat, burst.Kind);

            yield return TestWait.Until(() => _vfx.ActiveCount == 0, "the burst is over", 4f);
            Assert.IsFalse(burst.gameObject.activeSelf, "it went back to the pool, switched off");
            Assert.AreEqual(0, _vfx.ActiveOf(VfxKind.DeathSplat));

            Feedback.Raise(Cue.Death, _jackAt);
            Assert.AreSame(burst, _vfx.Live.First(), "and the same sprite is used again: nothing was created");
        }

        [Test]
        public void Effects_are_drawn_on_the_16_pixel_grid_with_point_filtered_sprites()
        {
            Feedback.Raise(Cue.Summon, _jackAt + new Vector3(0.0123f, 0.0371f, 0f));
            VfxSprite fx = _vfx.Live.First();
            Vector3 p = fx.transform.position;
            Assert.AreEqual(0f, p.x * 16f - Mathf.Round(p.x * 16f), 1e-3f, "x on a whole pixel");
            Assert.AreEqual(0f, p.y * 16f - Mathf.Round(p.y * 16f), 1e-3f, "y on a whole pixel");
            Sprite sprite = fx.Renderer.sprite;
            Assert.AreEqual(16f, sprite.pixelsPerUnit, "16 pixels per unit");
            Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
            Assert.AreEqual(Vector3.one, fx.transform.localScale, "drawn at 1x");
        }

        [Test]
        public void A_cue_out_of_sight_plays_nothing()
        {
            Vector3 unseen = Unseen();
            int spawned = _vfx.Spawned;
            int dropped = _vfx.DroppedOutOfSight;
            Feedback.Raise(Cue.Death, unseen);
            Feedback.Raise(Cue.Summon, unseen);
            Feedback.Raise(Cue.Wave, unseen);
            Assert.AreEqual(spawned, _vfx.Spawned, "nothing started");
            Assert.AreEqual(0, _vfx.ActiveCount);
            Assert.Greater(_vfx.DroppedOutOfSight, dropped);

            Feedback.Raise(Cue.Death, _jackAt);
            Assert.AreEqual(1, _vfx.ActiveCount, "what Jack's side sees plays");
        }

        [Test]
        public void With_fog_of_war_off_everything_is_seen()
        {
            RegionVision.IsOn = false;
            try
            {
                Feedback.Raise(Cue.Death, Unseen());
                Assert.AreEqual(1, _vfx.ActiveCount);
            }
            finally { RegionVision.IsOn = true; }
        }

        [Test]
        public void Each_kind_has_its_own_cap_and_the_pool_a_hard_one()
        {
            for (int i = 0; i < 300; i++) Feedback.Raise(Cue.Death, _jackAt + new Vector3(i % 7, i % 5, 0f) * 0.1f);
            Assert.LessOrEqual(_vfx.ActiveOf(VfxKind.DeathSplat), VfxClips.Of(VfxKind.DeathSplat).Cap);
            for (int i = 0; i < 300; i++) Feedback.Raise(Cue.Summon, _jackAt);
            for (int i = 0; i < 300; i++) Feedback.Raise(Cue.CorruptionSpread, _jackAt);
            Assert.LessOrEqual(_vfx.ActiveCount, VfxPlayer.PoolSize);
            Assert.Greater(_vfx.DroppedByCap, 0, "the rest were dropped");
        }

        [UnityTest]
        public IEnumerator Two_hundred_deaths_in_one_frame_hold_the_pool_cap()
        {
            HordeDebug.SpawnNearCharacter(200);
            yield return null;
            var members = Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsAlive).ToArray();
            Assert.GreaterOrEqual(members.Length, 150, "a horde of mortals to kill");
            int spawned = _vfx.Spawned;
            foreach (HordeMember m in members) m.Damageable.Kill();

            Assert.Greater(_vfx.Spawned, spawned, "deaths burst");
            Assert.LessOrEqual(_vfx.ActiveCount, VfxPlayer.PoolSize, "never more than the pool");
            Assert.LessOrEqual(_vfx.ActiveOf(VfxKind.DeathSplat), VfxClips.Of(VfxKind.DeathSplat).Cap);
            Assert.Greater(_vfx.DroppedByCap, 0, "the deaths past the cap were dropped");
            Assert.LessOrEqual(_vfx.Live.Select(v => v.gameObject).Distinct().Count(), VfxPlayer.PoolSize);
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator The_wisp_leaves_a_trail_until_a_revive_or_possession()
        {
            Feedback.Raise(Cue.Wisp, _jackAt);
            Assert.IsTrue(_vfx.IsTrailing);
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.WispBurst), "the body bursts into the wisp");
            Feedback.Raise(Cue.Revive, _jackAt);
            Assert.IsFalse(_vfx.IsTrailing);
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.Revive));
            Feedback.Raise(Cue.Wisp, _jackAt);
            Feedback.Raise(Cue.Possess, _jackAt);
            Assert.IsFalse(_vfx.IsTrailing);
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.Possess));
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator Jacks_death_makes_a_wisp_that_leaves_a_trail_until_he_is_back()
        {
            TestDebug.Toggle("God mode", false);
            RegionEconomy.Current.Stockpile.Add(ResourceType.Souls, 100);   // with none, the body's death would end the Region
            RegionSouls souls = RegionSouls.Current;
            Building anchor = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            _jack.Teleport(anchor.transform.position + new Vector3(15f, 0f, 0f));
            yield return TestWait.Frames(2);
            var seen = new System.Collections.Generic.List<Cue>();
            void Note(FeedbackCue c) => seen.Add(c.Kind);
            Feedback.Raised += Note;
            _jack.GetComponent<Damageable>().Kill();
            yield return TestWait.Until(() => souls.Wisp != null, "the wisp", 3f);
            Feedback.Raised -= Note;
            Assert.IsTrue(_vfx.IsTrailing, "the wisp cue starts the trail; cues: " + string.Join(",", seen));
            souls.Wisp.Steer = Vector2.right;
            yield return TestWait.Until(() => _vfx.ActiveOf(VfxKind.WispTrail) > 1, "sparkles are left behind the wisp", 4f);
            Assert.Greater(_vfx.ActiveOf(VfxKind.WispBurst) + _vfx.Spawned, 0);
        }

        // ---- Corruption ----

        [UnityTest]
        public IEnumerator Corruption_spreading_pulses_and_purifying_shows_a_light()
        {
            RegionGrid grid = RegionGrid.Current;
            CellLayer layer = grid.Layer(RegionCorruption.LayerName);
            Int2 cell = grid.CellAt(_jackAt);
            layer.Set(cell, 0);
            yield return Settle();

            layer.Set(cell, 3);   // the spread: FeedbackRaiser answers with a cue
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.CorruptionPulse), "the spread pulses");

            layer.Set(cell, 1);   // a priest purifying
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.Purify), "purifying shows a light");

            yield return Settle();
            Vector3 unseen = Unseen();
            Int2 far = grid.CellAt(unseen);
            layer.Set(far, 3);
            layer.Set(far, 0);
            Assert.AreEqual(0, _vfx.ActiveCount, "out of sight nothing shows");
        }

        // ---- Marks ----

        [UnityTest]
        public IEnumerator Waves_alarms_and_riders_are_marked()
        {
            Feedback.Raise(Cue.Wave, _jackAt);
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.Wave));
            Feedback.Raise(Cue.Alarm, _jackAt);
            Feedback.Raise(Cue.Rider, _jackAt);
            Assert.Greater(_vfx.ActiveOf(VfxKind.Alert), 1, "alarms and arriving riders raise the alert symbol");
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.RiderArrives));
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator A_discovered_spawn_circle_is_marked_while_it_is_seen()
        {
            VigilanceSpawnCircle circle = VigilanceDirector.Current.Circles.First();
            Vector3 at = circle.transform.position;
            Assert.IsFalse(circle.Discovered);
            yield return null;
            Assert.AreEqual(0, _vfx.ActiveOf(VfxKind.SpawnCircle), "an unseen circle is not marked");

            RegionGrid grid = RegionGrid.Current;
            Vector3 spot = at;
            for (int i = 0; i < 96; i++)
            {
                Vector3 s = at + new Vector3(Mathf.Cos(i * 0.61f), Mathf.Sin(i * 0.61f), 0f) * (6f + i * 0.05f);
                if (grid.IsWalkable(s)) { spot = s; break; }
            }
            _jack.Teleport(spot);
            yield return TestWait.Until(() => circle.Discovered, "Jack's side sees the circle", 6f);
            yield return TestWait.Until(() => _vfx.ActiveOf(VfxKind.SpawnCircle) > 0, "the circle is marked", 5f);
        }

        // ---- Capture ----

        [UnityTest]
        public IEnumerator Capture_celebrates_with_a_crown_and_fireworks()
        {
            Feedback.Raise(Cue.Capture);
            Assert.AreEqual(1, _vfx.ActiveOf(VfxKind.Crown));
            int before = _vfx.Spawned;
            yield return TestWait.Until(() => _vfx.Spawned >= before + 6, "the fireworks go off", 4f);
            Assert.Greater(_vfx.ActiveOf(VfxKind.FireworkGold) + _vfx.ActiveOf(VfxKind.FireworkGreen), 0);
            yield return Settle();
        }

        // ---- Hits ----

        [UnityTest]
        public IEnumerator A_hit_flashes_the_unit_and_ends()
        {
            HordeMember victim = SpawnMortal(_jackAt + new Vector3(1.5f, 0f, 0f));
            yield return null;
            SpriteRenderer sprite = victim.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(sprite);
            Assert.IsFalse(_vfx.Flashes.IsDrawn(sprite));

            victim.Damageable.Unit.TakeDamage(1f, DamageRules.TrueType, _jackUnit);
            Assert.IsTrue(_vfx.Flashes.IsFlashing(sprite), "the unit flashes");
            Assert.IsTrue(_vfx.Flashes.IsDrawn(sprite), "drawn with the flash shader");

            yield return TestWait.Until(() => !_vfx.Flashes.IsFlashing(sprite), "the flash ends", 2f);
            Assert.IsFalse(_vfx.Flashes.IsDrawn(sprite), "and the unit has its own material back");
        }

        [Test]
        public void Construction_wear_with_no_attacker_is_not_a_hit()
        {
            HordeMember victim = SpawnMortal(_jackAt + new Vector3(1.5f, 0f, 0f));
            victim.Damageable.Unit.TakeDamage(1f, DamageRules.TrueType);
            Assert.AreEqual(0, _vfx.Flashes.Count);
        }

        [Test]
        public void A_hit_out_of_sight_does_not_flash()
        {
            HordeMember victim = SpawnMortal(Unseen());
            victim.Damageable.Unit.TakeDamage(1f, DamageRules.TrueType, _jackUnit);
            Assert.AreEqual(0, _vfx.Flashes.Count);
        }

        [Test]
        public void Only_a_few_units_flash_at_once()
        {
            for (int i = 0; i < 60; i++)
                SpawnMortal(_jackAt + new Vector3(1f + i % 10 * 0.3f, i / 10 * 0.3f, 0f)).Damageable.Unit.TakeDamage(1f, DamageRules.TrueType, _jackUnit);
            Assert.LessOrEqual(_vfx.Flashes.Count, VfxHitFlashes.Capacity);
        }

        [UnityTest]
        public IEnumerator Jacks_own_hits_shake_the_camera_a_little_and_others_do_not()
        {
            VfxCameraShake shake = VfxCameraShake.Current;
            Assert.IsNotNull(shake);
            HordeMember victim = SpawnMortal(_jackAt + new Vector3(1.5f, 0f, 0f));
            HordeMember other = SpawnMortal(_jackAt + new Vector3(-1.5f, 0f, 0f));
            yield return null;
            Assert.AreEqual(0f, shake.Strength, 1e-4f);

            other.Damageable.Unit.TakeDamage(1f, DamageRules.TrueType, victim.Damageable.Unit);
            Assert.AreEqual(0f, shake.Strength, 1e-4f, "a mortal's hit does not shake the camera");

            victim.Damageable.Unit.TakeDamage(1f, DamageRules.TrueType, _jackUnit);
            Assert.Greater(shake.Strength, 0f, "Jack's hit does");
            Assert.LessOrEqual(shake.Strength, VfxCameraShake.MaxStrength, "only slightly");
            Assert.LessOrEqual(VfxCameraShake.MaxStrength, 4f / 16f, "no more than a few pixels");

            Vector3 rest = Camera.main.transform.position;
            yield return null;
            Vector3 shaken = rest + shake.Offset;
            Assert.AreEqual(0f, shake.Offset.x * 16f - Mathf.Round(shake.Offset.x * 16f), 1e-3f, "shaken in whole pixels");
            yield return TestWait.Until(() => shake.Strength <= 0f, "the shake dies away", 3f);
            yield return null;
            Assert.AreEqual(Vector3.zero, shake.Offset, "and the camera is left where it was");
            Assert.IsNotNull(shaken);
        }

        // ---- Fog edge ----

        [UnityTest]
        public IEnumerator The_fog_edge_is_a_pool_of_whole_pixel_puffs_along_the_edge_of_sight()
        {
            FogEdgeOverlay edge = FogEdgeOverlay.Current;
            Assert.IsNotNull(edge, "the fog edge overlay is a Region system");
            RegionVision.RangeMultiplier = 0.25f;   // sight reaches past the screen at full range: pull the edge into view
            yield return TestWait.Frames(30);
            yield return TestWait.Until(() => { edge.ScanNow(); return edge.PuffCount > 0; }, "puffs appear along the edge", 5f);
            yield return TestWait.Frames(12);
            Assert.LessOrEqual(edge.PuffCount, FogEdgeOverlay.PoolSize);

            RegionGrid grid = RegionGrid.Current;
            int checkedPuffs = 0;
            for (int i = 0; i < FogEdgeOverlay.PoolSize; i++)
            {
                SpriteRenderer puff = edge.PuffAt(i);
                if (puff == null) continue;
                Vector3 p = puff.transform.position;
                Assert.AreEqual(0f, p.x * 16f - Mathf.Round(p.x * 16f), 1e-3f, "x on a whole pixel");
                Assert.AreEqual(0f, p.y * 16f - Mathf.Round(p.y * 16f), 1e-3f, "y on a whole pixel");
                Assert.AreEqual(FilterMode.Point, puff.sprite.texture.filterMode);
                Assert.Greater(puff.sortingOrder, FogOverlay.SortingOrder, "above the fog");
                Int2 cell = grid.CellAt(p);
                bool nearSight = false;
                for (int dx = -4; dx <= 4 && !nearSight; dx++)
                for (int dy = -4; dy <= 4 && !nearSight; dy++)
                    nearSight = RegionVision.Current.CanSee(Side.Yours, new Int2(cell.x + dx, cell.y + dy));
                Assert.IsTrue(nearSight, "a puff sits at the edge of what is seen");
                checkedPuffs++;
            }
            Assert.Greater(checkedPuffs, 0);

            RegionVision.IsOn = false;
            try
            {
                yield return TestWait.Until(() => Enumerable.Range(0, FogEdgeOverlay.PoolSize).All(i => edge.PuffAt(i) == null), "the puffs fade with fog off", 4f);
            }
            finally { RegionVision.IsOn = true; RegionVision.RangeMultiplier = 1f; }
        }
    }
}
