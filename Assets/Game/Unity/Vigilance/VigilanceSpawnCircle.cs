using System;
using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A summoning circle on the Mortals' side that Waves can arrive through. It stands until something destroys it, and
    /// once destroyed it sends nothing more. Built in code (one object: art, a trigger collider so Jack's side can hit
    /// it, and a Damageable on the Mortals' team); <see cref="VigilanceDirector"/> places them. Discovery and staying
    /// marked on the map is ticket #17.
    /// </summary>
    public sealed class VigilanceSpawnCircle : MonoBehaviour
    {
        private const int Size = 48;
        private const float PixelsPerUnit = 16f;
        private const int SortingOrder = 10;

        private static Sprite s_sprite;

        private Damageable _damageable;
        private RegionEntity _entity;
        private bool _announced;

        /// <summary>It stands: placed, not destroyed.</summary>
        public bool IsAlive => this != null && isActiveAndEnabled && _damageable != null && !_damageable.IsDead;

        public Damageable Damageable => _damageable;

        /// <summary>It was destroyed (raised once).</summary>
        public event Action<VigilanceSpawnCircle> Destroyed;

        /// <summary>Makes a circle at <paramref name="position"/>.</summary>
        public static VigilanceSpawnCircle Create(Vector3 position, Transform parent)
        {
            var go = new GameObject("Spawn Circle");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var art = go.AddComponent<SpriteRenderer>();
            art.sprite = CircleSprite();
            art.sortingOrder = SortingOrder;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = VigilanceTuning.SpawnCircleRadius * 0.5f;

            var damageable = go.AddComponent<Damageable>();
            damageable.MaxHealth = VigilanceTuning.SpawnCircleHealth;
            _ = damageable.Unit;
            go.AddComponent<Team>().Id = TeamIds.Of(Side.Mortals);
            damageable.Unit.Heal(VigilanceTuning.SpawnCircleHealth);

            return go.AddComponent<VigilanceSpawnCircle>();
        }

        private void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _entity = new RegionEntity(_damageable, UnitKind.Building);
        }

        private void OnEnable()
        {
            _damageable.Died += OnDied;
            _entity.Enter();
        }

        private void OnDisable()
        {
            if (_damageable != null) _damageable.Died -= OnDied;
            _entity?.Leave();
        }

        private void OnDied(DamageResult result)
        {
            if (_announced) return;
            _announced = true;
            Destroyed?.Invoke(this);
            Destroy(gameObject);
        }

        // A ring of pixels at the game's 16 px per unit, drawn once.
        private static Sprite CircleSprite()
        {
            if (s_sprite != null) return s_sprite;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Spawn Circle" };
            var ring = new Color32(214, 96, 255, 255);
            var fill = new Color32(120, 30, 160, 70);
            float centre = (Size - 1) * 0.5f;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                texture.SetPixel(x, y, d <= centre - 3f ? fill : d <= centre ? ring : Color.clear);
            }
            texture.Apply();
            s_sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return s_sprite;
        }
    }
}
