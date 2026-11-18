using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Losing the Main Base loses the Run: the horde stops spawning, it says so on screen and <see cref="Defeated"/> fires.
    /// </summary>
    public sealed class RegionDefeat : MonoBehaviour
    {
        private bool _hadMainBase;

        public bool IsDefeated { get; private set; }

        /// <summary>The Main Base fell (<see cref="RegionRun"/> ends the Run).</summary>
        public event System.Action Defeated;

        private void Update()
        {
            if (IsDefeated) return;
            bool alive = false;
            foreach (Building b in Building.All)
            {
                if (!b.IsMainBase) continue;
                _hadMainBase = true;
                alive |= b.IsAlive;
            }
            if (_hadMainBase && !alive) Lose();
        }

        private void Lose()
        {
            IsDefeated = true;
            HordeSpawner spawner = FindAnyObjectByType<HordeSpawner>();
            if (spawner != null) spawner.enabled = false;
            BLog.Info("Region", "Main Base destroyed: the Run is lost.");
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Main Base destroyed", Color.red, 4f);
            Defeated?.Invoke();
        }
    }
}
