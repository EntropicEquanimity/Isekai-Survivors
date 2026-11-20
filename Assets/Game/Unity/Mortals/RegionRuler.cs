using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Seat's ruler for this Region: one named mortal hero at the Seat, rolled as a Hero (2-3 Traits), tougher than
    /// ordinary guards, and willing to retreat when the Seat is crumbling or they are badly hurt. The ring sets how
    /// tough they are, and the Capital's ruler makes a last stand. A ruler that escaped an earlier Region (#24) stands at
    /// the Seat they fled to as an extra hero, with the Traits the player learned and nothing else remembered.
    /// </summary>
    [RegionSystem]
    public sealed class RegionRuler : MonoBehaviour, IRegionPopulate, IRegionEndListener
    {
        private const int EscapeEdgeMargin = 2;

        private static readonly string[] FirstNames =
        {
            "Aldric", "Brann", "Cedric", "Dorian", "Edric", "Gareth", "Hadrian", "Leoric",
        };

        private static readonly string[] Titles =
        {
            "the Stern", "the Iron", "the Valiant", "the Watchful", "the Bold", "the Resolute",
        };

        [SerializeField] private MortalType _primaryType = MortalType.Knight;
        [SerializeField] private MortalType _fallbackType = MortalType.Soldier;
        [SerializeField, Min(1f)] private float _healthMultiplier = 1.7f;
        [SerializeField, Min(0.1f)] private float _damageMultiplier = 1.35f;
        [SerializeField, Min(0.1f)] private float _attackIntervalShare = 0.8f;
        [SerializeField, Range(0.01f, 1f)] private float _keepEscapeHealth = 0.3f;
        [SerializeField, Range(0.01f, 1f)] private float _selfEscapeHealth = 0.4f;
        [SerializeField, Min(0.2f)] private float _escapeSpeedCells = 3.2f;

        private RegionGrid _grid;
        private RegionSettlements _settlements;
        private RegionMortals _mortals;
        private PlanetRun _run;
        private RegionInfo _region;
        private RingRecipe _ring = RingRecipe.Of(RegionRing.Outer);
        private readonly List<MortalBrain> _returned = new();
        private MortalBrain _ruler;
        private Damageable _rulerDamageable;
        private SpriteRenderer _sprite;
        private string _rulerName;
        private TraitId[] _traits = System.Array.Empty<TraitId>();
        private RegionInfo _escapedTo;
        private List<Int2> _escapePath;
        private int _escapeCursor;
        private float _stuck;
        private bool _escaping;
        private bool _escaped;
        private bool _slain;

        public static RegionRuler Current { get; private set; }

        int IRegionPopulate.PopulateOrder => RegionOrder.Patrols + 10;

        public MortalBrain Ruler => _ruler;

        /// <summary>The rulers who escaped an earlier Region and stand at this Seat as extra heroes (they do not flee again).</summary>
        public IReadOnlyList<MortalBrain> ReturnedHeroes => _returned;
        public string RulerName => _rulerName;
        public IReadOnlyList<TraitId> RulerTraits => _traits;
        public bool IsEscaping => _escaping;
        public bool HasEscaped => _escaped;
        public bool IsSlain => _slain;
        public RegionInfo EscapedTo => _escapedTo;

        /// <summary>Escape speed in cells per second (tests may raise it).</summary>
        public float EscapeSpeedCells
        {
            get => _escapeSpeedCells;
            set => _escapeSpeedCells = Mathf.Max(0.2f, value);
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
            UnhookRuler();
        }

        void IRegionPopulate.Populate(RegionContext region)
        {
            _grid = RegionGrid.Current;
            _settlements = RegionSettlements.Current;
            _mortals = RegionMortals.Current;
            _run = region.Run;
            _region = region.Region;
            _ring = region.Ring;
            if (_grid == null || _settlements == null || _mortals == null) return;
            RegionSettlements.Settlement seat = _settlements.Seat;
            if (seat == null || seat.Keep == null) return;

            Vector3 spawn = PickSpawn(seat);
            _ruler = _mortals.Spawn(_primaryType, seat, spawn) ?? _mortals.Spawn(_fallbackType, seat, spawn);
            if (_ruler == null) return;
            _rulerName = BuildName(region.Seed, _region);
            _ruler.gameObject.name = "Ruler " + _rulerName;
            _sprite = _ruler.GetComponent<SpriteRenderer>();
            MakeStronger(_ruler);
            HookRuler(_ruler);
            StartCoroutine(RollHeroTraits());
            SpawnReturnedHeroes(seat);
        }

        // Each ruler that fled to this Region is back at the Seat: the same name and Traits, a hero's usual strength for the ring.
        private void SpawnReturnedHeroes(RegionSettlements.Settlement seat)
        {
            if (_run == null || _region == null) return;
            int n = 0;
            foreach (EscapedRuler escaped in _run.EscapedRulersAt(_region))
            {
                Vector3 at = PickSpawn(seat) + new Vector3(0.7f * (++n), 0f, 0f);
                if (!_grid.IsWalkable(at)) at = PickSpawn(seat);
                MortalBrain hero = _mortals.Spawn(_primaryType, seat, at) ?? _mortals.Spawn(_fallbackType, seat, at);
                if (hero == null) continue;
                hero.gameObject.name = "Ruler " + escaped.Name + " (returned)";
                MakeStronger(hero);
                _returned.Add(hero);
                StartCoroutine(GiveTraits(hero, escaped.Traits));
            }
        }

        private IEnumerator GiveTraits(MortalBrain hero, TraitId[] traits)
        {
            yield return null;
            for (int i = 0; i < 30 && hero != null; i++)
            {
                TraitSystem system = TraitSystem.Current;
                if (system != null && system.Give(hero.gameObject, traits)) yield break;
                yield return null;
            }
        }

        private void Update()
        {
            if (_ruler == null || _escaped || _slain) return;
            if (!_escaping)
            {
                if (ShouldEscape()) BeginEscape();
                return;
            }

            EscapeStep(Time.deltaTime);
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end == RegionEnd.Captured && !_escaped) _slain = true;
        }

        private bool ShouldEscape()
        {
            if (_ruler == null || _rulerDamageable == null || !_ruler.Member.IsAlive) return false;
            if (_region != null && _region.IsCapital) return false;   // nowhere further in to flee to: a last stand
            Building keep = _settlements.Seat?.Keep;
            bool keepAboutToFall = keep != null && keep.IsAlive && keep.Damageable.Normalized <= _keepEscapeHealth;
            bool badlyHurt = _rulerDamageable.Normalized <= _selfEscapeHealth;
            return keepAboutToFall || badlyHurt;
        }

        private void BeginEscape()
        {
            if (_escaping || _escaped || _ruler == null) return;
            if (!PlanEscapePath(out _escapePath) || _escapePath.Count < 2) return;
            _escapeCursor = 1;
            _stuck = 0f;
            _escaping = true;
            _ruler.enabled = false;
            _ruler.Member.SetRoaming(true);
        }

        private void EscapeStep(float dt)
        {
            if (_escapePath == null || _escapeCursor >= _escapePath.Count || _ruler == null) { EscapeNow(); return; }
            Vector3 at = _ruler.transform.position;
            Vector3 target = _grid.CentreOf(_escapePath[_escapeCursor]);
            float arrive = 0.15f * _grid.CellSize;
            Vector2 to = new(target.x - at.x, target.y - at.y);
            if (to.sqrMagnitude <= arrive * arrive)
            {
                _escapeCursor++;
                if (_escapeCursor >= _escapePath.Count) { EscapeNow(); return; }
                target = _grid.CentreOf(_escapePath[_escapeCursor]);
                to = new Vector2(target.x - at.x, target.y - at.y);
            }

            if (to.sqrMagnitude <= 0.0001f) return;
            Vector2 dir = to.normalized;
            float step = _escapeSpeedCells * _grid.CellSize * Mathf.Max(0f, dt);
            Vector3 next = at + new Vector3(dir.x * step, dir.y * step, 0f);

            if (!_grid.IsWalkable(next))
            {
                var slideX = new Vector3(next.x, at.y, at.z);
                var slideY = new Vector3(at.x, next.y, at.z);
                bool xFirst = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                if (xFirst && _grid.IsWalkable(slideX)) next = slideX;
                else if (_grid.IsWalkable(slideY)) next = slideY;
                else if (_grid.IsWalkable(slideX)) next = slideX;
                else
                {
                    _stuck += dt;
                    if (_stuck > 0.75f && PlanEscapePath(out _escapePath))
                    {
                        _escapeCursor = 1;
                        _stuck = 0f;
                    }
                    return;
                }
            }

            _stuck = 0f;
            _ruler.transform.position = next;
            if (_sprite != null && Mathf.Abs(dir.x) > 0.05f) _sprite.flipX = dir.x < 0f;
        }

        private void EscapeNow()
        {
            if (_escaped || _slain || _run == null || _region == null || _ruler == null) return;
            _escaping = false;
            _escaped = true;
            CaptureTraits();
            _escapedTo = _run.PickRetreatTarget(_region);
            if (_escapedTo != null) _run.RecordEscapedRuler(_region, _escapedTo, _rulerName, _traits);

            MortalBrain ruler = _ruler;
            UnhookRuler();
            _ruler = null;
            Pools.Despawn(ruler.gameObject);
        }

        private void MakeStronger(MortalBrain ruler)
        {
            HordeMember member = ruler.Member;
            member.ScaleHealth(_healthMultiplier * _ring.RulerHealth);
            ContactAttack attack = member.Attack;
            attack.Damage *= _damageMultiplier * _ring.RulerDamage;
            attack.Interval = Mathf.Max(0.2f, attack.Interval * _attackIntervalShare);
            member.Attack = attack;
        }

        private void HookRuler(MortalBrain ruler)
        {
            _rulerDamageable = ruler != null ? ruler.GetComponent<Damageable>() : null;
            if (_rulerDamageable != null) _rulerDamageable.Died += OnRulerDied;
        }

        private void UnhookRuler()
        {
            if (_rulerDamageable != null) _rulerDamageable.Died -= OnRulerDied;
            _rulerDamageable = null;
        }

        private void OnRulerDied(DamageResult _)
        {
            _slain = true;
            _escaping = false;
            UnhookRuler();
            _ruler = null;
        }

        private IEnumerator RollHeroTraits()
        {
            yield return null;
            for (int i = 0; i < 30 && _ruler != null; i++)
            {
                TraitSystem traits = TraitSystem.Current;
                if (traits != null)
                {
                    IReadOnlyList<TraitId> rolled = traits.Roll(_ruler.gameObject, TraitRank.Hero);
                    if (rolled.Count >= TraitTuning.HeroMin)
                    {
                        _traits = rolled.ToArray();
                        yield break;
                    }
                }
                yield return null;
            }
            CaptureTraits();
        }

        private void CaptureTraits()
        {
            UnitTraits own = UnitTraits.Of(_ruler != null ? _ruler.gameObject : null);
            if (own != null && own.Traits.Count > 0) _traits = own.Traits.ToArray();
        }

        private bool PlanEscapePath(out List<Int2> path)
        {
            path = null;
            if (_grid == null || _ruler == null) return false;
            Int2 start = _grid.CellAt(_ruler.transform.position);
            if (!_grid.Contains(start) || !_grid.IsWalkable(start)) return false;

            int w = _grid.Width, h = _grid.Height;
            int size = w * h;
            var prev = new int[size];
            for (int i = 0; i < prev.Length; i++) prev[i] = -1;
            var queue = new Queue<int>(size / 4);
            var edge = new List<int>(128);

            int startIndex = Idx(start.x, start.y, w);
            prev[startIndex] = startIndex;
            queue.Enqueue(startIndex);
            while (queue.Count > 0)
            {
                int at = queue.Dequeue();
                Int2 cell = Cell(at, w);
                if (IsEscapeEdge(cell, w, h)) edge.Add(at);
                Visit(cell.x + 1, cell.y, at);
                Visit(cell.x - 1, cell.y, at);
                Visit(cell.x, cell.y + 1, at);
                Visit(cell.x, cell.y - 1, at);
            }

            if (edge.Count == 0) return false;
            Vector2[] yours = Yours();
            int best = edge[0];
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < edge.Count; i++)
            {
                Vector3 world = _grid.CentreOf(Cell(edge[i], w));
                float score = Score(world, yours, _ruler.transform.position);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = edge[i];
                }
            }

            path = new List<Int2>();
            int cursor = best;
            while (cursor != startIndex)
            {
                path.Add(Cell(cursor, w));
                cursor = prev[cursor];
                if (cursor < 0) return false;
            }
            path.Add(start);
            path.Reverse();
            return path.Count > 1;

            void Visit(int x, int y, int from)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                int idx = Idx(x, y, w);
                if (prev[idx] >= 0) return;
                var cell = new Int2(x, y);
                if (!_grid.IsWalkable(cell)) return;
                prev[idx] = from;
                queue.Enqueue(idx);
            }
        }

        private static float Score(Vector3 cellWorld, IReadOnlyList<Vector2> yours, Vector3 fallbackAwayFrom)
        {
            if (yours.Count == 0)
            {
                float dx = cellWorld.x - fallbackAwayFrom.x;
                float dy = cellWorld.y - fallbackAwayFrom.y;
                return dx * dx + dy * dy;
            }

            float nearest = float.MaxValue;
            for (int i = 0; i < yours.Count; i++)
            {
                float dx = yours[i].x - cellWorld.x;
                float dy = yours[i].y - cellWorld.y;
                float d = dx * dx + dy * dy;
                if (d < nearest) nearest = d;
            }
            return nearest;
        }

        private static int Idx(int x, int y, int width) => y * width + x;
        private static Int2 Cell(int index, int width) => new(index % width, index / width);
        private static bool IsEscapeEdge(Int2 cell, int width, int height) =>
            cell.x <= EscapeEdgeMargin || cell.y <= EscapeEdgeMargin || cell.x >= width - 1 - EscapeEdgeMargin || cell.y >= height - 1 - EscapeEdgeMargin;

        private static string BuildName(int seed, RegionInfo region)
        {
            var random = new System.Random(StableHash.Seed(seed, "ruler/" + (region?.Id ?? "none")));
            string first = FirstNames[random.Next(FirstNames.Length)];
            string title = Titles[random.Next(Titles.Length)];
            return first + " " + title;
        }

        private static Vector2[] Yours()
        {
            var list = new List<Vector2>();
            foreach (RegionEntity entity in RegionEntities.Living)
            {
                if (entity.Side != Side.Yours) continue;
                Vector3 p = entity.GameObject.transform.position;
                list.Add(new Vector2(p.x, p.y));
            }
            if (list.Count == 0 && Player.Character != null)
            {
                Vector3 p = Player.Character.transform.position;
                list.Add(new Vector2(p.x, p.y));
            }
            return list.ToArray();
        }

        private Vector3 PickSpawn(RegionSettlements.Settlement seat)
        {
            Vector3 keep = seat.Keep != null ? seat.Keep.transform.position : transform.position;
            Vector3 best = keep;
            float bestDistance = float.MaxValue;
            foreach (RegionSettlements.SpawnPoint marker in seat.Markers)
            {
                if (marker.Kind != MarkerKind.Guard || !_grid.IsWalkable(marker.Position)) continue;
                float distance = (marker.Position - keep).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = marker.Position;
            }
            if (_grid.IsWalkable(best)) return best;

            Int2 centre = _grid.CellAt(keep);
            for (int r = 1; r < 10; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var cell = new Int2(centre.x + dx, centre.y + dy);
                        if (_grid.Contains(cell) && _grid.IsWalkable(cell)) return _grid.CentreOf(cell);
                    }
            return keep;
        }
    }
}
