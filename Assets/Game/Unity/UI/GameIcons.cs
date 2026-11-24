using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Isekai
{
    /// <summary>
    /// Where the UI gets its icons (docs/ui-rules.md: icons first). Resource, Soul path and HUD icons are set on the
    /// Resources/GameIcons asset; Buildings and units use their own art (a Building's preview, else its prefab's sprite),
    /// cut to the pixels it draws. A missing icon is null, and the control falls back to a short name.
    /// </summary>
    [CreateAssetMenu(menuName = "Isekai/Game Icons")]
    public sealed class GameIcons : ScriptableObject
    {
        [SerializeField] private Sprite _stone;
        [SerializeField] private Sprite _wood;
        [SerializeField] private Sprite _souls;
        [Tooltip("One per SoulPath, in the enum's order (Brawler, Overlord, Warlord, Corruptor, Soulweaver, Assassin, Reaper, Bonelord).")]
        [SerializeField] private Sprite[] _soulPaths = new Sprite[0];
        [Tooltip("The HUD and menu icons, found by file name without \"Icon_\" (Icon_Order_Guard is \"Order_Guard\").")]
        [SerializeField] private Sprite[] _named = new Sprite[0];

        private static GameIcons s_current;
        private static readonly Dictionary<Object, Sprite> s_fromArt = new();

        static GameIcons() => StaticReset.Register(() => s_fromArt.Clear());

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

        /// <summary>A HUD or menu icon by name ("Order_Guard", "Night"), or null.</summary>
        public static Sprite Named(string name)
        {
            GameIcons icons = Current;
            if (icons == null || string.IsNullOrEmpty(name)) return null;
            string file = "Icon_" + name;
            foreach (Sprite sprite in icons._named)
                if (sprite != null && sprite.name == file) return sprite;
            return null;
        }

        /// <summary>An Anchor's icon: its own, else its Building's art.</summary>
        public static Sprite Of(AnchorKind kind)
        {
            Sprite own = Named(kind == AnchorKind.WarThrone ? "Anchor_WarThrone" : "Anchor_Shrine");
            return own != null ? own : Of(AnchorCatalog.Asset(kind));
        }

        /// <summary>A Building's icon: its preview sprite, else its prefab's own art.</summary>
        public static Sprite Of(BuildingAsset building)
        {
            if (building == null) return null;
            return building.Preview != null ? Cut(building.Preview) : Of(building.Prefab);
        }

        /// <summary>A unit's or Building's icon from its prefab: the first sprite it draws.</summary>
        public static Sprite Of(GameObject prefab)
        {
            if (prefab == null) return null;
            SpriteRenderer art = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return art != null ? Cut(art.sprite) : null;
        }

        // Unit art sits in big empty frames: the icon is the part with pixels in it, so it can be drawn at a whole scale
        // instead of shrinking the whole frame into the icon.
        private static Sprite Cut(Sprite sprite)
        {
            if (sprite == null) return null;
            if (s_fromArt.TryGetValue(sprite, out Sprite known) && known != null) return known;
            Rect drawn = Drawn(sprite);
            Sprite cut = Sprite.Create(sprite.texture, drawn, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            cut.name = sprite.name + " icon";
            cut.hideFlags = HideFlags.DontSave;
            s_fromArt[sprite] = cut;
            return cut;
        }

        // The rectangle of a sprite's visible pixels in its texture. The art isn't readable on the CPU, so it is copied off
        // the GPU once per icon; without a GPU (a batch run) the whole frame stands.
        private static Rect Drawn(Sprite sprite)
        {
            Rect frame = sprite.rect;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return frame;
            int w = (int)frame.width, h = (int)frame.height;
            RenderTexture target = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture before = RenderTexture.active;
            Graphics.Blit(sprite.texture, target);
            RenderTexture.active = target;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.ReadPixels(frame, 0, 0);
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(target);
            Color32[] pixels = copy.GetPixels32();
            Object.Destroy(copy);
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (pixels[x + y * w].a < 8) continue;
                x0 = Mathf.Min(x0, x);
                x1 = Mathf.Max(x1, x);
                y0 = Mathf.Min(y0, y);
                y1 = Mathf.Max(y1, y);
            }
            return x1 < x0 ? frame : new Rect(frame.x + x0, frame.y + y0, x1 - x0 + 1, y1 - y0 + 1);
        }
    }
}
