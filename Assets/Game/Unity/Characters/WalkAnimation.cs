using System;
using Blondie.Controllers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Isekai
{
    /// <summary>
    /// Binding data for the shared unit flipbook animator. Existing prefabs already carry their idle/walk/mine frames
    /// here; units that use the shared table can set only <see cref="_key"/>.
    /// </summary>
    public sealed class WalkAnimation : MonoBehaviour
    {
        [SerializeField] private UnitAnimationKey _key = UnitAnimationKey.None;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private Sprite[] _idle = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _walk = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _attack = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _hurt = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _death = Array.Empty<Sprite>();
        [Tooltip("Played while mining (hitting Stone or a rock).")]
        [SerializeField, FormerlySerializedAs("_action")] private Sprite[] _mine = Array.Empty<Sprite>();
        [SerializeField, Min(0f)] private float _movingSpeed = 0.1f;

        private Miner _miner;
        private bool _minerResolved;
        private CharacterMotor _resolvedMotor;
        private bool _motorResolved;
        private SpriteRenderer _resolvedRenderer;
        private bool _rendererResolved;

        public UnitAnimationKey Key => _key;
        public float MovingSpeed => _movingSpeed;
        public CharacterMotor Motor
        {
            get
            {
                if (!_motorResolved)
                {
                    _resolvedMotor = _motor != null ? _motor : GetComponentInParent<CharacterMotor>();
                    _motorResolved = true;
                }
                return _resolvedMotor;
            }
        }

        public Miner Miner
        {
            get
            {
                if (!_minerResolved)
                {
                    _miner = GetComponentInParent<Miner>();
                    _minerResolved = true;
                }
                return _miner;
            }
        }

        public SpriteRenderer Renderer
        {
            get
            {
                if (!_rendererResolved)
                {
                    _resolvedRenderer = _renderer != null ? _renderer : ResolveRenderer();
                    _rendererResolved = true;
                }
                return _resolvedRenderer;
            }
        }
        public Sprite[] IdleFrames => _idle;
        public Sprite[] WalkFrames => _walk;
        public Sprite[] AttackFrames => _attack;
        public Sprite[] HurtFrames => _hurt;
        public Sprite[] DeathFrames => _death;
        public Sprite[] MineFrames => _mine;

        public void SetFrames(Sprite[] idle, Sprite[] walk, Sprite[] mine = null)
        {
            _idle = idle ?? Array.Empty<Sprite>();
            _walk = walk ?? Array.Empty<Sprite>();
            _mine = mine ?? Array.Empty<Sprite>();
        }

        private void Awake()
        {
            _resolvedRenderer = _renderer != null ? _renderer : ResolveRenderer();
            _rendererResolved = true;
            _resolvedMotor = _motor != null ? _motor : GetComponentInParent<CharacterMotor>();
            _motorResolved = true;
            _miner = GetComponentInParent<Miner>();
            _minerResolved = true;
        }

        public bool IsMining()
        {
            Miner miner = Miner;
            return miner != null && miner.Node != null && _mine != null && _mine.Length > 0;
        }

        private SpriteRenderer ResolveRenderer() =>
            GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
    }
}
