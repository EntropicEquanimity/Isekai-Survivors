using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests
{
    /// <summary>
    /// Waiting in Play Mode tests. Timeouts run on real time, so they still fire while the game is paused (a draft, a
    /// defeat) and don't shrink when a test speeds the game up.
    /// </summary>
    public static class TestWait
    {
        /// <summary>Yields frames until <paramref name="condition"/> holds; fails with <paramref name="what"/> after <paramref name="seconds"/> real seconds.</summary>
        public static IEnumerator Until(Func<bool> condition, string what, float seconds = 5f)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out: " + what);
                yield return null;
            }
        }

        /// <summary>A few frames, for one-frame setup (Start, a first Update) to run.</summary>
        public static IEnumerator Frames(int count = 2)
        {
            for (int i = 0; i < count; i++) yield return null;
        }
    }
}
