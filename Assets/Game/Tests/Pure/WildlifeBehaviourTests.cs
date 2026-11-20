using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>What each kind of wildlife does in a given situation (the shared weighted behaviours, as wildlife data), and what a wildlife spot produces.</summary>
    public class WildlifeBehaviourTests
    {
        private static BehaviourAction Decide(WildlifeType type, BehaviourContext context, BehaviourAction current = BehaviourAction.Idle) =>
            BehaviourScoring.Choose(WildlifeProfiles.For(type).Behaviour, context, current);

        private static BehaviourContext Calm(WildlifeType type)
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.Leash = WildlifeProfiles.For(type).LeashCells;
            return c;
        }

        [Test]
        public void Slimes_are_neutral_and_werewolves_and_werebears_hostile()
        {
            Assert.AreEqual(Side.Neutral, WildlifeProfiles.For(WildlifeType.Slime).Side);
            Assert.AreEqual(Side.Hostile, WildlifeProfiles.For(WildlifeType.Werewolf).Side);
            Assert.AreEqual(Side.Hostile, WildlifeProfiles.For(WildlifeType.Werebear).Side);
            Assert.IsTrue(WildlifeProfiles.For(WildlifeType.Slime).OnlyFightsBack);
            Assert.IsFalse(WildlifeProfiles.For(WildlifeType.Werewolf).OnlyFightsBack);
        }

        [Test]
        public void Calm_wildlife_strolls_about_its_spot()
        {
            foreach (WildlifeType type in WildlifeProfiles.Roster)
                Assert.AreEqual(BehaviourAction.Work, Decide(type, Calm(type)), type.ToString());
        }

        [Test]
        public void Wildlife_with_a_threat_in_sight_fights_it()
        {
            foreach (WildlifeType type in WildlifeProfiles.Roster)
            {
                BehaviourContext c = Calm(type);
                c.ThreatSeen = true;
                c.ThreatCloseness = 0.5f;
                Assert.AreEqual(BehaviourAction.Fight, Decide(type, c, BehaviourAction.Work), type.ToString());
            }
        }

        [Test]
        public void A_badly_hurt_slime_or_werewolf_flees_but_a_werebear_fights_to_the_death()
        {
            foreach (WildlifeType type in new[] { WildlifeType.Slime, WildlifeType.Werewolf, WildlifeType.Werebear })
            {
                BehaviourContext c = Calm(type);
                c.ThreatSeen = true;
                c.Health = 0.1f;
                c.Fear = 1f;
                BehaviourAction expected = type == WildlifeType.Werebear ? BehaviourAction.Fight : BehaviourAction.Flee;
                Assert.AreEqual(expected, Decide(type, c, BehaviourAction.Fight), type.ToString());
            }
        }

        [Test]
        public void A_wounded_creature_at_peace_does_not_flee_forever()
        {
            BehaviourContext c = Calm(WildlifeType.Werewolf);
            c.Health = 0.1f;
            Assert.AreNotEqual(BehaviourAction.Flee, Decide(WildlifeType.Werewolf, c));
        }

        [Test]
        public void A_healthy_frightened_creature_still_fights()
        {
            BehaviourContext c = Calm(WildlifeType.Slime);
            c.ThreatSeen = true;
            c.Fear = 1f;
            Assert.AreEqual(BehaviourAction.Fight, Decide(WildlifeType.Slime, c, BehaviourAction.Fight));
        }

        [Test]
        public void A_creature_far_from_its_spot_turns_back()
        {
            BehaviourContext c = Calm(WildlifeType.Werewolf);
            c.DistanceHome = c.Leash * 2.5f;
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(WildlifeType.Werewolf, c));
            c.ThreatSeen = true;
            Assert.AreEqual(BehaviourAction.ReturnHome, Decide(WildlifeType.Werewolf, c, BehaviourAction.Fight), "even a hunting wolf gives up a far chase");
        }

        [Test]
        public void Wildlife_never_purifies_or_patrols()
        {
            foreach (WildlifeType type in WildlifeProfiles.Roster)
            {
                BehaviourProfile p = WildlifeProfiles.For(type).Behaviour;
                Assert.IsFalse(p.Of(BehaviourAction.Purify).Enabled);
                Assert.IsFalse(p.Of(BehaviourAction.Patrol).Enabled);
            }
        }

        [Test]
        public void Every_kind_can_be_picked_at_a_spot_and_groups_stay_in_range()
        {
            var seen = new System.Collections.Generic.HashSet<WildlifeType>();
            for (int i = 0; i < 100; i++)
            {
                WildlifeSpawns.Choice choice = WildlifeSpawns.Pick(i / 100d);
                seen.Add(choice.Type);
                for (int j = 0; j < 10; j++)
                {
                    int n = WildlifeSpawns.GroupSize(choice, j / 10d);
                    Assert.That(n, Is.InRange(choice.GroupMin, choice.GroupMax));
                }
            }
            Assert.AreEqual(WildlifeProfiles.Roster.Count(), seen.Count);
        }

        [Test]
        public void Slimes_are_the_commonest_spot_and_bears_the_rarest()
        {
            var counts = WildlifeSpawns.Choices.ToDictionary(c => c.Type, c => c.Weight);
            Assert.Greater(counts[WildlifeType.Slime], counts[WildlifeType.Werewolf]);
            Assert.Greater(counts[WildlifeType.Werewolf], counts[WildlifeType.Werebear]);
        }
    }
}
