using UnityEngine;

namespace Isekai
{
    /// <summary>The Anchors' Building assets (loaded from Resources) and which one the Run picked.</summary>
    public static class AnchorCatalog
    {
        private const string Folder = "Anchors/";

        /// <summary>The Anchor the Run picked; a Run that picked none (a Region opened on its own) raises a Shrine.</summary>
        public static AnchorKind Chosen => RunSession.Anchor ?? AnchorKind.Shrine;

        public static BuildingAsset Asset(AnchorKind kind) => Resources.Load<BuildingAsset>(Folder + AnchorTuning.Name(kind));

        /// <summary>The kind of an Anchor Building, or null for a Main Base that is none of them (the scene's own).</summary>
        public static AnchorKind? KindOf(Building building) =>
            building != null && building.TryGetComponent(out Anchor anchor) ? anchor.Kind : null;
    }
}
