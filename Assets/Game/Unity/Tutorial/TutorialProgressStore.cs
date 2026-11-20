using UnityEngine;

namespace Isekai
{
    /// <summary>Whether the guided first Region has been finished or skipped on this machine.</summary>
    public enum TutorialState
    {
        NotSeen = 0,
        Skipped = 1,
        Finished = 2,
    }

    public static class TutorialProgressStore
    {
        private const string Key = "Isekai.Tutorial.State";

        public static TutorialState State
        {
            get
            {
                int raw = PlayerPrefs.GetInt(Key, 0);
                if (raw is < 0 or > 2) return TutorialState.NotSeen;
                return (TutorialState)raw;
            }
        }

        public static bool NeedsOffer => State == TutorialState.NotSeen;

        public static void MarkSkipped()
        {
            if (State == TutorialState.Finished) return;
            Save(TutorialState.Skipped);
        }

        public static void MarkFinished() => Save(TutorialState.Finished);

        internal static void ResetForTests()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        private static void Save(TutorialState state)
        {
            PlayerPrefs.SetInt(Key, (int)state);
            PlayerPrefs.Save();
        }
    }
}
