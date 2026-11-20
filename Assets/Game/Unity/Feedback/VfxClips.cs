using UnityEngine;

namespace Isekai
{
    /// <summary>The visual effects the Region can play, one flipbook each (see <see cref="VfxClips"/>).</summary>
    public enum VfxKind
    {
        DeathSplat,
        WispBurst,
        Summon,
        Possess,
        WispTrail,
        Revive,
        CorruptionPulse,
        Purify,
        Alert,
        Wave,
        SpawnCircle,
        RiderArrives,
        FireworkGold,
        FireworkGreen,
        Crown,
    }

    /// <summary>One effect's frames cut from its sheet, played at <see cref="Fps"/>.</summary>
    public sealed class VfxClip
    {
        public VfxClip(VfxKind kind, Sprite[] frames, float fps, int cap)
        {
            Kind = kind;
            Frames = frames;
            Fps = fps;
            Cap = cap;
        }

        public VfxKind Kind { get; }
        public Sprite[] Frames { get; }
        public float Fps { get; }

        /// <summary>Most of this effect alive at once, so one busy kind (deaths in a horde fight) cannot use up the pool.</summary>
        public int Cap { get; }

        public float Duration => Frames.Length / Fps;
    }

    /// <summary>
    /// The sheets in Art/Vfx/Resources (unTied Games' Super Pixel Effects Gigapack, see CREDITS.txt) cut into frames once,
    /// the way the wisp cuts its own: 16 pixels per unit, the pivot on a whole pixel so an effect drawn at a whole-pixel
    /// position stays on the 16 px grid.
    /// </summary>
    public static class VfxClips
    {
        private const float PixelsPerUnit = 16f;

        private readonly struct Def
        {
            public Def(VfxKind kind, string sheet, int width, int height, int frames, float fps, int cap)
            {
                Kind = kind; Sheet = sheet; Width = width; Height = height; Frames = frames; Fps = fps; Cap = cap;
            }

            public readonly VfxKind Kind;
            public readonly string Sheet;
            public readonly int Width, Height, Frames, Cap;
            public readonly float Fps;
        }

        // Frame size, frame count and fps from each pack sheet's spritesheet.txt; frames run left to right, then down.
        private static readonly Def[] s_defs =
        {
            new(VfxKind.DeathSplat, "Vfx_Death_Splat", 32, 32, 10, 24f, 64),
            new(VfxKind.WispBurst, "Vfx_Wisp_Burst", 32, 32, 12, 15f, 4),
            new(VfxKind.Summon, "Vfx_Summon", 48, 48, 10, 20f, 16),
            new(VfxKind.Possess, "Vfx_Possess", 64, 64, 31, 24f, 4),
            new(VfxKind.WispTrail, "Vfx_Wisp_Trail", 32, 32, 14, 24f, 24),
            new(VfxKind.Revive, "Vfx_Revive", 72, 72, 16, 18f, 4),
            new(VfxKind.CorruptionPulse, "Vfx_Corruption_Pulse", 32, 32, 8, 16f, 16),
            new(VfxKind.Purify, "Vfx_Purify", 48, 48, 24, 20f, 16),
            new(VfxKind.Alert, "Vfx_Alert", 40, 40, 14, 15f, 24),
            new(VfxKind.Wave, "Vfx_Wave", 48, 48, 10, 15f, 8),
            new(VfxKind.SpawnCircle, "Vfx_Spawn_Circle", 64, 64, 12, 15f, 16),
            new(VfxKind.RiderArrives, "Vfx_Rider_Arrives", 32, 32, 11, 18f, 4),
            new(VfxKind.FireworkGold, "Vfx_Firework_Gold", 48, 48, 27, 18f, 12),
            new(VfxKind.FireworkGreen, "Vfx_Firework_Green", 48, 48, 15, 18f, 12),
            new(VfxKind.Crown, "Vfx_Crown", 32, 24, 45, 18f, 2),
        };

        private static VfxClip[] s_clips;

        /// <summary>The clip of <paramref name="kind"/>, or null when its sheet is missing.</summary>
        public static VfxClip Of(VfxKind kind)
        {
            if (s_clips == null || !IsAlive(s_clips)) s_clips = Load();
            return s_clips[(int)kind];
        }

        // Sprites made in code are lost when Unity unloads unused assets between scenes.
        private static bool IsAlive(VfxClip[] clips)
        {
            foreach (VfxClip clip in clips)
                if (clip != null) return clip.Frames[0] != null;
            return false;
        }

        private static VfxClip[] Load()
        {
            var clips = new VfxClip[s_defs.Length];
            foreach (Def d in s_defs)
            {
                var sheet = Resources.Load<Texture2D>(d.Sheet);
                if (sheet == null) continue;
                int columns = Mathf.Max(1, sheet.width / d.Width);
                var pivot = new Vector2(Mathf.Floor(d.Width / 2f) / d.Width, Mathf.Floor(d.Height / 2f) / d.Height);
                var frames = new Sprite[d.Frames];
                for (int i = 0; i < d.Frames; i++)
                {
                    int x = i % columns * d.Width;
                    int y = sheet.height - (i / columns + 1) * d.Height;
                    frames[i] = Sprite.Create(sheet, new Rect(x, y, d.Width, d.Height), pivot, PixelsPerUnit);
                    frames[i].name = $"{d.Sheet}_{i}";
                }
                clips[(int)d.Kind] = new VfxClip(d.Kind, frames, d.Fps, d.Cap);
            }
            return clips;
        }
    }
}
