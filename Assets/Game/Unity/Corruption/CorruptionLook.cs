using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The sprites the corruption overlay draws: <see cref="Variants"/> plain ground tiles for each level 1 to 4, level
    /// by level (the placeholder Grass_Corrupted_L1..L4 tiles). Level 0 has no overlay. Loaded from Resources.
    /// </summary>
    public sealed class CorruptionLook : ScriptableObject
    {
        public const int Variants = 4;

        [SerializeField] private Sprite[] _sprites = System.Array.Empty<Sprite>();

        /// <summary>The sprite for <paramref name="level"/> (1 to 4), variant <paramref name="variant"/> (0 to <see cref="Variants"/> - 1); null when missing.</summary>
        public Sprite Sprite(int level, int variant)
        {
            int i = (level - 1) * Variants + variant;
            return level >= 1 && variant >= 0 && variant < Variants && i < _sprites.Length ? _sprites[i] : null;
        }
    }
}
