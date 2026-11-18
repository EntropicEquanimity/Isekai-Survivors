using System;
using Blondie.Core;
using Blondie.FlowFields;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// Fills the Region with a set number of horde enemies on walkable cells away from every target, a batch per frame,
    /// and (optionally) replaces the ones that die. Phase 5's wave director takes over from this; for now it is the
    /// horde stress test. A <c>-horde N</c> command-line argument overrides the count in builds.
    /// </summary>
    public sealed class HordeSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField, Min(0)] private int _count = 3000;
        [Tooltip("Closest an enemy may appear to a target.")]
        [SerializeField, Min(0f)] private float _minDistance = 14f;
        [SerializeField] private bool _replaceDead = true;
        [SerializeField, Min(1)] private int _perFrame = 200;
        [SerializeField] private int _seed = 1;

        private Random _random;
        private bool _filled;

        public int Count { get => _count; set => _count = Mathf.Max(0, value); }
        public GameObject Prefab => _prefab;

        private void Start()
        {
            _random = new Random(_seed);
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-horde");
            if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int n)) _count = n;
            Pools.Prewarm(_prefab, _count);
        }

        private void Update()
        {
            HordeDirector director = HordeDirector.Current;
            if (director == null || (_filled && !_replaceDead)) return;
            int missing = _count - director.Count;
            if (missing <= 0) { _filled = true; return; }
            for (int k = 0; k < Mathf.Min(missing, _perFrame); k++)
                if (TryPickSpot(director.Grid, out Vector3 spot)) Pools.Spawn(_prefab, spot, Quaternion.identity);
        }

        private bool TryPickSpot(FlowFieldGrid grid, out Vector3 spot)
        {
            float min2 = _minDistance * _minDistance;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var cell = new Int2(_random.Next(grid.Width), _random.Next(grid.Height));
                if (!grid.IsWalkable(cell)) continue;
                Float3 w = grid.CellToWorld(cell);
                spot = new Vector3(w.x + (float)_random.NextDouble() * 0.6f - 0.3f, w.y + (float)_random.NextDouble() * 0.6f - 0.3f, 0f);
                bool farEnough = true;
                foreach (HordeTarget target in HordeTarget.All)
                    if ((target.transform.position - spot).sqrMagnitude < min2) { farEnough = false; break; }
                if (farEnough) return true;
            }
            spot = default;
            return false;
        }
    }
}
