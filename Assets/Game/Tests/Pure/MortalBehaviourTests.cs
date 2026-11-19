using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>The weighted behaviour shared by every brain: what each mortal type does in a given situation, and how a trait changes it through data alone.</summary>
    public class MortalBehaviourTests
    {
        private static BehaviourAction Decide(MortalType type, BehaviourContext context, BehaviourAction current = BehaviourAction.Idle) =>
            BehaviourScoring.Choose(MortalProfiles.For(type).Behaviour, context, current);

        private static BehaviourContext Calm(MortalType type)
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.Leash = MortalProfiles.For(type).LeashCells;
            return c;
        }

        [TestCase(MortalType.Soldier)]
        [TestCase(MortalType.Archer)]
        [TestCase(MortalType.Knight)]
        public void At_night_a_guard_pulls_back_home_but_still_fights_what_it_sees(MortalType type)
        {
            BehaviourContext c = Calm(type);
            c.Night = true;
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(type, c, BehaviourAction.Patrol), "night: patrols pull back toward settlements");
            c.ThreatSeen = true;
            c.ThreatCloseness = 0.5f;
            Assert.AreEqual(BehaviourAction.Fight, Decide(type, c, BehaviourAction.ReturnHome));
        }

        [Test]
        public void A_calm_peasant_works()
        {
            Assert.AreEqual(BehaviourAction.Work, Decide(MortalType.Peasant, Calm(MortalType.Peasant)));
        }

        [Test]
        public void A_hurt_peasant_flees()
        {
            BehaviourContext c = Calm(MortalType.Peasant);
            c.Health = 0.6f;
            c.Fear = 1f;   // being hit frightens
            Assert.AreEqual(BehaviourAction.Flee, Decide(MortalType.Peasant, c));
        }

        [Test]
        public void A_peasant_flees_at_the_sight_of_a_threat_even_unhurt()
        {
            BehaviourContext c = Calm(MortalType.Peasant);
            c.ThreatSeen = true;
            c.ThreatCloseness = 0.3f;
            c.Fear = 1f;
            Assert.AreEqual(BehaviourAction.Flee, Decide(MortalType.Peasant, c, BehaviourAction.Work));
        }

        [Test]
        public void A_peasant_far_from_home_walks_back_and_a_frightened_one_runs_first()
        {
            BehaviourContext c = Calm(MortalType.Peasant);
            c.DistanceHome = c.Leash * 1.5f;
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(MortalType.Peasant, c));
            c.Fear = 0.8f;
            Assert.AreEqual(BehaviourAction.Flee, Decide(MortalType.Peasant, c));
        }

        [TestCase(MortalType.Soldier)]
        [TestCase(MortalType.Swordsman)]
        [TestCase(MortalType.Archer)]
        [TestCase(MortalType.ArmoredAxeman)]
        [TestCase(MortalType.Knight)]
        [TestCase(MortalType.Lancer)]
        public void A_calm_guard_patrols_and_a_guard_that_sees_a_threat_fights(MortalType type)
        {
            BehaviourContext c = Calm(type);
            Assert.AreEqual(BehaviourAction.Patrol, Decide(type, c));
            c.ThreatSeen = true;
            c.ThreatCloseness = 0.5f;
            c.Fear = 1f;
            Assert.AreEqual(BehaviourAction.Fight, Decide(type, c, BehaviourAction.Patrol), "healthy and scared, a guard still fights");
        }

        [Test]
        public void A_badly_hurt_guard_in_a_fight_flees_but_a_wounded_one_at_peace_does_not()
        {
            BehaviourContext fight = Calm(MortalType.Soldier);
            fight.ThreatSeen = true;
            fight.Fear = 1f;
            fight.Health = 0.1f;
            Assert.AreEqual(BehaviourAction.Flee, Decide(MortalType.Soldier, fight, BehaviourAction.Fight));

            BehaviourContext peace = Calm(MortalType.Soldier);
            peace.Health = 0.1f;
            Assert.AreEqual(BehaviourAction.Patrol, Decide(MortalType.Soldier, peace), "no fright, no flight");
        }

        [Test]
        public void A_guard_that_has_wandered_off_returns_and_a_guard_back_at_its_post_patrols_again()
        {
            BehaviourContext c = Calm(MortalType.Soldier);
            c.DistanceHome = c.Leash * 1.5f;
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(MortalType.Soldier, c));
            c.DistanceHome = 1f;
            Assert.AreEqual(BehaviourAction.Patrol, Decide(MortalType.Soldier, c, BehaviourAction.ReturnHome));
        }

        [Test]
        public void A_priest_purifies_near_corrupted_ground_and_otherwise_tends_the_place()
        {
            BehaviourContext c = Calm(MortalType.Priest);
            Assert.AreEqual(BehaviourAction.Work, Decide(MortalType.Priest, c));
            c.CorruptionNear = true;
            Assert.AreEqual(BehaviourAction.Purify, Decide(MortalType.Priest, c));
        }

        [Test]
        public void A_priest_never_fights_and_runs_from_a_threat_even_by_corruption()
        {
            BehaviourContext c = Calm(MortalType.Priest);
            c.CorruptionNear = true;
            c.ThreatSeen = true;
            c.ThreatCloseness = 0.5f;
            c.Fear = 1f;
            Assert.AreEqual(BehaviourAction.Flee, Decide(MortalType.Priest, c, BehaviourAction.Purify));
            Assert.IsFalse(MortalProfiles.For(MortalType.Priest).Behaviour.Of(BehaviourAction.Fight).Enabled);
        }

        [Test]
        public void Only_a_priest_purifies_and_only_guards_patrol_and_fight()
        {
            foreach (MortalType type in MortalProfiles.Roster)
            {
                BehaviourProfile p = MortalProfiles.For(type).Behaviour;
                MortalRole role = MortalProfiles.For(type).Role;
                Assert.AreEqual(role == MortalRole.Priest, p.Of(BehaviourAction.Purify).Enabled, type + " purify");
                Assert.AreEqual(role == MortalRole.Guard, p.Of(BehaviourAction.Fight).Enabled, type + " fight");
                Assert.AreEqual(role == MortalRole.Guard, p.Of(BehaviourAction.Patrol).Enabled, type + " patrol");
                Assert.IsTrue(p.Of(BehaviourAction.Flee).Enabled, type + " flee");
            }
        }

        [Test]
        public void The_action_it_is_doing_wins_a_near_tie()
        {
            var profile = new BehaviourProfile("tie", inertia: 0.2f)
                .Allow(BehaviourAction.Work, 0.5f)
                .Allow(BehaviourAction.Patrol, 0.6f);
            BehaviourContext c = BehaviourContext.Calm;
            Assert.AreEqual(BehaviourAction.Patrol, BehaviourScoring.Choose(profile, c, BehaviourAction.Idle));
            Assert.AreEqual(BehaviourAction.Work, BehaviourScoring.Choose(profile, c, BehaviourAction.Work), "0.5 + 0.2 beats 0.6");
        }

        [Test]
        public void A_tie_goes_to_the_earlier_action_every_time()
        {
            var profile = new BehaviourProfile("tie", inertia: 0f)
                .Allow(BehaviourAction.Patrol, 0.5f)
                .Allow(BehaviourAction.Work, 0.5f);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(BehaviourAction.Work, BehaviourScoring.Choose(profile, BehaviourContext.Calm, BehaviourAction.Idle));
        }

        [Test]
        public void A_profile_with_nothing_enabled_idles()
        {
            Assert.AreEqual(BehaviourAction.Idle, BehaviourScoring.Choose(new BehaviourProfile("empty"), BehaviourContext.Calm, BehaviourAction.Fight));
        }

        // ---- Traits are data: changes to a copy of the profile ----

        private static readonly BehaviourChange Cowardly =
            BehaviourChange.Add(BehaviourAction.Flee, new BehaviourTerm(BehaviourFactor.Health, 6f, 0.75f, 0.7f));

        [Test]
        public void A_Cowardly_trait_makes_a_soldier_flee_below_75_percent_health_through_data_alone()
        {
            BehaviourContext c = Calm(MortalType.Soldier);
            c.ThreatSeen = true;
            c.ThreatCloseness = 0.5f;
            c.Fear = 1f;
            c.Health = 0.72f;

            BehaviourProfile plain = MortalProfiles.For(MortalType.Soldier).Behaviour;
            BehaviourProfile coward = plain.With(Cowardly);

            Assert.AreEqual(BehaviourAction.Fight, BehaviourScoring.Choose(plain, c, BehaviourAction.Fight));
            Assert.AreEqual(BehaviourAction.Flee, BehaviourScoring.Choose(coward, c, BehaviourAction.Fight));
            c.Health = 0.8f;
            Assert.AreEqual(BehaviourAction.Fight, BehaviourScoring.Choose(coward, c, BehaviourAction.Fight), "above 75% it still fights");
        }

        [Test]
        public void A_Berserk_trait_never_flees()
        {
            BehaviourContext c = Calm(MortalType.Soldier);
            c.ThreatSeen = true;
            c.Fear = 1f;
            c.Health = 0.02f;
            BehaviourProfile berserk = MortalProfiles.For(MortalType.Soldier).Behaviour.With(BehaviourChange.Off(BehaviourAction.Flee));
            Assert.AreEqual(BehaviourAction.Fight, BehaviourScoring.Choose(berserk, c, BehaviourAction.Fight));
        }

        [Test]
        public void A_trait_makes_a_copy_and_leaves_the_types_own_profile_alone()
        {
            BehaviourProfile plain = MortalProfiles.For(MortalType.Peasant).Behaviour;
            float before = plain.Of(BehaviourAction.Flee).Base;
            int termsBefore = plain.Of(BehaviourAction.Flee).Terms.Length;
            plain.With(Cowardly, BehaviourChange.Bias(BehaviourAction.Flee, 5f));
            Assert.AreEqual(before, plain.Of(BehaviourAction.Flee).Base);
            Assert.AreEqual(termsBefore, plain.Of(BehaviourAction.Flee).Terms.Length);
        }

        [Test]
        public void A_trait_can_give_a_unit_an_action_it_did_not_have()
        {
            BehaviourProfile soldier = MortalProfiles.For(MortalType.Soldier).Behaviour;
            Assert.IsFalse(soldier.Of(BehaviourAction.Purify).Enabled);
            BehaviourProfile purifier = soldier.With(BehaviourChange.On(BehaviourAction.Purify, 0f, new BehaviourTerm(BehaviourFactor.Corruption, 2f)));
            BehaviourContext c = Calm(MortalType.Soldier);
            c.CorruptionNear = true;
            Assert.AreEqual(BehaviourAction.Purify, BehaviourScoring.Choose(purifier, c, BehaviourAction.Patrol));
        }

        [Test]
        public void The_night_flag_is_a_weight_so_patrols_can_pull_back_by_data()
        {
            BehaviourProfile soldier = MortalProfiles.For(MortalType.Soldier).Behaviour;
            BehaviourProfile atNight = soldier.With(
                BehaviourChange.Add(BehaviourAction.Patrol, new BehaviourTerm(BehaviourFactor.Night, -1f)),
                BehaviourChange.Add(BehaviourAction.ReturnHome, new BehaviourTerm(BehaviourFactor.Night, 1f)));
            BehaviourContext c = Calm(MortalType.Soldier);
            c.DistanceHome = 5f;
            Assert.AreEqual(BehaviourAction.Patrol, BehaviourScoring.Choose(atNight, c, BehaviourAction.Patrol));
            c.Night = true;
            Assert.AreEqual(BehaviourAction.ReturnHome, BehaviourScoring.Choose(atNight, c, BehaviourAction.Patrol));
        }

        [Test]
        public void A_term_is_linear_between_its_ends_and_a_step_when_they_meet()
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.Health = 0.5f;
            Assert.AreEqual(2f, new BehaviourTerm(BehaviourFactor.Health, 4f).Evaluate(c), 1e-5f);
            Assert.AreEqual(2f, new BehaviourTerm(BehaviourFactor.Health, 4f, 1f, 0f).Evaluate(c), 1e-5f, "wounded: full at 0, none at 1");
            Assert.AreEqual(4f, new BehaviourTerm(BehaviourFactor.Health, 4f, 0.5f, 0.5f).Evaluate(c), 1e-5f);
            c.Health = 0.49f;
            Assert.AreEqual(0f, new BehaviourTerm(BehaviourFactor.Health, 4f, 0.5f, 0.5f).Evaluate(c), 1e-5f);
        }

        // ---- Who spawns where ----

        [Test]
        public void Peasants_come_from_farms_and_villages_only()
        {
            Assert.IsTrue(MortalSpawns.TryPick(SettlementKind.Farm, MarkerKind.Peasant, 0.5, out MortalType a));
            Assert.IsTrue(MortalSpawns.TryPick(SettlementKind.Village, MarkerKind.Peasant, 0.5, out MortalType b));
            Assert.AreEqual(MortalType.Peasant, a);
            Assert.AreEqual(MortalType.Peasant, b);
            Assert.IsFalse(MortalSpawns.TryPick(SettlementKind.Camp, MarkerKind.Peasant, 0.5, out _));
            Assert.IsFalse(MortalSpawns.TryPick(SettlementKind.Seat, MarkerKind.Peasant, 0.5, out _));
        }

        [Test]
        public void Guards_come_from_villages_camps_and_the_Seat_and_priests_from_villages_and_the_Seat()
        {
            foreach (SettlementKind kind in new[] { SettlementKind.Village, SettlementKind.Camp, SettlementKind.Seat })
                Assert.IsTrue(MortalSpawns.TryPick(kind, MarkerKind.Guard, 0.3, out MortalType t) && MortalProfiles.For(t).Role == MortalRole.Guard, kind + " guard");
            Assert.IsFalse(MortalSpawns.TryPick(SettlementKind.Farm, MarkerKind.Guard, 0.3, out _));

            foreach (SettlementKind kind in new[] { SettlementKind.Village, SettlementKind.Seat })
                Assert.IsTrue(MortalSpawns.TryPick(kind, MarkerKind.Priest, 0.3, out MortalType t) && t == MortalType.Priest, kind + " priest");
            Assert.IsFalse(MortalSpawns.TryPick(SettlementKind.Camp, MarkerKind.Priest, 0.3, out _));
            Assert.IsFalse(MortalSpawns.TryPick(SettlementKind.Farm, MarkerKind.Priest, 0.3, out _));
        }

        [Test]
        public void Every_type_of_the_roster_spawns_somewhere_and_always_at_markers_of_its_own_kind()
        {
            var seen = new HashSet<MortalType>();
            foreach (SettlementKind kind in (SettlementKind[])System.Enum.GetValues(typeof(SettlementKind)))
            foreach (MarkerKind marker in (MarkerKind[])System.Enum.GetValues(typeof(MarkerKind)))
            foreach (MortalSpawns.Choice choice in MortalSpawns.Choices(kind, marker))
            {
                seen.Add(choice.Type);
                Assert.AreEqual(marker, MortalSpawns.MarkerOf(choice.Type), $"{choice.Type} at a {marker} marker of a {kind}");
                Assert.Greater(choice.Weight, 0);
            }
            CollectionAssert.AreEquivalent(MortalProfiles.Roster.ToList(), seen.ToList());
        }

        [Test]
        public void The_pick_follows_the_weights_and_the_roll_picks_the_same_type_every_time()
        {
            var counts = new Dictionary<MortalType, int>();
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(MortalSpawns.TryPick(SettlementKind.Seat, MarkerKind.Guard, i / 1000d, out MortalType t));
                counts[t] = counts.GetValueOrDefault(t) + 1;
            }
            Assert.Greater(counts[MortalType.Soldier], counts[MortalType.Lancer], "a heavier weight comes up more");
            MortalSpawns.TryPick(SettlementKind.Seat, MarkerKind.Guard, 0.42, out MortalType first);
            MortalSpawns.TryPick(SettlementKind.Seat, MarkerKind.Guard, 0.42, out MortalType again);
            Assert.AreEqual(first, again);
            Assert.IsTrue(MortalSpawns.TryPick(SettlementKind.Seat, MarkerKind.Guard, 1.0, out _), "a roll of 1 is clamped, not out of range");
        }
    }
}
