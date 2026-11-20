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
    /// <summary>Bones, witnessed kills, alarm riders, fleeing peasants, spotting and discovered spawn circles in a generated Region.</summary>
    public class AlarmPlayModeTests
    {
        private RegionMortals _mortals;
        private RegionSettlements _settlements;
        private RegionGrid _grid;
        private RegionBones _bones;
        private RegionAlarms _alarms;
        private VigilanceDirector _director;
        private CharacterControl _jack;
        private CombatUnit _jackUnit;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            _mortals = RegionMortals.Current;
            _settlements = RegionSettlements.Current;
            _grid = RegionGrid.Current;
            _bones = RegionBones.Current;
            _alarms = RegionAlarms.Current;
            _director = VigilanceDirector.Current;
            _jack = Player.Character;
            _jackUnit = _jack.GetComponent<Damageable>().Unit;
            Assert.IsNotNull(_bones, "the bones system");
            Assert.IsNotNull(_alarms, "the alarm system");
            TestDebug.Toggle("God mode", true);
            RegionTestScene.ClearHorde();   // the Region's own mortals would see what the tests set up
            yield return TestWait.Frames(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // ---- Helpers ----

        private Vector3 FreeSpotNear(Vector3 from, float distance)
        {
            for (int i = 0; i < 96; i++)
            {
                float angle = i * 0.61f;
                Vector3 spot = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (distance + i * 0.05f);
                if (_grid.IsWalkable(spot) && _grid.IsWalkable(spot + Vector3.right) && _grid.IsWalkable(spot + Vector3.up)) return spot;
            }
            Assert.Fail("no free ground near " + from);
            return from;
        }

        private Vector3 SeatCentre() => _mortals.Homes.Centre(_settlements.Seat);

        private static HordeMember SpawnPlainMortal(Vector3 at) =>
            Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, at, Quaternion.identity).GetComponent<HordeMember>();

        private MortalBrain SpawnBrain(MortalType type, Vector3 at, RegionSettlements.Settlement home = null)
        {
            MortalBrain brain = _mortals.Spawn(type, home ?? _settlements.Seat, at);
            Assert.IsNotNull(brain, "a " + type + " spawns");
            return brain;
        }

        private static void KillBy(CombatUnit victim, CombatUnit killer) => victim.TakeDamage(99999f, DamageRules.PhysicalType, killer);

        private static void Kill(CombatUnit victim) => victim.TakeDamage(99999f, DamageRules.TrueType);

        // Five piles in a ring round a point, close enough for a mortal standing there to see them.
        private void DropRingOfBones(Vector3 centre, int count = 5)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Assert.IsNotNull(_bones.Drop(centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 2.5f, BoneKind.Human), "a pile drops");
            }
        }

        // A peasant out in the open with five piles round it, waiting for it to be a witness.
        private IEnumerator Witness(System.Action<MortalBrain> found)
        {
            Vector3 at = FreeSpotNear(SeatCentre(), 18f);
            MortalBrain peasant = SpawnBrain(MortalType.Peasant, at);
            yield return TestWait.Until(() => RegionVision.Current.CanSee(Side.Mortals, at), "the peasant's ground is seen");
            DropRingOfBones(peasant.transform.position);
            yield return TestWait.Until(() => _alarms.PendingWitnesses == 1, "the peasant sees the bones and is a witness", 5f);
            found(peasant);
        }

        // ---- Bones ----

        [UnityTest]
        public IEnumerator A_mortal_dying_leaves_a_pile_that_is_used_once()
        {
            HordeMember mortal = SpawnPlainMortal(FreeSpotNear(_jack.transform.position, 12f));
            yield return null;
            Int2 cell = _grid.CellAt(mortal.transform.position);
            int before = _bones.Count;

            KillBy(mortal.Unit, _jackUnit);
            yield return null;

            Assert.AreEqual(before + 1, _bones.Count, "one pile");
            BonePile pile = _bones.Piles.Last();
            Assert.AreEqual(cell, pile.Cell, "where it fell");
            Assert.AreEqual(BoneKind.Human, pile.Kind);
            Assert.IsNotNull(pile.GetComponent<SpriteRenderer>().sprite);
            Assert.AreEqual(16f, pile.GetComponent<SpriteRenderer>().sprite.pixelsPerUnit, "pixel-perfect: 16 pixels per cell");

            Assert.IsTrue(_bones.Take(pile), "it can be used");
            Assert.IsFalse(_bones.Take(pile), "once");
            Assert.IsFalse(_bones.Remove(pile), "and is gone");
            Assert.AreEqual(BoneFate.Taken, pile.Fate);
            Assert.AreEqual(before, _bones.Count);
        }

        [UnityTest]
        public IEnumerator A_pile_can_be_removed_without_being_used()
        {
            BonePile pile = _bones.Drop(FreeSpotNear(_jack.transform.position, 10f), BoneKind.Beast);
            Assert.IsTrue(_bones.Remove(pile));
            Assert.AreEqual(BoneFate.Removed, pile.Fate);
            Assert.IsFalse(_bones.Take(pile));
            Assert.IsFalse(_bones.Piles.Contains(pile));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Piles_crumble_through_the_crumble_art_and_are_gone()
        {
            BonePile pile = _bones.Drop(FreeSpotNear(_jack.transform.position, 10f), BoneKind.Human);
            Sprite intact = pile.GetComponent<SpriteRenderer>().sprite;
            Assert.AreEqual(0, pile.Stage);

            Time.timeScale = 60f;
            int highest = 0;
            Sprite crumbling = null;
            float end = Time.realtimeSinceStartup + 30f;
            while (pile.Fate == BoneFate.Standing && Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (pile.Stage > highest) { highest = pile.Stage; crumbling = pile.GetComponent<SpriteRenderer>().sprite; }
            }

            Assert.AreEqual(BoneFate.Crumbled, pile.Fate, "it crumbles away");
            Assert.AreEqual(BoneRules.Stages - 1, highest, "through every look");
            Assert.AreNotEqual(intact, crumbling, "the crumble sheet's art");
            Assert.IsFalse(_bones.Piles.Contains(pile));
        }

        // ---- Witnessed kills ----

        [UnityTest]
        public IEnumerator A_kill_a_mortal_sees_raises_Vigilance_more_than_an_unseen_one()
        {
            // Nobody else is about: unseen.
            Vector3 alone = FreeSpotNear(_jack.transform.position, 14f);
            HordeMember loner = SpawnPlainMortal(alone);
            yield return null;
            float start = _director.Points;
            KillBy(loner.Unit, _jackUnit);
            yield return null;
            float unseen = _director.Points - start;
            Assert.AreEqual(VigilanceTuning.KillPoints, unseen, 1e-3f, "an unseen kill raises a little");

            // A mortal beside the victim and the ground in its sight: witnessed.
            Vector3 there = FreeSpotNear(_jack.transform.position, 20f);
            HordeMember victim = SpawnPlainMortal(there);
            HordeMember bystander = SpawnPlainMortal(there + new Vector3(1.5f, 0f, 0f));
            bystander.SetRoaming(true);   // stands and watches
            victim.SetRoaming(true);
            yield return TestWait.Until(() => RegionVision.Current.CanSee(Side.Mortals, _grid.CellAt(there)), "the mortals see the spot");
            start = _director.Points;
            KillBy(victim.Unit, _jackUnit);
            yield return null;
            float seen = _director.Points - start;

            Assert.AreEqual(VigilanceTuning.WitnessedKillPoints, seen, 1e-3f, "counted once, as a witnessed kill");
            Assert.Greater(seen, unseen);
            Assert.IsTrue(bystander.IsAlive);
        }

        // ---- Alarms and riders ----

        [UnityTest]
        public IEnumerator Killing_the_witness_before_it_sends_the_rider_stops_the_alarm()
        {
            MortalBrain witness = null;
            yield return Witness(p => witness = p);
            Assert.AreEqual(0, _alarms.RidersSent, "not yet: the witness waits a few seconds");

            Kill(witness.Member.Unit);
            yield return TestWait.Until(() => _alarms.AlarmsStopped == 1, "the alarm is stopped", 3f);
            yield return new WaitForSeconds(AlarmTuning.WitnessDelaySeconds + 1.5f);

            Assert.AreEqual(0, _alarms.RidersSent, "no rider goes");
            Assert.AreEqual(0, _alarms.AlarmsRaised);
            Assert.IsFalse(_alarms.IsAlerted(witness.transform.position), "the area is not alerted");
            Assert.AreEqual(0f, _director.Points, "nothing reached the Seat");
        }

        [UnityTest]
        public IEnumerator A_witness_alerts_its_area_and_sends_a_rider_that_can_be_seen_and_killed()
        {
            MortalBrain witness = null;
            yield return Witness(p => witness = p);
            Vector3 at = witness.transform.position;

            yield return TestWait.Until(() => _alarms.RidersSent == 1, "the witness sends its rider", AlarmTuning.WitnessDelaySeconds + 3f);
            Assert.AreEqual(1, _alarms.AlarmsRaised);
            Assert.IsTrue(_alarms.IsAlerted(at), "its area is alerted");
            AlarmRider rider = _alarms.Riders.Single();
            Assert.IsTrue(rider.IsRiding);
            Assert.AreEqual(Side.Mortals, new RegionEntity(rider.Member.Damageable, UnitKind.Mortal).Side);
            Assert.IsNotNull(rider.GetComponent<SpriteRenderer>().sprite, "it has art: a Lancer");
            Assert.IsTrue(RegionEntities.Living.Any(e => e.GameObject == rider.gameObject), "it is a living mortal in the Region: it can be seen and hit");
            Assert.AreEqual(MortalType.Lancer, rider.GetComponent<MortalBrain>().Type);

            Vector3 start = rider.transform.position;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(Vector3.Distance(start, rider.transform.position), 2f, "it rides for the Seat");

            float before = _director.Points;
            KillBy(rider.Member.Unit, _jackUnit);
            yield return TestWait.Until(() => _alarms.RidersKilled == 1, "the dead rider is noted", 3f);
            yield return new WaitForSeconds(1f);

            Assert.AreEqual(0, _alarms.RidersArrived);
            Assert.AreEqual(VigilanceStage.Unaware, _director.Stage, "a killed rider raises no stage");
            Assert.Less(_director.Points - before, VigilanceTuning.Threshold(VigilanceStage.Uneasy), "only the kill itself");
        }

        [UnityTest]
        public IEnumerator A_rider_reaching_the_Seat_raises_a_full_stage()
        {
            Assert.AreEqual(VigilanceStage.Unaware, _director.Stage);
            AlarmRider rider = _alarms.SendRider(FreeSpotNear(SeatCentre(), 16f));
            Assert.IsNotNull(rider, "a rider goes");

            yield return TestWait.Until(() => _alarms.RidersArrived == 1, "the rider reaches the Seat", 30f);

            Assert.AreEqual(VigilanceStage.Uneasy, _director.Stage, "a full stage");
            Assert.AreEqual(VigilanceTuning.Threshold(VigilanceStage.Uneasy), _director.Points, 1e-3f);
            Assert.AreEqual(0, _alarms.Riders.Count);
            Assert.IsFalse(rider.IsRiding);
        }

        // A cell on the edge of the Seat's arrival area (distance 0) or the given number of cells outside it, walkable.
        private Vector3 SeatRingSpot(int outside)
        {
            CellRect reach = _settlements.Seat.Plan.Interior.Inflate(AlarmTuning.SeatArriveMargin + outside);
            for (int x = reach.MinX; x <= reach.MaxX; x++)
            for (int y = reach.MinY; y <= reach.MaxY; y++)
            {
                bool onEdge = x == reach.MinX || x == reach.MaxX || y == reach.MinY || y == reach.MaxY;
                var cell = new Int2(x, y);
                if (onEdge && _grid.Contains(cell) && _grid.IsWalkable(cell)) return _grid.CentreOf(cell);
            }
            Assert.Fail("no walkable ground round the Seat");
            return default;
        }

        [UnityTest]
        public IEnumerator A_rider_killed_one_cell_from_the_Seat_raises_nothing()
        {
            Vector3 oneShort = SeatRingSpot(outside: 1);
            Assert.IsFalse(AlarmRules.HasArrived(_grid.CellAt(oneShort), _settlements.Seat.Plan.Interior), "that spot is just short of the Seat");
            AlarmRider rider = _alarms.SendRider(oneShort);
            Assert.IsNotNull(rider);

            Kill(rider.Member.Unit);
            yield return TestWait.Until(() => _alarms.RidersKilled == 1, "the dead rider is noted", 3f);
            yield return new WaitForSeconds(1f);

            Assert.AreEqual(0, _alarms.RidersArrived);
            Assert.AreEqual(VigilanceStage.Unaware, _director.Stage);
            Assert.AreEqual(0f, _director.Points);
        }

        [UnityTest]
        public IEnumerator A_rider_one_step_further_in_has_arrived()
        {
            AlarmRider rider = _alarms.SendRider(SeatRingSpot(outside: 0));
            Assert.IsNotNull(rider);
            yield return TestWait.Until(() => _alarms.RidersArrived == 1, "the rider is at the Seat", 5f);
            Assert.AreEqual(VigilanceStage.Uneasy, _director.Stage);
        }

        // ---- Fleeing peasants ----

        [UnityTest]
        public IEnumerator A_fleeing_peasant_spreads_the_alarm_to_the_stronghold_it_reaches()
        {
            Vector3 at = FreeSpotNear(SeatCentre(), 18f);
            MortalBrain peasant = SpawnBrain(MortalType.Peasant, at);
            RegionSettlements.Settlement stronghold = _mortals.Homes.NearestStronghold(at);
            Vector3 centre = _mortals.Homes.Centre(stronghold);
            Assert.IsFalse(_alarms.IsAlerted(centre));
            yield return new WaitForSeconds(0.5f);

            peasant.Member.Unit.TakeDamage(peasant.Member.Damageable.MaxHealth * 0.2f, DamageRules.TrueType);
            yield return TestWait.Until(() => _alarms.StrongholdsAlerted == 1, "the peasant reaches the stronghold and spreads the alarm", 40f);

            Assert.IsTrue(MortalHomes.IsAt(stronghold, _grid.CellAt(peasant.transform.position)));
            Assert.IsTrue(_alarms.IsAlerted(centre), "the stronghold is alerted");
            Assert.AreEqual(0, _alarms.RidersSent, "spreading the alarm sends no rider");
        }

        // ---- Spotting Jack ----

        [UnityTest]
        public IEnumerator Mortals_spotting_Jack_raise_Vigilance_but_not_every_frame()
        {
            Assert.AreEqual(0f, _director.Points);
            SpawnBrain(MortalType.Soldier, FreeSpotNear(_jack.transform.position, 5f));

            yield return TestWait.Until(() => _alarms.SpottedRaises == 1, "the guard spots Jack", 6f);
            float raised = _director.Points;
            Assert.GreaterOrEqual(raised, VigilanceTuning.SpottedPoints, "spotting Jack raises Vigilance");

            yield return new WaitForSeconds(4f);
            Assert.AreEqual(1, _alarms.SpottedRaises, "standing in sight is not a raise per frame");
        }

        [UnityTest]
        public IEnumerator Spotting_the_Anchor_is_one_call_with_its_own_cooldown()
        {
            Assert.IsTrue(_alarms.NoteSpotted(SpotTarget.Anchor));
            Assert.AreEqual(VigilanceTuning.SpottedPoints, _director.Points, 1e-3f);
            Assert.IsFalse(_alarms.NoteSpotted(SpotTarget.Anchor), "not again at once");
            Assert.IsTrue(_alarms.NoteSpotted(SpotTarget.Jack), "Jack's cooldown is his own");
            yield return null;
        }

        // ---- Spawn circles ----

        [UnityTest]
        public IEnumerator A_spawn_circle_is_discovered_once_seen_and_stays_marked_on_the_map()
        {
            VigilanceSpawnCircle circle = _director.Circles.First();
            var painter = Object.FindAnyObjectByType<SpawnCirclePainter>();
            Assert.IsNotNull(painter, "the map painter");
            Assert.IsFalse(circle.Discovered, "nobody has seen it yet");
            Assert.AreEqual(0, Painted(painter), "so the map does not show it");

            _jack.Teleport(FreeSpotNear(circle.transform.position, 6f));
            yield return TestWait.Until(() => circle.Discovered, "Jack's side sees the circle", 6f);

            _jack.Teleport(FreeSpotNear(_grid.CentreOf(new Int2(_grid.Width / 2, _grid.Height / 2)), 2f));
            yield return TestWait.Until(() => !RegionVision.Current.CanSee(Side.Yours, circle.transform.position), "it is out of sight again", 6f);

            Assert.IsTrue(circle.Discovered, "it stays discovered");
            Assert.Greater(Painted(painter), 0, "and marked on the map");

            circle.Damageable.Unit.TakeDamage(99999f, DamageRules.PhysicalType, _jackUnit);
            yield return null;
            Assert.AreEqual(0, Painted(painter), "a destroyed circle is no longer marked");
        }

        private int Painted(SpawnCirclePainter painter)
        {
            var pixels = new Color32[_grid.Width * MapCanvas.PixelsPerCell * _grid.Height * MapCanvas.PixelsPerCell];
            painter.Paint(new MapCanvas(_grid.Field, pixels));
            return pixels.Count(c => c.a != 0);
        }
    }
}
