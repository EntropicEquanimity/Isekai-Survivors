using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>"+1" with the Resource's icon beside it, rising and fading where something was mined or dropped off.</summary>
    public static class ResourcePopup
    {
        /// <summary>Shows it; returns the number's label, which holds the icon (null when there is no UI to show it on).</summary>
        public static Label Spawn(Vector3 at, ResourceType type, int amount)
        {
            Label number = FloatingText.Spawn(at, $"+{amount}", Color.white);
            Sprite sprite = GameIcons.Of(type);
            if (number == null || sprite == null) return number;
            // Inside the number's own label, so the two rise, fade and stay together at every zoom.
            VisualElement icon = GameStyle.Icon(sprite);
            icon.style.position = Position.Absolute;
            icon.style.left = new Length(100f, LengthUnit.Percent);
            icon.style.top = new Length(50f, LengthUnit.Percent);
            icon.style.marginLeft = 3;
            icon.style.marginTop = -sprite.rect.height * 0.5f;
            number.Add(icon);
            return number;
        }
    }
}
