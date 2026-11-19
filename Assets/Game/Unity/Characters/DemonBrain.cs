using System.Collections.Generic;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Controllers;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A demon from a Portal (an Imp, a bone demon, a raised skeleton): its weapons fire at hostile units in range
    /// (<see cref="AutoCaster"/>) and it decides what to do on its own, a few times a second (staggered), with the
    /// shared weighted behaviours (<see cref="DemonProfiles"/>) inside its Portal's standing order
    /// (<see cref="UnitProducer"/>: Guard the rally point, Raid the mark, Follow Jack):
    /// <list type="bullet">
    /// <item>Fight: walk to the nearest hostile unit around its post and stand in weapon range.</item>
    /// <item>ReturnHome: walk back to its post (the Raid mark is far away, so this is also how it marches there).</item>
    /// <item>Work (a bone demon, <see cref="BoneRaiser"/>): spend a bone pile on a skeleton.</item>
    /// <item>Idle: stand.</item>
    /// </list>
    /// A Portal whose bones policy is Destroy has its demons remove the piles near them. Moves are straight walks of the
    /// motor, reissued only when the goal moves, so no path fields are computed per demon. Without a Portal (a demon Jack
    /// left) it guards the spot it stands on. <see cref="IgnoresOrders"/> is the hook for the Unruly trait (#20).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor), typeof(AutoCaster))]
    public sealed class DemonBrain : MonoBehaviour
    {
        [SerializeField] private DemonType _type = DemonType.Imp;
        [SerializeField, Min(0f)] private float _attackRange = 1.2f;
        [SerializeField, Min(0.05f)] private float _thinkInterval = DemonTuning.ThinkSeconds;

        private readonly List<CombatUnit> _units = new();
        private CharacterMotor _motor;
        private Damageable _damageable;
        private BoneRaiser _raiser;
        private UnitProducer _portal;
        private BehaviourProfile _behaviour;
        private BehaviourContext _context;
        private DemonPost _post;
        private Vector3 _postAt;
        private Vector3 _ownPost;
        private Vector3 _goal;
        private bool _goalSet;
        private int _slot;
        private float _nextThink;

        public DemonType Type => _type;
        public UnitProducer Portal => _portal;

        /// <summary>The order it follows and the point it gathers at now (its Portal's order, in its own slot).</summary>
        public DemonPost Post => _post;
        public Vector3 PostAt => _postAt;
        public PortalOrder Order => _post.Order;

        /// <summary>What it is doing now.</summary>
        public BehaviourAction Action { get; private set; }

        /// <summary>The weights it decides with now (the type's, or a trait's copy).</summary>
        public BehaviourProfile Behaviour => _behaviour ??= DemonProfiles.For(_type);

        /// <summary>
        /// Set by the Unruly trait (#20): the demon ignores its Portal's order and guards where it stands, though it stays on
        /// your side.
        /// </summary>
        public bool IgnoresOrders { get; set; }

        /// <summary>A trait's weights: the brain decides with this copy from now on.</summary>
        public void SetBehaviour(BehaviourProfile profile) => _behaviour = profile;

        internal void Assign(UnitProducer portal, int slot)
        {
            _portal = portal;
            _slot = slot;
            _behaviour = null;
            Action = BehaviourAction.Idle;
            _goalSet = false;
            Resolve();
            GoTo(_postAt);
        }

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _damageable = GetComponent<Damageable>();
            _raiser = GetComponent<BoneRaiser>();
        }

        private void OnEnable()
        {
            _ownPost = transform.position;
            _nextThink = Time.time + Random.value * _thinkInterval;   // staggered, so a Portal's demons do not all think in one frame
        }

        private void Update()
        {
            if (Time.time < _nextThink) return;
            _nextThink = Time.time + _thinkInterval;
            Think();
        }

        private void Think()
        {
            Resolve();
            Vector3 position = transform.position;
            float reach = _post.Leash + _attackRange;
            CombatUnit hostile = Nearest(_postAt, reach, out float distance);
            // On the march it still fights what it walks into.
            if (hostile == null) hostile = Nearest(position, _attackRange + 0.5f, out distance);
            float fromPost = Vector2.Distance(position, _postAt);

            _context.Health = _damageable != null && _damageable.MaxHealth > 0f ? _damageable.Unit.Health / _damageable.MaxHealth : 1f;
            _context.ThreatSeen = hostile != null;
            _context.ThreatCloseness = hostile != null ? Mathf.Clamp01(1f - distance / reach) : 0f;
            _context.DistanceHome = fromPost;
            _context.Leash = _post.Leash;
            Action = BehaviourScoring.Choose(Behaviour, _context, Action);

            DestroyBones(position);
            switch (Action)
            {
                case BehaviourAction.Fight when hostile != null:
                    if (distance <= _attackRange) Halt();
                    else GoTo(new Vector3(hostile.Position.x, hostile.Position.y, 0f));
                    break;
                case BehaviourAction.ReturnHome:
                    if (fromPost > 0.5f) GoTo(_postAt);
                    else Halt();
                    break;
                case BehaviourAction.Work when _raiser != null && _raiser.Work(_portal, _postAt, _post.Leash, out Vector3 pile):
                    GoTo(pile);
                    break;
                default:
                    Halt();
                    break;
            }
        }

        // Where it stands now: its Portal's order resolved (a Raid mark and Jack both move), in its own slot.
        private void Resolve()
        {
            if (_portal == null || IgnoresOrders)
            {
                _post = new DemonPost(PortalOrder.Guard, _ownPost.x, _ownPost.y, DemonTuning.GuardLeash);
                _postAt = _ownPost;
                return;
            }
            _post = _portal.Resolve();
            DemonPosts.SlotOffset(_slot, out float dx, out float dy);
            _postAt = new Vector3(_post.X + dx, _post.Y + dy, 0f);
        }

        // A Destroy policy: the piles near it go, before a mortal can find them.
        private void DestroyBones(Vector3 position)
        {
            if (_portal == null || _portal.Policy != BonesPolicy.Destroy || RegionBones.Current == null) return;
            IReadOnlyList<BonePile> piles = RegionBones.Current.Piles;
            float radius = DemonTuning.DestroyRadius * DemonTuning.DestroyRadius;
            for (int i = piles.Count - 1; i >= 0; i--)
            {
                BonePile pile = piles[i];
                if (((Vector2)pile.transform.position - (Vector2)position).sqrMagnitude <= radius) RegionBones.Current.Remove(pile);
            }
        }

        // The nearest hostile unit around a point.
        private CombatUnit Nearest(Vector3 around, float radius, out float distance) =>
            Targeting.NearestHostile(_damageable != null ? _damageable.Unit : null, around, radius, transform.position, _units, out distance);

        // Walks to a goal; the order is reissued only when the goal has moved or the motor has stopped.
        private void GoTo(Vector3 goal)
        {
            if (_goalSet && _motor.HasDestination && (goal - _goal).sqrMagnitude < 0.5f) return;
            _goal = goal;
            _goalSet = true;
            _motor.MoveTo(goal);
        }

        private void Halt()
        {
            if (_motor.HasDestination) _motor.Stop();
            _goalSet = false;
        }
    }
}
