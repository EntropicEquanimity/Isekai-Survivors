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
        private IKeyValueStore _store;

        // The developer's own choices (the controls card left open, a rebound key) must not decide these tests.
        [UnitySetUp]
        public IEnumerator OwnSettings()
        {
            _store = KeyValueStorage.Current;
            KeyValueStorage.Current = new MemoryKeyValueStore();
            GameInput.ResetBindings();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            TextSize.Index = TextSize.Medium;
            GameInput.ResetBindings();
            KeyValueStorage.Current = _store;
            GameInput.LoadBindings();
            foreach (PanelRenderer p in Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Exclude))
                if (p.panelSettings != null && p.panelSettings.targetTexture != null) p.panelSettings.targetTexture = null;
            foreach (RenderTexture t in _screens) t.Release();
            _screens.Clear();
            Time.timeScale = 1f;
            RunSession.Clear();
            yield return null;
        }

        // Every panel laid out as on a 1920x1080 screen, whatever size the test runner's is.
        private IEnumerator FullHd() => Screen(1920, 1080);

        private IEnumerator Screen(int width, int height)
        {
            var seen = new HashSet<PanelSettings>();
            foreach (PanelRenderer p in Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Exclude))
            {
                if (p.panelSettings == null || !seen.Add(p.panelSettings)) continue;
                var screen = new RenderTexture(width, height, 24);
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
        public IEnumerator On_a_small_window_the_loadout_scrolls_and_Go_stays_on_screen()
        {
            RunSession.StartNew(5);
            RunSession.LoadoutPending = true;
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet screen shows", 20f);
            foreach ((int w, int h) in new[] { (1280, 720), (1600, 900), (1920, 1080) })
            foreach (int size in new[] { 1, 2 })
            {
                yield return Screen(w, h);
                yield return Size(size);
                string at = $"{w}x{h} text size {size}";
                IconButton go = Panels().SelectMany(p => p.Query<IconButton>("Confirm loadout").ToList()).Single();
                Assert.GreaterOrEqual(go.worldBound.yMin, 0f, at);
                Assert.LessOrEqual(go.worldBound.yMax, h + 0.5f, at + ": Go is on screen");
                Assert.GreaterOrEqual(go.worldBound.height, 48f, at + ": Go keeps its size");
                ScrollView scroll = Panels().SelectMany(p => p.Query<ScrollView>("loadout-scroll").ToList()).Single();
                List<IconButton> tiles = scroll.Query<IconButton>(className: "isekai-tile").ToList();
                for (int i = 0; i < tiles.Count; i++)
                {
                    Assert.GreaterOrEqual(tiles[i].worldBound.height, 96f, $"{at}: {tiles[i].Name} is not squashed");
                    Assert.IsTrue(tiles[i].worldBound.xMin >= -0.5f && tiles[i].worldBound.xMax <= w + 0.5f, $"{at}: {tiles[i].Name} is inside the window");
                    for (int j = i + 1; j < tiles.Count; j++) Assert.IsFalse(tiles[i].worldBound.Overlaps(tiles[j].worldBound), $"{at}: {tiles[i].Name} and {tiles[j].Name}");
                }
                scroll.ScrollTo(tiles[^1]);
                yield return null;
                yield return null;
                Assert.IsTrue(scroll.worldBound.yMax + 1f >= tiles[^1].worldBound.yMax, at + ": the last tile can be reached");
            }
        }

        [UnityTest]
        public IEnumerator Planet_map_names_never_overlap_or_leave_the_screen_and_every_Region_can_be_clicked()
        {
            RunSession.StartNew(5);
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet screen shows", 20f);
            PlanetMapView map = Object.FindAnyObjectByType<PlanetScreen>().Map;
            foreach ((int w, int h) in new[] { (1280, 720), (1600, 900), (1920, 1080) })
            foreach (int size in new[] { 1, 2 })
            {
                yield return Screen(w, h);
                yield return Size(size);
                for (int i = 0; i < 6; i++) yield return null;
                string at = $"{w}x{h} text size {size}";
                List<Label> names = map.Query<Label>(className: "isekai-role-body").ToList().Where(l => Shown(l) && l.resolvedStyle.visibility == Visibility.Visible).ToList();
                Assert.GreaterOrEqual(names.Count, 6, at + ": at least the Regions you can land on are named");
                for (int i = 0; i < names.Count; i++)
                {
                    Assert.IsTrue(names[i].worldBound.xMin >= 0f && names[i].worldBound.xMax <= w && names[i].worldBound.yMax <= h, $"{at}: {names[i].text} is on screen");
                    for (int j = i + 1; j < names.Count; j++) Assert.IsFalse(names[i].worldBound.Overlaps(names[j].worldBound), $"{at}: {names[i].text} and {names[j].text}");
                }
                foreach (RegionInfo open in RunSession.Run.Available())
                    Assert.IsTrue(names.Any(n => n.text == open.Name), $"{at}: {open.Name} can be entered, so it is named");
            }
        }

        [UnityTest]
        public IEnumerator The_mining_prompt_takes_no_clicks_and_a_popups_icon_stays_in_its_number()
        {
            yield return InRegion();
            VisualElement prompt = HudStack.Column(HudCorner.BottomCentre).Q("mining-prompt");
            Assert.AreEqual(PickingMode.Ignore, prompt.pickingMode);
            Assert.IsTrue(prompt.Query<VisualElement>().ToList().All(e => e.pickingMode == PickingMode.Ignore), "nor does anything in it");

            Label number = ResourcePopup.Spawn(Player.Character.transform.position, ResourceType.Stone, 3);
            Assert.IsNotNull(number);
            Assert.AreEqual("+3", number.text);
            Assert.IsNotNull(number.Q(className: "isekai-icon"), "the icon is inside the number, so it can't drift from it");
        }

        [UnityTest]
        public IEnumerator The_Region_map_is_drawn_at_a_whole_scale()
        {
            yield return InRegion();
            var map = Object.FindAnyObjectByType<MapScreen>();
            map.OpenRegion();
            for (int i = 0; i < 6; i++) yield return null;
            float scale = map.RegionMap.contentRect.width / map.RegionMap.Texture.width;
            Assert.GreaterOrEqual(scale, 1f);
            Assert.AreEqual(Mathf.Round(scale), scale, 0.0001f);
            Assert.AreEqual(scale, map.RegionMap.contentRect.height / map.RegionMap.Texture.height, 0.0001f);
            map.Close();
        }

        [UnityTest]
        public IEnumerator The_soul_HUD_and_the_key_name_follow_a_rebind()
        {
            yield return InRegion();
            Assert.AreEqual("R", BodyKey.Name);
            UnityEngine.InputSystem.InputAction body = GameInput.Find(BodyKey.ActionName);
            UnityEngine.InputSystem.InputActionRebindingExtensions.ApplyBindingOverride(body, 0, "<Keyboard>/f");
            EventCenter.Publish<BindingsChanged>();
            Assert.AreEqual("F", BodyKey.Name);
            RegionPossession.Current.Act();
            yield return null;
            yield return null;
            StringAssert.Contains("F over a demon", Object.FindAnyObjectByType<SoulsHud>().StatusText);
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
