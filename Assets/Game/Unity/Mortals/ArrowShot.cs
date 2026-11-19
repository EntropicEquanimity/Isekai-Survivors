using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>An arrow in flight for the eye only: it flies from the archer to where the target stood and goes back to the pool. The damage is the archer's reach attack.</summary>
    public sealed class ArrowShot : MonoBehaviour
    {
        private const float Speed = 16f;

        private Vector3 _from;
        private Vector3 _to;
        private float _duration;
        private float _age;

        public void Launch(Vector3 from, Vector3 to)
        {
            _from = from;
            _to = to;
            Vector3 d = to - from;
            _duration = Mathf.Max(0.06f, d.magnitude / Speed);
            _age = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.right = new Vector3(d.x, d.y, 0f).normalized;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _duration)
            {
                Pools.Despawn(gameObject);
                return;
            }
            transform.position = Vector3.Lerp(_from, _to, _age / _duration);
        }
    }
}
