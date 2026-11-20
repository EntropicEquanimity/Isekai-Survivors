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
    /// Demon levels, Loyalty and revolt in the real Region: a demon levels from kills, drifts while above Jack's soul level,
    /// turns rogue at zero (taking low-Loyalty demons near it along) and attacks your side, and Jack wins it back by
    /// defeating or possessing it. Timings run at 4x speed.
    /// </summary>
    public class LoyaltyPlayModeTests
    {
        private RegionBuildings _buildings;
        private Building _mainBase;
        private BuildMode _mode;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            RunSession.StartNew();
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            RegionTestScene.ClearOtherSides();   // wildlife and wandering bands would fight the demons and pull Jack off
            yield return TestWait.Until(() => RegionLoyalty.Current != null && RegionPossession.Current != null, "the Region systems");
            _buildings = RegionBuildings.Current;
            _mainBase = Building.All.First(b => b.IsMainBase);
            _mode = Object.FindAnyObjectByType<BuildMode>();
            _jack = Player.Character;
            TraitSystem.AutoRoll = false;   // an Unruly or Cowardly roll would muddy what these tests watch
            EconomyDebug.AddResources(1000);
            TestDebug.Toggle("God mode", true);
            Time.timeScale = 4f;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            TraitSystem.AutoRoll = true;
            yield return null;
        }

        private UnitProducer MakePortal()
        {
            BuildingAsset asset = _mode.Catalogue.First(a => a.DisplayName == "Portal");
            Int2 o = _mainBase.Origin;
            foreach (int d in new[] { 4, -4, 6, -6, 8, -8 })
            foreach (int e in new[] { 0, 4, -4 })
            {
                var cell = new Int2(o.x + d, o.y + e);
                if (!_buildings.Check(asset, cell).IsOk) continue;
                Building portal = _buildings.Place(asset, cell);
                portal.FinishConstruction();
                var producer = portal.GetComponent<UnitProducer>();
                return producer;
            }
            Assert.Fail("no free corrupted ground beside the Main Base");
            return null;
        }

        private IEnumerator Demons(UnitProducer portal, int count, System.Action<DemonLoyalty[]> got)
        {
            yield return TestWait.Until(() => portal.Units.Count >= count, $"the Portal brings {count} Imps", 60f);
            got(portal.Units.Take(count).Select(u => DemonLoyalty.Of(u.gameObject)).ToArray());
        }

        private static void Levelled(DemonLoyalty demon, int level) => demon.AddXp(LoyaltyRules.XpToReach(level) - demon.Xp);

        [UnityTest]
        public IEnumerator A_demon_levels_up_from_the_hostile_units_it_kills()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty demon = null;
            yield return Demons(portal, 1, d => demon = d[0]);
            Assert.IsNotNull(demon, "a Portal's demon has a level and Loyalty");
            Assert.AreEqual(1, demon.Level);
            Assert.IsNull(DemonLoyalty.Of(_jack.gameObject), "Jack's body has none");

            CombatUnit killer = demon.GetComponent<Damageable>().Unit;
            for (int i = 0; i < LoyaltyRules.XpToReach(2); i++)
            {
                GameObject mortal = Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, demon.transform.position + Vector3.right * 2f, Quaternion.identity);
                mortal.GetComponent<Damageable>().Unit.TakeDamage(99999f, source: killer);
            }
            Assert.AreEqual(LoyaltyRules.XpToReach(2), demon.Xp, "each kill is experience");
            Assert.AreEqual(2, demon.Level);
        }

        [UnityTest]
        public IEnumerator Loyalty_drops_while_the_demons_level_is_above_Jacks_soul_level_and_not_otherwise()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty demon = null;
            yield return Demons(portal, 1, d => demon = d[0]);
            Levelled(demon, 2);
            Assert.AreEqual(1, SoulRun.Ledger.Level);
            yield return TestWait.Until(() => demon.Loyalty < LoyaltyTuning.MaxLoyalty - 6f, "Loyalty drops", 20f);

            Assert.IsTrue(SoulRun.Ledger.TryRaiseLevel(_ => true), "Jack's soul level rises to the demon's");
            float at = demon.Loyalty;
            yield return new WaitForSeconds(1f);
            Assert.GreaterOrEqual(demon.Loyalty, at, "level equal to the soul level: no more drain");
        }

        [UnityTest]
        public IEnumerator At_zero_Loyalty_a_demon_turns_rogue_leaves_its_Portal_and_attacks_your_side()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty[] demons = null;
            yield return Demons(portal, 2, d => demons = d);
            DemonLoyalty rebel = demons[0], loyal = demons[1];
            Levelled(rebel, 3);
            rebel.SetLoyalty(1f);
            bool hitYours = false;
            void OnDamage(DamageTaken e) { if (e.Result.Info?.Source?.Team == TeamIds.Rogue && e.Result.Target.Team == TeamIds.Player) hitYours = true; }
            EventCenter.Subscribe<DamageTaken>(OnDamage);
            try
            {
                yield return TestWait.Until(() => rebel.IsRogue, "Loyalty reaches zero", 20f);
                RegionEntity entity = RegionEntities.Living.First(e => e.GameObject == rebel.gameObject);
                Assert.AreEqual(Side.Rogue, entity.Side);
                Assert.IsNull(rebel.GetComponent<DemonBrain>().Portal, "it left its Portal");
                Assert.IsFalse(portal.Units.Contains(rebel.GetComponent<DemonBrain>()), "its place under the cap is free");
                Assert.IsTrue(rebel.GetComponentInChildren<SpriteRenderer>().sharedMaterial == SideOutlineMaterials.MaterialFor(Side.Rogue), "its outline is rogue purple");
                Assert.IsTrue(Teams.AreHostile(rebel.GetComponent<Damageable>().Unit, loyal.GetComponent<Damageable>().Unit), "hostile to your side");

                rebel.transform.position = loyal.transform.position + Vector3.right;
                yield return TestWait.Until(() => hitYours, "the rogue demon attacks your side", 20f);
            }
            finally { EventCenter.Unsubscribe<DamageTaken>(OnDamage); }
        }

        [UnityTest]
        public IEnumerator Nearby_low_Loyalty_demons_follow_a_revolt_but_loyal_and_distant_ones_do_not()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty[] d = null;
            yield return Demons(portal, 4, x => d = x);
            DemonLoyalty rebel = d[0], weak = d[1], loyal = d[2], far = d[3];
            foreach (DemonLoyalty other in new[] { weak, loyal, far }) other.transform.position = rebel.transform.position + Vector3.right * 2f;
            far.transform.position = rebel.transform.position + Vector3.right * (LoyaltyTuning.FollowRadius + 10f);
            weak.SetLoyalty(LoyaltyTuning.FollowBelow - 20f);
            far.SetLoyalty(LoyaltyTuning.FollowBelow - 20f);
            Levelled(rebel, 3);
            rebel.SetLoyalty(0.1f);

            yield return TestWait.Until(() => rebel.IsRogue, "the rebel turns rogue", 20f);
            Assert.IsTrue(weak.IsRogue, "a nearby low-Loyalty demon goes along");
            Assert.IsFalse(loyal.IsRogue, "a loyal one stays");
            Assert.IsFalse(far.IsRogue, "a distant one stays");
            Assert.AreEqual(Side.Rogue, RegionEntities.Living.First(e => e.GameObject == weak.gameObject).Side);
        }

        [UnityTest]
        public IEnumerator Defeating_a_rogue_demon_wins_it_back_instead_of_killing_it()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty demon = null;
            yield return Demons(portal, 1, d => demon = d[0]);
            Assert.IsTrue(demon.MakeRogue());
            Damageable damageable = demon.GetComponent<Damageable>();
            CombatUnit jack = _jack.GetComponent<Damageable>().Unit;

            damageable.Unit.TakeDamage(damageable.MaxHealth * 99f, source: jack);

            Assert.IsFalse(damageable.IsDead, "Jack's side does not kill it");
            Assert.IsFalse(demon.IsRogue, "it surrenders");
            Assert.AreEqual(Side.Yours, RegionEntities.Living.First(e => e.GameObject == demon.gameObject).Side);
            Assert.AreEqual(LoyaltyTuning.WonBackLoyalty, demon.Loyalty);
            Assert.AreEqual(portal, demon.GetComponent<DemonBrain>().Portal, "it goes back to the Portal that has room");
            Assert.IsTrue(demon.GetComponentInChildren<SpriteRenderer>().sharedMaterial == SideOutlineMaterials.MaterialFor(Side.Yours));
        }

        [UnityTest]
        public IEnumerator A_rogue_demon_hurt_only_a_little_stays_rogue()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty demon = null;
            yield return Demons(portal, 1, d => demon = d[0]);
            demon.MakeRogue();
            Damageable damageable = demon.GetComponent<Damageable>();
            damageable.Unit.TakeDamage(damageable.MaxHealth * 0.1f, source: _jack.GetComponent<Damageable>().Unit);
            Assert.IsTrue(demon.IsRogue);
        }

        [UnityTest]
        public IEnumerator Possessing_a_rogue_demon_wins_it_back()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty demon = null;
            yield return Demons(portal, 1, d => demon = d[0]);
            Assert.IsTrue(demon.MakeRogue());
            Assert.AreEqual(Side.Rogue, RegionEntities.Living.First(e => e.GameObject == demon.gameObject).Side);

            Assert.IsTrue(RegionPossession.Current.TryPossess(demon.gameObject), RegionPossession.Current.Refusal);

            Assert.AreEqual(demon.gameObject, Player.Character.gameObject, "Jack is in it");
            Assert.IsFalse(demon.IsRogue);
            Assert.AreEqual(Side.Yours, RegionEntities.Living.First(e => e.GameObject == demon.gameObject).Side);
        }

        [UnityTest]
        public IEnumerator Jacks_body_has_no_Loyalty_and_a_demon_won_back_with_no_Portal_room_guards_near_Jack()
        {
            UnitProducer portal = MakePortal();
            DemonLoyalty demon = null;
            yield return Demons(portal, 1, d => demon = d[0]);
            Assert.IsFalse(_jack.TryGetComponent(out DemonLoyalty _), "no Loyalty on the Character");

            yield return TestWait.Until(() => !portal.HasRoom, "the Portal fills up", 60f);
            demon.MakeRogue();
            demon.transform.position = portal.transform.position + new Vector3(0f, 40f, 0f);   // out of the loyal demons' and Jack's reach, which would beat it into surrender
            yield return TestWait.Until(() => !portal.HasRoom, "the Portal fills the freed slot with a new demon", 30f);
            Vector3 there = _jack.transform.position + Vector3.right * 6f;
            demon.transform.position = there;
            Assert.IsTrue(demon.Reclaim());

            Assert.IsNull(demon.GetComponent<DemonBrain>().Portal, "no Portal has room");
            Assert.IsFalse(demon.IsRogue);
            yield return TestWait.Until(() => Vector2.Distance(demon.transform.position, _jack.transform.position) <= 4.5f, "it guards near Jack", 20f);
        }
    }
}
