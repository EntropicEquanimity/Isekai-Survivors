using Blondie.Save;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>
    /// Every PlayMode test saves into memory: a Run saved on the Planet screen (or deleted when it ends) never touches
    /// the developer's real save file.
    /// </summary>
    [SetUpFixture]
    public sealed class TestSaveStorage
    {
        private ISaveStorage _real;

        [OneTimeSetUp]
        public void UseMemory()
        {
            _real = SaveSystem.Storage;
            SaveSystem.Storage = new MemorySaveStorage();
        }

        [OneTimeTearDown]
        public void RestoreStorage() => SaveSystem.Storage = _real;
    }
}
