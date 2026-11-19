namespace Isekai
{
    /// <summary>What a side knows of a ground cell.</summary>
    public enum FogState
    {
        /// <summary>Never seen.</summary>
        Unseen,
        /// <summary>Seen once, out of sight now: shown as last seen.</summary>
        Explored,
        /// <summary>In sight right now.</summary>
        Visible,
    }
}
