using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class AutoplayStrategyTests
    {
        [TestCase("stealth")]
        [TestCase("RUSH")]
        [TestCase(" balanced ")]
        public void Strategies_are_found_by_name_in_any_case(string name) =>
            Assert.IsNotNull(AutoplayStrategies.Find(name));

        [Test]
        public void An_unknown_strategy_is_not_found() => Assert.IsNull(AutoplayStrategies.Find("turtle"));

        [Test]
        public void Stealth_avoids_witnesses_destroys_bones_and_raids_later_than_rush()
        {
            AutoplayStrategy stealth = AutoplayStrategies.Stealth, rush = AutoplayStrategies.Rush;
            Assert.IsTrue(stealth.AvoidWitnesses);
            Assert.IsFalse(rush.AvoidWitnesses);
            Assert.AreEqual(BonesPolicy.Destroy, stealth.Bones);
            Assert.Less(stealth.VigilanceCeiling, rush.VigilanceCeiling, "stealth stops provoking sooner");
            Assert.Greater(stealth.RaidNotBeforeSeconds, rush.RaidNotBeforeSeconds, "stealth raids late");
            Assert.Greater(stealth.UnitsToRaid, rush.UnitsToRaid, "with a bigger force");
        }

        [Test]
        public void Balanced_sits_between_stealth_and_rush()
        {
            AutoplayStrategy b = AutoplayStrategies.Balanced;
            Assert.Less(AutoplayStrategies.Rush.UnitsToRaid, b.UnitsToRaid);
            Assert.Less(b.UnitsToRaid, AutoplayStrategies.Stealth.UnitsToRaid);
            Assert.Less(AutoplayStrategies.Rush.RaidNotBeforeSeconds, b.RaidNotBeforeSeconds);
            Assert.Less(b.RaidNotBeforeSeconds, AutoplayStrategies.Stealth.RaidNotBeforeSeconds);
        }
    }

    public class AutoplayFlowTests
    {
        [Test]
        public void A_scene_is_not_asked_for_while_a_screen_transition_is_running_because_the_game_drops_it()
        {
            Assert.IsFalse(AutoplayFlow.MayAskForScene(transitionRunning: true));
            Assert.IsTrue(AutoplayFlow.MayAskForScene(transitionRunning: false));
            Assert.Greater(AutoplayFlow.AskAgainSeconds, 0f);
        }
    }

    public class BotRulesTests
    {
        private static BotSnapshot Base() => new()
        {
            RegionSeconds = 10f, Souls = 0, Stone = 0, SoulLevel = 1, LevelUpCost = 50, ReviveCost = 22, PossessCost = 110,
            Stage = VigilanceStage.Unaware, HealthFraction = 1f, AnchorStands = true, WorkerSouls = 25, PortalStone = 20, PortalSouls = 5,
            StoneLeft = true, PreyLeft = true, PortalSpot = true, SeatKnown = true,
        };

        [Test]
        public void Stealth_raids_late_but_rush_raids_as_soon_as_it_has_a_small_force()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 120f;
            s.Portals = 3;
            s.Units = 8;
            Assert.IsTrue(BotRules.ShouldRaid(AutoplayStrategies.Rush, s));
            Assert.IsFalse(BotRules.ShouldRaid(AutoplayStrategies.Stealth, s), "stealth waits for its time");
            s.RegionSeconds = 500f;
            Assert.IsTrue(BotRules.ShouldRaid(AutoplayStrategies.Stealth, s));
        }

        [Test]
        public void The_demons_wait_for_enough_Portals_and_demons()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 200f;
            s.Portals = 1;
            s.Units = 10;
            Assert.IsFalse(BotRules.ShouldRaid(AutoplayStrategies.Rush, s), "too few Portals");
            s.Portals = 2;
            s.Units = 1;
            Assert.IsFalse(BotRules.ShouldRaid(AutoplayStrategies.Rush, s), "too few demons");
        }

        [Test]
        public void When_the_waiting_is_over_it_raids_with_whatever_it_has()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = AutoplayStrategies.Balanced.AssaultAfterSeconds + 1f;
            Assert.IsTrue(BotRules.ShouldRaid(AutoplayStrategies.Balanced, s));
            Assert.IsFalse(BotRules.JackJoinsRaid(AutoplayStrategies.Balanced, s), "Jack is not sent to a death far from home yet");
            s.RegionSeconds = AutoplayStrategies.Balanced.AssaultAfterSeconds * 2f + 1f;
            Assert.IsTrue(BotRules.JackJoinsRaid(AutoplayStrategies.Balanced, s));
            Assert.AreEqual(BotMove.Assault, BotRules.NextMove(AutoplayStrategies.Balanced, s));
        }

        [Test]
        public void Nothing_is_raided_while_the_Seat_is_not_known()
        {
            BotSnapshot s = Base();
            s.SeatKnown = false;
            s.RegionSeconds = 5000f;
            Assert.IsFalse(BotRules.ShouldRaid(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void Jack_stays_home_while_only_the_demons_raid_and_goes_once_they_hold_the_Seat_and_his_Souls_see_him_back()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 100f;
            s.Portals = 2;
            s.Units = 6;
            s.ReviveCost = 22;
            s.DeathDrainPerSecond = 4f;
            s.SeatDistance = 120f;
            s.Souls = 500;
            Assert.IsTrue(BotRules.ShouldRaid(AutoplayStrategies.Rush, s));
            Assert.IsFalse(BotRules.JackJoinsRaid(AutoplayStrategies.Rush, s), "no demon has reached the Seat yet");
            s.DemonsAtSeat = AutoplayStrategies.Rush.JackJoinsAtDemons;
            s.SeatDefenders = s.DemonsAtSeat * 2;
            Assert.IsTrue(BotRules.JackJoinsRaid(AutoplayStrategies.Rush, s));
            s.SeatDefenders = s.DemonsAtSeat * 2 + 1;
            Assert.IsFalse(BotRules.JackJoinsRaid(AutoplayStrategies.Rush, s), "the demons are outnumbered more than two to one");
        }

        [Test]
        public void Jack_stays_home_when_a_death_at_the_Seat_would_lose_the_Run()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 100f;
            s.Portals = 2;
            s.Units = 6;
            s.DemonsAtSeat = 8;
            s.ReviveCost = 22;
            s.DeathDrainPerSecond = 4f;
            s.SeatDistance = 120f;   // 20 seconds of wisp flight, 80 Souls drained
            s.Souls = 102;
            Assert.IsTrue(BotRules.CanSurviveDeathAtSeat(s));
            Assert.IsTrue(BotRules.JackJoinsRaid(AutoplayStrategies.Rush, s));
            s.Souls = 101;
            Assert.IsFalse(BotRules.CanSurviveDeathAtSeat(s));
            Assert.IsFalse(BotRules.JackJoinsRaid(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void A_hurt_Jack_still_goes_to_the_Seat_when_his_Souls_would_see_him_back_but_not_otherwise()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 100f;
            s.Portals = 2;
            s.Units = 6;
            s.DemonsAtSeat = 8;
            s.SeatDefenders = 4;
            s.HealthFraction = 0.1f;
            s.ReviveCost = 22;
            s.DeathDrainPerSecond = 4f;
            s.SeatDistance = 120f;
            s.Souls = 102;
            Assert.AreEqual(BotMove.Assault, BotRules.NextMove(AutoplayStrategies.Rush, s), "a death only costs Souls");
            s.Souls = 50;
            Assert.AreEqual(BotMove.Retreat, BotRules.NextMove(AutoplayStrategies.Rush, s), "a death at the Seat would lose the Run");
        }

        [Test]
        public void Jack_also_goes_when_a_possession_beside_the_demons_at_the_Seat_would_see_him_through()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 100f;
            s.Portals = 2;
            s.Units = 6;
            s.DemonsAtSeat = 8;
            s.ReviveCost = 22;
            s.PossessCost = 110;
            s.DeathDrainPerSecond = 4f;
            s.SeatDistance = 180f;   // the flight home costs 120 and the revive 22
            s.Souls = 128;
            Assert.IsTrue(BotRules.CanSurviveDeathAtSeat(s), "110 to possess and 16 drained on the way");
            s.Souls = 125;
            Assert.IsFalse(BotRules.CanSurviveDeathAtSeat(s));
            s.Souls = 128;
            s.DemonsAtSeat = 0;
            Assert.IsFalse(BotRules.CanSurviveDeathAtSeat(s), "no demon there to possess");
        }

        [Test]
        public void A_raid_that_has_begun_goes_on_while_there_are_demons_even_with_a_Portal_lost()
        {
            BotSnapshot s = Base();
            s.RegionSeconds = 200f;
            s.Portals = 1;
            s.Units = 5;
            Assert.IsFalse(BotRules.ShouldRaid(AutoplayStrategies.Rush, s), "one Portal is too few to start");
            s.Raiding = true;
            Assert.IsTrue(BotRules.ShouldRaid(AutoplayStrategies.Rush, s));
            s.Units = 0;
            Assert.IsFalse(BotRules.ShouldRaid(AutoplayStrategies.Rush, s), "nothing left to send");
        }

        [Test]
        public void Balanced_and_stealth_want_more_demons_at_the_Seat_than_rush_before_Jack_goes()
        {
            Assert.Greater(AutoplayStrategies.Balanced.JackJoinsAtDemons, AutoplayStrategies.Rush.JackJoinsAtDemons);
            Assert.GreaterOrEqual(AutoplayStrategies.Stealth.JackJoinsAtDemons, AutoplayStrategies.Balanced.JackJoinsAtDemons);
        }

        [Test]
        public void A_hurt_Jack_retreats_to_the_Anchor_but_not_without_one_or_without_a_body()
        {
            BotSnapshot s = Base();
            s.HealthFraction = 0.1f;
            Assert.IsTrue(BotRules.ShouldRetreat(AutoplayStrategies.Balanced, s));
            Assert.AreEqual(BotMove.Retreat, BotRules.NextMove(AutoplayStrategies.Balanced, s));
            s.AnchorStands = false;
            Assert.IsFalse(BotRules.ShouldRetreat(AutoplayStrategies.Balanced, s));
            s.AnchorStands = true;
            s.Bodiless = true;
            Assert.IsFalse(BotRules.ShouldRetreat(AutoplayStrategies.Balanced, s));
        }

        [Test]
        public void Stealth_retreats_at_a_higher_health_than_rush()
        {
            BotSnapshot s = Base();
            s.HealthFraction = 0.4f;
            Assert.IsTrue(BotRules.ShouldRetreat(AutoplayStrategies.Stealth, s));
            Assert.IsFalse(BotRules.ShouldRetreat(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void It_mines_for_a_Portal_first_then_hunts_for_the_Souls()
        {
            BotSnapshot s = Base();
            Assert.AreEqual(BotMove.Mine, BotRules.NextMove(AutoplayStrategies.Rush, s), "no Stone yet");
            s.Stone = 20;
            Assert.AreEqual(BotMove.Hunt, BotRules.NextMove(AutoplayStrategies.Rush, s), "no Souls yet");
            s.Souls = 5;
            Assert.AreEqual(BotBuy.Portal, BotRules.NextBuy(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void With_a_couple_of_demons_Jack_stays_home_and_lets_their_kills_pay_for_him()
        {
            BotSnapshot s = Base();
            s.Stone = 20;
            s.Units = BotRules.DemonsThatHunt - 1;
            Assert.AreEqual(BotMove.Hunt, BotRules.NextMove(AutoplayStrategies.Rush, s));
            s.Units = BotRules.DemonsThatHunt;
            Assert.AreEqual(BotMove.Hold, BotRules.NextMove(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void It_does_not_mine_when_there_is_no_Stone_left_or_no_ground_for_a_Portal()
        {
            BotSnapshot s = Base();
            s.StoneLeft = false;
            Assert.AreNotEqual(BotMove.Mine, BotRules.NextMove(AutoplayStrategies.Rush, s));
            s.StoneLeft = true;
            s.PortalSpot = false;
            Assert.AreNotEqual(BotMove.Mine, BotRules.NextMove(AutoplayStrategies.Rush, s));
            Assert.AreEqual(BotBuy.Nothing, BotRules.NextBuy(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void It_stops_hunting_once_Vigilance_passes_the_strategys_ceiling_and_holds_at_the_Anchor()
        {
            BotSnapshot s = Base();
            s.Stone = 20;
            s.Stage = VigilanceStage.Alarmed;
            Assert.AreEqual(BotMove.Retreat, BotRules.NextMove(AutoplayStrategies.Stealth, s), "stealth stopped provoking");
            Assert.AreEqual(BotMove.Hunt, BotRules.NextMove(AutoplayStrategies.Rush, s), "rush never stops");
            s.AnchorStands = false;
            Assert.AreEqual(BotMove.Hold, BotRules.NextMove(AutoplayStrategies.Stealth, s));
        }

        [Test]
        public void It_still_mines_above_the_ceiling()
        {
            BotSnapshot s = Base();
            s.Stage = VigilanceStage.Crusade;
            Assert.AreEqual(BotMove.Mine, BotRules.NextMove(AutoplayStrategies.Stealth, s));
        }

        [Test]
        public void It_builds_Portals_before_workers_and_levels()
        {
            BotSnapshot s = Base();
            s.Stone = 20;
            s.Souls = 500;
            Assert.AreEqual(BotBuy.Portal, BotRules.NextBuy(AutoplayStrategies.Balanced, s));
            s.Portals = BotRules.PortalsWanted(AutoplayStrategies.Balanced);
            Assert.AreEqual(BotBuy.Worker, BotRules.NextBuy(AutoplayStrategies.Balanced, s));
            s.Workers = AutoplayStrategies.Balanced.WorkersWanted;
            Assert.AreEqual(BotBuy.SoulLevel, BotRules.NextBuy(AutoplayStrategies.Balanced, s));
            s.SoulLevel = AutoplayStrategies.Balanced.SoulLevelTarget;
            Assert.AreEqual(BotBuy.Nothing, BotRules.NextBuy(AutoplayStrategies.Balanced, s));
        }

        [Test]
        public void It_keeps_enough_Souls_for_a_revive_before_raising_a_level()
        {
            BotSnapshot s = Base();
            s.Portals = 3;
            s.Workers = 1;
            s.Souls = s.LevelUpCost + s.ReviveCost - 1;
            Assert.AreEqual(BotBuy.Nothing, BotRules.NextBuy(AutoplayStrategies.Balanced, s));
            s.Souls++;
            Assert.AreEqual(BotBuy.SoulLevel, BotRules.NextBuy(AutoplayStrategies.Balanced, s));
        }

        [Test]
        public void After_the_first_Portal_it_keeps_a_revive_s_worth_of_Souls_back()
        {
            BotSnapshot s = Base();
            s.Stone = 20;
            s.Portals = 1;
            s.Souls = s.PortalSouls + s.ReviveCost - 1;
            Assert.AreEqual(BotBuy.Nothing, BotRules.NextBuy(AutoplayStrategies.Rush, s));
            Assert.AreEqual(BotMove.Hunt, BotRules.NextMove(AutoplayStrategies.Rush, s), "it hunts for the difference");
            s.Souls++;
            Assert.AreEqual(BotBuy.Portal, BotRules.NextBuy(AutoplayStrategies.Rush, s));
            s.Portals = 0;
            s.Souls = s.PortalSouls;
            Assert.AreEqual(BotBuy.Portal, BotRules.NextBuy(AutoplayStrategies.Rush, s), "the first Portal is bought with whatever there is");
        }

        [Test]
        public void It_buys_nothing_while_bodiless()
        {
            BotSnapshot s = Base();
            s.Stone = 99;
            s.Souls = 999;
            s.Bodiless = true;
            Assert.AreEqual(BotBuy.Nothing, BotRules.NextBuy(AutoplayStrategies.Rush, s));
        }

        [Test]
        public void A_bodiless_Jack_goes_to_the_Anchor_when_he_can_pay_to_revive()
        {
            BotSnapshot s = Base();
            s.Souls = 30;
            Assert.AreEqual(BodilessPlan.ToAnchor, BotRules.WhenBodiless(s, demonsAlive: true));
        }

        [Test]
        public void A_bodiless_Jack_possesses_a_demon_when_there_is_no_Anchor_and_he_can_pay()
        {
            BotSnapshot s = Base();
            s.AnchorStands = false;
            s.Souls = 150;
            Assert.AreEqual(BodilessPlan.ToDemon, BotRules.WhenBodiless(s, demonsAlive: true));
            Assert.AreEqual(BodilessPlan.ToAnchor, BotRules.WhenBodiless(s, demonsAlive: false));
        }

        [Test]
        public void A_bodiless_Jack_who_can_pay_for_a_possession_but_not_a_revive_possesses()
        {
            BotSnapshot s = Base();
            s.ReviveCost = 500;
            s.Souls = 150;
            Assert.AreEqual(BodilessPlan.ToDemon, BotRules.WhenBodiless(s, demonsAlive: true));
        }

        [Test]
        public void A_wisp_too_far_from_the_Anchor_to_reach_it_alive_takes_a_demon_beside_it()
        {
            BotSnapshot s = Base();
            s.Souls = 60;
            s.ReviveCost = 22;
            s.PossessCost = 55;
            s.DrainPerSecond = 3f;
            Assert.AreEqual(BodilessPlan.ToAnchor, BotRules.WhenBodiless(s, anchorDistance: 30f, demonDistance: 5f), "30 cells cost 15 Souls: 45 are left to revive");
            Assert.AreEqual(BodilessPlan.ToDemon, BotRules.WhenBodiless(s, anchorDistance: 150f, demonDistance: 5f), "150 cells would drain 75: it never arrives");
            Assert.AreEqual(BodilessPlan.ToAnchor, BotRules.WhenBodiless(s, anchorDistance: 150f, demonDistance: -1f), "no demon: the Anchor is all there is");
            Assert.AreEqual(BodilessPlan.ToDemon, BotRules.WhenBodiless(s, anchorDistance: -1f, demonDistance: 5f), "no Anchor: a demon is all there is");
        }

        [Test]
        public void When_neither_can_be_reached_alive_it_heads_for_the_nearer()
        {
            BotSnapshot s = Base();
            s.Souls = 10;
            s.ReviveCost = 22;
            s.PossessCost = 55;
            s.DrainPerSecond = 3f;
            Assert.AreEqual(BodilessPlan.ToDemon, BotRules.WhenBodiless(s, anchorDistance: 100f, demonDistance: 20f));
            Assert.AreEqual(BodilessPlan.ToAnchor, BotRules.WhenBodiless(s, anchorDistance: 20f, demonDistance: 100f));
        }

        private static PreyCandidate Prey(float distance, int witnesses = 0, int danger = 0, int company = 0, float fromAnchor = 10f) =>
            new(distance, fromAnchor, witnesses, danger, company);

        [Test]
        public void Prey_is_the_nearest_unless_the_strategy_avoids_witnesses()
        {
            var prey = new[] { Prey(5f, witnesses: 3), Prey(12f), Prey(30f) };
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Rush, prey));
            Assert.AreEqual(1, BotRules.PickPrey(AutoplayStrategies.Stealth, prey), "the lonelier one is worth a longer walk");
        }

        [Test]
        public void Prey_among_too_much_threat_is_left_alone_and_a_loner_is_worth_a_longer_walk()
        {
            Assert.AreEqual(1, BotRules.PickPrey(AutoplayStrategies.Balanced, new[] { Prey(4f, company: 5), Prey(10f) }), "a band of six is more than Jack can take");
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Rush, new[] { Prey(4f, company: 4) }), "rush takes on more");
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Balanced, new[] { Prey(4f, company: 4) }));
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Stealth, new[] { Prey(4f, company: 2) }));
            Assert.AreEqual(1, BotRules.PickPrey(AutoplayStrategies.Balanced, new[] { Prey(5f, company: 2), Prey(7f) }), "threat costs distance");
        }

        [Test]
        public void A_soldier_is_more_dangerous_than_a_peasant()
        {
            Assert.AreEqual(0, BotRules.DangerOf(MortalType.Peasant));
            Assert.Greater(BotRules.DangerOf(MortalType.Priest), BotRules.DangerOf(MortalType.Peasant));
            Assert.Greater(BotRules.DangerOf(MortalType.Soldier), BotRules.DangerOf(MortalType.Priest));
            Assert.AreEqual(BotRules.DangerOf(MortalType.Soldier), BotRules.DangerOf(MortalType.Knight));
        }

        [Test]
        public void Only_stealth_and_a_peasant_alone_go_together_and_a_lone_soldier_is_for_the_bolder_strategies()
        {
            var peasant = new[] { Prey(5f, danger: BotRules.DangerOf(MortalType.Peasant)) };
            var soldier = new[] { Prey(5f, danger: BotRules.DangerOf(MortalType.Soldier)) };
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Stealth, peasant));
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Stealth, soldier));
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Balanced, soldier));
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Rush, soldier));
        }

        [Test]
        public void Prey_far_from_the_Anchor_is_not_hunted()
        {
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Rush, new[] { Prey(5f, fromAnchor: BotRules.HuntRadius + 1f) }));
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Rush, new[] { Prey(5f, fromAnchor: BotRules.HuntRadius - 1f) }));
        }

        [Test]
        public void The_longer_it_goes_without_a_kill_the_further_afield_and_the_riskier_it_hunts()
        {
            var far = new[] { Prey(5f, fromAnchor: BotRules.HuntRadius + 10f) };
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Balanced, far));
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Balanced, far, desperation: 1));
            var band = new[] { Prey(5f, company: 5) };
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Balanced, band));
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Balanced, band, desperation: 2));
        }

        [Test]
        public void Desperation_grows_every_half_minute_without_a_kill_to_a_limit()
        {
            Assert.AreEqual(0, BotRules.Desperation(0f));
            Assert.AreEqual(0, BotRules.Desperation(BotRules.SecondsPerDesperation - 1f));
            Assert.AreEqual(1, BotRules.Desperation(BotRules.SecondsPerDesperation));
            Assert.AreEqual(BotRules.MaxDesperation, BotRules.Desperation(10000f));
        }

        [Test]
        public void Stealth_skips_prey_among_many_witnesses_altogether()
        {
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Stealth, new[] { Prey(3f, witnesses: 9) }));
            Assert.AreEqual(0, BotRules.PickPrey(AutoplayStrategies.Rush, new[] { Prey(3f, witnesses: 9) }));
            Assert.AreEqual(-1, BotRules.PickPrey(AutoplayStrategies.Rush, new PreyCandidate[0]));
        }

        [Test]
        public void The_next_Region_is_the_deepest_open_one()
        {
            var shallow = new RegionInfo { Id = "a", Depth = 0 };
            var deep = new RegionInfo { Id = "b", Depth = 2 };
            var tie = new RegionInfo { Id = "c", Depth = 2 };
            Assert.AreSame(deep, BotRules.NextRegion(new[] { shallow, tie, deep }), "deepest first, then by Id");
            Assert.IsNull(BotRules.NextRegion(new RegionInfo[0]));
        }
    }

    public class GridPathTests
    {
        // A 10x6 map; '#' cannot be stood on.
        private static System.Func<Int2, bool> Map(params string[] rows) => c => rows[rows.Length - 1 - c.y][c.x] != '#';

        [Test]
        public void A_path_goes_round_a_wall()
        {
            var walkable = Map(
                "..........",
                "....#.....",
                "....#.....",
                "....#.....",
                "....#.....",
                "..........");
            var path = new List<Int2>();
            Assert.IsTrue(GridPath.Find(10, 6, walkable, new Int2(0, 2), new Int2(9, 2), path));
            Assert.AreEqual(new Int2(9, 2), path.Last());
            Assert.IsTrue(path.All(walkable), "never through a wall");
            Assert.IsTrue(path.Any(c => c.y == 5 || c.y == 0), "round the end of the wall");
        }

        [Test]
        public void A_walled_in_goal_has_no_path()
        {
            var walkable = Map(
                ".....",
                ".###.",
                ".#.#.",
                ".###.");
            Assert.IsFalse(GridPath.Find(5, 4, walkable, new Int2(0, 0), new Int2(2, 1), new List<Int2>()));
        }

        [Test]
        public void A_goal_that_cannot_be_stood_on_has_no_path()
        {
            var walkable = Map("..#..");
            Assert.IsFalse(GridPath.Find(5, 1, walkable, new Int2(0, 0), new Int2(2, 0), new List<Int2>()));
        }

        [Test]
        public void The_path_does_not_cut_a_corner_of_a_wall()
        {
            var walkable = Map(
                ".#",
                "..");
            var path = new List<Int2>();
            Assert.IsTrue(GridPath.Find(2, 2, walkable, new Int2(0, 1), new Int2(1, 0), path));
            Assert.AreEqual(2, path.Count, "down, then across (a diagonal would cut the wall's corner)");
        }

        [Test]
        public void Start_equal_to_goal_is_an_empty_path()
        {
            var path = new List<Int2> { new Int2(1, 1) };
            Assert.IsTrue(GridPath.Find(3, 3, _ => true, new Int2(1, 1), new Int2(1, 1), path));
            Assert.IsEmpty(path);
        }

        [Test]
        public void The_nearest_walkable_cell_to_a_blocked_one_is_beside_it()
        {
            var walkable = Map("..#..");
            Assert.IsTrue(GridPath.NearestWalkable(5, 1, walkable, new Int2(2, 0), 3, out Int2 found));
            Assert.AreEqual(1, System.Math.Abs(found.x - 2));
            Assert.IsFalse(GridPath.NearestWalkable(5, 1, c => false, new Int2(2, 0), 3, out _));
        }
    }

    public class PathWalkerTests
    {
        [Test]
        public void It_steers_to_each_waypoint_in_turn()
        {
            var walker = new PathWalker();
            walker.Set(new[] { (0f, 5f), (5f, 5f) });
            Assert.IsTrue(walker.Steer(0f, 0f, 0.1f, out float dx, out float dy));
            Assert.AreEqual(0f, dx, 0.01f);
            Assert.AreEqual(1f, dy, 0.01f);
            Assert.IsTrue(walker.Steer(0f, 5f, 0.1f, out dx, out dy), "the first waypoint is reached; on to the second");
            Assert.AreEqual(1f, dx, 0.01f);
        }

        [Test]
        public void It_has_arrived_when_the_last_waypoint_is_reached()
        {
            var walker = new PathWalker();
            walker.Set(new[] { (1f, 0f) });
            Assert.IsFalse(walker.Steer(1f, 0f, 0.1f, out float dx, out float dy));
            Assert.AreEqual(0f, dx);
            Assert.AreEqual(0f, dy);
            Assert.IsFalse(walker.HasPath);
        }

        [Test]
        public void It_is_stuck_after_a_while_without_getting_closer_and_not_while_it_does()
        {
            var walker = new PathWalker { StuckAfter = 1f };
            walker.Set(new[] { (10f, 0f) });
            for (int i = 0; i < 20; i++) walker.Steer(i * 0.3f, 0f, 0.1f, out _, out _);
            Assert.IsFalse(walker.IsStuck, "moving along");
            for (int i = 0; i < 12; i++) walker.Steer(3f, 0f, 0.1f, out _, out _);
            Assert.IsTrue(walker.IsStuck, "standing still");
        }
    }

    public class AutoplayOptionsTests
    {
        private static AutoplayOptions Parse(params string[] args) => AutoplayOptions.FromArgs(args);

        [Test]
        public void No_flag_means_no_autoplay() => Assert.IsNull(Parse("-batchmode", "-nographics"));

        [Test]
        public void The_flag_alone_is_one_balanced_Run_with_the_Shrine_at_speed_8()
        {
            AutoplayOptions o = Parse("-autoplay");
            Assert.AreEqual(1, o.Runs);
            Assert.AreEqual(AnchorKind.Shrine, o.Anchor);
            Assert.AreSame(AutoplayStrategies.Balanced, o.Strategy);
            Assert.AreEqual(8f, o.Speed);
            Assert.IsTrue(o.QuitWhenDone, "a command-line batch quits when done");
        }

        [Test]
        public void Every_option_is_read()
        {
            AutoplayOptions o = Parse("-batchmode", "-autoplay", "runs=5", "seeds=3,7,9", "anchor=WarThrone", "strategy=stealth", "speed=4", "-logFile", "x.log");
            Assert.AreEqual(5, o.Runs);
            CollectionAssert.AreEqual(new[] { 3, 7, 9 }, o.Seeds);
            Assert.AreEqual(AnchorKind.WarThrone, o.Anchor);
            Assert.AreSame(AutoplayStrategies.Stealth, o.Strategy);
            Assert.AreEqual(4f, o.Speed);
        }

        [Test]
        public void Without_runs_each_seed_is_a_Run()
        {
            Assert.AreEqual(3, Parse("-autoplay", "seeds=1,2,3").Runs);
        }

        [Test]
        public void Seeds_are_used_in_turn_and_round_again()
        {
            AutoplayOptions o = Parse("-autoplay", "runs=4", "seeds=10,20");
            CollectionAssert.AreEqual(new[] { 10, 20, 10, 20 }, Enumerable.Range(0, 4).Select(o.SeedOf));
        }

        [Test]
        public void Several_anchors_and_strategies_play_every_pairing_on_every_seed()
        {
            AutoplayOptions o = Parse("-autoplay", "seeds=7,8", "anchor=Shrine,WarThrone", "strategy=stealth,rush,balanced");
            Assert.AreEqual(12, o.Runs, "2 seeds x 2 Anchors x 3 strategies");
            var pairs = Enumerable.Range(0, o.Runs).Select(i => (o.SeedOf(i), o.AnchorOf(i), o.StrategyOf(i).Name)).ToList();
            Assert.AreEqual(12, pairs.Distinct().Count());
            Assert.AreEqual((7, AnchorKind.Shrine, "stealth"), pairs[0]);
            Assert.AreEqual((7, AnchorKind.WarThrone, "balanced"), pairs[5]);
            Assert.AreEqual((8, AnchorKind.Shrine, "stealth"), pairs[6]);
        }

        [Test]
        public void One_anchor_and_strategy_keep_the_old_meaning()
        {
            AutoplayOptions o = Parse("-autoplay", "runs=2", "anchor=WarThrone", "strategy=rush");
            Assert.AreEqual(AnchorKind.WarThrone, o.AnchorOf(1));
            Assert.AreEqual("rush", o.StrategyOf(1).Name);
        }

        [Test]
        public void Without_seeds_each_Run_has_its_own()
        {
            AutoplayOptions o = Parse("-autoplay", "runs=3");
            Assert.AreEqual(3, Enumerable.Range(0, 3).Select(o.SeedOf).Distinct().Count());
        }

        [Test]
        public void Anchor_names_are_forgiving()
        {
            Assert.AreEqual(AnchorKind.WarThrone, Parse("-autoplay", "anchor=war_throne").Anchor);
            Assert.AreEqual(AnchorKind.Shrine, Parse("-autoplay", "anchor=SHRINE").Anchor);
        }

        [Test]
        public void Test_planet_options_are_read()
        {
            AutoplayOptions o = Parse("-autoplay", "rings=1,2", "regioncap=90", "out=C:/reports");
            CollectionAssert.AreEqual(new[] { 1, 2 }, o.Rings);
            Assert.AreEqual(90f, o.RegionSecondsCap);
            Assert.AreEqual("C:/reports", o.OutputDirectory);
        }

        [TestCase("runs=0")]
        [TestCase("runs=x")]
        [TestCase("seeds=1,b")]
        [TestCase("anchor=Tower")]
        [TestCase("strategy=turtle")]
        [TestCase("speed=0")]
        [TestCase("speed=fast")]
        [TestCase("rings=0")]
        [TestCase("flavour=salty")]
        [TestCase("runs")]
        public void A_wrong_word_is_refused_with_a_message(string word)
        {
            bool ok = AutoplayOptions.TryParse(new[] { "-autoplay", word }, out AutoplayOptions options, out string error);
            Assert.IsFalse(ok);
            Assert.IsNull(options);
            StringAssert.StartsWith("autoplay:", error);
        }
    }

    public class AutoplaySpeedTests
    {
        [Test]
        public void The_scale_is_the_one_asked_for_within_the_cap()
        {
            Assert.AreEqual(8f, AutoplaySpeed.Scale(8f, 1f / 240f));
            Assert.AreEqual(AutoplaySpeed.MaxScale, AutoplaySpeed.Scale(500f, 1f / 1000f));
            Assert.AreEqual(1f, AutoplaySpeed.Scale(0.2f, 1f / 60f), "never slower than normal");
        }

        [Test]
        public void A_long_frame_lowers_the_scale_so_a_frame_never_advances_more_than_the_slice()
        {
            float scale = AutoplaySpeed.Scale(16f, 1f / 30f);
            Assert.LessOrEqual(scale / 30f, AutoplaySpeed.MaxFrameGameSeconds + 0.0001f);
            Assert.Greater(scale, 1f);
        }

        [Test]
        public void The_physics_step_stays_usual_at_normal_speed_and_grows_a_little_when_fast_but_is_capped()
        {
            Assert.AreEqual(AutoplaySpeed.NormalFixedStep, AutoplaySpeed.FixedStep(1f));
            Assert.AreEqual(AutoplaySpeed.NormalFixedStep, AutoplaySpeed.FixedStep(4f));
            Assert.Greater(AutoplaySpeed.FixedStep(8f), AutoplaySpeed.NormalFixedStep);
            Assert.AreEqual(AutoplaySpeed.FastFixedStep, AutoplaySpeed.FixedStep(AutoplaySpeed.MaxScale));
        }
    }

    public class RunReportTests
    {
        private static RunReport Sample()
        {
            var report = new RunReport { Index = 2, Seed = 77, Anchor = "Shrine", Strategy = "stealth", Speed = 8f, Result = RunResult.Won, RegionsTotal = 2, RealSeconds = 90.5f, Deaths = 1, SoulLevel = 3 };
            report.Frames = FrameTimes.Of(new float[] { 5f, 6f, 7f, 8f, 100f });
            report.Regions.Add(new RegionReport
            {
                Id = "r0", Name = "Green Pastures, North", Outcome = "Captured", RealSeconds = 40f, GameSeconds = 300f, SoulsEarned = 60, SoulsSpent = 30,
                HighestVigilance = VigilanceStage.Alarmed, WavesFaced = 2, RulersSlain = 1, PortalsBuilt = 3, WorkersSummoned = 1,
            });
            report.Regions.Add(new RegionReport
            {
                Id = "r1", Name = "Capital", IsCapital = true, Outcome = "Captured", RealSeconds = 50.5f, GameSeconds = 400f, SoulsEarned = 10, SoulsSpent = 5,
                HighestVigilance = VigilanceStage.Mobilized, WavesFaced = 4, RulersEscaped = 1, PortalsBuilt = 2,
            });
            return report;
        }

        [Test]
        public void Frame_percentiles_use_the_nearest_rank()
        {
            var times = new List<float>();
            for (int i = 1; i <= 100; i++) times.Add(i);
            FrameTimes t = FrameTimes.Of(times);
            Assert.AreEqual(100, t.Count);
            Assert.AreEqual(50f, t.P50);
            Assert.AreEqual(95f, t.P95);
            Assert.AreEqual(99f, t.P99);
            Assert.AreEqual(100f, t.Max);
        }

        [Test]
        public void Frame_percentiles_of_no_frames_are_zero() => Assert.AreEqual(0, FrameTimes.Of(new List<float>()).Count);

        [Test]
        public void The_totals_add_up_the_Regions()
        {
            RunReport r = Sample();
            Assert.AreEqual(2, r.RegionsCaptured);
            Assert.AreEqual(700f, r.GameSeconds);
            Assert.AreEqual(70, r.SoulsEarned);
            Assert.AreEqual(35, r.SoulsSpent);
            Assert.AreEqual(6, r.WavesFaced);
            Assert.AreEqual(1, r.RulersSlain);
            Assert.AreEqual(1, r.RulersEscaped);
            Assert.AreEqual(5, r.PortalsBuilt);
            Assert.AreEqual(VigilanceStage.Mobilized, r.HighestVigilance);
        }

        [Test]
        public void The_JSON_has_every_figure_the_ticket_asks_for()
        {
            string json = Sample().ToJson();
            foreach (string field in new[]
                     {
                         "\"result\": \"Won\"", "\"regions\"", "\"realSeconds\"", "\"gameSeconds\"", "\"deaths\": 1", "\"soulsEarned\": 70", "\"soulsSpent\": 35",
                         "\"highestVigilance\": \"Mobilized\"", "\"wavesFaced\": 6", "\"rulersSlain\": 1", "\"rulersEscaped\": 1", "\"portalsBuilt\": 5",
                         "\"frameMs\"", "\"p50\"", "\"p95\"", "\"p99\"", "\"errorsLogged\": 0", "\"inventory\"", "\"stable\": true",
                     })
                StringAssert.Contains(field, json);
        }

        [Test]
        public void The_JSON_escapes_text()
        {
            RunReport r = Sample();
            r.ErrorMessages.Add("bad \"thing\"\nline two \\ tab\t");
            r.ErrorsLogged = 1;
            string json = r.ToJson();
            StringAssert.Contains("bad \\\"thing\\\"\\nline two \\\\ tab\\t", json);
        }

        [Test]
        public void The_JSON_braces_balance()
        {
            string json = Sample().ToJson();
            Assert.AreEqual(json.Count(c => c == '{'), json.Count(c => c == '}'));
            Assert.AreEqual(json.Count(c => c == '['), json.Count(c => c == ']'));
        }

        [Test]
        public void A_CSV_row_has_a_cell_per_header_column_even_when_a_name_has_a_comma()
        {
            RunReport r = Sample();
            string row = r.ToCsvRow();
            Assert.AreEqual(RunReport.CsvHeader.Split(',').Length, CountCells(row));
            StringAssert.Contains("\"Green Pastures, North:40/300;Capital:50.5/400\"", row);
        }

        private static int CountCells(string row)
        {
            int cells = 1;
            bool quoted = false;
            foreach (char c in row)
            {
                if (c == '"') quoted = !quoted;
                else if (c == ',' && !quoted) cells++;
            }
            return cells;
        }

        [Test]
        public void A_batch_summary_counts_the_results_and_exits_clean_when_nothing_went_wrong()
        {
            var batch = new BatchSummary { Strategy = "rush", Anchor = "Shrine", Speed = 8f };
            batch.Runs.Add(Sample());
            batch.Runs.Add(new RunReport { Index = 3, Result = RunResult.Lost });
            Assert.AreEqual(1, batch.Count(RunResult.Won));
            Assert.AreEqual(1, batch.Count(RunResult.Lost));
            Assert.AreEqual(0, batch.ExitCode);
            StringAssert.Contains("\"won\": 1", batch.ToJson());
            StringAssert.Contains("\"lost\": 1", batch.ToJson());
        }

        [Test]
        public void Logged_errors_or_a_leaked_inventory_make_the_exit_code_2_and_a_stalled_Run_3()
        {
            var errors = new BatchSummary();
            errors.Runs.Add(new RunReport { Result = RunResult.Won, ErrorsLogged = 2 });
            Assert.AreEqual(2, errors.ExitCode);

            var leak = new BatchSummary();
            leak.Runs.Add(new RunReport { Result = RunResult.Won, InventoryStable = false });
            Assert.AreEqual(2, leak.ExitCode);

            var stalled = new BatchSummary();
            stalled.Runs.Add(new RunReport { Result = RunResult.Stalled });
            Assert.AreEqual(3, stalled.ExitCode);
        }
    }
}
