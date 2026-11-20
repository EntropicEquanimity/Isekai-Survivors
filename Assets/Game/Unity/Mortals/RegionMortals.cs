using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// A Region's mortals with a life of their own: it fills every settlement from its spawn markers
    /// (<see cref="MortalSpawns"/>), then runs all their brains from one place: each brain thinks a few times a second
    /// (staggered, so every frame does a slice of them), and the mortals walking by themselves step every frame. It
    /// also owns what they share: what they see (<see cref="MortalSenses"/>), the static fields they run along
    /// (<see cref="MortalHomes"/>), and the priests' effect on Jack's drain.
    /// <para>The hooks other tickets use are properties and methods here: <see cref="Night"/> (night, #15),
    /// <see cref="PatrolStrength"/> and <see cref="AddPatrols"/> (Vigilance, #18), <see cref="RaiseAlarm"/> (alarms, #17)
    /// and <see cref="MortalBrain.SetBehaviour"/> (traits, #20).</para>
    /// <para>Fighting mortals are ordinary horde members (<see cref="HordeMember"/>): they join the crowd and walk its one
    /// path field. Nothing here asks for a path field because something moved.</para>
    /// </summary>
    [RegionSystem]
    public sealed class RegionMortals : MonoBehaviour, IRegionPopulate
    {
        private readonly List<MortalBrain> _brains = new();
        private readonly Dictionary<int, List<Int2>> _patrolCells = new();
        private readonly MortalSenses _senses = new();
        private Random _random;
        private RegionGrid _grid;
        private MortalRoster _roster;
        private MortalHomes _homes;
        private RegionSettlements _settlements;
        private int _cursor;
        private float _nextRelease;
        private RingRecipe _ring = RingRecipe.Of(RegionRing.Outer);

        public static RegionMortals Current { get; private set; }

        int IRegionPopulate.PopulateOrder => RegionOrder.Patrols;

        /// <summary>It is night: guards' weights can pull them back to their settlements (the day-and-night ticket sets this).</summary>
        public bool Night { get; set; }

        /// <summary>What a new guard's health is multiplied by: how strong the patrols are (Vigilance sets this).</summary>
        public float PatrolStrength { get; set; } = 1f;

        /// <summary>What a new guard's health is multiplied by in this Region's ring, on top of <see cref="PatrolStrength"/>; the Seat's guards, the elite, also get the ring's elite share.</summary>
        public float GuardStrength(RegionSettlements.Settlement settlement) =>
            _ring.TroopStrength * (settlement != null && settlement.IsSeat ? _ring.EliteGuardStrength : 1f);

        /// <summary>Seconds between one priest's purifications (a test shortens it).</summary>
        public float PurifySeconds { get; set; } = CorruptionTuning.PurifySeconds;

        /// <summary>Every living mortal with a brain.</summary>
        public IReadOnlyList<MortalBrain> Brains => _brains;

        internal MortalSenses Senses => _senses;
        internal MortalHomes Homes => _homes;
        internal RegionGrid Grid => _grid;
        internal float CellSize => _grid != null ? _grid.CellSize : 1f;

        /// <summary>How many fields the mortals hold now (static ones, one per settlement they have run to or from).</summary>
        public int HeldFields => _homes != null ? _homes.Held : 0;

        /// <summary>How far a mortal sees now, in cells (night shortens it).</summary>
        public int SightCells => VisionRanges.Scaled(VisionRanges.Mortal, RegionVision.RangeMultiplier);

        /// <summary>How many living mortals of a type there are.</summary>
        public int Count(MortalType type)
        {
            int n = 0;
            foreach (MortalBrain b in _brains)
                if (b.Type == type) n++;
            return n;
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
            _homes?.Dispose();
            _homes = null;
            _brains.Clear();
        }

        internal void Register(MortalBrain brain)
        {
            if (brain.Index >= 0) return;
            brain.Index = _brains.Count;
            _brains.Add(brain);
        }

        internal void Unregister(MortalBrain brain)
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
            _settlements = RegionSettlements.Current;
            _random = region.Random("mortals");
            _ring = region.Ring;
            _roster = Resources.Load<MortalRoster>(MortalRoster.ResourceName);
            if (_grid == null) return;
            IReadOnlyList<RegionSettlements.Settlement> all = _settlements != null ? _settlements.Settlements : Array.Empty<RegionSettlements.Settlement>();
            _homes = new MortalHomes(_grid, all);
            foreach (RegionSettlements.Settlement s in all)
                foreach (RegionSettlements.SpawnPoint m in s.Markers)
                    if (MortalSpawns.TryPick(s.Kind, m.Kind, _random.NextDouble(), out MortalType type))
                        Spawn(type, s, m.Position);
        }

        /// <summary>Puts a mortal of <paramref name="type"/> at <paramref name="at"/>, belonging to <paramref name="settlement"/> (its home). Null when the roster has no prefab for it.</summary>
        public MortalBrain Spawn(MortalType type, RegionSettlements.Settlement settlement, Vector3 at)
        {
            GameObject prefab = _roster != null ? _roster.PrefabOf(type) : null;
            if (prefab == null || _grid == null) return null;
            GameObject go = Pools.Spawn(prefab, at, Quaternion.identity);
            if (!go.TryGetComponent(out MortalBrain brain)) { Pools.Despawn(go); return null; }
            if (brain.Profile.Role == MortalRole.Guard)
            {
                float strength = PatrolStrength * GuardStrength(settlement);
                if (!Mathf.Approximately(strength, 1f)) brain.Member.ScaleHealth(strength);
            }
            brain.Assign(this, settlement, at);
            return brain;
        }

        /// <summary>
        /// More guards for a settlement: the patrol count Vigilance raises (#18). They appear at its guard markers, of
        /// the types its kind of settlement has, and are as strong as <see cref="PatrolStrength"/> says.
        /// </summary>
        public int AddPatrols(RegionSettlements.Settlement settlement, int count)
        {
            int added = 0;
            var markers = new List<RegionSettlements.SpawnPoint>();
            foreach (RegionSettlements.SpawnPoint m in settlement.Markers)
                if (m.Kind == MarkerKind.Guard) markers.Add(m);
            if (markers.Count == 0) return 0;
            for (int i = 0; i < count; i++)
            {
                RegionSettlements.SpawnPoint m = markers[_random.Next(markers.Count)];
                if (!MortalSpawns.TryPick(settlement.Kind, MarkerKind.Guard, _random.NextDouble(), out MortalType type)) break;
                if (Spawn(type, settlement, m.Position) != null) added++;
            }
            return added;
        }

        /// <summary>Spawns <paramref name="count"/> more mortals across the settlements, each at one of its markers (the debug menu and the perf test).</summary>
        public int SpawnExtra(int count)
        {
            if (_settlements == null || _settlements.Settlements.Count == 0) return 0;
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                RegionSettlements.Settlement s = _settlements.Settlements[i % _settlements.Settlements.Count];
                if (s.Markers.Count == 0) continue;
                RegionSettlements.SpawnPoint m = s.Markers[_random.Next(s.Markers.Count)];
                if (!MortalSpawns.TryPick(s.Kind, m.Kind, _random.NextDouble(), out MortalType type)) continue;
                if (Spawn(type, s, m.Position) != null) added++;
            }
            return added;
        }

        /// <summary>A mortal hears or sees an alarm at <paramref name="point"/>: every brain within <paramref name="radius"/> (cells) has the Alarm factor on for <paramref name="seconds"/>. The alarm ticket (#17) raises it.</summary>
        public void RaiseAlarm(Vector3 point, float radius, float seconds)
        {
            float r = radius * CellSize;
            foreach (MortalBrain b in _brains)
            {
                Vector3 p = b.transform.position;
                if ((p.x - point.x) * (p.x - point.x) + (p.y - point.y) * (p.y - point.y) <= r * r) b.NoteAlarm(seconds);
            }
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

        /// <summary>A waypoint on the settlement's ground or its roads within <paramref name="leash"/> cells of <paramref name="home"/>.</summary>
        internal bool TryPickPatrol(RegionSettlements.Settlement settlement, Vector3 home, float leash, out Vector3 spot)
        {
            if (settlement == null) return TryPickNear(home, 6, out spot);
            List<Int2> cells = PatrolCellsOf(settlement);
            Int2 homeCell = _grid.CellAt(home);
            for (int i = 0; i < 8 && cells.Count > 0; i++)
            {
                Int2 cell = cells[_random.Next(cells.Count)];
                int dx = cell.x - homeCell.x, dy = cell.y - homeCell.y;
                if (dx * dx + dy * dy > leash * leash || !_grid.IsWalkable(cell)) continue;
                spot = _grid.CentreOf(cell);
                return true;
            }
            return TryPickNear(home, 6, out spot);
        }

        // The settlement's ground patch plus the stretch of each road that leaves it.
        private List<Int2> PatrolCellsOf(RegionSettlements.Settlement settlement)
        {
            int key = settlement.Plan.Index;
            if (_patrolCells.TryGetValue(key, out List<Int2> cached)) return cached;
            var cells = new List<Int2>();
            foreach (KeyValuePair<Int2, GroundTile> tile in settlement.Plan.Tiles) cells.Add(tile.Key);
            RegionLayout layout = RegionTerrain.Current != null ? RegionTerrain.Current.Layout : null;
            if (layout != null)
                foreach (Road road in layout.Settlements.Roads)
                {
                    bool from = road.From.Index == key;
                    if (!from && road.To.Index != key) continue;
                    int n = Mathf.Min(MortalTuning.PatrolRoadCells, road.Cells.Count);
                    for (int i = 0; i < n; i++) cells.Add(road.Cells[from ? i : road.Cells.Count - 1 - i]);
                }
            _patrolCells[key] = cells;
            return cells;
        }

        /// <summary>The nearest corrupted cell around a priest that is still within its leash of home (nearest first, higher level breaks ties).</summary>
        internal bool FindCorruption(Vector3 around, Vector3 home, float leash, out Int2 found)
        {
            found = default;
            RegionCorruption corruption = RegionCorruption.Current;
            if (corruption == null) return false;
            Int2 centre = _grid.CellAt(around);
            Int2 homeCell = _grid.CellAt(home);
            int r = MortalTuning.PurifyScanRadius;
            int bestScore = int.MinValue;
            bool any = false;
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                var cell = new Int2(centre.x + dx, centre.y + dy);
                if (!_grid.Contains(cell)) continue;
                int level = corruption.Level(cell);
                if (level <= 0) continue;
                int hx = cell.x - homeCell.x, hy = cell.y - homeCell.y;
                if (hx * hx + hy * hy > leash * leash) continue;
                int score = level - (dx * dx + dy * dy) * 10;
                if (score <= bestScore) continue;
                bestScore = score;
                found = cell;
                any = true;
            }
            return any;
        }

        internal bool Purify(Int2 cell) => RegionCorruption.Current != null && RegionCorruption.Current.Purify(cell);

        internal void ShowArrow(Vector3 from, Vector3 to)
        {
            GameObject prefab = _roster != null ? _roster.Arrow : null;
            if (prefab == null) return;
            GameObject go = Pools.Spawn(prefab, from, Quaternion.identity);
            if (!go.TryGetComponent(out ArrowShot shot)) shot = go.AddComponent<ArrowShot>();
            shot.Launch(from, to);
        }

        // ---- Running the brains ----

        private void Update()
        {
            if (_grid == null || _homes == null) return;
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            float dt = Time.deltaTime;
            float now = Time.time;
            _senses.Tick(now);

            int n = _brains.Count;
            if (n > 0 && dt > 0f)
            {
                // Every brain thinks once per ThinkSeconds: this frame's share of them, starting where the last frame stopped.
                int share = Mathf.CeilToInt(n * dt / MortalTuning.ThinkSeconds);
                for (int i = 0; i < share && _brains.Count > 0; i++)
                {
                    if (_cursor >= _brains.Count) _cursor = 0;
                    _brains[_cursor++].Think(now, this);
                }
            }

            for (int i = _brains.Count - 1; i >= 0; i--) _brains[i].Step(dt, now, this);

            if (now >= _nextRelease)
            {
                _nextRelease = now + 5f;
                _homes.ReleaseIdle(now);
            }
            MortalCost.Add(System.Diagnostics.Stopwatch.GetTimestamp() - start);
        }

        /// <summary>A living priest is within its aura of <paramref name="world"/> (<see cref="RegionSouls"/> doubles the wisp's drain there).</summary>
        public bool PriestNear(Vector3 world)
        {
            float reach = MortalTuning.PriestAuraCells * CellSize;
            for (int i = 0; i < _brains.Count; i++)
            {
                MortalBrain b = _brains[i];
                if (b.Type != MortalType.Priest || !b.Member.IsAlive) continue;
                Vector3 p = b.transform.position;
                if ((p.x - world.x) * (p.x - world.x) + (p.y - world.y) * (p.y - world.y) <= reach * reach) return true;
            }
            return false;
        }

        /// <summary>Holy ground is the Seat's cells.</summary>
        public bool IsHolyGround(Vector3 world)
        {
            RegionSettlements.Settlement seat = _settlements != null ? _settlements.Seat : null;
            return seat != null && !seat.IsRazed && seat.Plan.Interior.Contains(_grid.CellAt(world));
        }
    }
}
