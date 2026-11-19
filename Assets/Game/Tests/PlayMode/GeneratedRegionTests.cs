using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>A Region generated from a test recipe: Jack's corner, rocks and cliffs, nodes, wildlife spots and the Seat's spot.</summary>
    public class GeneratedRegionTests
    {
        private static readonly RegionRecipe Recipe = new(2026, 64, 48);

        private RegionLayout _layout;
        private RegionGrid _grid;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load(Recipe);
            _layout = RegionTerrain.Current.Layout;
            _grid = RegionGrid.Current;
        }

        private static IEnumerable<Obstacle> Rocks() =>
            Object.FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude).Where(o => o.name.StartsWith("Rock"));

        [UnityTest]
        public IEnumerator The_Region_is_the_recipes_small_map_laid_out_from_its_seed()
        {
            Assert.AreEqual(64, _grid.Width);
            Assert.AreEqual(48, _grid.Height);
            RegionLayout again = RegionLayout.Generate(Recipe);
            CollectionAssert.AreEqual(again.Rocks, _layout.Rocks, "the same seed lays out the same rocks");
            CollectionAssert.AreEquivalent(again.Rocks.Select(c => _grid.CentreOf(c)), Rocks().Select(r => r.transform.position), "and the Region has them where it said");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Jack_and_the_Main_Base_start_in_their_corner()
        {
            Vector3 jack = Player.Character.transform.position;
            Assert.AreEqual(_layout.Character, _grid.CellAt(jack), "the Character stands where the layout put it");
            Building home = Building.All.First(b => b.IsMainBase);
            yield return TestWait.Until(() => home.Origin.Equals(_layout.Anchor), "the Main Base takes its ground in the corner");
            int toEdgeX = Math.Min(_layout.Character.x, _grid.Width - 1 - _layout.Character.x);
            int toEdgeY = Math.Min(_layout.Character.y, _grid.Height - 1 - _layout.Character.y);
            Assert.LessOrEqual(toEdgeX, RegionLayout.CornerInset + 4, "in a far corner");
            Assert.LessOrEqual(toEdgeY, RegionLayout.CornerInset + 4, "in a far corner");
            Assert.IsFalse(_grid.IsWalkable(_layout.Anchor), "the Base blocks its ground");
            Assert.IsTrue(_grid.IsWalkable(jack), "and Jack is on open ground");
        }

        [UnityTest]
        public IEnumerator The_Seat_spot_is_reserved_toward_the_far_side_on_clear_ground()
        {
            RegionTerrain terrain = RegionTerrain.Current;
            Assert.AreEqual(_layout.Seat, _grid.CellAt(terrain.SeatSpot));
            Assert.Greater(Vector3.Distance(terrain.SeatSpot, Player.Character.transform.position), _grid.Width * 0.5f, "far from Jack");
            Assert.IsTrue(_grid.IsWalkable(terrain.SeatSpot));
            foreach (Obstacle o in Object.FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude))
                Assert.IsFalse(_layout.InSeatArea(_grid.CellAt(o.transform.position)), $"{o.name} stands in the Seat's area");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rocks_and_cliffs_block_movement_and_cut_sight_until_a_rock_is_mined_out()
        {
            CellLayer sight = _grid.Layer(TerrainLayers.SightBlock);
            Assert.Greater(_layout.Rocks.Count, 0);
            Assert.Greater(_layout.Cliffs.Count, 0);
            foreach (Int2 cell in _layout.Rocks.Concat(_layout.Cliffs))
            {
                Assert.IsFalse(_grid.IsWalkable(cell), $"{cell.x},{cell.y} blocks movement");
                Assert.AreEqual(1, sight.Get(cell), $"{cell.x},{cell.y} cuts sight");
            }
            Assert.AreEqual(0, sight.Get(_layout.Character), "open ground cuts no sight");
            foreach (Int2 cell in _layout.StoneNodes) Assert.AreEqual(0, sight.Get(cell), "a Stone node is no sight blocker");

            ResourceNode rock = Rocks().Select(r => r.GetComponent<ResourceNode>()).First();
            Int2 at = _grid.CellAt(rock.transform.position);
            rock.Take(rock.Remaining);
            yield return null;
            Assert.AreEqual(0, sight.Get(at), "a mined-out rock lets sight through");
            Assert.IsTrue(_grid.IsWalkable(at), "and movement");
        }

        [UnityTest]
        public IEnumerator Stone_and_Wood_nodes_stand_where_the_layout_put_them_and_Wood_is_mined_like_Stone()
        {
            ResourceNode[] wood = ResourceNode.All.Where(n => n.Type == ResourceType.Wood).ToArray();
            ResourceNode[] stone = ResourceNode.All.Where(n => n.Type == ResourceType.Stone && n.name.StartsWith("Stone")).ToArray();
            Assert.AreEqual(_layout.WoodNodes.Count, wood.Length);
            Assert.AreEqual(_layout.StoneNodes.Count, stone.Length);
            CollectionAssert.AreEquivalent(_layout.WoodNodes.Select(c => _grid.CentreOf(c)), wood.Select(n => n.transform.position));

            var miner = Object.FindAnyObjectByType<Miner>();
            miner.enabled = false;   // its Update would stop mining every frame: Interact isn't held in a test
            Stockpile pile = RegionEconomy.Current.Stockpile;
            int before = pile.Get(ResourceType.Wood);
            int mined = 0;
            for (int i = 0; i < 4; i++)
            {
                mined += miner.Mine(wood[0], 0.25f);
                yield return null;
            }
            Assert.Greater(mined, 0);
            Assert.AreEqual(before + mined, pile.Get(ResourceType.Wood), "mining a tree brings Wood");
        }

        [UnityTest]
        public IEnumerator Wildlife_spawn_spots_are_published_on_open_ground_far_from_Jack()
        {
            IReadOnlyList<Vector3> spots = RegionTerrain.Current.WildlifeSpots;
            Assert.AreEqual(_layout.WildlifeSpots.Count, spots.Count);
            CellLayer layer = _grid.Layer(TerrainLayers.WildlifeSpot);
            foreach (Vector3 spot in spots)
            {
                Assert.IsTrue(_grid.IsWalkable(spot));
                Assert.AreEqual(1, layer.Get(_grid.CellAt(spot)));
                Assert.Greater(Vector3.Distance(spot, Player.Character.transform.position), 15f);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_ground_covers_the_whole_map_and_the_edge_is_walled_in()
        {
            Tilemap ground = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude).First(t => t.name == "Ground");
            Assert.AreEqual(64 * 48, ground.GetTilesBlock(ground.cellBounds).Count(tile => tile != null), "a tile on every cell of the map, and none elsewhere");
            foreach (Int2 corner in new[] { new Int2(0, 0), new Int2(63, 0), new Int2(0, 47), new Int2(63, 47) })
                Assert.IsNotNull(ground.GetTile(ground.WorldToCell(_grid.CentreOf(corner))), $"grass at {corner.x},{corner.y}");
            Assert.IsNull(ground.GetTile(ground.WorldToCell(_grid.CentreOf(new Int2(64, 10)))), "none past the edge");

            GameObject edge = GameObject.Find("Map Edge");
            Assert.IsNotNull(edge);
            Assert.AreEqual(4, edge.GetComponents<BoxCollider2D>().Length);
            Assert.IsNull(GameObject.Find("Rocks"), "no hand-placed rocks in the scene");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Without_a_recipe_the_Region_is_made_from_the_Runs_seed_at_the_default_size()
        {
            yield return RegionTestScene.Load();
            RegionContext context = RegionBootstrap.Current.Context;
            Assert.AreEqual(RegionRecipe.DefaultWidth, RegionGrid.Current.Width);
            Assert.AreEqual(RegionRecipe.DefaultHeight, RegionGrid.Current.Height);
            Assert.AreEqual(context.Seed, RegionTerrain.Current.Layout.Recipe.Seed, "laid out from the Run's seed");
            Assert.AreEqual(RegionRecipe.DefaultWidth, RegionTerrain.Current.Layout.Width);
        }
    }
}
