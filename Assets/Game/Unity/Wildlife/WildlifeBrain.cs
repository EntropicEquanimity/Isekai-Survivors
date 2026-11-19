using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The mind of one wildlife creature (a slime, a werewolf, a werebear). Like a mortal's, it has no Update: the Region's
    /// <see cref="RegionWildlife"/> asks it to <see cref="Think"/> a few times a second (staggered across frames) and to
    /// <see cref="Step"/> every frame. Thinking scores the shared <see cref="BehaviourAction"/>s with the type's
    /// <see cref="BehaviourProfile"/> (data in Rules/Wildlife) and does the best one:
    /// <list type="bullet">
    /// <item>Work: strolls to spots near where it spawned. Idle: stands. ReturnHome: walks back once it has strayed.</item>
    /// <item>Fight: walks straight at its target and hits it in reach. A hostile creature's target is the nearest thing
    /// it sees that its side is hostile to (mortals included); a neutral one's is only whatever attacked it, for a while.</item>
    /// <item>Flee: runs from the threat, straight away from it.</item>
    /// </list>
    /// Fighting does not use the horde's crowd or path field: wildlife stay near their spot and walk directly, so they cost the
    /// horde nothing. They are horde members only to be found and hit (<see cref="HordeMember"/>).
    /// </summary>
    [RequireComponent(typeof(HordeMember))]
    public sealed class WildlifeBrain : MonoBehaviour
    {
        private enum Move : byte { None, Direct }

        [SerializeField] private WildlifeType _type;

        private HordeMember _member;
        private Damageable _damageable;
        private SpriteRenderer _sprite;
        private WildlifeProfile _profile;
        private BehaviourContext _context;
        private RegionWildlife _world;
        private Vector3 _home;
        private Vector3 _goal;
        private Vector3 _threatAt;
        private Damageable _target;
        private float _targetRadius;
        private CombatUnit _grudge;
        private float _grudgeUntil;
        private Move _move;
        private float _speed;
        private float _wait;
        private float _stuck;
        private float _fear;
        private float _lastThink;
        private bool _assigned;

        internal int Index = -1;

        public WildlifeType Type => _type;
        public WildlifeProfile Profile => _profile ??= WildlifeProfiles.For(_type);
        public HordeMember Member => _member != null ? _member : _member = GetComponent<HordeMember>();

        /// <summary>The side it is on now (Neutral or Hostile, from its team).</summary>
        public Side Side => TeamIds.SideOf(Member.Unit.Team);

        /// <summary>What it is doing now.</summary>
        public BehaviourAction Action { get; private set; }

        /// <summary>The latest context this brain scored its actions with.</summary>
        public BehaviourContext DebugContext => _context;

        /// <summary>The spot it spawned at and returns to.</summary>
        public Vector3 Home => _home;

        /// <summary>It is going for something now (Fight).</summary>
        public bool HasTarget => _target != null && !_target.Unit.IsDead;

        /// <summary>It has been frightened lately (0 not at all, 1 just now).</summary>
        public float Fear => _fear;

        /// <summary>It remembers being attacked: a neutral creature fights only while this holds.</summary>
        public bool HoldsGrudge => Time.time < _grudgeUntil;

        /// <summary>Called by the Region right after spawning: where it belongs.</summary>
        internal void Assign(RegionWildlife world, Vector3 home)
        {
            _world = world;
            _home = home;
            _assigned = true;
            _lastThink = Time.time;
            _wait = world.RandomRange(0f, WildlifeTuning.PauseMax);
        }

        private void OnEnable()
        {
            _damageable = GetComponent<Damageable>();
            _sprite = GetComponent<SpriteRenderer>();
            _context = BehaviourContext.Calm;
            Action = BehaviourAction.Idle;
            _move = Move.None;
            _fear = 0f;
            _wait = 0f;
            _stuck = 0f;
            _assigned = false;
            _target = null;
            _grudge = null;
            _grudgeUntil = 0f;
            _damageable.Damaged += OnDamaged;
            RegionWildlife.Current?.Register(this);
        }

        private void OnDisable()
        {
            if (_damageable != null) _damageable.Damaged -= OnDamaged;
            RegionWildlife.Current?.Unregister(this);
            _world = null;
            _target = null;
            _grudge = null;
        }

        // Being hit frightens, and a neutral creature now knows who to fight (and where the danger is).
        private void OnDamaged(DamageResult result)
        {
            _fear = 1f;
            CombatUnit source = result.Info?.Source;
            if (source == null || source == _damageable.Unit) return;
            _grudge = source;
            _grudgeUntil = Time.time + WildlifeTuning.GrudgeSeconds;
            Float3 at = source.Position;
            _threatAt = new Vector3(at.x, at.y, 0f);
        }

        // ---- Thinking: a few times a second ----

        internal void Think(float now, RegionWildlife world)
        {
            if (!_assigned || !Member.IsAlive) return;
            float dt = Mathf.Min(now - _lastThink, 1f);
            _lastThink = now;
            Vector3 pos = transform.position;
            float cell = world.CellSize;
            float sight = world.SightCells * cell;

            bool onlyAttacker = Profile.OnlyFightsBack;
            if (onlyAttacker && !HoldsGrudge) _grudge = null;
            bool seen = world.TryFindTarget(this, pos, sight, onlyAttacker, HoldsGrudge, _grudge, out WildlifeTarget found);
            if (seen)
            {
                _target = found.Damageable;
                _targetRadius = found.Radius;
                _threatAt = found.Position;
            }
            else _target = null;
            _fear = Mathf.Max(0f, _fear - dt / WildlifeTuning.FearSeconds);

            _context.Health = _damageable.Unit.Normalized;
            _context.ThreatSeen = seen;
            _context.ThreatCloseness = seen ? Mathf.Clamp01(1f - found.Distance / Mathf.Max(0.01f, sight)) : 0f;
            _context.Fear = _fear;
            _context.DistanceHome = Distance(pos, _home) / cell;
            _context.Leash = Profile.LeashCells;

            BehaviourAction next = BehaviourScoring.Choose(Profile.Behaviour, _context, Action);
            if (next != Action) Begin(next);
            Carry(dt, pos, world);
        }

        // Switching to another action: forget the old walk.
        private void Begin(BehaviourAction next)
        {
            Action = next;
            _move = Move.None;
            _wait = 0f;
            _stuck = 0f;
        }

        // Carrying on with the action it is doing: pick the next spot, or set off after the target.
        private void Carry(float dt, Vector3 pos, RegionWildlife world)
        {
            switch (Action)
            {
                case BehaviourAction.Fight:
                    if (HasTarget) Go(Move.Direct, Profile.ChaseSpeed);
                    else _move = Move.None;
                    break;
                case BehaviourAction.Work:
                    if (_move == Move.None && (_wait -= dt) <= 0f && world.TryPickNear(_home, WildlifeTuning.WanderRadius, out _goal))
                        Go(Move.Direct, WildlifeTuning.WanderSpeed);
                    break;
                case BehaviourAction.Flee:
                    Vector2 away = new Vector2(pos.x - _threatAt.x, pos.y - _threatAt.y);
                    if (away.sqrMagnitude < 0.01f) away = Vector2.right;
                    _goal = pos + (Vector3)(away.normalized * 6f);
                    Go(Move.Direct, Profile.ChaseSpeed * WildlifeTuning.FleeShare);
                    break;
                case BehaviourAction.ReturnHome:
                    if (Distance(pos, _home) <= 1.2f * world.CellSize) { _move = Move.None; break; }
                    _goal = _home;
                    Go(Move.Direct, WildlifeTuning.WanderSpeed * 1.5f);
                    break;
            }
        }

        private void Go(Move move, float cellsPerSecond)
        {
            _move = move;
            _speed = cellsPerSecond * Member.SpeedMultiplier;
        }

        // ---- Moving and hitting: every frame, only for what is on the move ----

        internal void Step(float dt, float now, RegionWildlife world)
        {
            if (!_assigned || !Member.IsAlive) return;
            Vector3 p = transform.position;
            float cell = world.CellSize;
            if (Action == BehaviourAction.Fight && HasTarget)
            {
                Vector3 t = _target.transform.position;
                var to = new Vector2(t.x - p.x, t.y - p.y);
                float d = to.magnitude;
                float reach = _targetRadius + Member.Reach;
                if (d <= reach)
                {
                    _move = Move.None;
                    FaceToward(to.x);
                    if (Member.Attack.TryHit(now))
                        _target.Unit.TakeDamage(Member.Attack.Damage, DamageRules.PhysicalType, Member.Unit);
                    return;
                }
                _goal = t;
                _move = Move.Direct;
            }
            if (_move == Move.None) return;
            var dir = new Vector2(_goal.x - p.x, _goal.y - p.y);
            float len = dir.magnitude;
            if (Action != BehaviourAction.Fight && len <= WildlifeTuning.ArriveDistance * cell) { Arrived(world); return; }
            if (len < 0.001f) return;
            Advance(p, dir / len, _speed * cell * dt, dt, world);
        }

        // Moves along dir, sliding along a wall on one axis when the straight step is blocked; stuck for a while, it gives up the spot.
        private void Advance(Vector3 p, Vector2 dir, float length, float dt, RegionWildlife world)
        {
            RegionGrid grid = world.Grid;
            Vector3 next = p + new Vector3(dir.x * length, dir.y * length, 0f);
            if (!grid.IsWalkable(next))
            {
                var slideX = new Vector3(next.x, p.y, p.z);
                var slideY = new Vector3(p.x, next.y, p.z);
                bool xFirst = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                if (xFirst && grid.IsWalkable(slideX)) next = slideX;
                else if (grid.IsWalkable(slideY)) next = slideY;
                else if (grid.IsWalkable(slideX)) next = slideX;
                else
                {
                    _stuck += dt;
                    if (_stuck > WildlifeTuning.StuckSeconds)
                    {
                        _stuck = 0f;
                        _move = Move.None;
                        _wait = 0.2f;
                    }
                    return;
                }
            }
            _stuck = 0f;
            transform.position = next;
            FaceToward(dir.x);
        }

        private void Arrived(RegionWildlife world)
        {
            _move = Move.None;
            _stuck = 0f;
            _wait = world.RandomRange(WildlifeTuning.PauseMin, WildlifeTuning.PauseMax);
        }

        private void FaceToward(float dx)
        {
            if (_sprite != null && Mathf.Abs(dx) > 0.05f) _sprite.flipX = dx < 0f;
        }

        private static float Distance(Vector3 a, Vector3 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }
}
