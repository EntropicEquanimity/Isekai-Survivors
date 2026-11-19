namespace Isekai
{
    /// <summary>How alert a Region's mortals are right now.</summary>
    public enum VigilanceStage
    {
        Unaware = 0,
        Uneasy = 1,
        Alarmed = 2,
        Mobilized = 3,
        Crusade = 4,
    }

    public static class VigilanceStageNames
    {
        public static string Name(VigilanceStage stage) => stage switch
        {
            VigilanceStage.Unaware => "Unaware",
            VigilanceStage.Uneasy => "Uneasy",
            VigilanceStage.Alarmed => "Alarmed",
            VigilanceStage.Mobilized => "Mobilized",
            _ => "Crusade",
        };
    }
}
