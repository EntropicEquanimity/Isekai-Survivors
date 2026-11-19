using System;

namespace Isekai
{
    /// <summary>
    /// The Main Base's Cloning Pods. Each Character death uses up one pod to clone the Character back after
    /// <see cref="CloneSeconds"/>; with no pods left the Character stays dead (that alone doesn't lose the Run).
    /// With <see cref="RebuildSeconds"/> above 0 (a meta upgrade), used pods are rebuilt one at a time.
    /// </summary>
    public sealed class CloningBay
    {
        public int Pods { get; private set; }
        public float CloneSeconds { get; }
        public bool IsCloning { get; private set; }
        public float Remaining { get; private set; }

        /// <summary>Pods the bay holds when none are used.</summary>
        public int Capacity { get; private set; }

        /// <summary>Seconds to rebuild one used pod; 0 = used pods are gone for good.</summary>
        public float RebuildSeconds { get; }

        /// <summary>Seconds until the next used pod is rebuilt (0 when none is being rebuilt).</summary>
        public float RebuildRemaining { get; private set; }

        public CloningBay(int pods, float cloneSeconds, float rebuildSeconds = 0f)
        {
            if (pods < 0) throw new ArgumentOutOfRangeException(nameof(pods));
            Pods = Capacity = pods;
            CloneSeconds = Math.Max(0f, cloneSeconds);
            RebuildSeconds = Math.Max(0f, rebuildSeconds);
        }

        /// <summary>The Character died: starts cloning if a pod is left. Returns whether it did.</summary>
        public bool CharacterDied()
        {
            if (IsCloning || Pods <= 0) return false;
            Pods--;
            if (RebuildSeconds > 0f && RebuildRemaining <= 0f) RebuildRemaining = RebuildSeconds;
            IsCloning = true;
            Remaining = CloneSeconds;
            return true;
        }

        /// <returns>True on the tick the clone is ready.</returns>
        public bool Tick(float seconds)
        {
            seconds = Math.Max(0f, seconds);
            TickRebuild(seconds);
            if (!IsCloning) return false;
            Remaining = Math.Max(0f, Remaining - seconds);
            if (Remaining > 0f) return false;
            IsCloning = false;
            return true;
        }

        public void AddPods(int count)
        {
            Pods += Math.Max(0, count);
            Capacity += Math.Max(0, count);
        }

        private void TickRebuild(float seconds)
        {
            if (RebuildSeconds <= 0f || Pods >= Capacity) return;
            RebuildRemaining -= seconds;
            if (RebuildRemaining > 0f) return;
            Pods++;
            RebuildRemaining = Pods < Capacity ? RebuildSeconds : 0f;
        }
    }
}
