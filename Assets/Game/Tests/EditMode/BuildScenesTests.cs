using System.IO;
using System.Linq;
using Blondie.GameSetup;
using NUnit.Framework;
using UnityEditor;

namespace Isekai.Tests
{
    public class BuildScenesTests
    {
        // Game Setup loads its scenes by name, so each one it names must be in the build and enabled.
        [Test]
        public void Every_GameSetup_scene_is_an_enabled_build_scene()
        {
            GameSetupSettings s = GameSetupSettings.Current;
            string[] enabled = EditorBuildSettings.scenes.Where(b => b.enabled).Select(b => Path.GetFileNameWithoutExtension(b.path)).ToArray();
            foreach (string name in new[] { s.BootScene, s.MainMenuScene, s.GameScene })
                Assert.That(enabled, Does.Contain(name), $"'{name}' is not an enabled scene in Build Settings.");
        }

        [Test]
        public void Boot_is_the_first_build_scene()
        {
            Assert.That(Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path), Is.EqualTo(GameSetupSettings.Current.BootScene));
        }
    }
}
