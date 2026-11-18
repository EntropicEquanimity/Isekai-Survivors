using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>A kind of Building: its footprint, price, build time and the prefab placed in the world.</summary>
    [CreateAssetMenu(menuName = "Isekai/Building", fileName = "Building")]
    public sealed class BuildingAsset : ScriptableObject
    {
        [SerializeField] private string _displayName = "Building";
        [SerializeField] private GameObject _prefab;
        [Tooltip("Cells wide and tall.")]
        [SerializeField] private Vector2Int _size = new(2, 2);
        [SerializeField] private ResourceCost[] _costs = Array.Empty<ResourceCost>();
        [SerializeField, Min(0f)] private float _buildSeconds = 8f;
        [Tooltip("Shown on the placement ghost.")]
        [SerializeField] private Sprite _preview;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public Vector2Int Size => _size;
        public ResourceCost[] Costs => _costs;
        public float BuildSeconds => _buildSeconds;
        public Sprite Preview => _preview;

        public string PriceText => _costs.Length == 0 ? "free" : string.Join(", ", Array.ConvertAll(_costs, c => c.ToString()));
    }
}
