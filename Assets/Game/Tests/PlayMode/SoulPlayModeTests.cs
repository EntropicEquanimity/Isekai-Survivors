using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.FlowFields;
using Blondie.Pooling;
using Blondie.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The soul economy in the real Region: Jack's body dying, the wisp, reviving at the Anchor, soul level and losing.</summary>
    public class SoulPlayModeTests
    {
        private Stockpile _pile;
        private RegionSouls _souls;
        private CharacterControl _body;
        private Building _anchor;
        private RegionDefeat _defeat;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            RunSession.StartNew();   // a new Run: soul level 1, no deaths
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated && RegionSouls.Current != null, "the Region fills");
            yield return TestWait.Until(() => Building.All.Any(b => b.IsMainBase), "the Anchor");
            _pile = RegionEconomy.Current.Stockpile;
            _souls = RegionSouls.Current;
            _body = Player.Character;
            _anchor = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            _defeat = Object.FindAnyObjectByType<RegionDefeat>();
        }

        // Puts the body on the Anchor, so a wisp made there touches it at once.
        private void BodyAtAnchor() => _body.Teleport(_anchor.transform.position);

        private void BodyFarFromAnchor() => _body.Teleport(_anchor.transform.position + new Vector3(15f, 0f, 0f));

        private void KillBody() => _body.GetComponent<Damageable>().Kill();

        [UnityTest]
        public IEnumerator Killing_mortals_gives_Souls()
        {
            GameObject prefab = Object.FindAnyObjectByType<HordeSpawner>().Prefab;
            GameObject mortal = Pools.Spawn(prefab, _body.transform.position + new Vector3(6f, 0f, 0f), Quaternion.identity);
            yield return null;
            int before = _pile.Get(ResourceType.Souls);
            mortal.GetComponent<HordeMember>().Unit.TakeDamage(100000f, DamageRules.PhysicalType, _body.GetComponent<Damageable>().Unit);
            Assert.AreEqual(before + SoulRules.SoulsForKill(UnitKind.Mortal), _pile.Get(ResourceType.Souls));
        }

        [UnityTest]
        public IEnumerator Jacks_body_dying_makes_a_wisp_that_drains_Souls_and_says_so()
        {
            _pile.Add(ResourceType.Souls, 100);
            BodyFarFromAnchor();
            KillBody();
            yield return TestWait.Until(() => _souls.IsBodiless, "the wisp appears");

            Wisp wisp = _souls.Wisp;
            Assert.IsNull(wisp.GetComponent<Collider2D>(), "walls don't stop it");
            Assert.IsNull(wisp.GetComponent<Damageable>(), "ordinary units can't hit it");
            Assert.IsFalse(RegionEntities.Living.Any(u => u.GameObject == wisp.gameObject), "not a unit the horde sees");
            Assert.AreEqual(1, SoulRun.Ledger.Deaths);
            Assert.AreEqual(wisp.transform, _souls.CameraTarget, "the camera follows the wisp");

            yield return TestWait.Until(() => _pile.Get(ResourceType.Souls) < 100, "Souls drain", 5f);
            var hud = Object.FindAnyObjectByType<SoulsHud>();
            yield return TestWait.Until(() => hud.DrainText.Contains("Souls/s"), "the HUD shows the drain");
            StringAssert.Contains("over the Anchor", hud.StatusText);
            Assert.IsFalse(_defeat.IsDefeated, "a bodiless Jack with Souls left is not lost");
        }

        [UnityTest]
        public IEnumerator The_wisp_is_steered_with_the_move_input()
        {
            _pile.Add(ResourceType.Souls, 100);
            BodyFarFromAnchor();
            KillBody();
            yield return TestWait.Until(() => _souls.IsBodiless, "the wisp appears");
            Wisp wisp = _souls.Wisp;
            Vector3 start = wisp.transform.position;
            wisp.Steer = new Vector2(0f, 1f);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(wisp.transform.position.y, start.y + 1f, "it flew up");
        }

        [UnityTest]
        public IEnumerator Pressing_R_over_the_Anchor_revives_Jack_for_the_revive_cost_and_touching_alone_does_not()
        {
            _pile.Add(ResourceType.Souls, 100);
            BodyAtAnchor();
            KillBody();
            yield return TestWait.Until(() => _souls.IsBodiless, "the wisp appears");
            Assert.IsTrue(_souls.WispAtAnchor);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(_souls.IsBodiless, "over the Anchor, nothing happens until R");
            yield return TestWait.Until(() =>
            {
                if (_souls.IsBodiless) BodyKey.Press();
                return !_body.IsDead && !_souls.IsBodiless;
            }, "Jack revives at the Anchor");

            Assert.AreEqual(100 - SoulRules.ReviveCost(1), _pile.Get(ResourceType.Souls), "22 Souls at soul level 1");
            Assert.AreEqual(1, SoulRun.Ledger.Deaths);
            Assert.IsTrue(_body.GetComponent<Damageable>().Health > 0f);
            Assert.AreSame(_body, Player.Character);
            Assert.AreEqual(_body.transform, _souls.CameraTarget, "the camera is back on the body");
            Assert.IsNull(Object.FindAnyObjectByType<Wisp>(), "the wisp is gone");
            Assert.IsFalse(_defeat.IsDefeated);
        }

        [UnityTest]
        public IEnumerator A_wisp_without_the_Souls_cannot_revive_and_the_HUD_says_why()
        {
            _pile.Add(ResourceType.Souls, 5);
            BodyAtAnchor();
            KillBody();
            var hud = Object.FindAnyObjectByType<SoulsHud>();
            yield return TestWait.Until(() => hud.StatusText.Contains("needs"), "the HUD says Souls are short");
            Assert.IsTrue(_souls.IsBodiless);
        }

        [UnityTest]
        public IEnumerator Soul_level_is_bought_with_Souls_raises_Jacks_stats_and_the_revive_cost()
        {
            var stats = _body.GetComponent<StatBlock>();
            float health = stats.GetValue(StatId.HealthMax.ToString());
            _pile.Add(ResourceType.Souls, 200);
            TestDebug.Press("Raise soul level");
            Assert.AreEqual(2, SoulRun.Ledger.Level);
            Assert.AreEqual(150, _pile.Get(ResourceType.Souls), "50 x level 1");
            yield return null;
            Assert.Greater(stats.GetValue(StatId.HealthMax.ToString()), health, "soul level gives stats");

            BodyAtAnchor();
            KillBody();
            yield return TestWait.Until(() =>
            {
                if (_souls.IsBodiless) BodyKey.Press();
                return !_body.IsDead && !_souls.IsBodiless;
            }, "Jack revives");
            Assert.AreEqual(150 - SoulRules.ReviveCost(2), _pile.Get(ResourceType.Souls), "the cost rose with soul level");
        }

        [UnityTest]
        public IEnumerator Soul_level_cannot_be_bought_without_the_Souls()
        {
            _pile.Add(ResourceType.Souls, 49);
            TestDebug.Press("Raise soul level");
            Assert.AreEqual(1, SoulRun.Ledger.Level);
            Assert.AreEqual(49, _pile.Get(ResourceType.Souls));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Losing_the_Anchor_does_not_end_the_Run_but_a_bodiless_Jack_cannot_revive()
        {
            _pile.Add(ResourceType.Souls, 200);
            _anchor.Damageable.Unit.TakeDamage(100000f, DamageRules.TrueType);
            yield return TestWait.Until(() => !_anchor.IsAlive, "the Anchor falls");
            Assert.IsFalse(_defeat.IsDefeated, "the Run goes on");

            BodyAtAnchor();
            KillBody();
            var hud = Object.FindAnyObjectByType<SoulsHud>();
            yield return TestWait.Until(() => hud.StatusText.Contains("No Anchor"), "the HUD says there is no Anchor");
            StringAssert.Contains("possess", hud.StatusText, "and still says possessing a demon is a way back");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(_souls.IsBodiless, "no revive without an Anchor");
            Assert.IsTrue(_body.IsDead);
            Assert.IsFalse(_defeat.IsDefeated, "still has Souls");

            _pile.TrySpend(ResourceType.Souls, _pile.Get(ResourceType.Souls));
            yield return TestWait.Until(() => _defeat.IsDefeated, "the Souls run out and the Run is lost", 3f);
        }

        [UnityTest]
        public IEnumerator Souls_running_out_while_bodiless_loses_the_Run()
        {
            _pile.Add(ResourceType.Souls, 3);
            BodyFarFromAnchor();
            KillBody();
            yield return TestWait.Until(() => _defeat.IsDefeated, "the Souls drain to nothing", 10f);
            Assert.AreEqual(0, _pile.Get(ResourceType.Souls));
        }

        [UnityTest]
        public IEnumerator The_body_dying_with_exactly_zero_Souls_loses_the_Run_at_once()
        {
            Assert.AreEqual(0, _pile.Get(ResourceType.Souls));
            BodyAtAnchor();
            KillBody();
            yield return TestWait.Until(() => _defeat.IsDefeated, "lost the moment he dies with no Souls", 3f);
            Assert.IsTrue(_body.IsDead, "no revive for nothing");
        }
    }
}
