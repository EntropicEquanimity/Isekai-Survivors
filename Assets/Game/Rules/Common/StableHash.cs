namespace Isekai
{
    /// <summary>
    /// A hash that is the same on every run and machine (string.GetHashCode is not), for seeding a Region's random
    /// streams from its id so the same Run lays out the same Region.
    /// </summary>
    public static class StableHash
    {
        /// <summary>32-bit FNV-1a of <paramref name="text"/>.</summary>
        public static int Of(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }

        /// <summary>Combines a seed with a named stream, e.g. (run seed, "region/thornvale/wilds").</summary>
        public static int Seed(int seed, string stream) => unchecked(seed * 31 + Of(stream));
    }
}
