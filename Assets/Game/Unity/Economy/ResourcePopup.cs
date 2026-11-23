using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>"+1" with the Resource's icon beside it, rising and fading where something was mined or dropped off.</summary>
    public sealed class ResourcePopup : MonoBehaviour
    {
        private const float Seconds = 0.8f;
        private const float Rise = 0.8f;
        private const int SortingOrder = 4000;

        private SpriteRenderer _icon;
        private Vector3 _from;
        private float _age;

        public static void Spawn(Vector3 at, ResourceType type, int amount)
        {
            FloatingText.Spawn(at, $"+{amount}", Color.white);
            Sprite sprite = GameIcons.Of(type);
            if (sprite == null) return;
            var go = new GameObject("Resource Popup");
            var popup = go.AddComponent<ResourcePopup>();
            popup._from = at + new Vector3(-0.9f, 0f, 0f);
            popup._icon = go.AddComponent<SpriteRenderer>();
            popup._icon.sprite = sprite;
            popup._icon.sortingOrder = SortingOrder;
            Material unlit = SideOutlineMaterials.Unlit;
            if (unlit != null) popup._icon.sharedMaterial = unlit;
            go.transform.position = popup._from;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Seconds);
            transform.position = _from + new Vector3(0f, Rise * t, 0f);
            _icon.color = new Color(1f, 1f, 1f, 1f - t * t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
