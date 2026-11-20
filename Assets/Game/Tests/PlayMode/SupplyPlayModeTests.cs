using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Supply and troop raising in the real Region scene.</summary>
    public class SupplyPlayModeTests
    {
        private RegionSupply _supply;
        private RegionTroops _troops;
        private VigilanceDirector _director;
        private RegionSettlements _settlements;
        private RegionMortals _mortals;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Frames(2);
            _supply = RegionSupply.Current;
            _troops = RegionTroops.Current;
            _director = VigilanceDirector.Current;
            _settlements = RegionSettlements.Current;
            _mortals = RegionMortals.Current;
            Assert.IsNotNull(_supply, "the Supply system");
            Assert.IsNotNull(_troops, "the troop-raising system");
            Assert.IsNotNull(_director);
            Assert.IsNotNull(_settlements);
            Assert.IsNotNull(_mortals);
            _supply.Speed = 120f;
        }

        [UnityTest]
        public IEnumerator An_undetected_Region_raises_no_troops_though_it_is_rich()
        {
            Assert.AreEqual(VigilanceStage.Unaware, _director.Stage);
            _supply.Pool.Add(1000f);
            yield return new WaitForSeconds(1.5f);

            Assert.AreEqual(0, _troops.TroopsRaised);
            Assert.AreEqual(0, _troops.TroopsAt(_settlements.Seat));
        }

        [UnityTest]
        public IEnumerator Once_alerted_the_Seat_raises_troops_over_time()
        {
            _supply.Pool.Add(1000f);
            _director.StartAt(VigilanceStage.Uneasy);
            int before = _mortals.Brains.Count;

            yield return TestWait.Until(() => _troops.TroopsAt(_settlements.Seat) >= 2, "the Seat raises troops", 15f);

            Assert.GreaterOrEqual(_mortals.Brains.Count, before + 2, "the troops are mortals in the Region");
        }

        [UnityTest]
        public IEnumerator Razing_the_farms_lowers_Supply_income()
        {
            Assume.That(_settlements.Settlements.Any(s => s.Kind == SettlementKind.Farm), "the Region has farms");
            float before = _supply.PerMinute;

            Assert.Greater(SupplyDebug.RazeFarms(), 0);
            yield return TestWait.Until(() => _supply.PerMinute < before, "income drops once the farms are razed", 5f);
        }

        [UnityTest]
        public IEnumerator Patrols_grow_with_the_stage()
        {
            _director.StartAt(VigilanceStage.Uneasy);
            yield return TestWait.Until(() => _troops.PatrolsAdded > 0, "patrols are added", 5f);
            int uneasy = _troops.PatrolsAdded;
            float strength = _mortals.PatrolStrength;

            _director.StartAt(VigilanceStage.Mobilized);
            yield return TestWait.Until(() => _troops.PatrolsAdded > uneasy, "more patrols at a higher stage", 5f);

            Assert.Greater(_mortals.PatrolStrength, strength, "and they are tougher");
        }
    }
}
