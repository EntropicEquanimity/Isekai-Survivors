using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>Code-built UI panels for the game's screens, the Blondie way (<see cref="UIPanels"/>).</summary>
    public static class GamePanels
    {
        /// <summary>
        /// The child PanelRenderer named <paramref name="name"/> under <paramref name="owner"/> (made if missing), with
        /// <paramref name="settings"/> (or a fresh PanelSettings at <paramref name="sortingOrder"/>), and its mounted
        /// root, which ignores the pointer except where children take it. Call <see cref="UIPanels.Created"/> after
        /// building into it. The root gets the game's HUD stylesheet (<see cref="GameStyle.HudSheet"/>, light-on-dark
        /// buttons and the text roles) and follows the player's Text size (<see cref="TextSize"/>).
        /// </summary>
        public static VisualElement Mount(Component owner, string name, PanelSettings settings, out PanelRenderer panel, float sortingOrder = 50f)
        {
            panel = UIPanels.FindOrCreateRenderer(owner, name);
            if (settings != null) UIPanels.SetPanelSettings(panel, settings);
            else if (panel.panelSettings == null) UIPanels.SetPanelSettings(panel, UIPanels.CreatePanelSettings(name, null, sortingOrder));
            VisualElement root = UIPanels.Mount(panel);
            root.pickingMode = PickingMode.Ignore;
            StyleSheet sheet = GameStyle.HudSheet;
            if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            root.AddToClassList("isekai-hud");
            TextSize.Register(root);
            return root;
        }
    }
}
