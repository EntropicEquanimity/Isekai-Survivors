using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// Pixel-perfect zoom: each level is a whole-number upscale of the art (×2 shows each art pixel as 2×2 screen
    /// pixels). The camera renders the screen size divided by that scale, so art never lands between screen pixels.
    /// A higher scale is zoomed in.
    /// </summary>
    public sealed class ZoomLevels
    {
        public int MinScale { get; }
        public int MaxScale { get; }

        /// <summary>The widest view rendered, in art pixels, so zooming out can't draw an unbounded area.</summary>
        public Int2 MaxView { get; }

        public int Scale { get; private set; }

        public ZoomLevels(int minScale, int maxScale, int startScale, Int2 maxView)
        {
            if (minScale < 1) throw new ArgumentOutOfRangeException(nameof(minScale));
            if (maxScale < minScale) throw new ArgumentOutOfRangeException(nameof(maxScale));
            MinScale = minScale;
            MaxScale = maxScale;
            MaxView = maxView;
            Scale = Math.Clamp(startScale, minScale, maxScale);
        }

        /// <summary>Steps the scale by <paramref name="steps"/> (positive zooms in) and returns whether it changed.</summary>
        public bool Step(int steps)
        {
            int next = Math.Clamp(Scale + steps, MinScale, MaxScale);
            if (next == Scale) return false;
            Scale = next;
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
