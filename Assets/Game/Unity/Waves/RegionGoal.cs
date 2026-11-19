using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Region's capture condition (<see cref="RegionCapture"/>): every <see cref="EnemyNest"/> destroyed and the
    /// final Wave defeated. On capture it ends the Region (<see cref="RegionEvents"/>): the Waves stop and
    /// <see cref="RegionRun"/> takes it from there.
    /// </summary>
    public sealed class RegionGoal : MonoBehaviour
    {
        public static RegionGoal Current { get; private set; }

        public RegionCapture Capture { get; } = new();

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
            RegionEvents.End(RegionEnd.Captured);
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Region captured!", Color.yellow, 4f);
        }
    }
}
