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
        /// Loads the Region and waits for its setup. Item drafts are off by default: a draft pauses the game, so a test
        /// that destroys a nest would otherwise stall the tests after it.
        /// </summary>
        public static IEnumerator Load(bool drafts = false) => Load(null, drafts);

        /// <summary>Loads the Region made from <paramref name="recipe"/> (its seed and map size); null keeps the Run's own.</summary>
        public static IEnumerator Load(RegionRecipe recipe, bool drafts = false)
        {
            RegionRecipes.Pending = recipe;
            SceneManager.LoadScene(RunSession.RegionScene);
            yield return null;
            if (!drafts) DisableDrafts();
            yield return null;
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
    }
}
