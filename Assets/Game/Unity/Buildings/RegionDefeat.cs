using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Run is lost when Jack's Souls run out while he is bodiless (<see cref="RegionSouls.IsLost"/>); his body dying or the
    /// Anchor falling does not lose it. Then the game freezes (time stops, the Character and building stop taking orders),
    /// it says so on screen and the Region ends as defeated. The freeze lifts when the Region unloads.
    /// </summary>
    public sealed class RegionDefeat : MonoBehaviour
    {
        public bool IsDefeated { get; private set; }

        private void Update()
        {
            if (IsDefeated) return;
            RegionSouls souls = RegionSouls.Current;
            if (souls != null && souls.IsLost) Lose();
        }

        private void OnDestroy()
        {
            if (IsDefeated) GamePause.Resume(this);
        }

        private void Lose()
        {
            IsDefeated = true;
            GamePause.Pause(this);
            BLog.Info("Region", "Jack's Souls ran out: the Run is lost.");
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Jack's soul has faded", Color.red, 4f);
            RegionEvents.End(RegionEnd.Defeated);   // each system stops itself (IRegionEndListener)
        }
    }
}
