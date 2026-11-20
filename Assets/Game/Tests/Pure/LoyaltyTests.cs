using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>Demon levels from kills, Loyalty against Jack's soul level, and going rogue.</summary>
    public class LoyaltyTests
    {
        [Test]
        public void A_demon_levels_up_from_kills()
        {
            var demon = new LoyaltyState();
            Assert.AreEqual(1, demon.Level);
            for (int i = 0; i < LoyaltyRules.XpToReach(2) - 1; i++) Assert.IsFalse(demon.AddXp(LoyaltyTuning.XpPerKill));
            Assert.AreEqual(1, demon.Level);
            Assert.IsTrue(demon.AddXp(LoyaltyTuning.XpPerKill), "the kill that fills the bar is a level up");
            Assert.AreEqual(2, demon.Level);
        }

        [Test]
        public void Each_level_takes_more_kills_than_the_last()
        {
            Assert.AreEqual(0, LoyaltyRules.XpToReach(1));
            Assert.Greater(LoyaltyRules.XpToReach(3) - LoyaltyRules.XpToReach(2), LoyaltyRules.XpToReach(2) - LoyaltyRules.XpToReach(1));
            Assert.AreEqual(3, LoyaltyRules.LevelFor(LoyaltyRules.XpToReach(3)));
            Assert.AreEqual(2, LoyaltyRules.LevelFor(LoyaltyRules.XpToReach(3) - 1));
        }

        [Test]
        public void Loyalty_drops_only_while_the_demons_level_is_above_Jacks_soul_level()
        {
            var demon = new LoyaltyState();
            demon.AddXp(LoyaltyRules.XpToReach(2));
            Assert.IsFalse(demon.Tick(10f, soulLevel: 2));
            Assert.AreEqual(LoyaltyTuning.MaxLoyalty, demon.Loyalty, "level equal to the soul level: no drain");
            demon.Tick(10f, soulLevel: 1);
            Assert.AreEqual(LoyaltyTuning.MaxLoyalty - 10f * LoyaltyTuning.DrainPerLevelAbove, demon.Loyalty, 0.001f);
        }

        [Test]
        public void The_further_above_the_faster_it_drops()
        {
            Assert.Less(LoyaltyRules.LoyaltyChange(4, 1, 1f), LoyaltyRules.LoyaltyChange(2, 1, 1f));
        }

        [Test]
        public void Loyalty_recovers_slowly_when_the_demon_is_back_in_line_but_never_past_the_maximum()
        {
            var demon = new LoyaltyState();
            demon.SetLoyalty(40f);
            demon.Tick(5f, soulLevel: 3);
            Assert.AreEqual(40f + 5f * LoyaltyTuning.RecoverPerSecond, demon.Loyalty, 0.001f);
            demon.Tick(1000f, soulLevel: 3);
            Assert.AreEqual(LoyaltyTuning.MaxLoyalty, demon.Loyalty);
        }

        [Test]
        public void At_zero_Loyalty_the_demon_turns_rogue_once()
        {
            var demon = new LoyaltyState();
            demon.AddXp(LoyaltyRules.XpToReach(3));
            demon.SetLoyalty(2f);
            Assert.IsFalse(demon.Tick(0.1f, 1));
            Assert.IsTrue(demon.Tick(5f, 1), "Loyalty hit zero");
            Assert.IsTrue(demon.IsRogue);
            Assert.AreEqual(0f, demon.Loyalty);
            Assert.IsFalse(demon.Tick(5f, 1), "it turns rogue only once");
            demon.Tick(1000f, 5);
            Assert.AreEqual(0f, demon.Loyalty, "a rogue demon's Loyalty does not recover by itself");
        }

        [Test]
        public void Only_nearby_low_Loyalty_demons_follow_a_revolt()
        {
            Assert.IsTrue(LoyaltyRules.FollowsRevolt(LoyaltyTuning.FollowRadius - 1f, LoyaltyTuning.FollowBelow - 1f));
            Assert.IsFalse(LoyaltyRules.FollowsRevolt(LoyaltyTuning.FollowRadius + 1f, 0f), "too far");
            Assert.IsFalse(LoyaltyRules.FollowsRevolt(1f, LoyaltyTuning.FollowBelow), "loyal enough");
        }

        [Test]
        public void Winning_it_back_makes_it_loyal_again_and_a_new_life_starts_over()
        {
            var demon = new LoyaltyState();
            demon.AddXp(LoyaltyRules.XpToReach(3));
            demon.GoRogue();
            Assert.IsFalse(demon.GoRogue());
            demon.WinBack();
            Assert.IsFalse(demon.IsRogue);
            Assert.AreEqual(LoyaltyTuning.WonBackLoyalty, demon.Loyalty);
            Assert.AreEqual(3, demon.Level, "it keeps its level");
            demon.Reset();
            Assert.AreEqual((1, LoyaltyTuning.MaxLoyalty), (demon.Level, demon.Loyalty));
        }

        [Test]
        public void A_rogue_demon_surrenders_at_low_health()
        {
            Assert.IsTrue(LoyaltyRules.Surrenders(LoyaltyTuning.SurrenderHealth));
            Assert.IsFalse(LoyaltyRules.Surrenders(0.8f));
        }
    }
}
