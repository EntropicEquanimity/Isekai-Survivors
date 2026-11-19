using System.Collections;
using System.Linq;
using Blondie.Core;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Fog of war in the real Region: units out of your sight are not drawn, the map obeys the same rules, and a side can ask what it sees.</summary>
    public class VisionPlayModeTests
    {
        private RegionGrid _grid;
        private RegionVision _vision;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region is ready");
            RegionTestScene.ClearHorde();
            TestDebug.Toggle("God mode", true);
            _grid = RegionGrid.Current;
            _jack = Player.Character;
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            _vision = RegionVision.Current;
            yield return TestWait.Frames(3);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RegionVision.RangeMultiplier = 1f;
            RegionVision.IsOn = true;
            Time.timeScale = 1f;
            yield return null;
        }

        private Int2 JackCell => _grid.CellAt(_jack.transform.position);

        // A walkable cell at about this many cells from Jack, in the first direction that has one.
        private Int2 WalkableAt(Int2 from, int distance)
        {
            for (int turn = 0; turn < 32; turn++)
            {
                float a = turn * Mathf.PI / 16f;
                var cell = new Int2(from.x + Mathf.RoundToInt(Mathf.Cos(a) * distance), from.y + Mathf.RoundToInt(Mathf.Sin(a) * distance));
                if (_grid.Contains(cell) && _grid.IsWalkable(cell)) return cell;
            }
            Assert.Fail($"no walkable cell {distance} cells from {from}");
            return default;
        }

        private static HordeMember SpawnMortalAt(Vector3 at)
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            GameObject go = Pools.Spawn(spawner.Prefab, at, Quaternion.identity);
            var member = go.GetComponent<HordeMember>();
            member.SetRoaming(true);
            return member;
        }

        private static RegionEntity EntityOf(HordeMember m) => RegionEntities.Living.First(e => e.GameObject == m.gameObject);

        private static bool Drawn(HordeMember m)
        {
            Renderer[] renderers = m.GetComponentsInChildren<Renderer>(true);
            return renderers.Length > 0 && renderers.Any(r => r.enabled);
        }

        [UnityTest]
        public IEnumerator Fog_hides_a_mortal_until_the_Character_sees_it()
        {
            Int2 far = WalkableAt(JackCell, 40);
            Assert.IsFalse(_vision.CanSee(Side.Yours, far));
            HordeMember mortal = SpawnMortalAt(_grid.CentreOf(far));
            yield return TestWait.Until(() => !Drawn(mortal), "the mortal is hidden in the fog");
            Assert.AreEqual(FogState.Unseen, _vision.StateOf(Side.Yours, far));
            Assert.AreEqual(255, FogOverlay.Current.ColourAt(far).a, "the ground over it is black");

            _jack.transform.position = _grid.CentreOf(WalkableAt(far, 4));
            yield return TestWait.Until(() => Drawn(mortal), "the Character comes within sight and the mortal shows");
            Assert.IsTrue(_vision.CanSee(Side.Yours, far));
            Assert.AreEqual(0, FogOverlay.Current.ColourAt(far).a, "the fog lifts over it");
        }

        [UnityTest]
        public IEnumerator Ground_stays_explored_after_Jack_looks_away_and_units_there_stay_hidden()
        {
            Int2 near = WalkableAt(JackCell, 9);
            yield return TestWait.Until(() => _vision.CanSee(Side.Yours, near), "Jack sees ground 9 cells away");
            HordeMember mortal = SpawnMortalAt(_grid.CentreOf(near));
            yield return TestWait.Until(() => Drawn(mortal), "visible while in sight");

            RegionVision.RangeMultiplier = 0.2f;   // Jack's 14 cells shrink to 3: the ground is remembered, not seen
            yield return TestWait.Until(() => !_vision.CanSee(Side.Yours, near), "the range shrinks");
            Assert.AreEqual(FogState.Explored, _vision.StateOf(Side.Yours, near));
            Assert.Greater(FogOverlay.Current.ColourAt(near).a, 0, "dimmed");
            Assert.Less(FogOverlay.Current.ColourAt(near).a, 255, "but not black");
            yield return TestWait.Until(() => !Drawn(mortal), "the mortal on explored ground is hidden once out of sight");
        }

        [UnityTest]
        public IEnumerator The_map_screen_blacks_out_unseen_ground_and_hides_units_out_of_sight()
        {
            Int2 near = WalkableAt(JackCell, 9);
            yield return TestWait.Until(() => _vision.CanSee(Side.Yours, near), "Jack sees ground 9 cells away");
            HordeMember mortal = SpawnMortalAt(_grid.CentreOf(near));
            yield return TestWait.Frames(2);
            var view = new RegionMapView(_grid.Field);
            Vector3 at = _grid.CentreOf(near);

            RegionVision.IsOn = false;
            view.Redraw();
            Assert.AreEqual(RegionMapView.Roamer, view.PixelAt(at), "with the fog off the map shows the roaming mortal");

            RegionVision.IsOn = true;
            view.Redraw();
            Assert.AreEqual(RegionMapView.Roamer, view.PixelAt(at), "in sight it shows");

            RegionVision.RangeMultiplier = 0.2f;
            yield return TestWait.Until(() => !_vision.CanSee(Side.Yours, near), "the range shrinks");
            view.Redraw();
            Assert.AreNotEqual(RegionMapView.Roamer, view.PixelAt(at), "out of sight the mortal is gone from the map");
            Assert.AreNotEqual(new Color32(8, 9, 12, 255), view.PixelAt(at), "but the ground is explored, so it still shows");

            Int2 unseen = WalkableAt(JackCell, 60);
            Assert.AreEqual(FogState.Unseen, _vision.StateOf(Side.Yours, unseen));
            Assert.AreEqual(new Color32(8, 9, 12, 255), view.PixelAt(_grid.CentreOf(unseen)), "never seen: black");
            view.Dispose();
            Assert.IsNotNull(mortal);
        }

        [UnityTest]
        public IEnumerator A_demons_sight_shows_mortals_near_it_while_the_rest_stay_hidden()
        {
            Time.timeScale = 4f;
            RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, 100);
            RegionEconomy.Current.Stockpile.Add(ResourceType.Souls, 20);
            Building mainBase = Building.All.First(b => b.IsMainBase);
            BuildingAsset portal = Object.FindAnyObjectByType<BuildMode>().Catalogue.First(a => a.DisplayName == "Portal");
            Building placed = RegionBuildings.Current.Place(portal, new Int2(mainBase.Origin.x + 4, mainBase.Origin.y));
            Assert.IsNotNull(placed);
            placed.FinishConstruction();
            var producer = placed.GetComponent<UnitProducer>();
            yield return TestWait.Until(() => producer.Units.Count > 0, "the Portal brings an Imp", 15f);
            DemonBrain imp = producer.Units[0];

            // Jack goes far from the Imp, so only the Imp's eyes are near the Portal.
            Int2 start = _grid.CellAt(imp.transform.position);
            var corners = new[] { new Int2(2, 2), new Int2(_grid.Width - 3, 2), new Int2(2, _grid.Height - 3), new Int2(_grid.Width - 3, _grid.Height - 3) };
            Int2 corner = corners.OrderByDescending(c => (c.x - start.x) * (c.x - start.x) + (c.y - start.y) * (c.y - start.y)).First();
            _jack.transform.position = _grid.CentreOf(corner);
            yield return TestWait.Frames(3);
            Int2 impCell = _grid.CellAt(imp.transform.position);
            Assert.IsTrue(_vision.CanSee(Side.Yours, impCell), "the Imp sees its own ground");

            Int2 farCell = WalkableAt(impCell, 40);
            Int2 closeCell = default;
            bool found = false;
            for (int d = 4; d <= 6 && !found; d++)
                for (int turn = 0; turn < 32 && !found; turn++)
                {
                    float a = turn * Mathf.PI / 16f;
                    var c = new Int2(impCell.x + Mathf.RoundToInt(Mathf.Cos(a) * d), impCell.y + Mathf.RoundToInt(Mathf.Sin(a) * d));
                    if (_grid.Contains(c) && _grid.IsWalkable(c) && _vision.CanSee(Side.Yours, c)) { closeCell = c; found = true; }
                }
            Assert.IsTrue(found, "ground in the Imp's sight a few cells off");
            HordeMember hidden = SpawnMortalAt(_grid.CentreOf(farCell));
            HordeMember seen = SpawnMortalAt(_grid.CentreOf(closeCell));
            yield return TestWait.Frames(3);
            Assert.IsFalse(Drawn(hidden), "out of every viewer's sight");
            Assert.IsTrue(Drawn(seen), "the Imp sees it");
            Assert.IsFalse(FogUnits.Current.IsHidden(EntityOf(seen)));
            Assert.IsTrue(FogUnits.Current.IsHidden(EntityOf(hidden)));
        }

        [UnityTest]
        public IEnumerator A_mortal_sees_what_is_near_it_on_its_own_side_and_the_mortals_can_be_asked()
        {
            Int2 far = WalkableAt(JackCell, 40);
            Assert.IsFalse(_vision.CanSee(Side.Mortals, far));
            SpawnMortalAt(_grid.CentreOf(far));
            yield return TestWait.Until(() => _vision.CanSee(Side.Mortals, far), "the mortal sees the ground it stands on");
            Assert.IsFalse(_vision.CanSee(Side.Yours, far), "and your side does not");
            Assert.AreEqual(1, _grid.Layer("Visible.Mortals").Get(far));
            Assert.AreEqual(0, _grid.Layer("Visible.Yours").Get(far));
            Assert.AreEqual(1, _grid.Layer("Explored.Mortals").Get(far));
        }

        [UnityTest]
        public IEnumerator A_Building_destroyed_out_of_sight_stays_on_the_map_until_seen_again()
        {
            RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, 200);
            Building mainBase = Building.All.First(b => b.IsMainBase);
            BuildingAsset spire = Object.FindAnyObjectByType<BuildMode>().Catalogue.First(a => a.DisplayName == "Spire");
            Building tower = RegionBuildings.Current.Place(spire, new Int2(mainBase.Origin.x + 4, mainBase.Origin.y));
            Assert.IsNotNull(tower);
            Int2 cell = _grid.CellAt(tower.transform.position);
            yield return TestWait.Until(() => _vision.CanSee(Side.Yours, cell), "Jack sees the Spire's ground");

            RegionVision.RangeMultiplier = 0.1f;
            yield return TestWait.Until(() => !_vision.CanSee(Side.Yours, cell), "the Spire is out of sight");
            tower.Damageable.Kill();
            yield return TestWait.Until(() => FogUnits.Current.Ghosts.Count == 1, "a ghost of the Spire is left behind");

            RegionVision.RangeMultiplier = 1f;
            yield return TestWait.Until(() => FogUnits.Current.Ghosts.Count == 0, "seeing the ground again clears the ghost");
        }
    }
}
