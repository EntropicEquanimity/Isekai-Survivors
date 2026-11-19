using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>A system that joins every Region from code alone: no scene object, just the attribute.</summary>
    [RegionSystem]
    public sealed class ProbeSystem : MonoBehaviour, IRegionSetup, IRegionPopulate
    {
        public static readonly List<string> Log = new();
        public static RegionContext SetupContext;
        public static string RegionRunKnewItsRegionAtSetup;

        int IRegionSetup.SetupOrder => RegionOrder.Terrain;
        int IRegionPopulate.PopulateOrder => RegionOrder.Patrols;

        void IRegionSetup.Setup(RegionContext region)
        {
            Log.Add("setup");
            SetupContext = region;
            RegionRunKnewItsRegionAtSetup = FindAnyObjectByType<RegionRun>()?.Region?.Name;
        }

        void IRegionPopulate.Populate(RegionContext region) => Log.Add("populate");
    }

    /// <summary>A map layer that paints one square, on top of the ground and units.</summary>
    public sealed class DotPainter : IMapPainter
    {
        public static readonly Color32 Colour = new(255, 0, 255, 255);
        public Vector3 At;
        public int Order => MapOrder.Overlays;
        public void Paint(MapCanvas canvas) => canvas.Square(canvas.ToPixel(At), 2, Colour);
    }

    /// <summary>The seams new mechanics plug into: ordered Region startup, placement rules, sides, Region end, debug entries, grid layers, the unit registry, map painters and recipes.</summary>
    public class FoundationTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Teams.ClearRelations();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_Region_sets_up_then_populates_with_a_stable_seed()
        {
            RegionBootstrap boot = RegionBootstrap.Current;
            Assert.IsNotNull(boot, "the Region scene has a bootstrap");
            yield return TestWait.Until(() => boot.IsPopulated, "the Region fills once its walls are baked");
            Assert.IsTrue(RegionGrid.Current.IsBaked);
            Assert.AreEqual(1f, boot.Context.HealthMultiplier, "no threat registered: enemies keep their authored health");

            var again = new RegionContext(boot.Context.Region, boot.Context.Run, RunSession.Seed);
            Assert.AreEqual(boot.Context.Seed, again.Seed, "the same Run gives the Region the same seed");
            Assert.AreEqual(boot.Context.Random("wilds").Next(), again.Random("wilds").Next());
            Assert.AreNotEqual(boot.Context.Random("wilds").Next(), boot.Context.Random("waves").Next(), "each system has its own stream");
            Assert.AreEqual(Player.Character, Object.FindAnyObjectByType<CharacterControl>(), "the player's body is known without searching");
        }

        [UnityTest]
        public IEnumerator A_new_placement_rule_can_forbid_building_and_say_why()
        {
            yield return TestWait.Until(() => Building.All.Any(b => b.IsMainBase), "the Main Base");
            RegionBuildings buildings = RegionBuildings.Current;
            BuildingAsset tower = Object.FindAnyObjectByType<BuildMode>().Catalogue.First(a => a.DisplayName == "Spire");
            RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, 100);
            Building home = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            var beside = new Int2(home.Origin.x + 4, home.Origin.y);
            Assert.IsTrue(buildings.Check(tower, beside).IsOk);

            buildings.Rules.Add(new NoBuildingRule());
            Placement result = buildings.Check(tower, beside);
            Assert.AreEqual(Verdict.NotAllowed, result.Verdict);
            Assert.AreEqual("Not on hallowed ground", result.Reason);
            Assert.IsNull(buildings.Place(tower, beside));
        }

        private sealed class NoBuildingRule : IPlacementRule
        {
            public Placement Check(in PlacementQuery query) => Placement.No(Verdict.NotAllowed, "Not on hallowed ground");
        }

        [UnityTest]
        public IEnumerator Who_the_horde_attacks_comes_from_team_relations()
        {
            RegionTestScene.ClearHorde();
            Object.FindAnyObjectByType<FlowFieldCrowd>().Speed = 0f;
            CharacterControl character = Player.Character;
            character.GetComponent<AutoCaster>().enabled = false;
            var health = character.GetComponent<Damageable>();
            GameObject prefab = Object.FindAnyObjectByType<HordeSpawner>().Prefab;

            Teams.SetRelation(TeamIds.Enemy, TeamIds.Player, TeamRelation.Neutral);
            Pools.Spawn(prefab, character.transform.position + new Vector3(0.3f, 0f, 0f), Quaternion.identity);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(health.MaxHealth, health.Health, 1e-3f, "a neutral horde leaves the Character alone");

            Teams.ClearRelations();
            yield return TestWait.Until(() => health.Health < health.MaxHealth, "a hostile horde hits", 3f);
        }

        [UnityTest]
        public IEnumerator Systems_add_their_own_debug_entries()
        {
            yield return null;
            var owner = new object();
            int runs = 0;
            DebugActions.Button(owner, "Test", "Run me", () => runs++);
            DebugActions.All.First(e => e.Owner == owner && e.Label == "Run me").Run();
            Assert.AreEqual(1, runs, "the entry runs its action");
            DebugActions.Remove(owner);
            Assert.IsFalse(DebugActions.All.Any(e => e.Owner == owner), "a removed system takes its entries away");
        }

        [UnityTest]
        public IEnumerator A_system_joins_the_Region_from_code_without_a_scene_object()
        {
            ProbeSystem.Log.Clear();
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            CollectionAssert.AreEqual(new[] { "setup", "populate" }, ProbeSystem.Log, "set up, then populated, once each");
            Assert.AreSame(RegionBootstrap.Current.Context, ProbeSystem.SetupContext);
            Assert.IsNull(ProbeSystem.RegionRunKnewItsRegionAtSetup, "RegionOrder.Terrain runs before the existing Settings step");
            var probe = Object.FindAnyObjectByType<ProbeSystem>();
            Assert.AreEqual("Region Systems", probe.gameObject.name, "it lives on the object the bootstrap made");
            Assert.AreEqual(1, Object.FindObjectsByType<ProbeSystem>(FindObjectsInactive.Include).Length, "once per Region");
        }

        [UnityTest]
        public IEnumerator Every_pair_of_sides_gets_its_relation_when_the_Region_starts()
        {
            yield return null;
            foreach (Side a in SideRelations.All)
            foreach (Side b in SideRelations.All)
            {
                TeamRelation expected = SideRelations.Between(a, b) switch
                {
                    SideRelation.Hostile => TeamRelation.Hostile,
                    SideRelation.Allied => TeamRelation.Allied,
                    _ => TeamRelation.Neutral,
                };
                Assert.AreEqual(expected, Teams.Relation(TeamIds.Of(a), TeamIds.Of(b)), $"{a} / {b}");
            }
            Assert.AreEqual(TeamRelation.Hostile, Teams.Relation(TeamIds.Player, TeamIds.Enemy), "the existing prefabs teams are still enemies");
        }

        [UnityTest]
        public IEnumerator Grid_layers_hold_bytes_and_never_touch_the_flow_field()
        {
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            RegionGrid grid = RegionGrid.Current;
            CellLayer vision = grid.Layer("vision");
            Assert.AreSame(vision, grid.Layer("vision"), "a name is one layer");
            Assert.AreNotSame(vision, grid.Layer("corruption"));
            Assert.AreEqual(grid.Width, vision.Width);
            Assert.AreEqual(grid.Height, vision.Height);

            var cell = new Int2(10, 10);
            byte cost = grid.Field.GetCost(cell);
            int version = grid.Field.Version;
            int changes = 0;
            vision.Changed += (c, before, after) => changes++;
            vision.Set(cell, 200);
            vision.Set(cell, 200);
            Assert.AreEqual(200, vision.Get(cell));
            Assert.AreEqual(1, changes, "one change, told once");
            Assert.AreEqual(cost, grid.Field.GetCost(cell), "the path cost of the cell is untouched");
            Assert.AreEqual(version, grid.Field.Version, "no path is invalidated");
            Assert.AreEqual(0, grid.Layer("corruption").Get(cell), "layers are separate");
        }

        [UnityTest]
        public IEnumerator A_death_is_announced_once_with_side_kind_killer_and_cell()
        {
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            RegionTestScene.ClearHorde();
            Object.FindAnyObjectByType<FlowFieldCrowd>().Speed = 0f;
            CharacterControl character = Player.Character;
            GameObject prefab = Object.FindAnyObjectByType<HordeSpawner>().Prefab;
            Vector3 where = character.transform.position + new Vector3(6f, 0f, 0f);
            GameObject enemy = Pools.Spawn(prefab, where, Quaternion.identity);
            yield return null;
            var member = enemy.GetComponent<HordeMember>();

            Assert.IsTrue(RegionUnits.Living.Any(u => u.GameObject == enemy && u.Kind == UnitKind.Mortal && u.Side == Side.Mortals), "a living mortal");
            Assert.IsTrue(RegionUnits.Living.Any(u => u.GameObject == character.gameObject && u.Kind == UnitKind.Jack && u.Side == Side.Yours), "Jack");
            Assert.IsTrue(RegionUnits.Living.Any(u => u.Kind == UnitKind.Building && u.Side == Side.Yours), "the Main Base");

            var deaths = new List<UnitDeath>();
            RegionUnits.Died += deaths.Add;
            int mortals = RegionUnits.Count(Side.Mortals);
            member.Unit.TakeDamage(100000f, DamageRules.PhysicalType, character.GetComponent<Damageable>().Unit);
            Assert.AreEqual(1, deaths.Count);
            UnitDeath death = deaths[0];
            Assert.AreSame(enemy, death.Unit.GameObject);
            Assert.AreEqual(Side.Mortals, death.Side);
            Assert.AreEqual(UnitKind.Mortal, death.Kind);
            Assert.AreEqual(Side.Yours, death.KillerSide);
            Assert.AreEqual(RegionGrid.Current.CellAt(where), death.Cell);
            Assert.AreEqual(mortals - 1, RegionUnits.Count(Side.Mortals), "the dead leave the registry");

            GameObject second = Pools.Spawn(prefab, where, Quaternion.identity);
            yield return null;
            second.GetComponent<HordeMember>().Damageable.Kill();
            Assert.AreEqual(2, deaths.Count);
            Assert.IsNull(deaths[1].KillerSide, "a death with no killer says so");
        }

        [UnityTest]
        public IEnumerator A_pooled_enemy_that_comes_back_is_registered_again()
        {
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            RegionTestScene.ClearHorde();
            Object.FindAnyObjectByType<FlowFieldCrowd>().Speed = 0f;
            GameObject prefab = Object.FindAnyObjectByType<HordeSpawner>().Prefab;
            Vector3 where = Player.Character.transform.position + new Vector3(6f, 0f, 0f);
            GameObject first = Pools.Spawn(prefab, where, Quaternion.identity);
            yield return null;
            first.GetComponent<HordeMember>().Damageable.Kill();
            Pools.Despawn(first);
            yield return null;
            Assert.IsFalse(RegionUnits.Living.Any(u => u.GameObject == first && !u.Damageable.IsDead && first.activeInHierarchy), "gone while pooled");
            GameObject again = Pools.Spawn(prefab, where, Quaternion.identity);
            yield return null;
            Assert.AreEqual(1, RegionUnits.Living.Count(u => u.GameObject == again && !u.Damageable.IsDead), "listed again, once");
        }

        [UnityTest]
        public IEnumerator The_map_is_drawn_by_painters_that_systems_register()
        {
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            CollectionAssert.AreEqual(
                new[] { typeof(GroundPainter), typeof(BuildingsPainter), typeof(UnitsPainter), typeof(CameraPainter) },
                MapPainters.All.Select(p => p.GetType()).ToArray(), "ground, buildings, units, camera, in drawing order");

            var map = new RegionMapView(RegionGrid.Current.Field);
            Vector3 spot = RegionGrid.Current.CentreOf(new Int2(30, 30));
            var dot = new DotPainter { At = spot };
            MapPainters.Add(dot);
            map.Redraw();
            Assert.AreEqual(DotPainter.Colour, map.PixelAt(spot), "a registered painter draws");
            Assert.AreEqual(typeof(CameraPainter), MapPainters.All[MapPainters.All.Count - 1].GetType(), "the camera still draws last: order, not registration, decides");

            MapPainters.Remove(dot);
            map.Redraw();
            Assert.AreNotEqual(DotPainter.Colour, map.PixelAt(spot), "a removed painter stops drawing");
            map.Dispose();
        }

        [UnityTest]
        public IEnumerator The_debug_menu_rows_are_entries_owned_by_their_systems()
        {
            yield return null;
            foreach (string label in new[] { "+100 each", "+1000 each", "God mode", "Heal", "Finish buildings", "Spawn 20 here", "Spawn 200", "Kill all", "Capture Region", "Open Item draft", "×1" })
            {
                DebugActions.Entry e = DebugActions.Find(label);
                Assert.IsNotNull(e, label);
                Assert.IsNotNull(e.Owner, label);
                Assert.IsNotInstanceOf<DebugMenu>(e.Owner, label + " belongs to a system, not the menu");
            }
        }

        [UnityTest]
        public IEnumerator A_Region_loads_from_a_recipe_with_its_seed_and_map_size()
        {
            var recipe = new RegionRecipe(777, 96, 64);
            yield return RegionTestScene.Load(recipe);
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            RegionContext context = RegionBootstrap.Current.Context;
            Assert.AreSame(recipe, context.Recipe);
            Assert.AreEqual(777, context.Seed);
            Assert.AreEqual(96, RegionGrid.Current.Width);
            Assert.AreEqual(64, RegionGrid.Current.Height);
            Assert.AreEqual(96, RegionGrid.Current.Layer("vision").Width);

            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills");
            Assert.AreNotEqual(777, RegionBootstrap.Current.Context.Seed, "a recipe applies to one Region");
            Assert.AreEqual(RegionRecipe.DefaultWidth, RegionGrid.Current.Width);
            Assert.AreEqual(RegionRecipe.DefaultHeight, RegionBootstrap.Current.Context.Recipe.Height);
        }
    }
}
