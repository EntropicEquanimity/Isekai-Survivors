namespace Isekai
{
    /// <summary>How a Building's Units engage, set on the Building.</summary>
    public enum Stance
    {
        /// <summary>Stay at the post; attack only what comes into range.</summary>
        Hold,

        /// <summary>Chase enemies near the post, then come back.</summary>
        Guard,

        /// <summary>Chase enemies far from the post.</summary>
        Hunt,
    }

    public static class Stances
    {
        /// <summary>How far from its post a Unit with this stance will chase.</summary>
        public static float Leash(this Stance stance) => stance switch
        {
            Stance.Guard => 6f,
            Stance.Hunt => 14f,
            _ => 0f,
        };
    }
}
