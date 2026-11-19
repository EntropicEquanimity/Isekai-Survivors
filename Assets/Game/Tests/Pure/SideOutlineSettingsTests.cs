using NUnit.Framework;

namespace Isekai.Tests
{
    public class SideOutlineSettingsTests
    {
        [Test]
        public void Default_preset_matches_the_spec_colours()
        {
            var settings = new SideOutlineSettings();
            Assert.AreEqual(new OutlineColor(61, 220, 100), settings.ColorOf(Side.Yours));
            Assert.AreEqual(new OutlineColor(255, 74, 74), settings.ColorOf(Side.Mortals));
            Assert.AreEqual(new OutlineColor(255, 255, 255), settings.ColorOf(Side.Neutral));
            Assert.AreEqual(new OutlineColor(242, 232, 75), settings.ColorOf(Side.Hostile));
            Assert.AreEqual(new OutlineColor(183, 92, 255), settings.ColorOf(Side.Rogue));
        }

        [Test]
        public void Every_named_preset_maps_every_side()
        {
            foreach (OutlinePreset preset in new[]
                     { OutlinePreset.Default, OutlinePreset.Deuteranopia, OutlinePreset.Protanopia, OutlinePreset.Tritanopia, OutlinePreset.HighContrast })
            {
                foreach (Side side in SideRelations.All)
                    Assert.AreNotEqual(default(OutlineColor), SideOutlinePalettes.ColorOf(preset, side), $"{preset} / {side}");
            }
        }

        [Test]
        public void Per_side_override_wins_and_can_be_cleared()
        {
            var settings = new SideOutlineSettings(OutlinePreset.Tritanopia);
            OutlineColor original = settings.ColorOf(Side.Mortals);
            settings.SetOverride(Side.Mortals, new OutlineColor(1, 2, 3));
            Assert.IsTrue(settings.TryGetOverride(Side.Mortals, out OutlineColor overrideColor));
            Assert.AreEqual(new OutlineColor(1, 2, 3), overrideColor);
            Assert.AreEqual(new OutlineColor(1, 2, 3), settings.ColorOf(Side.Mortals));

            Assert.IsTrue(settings.ClearOverride(Side.Mortals));
            Assert.IsFalse(settings.TryGetOverride(Side.Mortals, out _));
            Assert.AreEqual(original, settings.ColorOf(Side.Mortals));
        }

        [Test]
        public void Changing_preset_keeps_overrides_and_updates_non_overridden_sides()
        {
            var settings = new SideOutlineSettings(OutlinePreset.Default);
            settings.SetOverride(Side.Yours, new OutlineColor(9, 9, 9));
            OutlineColor hostileDefault = settings.ColorOf(Side.Hostile);

            settings.SetPreset(OutlinePreset.HighContrast);

            Assert.AreEqual(new OutlineColor(9, 9, 9), settings.ColorOf(Side.Yours), "override survives preset switch");
            Assert.AreNotEqual(hostileDefault, settings.ColorOf(Side.Hostile), "non-overridden side follows the new preset");
        }
    }
}
