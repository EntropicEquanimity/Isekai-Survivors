using Blondie.Core;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Raises the interface cues for every UI Toolkit panel Blondie announces (<see cref="UIPanels"/>: the HUDs, Game
    /// Setup's menus, the pause screen): a click on a button is <see cref="Cue.UiClick"/>, the pointer coming onto one
    /// is <see cref="Cue.UiHover"/>. One handler for all panels, so no screen needs to know about sound.
    /// </summary>
    public sealed class UiFeedback : IUIPanelHandler
    {
        private static readonly UiFeedback Instance = new();

        private Button _hovered;

        public static void Register() => UIPanels.Register(Instance);

        public static void Unregister() => UIPanels.Unregister(Instance);

        void IUIPanelHandler.OnPanelCreated(PanelRenderer panel)
        {
            VisualElement root = UIPanels.RootOf(panel);
            if (root == null) return;
            // Registering the same callback again does nothing, so being told twice for one root is harmless.
            root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerOverEvent>(OnOver, TrickleDown.TrickleDown);
        }

        private void OnClick(ClickEvent e)
        {
            if (ButtonOf(e.target as VisualElement) != null) Feedback.Raise(Cue.UiClick);
        }

        private void OnOver(PointerOverEvent e)
        {
            Button button = ButtonOf(e.target as VisualElement);
            if (button == _hovered) return;
            _hovered = button;
            if (button != null) Feedback.Raise(Cue.UiHover);
        }

        // The enabled button the element is, or is inside (a button's label is what the pointer is over).
        private static Button ButtonOf(VisualElement element)
        {
            for (VisualElement e = element; e != null; e = e.parent)
                if (e is Button button) return button.enabledInHierarchy ? button : null;
            return null;
        }
    }
}
