using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Losing the Main Base loses the Run: the game freezes (time stops, the Character and building stop taking orders),
    /// it says so on screen and <see cref="Defeated"/> fires. The freeze lifts when the Region unloads.
    /// </summary>
    public sealed class RegionDefeat : MonoBehaviour
    {
        private bool _hadMainBase;

        public bool IsDefeated { get; private set; }

        private void Update()
        {
            if (IsDefeated) return;
            bool alive = false;
            foreach (Building b in Building.All)
            {
                if (!b.IsMainBase || !b.IsPlayers) continue;
                _hadMainBase = true;
                alive |= b.IsAlive;
            }
            if (_hadMainBase && !alive) Lose();
        }

        private void OnDestroy()
        {
            if (IsDefeated) GamePause.Resume(this);
        }

        private void Lose()
        {
            IsDefeated = true;
            GamePause.Pause(this);
            BLog.Info("Region", "Main Base destroyed: the Run is lost.");
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Main Base destroyed", Color.red, 4f);
            RegionEvents.End(RegionEnd.Defeated);   // each system stops itself (IRegionEndListener)
        }
    }
}
