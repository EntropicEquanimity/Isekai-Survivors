using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class AlarmRulesTests
    {
        [TestCase(0, false)]
        [TestCase(4, false)]
        [TestCase(5, true)]
        [TestCase(12, true)]
        public void Five_or_more_piles_seen_raise_the_alarm(int piles, bool raises) =>
            Assert.AreEqual(raises, AlarmRules.Raises(piles));

        [Test]
        public void A_rider_has_arrived_inside_the_Seat_or_at_its_walls_not_one_cell_short()
        {
            var interior = new CellRect(40, 40, 50, 50);
            int m = AlarmTuning.SeatArriveMargin;

            Assert.IsTrue(AlarmRules.HasArrived(new Int2(45, 45), interior), "inside");
            Assert.IsTrue(AlarmRules.HasArrived(new Int2(40 - m, 45), interior), "at the walls");
            Assert.IsFalse(AlarmRules.HasArrived(new Int2(40 - m - 1, 45), interior), "one cell from the Seat has not arrived");
            Assert.IsFalse(AlarmRules.HasArrived(new Int2(45, 50 + m + 1), interior));
        }

        [Test]
        public void An_alerted_area_covers_its_radius_until_it_runs_out()
        {
            var areas = new AlertedAreas();
            areas.Mark(new Int2(10, 10), 5, until: 100f);

            Assert.IsTrue(areas.Contains(new Int2(13, 14), 50f), "inside the radius");
            Assert.IsFalse(areas.Contains(new Int2(20, 10), 50f), "outside it");
            Assert.IsFalse(areas.Contains(new Int2(10, 10), 100f), "after it runs out");
            Assert.AreEqual(0, areas.Count, "an area that ran out is forgotten");
        }

        [Test]
        public void Being_spotted_raises_once_per_cooldown()
        {
            var limiter = new SpotLimiter(20f);

            Assert.IsTrue(limiter.TryRaise(5f));
            Assert.IsFalse(limiter.TryRaise(5.1f), "standing in sight is not a raise per frame");
            Assert.IsFalse(limiter.TryRaise(24.9f));
            Assert.IsTrue(limiter.TryRaise(25f));
        }

        [TestCase(UnitKind.Mortal, true)]
        [TestCase(UnitKind.Wildlife, true)]
        [TestCase(UnitKind.Demon, false)]
        [TestCase(UnitKind.Character, false)]
        [TestCase(UnitKind.Building, false)]
        public void Mortal_and_wildlife_deaths_leave_bones(UnitKind kind, bool leaves) =>
            Assert.AreEqual(leaves, BoneRules.LeavesBones(kind));

        [Test]
        public void A_pile_crumbles_through_its_looks_and_is_gone_at_the_end()
        {
            Assert.AreEqual(0, BoneRules.Stage(0f));
            Assert.AreEqual(3, BoneRules.Stage(BoneRules.CrumbleSeconds - 0.1f));
            int last = -1;
            for (float age = 0f; age < BoneRules.CrumbleSeconds; age += 1f)
            {
                int stage = BoneRules.Stage(age);
                Assert.GreaterOrEqual(stage, last, "it only ever crumbles further");
                last = stage;
            }
            Assert.IsFalse(BoneRules.Expired(BoneRules.CrumbleSeconds - 1f));
            Assert.IsTrue(BoneRules.Expired(BoneRules.CrumbleSeconds));
        }

        [Test]
        public void Wildlife_leaves_beast_bones_and_mortals_human_ones()
        {
            Assert.AreEqual(BoneKind.Human, BoneRules.KindOf(UnitKind.Mortal));
            Assert.AreEqual(BoneKind.Beast, BoneRules.KindOf(UnitKind.Wildlife));
        }
    }
}
