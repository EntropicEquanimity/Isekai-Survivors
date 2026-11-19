using System;

namespace Isekai
{
    /// <summary>Named outline palettes players can pick from, plus a high-contrast option.</summary>
    public enum OutlinePreset
    {
        Default,
        Deuteranopia,
        Protanopia,
        Tritanopia,
        HighContrast,
    }

    /// <summary>Engine-free RGB colour for side outlines.</summary>
    public readonly struct OutlineColor : IEquatable<OutlineColor>
    {
        public OutlineColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public bool Equals(OutlineColor other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is OutlineColor other && Equals(other);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
        public static bool operator ==(OutlineColor a, OutlineColor b) => a.Equals(b);
        public static bool operator !=(OutlineColor a, OutlineColor b) => !a.Equals(b);
        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }

    /// <summary>Built-in side colours for each outline preset.</summary>
    public static class SideOutlinePalettes
    {
        private static readonly OutlineColor[] s_default =
        {
            new(61, 220, 100),  // yours
            new(255, 74, 74),   // mortals
            new(255, 255, 255), // neutral
            new(242, 232, 75),  // hostile wildlife
            new(183, 92, 255),  // rogue
        };

        private static readonly OutlineColor[] s_deuteranopia =
        {
            new(38, 150, 246),
            new(245, 139, 36),
            new(255, 255, 255),
            new(255, 224, 77),
            new(192, 89, 255),
        };

        private static readonly OutlineColor[] s_protanopia =
        {
            new(52, 158, 235),
            new(255, 152, 88),
            new(255, 255, 255),
            new(255, 228, 91),
            new(185, 104, 255),
        };

        private static readonly OutlineColor[] s_tritanopia =
        {
            new(62, 212, 120),
            new(255, 122, 84),
            new(255, 255, 255),
            new(233, 217, 71),
            new(175, 92, 255),
        };

        private static readonly OutlineColor[] s_highContrast =
        {
            new(0, 255, 0),
            new(255, 0, 64),
            new(255, 255, 255),
            new(255, 234, 0),
            new(181, 0, 255),
        };

        public static OutlineColor ColorOf(OutlinePreset preset, Side side)
        {
            int index = (int)side;
            OutlineColor[] table = preset switch
            {
                OutlinePreset.Deuteranopia => s_deuteranopia,
                OutlinePreset.Protanopia => s_protanopia,
                OutlinePreset.Tritanopia => s_tritanopia,
                OutlinePreset.HighContrast => s_highContrast,
                _ => s_default,
            };
            if ((uint)index >= (uint)table.Length) index = (int)Side.Neutral;
            return table[index];
        }
    }

    /// <summary>The chosen outline preset and optional per-side colour overrides.</summary>
    public sealed class SideOutlineSettings
    {
        private readonly bool[] _hasOverride = new bool[SideRelations.All.Length];
        private readonly OutlineColor[] _overrides = new OutlineColor[SideRelations.All.Length];

        public SideOutlineSettings(OutlinePreset preset = OutlinePreset.Default) => Preset = preset;

        public OutlinePreset Preset { get; private set; }

        public void SetPreset(OutlinePreset preset) => Preset = preset;

        public OutlineColor ColorOf(Side side)
        {
            int i = (int)side;
            if ((uint)i < (uint)_hasOverride.Length && _hasOverride[i]) return _overrides[i];
            return SideOutlinePalettes.ColorOf(Preset, side);
        }

        public void SetOverride(Side side, OutlineColor color)
        {
            int i = (int)side;
            if ((uint)i >= (uint)_hasOverride.Length) return;
            _hasOverride[i] = true;
            _overrides[i] = color;
        }

        public bool TryGetOverride(Side side, out OutlineColor color)
        {
            int i = (int)side;
            if ((uint)i < (uint)_hasOverride.Length && _hasOverride[i])
            {
                color = _overrides[i];
                return true;
            }
            color = default;
            return false;
        }

        public bool ClearOverride(Side side)
        {
            int i = (int)side;
            if ((uint)i >= (uint)_hasOverride.Length || !_hasOverride[i]) return false;
            _hasOverride[i] = false;
            _overrides[i] = default;
            return true;
        }

        public void ClearOverrides()
        {
            for (int i = 0; i < _hasOverride.Length; i++)
            {
                _hasOverride[i] = false;
                _overrides[i] = default;
            }
        }
    }
}
