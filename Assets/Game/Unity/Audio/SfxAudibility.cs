using UnityEngine;

namespace Isekai
{
    /// <summary>Whether a sound at a place is worth playing: the player sees that ground (fog of war) or it is close to the camera.</summary>
    public static class SfxAudibility
    {
        /// <summary>World units from the camera that a sound is heard even without seeing the ground.</summary>
        public const float HearDistance = 8f;

        public static bool IsAudible(Vector3 position)
        {
            RegionVision vision = RegionVision.Current;
            // No fog (a menu, no Region, fog switched off, not yet baked): everything is audible.
            if (vision == null || !RegionVision.IsOn || !vision.IsReady) return true;
            if (vision.CanSee(Side.Yours, position)) return true;
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 d = cam.transform.position - position;
            return d.x * d.x + d.y * d.y <= HearDistance * HearDistance;
        }
    }
}
