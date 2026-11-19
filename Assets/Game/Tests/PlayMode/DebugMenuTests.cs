using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Coverage for the complete debug menu sections added in ticket #31.</summary>
    public class DebugMenuTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region fills", 10f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Every_section_registers_rows_and_safe_rows_run_without_errors()
        {
            foreach (string label in new[]
                     {
                         "Run seed -1",
                         "Run seed +1",
                         "Start new Run (seed + Anchor)",
                         "Jump to selected Region",
                         "Capture this Region",
                         "Win the Run now",
                         "Lose the Run now",
                         "Corruption brush",
                         "Clear corruption under mouse",
                         "Unit type prev",
                         "Unit type next",
                         "Spawn side prev",
                         "Spawn side next",
                         "Spawn unit at mouse",
                         "Select hovered unit",
                         "Kill selection",
                         "Heal selection",
                         "Selected demon -> Rogue",
                         "Possess selected demon",
                         "Perf overlay",
                     })
                Assert.IsNotNull(DebugActions.Find(label), label);

            // Existing rows reused by the new sections (no duplicates).
            foreach (string label in new[] { "Fog of war", "Skip to day", "Skip to night", "Raise Vigilance", "Lower Vigilance", "Send next Wave", "God mode", "Kill Jack's body" })
                Assert.IsNotNull(DebugActions.Find(label), label);

            foreach (string label in new[]
                     {
                         "Run seed -1",
                         "Run seed +1",
                         "Run seed: random",
                         "Run anchor: Shrine",
                         "Run anchor: War Throne",
                         "Run region prev",
                         "Run region next",
                         "Brush level -",
                         "Brush level +",
                         "Clear corruption under mouse",
                         "Unit type prev",
                         "Unit type next",
                         "Spawn side prev",
                         "Spawn side next",
                         "Spawn unit at mouse",
                         "Select hovered unit",
                         "Clear unit selection",
                         "Kill selection",
                         "Heal selection",
                         "Selected demon -> Rogue",
                         "Possess selected demon",
                     })
                TestDebug.Press(label);

            TestDebug.Toggle("Corruption brush", true);
            TestDebug.Toggle("Corruption brush", false);
            TestDebug.Toggle("Perf overlay", true);
            TestDebug.Toggle("Perf overlay", false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Inspector_shows_selected_mortal_side_and_action()
        {
            RegionSettlements.Settlement settlement = RegionSettlements.Current.Settlements[0];
            Vector3 at = settlement.Markers.Count > 0 ? settlement.Markers[0].Position : Player.Character.transform.position + Vector3.right * 2f;
            MortalBrain brain = RegionMortals.Current.Spawn(MortalType.Soldier, settlement, at);
            Assert.IsNotNull(brain);
            yield return null;

            Assert.IsTrue(DebugUnitSelection.SelectAt(brain.transform.position, 2.5f), "the spawned mortal can be selected");
            yield return null;

            string summary = DebugUnitInspector.CurrentSummary;
            StringAssert.Contains("side Mortals", summary);
            StringAssert.Contains("action", summary);
            StringAssert.Contains(brain.Action.ToString(), summary);
        }
    }
}
