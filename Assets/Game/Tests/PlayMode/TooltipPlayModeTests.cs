using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Ticket #29 checks for HUD and world tooltip content through the shared providers.</summary>
    public class TooltipPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Frames(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            TraitSystem.AutoRoll = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Build_button_tooltip_lists_cost_and_reason()
        {
            BuildMode mode = Object.FindAnyObjectByType<BuildMode>();
            Assert.IsNotNull(mode, "Build mode");
            BuildingAsset portal = mode.Catalogue.First(a => a.DisplayName == "Portal");
            string body = TooltipProvider.BuildButtonBody(portal, canAfford: false, placementReason: CorruptedGroundRule.Reason);
            StringAssert.Contains(portal.PriceText, body);
            StringAssert.Contains(CorruptedGroundRule.Reason, body);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Unit_tooltip_lists_side_and_traits()
        {
            RegionEntity mortal = RegionEntities.Living
                .FirstOrDefault(e => e.Kind == UnitKind.Mortal && e.Side == Side.Mortals && e.GameObject.GetComponent<MortalBrain>() != null);
            Assert.IsNotNull(mortal, "a mortal with a brain");

            TraitSystem.AutoRoll = false;
            Assert.IsTrue(TraitSystem.Current.Give(mortal.GameObject, TraitId.Vigilant), "Trait applied");
            yield return null;

            Assert.IsTrue(TooltipProvider.TryWorldTooltip(mortal.GameObject.transform.position, 0.75f, out TooltipContent tip));
            StringAssert.Contains("Side: Mortals", tip.Body);
            StringAssert.Contains("Vigilant", tip.Body);
            StringAssert.Contains(TraitTable.Get(TraitId.Vigilant).Description, tip.Body);
        }

        [Test]
        public void Tooltip_text_table_has_an_entry_for_every_key()
        {
            foreach (TooltipKey key in Enum.GetValues(typeof(TooltipKey)))
                Assert.IsTrue(TooltipText.Has(key), key.ToString());
        }
    }
}
