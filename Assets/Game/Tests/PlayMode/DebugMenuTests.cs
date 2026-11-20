using System.Collections;
using System.Linq;
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
                         "Spawn trait prev",
                         "Spawn trait next",
                         "Spawn level -",
                         "Spawn level +",
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
                         "Spawn trait prev",
                         "Spawn trait next",
                         "Spawn level -",
                         "Spawn level +",
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

        private static RegionEntity EntityOf(GameObject go) => RegionEntities.Living.First(e => e.GameObject == go);

        [UnityTest]
        public IEnumerator Spawning_a_demon_with_a_chosen_trait_and_level_shows_them_and_its_behaviour_in_the_inspector()
        {
            UnitsDebugSection units = UnitsDebugSection.Current;
            Assert.IsNotNull(units);
            Vector3 at = Player.Character.transform.position + Vector3.right * 3f;
            Assert.IsTrue(units.Pick("Imp", Side.Yours, nameof(TraitId.Berserk), 3));
            GameObject imp = units.SpawnPicked(at);
            Assert.IsNotNull(imp, "an Imp spawns");
            yield return null;

            Assert.AreEqual(Side.Yours, EntityOf(imp).Side);
            Assert.IsTrue(UnitTraits.Of(imp).Has(TraitId.Berserk), "the chosen trait is on it");
            Assert.AreEqual(1, UnitTraits.Of(imp).Traits.Count, "and only that one");
            Assert.AreEqual(3, DemonLoyalty.Of(imp).Level, "the chosen level");

            string text = DebugUnitInspector.Describe(EntityOf(imp));
            StringAssert.Contains("Side: Yours", text);
            StringAssert.Contains("Traits: Berserk", text);
            StringAssert.Contains("Level 3", text);
            StringAssert.Contains("Loyalty", text);
            StringAssert.Contains("Action: " + imp.GetComponent<DemonBrain>().Action, text);
            StringAssert.Contains("Scores:", text);
            StringAssert.DoesNotContain("Scores: n/a", text);
        }

        [UnityTest]
        public IEnumerator Spawning_a_mortal_for_another_side_with_no_traits_and_the_inspector_lists_traits_for_every_unit()
        {
            UnitsDebugSection units = UnitsDebugSection.Current;
            Vector3 at = Player.Character.transform.position + Vector3.left * 3f;
            Assert.IsTrue(units.Pick("Soldier", Side.Hostile, "none", 5));
            GameObject soldier = units.SpawnPicked(at);
            Assert.IsNotNull(soldier);
            yield return null;
            Assert.AreEqual(Side.Hostile, EntityOf(soldier).Side);
            Assert.IsNull(UnitTraits.Of(soldier), "none means none, whatever the roll");
            StringAssert.Contains("Traits: none", DebugUnitInspector.Describe(EntityOf(soldier)));

            Assert.IsTrue(units.Pick("Soldier", Side.Mortals, nameof(TraitId.Cowardly), 1));
            GameObject coward = units.SpawnPicked(at + Vector3.up * 2f);
            yield return null;
            StringAssert.Contains("Traits: Cowardly", DebugUnitInspector.Describe(EntityOf(coward)));
            yield return null;
            Assert.IsTrue(UnitTraits.Of(coward).Has(TraitId.Cowardly), "a pending roll does not replace it");

            string jack = DebugUnitInspector.Describe(EntityOf(Player.Character.gameObject));
            StringAssert.Contains("Traits:", jack);
            StringAssert.DoesNotContain("Action: n/a", jack);
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
