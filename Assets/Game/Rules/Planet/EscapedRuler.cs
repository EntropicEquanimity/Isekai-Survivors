using System;

namespace Isekai
{
    /// <summary>
    /// One ruler that escaped a captured Region: where they fled, who they are, and the Traits the player now knows.
    /// Kept on the Run and saved between Regions.
    /// </summary>
    [Serializable]
    public sealed class EscapedRuler
    {
        public EscapedRuler(string fromRegionId, string toRegionId, string name, TraitId[] traits)
        {
            FromRegionId = fromRegionId;
            ToRegionId = toRegionId;
            Name = name;
            Traits = traits ?? Array.Empty<TraitId>();
        }

        public string FromRegionId;
        public string ToRegionId;
        public string Name;
        public TraitId[] Traits = Array.Empty<TraitId>();
    }
}
