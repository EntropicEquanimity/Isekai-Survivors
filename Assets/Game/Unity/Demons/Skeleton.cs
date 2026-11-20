using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>A skeleton a bone demon raised from a pile: it fights like any demon of the Portal, then crumbles away after its time is up.</summary>
    public sealed class Skeleton : MonoBehaviour
    {
        private float _crumbleAt = float.MaxValue;

        /// <summary>Seconds left before it crumbles.</summary>
        public float Remaining => Mathf.Max(0f, _crumbleAt - Time.time);

        /// <summary>Starts its clock: it crumbles <paramref name="seconds"/> from now.</summary>
        public void Raise(float seconds) => _crumbleAt = Time.time + seconds;

        private void OnEnable() => _crumbleAt = float.MaxValue;   // a pooled skeleton comes back fresh, until it is raised

        private void Update()
        {
            if (Time.time >= _crumbleAt) Pools.Despawn(gameObject);
        }
    }
}
