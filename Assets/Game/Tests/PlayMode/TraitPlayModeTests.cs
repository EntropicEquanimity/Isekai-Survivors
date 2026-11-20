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
    /// <summary>
    /// Traits in the real Region: rolled at spawn, their weights on the shared behaviours (mortals and demons), their buffs,
    /// and Sneaky units. Most tests put Traits on by hand (<see cref="TraitSystem.Give"/>) so no roll interferes.
    /// </summary>
    public class TraitPlayModeTests
    {
        private RegionMortals _mortals;
        private RegionSettlements _settlements;
        private RegionGrid _grid;
        private RegionBuildings _buildings;
        private Building _mainBase;
        private BuildMode _mode;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            _mortals = RegionMortals.Current;
            _settlements = RegionSettlements.Current;
            _grid = RegionGrid.Current;
            _buildings = RegionBuildings.Current;
            _mainBase = Building.All.First(b => b.IsMainBase);
            _mode = Object.FindAnyObjectByType<BuildMode>();
            _jack = Player.Character;
            TraitSystem.AutoRoll = false;
            TestDebug.Toggle("God mode", true);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            TraitSystem.AutoRoll = true;
            Time.timeScale = 1f;
            yield return null;
        }

        private Vector3 FreeSpotNear(Vector3 from, float distance)
        {
            for (int i = 0; i < 96; i++)
            {
                float angle = i * 0.61f;
                Vector3 spot = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (distance + i * 0.03f);
                if (_grid.IsWalkable(spot) && _grid.IsWalkable(spot + Vector3.right) && _grid.IsWalkable(spot + Vector3.up) && _grid.IsWalkable(spot - Vector3.right) && _grid.IsWalkable(spot - Vector3.up)) return spot;
            }
            Assert.Fail("no free ground near " + from);
            return from;
        }

        // Open ground 8 to 13 cells from Jack that your side sees (his sight is 14) but a mortal there cannot see him (7).
        private Vector3 SightSpot()
        {
            for (float d = 8f; d <= 13f; d += 0.5f)
            for (int i = 0; i < 360; i += 15)
            {
                float a = i * Mathf.Deg2Rad;
                Vector3 p = _jack.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * d;
                if (!_grid.Contains(_grid.CellAt(p)) || !_grid.IsWalkable(p) || !_grid.IsWalkable(p + Vector3.right * 2f) || !_grid.IsWalkable(p - Vector3.right * 2f)) continue;
                if (RegionVision.Current.CanSee(Side.Yours, p) && RegionVision.Current.CanSee(Side.Yours, p + Vector3.right * 2f)) return p;
            }
            Assert.Fail("no ground in Jack's sight at that distance");
            return default;
        }

        private Vector3 Centre(RegionSettlements.Settlement s) =>
            _grid.CentreOf(new Int2((s.Plan.Interior.MinX + s.Plan.Interior.MaxX) / 2, (s.Plan.Interior.MinY + s.Plan.Interior.MaxY) / 2));

        private MortalBrain Spawn(MortalType type, Vector3 at)
        {
            RegionSettlements.Settlement home = _settlements.Settlements.OrderBy(s => (Centre(s) - at).sqrMagnitude).First();
            MortalBrain brain = _mortals.Spawn(type, home, at);
            Assert.IsNotNull(brain, "a " + type + " spawns");
            return brain;
        }

        private static void Hurt(Damageable d, float fraction) => d.Unit.TakeDamage(d.MaxHealth * fraction, DamageRules.TrueType);

        private static void Give(GameObject unit, params TraitId[] traits) => Assert.IsTrue(TraitSystem.Current.Give(unit, traits), "the traits go on " + unit.name);

        // ---- Demons: a Portal's Imp ----

        private BuildingAsset PortalAsset => _mode.Catalogue.First(a => a.DisplayName == "Portal");

        private UnitProducer MakePortal()
        {
            EconomyDebug.AddResources(1000);
            Int2 o = _mainBase.Origin;
            foreach (int d in new[] { 4, -4, 6, -6, 8, -8 })
            foreach (int e in new[] { 0, 4, -4 })
            {
                var cell = new Int2(o.x + d, o.y + e);
                if (!_buildings.Check(PortalAsset, cell).IsOk) continue;
                Building portal = _buildings.Place(PortalAsset, cell);
                var producer = portal.GetComponent<UnitProducer>();
                producer.Roster = DemonType.Imp;
                portal.FinishConstruction();
                return producer;
            }
            Assert.Fail("no free corrupted ground beside the Main Base");
            return null;
        }

        private IEnumerator AnImp(System.Action<DemonBrain> got)
        {
            Time.timeScale = 4f;
            UnitProducer portal = MakePortal();
            yield return TestWait.Until(() => portal.Units.Count >= 1, "the Portal brings an Imp", 30f);
            DemonBrain imp = portal.Units[0];
            yield return TestWait.Until(() => Vector2.Distance(imp.transform.position, portal.Rally) <= DemonTuning.SlotRadius + 1.5f, "the Imp settles at the Portal", 30f);
            got(imp);
        }

        // ---- Weights ----

        [UnityTest]
        public IEnumerator A_Cowardly_mortal_flees_below_75_percent_health_and_not_above()
        {
            MortalBrain coward = Spawn(MortalType.Soldier, FreeSpotNear(Centre(_settlements.Seat), 6f));
            Give(coward.gameObject, TraitId.Cowardly);
            yield return new WaitForSeconds(1.5f);
            Assert.AreNotEqual(BehaviourAction.Flee, coward.Action, "at full health it does not flee");

            Hurt(coward.Member.Damageable, 0.15f);   // 85% of its (boosted) health left
            yield return new WaitForSeconds(1.5f);
            Assert.AreNotEqual(BehaviourAction.Flee, coward.Action, "at 85% it holds");

            Hurt(coward.Member.Damageable, 0.15f);   // about 70%
            yield return TestWait.Until(() => coward.Action == BehaviourAction.Flee, "it flees below 75%", 5f);
        }

        [UnityTest]
        public IEnumerator A_Cowardly_demon_flees_from_the_hostile_unit_instead_of_standing_still()
        {
            DemonBrain imp = null;
            yield return AnImp(i => imp = i);
            Give(imp.gameObject, TraitId.Cowardly);
            Vector3 at = imp.transform.position;
            Vector3 foeAt = FreeSpotNear(at, 3f);
            MortalBrain foe = Spawn(MortalType.Soldier, foeAt);
            _mortals.Unregister(foe);                       // it stands where it is
            foe.Member.Damageable.Invulnerable = true;      // and the Imp's weapons cannot end the test
            Hurt(imp.GetComponent<Damageable>(), 0.5f);
            float before = Vector2.Distance(imp.transform.position, foeAt);

            yield return TestWait.Until(() => imp.Action == BehaviourAction.Flee, "the hurt Imp decides to flee", 10f);
            yield return TestWait.Until(() => Vector2.Distance(imp.transform.position, foeAt) > before + 2f, "it runs away from the hostile unit", 10f);
        }

        [UnityTest]
        public IEnumerator A_Berserk_unit_never_flees_while_an_ordinary_one_in_its_place_does()
        {
            Vector3 near = FreeSpotNear(_jack.transform.position, 5f);
            MortalBrain ordinary = Spawn(MortalType.Soldier, near);
            MortalBrain berserk = Spawn(MortalType.Soldier, FreeSpotNear(near, 1.5f));
            Give(berserk.gameObject, TraitId.Berserk);
            Hurt(ordinary.Member.Damageable, 0.9f);
            Hurt(berserk.Member.Damageable, 0.9f);   // both at about 10% with Jack in sight

            bool ordinaryFled = false;
            float end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end && berserk.Member.IsAlive)
            {
                Assert.AreNotEqual(BehaviourAction.Flee, berserk.Action, "a Berserk soldier fights on");
                ordinaryFled |= ordinary.Action == BehaviourAction.Flee;
                yield return null;
            }
            Assert.IsTrue(ordinaryFled || !ordinary.Member.IsAlive, "the ordinary soldier, hurt and afraid, ran");
        }

        [UnityTest]
        public IEnumerator A_Brave_soldier_holds_where_an_ordinary_one_flees_until_ten_percent()
        {
            Vector3 near = FreeSpotNear(_jack.transform.position, 5f);
            MortalBrain brave = Spawn(MortalType.Soldier, near);
            Give(brave.gameObject, TraitId.Brave);
            Hurt(brave.Member.Damageable, 0.7f / 1f);   // 30% left
            float end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end && brave.Member.IsAlive)
            {
                Assert.AreNotEqual(BehaviourAction.Flee, brave.Action, "a Brave soldier does not flee at 30%");
                yield return null;
            }
        }

        // ---- Buffs ----

        [UnityTest]
        public IEnumerator Buffs_change_stats_for_mortals_and_demons()
        {
            // A mortal has no Stat Block: it gets one, so Combat's rules read its armour and damage bonus; max health is its Damageable's.
            MortalBrain soldier = Spawn(MortalType.Soldier, FreeSpotNear(Centre(_settlements.Seat), 6f));
            Damageable d = soldier.Member.Damageable;
            float baseMax = d.MaxHealth;
            Give(soldier.gameObject, TraitId.Cowardly);
            Assert.AreEqual(baseMax * 1.3f, d.MaxHealth, 0.01f, "Cowardly: +30% max health");
            Assert.AreEqual(d.MaxHealth, d.Health, 0.01f, "it comes in at its new full health");
            Give(soldier.gameObject, TraitId.Brave);
            Assert.AreEqual(baseMax, d.MaxHealth, 0.01f, "the old Trait's health goes with it");
            Assert.AreEqual(10f, d.Stats.GetStatValue(nameof(StatId.Armor)), 0.001f, "Brave: armour");
            Give(soldier.gameObject, TraitId.Berserk);
            Assert.AreEqual(0.20f, d.Stats.GetStatValue(nameof(StatId.DamageBonus)), 0.001f, "Berserk: +20% damage");
            Assert.AreEqual(0f, d.Stats.GetStatValue(nameof(StatId.Armor)), 0.001f);
            TraitSystem.Current.Strip(soldier.gameObject);
            Assert.AreEqual(0f, d.Stats.GetStatValue(nameof(StatId.DamageBonus)), 0.001f, "stripped: back to normal");

            // A demon has stats of its own.
            DemonBrain imp = null;
            yield return AnImp(i => imp = i);
            Damageable id = imp.GetComponent<Damageable>();
            float impMax = id.MaxHealth;
            Give(imp.gameObject, TraitId.Cowardly);
            Assert.AreEqual(impMax * 1.3f, id.MaxHealth, 0.01f, "Cowardly Imp: +30% max health");
            Give(imp.gameObject, TraitId.Berserk);
            Assert.AreEqual(impMax, id.MaxHealth, 0.01f);
            Assert.Greater(id.Stats.GetStatValue(nameof(StatId.DamageBonus)), 0.19f, "Berserk Imp: +20% damage");
        }

        // ---- Sneaky ----

        [UnityTest]
        public IEnumerator A_Sneaky_mortal_is_hidden_from_your_side_even_in_sight_until_it_attacks_and_visible_for_3_seconds_after()
        {
            Vector3 spot = SightSpot();   // in Jack's sight (14), beyond a mortal's (7)
            MortalBrain plain = Spawn(MortalType.Soldier, spot);
            MortalBrain sneaky = Spawn(MortalType.Soldier, FreeSpotNear(spot, 1.5f));
            Give(sneaky.gameObject, TraitId.Sneaky);
            _mortals.Unregister(sneaky);   // it stands still: only the hit below is its attack
            RegionEntity plainEntity = RegionEntities.Living.First(e => e.GameObject == plain.gameObject);
            RegionEntity sneakyEntity = RegionEntities.Living.First(e => e.GameObject == sneaky.gameObject);

            yield return TestWait.Until(() => RegionVision.Current.CanSee(Side.Yours, plainEntity.Cell) && RegionVision.Current.CanSee(Side.Yours, sneakyEntity.Cell), "both stand in your sight", 5f);
            yield return TestWait.Until(() => FogUnits.Current.IsHidden(sneakyEntity), "the Sneaky mortal is hidden", 5f);
            Assert.IsFalse(FogUnits.Current.IsHidden(plainEntity), "an ordinary one in the same sight is drawn");
            Assert.IsTrue(Concealment.IsHidden(sneaky.gameObject));

            Damageable victim = Spawn(MortalType.Peasant, FreeSpotNear(spot, 3f)).Member.Damageable;   // god mode would block a hit on Jack, and a blocked hit is no attack
            float hitAt = Time.time;
            victim.TakeDamage(1f, DamageRules.PhysicalType, sneaky.gameObject);   // it attacks
            Assert.IsFalse(Concealment.IsHidden(sneaky.gameObject), "it is seen the moment it attacks");
            yield return TestWait.Until(() => !FogUnits.Current.IsHidden(sneakyEntity), "it is drawn", 5f);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(Concealment.IsHidden(sneaky.gameObject), "still visible 2 seconds on");
            UnitTraits st = sneaky.GetComponent<UnitTraits>();
            float giveUp = Time.realtimeSinceStartup + 5f;
            while (!Concealment.IsHidden(sneaky.gameObject))
            {
                if (Time.realtimeSinceStartup > giveUp) Assert.Fail("not hidden again: time " + Time.time + " visibleUntil " + st.VisibleUntil + " concealed " + st.IsConcealed + " tracked " + TraitSystem.Current.TrackedCount + " listed " + (st.Entity != null && st.Entity.IsListed) + " action " + sneaky.Action);
                yield return null;
            }
            Assert.GreaterOrEqual(Time.time - hitAt, TraitTuning.VisibleSeconds - 0.1f, "it was visible for the full 3 seconds");
            yield return TestWait.Until(() => FogUnits.Current.IsHidden(sneakyEntity), "and hidden from the fog drawing too", 5f);
        }

        [UnityTest]
        public IEnumerator A_Sneaky_demon_is_unseen_by_the_mortals_until_it_attacks()
        {
            DemonBrain imp = null;
            yield return AnImp(i => imp = i);
            Time.timeScale = 1f;
            Give(imp.gameObject, TraitId.Sneaky);
            Assert.IsTrue(Concealment.IsHidden(imp.gameObject));

            // A soldier right beside it gives the mortals eyes there; they do not notice the demon.
            MortalBrain soldier = Spawn(MortalType.Soldier, FreeSpotNear(imp.transform.position, 2f));
            soldier.Member.Damageable.Invulnerable = true;
            yield return new WaitForSeconds(1.5f);
            bool Seen() => _mortals.Senses.TryNearest(imp.transform.position, 0.05f, out HordeTarget t, out _) && t.gameObject == imp.gameObject;
            Assert.IsFalse(Seen(), "the mortals do not see the hidden demon");

            soldier.Member.Damageable.Invulnerable = false;   // an invulnerable target blocks the hit, and a blocked hit is no attack
            soldier.Member.Damageable.TakeDamage(1f, DamageRules.PhysicalType, imp.gameObject);
            Assert.IsFalse(Concealment.IsHidden(imp.gameObject), "the attack gives it away");
            yield return TestWait.Until(Seen, "the mortals now see it", 3f);
        }


        // ---- Ambusher, Vigilant, Protector, Purifier, Unruly ----

        [UnityTest]
        public IEnumerator An_Ambusher_backs_off_after_three_kills_and_fights_again_after_a_while()
        {
            MortalBrain ambusher = Spawn(MortalType.Soldier, FreeSpotNear(Centre(_settlements.Seat), 6f));
            Give(ambusher.gameObject, TraitId.Ambusher);
            UnitTraits traits = ambusher.GetComponent<UnitTraits>();
            for (int i = 0; i < TraitTuning.BackOffKills; i++)
            {
                Assert.IsFalse(traits.IsBackingOff, "not before the third kill");
                MortalBrain prey = Spawn(MortalType.Peasant, FreeSpotNear(ambusher.transform.position, 3f));
                prey.Member.Damageable.TakeDamage(9999f, DamageRules.TrueType, ambusher.gameObject);
            }
            Assert.IsTrue(traits.IsBackingOff, "three kills and it backs off");
            Assert.IsFalse(ambusher.Behaviour.Of(BehaviourAction.Fight).Enabled, "it does not fight while backing off");
            Assert.IsTrue(ambusher.Behaviour.Of(BehaviourAction.Flee).Enabled);
            yield return TestWait.Until(() => !traits.IsBackingOff, "it comes back", TraitTuning.BackOffSeconds + 5f);
            Assert.IsTrue(ambusher.Behaviour.Of(BehaviourAction.Fight).Enabled, "and fights again");
            Assert.AreEqual(0, traits.Kills);
        }

        [UnityTest]
        public IEnumerator Unruly_demons_ignore_orders_Vigilant_witnesses_are_quick_and_a_Protector_is_stronger_near_its_post()
        {
            DemonBrain imp = null;
            yield return AnImp(i => imp = i);
            Give(imp.gameObject, TraitId.Unruly);
            Assert.IsTrue(imp.IgnoresOrders, "Unruly: it ignores its Portal's order");
            Give(imp.gameObject, TraitId.Protector);
            Assert.IsFalse(imp.IgnoresOrders, "the old Traits go when new ones are given");

            MortalBrain guard = Spawn(MortalType.Soldier, FreeSpotNear(Centre(_settlements.Seat), 6f));
            Give(guard.gameObject, TraitId.Vigilant);
            Assert.AreEqual(TraitTuning.VigilantAlarmDelayShare, UnitTraits.AlarmDelayShare(guard), "Vigilant: a shorter witness delay");
            Assert.AreEqual(1.5f, UnitTraits.SightOf(guard), 0.001f, "Vigilant: +50% vision");
            Assert.AreEqual(1.5f, Concealment.SightFactor(guard.gameObject), 0.001f, "and its viewer looks further");

            Damageable d = guard.Member.Damageable;
            float baseMax = d.MaxHealth;
            Give(guard.gameObject, TraitId.Protector);
            yield return TestWait.Until(() => d.MaxHealth > baseMax * 1.19f, "+20% health near its settlement", 5f);
            guard.transform.position = FreeSpotNear(guard.transform.position, 25f);
            yield return TestWait.Until(() => Mathf.Approximately(d.MaxHealth, baseMax), "away from it the bonus is gone", 5f);
        }

        [UnityTest]
        public IEnumerator A_Purifier_mortal_purifies_corruption_and_heals_nearby_allies()
        {
            MortalBrain purifier = Spawn(MortalType.Soldier, FreeSpotNear(Centre(_settlements.Seat), 6f));
            Give(purifier.gameObject, TraitId.Purifier);
            Assert.IsTrue(purifier.Behaviour.Of(BehaviourAction.Purify).Enabled, "it purifies like a priest");

            MortalBrain hurt = Spawn(MortalType.Soldier, FreeSpotNear(purifier.transform.position, 2f));
            Hurt(hurt.Member.Damageable, 0.5f);
            float health = hurt.Member.Damageable.Health;
            yield return new WaitForSeconds(2.5f);
            Assert.Greater(hurt.Member.Damageable.Health, health, "an ally beside it mends");
        }

        // ---- Rolling at spawn ----

        [UnityTest]
        public IEnumerator Mortals_roll_their_traits_when_they_enter_the_Region_within_the_odds_and_the_rules()
        {
            TraitSystem.AutoRoll = true;
            var spawned = Enumerable.Range(0, 300).Select(i => Spawn(MortalType.Soldier, FreeSpotNear(Centre(_settlements.Seat), 4f + i % 8))).ToList();
            yield return TestWait.Until(() => spawned.All(b => b.GetComponent<UnitTraits>() != null || TraitSystem.Current != null), "rolled", 5f);
            yield return TestWait.Frames(3);

            int with = spawned.Count(b => UnitTraits.Of(b.gameObject) != null);
            Assert.That(with, Is.InRange(8, 60), "about 10% of 300 soldiers have a Trait");
            foreach (MortalBrain b in spawned)
            {
                UnitTraits t = UnitTraits.Of(b.gameObject);
                if (t == null) continue;
                Assert.AreEqual(1, t.Traits.Count, "an ordinary unit has one");
                Assert.IsTrue(TraitTable.Get(t.Traits[0]).CanHave(Side.Mortals, UnitKind.Mortal), t.Traits[0] + " is allowed on a mortal");
            }
        }
    }
}
