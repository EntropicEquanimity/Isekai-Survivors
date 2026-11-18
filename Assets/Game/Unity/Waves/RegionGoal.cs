using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Region's capture condition (<see cref="RegionCapture"/>): every <see cref="EnemyNest"/> destroyed and the
    /// final Wave defeated. On capture the waves stop and, with Game Setup's front end running, it offers the way back
    /// to the menu (phase 7 replaces this with the Planet map).
    /// </summary>
    public sealed class RegionGoal : MonoBehaviour
    {
        public static RegionGoal Current { get; private set; }

        public RegionCapture Capture { get; } = new();

        /// <summary>The Region was just captured.</summary>
        public event System.Action Captured;

        private bool _announced;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        internal void NestAdded() => Capture.AddEnemyBase();

        internal void NestDestroyed() => Capture.EnemyBaseDestroyed();

        private void Update()
        {
            if (_announced || !Capture.IsCaptured) return;
            _announced = true;
            Captured?.Invoke();
            WaveDirector waves = FindAnyObjectByType<WaveDirector>();
            if (waves != null) waves.enabled = false;
            BLog.Info("Region", "Region captured.");
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Region captured!", Color.yellow, 4f);
            if (GameSetup.IsRunning)
                ConfirmPresenter.Ask("Region captured", "Back to the menu?", GameSetup.GoToMainMenu);
        }
    }
}
