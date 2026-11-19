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

        private void OnDestroy()
        {
            if (IsDefeated) GamePause.Resume(this);
        }

        private void Lose()
        {
            IsDefeated = true;
            HordeSpawner spawner = FindAnyObjectByType<HordeSpawner>();
            if (spawner != null) spawner.enabled = false;
            GamePause.Pause(this);
            foreach (CharacterControl c in FindObjectsByType<CharacterControl>(FindObjectsInactive.Exclude)) c.Freeze();
            foreach (Behaviour b in new Behaviour[] { FindAnyObjectByType<BuildMode>(), FindAnyObjectByType<Miner>(), FindAnyObjectByType<MapScreen>() })
                if (b != null) b.enabled = false;
            BLog.Info("Region", "Main Base destroyed: the Run is lost.");
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Main Base destroyed", Color.red, 4f);
            Defeated?.Invoke();
        }
    }
}
