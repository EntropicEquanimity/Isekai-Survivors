using System.Collections;
using Blondie.FlowFields;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Guards against the costs found while profiling: a chased player must not allocate a new map-wide path field per step.</summary>
    public class PerformanceTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region is ready");
        }

        [UnityTest]
        public IEnumerator Walking_reuses_the_hordes_path_fields()
        {
            CharacterControl body = Player.Character;
            Vector3 start = body.transform.position;
            yield return new WaitForSeconds(0.5f);
            int cached = FlowFields.CachedCount;
            int recycled = FlowFields.RecycledCount;

            // Walk 12 cells, one cell every 0.25 s: each step is a new target set for the horde.
            for (int i = 1; i <= 12; i++)
            {
                body.transform.position = start + new Vector3(i * RegionGrid.Current.CellSize, 0f, 0f);
                yield return new WaitForSeconds(0.25f);
            }

            Assert.Greater(FlowFields.RecycledCount, recycled, "new target sets reuse idle fields");
            Assert.LessOrEqual(FlowFields.CachedCount, cached + 2, "no pile of map-sized fields");
        }
    }
}
