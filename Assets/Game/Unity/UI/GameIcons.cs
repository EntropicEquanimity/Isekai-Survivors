using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Where the UI gets its icons (docs/ui-rules.md: icons first). Resource and Soul path icons are set on the
    /// Resources/GameIcons asset; Buildings and units use their own art (a Building's preview, else its prefab's sprite).
    /// A missing icon is null, and the control falls back to a short name.
    /// </summary>
    [CreateAssetMenu(menuName = "Isekai/Game Icons")]
    public sealed class GameIcons : ScriptableObject
    {
        [SerializeField] private Sprite _stone;
        [SerializeField] private Sprite _wood;
        [SerializeField] private Sprite _souls;
        [SerializeField] private Sprite _worker;
        [Tooltip("One per SoulPath, in the enum's order (Brawler, Overlord, Warlord, Corruptor, Soulweaver, Assassin, Reaper, Bonelord).")]
        [SerializeField] private Sprite[] _soulPaths = new Sprite[0];

        private static GameIcons s_current;
        private static readonly Dictionary<Object, Sprite> s_fromArt = new();

        private static GameIcons Current => s_current != null ? s_current : s_current = Resources.Load<GameIcons>("GameIcons");

        /// <summary>The icon of a Resource (Stone, Wood, Souls).</summary>
        public static Sprite Of(ResourceType type)
        {
            GameIcons icons = Current;
            if (icons == null) return null;
            return type switch
            {
                ResourceType.Stone => icons._stone,
                ResourceType.Wood => icons._wood,
                ResourceType.Souls => icons._souls,
                _ => null,
            };
        }

        /// <summary>The icon of a Soul path (null until its art is set).</summary>
        public static Sprite Of(SoulPath path)
        {
            GameIcons icons = Current;
            int i = (int)path;
            return icons != null && i >= 0 && i < icons._soulPaths.Length ? icons._soulPaths[i] : null;
        }

        /// <summary>The worker's icon (set on the asset, else null).</summary>
        public static Sprite Worker => Current != null ? Current._worker : null;

        /// <summary>A Building's icon: its preview sprite, else its prefab's own art.</summary>
        public static Sprite Of(BuildingAsset building)
        {
            if (building == null) return null;
            return building.Preview != null ? building.Preview : Of(building.Prefab);
        }

        /// <summary>A unit's or Building's icon from its prefab: the first sprite it draws (kept once found).</summary>
        public static Sprite Of(GameObject prefab)
        {
            if (prefab == null) return null;
            if (s_fromArt.TryGetValue(prefab, out Sprite known)) return known;
            SpriteRenderer art = prefab.GetComponentInChildren<SpriteRenderer>(true);
            Sprite sprite = art != null ? art.sprite : null;
            s_fromArt[prefab] = sprite;
            return sprite;
        }
    }
}
