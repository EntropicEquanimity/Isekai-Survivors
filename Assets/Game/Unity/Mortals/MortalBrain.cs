using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The mind of one mortal with a life of its own (a peasant, a guard, a priest). It has no Update: the Region's
    /// <see cref="RegionMortals"/> asks it to <see cref="Think"/> a few times a second (staggered across frames) and to
    /// <see cref="Step"/> its walking every frame. Thinking scores the shared <see cref="BehaviourAction"/>s with the
    /// type's <see cref="BehaviourProfile"/> and does the best one:
    /// <list type="bullet">
    /// <item>Fight: joins the horde's crowd (its <see cref="HordeMember"/> wakes) and goes for its target along the
    /// one shared path field; an archer holds still once the target is in reach. When nothing is in sight it drops
    /// back out of the crowd and chooses again.</item>
    /// <item>Work, Patrol, Purify: short walks straight at a nearby spot, sliding along walls. No path fields.</item>
    /// <item>Flee, ReturnHome: along a settlement's own static field (<see cref="MortalHomes"/>).</item>
    /// </list>
    /// A trait changes behaviour by giving the brain another profile (<see cref="SetBehaviour"/>).
    /// </summary>
    [RequireComponent(typeof(HordeMember))]
    public sealed class MortalBrain : MonoBehaviour
    {
        private enum Move : byte { None, Direct, Field }

        [SerializeField] private MortalType _type;

        private HordeMember _member;
        private Damageable _damageable;
        private SpriteRenderer _sprite;
        private MortalProfile _profile;
        private BehaviourProfile _behaviour;
        private BehaviourContext _context;
        private RegionMortals _world;
        private RegionSettlements.Settlement _settlement;
        private RegionSettlements.Settlement _fieldTarget;
        private Vector3 _home;
        private Vector3 _goal;
        private Vector3 _threatAt;
        private Move _move;
        private float _speed;
        private float _wait;
        private float _stuck;
        private float _fear;
        private float _lastThink;
        private float _alarmUntil;
        private float _purifyReadyAt;
        private float _seenAttack;
        private float _threatRadius;
        private bool _assigned;
        private bool _hasPurifyCell;
        private bool _fled;
        private Int2 _purifyCell;

        internal int Index = -1;

        public MortalType Type => _type;
        public MortalProfile Profile => _profile ??= MortalProfiles.For(_type);
        public HordeMember Member => _member != null ? _member : _member = GetComponent<HordeMember>();

        /// <summary>What it is doing now.</summary>
        public BehaviourAction Action { get; private set; }

        /// <summary>The weights it decides with now (the type's, or a trait's copy).</summary>
        public BehaviourProfile Behaviour => _behaviour ??= Profile.Behaviour;

        /// <summary>The settlement it belongs to and the spot it starts at and returns to.</summary>
        public RegionSettlements.Settlement Settlement => _settlement;
        public Vector3 Home => _home;

        /// <summary>It is in the horde's crowd, going for its target.</summary>
        public bool IsFighting => Member != null && !Member.IsRoaming;

        /// <summary>It has been frightened lately (0 not at all, 1 just now).</summary>
        public float Fear => _fear;

        /// <summary>Wants to be walking somewhere on its own (not standing, not in the crowd).</summary>
        public bool IsMoving => _move != Move.None;

        /// <summary>Corruption levels this priest has removed.</summary>
        public int Purified { get; private set; }

        /// <summary>Changes how it decides: a trait's copy of the type's profile (see <see cref="BehaviourProfile.With"/>).</summary>
        public void SetBehaviour(BehaviourProfile profile) => _behaviour = profile ?? Profile.Behaviour;

        /// <summary>It has seen or heard an alarm: for <paramref name="seconds"/> the Alarm factor is on. Raised by the alarm system.</summary>
        public void NoteAlarm(float seconds) => _alarmUntil = Mathf.Max(_alarmUntil, Time.time + seconds);

        /// <summary>Called by the Region right after spawning: where it belongs.</summary>
        internal void Assign(RegionMortals world, RegionSettlements.Settlement settlement, Vector3 home)
        {
            _world = world;
            _settlement = settlement;
            _home = home;
            _assigned = true;
            _lastThink = Time.time;
            _purifyReadyAt = Time.time + world.RandomRange(0f, world.PurifySeconds);
            _wait = world.RandomRange(0f, MortalTuning.WorkPauseMax);
        }

        private void OnEnable()
        {
            _damageable = GetComponent<Damageable>();
            _sprite = GetComponent<SpriteRenderer>();
            _behaviour = Profile.Behaviour;
            _context = BehaviourContext.Calm;
            Action = BehaviourAction.Idle;
            _move = Move.None;
            _fear = 0f;
            _alarmUntil = 0f;
            _seenAttack = 0f;
            _wait = 0f;
            _stuck = 0f;
            _assigned = false;
            _hasPurifyCell = false;
            _fled = false;
            _fieldTarget = null;
            Purified = 0;
            _damageable.Damaged += OnDamaged;
            RegionMortals.Current?.Register(this);
        }

        private void OnDisable()
        {
            if (_damageable != null) _damageable.Damaged -= OnDamaged;
            RegionMortals.Current?.Unregister(this);
            _world = null;
        }

        // Being hit frightens. Who hit is not known here: a threat in sight tells the brain where from.
        private void OnDamaged(DamageResult result) => _fear = 1f;

        // ---- Thinking: a few times a second ----

        internal void Think(float now, RegionMortals world)
        {
            if (!_assigned || !Member.IsAlive) return;
            float dt = Mathf.Min(now - _lastThink, 1f);
            _lastThink = now;
            Vector3 pos = transform.position;
            float cell = world.CellSize;

            bool seen = world.Senses.TryNearest(pos, world.SightCells * cell, out HordeTarget threat, out float distance);
            if (seen)
            {
                _fear = 1f;
                _threatAt = threat.transform.position;
                _threatRadius = threat.Radius;
            }
            else _fear = Mathf.Max(0f, _fear - dt / MortalTuning.FearSeconds);

            bool purifier = Behaviour.Of(BehaviourAction.Purify).Enabled;
            _hasPurifyCell = purifier && world.FindCorruption(pos, _home, Profile.LeashCells, out _purifyCell);

            _context.Health = _damageable.Unit.Normalized;
            _context.ThreatSeen = seen;
            _context.ThreatCloseness = seen ? Mathf.Clamp01(1f - distance / Mathf.Max(0.01f, world.SightCells * cell)) : 0f;
            _context.Fear = _fear;
            _context.DistanceHome = Distance(pos, _home) / cell;
            _context.Leash = Profile.LeashCells;
            _context.Night = world.Night;
            _context.CorruptionNear = _hasPurifyCell;
            _context.AlarmSeen = now < _alarmUntil;

            BehaviourAction next = BehaviourScoring.Choose(Behaviour, _context, Action);
            if (next != Action) Begin(next);
            if (_fled) SpreadAlarm(pos, world);
            Carry(now, dt, pos, seen, distance, world);
        }

        // A peasant that has fled brings the alarm with it when it reaches a stronghold (even if its fear ran out on the way).
        private void SpreadAlarm(Vector3 pos, RegionMortals world)
        {
            RegionSettlements.Settlement stronghold = world.Homes.NearestStronghold(pos);
            if (stronghold == null || !MortalHomes.IsAt(stronghold, world.Grid.CellAt(pos))) return;
            _fled = false;
            RegionAlarms.Current?.AlertStronghold(stronghold);
        }

        // Switching to another action: into or out of the crowd, and forget the old walk.
        private void Begin(BehaviourAction next)
        {
            bool wasFight = Action == BehaviourAction.Fight;
            Action = next;
            if (next == BehaviourAction.Flee && Profile.Role == MortalRole.Civilian) _fled = true;
            else if (next == BehaviourAction.Work || next == BehaviourAction.Patrol) _fled = false;
            _move = Move.None;
            _wait = 0f;
            _stuck = 0f;
            if (wasFight && next != BehaviourAction.Fight) Member.SetRoaming(true);
        }

        // Carrying on with the action it is doing: pick the next spot, hold still, or act.
        private void Carry(float now, float dt, Vector3 pos, bool seen, float distance, RegionMortals world)
        {
            switch (Action)
            {
                case BehaviourAction.Fight:
                    CarryFight(seen, distance);
                    break;
                case BehaviourAction.Work:
                    if (_move == Move.None && (_wait -= dt) <= 0f && world.TryPickNear(_home, MortalTuning.WorkRadius, out _goal))
                        Go(Move.Direct, MortalTuning.WalkSpeed);
                    break;
                case BehaviourAction.Patrol:
                    if (_move == Move.None && (_wait -= dt) <= 0f && world.TryPickPatrol(_settlement, _home, Profile.LeashCells, out _goal))
                        Go(Move.Direct, MortalTuning.WalkSpeed);
                    break;
                case BehaviourAction.Flee:
                    CarryFlee(pos, world);
                    break;
                case BehaviourAction.ReturnHome:
                    CarryReturn(pos, world);
                    break;
                case BehaviourAction.Purify:
                    CarryPurify(now, pos, world);
                    break;
            }
        }

        private void CarryFight(bool seen, float distance)
        {
            // Archers stand and shoot once the target is in reach; everyone else walks the crowd's field up to it.
            bool hold = Profile.Ranged && seen && distance <= (Member.Reach + _threatRadius) * MortalTuning.RangedHoldShare;
            if (hold == Member.IsRoaming) return;
            Member.SetRoaming(hold);
            if (hold) FaceToward(_threatAt.x - transform.position.x);
        }

        private void CarryFlee(Vector3 pos, RegionMortals world)
        {
            RegionSettlements.Settlement to = world.Homes.NearestStronghold(pos);
            _fieldTarget = to;
            if (to != null && MortalHomes.IsAt(to, world.Grid.CellAt(pos))) { _move = Move.None; return; }   // safe at the stronghold
            if (to != null) { Go(Move.Field, MortalTuning.FleeSpeed); return; }
            // No stronghold stands: run from whatever frightened it.
            Vector2 away = new Vector2(pos.x - _threatAt.x, pos.y - _threatAt.y);
            if (away.sqrMagnitude < 0.01f) away = Vector2.right;
            _goal = pos + (Vector3)(away.normalized * 6f);
            Go(Move.Direct, MortalTuning.FleeSpeed);
        }

        private void CarryReturn(Vector3 pos, RegionMortals world)
        {
            float away = Distance(pos, _home);
            if (away <= 1.2f * world.CellSize) { _move = Move.None; return; }
            if (away > 6f * world.CellSize && _settlement != null) { _fieldTarget = _settlement; Go(Move.Field, MortalTuning.ReturnSpeed); }
            else { _goal = _home; Go(Move.Direct, MortalTuning.ReturnSpeed); }
        }

        private void CarryPurify(float now, Vector3 pos, RegionMortals world)
        {
            if (!_hasPurifyCell) { _move = Move.None; return; }
            _purifyReadyAt = Mathf.Min(_purifyReadyAt, now + world.PurifySeconds);
            Vector3 at = world.Grid.CentreOf(_purifyCell);
            if (Distance(pos, at) > MortalTuning.PurifyReach * world.CellSize)
            {
                _goal = at;
                Go(Move.Direct, MortalTuning.WalkSpeed);
                return;
            }
            _move = Move.None;
            FaceToward(at.x - pos.x);
            if (now < _purifyReadyAt) return;
            if (world.Purify(_purifyCell)) Purified++;
            _purifyReadyAt = now + world.PurifySeconds;
        }

        private void Go(Move move, float cellsPerSecond)
        {
            _move = move;
            _speed = cellsPerSecond * Member.SpeedMultiplier;
        }

        // ---- Walking: every frame, only for what is steering itself ----

        internal void Step(float dt, float now, RegionMortals world)
        {
            if (Profile.Ranged) ShowShot(world);
            if (_move == Move.None || !_assigned || !Member.IsAlive) return;
            Vector3 p = transform.position;
            float cell = world.CellSize;
            Vector2 dir;
            if (_move == Move.Direct)
            {
                var to = new Vector2(_goal.x - p.x, _goal.y - p.y);
                float d = to.magnitude;
                if (d <= MortalTuning.ArriveDistance * cell) { Arrived(world); return; }
                dir = to / d;
            }
            else
            {
                RegionSettlements.Settlement target = Action == BehaviourAction.Flee ? _fieldTarget : _settlement;
                if (target == null) { _move = Move.None; return; }
                if (Action == BehaviourAction.ReturnHome && Distance(p, _home) <= 5f * cell) { _move = Move.None; return; }
                if (Action == BehaviourAction.Flee && MortalHomes.IsAt(target, world.Grid.CellAt(p))) { Arrived(world); return; }
                if (!world.Homes.TryDirection(target, p, now, out dir))
                {
                    // The field is still being computed, or there is no way through: head for the middle and slide.
                    Vector3 c = world.Homes.Centre(target);
                    dir = new Vector2(c.x - p.x, c.y - p.y);
                    if (dir.sqrMagnitude < 0.01f) return;
                    dir.Normalize();
                }
            }
            Advance(p, dir, _speed * cell * dt, dt, world);
        }

        // Moves along dir, sliding along a wall on one axis when the straight step is blocked; stuck for a while, it gives up the spot.
        private void Advance(Vector3 p, Vector2 dir, float length, float dt, RegionMortals world)
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
                    if (_stuck > MortalTuning.StuckSeconds)
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

        private void Arrived(RegionMortals world)
        {
            _move = Move.None;
            _stuck = 0f;
            _wait = Action == BehaviourAction.Patrol
                ? world.RandomRange(MortalTuning.PatrolPauseMin, MortalTuning.PatrolPauseMax)
                : world.RandomRange(MortalTuning.WorkPauseMin, MortalTuning.WorkPauseMax);
        }

        private void FaceToward(float dx)
        {
            if (_sprite != null && Mathf.Abs(dx) > 0.05f) _sprite.flipX = dx < 0f;
        }

        // An archer's attack is the horde's reach hit; the arrow flying to the target is only for the eye.
        private void ShowShot(RegionMortals world)
        {
            float readyAt = Member.Attack.ReadyAt;
            if (readyAt == _seenAttack) return;
            _seenAttack = readyAt;
            if (_fear > 0.5f) world.ShowArrow(transform.position, _threatAt);
        }

        private static float Distance(Vector3 a, Vector3 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }
}
