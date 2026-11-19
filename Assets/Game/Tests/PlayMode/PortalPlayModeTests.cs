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
    /// <summary>
    /// Portals in the real Region: standing orders (Guard, Raid, Follow), the bones policy, and the bone demon that
    /// raises skeletons. Timings run at 4x speed; skeletons are given a short life.
    /// </summary>
    public class PortalPlayModeTests
    {
        private RegionBuildings _buildings;
        private RegionGrid _grid;
        private RegionBones _bones;
        private Building _mainBase;
        private BuildMode _mode;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return null;
            _buildings = RegionBuildings.Current;
            _grid = RegionGrid.Current;
            _bones = RegionBones.Current;
            _mainBase = Building.All.First(b => b.IsMainBase);
            _mode = Object.FindAnyObjectByType<BuildMode>();
            _jack = Player.Character;
            EconomyDebug.AddResources(1000);
            TestDebug.Toggle("God mode", true);
            Time.timeScale = 4f;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            PortalMarking.Disarm();
            yield return null;
        }

        private BuildingAsset PortalAsset => _mode.Catalogue.First(a => a.DisplayName == "Portal");

        // A finished Portal on free corrupted ground beside the Main Base (the start corner is generated, so look around it).
        private UnitProducer MakePortal(DemonType roster = DemonType.Imp)
        {
            Int2 o = _mainBase.Origin;
            foreach (int d in new[] { 4, -4, 6, -6, 8, -8 })
            foreach (int e in new[] { 0, 4, -4 })
            {
                var cell = new Int2(o.x + d, o.y + e);
                if (!_buildings.Check(PortalAsset, cell).IsOk) continue;
                Building portal = _buildings.Place(PortalAsset, cell);
                var producer = portal.GetComponent<UnitProducer>();
                producer.Roster = roster;
                portal.FinishConstruction();
                return producer;
            }
            Assert.Fail("no free corrupted ground beside the Main Base");
            return null;
        }

        // Open ground about `distance` from a point with a clear straight walk to it (the demons walk straight).
        private Vector3 ClearSpot(Vector3 from, float distance)
        {
            for (float d = distance; d >= 5f; d -= 2f)
            for (int i = 0; i < 360; i += 10)
            {
                float angle = i * Mathf.Deg2Rad;
                Vector3 spot = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * d;
                if (!_grid.Contains(_grid.CellAt(spot)) || !IsOpen(spot)) continue;
                bool clear = true;
                for (float t = 0f; t <= 1.001f && clear; t += 0.05f) clear = (Vector3.Lerp(from, spot, t) - from).magnitude < 2.2f || _grid.IsWalkable(Vector3.Lerp(from, spot, t));   // the start may be a Building's own ground
                if (clear) return spot;
            }
            Assert.Fail("no clear ground near " + from);
            return from;
        }

        private bool IsOpen(Vector3 at) => _grid.IsWalkable(at) && _grid.IsWalkable(at + Vector3.right * 0.5f) && _grid.IsWalkable(at + Vector3.up * 0.5f)
                                          && _grid.IsWalkable(at - Vector3.right * 0.5f) && _grid.IsWalkable(at - Vector3.up * 0.5f);

        private static float Distance(Component a, Vector3 b) => Vector2.Distance(a.transform.position, b);

        private static bool AllNear(UnitProducer portal, Vector3 at, float radius) =>
            portal.Units.Count > 0 && portal.Units.All(u => Distance(u, at) <= radius);

        // The ring of posts is DemonTuning.RingRadius wide; a little over that counts as arrived.
        private const float Arrived = DemonTuning.RingRadius + 1.5f;

        [UnityTest]
        public IEnumerator A_Raid_order_sends_the_Portals_demons_to_the_mark()
        {
            UnitProducer portal = MakePortal();
            yield return TestWait.Until(() => portal.Units.Count >= 2, "the Portal brings two Imps", 30f);
            Assert.AreEqual(PortalOrder.Guard, portal.Order, "Guard until ordered otherwise");

            Vector3 mark = ClearSpot(portal.transform.position, 20f);
            Assert.IsTrue(PortalMarking.Mark(portal, PortalMark.Raid, mark), "the mark is on the map");
            Assert.AreEqual(PortalOrder.Raid, portal.Order, "marking a target orders the Raid");
            Assert.IsTrue(portal.HasRaidTarget);

            yield return TestWait.Until(() => AllNear(portal, mark, Arrived), "the demons reach the mark", 30f);
            Assert.IsTrue(portal.Units.All(u => u.Order == PortalOrder.Raid));
        }

        [UnityTest]
        public IEnumerator Guard_keeps_the_demons_near_their_rally_point_and_brings_back_those_that_stray()
        {
            UnitProducer portal = MakePortal();
            yield return TestWait.Until(() => portal.Units.Count >= 2, "the Portal brings two Imps", 30f);
            Vector3 rally = ClearSpot(portal.transform.position, 9f);
            Assert.IsTrue(PortalMarking.Mark(portal, PortalMark.Rally, rally));
            yield return TestWait.Until(() => AllNear(portal, rally, Arrived), "the demons gather at the rally point", 30f);

            DefenderBrain stray = portal.Units[0];
            Vector3 away = ClearSpot(rally, 14f);
            stray.transform.position = away;
            yield return TestWait.Until(() => Distance(stray, rally) <= Arrived, "the stray comes back to its post", 30f);
            Assert.AreEqual(PortalOrder.Guard, stray.Order);
        }

        [UnityTest]
        public IEnumerator Follow_keeps_the_demons_near_Jack()
        {
            UnitProducer portal = MakePortal();
            yield return TestWait.Until(() => portal.Units.Count >= 2, "the Portal brings two Imps", 30f);
            portal.Order = PortalOrder.Follow;
            Vector3 there = ClearSpot(portal.transform.position, 16f);
            _jack.Teleport(there);
            _jack.enabled = false;   // standing still: his own auto-fight would walk him back to where he idled
            yield return TestWait.Until(() => AllNear(portal, _jack.transform.position, Arrived), "the demons gather round Jack", 30f);

            Assert.IsTrue(portal.Units.All(u => u.Order == PortalOrder.Follow));
        }

        [UnityTest]
        public IEnumerator A_demon_fights_a_mortal_that_comes_near_its_post()
        {
            UnitProducer portal = MakePortal();
            yield return TestWait.Until(() => portal.Units.Count >= 2, "the Portal brings two Imps", 30f);
            yield return TestWait.Until(() => AllNear(portal, portal.Rally, Arrived), "they settle at the Portal", 30f);

            Vector3 at = ClearSpot(portal.transform.position, 5f);
            HordeMember mortal = Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, at, Quaternion.identity).GetComponent<HordeMember>();
            CombatUnit unit = mortal.GetComponent<Damageable>().Unit;
            yield return TestWait.Until(() => portal.Units.Any(u => u.Action == BehaviourAction.Fight) || unit.IsDead, "a demon goes for it", 15f);
            yield return TestWait.Until(() => unit.IsDead, "the demons kill it", 30f);
        }

        [UnityTest]
        public IEnumerator Bones_policy_Destroy_removes_piles_near_the_demons_and_Ignore_and_Harvest_leave_them_to_an_Imp()
        {
            UnitProducer portal = MakePortal();
            yield return TestWait.Until(() => portal.Units.Count >= 1, "the Portal brings an Imp", 30f);
            DefenderBrain imp = portal.Units[0];
            Vector3 beside = imp.transform.position + Vector3.right;

            BonePile ignored = _bones.Drop(beside, BoneKind.Human);
            portal.Policy = BonesPolicy.Ignore;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(ignored.IsAvailable, "Ignore leaves the pile");
            portal.Policy = BonesPolicy.Harvest;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(ignored.IsAvailable, "an Imp has no use for bones under Harvest");

            portal.Policy = BonesPolicy.Destroy;
            yield return TestWait.Until(() => ignored.Fate == BoneFate.Removed, "Destroy removes the pile beside the demon", 10f);

            BonePile far = _bones.Drop(imp.transform.position + Vector3.right * (DemonTuning.DestroyRadius + 6f), BoneKind.Human);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(far.IsAvailable, "a pile far from every demon stays");
        }

        [UnityTest]
        public IEnumerator A_bone_demon_spends_a_pile_once_to_raise_a_skeleton_that_crumbles()
        {
            UnitProducer portal = MakePortal(DemonType.BoneDemon);
            yield return TestWait.Until(() => portal.Units.Count >= 1, "the Portal brings a bone demon", 30f);
            DefenderBrain demon = portal.Units[0];
            Assert.AreEqual(DemonType.BoneDemon, demon.Type);
            var raiser = demon.GetComponent<BoneRaiser>();
            raiser.SkeletonSeconds = 3f;
            yield return TestWait.Until(() => AllNear(portal, portal.Rally, Arrived), "it settles at the Portal", 30f);

            BonePile pile = _bones.Drop(demon.transform.position + new Vector3(2.5f, 0f, 0f), BoneKind.Human);
            portal.Policy = BonesPolicy.Ignore;
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(pile.IsAvailable, "it leaves the pile when the policy is Ignore");
            Assert.AreEqual(0, raiser.Raised);

            portal.Policy = BonesPolicy.Harvest;
            yield return TestWait.Until(() => raiser.Raised == 1, "it raises a skeleton from the pile", 20f);
            Assert.AreEqual(BoneFate.Taken, pile.Fate, "the pile is spent");
            Assert.IsFalse(_bones.Take(pile), "a pile is used once");
            Skeleton skeleton = Object.FindObjectsByType<Skeleton>(FindObjectsInactive.Exclude).Single();
            Assert.AreEqual("Players", skeleton.GetComponent<Damageable>().Unit.Team, "it fights on your side");
            Assert.IsNotNull(skeleton.GetComponent<DefenderBrain>().Portal, "under the Portal's orders");

            yield return TestWait.Until(() => !skeleton.isActiveAndEnabled, "the skeleton crumbles", 20f);
            Assert.AreEqual(0, raiser.Alive);
        }
    }
}
