using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Possession and Demon King bodies in the real Region: the cost, the new Character, the weaker ordinary demon, summoning.</summary>
    public class PossessionPlayModeTests
    {
        private Stockpile _pile;
        private RegionSouls _souls;
        private RegionPossession _possession;
        private RegionBuildings _buildings;
        private RegionCorruption _corruption;
        private CharacterControl _jack;
        private Building _anchor;
        private BuildMode _mode;
        private Int2 _far, _far2;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            RunSession.StartNew();
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated && RegionSouls.Current != null && RegionPossession.Current != null, "the Region fills");
            yield return TestWait.Until(() => Building.All.Any(b => b.IsMainBase), "the Anchor");
            _pile = RegionEconomy.Current.Stockpile;
            _souls = RegionSouls.Current;
            _possession = RegionPossession.Current;
            _buildings = RegionBuildings.Current;
            _corruption = RegionCorruption.Current;
            _jack = Player.Character;
            _anchor = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            _mode = Object.FindAnyObjectByType<BuildMode>();
            EconomyDebug.AddResources(1000);
            Int2 baseCell = RegionGrid.Current.CellAt(_anchor.transform.position);
            _far = FindSpot(baseCell, null);
            _far2 = FindSpot(baseCell, _far);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private BuildingAsset Catalogue(string name) => _mode.Catalogue.First(a => a.DisplayName == name);

        // A Portal beside the Anchor and the Imp it brings (an ordinary demon).
        private IEnumerator MakeImp(System.Action<GameObject> got)
        {
            Int2 o = _anchor.Origin;
            Int2? spot = null;
            foreach (int d in new[] { 4, -4, 6, -6, 8, -8 })
            foreach (int e in new[] { 0, 4, -4 })
            {
                var c = new Int2(o.x + d, o.y + e);
                if (spot == null && _buildings.Check(Catalogue("Portal"), c).IsOk) spot = c;
            }
            Assert.IsTrue(spot.HasValue, "free corrupted ground beside the Anchor");
            Building portal = _buildings.Place(Catalogue("Portal"), spot.Value);
            portal.FinishConstruction();
            var producer = portal.GetComponent<UnitProducer>();
            Time.timeScale = 4f;
            yield return TestWait.Until(() => producer.Units.Count > 0, "the Portal brings an Imp", 30f);
            Time.timeScale = 1f;
            got(producer.Units[0].gameObject);
        }

        private bool OpenSpot(Int2 c, Int2 baseCell, Int2? other)
        {
            if (System.Math.Max(System.Math.Abs(c.x - baseCell.x), System.Math.Abs(c.y - baseCell.y)) < 20) return false;
            if (other.HasValue && System.Math.Max(System.Math.Abs(c.x - other.Value.x), System.Math.Abs(c.y - other.Value.y)) < 12) return false;
            for (int dx = -3; dx <= 4; dx++)
            for (int dy = -3; dy <= 4; dy++)
                if (!RegionGrid.Current.IsWalkable(new Int2(c.x + dx, c.y + dy))) return false;
            return true;
        }

        private Int2 FindSpot(Int2 baseCell, Int2? other)
        {
            RegionGrid g = RegionGrid.Current;
            for (int x = g.Width - 1; x >= 0; x--)
            for (int y = g.Height - 1; y >= 0; y--)
                if (OpenSpot(new Int2(x, y), baseCell, other)) return new Int2(x, y);
            Assert.Fail("no open ground far from the Anchor");
            return default;
        }

        private static void Teleport(Component thing, Vector3 position)
        {
            thing.transform.position = position;
            if (thing.TryGetComponent(out Rigidbody2D body)) body.position = position;
            Physics2D.SyncTransforms();
        }

        private static float DamageBonus(Component body) =>
            body.GetComponent<Damageable>().Stats.GetStatValue(nameof(StatId.DamageBonus), 0f);

        [UnityTest]
        public IEnumerator Jack_starts_in_a_Demon_King_and_it_spreads_corruption()
        {
            Assert.IsTrue(DemonKing.IsKing(_jack), "Jack starts as a Demon King");
            Teleport(_jack, RegionGrid.Current.CentreOf(_far));
            Time.timeScale = 4f;
            yield return TestWait.Until(() => _corruption.Level(_far) > 0, "a Demon King body corrupts the ground it walks on", 15f);
        }

        [UnityTest]
        public IEnumerator Possessing_while_alive_costs_a_quarter_less_and_the_demon_becomes_Jack()
        {
            GameObject imp = null;
            yield return MakeImp(g => imp = g);
            _pile.Add(ResourceType.Souls, 300);
            int before = _pile.Get(ResourceType.Souls);
            Assert.IsFalse(_souls.IsBodiless);

            Assert.IsTrue(_possession.TryPossess(imp));

            Assert.AreEqual(before - SoulRules.PossessCost(1, bodyAlive: true), _pile.Get(ResourceType.Souls), "83 Souls at soul level 1");
            CharacterControl body = Player.Character;
            Assert.AreSame(imp, body.gameObject, "the Imp is the Player's Character");
            Assert.AreEqual(body.transform, _souls.CameraTarget, "the camera follows the new body");
            Assert.IsTrue(body.enabled && ((Behaviour)body.GetComponent("PlayerBrain")).enabled, "the player steers it");
            Assert.IsFalse(imp.GetComponent<DemonBrain>().enabled, "its own brain stands down");
            Assert.IsFalse(DemonKing.IsKing(body));

            // The old body is an ordinary unit of his again, and the inventory stays usable.
            Assert.IsFalse(_jack.enabled, "not steered any more");
            Assert.IsTrue(_jack.TryGetComponent(out DemonBrain brain) && brain.enabled, "the old Demon King fights on its own");
            Assert.IsFalse(_jack.IsDead);
            Assert.IsNotNull(CharacterInventory.Current, "the grid inventory still works");
        }

        [UnityTest]
        public IEnumerator Jack_in_an_ordinary_demon_is_weaker_and_spreads_no_corruption()
        {
            GameObject imp = null;
            yield return MakeImp(g => imp = g);
            _pile.Add(ResourceType.Souls, 300);
            Assert.IsTrue(_possession.TryPossess(imp));
            Teleport(imp.transform, RegionGrid.Current.CentreOf(_far2));
            Time.timeScale = 4f;
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(-SoulTuning.OrdinaryDemonPenalty, DamageBonus(imp.transform) - CorruptionOffset(imp), 1e-3f, "30% less damage");
            yield return new WaitForSeconds(3.5f);   // far longer than a Demon King needs to corrupt a cell
            Assert.AreEqual(0, _corruption.Level(_far2), "an ordinary demon spreads none");
        }

        // Off corrupted ground every demon carries the corruption weakness too; take it out of the reading.
        private static float CorruptionOffset(GameObject body) =>
            CorruptionWeakness.Current != null && CorruptionWeakness.Current.IsWeak(FindEntity(body)) ? CorruptionWeakness.Penalty : 0f;

        private static RegionEntity FindEntity(GameObject go) => RegionEntities.Living.FirstOrDefault(u => u.GameObject == go);

        [UnityTest]
        public IEnumerator A_wisp_touching_a_demon_possesses_it_for_the_full_cost()
        {
            GameObject imp = null;
            yield return MakeImp(g => imp = g);
            _pile.Add(ResourceType.Souls, 300);
            Vector3 away = RegionGrid.Current.CentreOf(_far);
            Teleport(_jack, away);
            int before = _pile.Get(ResourceType.Souls);
            _jack.GetComponent<Damageable>().Kill();

            yield return TestWait.Until(() => _souls.IsBodiless, "the wisp appears", 5f);
            // The wisp flies to the Imp (the Imp keeps walking about, so follow it).
            yield return TestWait.Until(() =>
            {
                if (_souls.Wisp != null) _souls.Wisp.transform.position = imp.transform.position;
                return Player.Character != null && Player.Character.gameObject == imp;
            }, "the wisp possesses the Imp it touches", 5f);

            int spent = before - _pile.Get(ResourceType.Souls);
            int cost = SoulRules.PossessCost(1, bodyAlive: false);
            Assert.GreaterOrEqual(spent, cost);
            Assert.LessOrEqual(spent, cost + 3, "the full price, and only a moment of drain");
            Assert.IsFalse(_souls.IsBodiless);
            Assert.IsNull(Object.FindAnyObjectByType<Wisp>(), "the wisp is gone");
            Assert.AreEqual(imp.transform, _souls.CameraTarget);
            Assert.IsTrue(_jack.IsDead, "the dead Demon King stays where it fell");
        }

        [UnityTest]
        public IEnumerator Without_the_Souls_possession_is_refused_and_says_why()
        {
            GameObject imp = null;
            yield return MakeImp(g => imp = g);
            _pile.TrySpend(ResourceType.Souls, _pile.Get(ResourceType.Souls));
            _pile.Add(ResourceType.Souls, 10);

            Assert.IsFalse(_possession.TryPossess(imp));

            StringAssert.Contains("Souls", _possession.Refusal);
            StringAssert.Contains(SoulRules.PossessCost(1, bodyAlive: true).ToString(), _possession.Refusal);
            Assert.AreEqual(10, _pile.Get(ResourceType.Souls), "nothing is spent");
            Assert.AreSame(_jack, Player.Character);
            Assert.IsTrue(imp.GetComponent<DemonBrain>().enabled);
        }

        [UnityTest]
        public IEnumerator A_Demon_King_is_summoned_at_the_Anchor_for_Souls_and_possessing_it_keeps_Jack_whole()
        {
            int kings = Object.FindObjectsByType<DemonKing>(FindObjectsInactive.Exclude).Length;
            _pile.TrySpend(ResourceType.Souls, _pile.Get(ResourceType.Souls));
            Assert.IsNull(_possession.TrySummonDemonKing(), "no Souls, no King");
            StringAssert.Contains("Souls", _possession.Refusal);

            _pile.Add(ResourceType.Souls, 500);
            GameObject king = _possession.TrySummonDemonKing();
            Assert.IsNotNull(king);
            Assert.AreEqual(500 - SoulRules.SummonKingCost(1), _pile.Get(ResourceType.Souls));
            Assert.AreEqual(kings + 1, Object.FindObjectsByType<DemonKing>(FindObjectsInactive.Exclude).Length);
            Assert.AreSame(_jack, Player.Character, "a summoned King is not Jack until he possesses it");
            yield return null;

            Assert.IsTrue(_possession.TryPossess(king));
            Assert.AreSame(king, Player.Character.gameObject);
            Assert.IsTrue(DemonKing.IsKing(Player.Character));
            Assert.AreEqual(0f, DamageBonus(king.transform), 1e-3f, "full strength in a Demon King");

            Teleport(king.transform, RegionGrid.Current.CentreOf(_far));
            Time.timeScale = 4f;
            yield return TestWait.Until(() => _corruption.Level(_far) > 0, "this Demon King spreads corruption too", 15f);
        }

        [UnityTest]
        public IEnumerator Summoning_needs_an_Anchor()
        {
            _pile.Add(ResourceType.Souls, 500);
            int souls = _pile.Get(ResourceType.Souls);
            _anchor.GetComponent<Damageable>().Kill();
            yield return null;
            Assert.IsNull(_possession.TrySummonDemonKing());
            StringAssert.Contains("Anchor", _possession.Refusal);
            Assert.AreEqual(souls, _pile.Get(ResourceType.Souls), "nothing is spent");
        }

        [UnityTest]
        public IEnumerator The_debug_rows_possess_the_nearest_demon_and_summon_a_King()
        {
            GameObject imp = null;
            yield return MakeImp(g => imp = g);
            _pile.Add(ResourceType.Souls, 600);
            TestDebug.Press("Summon Demon King");
            Assert.AreEqual(2, Object.FindObjectsByType<DemonKing>(FindObjectsInactive.Exclude).Length, "Jack and the new King");
            TestDebug.Press("Possess nearest demon");
            Assert.AreNotSame(_jack, Player.Character, "Jack moved into a demon");
            Assert.IsNotNull(imp);
        }
    }
}
