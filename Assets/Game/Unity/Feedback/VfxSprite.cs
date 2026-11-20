using UnityEngine;

namespace Isekai
{
    /// <summary>One pooled effect: a sprite that plays a <see cref="VfxClip"/> once. <see cref="VfxPlayer"/> starts, ticks and recycles it.</summary>
    public sealed class VfxSprite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private VfxClip _clip;
        private float _clock;
        private int _frame;

        public SpriteRenderer Renderer => _renderer != null ? _renderer : _renderer = GetComponent<SpriteRenderer>();
        public VfxClip Clip => _clip;
        public VfxKind Kind => _clip != null ? _clip.Kind : default;
        public int Frame => _frame;

        internal void Begin(VfxClip clip, Vector3 at, Color tint, int sortingOrder)
        {
            _clip = clip;
            _clock = 0f;
            _frame = 0;
            transform.position = at;
            SpriteRenderer r = Renderer;
            r.sprite = clip.Frames[0];
            r.color = tint;
            r.sortingOrder = sortingOrder;
            gameObject.SetActive(true);
        }

        /// <summary>Moves on by <paramref name="dt"/>; false once the last frame has shown.</summary>
        internal bool Tick(float dt)
        {
            _clock += dt * _clip.Fps;
            int frame = (int)_clock;
            if (frame >= _clip.Frames.Length) return false;
            if (frame != _frame)
            {
                _frame = frame;
                Renderer.sprite = _clip.Frames[frame];
            }
            return true;
        }

        internal void End()
        {
            _clip = null;
            gameObject.SetActive(false);
        }
    }
}
