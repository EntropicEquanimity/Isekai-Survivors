using System;
using Blondie.GameSetup;

namespace Isekai
{
    /// <summary>
    /// The game's own rows in Blondie Game Setup's settings screen. The screen already has audio (master, music, sound
    /// volumes), video (window mode, resolution, vsync) and controls; these add the outline colours (the colour-blind
    /// presets and a colour per side, <see cref="SideOutlineSettingsStore"/>) under Accessibility and the game's keys
    /// under Controls. Changes apply at once; the store saves them itself.
    /// </summary>
    public static class GameSettingsRows
    {
        private static readonly (string Name, OutlineColor Colour)[] Palette =
        {
            ("Green", new OutlineColor(61, 220, 100)),
            ("Red", new OutlineColor(255, 74, 74)),
            ("White", new OutlineColor(255, 255, 255)),
            ("Yellow", new OutlineColor(242, 232, 75)),
            ("Purple", new OutlineColor(183, 92, 255)),
            ("Blue", new OutlineColor(64, 190, 255)),
            ("Orange", new OutlineColor(255, 148, 63)),
        };

        private static readonly string[] PresetNames = { "Default", "Deuteranopia", "Protanopia", "Tritanopia", "High contrast" };

        /// <summary>The game's keys, for the Controls tab (read only).</summary>
        public static readonly (string Action, string Keys)[] GameKeys =
        {
            ("Pause", "Esc"),
            ("Map", "M"),
            ("Build", "1-9, hold Shift to keep placing"),
            ("Possess a demon", "P"),
            ("Raise soul level", "L"),
            ("Debug menu", "F1"),
        };

        /// <summary>Adds the rows (before the settings screen opens; Game Setup forgets rows each Play Mode session).</summary>
        public static void Register()
        {
            AddOutlines();
            AddKeys();
        }

        /// <summary>The label of the choice that lets a side use its preset colour.</summary>
        public const string PresetColourText = "Preset colour";

        /// <summary>The options of a side's colour row: the preset colour, the palette, then "Custom" (a colour set another way).</summary>
        public static string[] ColourOptions()
        {
            var options = new string[Palette.Length + 2];
            options[0] = PresetColourText;
            for (int i = 0; i < Palette.Length; i++) options[i + 1] = Palette[i].Name;
            options[^1] = "Custom";
            return options;
        }

        /// <summary>Which of <see cref="ColourOptions"/> a side shows now.</summary>
        public static int ColourIndex(Side side)
        {
            if (!SideOutlineSettingsStore.TryGetOverride(side, out OutlineColor colour)) return 0;
            for (int i = 0; i < Palette.Length; i++)
                if (Palette[i].Colour == colour) return i + 1;
            return Palette.Length + 1;
        }

        /// <summary>Picks option <paramref name="index"/> of <see cref="ColourOptions"/> for a side ("Custom" changes nothing).</summary>
        public static void SetColour(Side side, int index)
        {
            if (index == 0) SideOutlineSettingsStore.ClearOverride(side);
            else if (index <= Palette.Length) SideOutlineSettingsStore.SetOverride(side, Palette[index - 1].Colour);
        }

        private static void AddOutlines()
        {
            const SettingsTab tab = SettingsTab.Accessibility;
            GameOptions.AddHeader(tab, "Outline colours");
            GameOptions.AddChoice(tab, "Colour preset", PresetNames, () => (int)SideOutlineSettingsStore.Preset,
                i => SideOutlineSettingsStore.SetPreset((OutlinePreset)i), wrap: false);
            string[] options = ColourOptions();
            foreach (Side side in SideRelations.All)
            {
                Side captured = side;
                GameOptions.AddChoice(tab, $"{captured} outline", options, () => ColourIndex(captured), i => SetColour(captured, i));
            }
        }

        private static void AddKeys()
        {
            const SettingsTab tab = SettingsTab.Controls;
            GameOptions.AddHeader(tab, "Game keys");
            foreach ((string action, string keys) in GameKeys)
                GameOptions.AddChoice(tab, action, new[] { keys }, () => 0, _ => { }, wrap: false);
        }
    }
}
