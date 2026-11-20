using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class SupplyTests
    {
        [Test]
        public void Farms_and_villages_make_Supply_and_nothing_else_does()
        {
            Assert.Greater(SupplyRules.PerMinute(SettlementKind.Farm, false), 0f);
            Assert.Greater(SupplyRules.PerMinute(SettlementKind.Village, false), 0f);
            Assert.AreEqual(0f, SupplyRules.PerMinute(SettlementKind.Camp, false));
            Assert.AreEqual(0f, SupplyRules.PerMinute(SettlementKind.Seat, false));
        }

        [Test]
        public void Razing_a_farm_or_village_cuts_the_Supply()
        {
            var standing = new[] { (SettlementKind.Farm, false), (SettlementKind.Farm, false), (SettlementKind.Village, false) };
            var oneRazed = new[] { (SettlementKind.Farm, true), (SettlementKind.Farm, false), (SettlementKind.Village, false) };
            Assert.Less(SupplyRules.RegionPerMinute(oneRazed), SupplyRules.RegionPerMinute(standing));
            Assert.AreEqual(0f, SupplyRules.RegionPerMinute(standing.Select(s => (s.Item1, true))));
        }

        [Test]
        public void Supply_is_made_over_time_and_spent_on_troops()
        {
            var pool = new SupplyPool(0f);
            pool.Produce(60f, 30f);
            Assert.AreEqual(30f, pool.Amount, 1e-4f);
            Assert.IsFalse(pool.TryTake(31f), "not enough");
            Assert.AreEqual(30f, pool.Amount, 1e-4f);
            Assert.IsTrue(pool.TryTake(10f));
            Assert.AreEqual(20f, pool.Amount, 1e-4f);
        }

        [Test]
        public void An_undetected_Region_raises_no_troops_however_long_it_waits()
        {
            var pool = new SupplyPool(1000f);
            var seat = new TroopRaiser(SettlementKind.Seat);
            for (int i = 0; i < 1000; i++) Assert.IsFalse(seat.Tick(10f, VigilanceStage.Unaware, false, pool));
            Assert.AreEqual(0, seat.Raised);
            Assert.AreEqual(1000f, pool.Amount, 1e-4f, "no Supply spent");
        }

        [Test]
        public void An_alerted_Region_raises_troops_from_Supply_at_the_Seat_and_towns_only()
        {
            var pool = new SupplyPool(1000f);
            var seat = new TroopRaiser(SettlementKind.Seat);
            var village = new TroopRaiser(SettlementKind.Village);
            var farm = new TroopRaiser(SettlementKind.Farm);
            var camp = new TroopRaiser(SettlementKind.Camp);
            for (int i = 0; i < 20; i++)
                foreach (TroopRaiser r in new[] { seat, village, farm, camp }) r.Tick(5f, VigilanceStage.Uneasy, false, pool);
            Assert.Greater(seat.Raised, 0);
            Assert.Greater(village.Raised, 0);
            Assert.AreEqual(0, farm.Raised);
            Assert.AreEqual(0, camp.Raised);
            Assert.AreEqual(1000f - SupplyTuning.TroopCost * (seat.Raised + village.Raised), pool.Amount, 1e-3f);
        }

        [Test]
        public void An_area_alerted_by_alarms_raises_troops_though_Vigilance_is_Unaware()
        {
            var pool = new SupplyPool(1000f);
            var seat = new TroopRaiser(SettlementKind.Seat);
            for (int i = 0; i < 30; i++) seat.Tick(5f, VigilanceStage.Unaware, true, pool);
            Assert.Greater(seat.Raised, 0);
        }

        [Test]
        public void Troops_come_faster_at_each_stage()
        {
            float? previous = null;
            foreach (VigilanceStage stage in new[] { VigilanceStage.Uneasy, VigilanceStage.Alarmed, VigilanceStage.Mobilized, VigilanceStage.Crusade })
            {
                float interval = SupplyRules.TroopInterval(stage, false).Value;
                if (previous != null) Assert.Less(interval, previous.Value, stage.ToString());
                previous = interval;
            }
        }

        [Test]
        public void A_stronghold_stops_at_its_limit_however_fast_and_rich()
        {
            var pool = new SupplyPool(100000f);
            var village = new TroopRaiser(SettlementKind.Village);
            for (int i = 0; i < 1000; i++) village.Tick(10f, VigilanceStage.Crusade, true, pool);
            Assert.AreEqual(SupplyRules.TroopLimit(SettlementKind.Village), village.Raised);
        }

        [Test]
        public void Without_Supply_no_troop_is_raised_until_Supply_comes()
        {
            var pool = new SupplyPool(0f);
            var seat = new TroopRaiser(SettlementKind.Seat);
            for (int i = 0; i < 20; i++) Assert.IsFalse(seat.Tick(10f, VigilanceStage.Crusade, true, pool));
            pool.Add(SupplyTuning.TroopCost);
            Assert.IsTrue(seat.Tick(0.1f, VigilanceStage.Crusade, true, pool), "ready as soon as it can pay");
        }

        [Test]
        public void Patrols_grow_with_the_stage_and_never_shrink()
        {
            int previous = -1;
            foreach (VigilanceStage stage in new[] { VigilanceStage.Unaware, VigilanceStage.Uneasy, VigilanceStage.Alarmed, VigilanceStage.Mobilized, VigilanceStage.Crusade })
            {
                int count = SupplyRules.PatrolCount(stage);
                Assert.Greater(count, previous, stage.ToString());
                previous = count;
            }
            Assert.AreEqual(SupplyRules.PatrolCount(VigilanceStage.Alarmed), SupplyRules.PatrolsToAdd(VigilanceStage.Alarmed, 0));
            Assert.AreEqual(0, SupplyRules.PatrolsToAdd(VigilanceStage.Uneasy, 5), "never negative");
        }
    }
}
