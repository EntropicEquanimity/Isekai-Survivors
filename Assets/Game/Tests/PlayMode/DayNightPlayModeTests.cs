using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Day-night behaviour in a real Region: night vision, demon bonus, and the clock wrapping.</summary>
    public class DayNightPlayModeTests
    {
        private RegionGrid _grid;
        private RegionVision _vision;
        private RegionDayNight _dayNight;
        private CharacterControl _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            yield return TestWait.Until(() => RegionDayNight.Current != null, "day-night starts");
            _grid = RegionGrid.Current;
            _vision = RegionVision.Current;
            _dayNight = RegionDayNight.Current;
            _jack = Player.Character;
            _dayNight.ConfigureForTests(DayNightCycle.DefaultCycleSeconds, DayNightCycle.DefaultNightSeconds, 0f);
            _dayNight.CycleSpeedMultiplier = 1f;
            _dayNight.SkipToDay();
            TestDebug.Toggle("God mode", true);
            yield return TestWait.Frames(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RegionVision.RangeMultiplier = 1f;
            if (_dayNight != null)
            {
                _dayNight.ConfigureForTests(DayNightCycle.DefaultCycleSeconds, DayNightCycle.DefaultNightSeconds, DayNightCycle.DefaultTransitionSeconds);
                _dayNight.CycleSpeedMultiplier = 1f;
                _dayNight.SkipToDay();
            }
            Time.timeScale = 1f;
            yield return null;
        }

        private Int2 Around(Int2 from, int distance, int turn)
        {
            float angle = turn * Mathf.PI / 16f;
            return new Int2(from.x + Mathf.RoundToInt(Mathf.Cos(angle) * distance), from.y + Mathf.RoundToInt(Mathf.Sin(angle) * distance));
        }

        private Int2 ADayCellOutsideNightRange(Int2 origin)
        {
            int min = VisionRanges.Scaled(VisionRanges.Character, 0.5f) + 1;
            int max = VisionRanges.Character;
            for (int d = min; d <= max; d++)
                for (int turn = 0; turn < 32; turn++)
                {
                    Int2 cell = Around(origin, d, turn);
                    if (!_grid.Contains(cell) || !_grid.IsWalkable(cell)) continue;
                    if (_vision.CanSee(Side.Yours, cell)) return cell;
                }
            Assert.Fail("no day-visible cell between half and full Character range");
            return default;
        }

        private UnitProducer PlacePortal()
        {
            RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, 200);
            RegionEconomy.Current.Stockpile.Add(ResourceType.Souls, 20);
            Building mainBase = Building.All.First(b => b.IsMainBase);
            BuildingAsset portal = Object.FindAnyObjectByType<BuildMode>().Catalogue.First(a => a.DisplayName == "Portal");
            Building built = RegionBuildings.Current.Place(portal, new Int2(mainBase.Origin.x + 4, mainBase.Origin.y));
            Assert.IsNotNull(built, "a Portal was placed");
            built.FinishConstruction();
            UnitProducer producer = built.GetComponent<UnitProducer>();
            Assert.IsNotNull(producer);
            return producer;
        }

        [UnityTest]
        public IEnumerator Night_halves_what_your_side_can_see()
        {
            Int2 target = ADayCellOutsideNightRange(_grid.CellAt(_jack.transform.position));
            Assert.IsTrue(_vision.CanSee(Side.Yours, target), "visible by day");

            _dayNight.SkipToNight();
            yield return TestWait.Until(() => _dayNight.IsNight, "night starts");
            yield return TestWait.Frames(2);

            Assert.IsFalse(_vision.CanSee(Side.Yours, target), "outside the halved night range");
        }

        [UnityTest]
        public IEnumerator Your_demons_get_the_night_bonus_and_lose_it_by_day()
        {
            UnitProducer producer = PlacePortal();
            yield return TestWait.Until(() => producer.Units.Count > 0, "the Portal brings an Imp", 15f);
            DemonBrain imp = producer.Units[0];

            Damageable jackDamageable = _jack.GetComponent<Damageable>();
            Damageable impDamageable = imp.GetComponent<Damageable>();
            float jackDay = jackDamageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f);
            float impDay = impDamageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f);

            _dayNight.SkipToNight();
            yield return TestWait.Until(() => _dayNight.IsNight, "night starts");
            yield return TestWait.Frames(2);

            float jackNight = jackDamageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f);
            float impNight = impDamageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f);
            Assert.AreEqual(jackDay + _dayNight.NightDamageBonus, jackNight, 1e-3f);
            Assert.AreEqual(impDay + _dayNight.NightDamageBonus, impNight, 1e-3f);

            _dayNight.SkipToDay();
            yield return TestWait.Until(() => !_dayNight.IsNight, "day returns");
            yield return TestWait.Frames(2);

            Assert.AreEqual(jackDay, jackDamageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f), 1e-3f);
            Assert.AreEqual(impDay, impDamageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f), 1e-3f);
        }

        [UnityTest]
        public IEnumerator The_day_night_clock_wraps_to_day_after_cycle_end()
        {
            _dayNight.ConfigureForTests(12f, 4f, 0f);
            _dayNight.SetTimeInCycle(11.7f);
            Assert.IsTrue(_dayNight.IsNight, "late in the cycle it is still night");

            yield return TestWait.Until(() => !_dayNight.IsNight, "clock wraps to day", 3f);
            Assert.Less(_dayNight.TimeInCycle, 2f);
            Assert.Greater(_dayNight.SecondsUntilChange, 6f, "day time remains before the next night");
        }
    }
}
