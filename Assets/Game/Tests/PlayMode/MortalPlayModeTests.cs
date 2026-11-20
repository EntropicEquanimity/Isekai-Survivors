using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Mortals with a life of their own in a generated Region: who the settlements spawn, what they do, and how they join and leave the horde's fight.</summary>
    public class MortalPlayModeTests
    {
        private RegionMortals _mortals;
        private RegionSettlements _settlements;
        private RegionGrid _grid;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            _mortals = RegionMortals.Current;
            _settlements = RegionSettlements.Current;
            _grid = RegionGrid.Current;
            _jack = Player.Character;
            TestDebug.Toggle("God mode", true);   // Jack must outlive the fights below
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (_mortals != null) _mortals.Night = false;
            Time.timeScale = 1f;
            yield return null;
        }

        // A walkable spot about `distance` cells from `from`, searched round it.
        private Vector3 FreeSpotNear(Vector3 from, float distance)
        {
            for (int i = 0; i < 64; i++)
            {
                float angle = i * 0.61f;
                Vector3 spot = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (distance + i * 0.05f);
                if (_grid.IsWalkable(spot) && _grid.IsWalkable(spot + Vector3.right) && _grid.IsWalkable(spot + Vector3.up)) return spot;
            }
            Assert.Fail("no free ground near " + from);
            return from;
        }

        private Vector3 Centre(RegionSettlements.Settlement s) =>
            _grid.CentreOf(new Int2((s.Plan.Interior.MinX + s.Plan.Interior.MaxX) / 2, (s.Plan.Interior.MinY + s.Plan.Interior.MaxY) / 2));

        private RegionSettlements.Settlement Nearest(Vector3 p) => _settlements.Settlements.OrderBy(s => (Centre(s) - p).sqrMagnitude).First();

        private MortalBrain Spawn(MortalType type, Vector3 at, RegionSettlements.Settlement home = null)
        {
            MortalBrain brain = _mortals.Spawn(type, home ?? Nearest(at), at);
            Assert.IsNotNull(brain, "a " + type + " spawns");
            return brain;
        }

        private static void Hurt(MortalBrain b, float fraction) =>
            b.Member.Unit.TakeDamage(b.Member.Damageable.MaxHealth * fraction, DamageRules.TrueType);

        [UnityTest]
        public IEnumerator Settlements_spawn_their_mortals_at_the_markers_by_kind()
        {
            int peasants = 0, guards = 0, priests = 0;
            foreach (RegionSettlements.Settlement s in _settlements.Settlements)
                foreach (RegionSettlements.SpawnPoint m in s.Markers)
                {
                    if (MortalSpawns.Choices(s.Kind, m.Kind).Count == 0) continue;
                    switch (m.Kind)
                    {
                        case MarkerKind.Peasant: peasants++; break;
                        case MarkerKind.Guard: guards++; break;
                        default: priests++; break;
                    }
                }
            Assert.Greater(peasants + guards + priests, 0, "the Region has markers");

            Assert.AreEqual(peasants, _mortals.Brains.Count(b => b.Profile.Role == MortalRole.Civilian), "a peasant at each peasant marker");
            int ruler = RegionRuler.Current != null && RegionRuler.Current.Ruler != null ? 1 : 0;
            Assert.AreEqual(guards + ruler, _mortals.Brains.Count(b => b.Profile.Role == MortalRole.Guard), "a guard at each guard marker, plus the Seat's ruler");
            Assert.AreEqual(priests, _mortals.Brains.Count(b => b.Type == MortalType.Priest), "a priest at each priest marker");
            foreach (MortalBrain b in _mortals.Brains)
            {
                Assert.IsNotNull(b.Settlement, b.Type + " belongs to a settlement");
                Assert.IsTrue(b.Settlement.Markers.Any(m => (m.Position - b.Home).sqrMagnitude < 0.01f && m.Kind == MortalSpawns.MarkerOf(b.Type)), b.Type + " started on a marker of its kind");
                Assert.AreEqual(Side.Mortals, new RegionEntity(b.Member.Damageable, UnitKind.Mortal).Side);
            }
            // The Seat holds the better troops and a priest.
            Assert.IsTrue(_mortals.Brains.Any(b => b.Settlement.IsSeat && b.Type == MortalType.Priest), "a priest in the Seat");
            Assert.IsTrue(_mortals.Brains.Any(b => b.Settlement.IsSeat && b.Profile.Role == MortalRole.Guard), "guards in the Seat");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Every_type_of_the_roster_has_a_prefab_with_its_own_brain()
        {
            foreach (MortalType type in MortalProfiles.Roster)
            {
                MortalBrain brain = Spawn(type, FreeSpotNear(_jack.transform.position, 20f));
                Assert.AreEqual(type, brain.Type);
                Assert.IsTrue(brain.Member.BrainDriven, type + " is driven by its brain, not woken by the horde");
                Assert.IsNotNull(brain.GetComponent<SpriteRenderer>().sprite, type + " has art");
                Assert.AreEqual(16f, brain.GetComponent<SpriteRenderer>().sprite.pixelsPerUnit, "pixel-perfect: 16 pixels per cell");
            }
            Assert.IsTrue(_mortals.Brains.Any(b => b.Type == MortalType.Archer && b.Member.Reach > 2f), "archers shoot from range");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Peasants_work_near_their_settlement_and_guards_patrol_around_it()
        {
            yield return new WaitForSeconds(0.5f);
            MortalBrain peasant = _mortals.Brains.First(b => b.Profile.Role == MortalRole.Civilian);
            MortalBrain guard = _mortals.Brains.First(b => b.Profile.Role == MortalRole.Guard);
            Vector3 peasantStart = peasant.transform.position;
            Vector3 guardStart = guard.transform.position;

            float peasantAway = 0f, guardAway = 0f;
            float end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                peasantAway = Mathf.Max(peasantAway, (peasant.transform.position - peasantStart).magnitude);
                guardAway = Mathf.Max(guardAway, (guard.transform.position - guardStart).magnitude);
                Assert.LessOrEqual((peasant.transform.position - peasant.Home).magnitude, MortalTuning.WorkRadius + 3f, "a peasant stays near where it works");
                Assert.LessOrEqual((guard.transform.position - guard.Home).magnitude, guard.Profile.LeashCells + 2f, "a guard stays within its leash");
            }
            Assert.Greater(peasantAway, 0.5f, "the peasant moved about");
            Assert.Greater(guardAway, 2f, "the guard walked a patrol");
        }

        [UnityTest]
        public IEnumerator A_hurt_peasant_flees_to_the_nearest_stronghold()
        {
            RegionSettlements.Settlement seat = _settlements.Seat;
            Vector3 seatCentre = Centre(seat);
            // Out in the open, a dozen cells from the Seat's walls, nowhere near Jack.
            Vector3 at = FreeSpotNear(seatCentre, 18f);
            MortalBrain peasant = Spawn(MortalType.Peasant, at, seat);
            yield return new WaitForSeconds(1f);
            Assert.AreNotEqual(BehaviourAction.Flee, peasant.Action, "nothing frightens it yet");

            Hurt(peasant, 0.2f);
            yield return TestWait.Until(() => peasant.Action == BehaviourAction.Flee, "the hurt peasant flees", 3f);
            float before = (peasant.transform.position - seatCentre).magnitude;
            yield return new WaitForSeconds(2f);
            Assert.Less((peasant.transform.position - seatCentre).magnitude, before - 1f, "it runs toward the stronghold");
            yield return TestWait.Until(() => MortalHomes.IsAt(seat, _grid.CellAt(peasant.transform.position)), "it reaches the Seat", 25f);
            Assert.GreaterOrEqual(_mortals.HeldFields, 1, "by a static field of its own");
        }

        [UnityTest]
        public IEnumerator A_peasant_that_sees_a_demon_or_Jack_flees()
        {
            MortalBrain peasant = Spawn(MortalType.Peasant, FreeSpotNear(_jack.transform.position, 4f));
            yield return TestWait.Until(() => peasant.Action == BehaviourAction.Flee, "the peasant sees Jack and runs", 5f);
        }

        [UnityTest]
        public IEnumerator A_guard_that_sees_a_target_joins_the_horde_and_drops_back_when_none_remains()
        {
            MortalBrain guard = Spawn(MortalType.Soldier, FreeSpotNear(_jack.transform.position, 5f));
            yield return TestWait.Until(() => guard.Action == BehaviourAction.Fight && guard.IsFighting, "the guard fights", 5f);
            Assert.GreaterOrEqual(HordeDirector.Current.Attacking, 1, "in the horde's crowd");

            // Far from any target, it falls back on its brain.
            guard.transform.position = FreeSpotNear(_jack.transform.position, 45f);
            yield return TestWait.Until(() => !guard.IsFighting && guard.Action != BehaviourAction.Fight, "it drops back out of the crowd", 5f);
        }

        [UnityTest]
        public IEnumerator An_archer_holds_still_at_range_while_a_melee_guard_closes_in()
        {
            Vector3 at = FreeSpotNear(_jack.transform.position, 6f);
            MortalBrain archer = Spawn(MortalType.Archer, at);
            yield return TestWait.Until(() => archer.Action == BehaviourAction.Fight, "the archer fights", 5f);
            yield return TestWait.Until(() => !archer.IsFighting, "the archer stops walking once in range", 8f);
            // Whatever it holds still at (Jack, or a building of his beside him) is a hostile target in reach but not next to it.
            float distance = HordeTarget.All.Where(t => t.IsAlive && Teams.Relation(TeamIds.Mortals, t.Team) == TeamRelation.Hostile)
                .Min(t => (archer.transform.position - t.transform.position).magnitude);
            Assert.Greater(distance, 1.5f, "it shoots from a distance");
            Assert.Less(distance, archer.Member.Reach + 1.5f, "but within reach");
            Assert.AreEqual(BehaviourAction.Fight, archer.Action);
        }

        [UnityTest]
        public IEnumerator A_trait_is_a_profile_change_a_Cowardly_soldier_flees_a_plain_one_fights()
        {
            var cowardly = BehaviourChange.Add(BehaviourAction.Flee, new BehaviourTerm(BehaviourFactor.Health, 6f, 0.75f, 0.7f));
            MortalBrain coward = Spawn(MortalType.Soldier, FreeSpotNear(_jack.transform.position, 5f));
            MortalBrain plain = Spawn(MortalType.Soldier, FreeSpotNear(_jack.transform.position, 5.5f));
            coward.SetBehaviour(coward.Behaviour.With(cowardly));
            Hurt(coward, 0.3f);
            Hurt(plain, 0.3f);

            yield return TestWait.Until(() => coward.Action == BehaviourAction.Flee, "the Cowardly soldier flees at 70% health", 5f);
            yield return TestWait.Until(() => plain.Action == BehaviourAction.Fight, "the plain one fights on", 5f);
            Assert.AreEqual(BehaviourAction.Flee, coward.Action);
        }

        [UnityTest]
        public IEnumerator A_priest_purifies_corruption_near_its_settlement_one_level_at_a_time()
        {
            _mortals.PurifySeconds = 1f;
            MortalBrain priest = _mortals.Brains.First(b => b.Type == MortalType.Priest);
            Vector3 spot = FreeSpotNear(priest.Home, 3f);
            Int2 cell = _grid.CellAt(spot);
            RegionCorruption corruption = RegionCorruption.Current;
            corruption.Raise(cell);
            corruption.Raise(cell);
            int level = corruption.Level(cell);
            Assert.GreaterOrEqual(level, 1, "the cell is corrupted");

            yield return TestWait.Until(() => priest.Action == BehaviourAction.Purify, "the priest sets about it", 5f);
            yield return TestWait.Until(() => corruption.Level(cell) < level, "the priest takes a level off", 15f);
            Assert.AreEqual(level - 1, corruption.Level(cell), "one level at a time, not the lot at once");
            Assert.GreaterOrEqual(priest.Purified, 1);
        }

        [UnityTest]
        public IEnumerator A_priest_only_purifies_about_every_20_seconds_by_default()
        {
            Assert.AreEqual(20f, _mortals.PurifySeconds, "the corruption rules' figure");
            Assert.AreEqual(CorruptionTuning.PurifySeconds, _mortals.PurifySeconds);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Jacks_wisp_drains_double_near_a_priest_and_on_holy_ground_and_not_elsewhere()
        {
            RegionSouls souls = RegionSouls.Current;
            RegionEconomy.Current.Stockpile.Add(ResourceType.Souls, 500);   // enough that the Run is not lost while we measure
            Building anchor = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            _jack.Teleport(anchor.transform.position + new Vector3(15f, 0f, 0f));   // a wisp beside the Anchor would revive at once
            _jack.GetComponent<Damageable>().Kill();
            yield return TestWait.Until(() => souls.IsBodiless, "the wisp appears");
            Wisp wisp = souls.Wisp;
            MortalBrain priest = _mortals.Brains.First(b => b.Type == MortalType.Priest && !b.Settlement.IsSeat);

            wisp.transform.position = priest.transform.position + Vector3.right * 2f;
            yield return TestWait.Until(() => souls.DrainDoubled, $"double near a priest (bodiless {souls.IsBodiless}, wisp {(wisp != null ? wisp.transform.position.ToString() : "gone")}, priest {priest.transform.position} alive {priest.Member.IsAlive}, brains {_mortals.Brains.Count})", 3f);
            Assert.AreEqual(souls.Ledger.DrainPerSecond(true), souls.DrainPerSecond, 1e-4f);

            // Somewhere no priest is and that is not the Seat.
            Vector3 away = FarFromPriests();
            wisp.transform.position = away;
            yield return TestWait.Until(() => !souls.DrainDoubled, "normal away from priests", 3f);
            Assert.AreEqual(souls.Ledger.DrainPerSecond(false), souls.DrainPerSecond, 1e-4f);

            // The Seat's cells are holy ground.
            RegionSettlements.Settlement seat = _settlements.Seat;
            wisp.transform.position = _grid.CentreOf(new Int2(seat.Plan.Interior.MinX + 1, seat.Plan.Interior.MinY + 1));
            Assert.IsTrue(_mortals.IsHolyGround(wisp.transform.position));
            yield return TestWait.Until(() => souls.DrainDoubled, "double on holy ground", 3f);
        }

        private Vector3 FarFromPriests()
        {
            Vector3 best = default;
            float bestD = -1f;
            foreach (Vector3 corner in new[]
                     {
                         _grid.CentreOf(new Int2(2, 2)), _grid.CentreOf(new Int2(_grid.Width - 3, 2)),
                         _grid.CentreOf(new Int2(2, _grid.Height - 3)), _grid.CentreOf(new Int2(_grid.Width - 3, _grid.Height - 3)),
                     })
            {
                if (_mortals.IsHolyGround(corner)) continue;
                float d = _mortals.Brains.Where(b => b.Type == MortalType.Priest).Select(b => (b.transform.position - corner).magnitude).DefaultIfEmpty(1000f).Min();
                if (d > bestD) { bestD = d; best = corner; }
            }
            Assert.Greater(bestD, MortalTuning.PriestAuraCells + 2f, "a corner away from every priest");
            return best;
        }

        [UnityTest]
        public IEnumerator Mortals_in_fight_mode_use_the_hordes_crowd_not_a_field_of_their_own()
        {
            int cached = Blondie.FlowFields.FlowFields.CachedCount;
            int held = _mortals.HeldFields;
            MortalBrain a = Spawn(MortalType.Soldier, FreeSpotNear(_jack.transform.position, 5f));
            MortalBrain b = Spawn(MortalType.Swordsman, FreeSpotNear(_jack.transform.position, 5.5f));
            yield return TestWait.Until(() => a.IsFighting && b.IsFighting, "both fight", 5f);
            yield return new WaitForSeconds(1f);
            Assert.LessOrEqual(Blondie.FlowFields.FlowFields.CachedCount, cached + 1, "fighting asks for no new field of its own");
            Assert.AreEqual(held, _mortals.HeldFields, "and none was held for walking home");
        }

        [UnityTest]
        public IEnumerator Hooks_for_the_other_tickets_are_there_Night_alarms_and_patrol_strength()
        {
            RegionSettlements.Settlement camp = _settlements.Settlements.FirstOrDefault(s => s.Kind == SettlementKind.Camp) ?? _settlements.Seat;
            _mortals.PatrolStrength = 2f;
            int before = _mortals.Brains.Count;
            int added = _mortals.AddPatrols(camp, 3);
            Assert.AreEqual(3, added, "more guards for a settlement");
            Assert.AreEqual(before + 3, _mortals.Brains.Count);
            MortalBrain strong = _mortals.Brains.Last();
            Assert.AreEqual(strong.Member.BaseHealth * 2f, strong.Member.Damageable.MaxHealth, 1e-3f, "stronger patrols");

            MortalBrain peasant = Spawn(MortalType.Peasant, FreeSpotNear(_jack.transform.position, 25f));
            _mortals.RaiseAlarm(peasant.transform.position, 3f, 5f);
            yield return TestWait.Until(() => peasant.Action == BehaviourAction.Flee, "a peasant that has seen an alarm flees", 3f);

            _mortals.Night = true;
            Assert.IsTrue(_mortals.Night);
        }
    }
}
