namespace Isekai
{
    /// <summary>
    /// A kind of Resource that is mined, carried and spent on Buildings. Unity saves these as numbers in costs and
    /// assets, so the values are fixed: never reorder or renumber them, only add new ones with new numbers. (The
    /// redesign replaces this enum with Resource definition assets; see the plan's architecture notes.)
    /// </summary>
    public enum ResourceType
    {
        Wood = 0,
        Stone = 1,
    }
}
