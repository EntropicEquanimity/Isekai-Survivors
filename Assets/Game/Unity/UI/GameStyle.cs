using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>The kind of text a label shows; its size and colour come from <see cref="GameStyle"/>.</summary>
    public enum TextRole { Body, Title, Muted, Accent, Warning }

    /// <summary>
    /// The game's look (dark reds, black, ember) in one place for code-built UI: the pixel font's sizes (multiples of its native 12 px, anything
    /// else blurs) and the palette. Screens ask for a role ("title", "muted") instead of writing sizes and colours, so
    /// a restyle (the demon theme) changes this file, not every screen. A role is a USS class
    /// (<c>isekai-role-body</c>...) whose size comes from <see cref="HudSheet"/>, so the player's Text size reaches it.
    /// Rules for what a control shows: docs/ui-rules.md (icons first, then numbers, text last).
    /// </summary>
    public static class GameStyle
    {
        /// <summary>The pixel font's native size.</summary>
        public const int FontSize = 12;

        public const int TitleSize = FontSize * 2;

        public static readonly Color Text = Color.white;
        public static readonly Color Muted = new(0.78f, 0.67f, 0.64f);
        public static readonly Color Accent = new(1f, 0.48f, 0.15f);   // ember
        public static readonly Color Warning = new(1f, 0.82f, 0.35f);

        /// <summary>Full-screen backdrops behind windows (solid: nothing shows through).</summary>
        public static readonly Color Backdrop = new(0.04f, 0.015f, 0.02f, 1f);

        public static readonly Color Border = new(0.42f, 0.12f, 0.11f);

        public static int SizeOf(TextRole role) => role == TextRole.Title ? TitleSize : FontSize;

        /// <summary>The class a role's text carries (its size comes from the stylesheet).</summary>
        public static string ClassOf(TextRole role) => "isekai-role-" + role.ToString().ToLowerInvariant();

        /// <summary>The stylesheet of the game's own panels: text roles, buttons, icon buttons (Resources/IsekaiHud).</summary>
        public static StyleSheet HudSheet => s_hudSheet != null ? s_hudSheet : s_hudSheet = Resources.Load<StyleSheet>("IsekaiHud");

        private static StyleSheet s_hudSheet;

        /// <summary>An icon: a sprite drawn into a square (<c>isekai-icon</c>; add <c>--large</c> or <c>--huge</c> for 32 or 48 px).</summary>
        public static VisualElement Icon(Sprite sprite, string size = null)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("isekai-icon");
            if (size != null) icon.AddToClassList("isekai-icon--" + size);
            if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
            return icon;
        }

        public static Color ColourOf(TextRole role) => role switch
        {
            TextRole.Muted => Muted,
            TextRole.Accent => Accent,
            TextRole.Warning => Warning,
            _ => Text,
        };

        /// <summary>A label in the game's style for <paramref name="role"/>.</summary>
        public static Label Label(string text, TextRole role = TextRole.Body)
        {
            var label = new Label(text);
            Apply(label, role);
            return label;
        }

        /// <summary>Gives an existing element the size and colour of <paramref name="role"/>.</summary>
        public static T Apply<T>(T element, TextRole role) where T : VisualElement
        {
            foreach (TextRole r in s_roles) element.RemoveFromClassList(ClassOf(r));
            element.AddToClassList(ClassOf(role));
            element.style.color = ColourOf(role);
            return element;
        }

        private static readonly TextRole[] s_roles = { TextRole.Body, TextRole.Title, TextRole.Muted, TextRole.Accent, TextRole.Warning };
    }
}
