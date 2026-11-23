using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using Blondie.Input;
using Blondie.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The rules in docs/ui-rules.md that a machine can check, on the real screens (laid out at 1920x1080).</summary>
    public class UiRulesPlayModeTests
    {
        private readonly List<RenderTexture> _screens = new();

        [UnityTearDown]
        public IEnumerator Restore()
        {
            TextSize.Index = TextSize.Medium;
            foreach (PanelRenderer p in Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Exclude))
                if (p.panelSettings != null && p.panelSettings.targetTexture != null) p.panelSettings.targetTexture = null;
            foreach (RenderTexture t in _screens) t.Release();
            _screens.Clear();
            Time.timeScale = 1f;
            RunSession.Clear();
            yield return null;
        }

        // Every panel laid out as on a 1920x1080 screen, whatever size the test runner's is.
        private IEnumerator FullHd()
        {
            foreach (PanelRenderer p in Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Exclude))
            {
                if (p.panelSettings == null || p.panelSettings.targetTexture != null) continue;
                var screen = new RenderTexture(1920, 1080, 24);
                _screens.Add(screen);
                p.panelSettings.targetTexture = screen;
            }
            for (int i = 0; i < 6; i++) yield return null;
        }

        private static IEnumerator Size(int index)
        {
            TextSize.Index = index;
            for (int i = 0; i < 6; i++) yield return null;
        }

        private static bool Shown(VisualElement e)
        {
            for (VisualElement v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
            return e.worldBound.width > 1f && e.worldBound.height > 1f;
        }

        private static IEnumerable<VisualElement> Panels() =>
            Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Exclude).Select(p => UIPanels.RootOf(p)).Where(r => r != null)
                .SelectMany(r => r.Query(className: "isekai-hud").ToList());

        private static IEnumerable<TextElement> Texts() =>
            Panels().SelectMany(p => p.Query<TextElement>().ToList()).Where(t => !string.IsNullOrWhiteSpace(t.text) && Shown(t));

        private IEnumerator InRegion()
        {
            RunSession.StartNew(5);
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated && Object.FindAnyObjectByType<SoulsHud>() != null, "the Region fills", 30f);
            for (int i = 0; i < 20; i++) yield return null;
            yield return FullHd();
        }

        [UnityTest]
        public IEnumerator HUD_boxes_never_overlap_at_any_text_size()
        {
            yield return InRegion();
            foreach (int size in new[] { 1, 2, 3 })
            {
                yield return Size(size);
                var boxes = new List<(string name, Rect rect)>();
                foreach (HudCorner corner in System.Enum.GetValues(typeof(HudCorner)))
                    foreach (VisualElement box in HudStack.Column(corner).Children())
                        if (Shown(box)) boxes.Add((corner + " " + box.childCount, box.worldBound));
                Assert.Greater(boxes.Count, 5, "the HUD is up");
                for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                    Assert.IsFalse(boxes[i].rect.Overlaps(boxes[j].rect), $"text size {size}: {boxes[i]} and {boxes[j]}");
            }
        }

        [UnityTest]
        public IEnumerator HUD_resources_are_icons_and_text_is_pixel_sized_light_and_never_bold()
        {
            yield return InRegion();
            foreach (int size in new[] { 1, 2, 3 })
            {
                yield return Size(size);
                foreach (TextElement t in Texts())
                {
                    StringAssert.DoesNotMatch("Wood|Stone|Souls", t.text, "a Resource is an icon, not its name");
                    float px = t.resolvedStyle.fontSize;
                    Assert.IsTrue(px is 12f or 24f or 36f or 48f, $"'{t.text}' is {px} px");
                    Assert.AreEqual(FontStyle.Normal, t.resolvedStyle.unityFontStyleAndWeight, $"'{t.text}' is not bold");
                    Assert.Greater(t.resolvedStyle.color.grayscale, 0.35f, $"'{t.text}' is light on the dark panels");
                }
            }
            Assert.IsTrue(Texts().All(t => t.resolvedStyle.fontSize >= 36f), "Text size reaches every HUD text");
        }

        [UnityTest]
        public IEnumerator The_loadout_fills_the_screen_and_its_names_stay_in_their_tiles_at_every_text_size()
        {
            RunSession.StartNew(5);
            RunSession.LoadoutPending = true;
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet screen shows", 20f);
            var screen = Object.FindAnyObjectByType<PlanetScreen>();
            yield return FullHd();
            foreach (int size in new[] { 1, 2, 3 })
            {
                yield return Size(size);
                List<IconButton> tiles = Panels().SelectMany(p => p.Query<IconButton>(className: "isekai-tile").ToList()).Where(Shown).ToList();
                Assert.AreEqual(10, tiles.Count, "two Anchors and eight Soul paths");
                foreach (IconButton tile in tiles)
                {
                    Assert.IsTrue(tile.HasIcon, tile.Name + " has an icon");
                    foreach (Label label in tile.Query<Label>().ToList().Where(l => Shown(l) && l.text.Length > 0))
                    {
                        Vector2 want = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                        Assert.LessOrEqual(want.x, tile.contentRect.width + 1f, $"text size {size}: '{label.text}' fits its tile");
                    }
                    Assert.IsTrue(new Rect(0f, 0f, 1920f, 1080f).Contains(tile.worldBound.min) && new Rect(0f, 0f, 1921f, 1081f).Contains(tile.worldBound.max), $"text size {size}: {tile.Name} is on screen");
                }
            }
            yield return Size(1);
            VisualElement first = Panels().SelectMany(p => p.Query<IconButton>(className: "isekai-tile").ToList()).First();
            screen.TogglePath(SoulPath.Reaper);
            Assert.AreSame(first, Panels().SelectMany(p => p.Query<IconButton>(className: "isekai-tile").ToList()).First(), "a click rebuilds nothing");
        }

        [UnityTest]
        public IEnumerator The_map_tabs_and_the_Portal_options_change_in_place()
        {
            yield return InRegion();
            var map = Object.FindAnyObjectByType<MapScreen>();
            map.OpenRegion();
            RegionMapView region = map.RegionMap;
            map.Show(MapScreen.Tab.Planet);
            map.Show(MapScreen.Tab.Region);
            Assert.AreSame(region, map.RegionMap);
            Assert.IsNotNull(region.parent, "the Region page stays built behind the Planet tab");
            map.Close();

            var panel = new PortalPanel();
            UnitProducer portal = new GameObject("Portal").AddComponent<UnitProducer>();
            panel.Show(portal);
            IconButton guard = panel.Q<IconButton>("order-Guard"), raid = panel.Q<IconButton>("order-Raid");
            Assert.IsTrue(guard.Picked, "the order in force is framed");
            Assert.IsTrue(guard.enabledSelf, "not greyed out");
            portal.Order = PortalOrder.Raid;
            panel.Refresh();
            Assert.IsFalse(guard.Picked);
            Assert.IsTrue(raid.Picked);
            Assert.AreSame(raid, panel.Q<IconButton>("order-Raid"));
            Object.Destroy(portal.gameObject);
        }

        [UnityTest]
        public IEnumerator Nothing_of_the_HUD_is_above_the_end_screen()
        {
            yield return InRegion();
            RunEndScreen.Current.Show(new RunSummary(false, 2, 14, 900f, 3, 40, 2, 1, 1));
            yield return null;
            float end = OrderOf(RunEndScreen.Current), hud = OrderOf(Object.FindAnyObjectByType<HudStack>());
            Assert.Greater(end, hud);
            foreach (IconButton b in Panels().SelectMany(p => p.Query<IconButton>().ToList()).Where(b => b.Name is "New Run" or "Main menu")) Assert.IsTrue(b.HasIcon, b.Name);
            Object.FindAnyObjectByType<RunEndScreen>().enabled = true;
        }

        private static float OrderOf(Component owner) => owner.GetComponentsInChildren<PanelRenderer>(true)
            .Where(p => p.panelSettings != null && (owner is not RunEndScreen || p.name == "Run End Screen") && (owner is not HudStack || p.name == "HUD"))
            .Max(p => p.panelSettings.sortingOrder);

        [UnityTest]
        public IEnumerator Your_wisp_and_bars_ignore_the_night_and_the_outline_materials_ship_with_the_game()
        {
            foreach (Side side in SideRelations.All) Assert.IsNotNull(Resources.Load<Material>("Outlines/Outline_" + side), $"{side}'s outline is in Resources, so a build has it");
            Assert.IsNotNull(SideOutlineMaterials.Unlit);
            yield return InRegion();
            SpriteRenderer[] bars = Player.Character.GetComponentsInChildren<SpriteRenderer>(true).Where(HealthBar.IsPart).ToArray();
            Assert.IsNotEmpty(bars, "Jack has a health bar");
            foreach (SpriteRenderer bar in bars) Assert.AreSame(SideOutlineMaterials.Unlit, bar.sharedMaterial, "the bar is readable at night");
            Assert.IsTrue(RegionPossession.Current.Act(), "R leaves the body");
            yield return null;
            Assert.AreSame(SideOutlineMaterials.MaterialFor(Side.Yours), RegionSouls.Current.Wisp.GetComponent<SpriteRenderer>().sharedMaterial, "the wisp too");
        }

        [Test]
        public void The_body_key_is_R_by_default_rebindable_and_nothing_else_uses_R()
        {
            CollectionAssert.Contains(InputBindings.ButtonActions, BodyKey.ActionName, "it is listed under Key bindings");
            Assert.AreEqual("R", BodyKey.Name);
            foreach (UnityEngine.InputSystem.InputAction action in GameInput.Player.actions)
                if (action.name != BodyKey.ActionName)
                    Assert.IsFalse(action.bindings.Any(b => b.effectivePath == "<Keyboard>/r"), action.name + " is not on R");
            Assert.IsFalse(GameSettingsRows.GameKeys.Any(k => k.Keys == "P"), "P possesses nothing any more");
        }
    }
}
