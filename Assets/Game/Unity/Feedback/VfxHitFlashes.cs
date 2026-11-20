using Blondie.Juice;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The hit flash: a unit's sprite goes solid white for a moment when it is hit, drawn by Blondie Juice's sprite
    /// shader. A fixed few flash at once (a horde fight hits hundreds a frame; only the first few are worth the
    /// material swap), and each flash re-applies every frame because the unit's flipbook changes its sprite.
    /// </summary>
    public sealed class VfxHitFlashes
    {
        public const int Capacity = 24;
        public const float Seconds = 0.07f;

        private readonly SpriteRenderer[] _renderers = new SpriteRenderer[Capacity];
        private readonly float[] _until = new float[Capacity];
        private MaterialPropertyBlock _block;
        private JuiceFrame _frame = JuiceFrame.Rest;

        public VfxHitFlashes() => _frame.FlashAmount = 1f;

        /// <summary>Sprites flashing now.</summary>
        public int Count { get; private set; }

        /// <summary>Hits that got no flash because every slot was busy.</summary>
        public int Skipped { get; private set; }

        public bool IsFlashing(SpriteRenderer renderer)
        {
            for (int i = 0; i < Count; i++)
                if (_renderers[i] == renderer) return true;
            return false;
        }

        /// <summary>Whether <paramref name="renderer"/> is drawn with the flash shader right now.</summary>
        public bool IsDrawn(SpriteRenderer renderer) => JuiceSpriteShader.IsApplied(renderer);

        /// <summary>Whether a new hit would be flashed (a slot is free), so callers can skip looking the sprite up.</summary>
        public bool HasRoom => Count < Capacity;

        /// <summary>Flashes <paramref name="renderer"/> (again from the start when it already flashes).</summary>
        public bool Start(SpriteRenderer renderer, float now)
        {
            if (renderer == null) return false;
            for (int i = 0; i < Count; i++)
                if (_renderers[i] == renderer) { _until[i] = now + Seconds; return true; }
            if (Count >= Capacity) { Skipped++; return false; }
            _renderers[Count] = renderer;
            _until[Count] = now + Seconds;
            Count++;
            JuiceSpriteShader.Apply(renderer, ref _block, _frame);
            return true;
        }

        /// <summary>Redraws the flashes and ends the ones that are over, or whose sprite went away.</summary>
        public void Tick(float now)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                SpriteRenderer r = _renderers[i];
                if (r != null && r.gameObject.activeInHierarchy && now < _until[i])
                {
                    JuiceSpriteShader.Apply(r, ref _block, _frame);
                    continue;
                }
                if (r != null) JuiceSpriteShader.Clear(r, ref _block);
                Count--;
                _renderers[i] = _renderers[Count];
                _until[i] = _until[Count];
                _renderers[Count] = null;
            }
        }

        /// <summary>Gives every flashing sprite its own material back.</summary>
        public void Clear()
        {
            for (int i = 0; i < Count; i++)
            {
                if (_renderers[i] != null) JuiceSpriteShader.Clear(_renderers[i], ref _block);
                _renderers[i] = null;
            }
            Count = 0;
        }
    }
}
