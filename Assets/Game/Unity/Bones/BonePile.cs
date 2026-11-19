using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>What became of a pile.</summary>
    public enum BoneFate
    {
        /// <summary>It lies there.</summary>
        Standing,
        /// <summary>Something used it (a bone demon, a Portal's Harvest policy): it can be used once.</summary>
        Taken,
        /// <summary>Something destroyed it (a Portal's Destroy policy).</summary>
        Removed,
        /// <summary>It crumbled away.</summary>
        Crumbled,
    }

    /// <summary>
    /// A pile of bones a mortal's or wildlife's death left: one ground sprite under the units. It looks intact, then
    /// cracks and crumbles with age (<see cref="BoneRules"/>); <see cref="RegionBones"/> ages and removes it. The art
    /// is the two placeholder sheets, cut into 20x12 px cells at 16 px per cell.
    /// </summary>
    public sealed class BonePile : MonoBehaviour
    {
        private const int CellWidth = 20;
        private const int CellHeight = 12;
        private const float PixelsPerUnit = 16f;

        // Bones_Sheet cells (row * 3 + column) of each kind's intact variants.
        private static readonly int[][] IntactVariants =
        {
            new[] { 0, 1, 2 },   // Human
            new[] { 4, 5 },      // Beast
            new[] { 6, 7 },      // Demon
            new[] { 3 },         // Small
        };

        private static readonly Dictionary<Texture2D, Sprite[]> s_cut = new();

        [SerializeField] private Texture2D _bonesSheet;
        [SerializeField] private Texture2D _crumbleSheet;

        private SpriteRenderer _renderer;
        private int _variant;
        private int _stage = -1;

        public BoneKind Kind { get; private set; }
        public Int2 Cell { get; private set; }
        public BoneFate Fate { get; internal set; }

        /// <summary><see cref="Time.time"/> it was left at.</summary>
        public float BornAt { get; private set; }

        /// <summary>It lies there and can still be used.</summary>
        public bool IsAvailable => Fate == BoneFate.Standing;

        /// <summary>The look it has now (0 intact .. 3 mostly crumbled).</summary>
        public int Stage => _stage;

        internal void Place(BoneKind kind, int variantRoll, Int2 cell, float now)
        {
            Kind = kind;
            Cell = cell;
            BornAt = now;
            Fate = BoneFate.Standing;
            int[] variants = IntactVariants[(int)kind];
            _variant = variants[variantRoll % variants.Length];
            _stage = -1;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            Look(0);
        }

        /// <summary>Shows the look for the pile's age at <paramref name="now"/>.</summary>
        internal void Age(float now) => Look(BoneRules.Stage(now - BornAt));

        private void Look(int stage)
        {
            if (stage == _stage) return;
            _stage = stage;
            // Intact is one of the kind's variants; it then crumbles along the kind's row of the crumble sheet.
            _renderer.sprite = stage == 0
                ? Cut(_bonesSheet, 3, 3)[_variant]
                : Cut(_crumbleSheet, 4, 4)[(int)Kind * 4 + stage];
        }

        // The sheet cut into cells, row 0 at the top, cached.
        private static Sprite[] Cut(Texture2D sheet, int columns, int rows)
        {
            if (s_cut.TryGetValue(sheet, out Sprite[] cut) && cut[0] != null) return cut;
            cut = new Sprite[columns * rows];
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                var rect = new Rect(col * CellWidth, sheet.height - (row + 1) * CellHeight, CellWidth, CellHeight);
                Sprite sprite = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = $"{sheet.name}_{row * columns + col}";
                cut[row * columns + col] = sprite;
            }
            s_cut[sheet] = cut;
            return cut;
        }
    }
}
