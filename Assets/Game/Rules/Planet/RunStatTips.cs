namespace Isekai
{
    /// <summary>What hovering each figure on the victory and defeat screens explains (one table, ready for localisation).</summary>
    public static class RunStatTips
    {
        public static string Header(RunStat stat) => stat switch
        {
            RunStat.Regions => "Regions captured",
            RunStat.Time => "Run time",
            RunStat.Deaths => "Deaths",
            RunStat.Souls => "Souls",
            RunStat.SoulLevel => "Soul level",
            RunStat.RulersSlain => "Rulers slain",
            _ => "Rulers escaped",
        };

        public static string Text(RunStat stat) => stat switch
        {
            RunStat.Regions => "Regions of the Planet you took by destroying their Seat, out of every Region on it. Taking the Capital wins the Run.",
            RunStat.Time => "Time spent playing Regions across the whole Run. The Planet map does not count.",
            RunStat.Deaths => "How many times Jack's body died in this Run. Each death costs Souls to come back.",
            RunStat.Souls => "Souls Jack held when the Run ended. Souls revive him, and a bodiless Jack with none left loses the Run.",
            RunStat.SoulLevel => "How far Jack's soul had grown. A higher level lets him hold more demons before they turn on him.",
            RunStat.RulersSlain => "Rulers who fell with their Seats.",
            _ => "Rulers who escaped inward when their Seat fell. They return, with the same Traits, in the Region they fled to.",
        };
    }
}
