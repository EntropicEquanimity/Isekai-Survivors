using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>What a creature has picked to go for: who, how big, where and how far.</summary>
    internal readonly struct WildlifeTarget
    {
        public WildlifeTarget(Damageable damageable, float radius, Vector3 position, float distance)
        {
            Damageable = damageable;
            Radius = radius;
            Position = position;
            Distance = distance;
        }

        public Damageable Damageable { get; }
        public float Radius { get; }
        public Vector3 Position { get; }
        public float Distance { get; }
    }

    /// <summary>
    /// A Region's wildlife (spec: "Wildlife: Werebear, Werewolf, Slime. Wildlife are neutral or hostile and give a little
    /// Souls, without Vigilance."): it fills the wildlife spots <see cref="RegionTerrain"/> chose with groups of
    /// <see cref="WildlifeSpawns"/>, then runs every creature's brain from one place, as <see cref="RegionMortals"/> does for mortals:
    /// each thinks a few times a second (staggered) and the ones on the move step every frame. It owns what they share:
    /// what they see and the 6-cell vision range (<see cref="VisionRanges.Wildlife"/>).
    /// <para>Slimes are on the Neutral side, werewolves and werebears on the Hostile side (<see cref="WildlifeProfiles"/>); their deaths
    /// are announced as <see cref="UnitKind.Wildlife"/>, which gives Souls (<see cref="SoulRules"/>) and never Vigilance.</para>
    /// </summary>
    [RegionSystem]
    public sealed class RegionWildlife : MonoBehaviour, IRegionPopulate
    {
        private readonly List<WildlifeBrain> _brains = new();
        private readonly List<HordeMember> _nearby = new();
        private Random _random;
        private RegionGrid _grid;
        private WildlifeRoster _roster;
        private int _cursor;

        public static RegionWildlife Current { get; private set; }

        int IRegionPopulate.PopulateOrder => RegionOrder.Wilds;

        /// <summary>Every living creature with a brain.</summary>
        public IReadOnlyList<WildlifeBrain> Brains => _brains;

        internal RegionGrid Grid => _grid;
        internal float CellSize => _grid != null ? _grid.CellSize : 1f;

        /// <summary>How far a creature sees now, in cells (night shortens it).</summary>
        public int SightCells => VisionRanges.Scaled(VisionRanges.Wildlife, RegionVision.RangeMultiplier);

        /// <summary>How many living creatures of a type there are.</summary>
        public int Count(WildlifeType type)
        {
            int n = 0;
            foreach (WildlifeBrain b in _brains)
                if (b.Type == type) n++;
            return n;
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
            _brains.Clear();
        }

        internal void Register(WildlifeBrain brain)
        {
            if (brain.Index >= 0) return;
            brain.Index = _brains.Count;
            _brains.Add(brain);
        }

        internal void Unregister(WildlifeBrain brain)
        {
            int i = brain.Index;
            if (i < 0) return;
            brain.Index = -1;
            int last = _brains.Count - 1;
            if (i != last)
            {
                _brains[i] = _brains[last];
                _brains[i].Index = i;
            }
            _brains.RemoveAt(last);
        }

        // ---- Spawning ----

        void IRegionPopulate.Populate(RegionContext region)
        {
            _grid = RegionGrid.Current;
            _random = region.Random("wildlife");
            _roster = Resources.Load<WildlifeRoster>(WildlifeRoster.ResourceName);
            RegionTerrain terrain = RegionTerrain.Current;
            if (_grid == null || terrain == null) return;
            foreach (Vector3 spot in terrain.WildlifeSpots)
            {
                WildlifeSpawns.Choice choice = WildlifeSpawns.Pick(_random.NextDouble());
                int size = WildlifeSpawns.GroupSize(choice, _random.NextDouble());
                for (int i = 0; i < size; i++) Spawn(choice.Type, GroupSpot(spot));
            }
        }

        // A walkable spot within a cell of the group's spot (the spot itself when none is).
        private Vector3 GroupSpot(Vector3 spot)
        {
            for (int i = 0; i < 6; i++)
            {
                var at = spot + new Vector3(RandomRange(-1f, 1f), RandomRange(-1f, 1f), 0f) * _grid.CellSize;
                if (_grid.IsWalkable(at)) return at;
            }
            return spot;
        }

        /// <summary>Puts a creature of <paramref name="type"/> at <paramref name="at"/>, which is its spot. Null when the roster has no prefab for it.</summary>
        public WildlifeBrain Spawn(WildlifeType type, Vector3 at)
        {
            GameObject prefab = _roster != null ? _roster.PrefabOf(type) : null;
            if (prefab == null || _grid == null) return null;
            GameObject go = Pools.Spawn(prefab, at, Quaternion.identity);
            if (!go.TryGetComponent(out WildlifeBrain brain)) { Pools.Despawn(go); return null; }
            brain.Assign(this, at);
            return brain;
        }

        // ---- The shared services ----

        internal float RandomRange(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        /// <summary>A walkable spot within <paramref name="radius"/> cells of <paramref name="around"/> (a few tries).</summary>
        internal bool TryPickNear(Vector3 around, int radius, out Vector3 spot)
        {
            Int2 centre = _grid.CellAt(around);
            for (int i = 0; i < 6; i++)
            {
                var cell = new Int2(centre.x + _random.Next(-radius, radius + 1), centre.y + _random.Next(-radius, radius + 1));
                if (!_grid.Contains(cell) || !_grid.IsWalkable(cell)) continue;
                spot = _grid.CentreOf(cell) + new Vector3(RandomRange(-0.3f, 0.3f), RandomRange(-0.3f, 0.3f), 0f);
                return true;
            }
            spot = default;
            return false;
        }

        /// <summary>
        /// What <paramref name="me"/> goes for. A hostile creature takes the nearest living thing its side is hostile to within
        /// <paramref name="sight"/> that its side sees (mortals, Jack's side and neutral creatures; Buildings are not hunted).
        /// A neutral one (<paramref name="onlyAttacker"/>) takes only what attacked it while it <paramref name="holdsGrudge"/>: that unit
        /// (<paramref name="grudge"/>) within twice its sight, or, when it never learned who, the nearest of Jack's side or the mortals.
        /// </summary>
        internal bool TryFindTarget(WildlifeBrain me, Vector3 from, float sight, bool onlyAttacker, bool holdsGrudge, CombatUnit grudge, out WildlifeTarget target)
        {
            target = default;
            if (onlyAttacker && !holdsGrudge) return false;
            Side side = me.Side;
            bool known = onlyAttacker && grudge != null;
            float range = known ? sight * 2f : sight;
            RegionVision vision = !known && RegionVision.IsOn ? RegionVision.Current : null;
            float best = float.MaxValue;

            foreach (HordeTarget t in HordeTarget.All)
            {
                if (!t.IsAlive || !Wanted(side, t.Team, t.Damageable.Unit, onlyAttacker, known, grudge)) continue;
                if (!known && t.TryGetComponent(out Building _)) continue;
                Consider(t.Damageable, t.Radius, from, range, vision, side, ref best, ref target);
            }

            HordeDirector director = HordeDirector.Current;
            if (director != null)
            {
                _nearby.Clear();
                director.Hash.Query(from.x, from.y, range + 2f, _nearby);
                foreach (HordeMember m in _nearby)
                {
                    if (m == null || !m.IsAlive || m.gameObject == me.gameObject || !Wanted(side, m.Unit.Team, m.Unit, onlyAttacker, known, grudge)) continue;
                    Consider(m.Damageable, 0.25f, from, range, vision, side, ref best, ref target);
                }
            }
            return best < float.MaxValue;
        }

        private static bool Wanted(Side side, string team, CombatUnit unit, bool onlyAttacker, bool known, CombatUnit grudge)
        {
            if (known) return unit == grudge;
            Side other = TeamIds.SideOf(team);
            if (onlyAttacker) return other == Side.Yours || other == Side.Mortals;
            return SideRelations.AreHostile(side, other);
        }

        private static void Consider(Damageable d, float radius, Vector3 from, float range, RegionVision vision, Side side, ref float best, ref WildlifeTarget target)
        {
            Vector3 p = d.transform.position;
            float dist = Mathf.Sqrt((p.x - from.x) * (p.x - from.x) + (p.y - from.y) * (p.y - from.y));
            if (dist - radius > range || dist >= best) return;
            if (vision != null && vision.IsReady && !vision.CanSee(side, p)) return;
            best = dist;
            target = new WildlifeTarget(d, radius, p, dist);
        }

        // ---- Running the brains ----

        private void Update()
        {
            if (_grid == null) return;
            float dt = Time.deltaTime;
            float now = Time.time;
            int n = _brains.Count;
            if (n > 0 && dt > 0f)
            {
                // Every creature thinks once per ThinkSeconds: this frame's share of them, starting where the last frame stopped.
                int share = Mathf.CeilToInt(n * dt / WildlifeTuning.ThinkSeconds);
                for (int i = 0; i < share && _brains.Count > 0; i++)
                {
                    if (_cursor >= _brains.Count) _cursor = 0;
                    _brains[_cursor++].Think(now, this);
                }
            }
            for (int i = _brains.Count - 1; i >= 0; i--) _brains[i].Step(dt, now, this);
        }
    }
}
