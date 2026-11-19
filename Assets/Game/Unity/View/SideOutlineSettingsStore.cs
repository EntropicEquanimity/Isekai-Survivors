using System.Globalization;
using UnityEngine;

namespace Isekai
{
    /// <summary>Persistent player settings for side outline colours (preset + per-side overrides).</summary>
    public static class SideOutlineSettingsStore
    {
        private const string KeyPreset = "Isekai.View.Outline.Preset";
        private const string KeyOverridePrefix = "Isekai.View.Outline.Override.";

        private static SideOutlineSettings s_settings;

        public static SideOutlineSettings Settings
        {
            get
            {
                if (s_settings == null) s_settings = Load();
                return s_settings;
            }
        }

        public static OutlinePreset Preset => Settings.Preset;

        public static OutlineColor ColorOf(Side side) => Settings.ColorOf(side);

        public static void SetPreset(OutlinePreset preset)
        {
            Settings.SetPreset(preset);
            SaveAndApply();
        }

        public static void SetOverride(Side side, OutlineColor color)
        {
            Settings.SetOverride(side, color);
            SaveAndApply();
        }

        public static bool ClearOverride(Side side)
        {
            bool changed = Settings.ClearOverride(side);
            if (changed) SaveAndApply();
            return changed;
        }

        public static void ClearAllOverrides()
        {
            Settings.ClearOverrides();
            SaveAndApply();
        }

        public static bool TryGetOverride(Side side, out OutlineColor color) => Settings.TryGetOverride(side, out color);

        internal static void ResetForTests() => s_settings = null;

        private static SideOutlineSettings Load()
        {
            var preset = (OutlinePreset)PlayerPrefs.GetInt(KeyPreset, (int)OutlinePreset.Default);
            if (!System.Enum.IsDefined(typeof(OutlinePreset), preset)) preset = OutlinePreset.Default;

            var settings = new SideOutlineSettings(preset);
            foreach (Side side in SideRelations.All)
            {
                string packed = PlayerPrefs.GetString(KeyFor(side), string.Empty);
                if (!TryParseColor(packed, out OutlineColor color)) continue;
                settings.SetOverride(side, color);
            }
            SideOutlineMaterials.Apply(settings);
            return settings;
        }

        private static void SaveAndApply()
        {
            PlayerPrefs.SetInt(KeyPreset, (int)Settings.Preset);
            foreach (Side side in SideRelations.All)
            {
                if (Settings.TryGetOverride(side, out OutlineColor color)) PlayerPrefs.SetString(KeyFor(side), Pack(color));
                else PlayerPrefs.DeleteKey(KeyFor(side));
            }
            PlayerPrefs.Save();
            SideOutlineMaterials.Apply(Settings);
        }

        private static string KeyFor(Side side) => KeyOverridePrefix + side;

        private static string Pack(OutlineColor c) => $"{c.R},{c.G},{c.B},{c.A}";

        private static bool TryParseColor(string packed, out OutlineColor color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(packed)) return false;
            string[] parts = packed.Split(',');
            if (parts.Length < 3) return false;
            if (!TryByte(parts[0], out byte r) || !TryByte(parts[1], out byte g) || !TryByte(parts[2], out byte b)) return false;
            byte a = 255;
            if (parts.Length > 3 && !TryByte(parts[3], out a)) return false;
            color = new OutlineColor(r, g, b, a);
            return true;
        }

        private static bool TryByte(string value, out byte result) =>
            byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
