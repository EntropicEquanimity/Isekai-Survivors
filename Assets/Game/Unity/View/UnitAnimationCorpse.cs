using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>Short-lived sprite used to play a death flipbook when a unit is hidden or pooled at once.</summary>
    public sealed class UnitAnimationCorpse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private Sprite[] _frames = Array.Empty<Sprite>();
        private float _fps;
        private int _frame;
        private float _clock;
        private float _hideAt;

        public SpriteRenderer Renderer => _renderer != null ? _renderer : _renderer = GetComponent<SpriteRenderer>();
        public bool IsPlaying { get; private set; }
        public Sprite CurrentSprite => _renderer != null ? _renderer.sprite : null;

        internal void Play(Sprite[] frames, float fps, Vector3 at, bool flipX, Material material, Color color, int sortingLayer, int sortingOrder)
        {
            _frames = frames ?? Array.Empty<Sprite>();
            _fps = Mathf.Max(1f, fps);
            _frame = 0;
            _clock = 0f;
            _hideAt = Time.time + Mathf.Max(0.1f, _frames.Length / _fps);
            transform.position = at;
            SpriteRenderer renderer = Renderer;
            renderer.flipX = flipX;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            renderer.sprite = _frames.Length > 0 ? _frames[0] : null;
            IsPlaying = _frames.Length > 0;
            gameObject.SetActive(IsPlaying);
        }

        internal void Tick(float dt, float now)
        {
            if (!IsPlaying) return;
            if (_frames.Length > 1)
            {
                _clock += dt * _fps;
                int steps = (int)_clock;
                if (steps > 0)
                {
                    _clock -= steps;
                    _frame = Mathf.Min(_frames.Length - 1, _frame + steps);
                    Renderer.sprite = _frames[_frame];
                }
            }

            if (now < _hideAt) return;
            IsPlaying = false;
            gameObject.SetActive(false);
        }
    }
}
