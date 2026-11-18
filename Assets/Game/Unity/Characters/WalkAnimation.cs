using Blondie.Controllers;
using Blondie.Utils2D;
using UnityEngine;

namespace Isekai
{
    /// <summary>Plays the idle or walk flipbook on a <see cref="SpriteFrameAnimator"/> depending on whether the motor is moving.</summary>
    [RequireComponent(typeof(SpriteFrameAnimator))]
    public sealed class WalkAnimation : MonoBehaviour
    {
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private Sprite[] _idle = new Sprite[0];
        [SerializeField] private Sprite[] _walk = new Sprite[0];
        [SerializeField, Min(0f)] private float _movingSpeed = 0.1f;

        private SpriteFrameAnimator _animator;
        private bool? _walking;

        public void SetFrames(Sprite[] idle, Sprite[] walk)
        {
            _idle = idle;
            _walk = walk;
            _walking = null;
        }

        private void Awake()
        {
            _animator = GetComponent<SpriteFrameAnimator>();
            if (_motor == null) _motor = GetComponentInParent<CharacterMotor>();
        }

        private void Update()
        {
            bool walking = _motor != null && _motor.Velocity.sqrMagnitude > _movingSpeed * _movingSpeed;
            if (walking == _walking) return;
            _walking = walking;
            Sprite[] frames = walking && _walk.Length > 0 ? _walk : _idle;
            if (frames.Length == 0) return;
            _animator.SetFrames(frames);
            _animator.Play();
        }
    }
}
