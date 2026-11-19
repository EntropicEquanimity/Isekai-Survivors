using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Corruption in the real Region: the build rule, the Anchor's spread, dying demons, weakness and the overlay.</summary>
    public class CorruptionPlayModeTests
    {
        private RegionBuildings _buildings;
        private RegionCorruption _corruption;
        private Building _mainBase;
        private BuildMode _mode;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return null;
            _buildings = RegionBuildings.Current;
            _corruption = RegionCorruption.Current;
            _mainBase = Building.All.First(b => b.IsMainBase);
            _mode = Object.FindAnyObjectByType<BuildMode>();
            EconomyDebug.AddResources(1000);
            Int2 baseCell = RegionGrid.Current.CellAt(_mainBase.transform.position);
            Far = FindSpot(baseCell, null);
            Far2 = FindSpot(baseCell, Far);
            Time.timeScale = 4f;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private BuildingAsset Catalogue(string name) => _mode.Catalogue.First(a => a.DisplayName == name);

        // Open ground out of the Anchor reach (set per Region: the map size and start corner are generated).
        private Int2 Far;

        // Free corrupted ground next to the Main Base (the start corner is generated, so look around it).
        private Int2 Beside
        {
            get
            {
                Int2 o = _mainBase.Origin;
                foreach (int d in new[] { 4, -4, 6, -6, 8, -8 })
                foreach (int e in new[] { 0, 4, -4 })
                {
                    var c = new Int2(o.x + d, o.y + e);
                    if (_buildings.Check(Catalogue("Portal"), c).IsOk) return c;
                }
                Assert.Fail("no free corrupted ground beside the Main Base");
                return default;
            }
        }
        private Int2 Far2;

        private bool OpenSpot(Int2 c, Int2 baseCell, Int2? other)
        {
            if (System.Math.Max(System.Math.Abs(c.x - baseCell.x), System.Math.Abs(c.y - baseCell.y)) < 20) return false;
            if (other.HasValue && System.Math.Max(System.Math.Abs(c.x - other.Value.x), System.Math.Abs(c.y - other.Value.y)) < 12) return false;
            for (int dx = -3; dx <= 4; dx++)
            for (int dy = -3; dy <= 4; dy++)
                if (!RegionGrid.Current.IsWalkable(new Int2(c.x + dx, c.y + dy))) return false;
            return _buildings.IsFree(c, new Vector2Int(2, 2));
        }

        private Int2 FindSpot(Int2 baseCell, Int2? other)
        {
            RegionGrid g = RegionGrid.Current;
            for (int x = g.Width - 1; x >= 0; x--)
            for (int y = g.Height - 1; y >= 0; y--)
                if (OpenSpot(new Int2(x, y), baseCell, other)) return new Int2(x, y);
            Assert.Fail("no open ground far from the Main Base");
            return default;
        }

        private Int2 CellOf(Vector3 world) => RegionGrid.Current.CellAt(world);

        // Moves the transform and its body together, so physics does not pull it back.
        private static void Teleport(Component thing, Vector3 position)
        {
            thing.transform.position = position;
            if (thing.TryGetComponent(out Rigidbody2D body)) body.position = position;
            Physics2D.SyncTransforms();
        }

        [Test]
        public void The_Anchors_ground_starts_corrupted_and_far_ground_does_not()
        {
            Assert.GreaterOrEqual(_corruption.Level(_mainBase.Origin), CorruptionTuning.BuildLevel);
            Assert.AreEqual(0, _corruption.Level(Far));
        }

        [Test]
        public void Building_off_corruption_is_refused_with_not_corrupted_enough()
        {
            BuildingAsset tower = Catalogue("Spire");
            Placement refused = _buildings.Check(tower, Far);
            Assert.AreEqual(Verdict.NotCorrupted, refused.Verdict);
            Assert.AreEqual("not corrupted enough", refused.Reason);
            Assert.IsNull(_buildings.Place(tower, Far));
            Assert.IsTrue(_buildings.Check(tower, Beside).IsOk, "beside the Main Base it is fine");
        }

        [Test]
        public void Building_needs_level_2_on_every_footprint_cell()
        {
            BuildingAsset tower = Catalogue("Portal");
            Int2 origin = Far;
            _corruption.Field.Seed(origin, 3, 1);
            Assert.AreEqual(Verdict.NotCorrupted, _buildings.Check(tower, origin).Verdict, "level 1 is not enough");
            _corruption.Field.Set(origin, 2);
            Assert.AreEqual(Verdict.NotCorrupted, _buildings.Check(tower, origin).Verdict, "one cell is not the whole footprint");
            _corruption.Field.Seed(origin, 1, 2);
            Assert.IsTrue(_buildings.Check(tower, origin).IsOk, "level 2 is enough");
            _corruption.Field.Seed(origin, 1, 4);
            Assert.IsTrue(_buildings.Check(tower, origin).IsOk, "level 4 too");
            Assert.IsNotNull(_buildings.Place(tower, origin));
        }

        [UnityTest]
        public IEnumerator The_Anchor_spreads_corruption_over_time()
        {
            Int2 cell = CellOf(_mainBase.transform.position);
            _corruption.Field.Set(cell, 0);
            yield return TestWait.Until(() => _corruption.Level(cell) > 0, "the Anchor corrupts its own cell again", 10f);
        }

        [UnityTest]
        public IEnumerator A_Spire_does_not_spread_but_an_Outpost_does()
        {
            Int2 spireAt = Far;
            Int2 outpostAt = Far2;
            _corruption.Field.Seed(spireAt, 2, 2);
            _corruption.Field.Seed(outpostAt, 2, 2);
            Building spire = _buildings.Place(Catalogue("Spire"), spireAt);
            Building outpost = _buildings.Place(Catalogue("Outpost"), outpostAt);
            Assert.IsNotNull(spire);
            Assert.IsNotNull(outpost);
            spire.FinishConstruction();
            outpost.FinishConstruction();
            Int2 spireCell = CellOf(spire.transform.position);
            Int2 outpostCell = CellOf(outpost.transform.position);
            yield return TestWait.Until(() => _corruption.Level(outpostCell) > 2, "the Outpost raises its ground past level 2", 15f);
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(2, _corruption.Level(spireCell), "a Spire spreads nothing");
        }

        [UnityTest]
        public IEnumerator A_dying_demon_raises_its_cell_when_the_roll_succeeds_and_not_when_it_fails()
        {
            Building portal = _buildings.Place(Catalogue("Portal"), Beside);
            Assert.IsNotNull(portal);
            var producer = portal.GetComponent<UnitProducer>();
            yield return TestWait.Until(() => producer.Units.Count >= 2, "the Portal brings two Imps", 30f);
            DefenderBrain failing = producer.Units[0], succeeding = producer.Units[1];
            Int2 a = Far, b = Far2;

            Time.timeScale = 0f;
            failing.transform.position = RegionGrid.Current.CentreOf(a);
            succeeding.transform.position = RegionGrid.Current.CentreOf(b);
            Assert.AreEqual(0, _corruption.Level(a));
            _corruption.DyingDemonChance = 0f;
            failing.GetComponent<Damageable>().Unit.TakeDamage(100000f, DamageRules.TrueType);
            Assert.AreEqual(0, _corruption.Level(a), "a failed roll changes nothing");
            _corruption.DyingDemonChance = 1f;
            succeeding.GetComponent<Damageable>().Unit.TakeDamage(100000f, DamageRules.TrueType);
            Assert.AreEqual(1, _corruption.Level(b), "a successful roll raises the cell one level");
            Time.timeScale = 4f;
        }

        [UnityTest]
        public IEnumerator A_dying_demon_on_level_4_changes_nothing()
        {
            Building portal = _buildings.Place(Catalogue("Portal"), Beside);
            var producer = portal.GetComponent<UnitProducer>();
            yield return TestWait.Until(() => producer.Units.Count >= 1, "the Portal brings an Imp", 30f);
            DefenderBrain imp = producer.Units[0];
            Time.timeScale = 0f;
            Int2 cell = Far;
            imp.transform.position = RegionGrid.Current.CentreOf(cell);
            _corruption.Field.Set(cell, 4);
            _corruption.DyingDemonChance = 1f;
            imp.GetComponent<Damageable>().Unit.TakeDamage(100000f, DamageRules.TrueType);
            Assert.AreEqual(4, _corruption.Level(cell));
            Time.timeScale = 4f;
        }

        [UnityTest]
        public IEnumerator Purify_lowers_a_cell_one_level_and_stops_at_0()
        {
            Time.timeScale = 0f;
            Int2 cell = Far;
            _corruption.Field.Set(cell, 2);
            Assert.IsTrue(_corruption.Purify(cell));
            Assert.AreEqual(1, _corruption.Level(cell));
            Assert.IsTrue(_corruption.Purify(cell));
            Assert.IsFalse(_corruption.Purify(cell), "already clean");
            Assert.AreEqual(0, _corruption.Level(cell));
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_Character_is_weaker_on_ground_below_level_2_and_normal_on_corrupted_ground()
        {
            CharacterControl jack = Player.Character;
            RegionEntity entity = RegionEntities.Living.First(u => u.GameObject == jack.gameObject);
            var weakness = CorruptionWeakness.Current;
            var stats = jack.GetComponent<Damageable>().Stats;
            const string bonus = nameof(StatId.DamageBonus);
            Vector3 homeSpot = RegionGrid.Current.CentreOf(CellOf(_mainBase.transform.position));
            Time.timeScale = 1f;

            Teleport(jack, homeSpot);
            yield return TestWait.Until(() => !weakness.IsWeak(entity), "Jack is not weak on the Anchor ground", 3f);
            float normal = stats.GetStatValue(bonus, 0f);

            Teleport(jack, RegionGrid.Current.CentreOf(Far));
            yield return TestWait.Until(() => weakness.IsWeak(entity), "Jack is weak off corruption", 3f);
            Assert.AreEqual(normal - 0.2f, stats.GetStatValue(bonus, 0f), 1e-4f, "20% less damage");

            Teleport(jack, homeSpot);
            yield return TestWait.Until(() => !weakness.IsWeak(entity), "Jack is back to normal on corrupted ground", 3f);
            Assert.AreEqual(normal, stats.GetStatValue(bonus, 0f), 1e-4f);
        }

        [UnityTest]
        public IEnumerator An_Imp_below_level_2_is_weakened_too()
        {
            Building portal = _buildings.Place(Catalogue("Portal"), Beside);
            var producer = portal.GetComponent<UnitProducer>();
            yield return TestWait.Until(() => producer.Units.Count >= 1, "the Portal brings an Imp", 30f);
            DefenderBrain imp = producer.Units[0];
            var stats = imp.GetComponent<Damageable>().Stats;
            Time.timeScale = 1f;
            imp.transform.position = RegionGrid.Current.CentreOf(Far);
            yield return TestWait.Until(() => stats.GetStatValue(nameof(StatId.DamageBonus), 0f) < -0.19f, "the Imp is weaker off corruption", 3f);
        }

        [UnityTest]
        public IEnumerator The_overlay_draws_corrupted_ground_and_redraws_only_changed_cells()
        {
            var overlay = Object.FindAnyObjectByType<CorruptionOverlay>();
            Time.timeScale = 0f;
            yield return null;
            Int2 seeded = CellOf(_mainBase.transform.position);
            Assert.IsNotNull(overlay.SpriteAt(seeded), "corrupted ground has a tile");
            Assert.IsNull(overlay.SpriteAt(Far), "level 0 has none");

            int before = overlay.Redraws;
            _corruption.Field.Set(Far, 1);
            Assert.AreEqual(before + 1, overlay.Redraws, "one changed cell, one redraw");
            Assert.IsNotNull(overlay.SpriteAt(Far));
            _corruption.Field.Set(Far, 1);
            Assert.AreEqual(before + 1, overlay.Redraws, "setting the same level redraws nothing");
            _corruption.Field.Set(Far, 4);
            _corruption.Field.Raise(Far);
            Assert.AreEqual(before + 2, overlay.Redraws, "raising past level 4 redraws nothing");
            _corruption.Field.Set(Far, 0);
            Assert.IsNull(overlay.SpriteAt(Far), "back to level 0 clears the tile");
        }
    }
}
