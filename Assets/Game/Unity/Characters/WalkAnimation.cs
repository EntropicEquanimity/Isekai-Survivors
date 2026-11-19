using Blondie.Controllers;
using Blondie.Utils2D;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Plays the idle, walk or action flipbook on a <see cref="SpriteFrameAnimator"/>: the action (a swing) while its
    /// <see cref="Miner"/> is mining, else walk while the motor moves, else idle.
    /// </summary>
    [RequireComponent(typeof(SpriteFrameAnimator))]
    public sealed class WalkAnimation : MonoBehaviour
    {
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private Sprite[] _idle = new Sprite[0];
        [SerializeField] private Sprite[] _walk = new Sprite[0];
        [Tooltip("Played while mining (hitting Stone or a rock).")]
        [SerializeField] private Sprite[] _action = new Sprite[0];
        [SerializeField, Min(0f)] private float _movingSpeed = 0.1f;

        private SpriteFrameAnimator _animator;
        private Miner _miner;
        private int _state = -1;   // 0 idle, 1 walk, 2 action

        public void SetFrames(Sprite[] idle, Sprite[] walk, Sprite[] action = null)
        {
            _idle = idle;
            _walk = walk;
            _action = action ?? new Sprite[0];
            _state = -1;
        }

        private void Awake()
        {
            _animator = GetComponent<SpriteFrameAnimator>();
            if (_motor == null) _motor = GetComponentInParent<CharacterMotor>();
            _miner = GetComponentInParent<Miner>();
        }

        private void Update()
        {
            bool walking = _motor != null && _motor.Velocity.sqrMagnitude > _movingSpeed * _movingSpeed;
            int state = _miner != null && _miner.Node != null && _action.Length > 0 ? 2 : walking && _walk.Length > 0 ? 1 : 0;
            if (state == _state) return;
            _state = state;
            Sprite[] frames = state == 2 ? _action : state == 1 ? _walk : _idle;
            if (frames.Length == 0) return;
            _animator.SetFrames(frames);
            _animator.Play();
        }
    }
}
