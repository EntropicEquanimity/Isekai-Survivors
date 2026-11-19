using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Roguelike.Drafts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The mortals' places in a generated Region: the Seat's fort, villages, farms and camps, their roads and walls, and what razing them and taking the Seat does.</summary>
    public class SettlementPlayModeTests
    {
        private RegionSettlements _settlements;
        private RegionLayout _layout;
        private RegionGrid _grid;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            // Drafts on: razing a settlement opens one.
            yield return RegionTestScene.Load(drafts: true);
            _settlements = RegionSettlements.Current;
            _layout = RegionTerrain.Current.Layout;
            _grid = RegionGrid.Current;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RunDrafts.ClearQueue();
            Time.timeScale = 1f;
            yield return null;
        }

        private static void Kill(Building b) => b.Damageable.Unit.TakeDamage(b.Damageable.MaxHealth * 10f, DamageRules.TrueType);

        private RegionSettlements.Settlement Village() => _settlements.Settlements.First(s => s.Kind == SettlementKind.Village);

        [UnityTest]
        public IEnumerator The_Region_has_the_settlements_the_layout_decided_with_every_Building_in_place()
        {
            Assert.IsNotNull(_settlements, "the settlements system is in the Region");
            Assert.AreEqual(_layout.Settlements.Settlements.Count, _settlements.Settlements.Count);
            Assert.IsTrue(_settlements.Seat.IsSeat);
            foreach (RegionSettlements.Settlement s in _settlements.Settlements)
                for (int i = 0; i < s.Buildings.Count; i++)
                {
                    Building b = s.Buildings[i];
                    PlacedBuilding spot = s.Plan.Buildings[i];
                    Assert.IsNotNull(b, $"{s.Kind} {spot.Kind} was built");
                    Assert.AreEqual(spot.Origin, b.Origin, $"{s.Kind} {spot.Kind} stands on its spot");
                    Assert.AreEqual(new Vector2Int(spot.Size.x, spot.Size.y), b.Asset.Size);
                    SpriteRenderer art = b.GetComponentInChildren<SpriteRenderer>();
                    Assert.IsNotNull(art.sprite, $"{s.Kind} {spot.Kind} has its art");
                    Assert.AreEqual(16f, art.sprite.pixelsPerUnit, "pixel-perfect: 16 pixels per cell");
                    Assert.AreEqual(FilterMode.Point, art.sprite.texture.filterMode);
                }
            Assert.GreaterOrEqual(_settlements.Settlements.Count, 1 + SettlementPlan.MinCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Mortal_buildings_belong_to_the_mortals_block_their_ground_and_are_listed_as_Buildings()
        {
            RegionSettlements.Settlement village = Village();
            Building hall = village.Buildings[village.Plan.Buildings.Select((b, i) => (b, i)).First(t => t.b.Kind == MortalBuilding.TownHall).i];
            Assert.AreEqual(TeamIds.Enemy, hall.Team, "on the mortals' team");
            Assert.IsFalse(hall.IsPlayers);
            RegionEntity entity = RegionEntities.Living.First(e => e.GameObject == hall.gameObject);
            Assert.AreEqual(Side.Mortals, entity.Side);
            Assert.AreEqual(UnitKind.Building, entity.Kind);
            Assert.IsNotNull(hall.GetComponent<HordeTarget>());
            Assert.IsFalse(hall.GetComponent<HordeTarget>().AttractsHorde, "no map-wide path is drawn to them");
            Assert.IsFalse(_grid.IsWalkable(_grid.CentreOf(hall.Origin)), "it blocks its cells for paths");
            Assert.IsTrue(Teams.AreHostile(hall.Damageable.Unit, Player.Character.GetComponent<Damageable>().Unit), "hostile to Jack's side");
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_mortal_building_takes_damage_and_can_be_razed_freeing_its_ground()
        {
            RegionSettlements.Settlement village = Village();
            Building house = village.Buildings[village.Plan.Buildings.Select((b, i) => (b, i)).First(t => t.b.Kind == MortalBuilding.PeasantHouse).i];
            Damageable damageable = house.Damageable;
            Int2 cell = house.Origin;
            float full = damageable.MaxHealth;
            damageable.Unit.TakeDamage(full * 0.25f, DamageRules.PhysicalType);
            Assert.Less(damageable.Normalized, 1f, "it takes damage");
            Assert.IsFalse(damageable.IsDead);

            Kill(house);
            Assert.IsTrue(damageable.IsDead);
            Assert.IsFalse(RegionEntities.Living.Any(e => e.GameObject == house.gameObject), "no longer listed");
            yield return TestWait.Until(() => house == null, "the ruin is cleared away", 5f);
            Assert.IsTrue(_grid.IsWalkable(cell), "its ground is open again");
        }

        [UnityTest]
        public IEnumerator Roads_and_ground_patches_are_laid_as_path_tiles()
        {
            Tilemap ground = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude).First(t => t.name == "Ground");
            Assert.Greater(_layout.Settlements.Roads.Count, 0);
            foreach (Road road in _layout.Settlements.Roads)
                foreach (Int2 cell in road.Cells)
                {
                    TileBase tile = ground.GetTile(ground.WorldToCell(_grid.CentreOf(cell)));
                    Assert.IsNotNull(tile, $"a road tile at {cell.x},{cell.y}");
                    Assert.AreEqual("Path_A", tile.name);
                }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rocks_and_nodes_keep_clear_of_the_places_and_the_places_publish_their_spawn_markers()
        {
            foreach (Obstacle o in Object.FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude).Where(o => o.name.StartsWith("Rock") || o.name.StartsWith("Cliff")))
                Assert.IsFalse(_layout.Settlements.IsKeepOut(_grid.CellAt(o.transform.position)), $"{o.name} stands on a settlement");
            foreach (ResourceNode n in ResourceNode.All)
                Assert.IsFalse(_layout.Settlements.IsKeepOut(_grid.CellAt(n.transform.position)), $"{n.name} stands on a settlement");

            foreach (RegionSettlements.Settlement s in _settlements.Settlements)
            {
                Assert.AreEqual(s.Plan.Piece.Markers.Count, s.Markers.Count, $"{s.Kind} markers");
                foreach (RegionSettlements.SpawnPoint m in s.Markers)
                    Assert.IsTrue(_grid.IsWalkable(m.Position), $"a {m.Kind} marker is on open ground");
            }
            Assert.IsTrue(_settlements.Seat.Markers.Any(m => m.Kind == MarkerKind.Guard));
            Assert.IsTrue(Village().Markers.Any(m => m.Kind == MarkerKind.Peasant));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Destroying_the_Seat_captures_the_Region()
        {
            Building keep = _settlements.Seat.Keep;
            Assert.IsNotNull(keep, "the Seat has a keep");
            Assert.IsFalse(RegionEvents.HasEnded);
            Kill(keep);
            Assert.IsTrue(RegionEvents.HasEnded);
            Assert.AreEqual(RegionEnd.Captured, RegionEvents.Outcome);
            Assert.IsTrue(_settlements.SeatCaptured);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Taking_the_Seat_needs_no_other_kill_and_breaking_its_walls_alone_captures_nothing()
        {
            RegionSettlements.Settlement seat = _settlements.Seat;
            for (int i = 0; i < seat.Buildings.Count; i++)
                if (seat.Plan.Buildings[i].IsWall) Kill(seat.Buildings[i]);
            Assert.IsFalse(RegionEvents.HasEnded, "the fort's wall and gate are no ruler");

            int mortals = RegionEntities.Count(Side.Mortals);
            Kill(seat.Keep);
            Assert.IsTrue(RegionEvents.HasEnded);
            Assert.AreEqual(RegionEnd.Captured, RegionEvents.Outcome);
            Assert.Greater(RegionEntities.Count(Side.Mortals), 0, "the Region fell with mortals still standing");
            Assert.Greater(mortals, 1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_debug_row_destroys_the_Seat_the_real_way()
        {
            Assert.IsNull(DebugActions.Find("Capture Region"), "the old stand-in is gone");
            TestDebug.Press("Destroy the Seat");
            yield return null;
            Assert.IsTrue(_settlements.Seat.Keep == null || !_settlements.Seat.Keep.IsAlive);
            Assert.AreEqual(RegionEnd.Captured, RegionEvents.Outcome);
        }

        [UnityTest]
        public IEnumerator Razing_a_settlement_opens_an_Item_draft()
        {
            RegionSettlements.Settlement village = Village();
            RegionSettlements.Settlement razed = null;
            _settlements.Razed += s => razed = s;
            Assert.IsNull(RunDrafts.Current);

            // Walls and the like don't count: knock the fence down first, nothing is razed.
            for (int i = 0; i < village.Buildings.Count; i++)
                if (village.Plan.Buildings[i].IsWall) Kill(village.Buildings[i]);
            yield return null;
            Assert.IsNull(razed);
            Assert.IsNull(RunDrafts.Current, "a fence falling is no razing");

            for (int i = 0; i < village.Buildings.Count; i++)
                if (!village.Plan.Buildings[i].IsWall) Kill(village.Buildings[i]);
            yield return null;
            Assert.AreSame(village, razed);
            Assert.IsTrue(village.IsRazed);
            Assert.IsNotNull(RunDrafts.Current, "an Item draft is open");
            Assert.IsFalse(RegionEvents.HasEnded, "razing a village does not capture the Region");
        }
    }
}
