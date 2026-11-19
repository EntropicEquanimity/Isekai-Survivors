using Blondie.Core;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Lets the Character mine: holding Interact next to a <see cref="ResourceNode"/> mines it at the MiningSpeed stat
    /// (units per second) into the Region's stockpile, with a "+1 Stone" popup per unit.
    /// </summary>
    public sealed class Miner : MonoBehaviour
    {
        public const string InteractId = "Interact";

        [SerializeField, Min(0f)] private float _reach = 1.25f;
        [Tooltip("Units per second when there is no MiningSpeed stat.")]
        [SerializeField, Min(0f)] private float _baseRate = 1f;

        private readonly MiningProgress _progress = new();
        private IStatModifierTarget _stats;
        private ResourceNode _node;

        /// <summary>The node being mined this frame, or null.</summary>
        public ResourceNode Node => _node;

        /// <summary>The node that holding Interact would mine now, or null.</summary>
        public ResourceNode InReach => ResourceNode.NearestTo(transform.position, _reach);

        public float Rate => _stats != null && _stats.HasStat(nameof(StatId.MiningSpeed)) ? _stats.GetStatValue(nameof(StatId.MiningSpeed)) : _baseRate;

        private void Awake() => _stats = GetComponent<IStatModifierTarget>();

        private void Update()
        {
            ResourceNode node = ActionInput.IsHeld(InteractId) ? ResourceNode.NearestTo(transform.position, _reach) : null;
            Mine(node, Time.deltaTime);
        }

        /// <summary>Mines <paramref name="node"/> (null = stop) for <paramref name="seconds"/>; returns the units mined.</summary>
        internal int Mine(ResourceNode node, float seconds)
        {
            if (node != _node) _progress.Reset();
            _node = node;
            if (node == null) return 0;

            int mined = node.Take(_progress.Tick(seconds, Rate, node.Remaining));
            if (mined <= 0) return 0;
            RegionEconomy economy = RegionEconomy.Current;
            if (economy != null) economy.Stockpile.Add(node.Type, mined);
            FloatingText.Spawn(transform.position + Vector3.up, $"+{mined} {node.Type}", Color.white);
            return mined;
        }
    }
}
