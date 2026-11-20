using System.Collections;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Workers in the generated Region: summoning and carrying Stone back.</summary>
    public class WorkerPlayModeTests
    {
        private Building _anchor;
        private RegionCorruption _corruption;
        private Stockpile _stockpile;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return null;
            _anchor = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            _corruption = RegionCorruption.Current;
            _stockpile = RegionEconomy.Current.Stockpile;
            EconomyDebug.AddResources(1000);
            Time.timeScale = 4f;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_summoned_worker_delivers_Stone_to_the_Anchor()
        {
            ResourceNode stone = NearestStoneTo(_anchor.transform.position);
            Assert.IsNotNull(stone, "a Stone node exists in the generated Region");
            Int2 stoneCell = RegionGrid.Current.CellAt(stone.transform.position);

            // Make this the only corrupted node so workers must pick this Stone first.
            foreach (ResourceNode node in ResourceNode.All)
            {
                if (node == null || node == stone || node.IsEmpty) continue;
                _corruption.Field.Set(RegionGrid.Current.CellAt(node.transform.position), 0);
            }
            _corruption.Field.Set(stoneCell, CorruptionTuning.MaxLevel);

            int before = _stockpile.Get(ResourceType.Stone);
            RegionWorkers workers = Object.FindAnyObjectByType<RegionWorkers>();
            Assert.IsNotNull(workers, "worker system exists");
            Assert.IsTrue(workers.TrySummonWorker(), "a worker can be summoned from the Anchor panel");

            yield return TestWait.Until(() => Object.FindAnyObjectByType<WorkerBrain>() != null, "a worker appears", 5f);
            yield return TestWait.Until(() => _stockpile.Get(ResourceType.Stone) > before, "the worker delivers Stone", 90f);
        }

        private static ResourceNode NearestStoneTo(Vector3 world)
        {
            ResourceNode best = null;
            float bestDistance = float.MaxValue;
            foreach (ResourceNode node in ResourceNode.All)
            {
                if (node == null || node.IsEmpty || node.Type != ResourceType.Stone) continue;
                float d = (node.transform.position - world).sqrMagnitude;
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = node;
            }
            return best;
        }
    }
}
