using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// Pixel-perfect zoom: each level is a whole-number upscale of the art (×2 shows each art pixel as 2×2 screen
    /// pixels). The camera renders the screen size divided by that scale, so art never lands between screen pixels.
    /// A higher scale is zoomed in.
    /// <para><b>Far levels.</b> Past the widest view, zooming out keeps the same low-res picture but halves the pixels
    /// per world unit for each far level (<see cref="PixelDivisor"/>): twice the Region across, art drawn at half size,
    /// still point-sampled and pixelated.</para>
    /// </summary>
    public sealed class ZoomLevels
    {
        public int MinScale { get; }
        public int MaxScale { get; }

        /// <summary>The widest view rendered, in art pixels, so zooming out can't draw an unbounded area.</summary>
        public Int2 MaxView { get; }

        /// <summary>How many far levels lie past the widest view.</summary>
        public int FarLevels { get; }

        public int Scale { get; private set; }

        /// <summary>The far level in use: 0 is the normal range, each step out halves the pixels per unit.</summary>
        public int Far { get; private set; }

        /// <summary>What the art's pixels per unit is divided by at the current far level (1, 2, 4...).</summary>
        public int PixelDivisor => 1 << Far;

        public ZoomLevels(int minScale, int maxScale, int startScale, Int2 maxView, int farLevels = 0)
        {
            if (minScale < 1) throw new ArgumentOutOfRangeException(nameof(minScale));
            if (maxScale < minScale) throw new ArgumentOutOfRangeException(nameof(maxScale));
            if (farLevels < 0) throw new ArgumentOutOfRangeException(nameof(farLevels));
            MinScale = minScale;
            MaxScale = maxScale;
            MaxView = maxView;
            FarLevels = farLevels;
            Scale = Math.Clamp(startScale, minScale, maxScale);
        }

        /// <summary>Steps the scale by <paramref name="steps"/> (positive zooms in) and returns whether it changed. Ignores far levels.</summary>
        public bool Step(int steps)
        {
            int next = Math.Clamp(Scale + steps, MinScale, MaxScale);
            if (next == Scale) return false;
            Scale = next;
            return true;
        }

        /// <summary>
        /// One zoom step on <paramref name="screen"/> (positive = in), through the far levels too: out past the widest
        /// view goes to the next far level, and in from a far level comes back before the scale changes. Returns
        /// whether the picture changed.
        /// </summary>
        public bool Zoom(int steps, Int2 screen)
        {
            bool changed = false;
            for (int i = 0; i < Math.Abs(steps); i++) changed |= steps > 0 ? StepIn(screen) : StepOut(screen);
            return changed;
        }

        private bool StepIn(Int2 screen)
        {
            if (Far > 0) { Far--; return true; }
            int before = EffectiveScale(screen);
            while (Step(+1))
                if (EffectiveScale(screen) != before) return true;
            return false;
        }

        private bool StepOut(Int2 screen)
        {
            if (Far == 0)
            {
                int before = EffectiveScale(screen);
                int saved = Scale;
                while (Step(-1))
                    if (EffectiveScale(screen) != before) return true;
                Scale = saved;   // nothing wider at this pixel size: don't drift below what the screen shows
            }
            if (Far >= FarLevels) return false;
            Far++;
            return true;
        }

        /// <summary>The scale actually used on <paramref name="screen"/>: raised until the view fits <see cref="MaxView"/>.</summary>
        public int EffectiveScale(Int2 screen)
        {
            int scale = Scale;
            while (scale < MaxScale && (Divide(screen.x, scale) > MaxView.x || Divide(screen.y, scale) > MaxView.y)) scale++;
            return scale;
        }

        /// <summary>The camera's reference resolution on <paramref name="screen"/> at the current level.</summary>
        public Int2 ViewFor(Int2 screen)
        {
            int scale = EffectiveScale(screen);
            return new Int2(Divide(screen.x, scale), Divide(screen.y, scale));
        }

        private static int Divide(int pixels, int scale) => Math.Max(1, pixels / scale);
    }
}
