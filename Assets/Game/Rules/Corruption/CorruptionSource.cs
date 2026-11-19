using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// What a spreader is: how far it reaches (<see cref="Radius"/> cells) and how fast (each <see cref="SecondsPerStep"/>
    /// its front grows one ring wider and every cell in reach goes up one level, up to its cap).
    /// </summary>
    public readonly struct CorruptionSource
    {
        public CorruptionSource(int radius, float secondsPerStep)
        {
            Radius = radius;
            SecondsPerStep = secondsPerStep;
        }

        public int Radius { get; }
        public float SecondsPerStep { get; }

        /// <summary>The highest level this source takes a cell <paramref name="distance"/> cells away to: the maximum at its heart, level 1 at its edge.</summary>
        public int Cap(int distance)
        {
            if (distance < 0 || distance > Radius) return 0;
            int cap = CorruptionTuning.MaxLevel - distance * CorruptionTuning.MaxLevel / (Radius + 1);
            return cap < 1 ? 1 : cap;
        }
    }

    /// <summary>
    /// One spreader at work: every step its front grows a ring wider (nearer cells first, up to the source's radius)
    /// and every cell inside the front below its cap goes up one level. Purifying lowers a cell again, so ground near a
    /// source is won back at its rate.
    /// </summary>
    public sealed class CorruptionSpreader
    {
        private float _clock;
        private int _steps;

        public CorruptionSpreader(CorruptionSource source, Int2 cell)
        {
            Source = source;
            Cell = cell;
        }

        public CorruptionSource Source { get; }

        /// <summary>Where it stands now (a body moves).</summary>
        public Int2 Cell { get; set; }

        /// <summary>How many rings out it has reached (0 is just its own cell, -1 nothing yet).</summary>
        public int Front => _steps == 0 ? -1 : Math.Min(_steps - 1, Source.Radius);

        /// <summary>Lets time pass; spreads onto <paramref name="field"/> for every step it completes.</summary>
        public void Tick(float seconds, CorruptionField field)
        {
            _clock += seconds;
            while (_clock >= Source.SecondsPerStep)
            {
                _clock -= Source.SecondsPerStep;
                _steps++;
                Step(field);
            }
        }

        private void Step(CorruptionField field)
        {
            int front = Front;
            for (int dy = -front; dy <= front; dy++)
            for (int dx = -front; dx <= front; dx++)
            {
                int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                field.RaiseTo(new Int2(Cell.x + dx, Cell.y + dy), Source.Cap(d), 1);
            }
        }
    }
}
