using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>Every Play Mode test keeps meta progress in memory: tests never touch the player's real Shards and upgrades.</summary>
    [SetUpFixture]
    public class MetaTestSetup
    {
        [OneTimeSetUp]
        public void UseMemory()
        {
            MetaProgress.Store = new MemoryKeyValueStore();
            MetaProgress.Wipe();
        }

        [OneTimeTearDown]
        public void Restore()
        {
            MetaProgress.Wipe();
            MetaProgress.Store = null;
        }
    }
}
