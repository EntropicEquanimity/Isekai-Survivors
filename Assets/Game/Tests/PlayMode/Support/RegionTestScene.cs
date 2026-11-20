using System.Collections;
using Blondie.Pooling;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Loading the Region scene for a test and taking out what would get in its way.</summary>
    public static class RegionTestScene
    {
        /// <summary>
        /// Loads the Region and waits for it to be set up, baked and populated (the generated terrain, the Main Base's
        /// claim on its ground). Item drafts are off by default: a draft pauses the game, so a test
        /// that destroys a nest would otherwise stall the tests after it.
        /// </summary>
        public static IEnumerator Load(bool drafts = false) => Load(Default, drafts);

        /// <summary>The seed tests use unless they ask for another, so a test sees the same Region every run.</summary>
        public const int TestSeed = 20261119;

        /// <summary>The Region tests load by default: a fixed seed at the outer ring's usual size.</summary>
        public static RegionRecipe Default => new RegionRecipe(TestSeed);

        /// <summary>Loads the Region made from <paramref name="recipe"/> (its seed and map size); null keeps the Run's own.</summary>
        public static IEnumerator Load(RegionRecipe recipe, bool drafts = false)
        {
            RegionRecipes.Pending = recipe;
            SceneManager.LoadScene(RunSession.RegionScene);
            yield return null;
            if (!drafts) DisableDrafts();
            yield return null;
            yield return TestWait.Until(() => RegionBootstrap.Current != null && RegionBootstrap.Current.IsPopulated, "the Region is populated", 10f);
        }

        public static void DisableDrafts()
        {
            var drafts = Object.FindAnyObjectByType<ItemDrafts>();
            if (drafts != null) drafts.enabled = false;
        }

        /// <summary>Stops the stress spawner and despawns every horde enemy, for a fight of the test's own.</summary>
        public static void ClearHorde()
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            if (spawner != null) spawner.enabled = false;
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude)) Pools.Despawn(m.gameObject);
        }

        /// <summary>
        /// Takes every unit that is not Yours out of the Region without a death (no bones, no Souls, no Vigilance): the
        /// settlement mortals, the wanderers, the wildlife, anything else of another side. Nothing refills them while a test
        /// runs, so a fight or a walk the test stages is not joined by units that wander in from the generated Region.
        /// Buildings stay.
        /// </summary>
        public static void ClearOtherSides()
        {
            foreach (RegionEntity e in new System.Collections.Generic.List<RegionEntity>(RegionEntities.Living))
            {
                if (e.Kind == UnitKind.Building || e.Side == Side.Yours || e.GameObject == null) continue;
                Pools.Despawn(e.GameObject);
            }
        }
    }
}
