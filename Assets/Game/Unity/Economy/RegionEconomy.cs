using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>The Region's <see cref="Stockpile"/>: one per Region scene, found through <see cref="Current"/>.</summary>
    public sealed class RegionEconomy : MonoBehaviour
    {
        [Serializable]
        private struct StartingAmount
        {
            public ResourceType Type;
            [Min(0)] public int Amount;
        }

        [SerializeField] private StartingAmount[] _starting = Array.Empty<StartingAmount>();

        public static RegionEconomy Current { get; private set; }

        public Stockpile Stockpile { get; } = new();

        private void Awake()
        {
            foreach (StartingAmount s in _starting) Stockpile.Add(s.Type, s.Amount);
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }
    }
}
