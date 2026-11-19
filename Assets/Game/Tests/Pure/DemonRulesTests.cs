using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>A Portal's standing orders as posts, and how demons choose what to do inside them.</summary>
    public class DemonRulesTests
    {
        private static DemonPost Post(PortalOrder order, bool hasMark = true, bool hasJack = true) =>
            DemonPosts.Resolve(order, 1f, 2f, hasMark, 30f, 40f, hasJack, 5f, 6f);

        [Test]
        public void Guard_gathers_at_the_rally_point()
        {
            DemonPost post = Post(PortalOrder.Guard);
            Assert.AreEqual(PortalOrder.Guard, post.Order);
            Assert.AreEqual((1f, 2f), (post.X, post.Y));
            Assert.AreEqual(DemonTuning.GuardLeash, post.Leash);
        }

        [Test]
        public void Raid_gathers_at_the_mark_and_Follow_at_Jack()
        {
            DemonPost raid = Post(PortalOrder.Raid);
            Assert.AreEqual((PortalOrder.Raid, 30f, 40f), (raid.Order, raid.X, raid.Y));
            DemonPost follow = Post(PortalOrder.Follow);
            Assert.AreEqual((PortalOrder.Follow, 5f, 6f), (follow.Order, follow.X, follow.Y));
        }

        [Test]
        public void An_order_with_no_point_to_go_to_does_what_Guard_does()
        {
            Assert.AreEqual(PortalOrder.Guard, Post(PortalOrder.Raid, hasMark: false).Order, "no mark yet");
            Assert.AreEqual(PortalOrder.Guard, Post(PortalOrder.Follow, hasJack: false).Order, "Jack has no body");
            Assert.AreEqual(1f, Post(PortalOrder.Raid, hasMark: false).X);
        }

        [Test]
        public void Demons_stand_in_slots_and_not_on_each_other()
        {
            DemonPosts.SlotOffset(0, out float ax, out float ay);
            DemonPosts.SlotOffset(1, out float bx, out float by);
            Assert.AreEqual(DemonTuning.SlotRadius, System.MathF.Sqrt(ax * ax + ay * ay), 1e-3f);
            Assert.Greater(System.MathF.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by)), 1f);
        }

        private static BehaviourAction Decide(DemonType type, BehaviourContext c, BehaviourAction current = BehaviourAction.Idle) =>
            BehaviourScoring.Choose(DemonProfiles.For(type), c, current);

        private static BehaviourContext Context(float distanceHome = 0f, bool threat = false, float closeness = 0.5f)
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.Leash = DemonTuning.GuardLeash;
            c.DistanceHome = distanceHome;
            c.ThreatSeen = threat;
            c.ThreatCloseness = closeness;
            return c;
        }

        [Test]
        public void A_calm_imp_at_its_post_idles()
        {
            Assert.AreEqual(BehaviourAction.Idle, Decide(DemonType.Imp, Context()));
        }

        [Test]
        public void An_imp_fights_a_threat_inside_its_leash()
        {
            Assert.AreEqual(BehaviourAction.Fight, Decide(DemonType.Imp, Context(distanceHome: 3f, threat: true)));
        }

        [Test]
        public void An_imp_past_its_leash_goes_back_even_with_a_threat_in_sight()
        {
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(DemonType.Imp, Context(distanceHome: 8f, threat: true), BehaviourAction.Fight));
        }

        [Test]
        public void An_imp_away_from_its_post_goes_back_when_nothing_is_about()
        {
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(DemonType.Imp, Context(distanceHome: 8f)));
        }

        [Test]
        public void A_bone_demon_works_the_piles_when_calm_and_fights_when_it_must()
        {
            Assert.AreEqual(BehaviourAction.Work, Decide(DemonType.BoneDemon, Context(distanceHome: 2f)));
            Assert.AreEqual(BehaviourAction.Fight, Decide(DemonType.BoneDemon, Context(distanceHome: 2f, threat: true)));
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(DemonType.BoneDemon, Context(distanceHome: 8f)));
        }

        [Test]
        public void An_imp_never_works_and_nothing_flees()
        {
            foreach (DemonType type in new[] { DemonType.Imp, DemonType.BoneDemon, DemonType.Skeleton })
            {
                BehaviourProfile profile = DemonProfiles.For(type);
                Assert.IsFalse(profile.Of(BehaviourAction.Flee).Enabled, type + " does not flee");
                Assert.AreEqual(type == DemonType.BoneDemon, profile.Of(BehaviourAction.Work).Enabled, type + " work");
            }
        }

        [Test]
        public void A_trait_can_still_change_a_demon_through_data()
        {
            BehaviourProfile cowardly = DemonProfiles.For(DemonType.Imp).With(BehaviourChange.On(BehaviourAction.Flee, 0f, new BehaviourTerm(BehaviourFactor.Health, 6f, 0.75f, 0.7f)));
            BehaviourContext c = Context(threat: true);
            c.Health = 0.5f;
            Assert.AreEqual(BehaviourAction.Flee, BehaviourScoring.Choose(cowardly, c, BehaviourAction.Fight));
            Assert.AreEqual(BehaviourAction.Fight, BehaviourScoring.Choose(DemonProfiles.For(DemonType.Imp), c, BehaviourAction.Fight), "the Imp's own profile is unchanged");
        }
    }
}
