using System;

namespace Isekai
{
    /// <summary>
    /// The Main Base's Cloning Pods. Each Character death uses up one pod to clone the Character back after
    /// <see cref="CloneSeconds"/>; with no pods left the Character stays dead (that alone doesn't lose the Run).
    /// Meta upgrades will add pods or let used ones be rebuilt.
    /// </summary>
    public sealed class CloningBay
    {
        public int Pods { get; private set; }
        public float CloneSeconds { get; }
        public bool IsCloning { get; private set; }
        public float Remaining { get; private set; }

        public CloningBay(int pods, float cloneSeconds)
        {
            if (pods < 0) throw new ArgumentOutOfRangeException(nameof(pods));
            Pods = pods;
            CloneSeconds = Math.Max(0f, cloneSeconds);
        }

        /// <summary>The Character died: starts cloning if a pod is left. Returns whether it did.</summary>
        public bool CharacterDied()
        {
            if (IsCloning || Pods <= 0) return false;
            Pods--;
            IsCloning = true;
            Remaining = CloneSeconds;
            return true;
        }

        /// <returns>True on the tick the clone is ready.</returns>
        public bool Tick(float seconds)
        {
            if (!IsCloning) return false;
            Remaining = Math.Max(0f, Remaining - Math.Max(0f, seconds));
            if (Remaining > 0f) return false;
            IsCloning = false;
            return true;
        }

        public void AddPods(int count) => Pods += Math.Max(0, count);
    }
}
