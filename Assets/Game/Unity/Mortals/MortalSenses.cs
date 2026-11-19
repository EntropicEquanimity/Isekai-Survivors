using System.Collections.Generic;
using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// What the mortals see: one spatial hash of every living target hostile to them (the Character, demons, Jack's
    /// Buildings), rebuilt a few times a second and shared by every brain, so a brain's look is one small query.
    /// A target counts only inside the mortal's sight range and on ground the mortals' side sees (walls block it,
    /// night shortens it: <see cref="RegionVision"/>).
    /// </summary>
    internal sealed class MortalSenses
    {
        private const float RebuildSeconds = 0.25f;
        private const float HashCell = 4f;

        private readonly SpatialHash<HordeTarget> _hash = new(HashCell);
        private readonly List<HordeTarget> _found = new();
        private readonly string _team = TeamIds.Of(Side.Mortals);
        private float _nextRebuild;

        public int Count => _hash.Count;

        public void Tick(float now)
        {
            if (now < _nextRebuild) return;
            _nextRebuild = now + RebuildSeconds;
            _hash.Clear();
            foreach (HordeTarget t in HordeTarget.All)
            {
                if (!t.IsAlive || Teams.Relation(_team, t.Team) != TeamRelation.Hostile) continue;
                Vector3 p = t.transform.position;
                _hash.Insert(t, p.x, p.y);
            }
        }

        /// <summary>The closest hostile target within <paramref name="sight"/> of <paramref name="from"/> that the mortals can see; false when there is none.</summary>
        public bool TryNearest(Vector3 from, float sight, out HordeTarget nearest, out float distance)
        {
            nearest = null;
            distance = float.MaxValue;
            _found.Clear();
            // A margin for what moved since the last rebuild and for the target's own size.
            _hash.Query(from.x, from.y, sight + 2f, _found);
            RegionVision vision = RegionVision.IsOn ? RegionVision.Current : null;
            foreach (HordeTarget t in _found)
            {
                if (!t.IsAlive) continue;
                Vector3 p = t.transform.position;
                float d = Mathf.Sqrt((p.x - from.x) * (p.x - from.x) + (p.y - from.y) * (p.y - from.y));
                if (d - t.Radius > sight || d >= distance) continue;
                if (vision != null && vision.IsReady && !vision.CanSee(Side.Mortals, p)) continue;
                nearest = t;
                distance = d;
            }
            return nearest != null;
        }
    }
}
