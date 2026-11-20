using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>The Anchor's debug rows: pick the Anchor for the next Region, reveal the Shrine, destroy the Anchor, raise a new one.</summary>
    [RegionSystem]
    public sealed class AnchorDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Anchor", "Next Region: Shrine", () => RunSession.Anchor = AnchorKind.Shrine, "anchor");
            DebugActions.Button(this, "Anchor", "Next Region: War Throne", () => RunSession.Anchor = AnchorKind.WarThrone, "anchor");
            DebugActions.Button(this, "Anchor", "Reveal the Shrine", RevealShrine, "anchor");
            DebugActions.Button(this, "Anchor", "Destroy the Anchor", DestroyAnchor, "anchor");
            DebugActions.Button(this, "Anchor", "Raise a new Anchor", () => RegionAnchor.Current?.TryRaise(out _), "anchor");
            DebugActions.Info(this, "Info", () =>
            {
                Building anchor = RegionAnchor.Standing();
                string next = RunSession.Anchor is { } picked ? AnchorTuning.Name(picked) : "none picked";
                if (anchor == null) return $"No Anchor stands · Run's Anchor {next}";
                string kind = AnchorCatalog.KindOf(anchor) is { } k ? AnchorTuning.Name(k) : "Main Base";
                bool hidden = anchor.TryGetComponent(out Anchor a) && a.IsHidden;
                return $"Anchor {kind}" + (hidden ? " (hidden)" : string.Empty) + $" · Run's Anchor {next}";
            });
        }

        private void OnDisable() => DebugActions.Remove(this);

        public static void RevealShrine()
        {
            foreach (Anchor anchor in Anchor.All)
                if (anchor.IsHidden) RegionAnchor.Current?.Find(anchor);
        }

        public static void DestroyAnchor()
        {
            Building anchor = RegionAnchor.Standing();
            if (anchor != null && anchor.TryGetComponent(out Damageable d)) d.Kill();
        }
    }
}
