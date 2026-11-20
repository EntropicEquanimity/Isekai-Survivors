using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Vigilance and its Waves in the real Region scene.</summary>
    public class VigilancePlayModeTests
    {
        private VigilanceDirector _director;
        private CombatUnit _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return TestWait.Frames(2);
            _director = VigilanceDirector.Current;
            Assert.IsNotNull(_director, "the Vigilance director");
            _jack = Player.Character.GetComponent<Damageable>().Unit;
        }

        private static HordeMember SpawnMortal(Vector3 at) =>
            Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, at, Quaternion.identity).GetComponent<HordeMember>();

        private static bool InEdgeBand(Vector3 at, int band)
        {
            Blondie.Core.Int2 cell = RegionGrid.Current.CellAt(at);
            int fromEdge = Mathf.Min(Mathf.Min(cell.x, RegionGrid.Current.Width - 1 - cell.x), Mathf.Min(cell.y, RegionGrid.Current.Height - 1 - cell.y));
            return fromEdge < band;
        }

        // Waves from earlier in a test are gone before the next is judged.
        private IEnumerator ClearWaves()
        {
            RegionTestScene.ClearHorde();
            yield return TestWait.Until(() => _director.AliveWaves == 0, "Waves end once their mortals are gone", 5f);
        }

        [UnityTest]
        public IEnumerator Killing_a_mortal_raises_vigilance()
        {
            float before = _director.Points;
            HordeMember mortal = SpawnMortal(Player.Character.transform.position + new Vector3(7f, 0f, 0f));
            yield return null;
            mortal.Unit.TakeDamage(99999f, DamageRules.PhysicalType, _jack);
            yield return null;

            Assert.Greater(_director.Points, before);
        }

        [UnityTest]
        public IEnumerator A_mortal_dying_to_something_else_does_not_raise_vigilance()
        {
            HordeMember mortal = SpawnMortal(Player.Character.transform.position + new Vector3(7f, 0f, 0f));
            yield return null;
            mortal.Unit.TakeDamage(99999f, DamageRules.PhysicalType);
            yield return null;

            Assert.AreEqual(0f, _director.Points);
        }

        [UnityTest]
        public IEnumerator Vigilance_drives_the_Regions_threat()
        {
            RegionContext context = RegionBootstrap.Current.Context;
            Assert.AreEqual(1f, context.HealthMultiplier, "unaware: no escalation");

            _director.RaiseStage();
            _director.RaiseStage();
            yield return null;

            Assert.AreEqual(VigilanceStage.Alarmed, _director.Stage);
            Assert.AreEqual(VigilanceTuning.HealthMultiplier(VigilanceStage.Alarmed), context.HealthMultiplier, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Reaching_a_new_stage_sends_a_Wave_from_the_edge_that_fights()
        {
            TestDebug.Press("Raise Vigilance");
            yield return TestWait.Until(() => _director.WavesSent == 1, "a Wave for the new stage", 4f);

            VigilanceDirector.Wave wave = _director.LastWave;
            Assert.IsFalse(wave.FromCircle);
            Assert.AreEqual(1, _director.WavesFromEdge);
            Assert.IsTrue(InEdgeBand(wave.Origin, VigilanceTuning.EdgeBandCells), "it arrives from the map edge, not out of nowhere");
            Assert.AreEqual(VigilanceTuning.WaveSize(VigilanceStage.Uneasy), wave.Members.Count);

            var members = wave.Members.ToArray();
            Vector3[] start = members.Select(m => m.transform.position).ToArray();
            Assert.IsTrue(members.All(m => m.IsAlive && !m.IsRoaming), "they attack at once, they do not roam");
            yield return new WaitForSeconds(2f);
            float moved = 0f;
            for (int i = 0; i < members.Length; i++) moved += Vector3.Distance(start[i], members[i].transform.position);
            Assert.Greater(moved / members.Length, 1f, "they walk toward the base");
        }

        [UnityTest]
        public IEnumerator A_Wave_is_for_each_new_stage_and_at_most_two_are_alive()
        {
            for (int i = 0; i < 3; i++) _director.RaiseStage();
            yield return TestWait.Until(() => _director.WavesSent == 2, "two Waves go at once", 4f);
            yield return TestWait.Frames(10);

            Assert.AreEqual(2, _director.AliveWaves, "the cap");
            Assert.AreEqual(1, _director.PendingWaves, "the third waits");

            RegionTestScene.ClearHorde();
            yield return TestWait.Until(() => _director.WavesSent == 3, "the third goes once there is room", 5f);
        }

        [UnityTest]
        public IEnumerator Waves_alternate_between_the_edge_and_a_standing_spawn_circle()
        {
            Assert.AreEqual(1, _director.StandingCircles, "a spawn circle stands");
            VigilanceSpawnCircle circle = _director.Circles.First();
            Assert.IsTrue(InEdgeBand(circle.transform.position, VigilanceTuning.EdgeBandCells + 6));

            TestDebug.Press("Send next Wave");
            yield return TestWait.Until(() => _director.WavesSent == 1, "the first Wave", 4f);
            Assert.IsFalse(_director.LastWave.FromCircle);
            yield return ClearWaves();

            TestDebug.Press("Send next Wave");
            yield return TestWait.Until(() => _director.WavesSent == 2, "the second Wave", 4f);
            Assert.IsTrue(_director.LastWave.FromCircle);
            Assert.Less(Vector3.Distance(_director.LastWave.Origin, circle.transform.position), 0.01f);
            Assert.IsTrue(_director.LastWave.Members.All(m => Vector3.Distance(m.transform.position, circle.transform.position) < 6f), "they come out round the circle");
        }

        [UnityTest]
        public IEnumerator Destroying_a_spawn_circle_stops_it_sending_Waves()
        {
            VigilanceSpawnCircle circle = _director.Circles.First();
            float points = _director.Points;
            circle.Damageable.Unit.TakeDamage(99999f, DamageRules.PhysicalType, _jack);
            yield return null;

            Assert.AreEqual(0, _director.StandingCircles);
            Assert.IsFalse(circle != null && circle.IsAlive);
            Assert.AreEqual(points, _director.Points, "a circle is not a mortal");

            for (int i = 0; i < 4; i++)
            {
                TestDebug.Press("Send next Wave");
                yield return TestWait.Until(() => _director.WavesSent == i + 1, "a Wave", 4f);
                yield return ClearWaves();
            }
            Assert.AreEqual(0, _director.WavesFromCircle, "every Wave comes from the edge now");
            Assert.AreEqual(4, _director.WavesFromEdge);
        }

        [UnityTest]
        public IEnumerator The_HUD_shows_the_stage_and_progress()
        {
            var hud = Object.FindAnyObjectByType<VigilanceHud>();
            Assert.IsNotNull(hud, "the Vigilance HUD");
            yield return TestWait.Until(() => hud.StageText == "Unaware", "the HUD shows the starting stage", 3f);

            TestDebug.Press("Raise Vigilance");
            yield return TestWait.Until(() => hud.StageText == "Uneasy", "the HUD shows the new stage", 3f);
            StringAssert.StartsWith($"{VigilanceTuning.Threshold(VigilanceStage.Uneasy)}", hud.NextText);
            Assert.AreEqual(0f, hud.Fill, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Debug_rows_raise_and_lower_vigilance_and_send_Waves()
        {
            foreach (string label in new[] { "Raise Vigilance", "Lower Vigilance", "Send next Wave" })
                Assert.IsNotNull(DebugActions.Find(label), label);

            TestDebug.Press("Raise Vigilance");
            TestDebug.Press("Raise Vigilance");
            Assert.AreEqual(VigilanceStage.Alarmed, _director.Stage);
            TestDebug.Press("Lower Vigilance");
            Assert.AreEqual(VigilanceStage.Uneasy, _director.Stage);
            yield return null;
        }
    }
}
