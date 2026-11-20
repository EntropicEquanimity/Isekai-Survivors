using NUnit.Framework;

namespace Isekai.Tests
{
    public class AnchorTests
    {
        [Test]
        public void Only_the_Shrine_starts_hidden()
        {
            Assert.IsTrue(AnchorTuning.StartsHidden(AnchorKind.Shrine));
            Assert.IsFalse(AnchorTuning.StartsHidden(AnchorKind.WarThrone));
        }

        [Test]
        public void Vigilance_starts_at_Uneasy_only_with_the_War_Throne()
        {
            Assert.AreEqual(VigilanceStage.Unaware, AnchorTuning.StartStage(AnchorKind.Shrine));
            Assert.AreEqual(VigilanceStage.Uneasy, AnchorTuning.StartStage(AnchorKind.WarThrone));
        }

        [Test]
        public void Only_the_War_Throne_has_an_aura()
        {
            Assert.IsTrue(AnchorTuning.HasAura(AnchorKind.WarThrone));
            Assert.IsFalse(AnchorTuning.HasAura(AnchorKind.Shrine));
            Assert.Greater(AnchorTuning.AuraDamageBonus, 0f);
        }

        [Test]
        public void The_War_Throne_spreads_corruption_faster_than_the_Shrine()
        {
            CorruptionSource shrine = AnchorTuning.Source(AnchorKind.Shrine);
            CorruptionSource throne = AnchorTuning.Source(AnchorKind.WarThrone);
            Assert.Less(throne.SecondsPerStep, shrine.SecondsPerStep);
            Assert.GreaterOrEqual(throne.Radius, shrine.Radius);
        }
    }
}
