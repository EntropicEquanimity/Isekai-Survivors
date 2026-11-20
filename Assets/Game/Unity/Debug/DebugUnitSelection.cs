using System.Collections.Generic;
using Blondie.Core;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>Tracks the unit under the mouse and the one the debugger selected.</summary>
    [RegionSystem]
    public sealed class DebugUnitSelection : MonoBehaviour
    {
        private const float HoverRadius = 1.25f;

        static DebugUnitSelection() => Blondie.Core.StaticReset.Register(Clear);

        public static RegionEntity Hovered { get; private set; }
        public static RegionEntity Selected { get; private set; }
        public static Vector3 MouseWorld { get; private set; }

        public static bool TrySelected(out RegionEntity entity)
        {
            entity = IsValid(Selected) ? Selected : null;
            return entity != null;
        }

        public static void Clear() => Hovered = Selected = null;

        public static void ClearSelection() => Selected = null;

        public static bool SelectAt(Vector3 world, float radius = 1.5f)
        {
            Selected = Pick(world, radius);
            return Selected != null;
        }

        public static void SelectHovered()
        {
            if (IsValid(Hovered)) Selected = Hovered;
        }

        private void Update()
        {
            if (!TryMouse(out Vector2 screen, out Vector3 world))
            {
                Hovered = null;
                return;
            }

            MouseWorld = world;
            Hovered = Pick(world, HoverRadius);
            if (Mouse.current.leftButton.wasPressedThisFrame && !UIPanels.IsPointerOverUI(screen) && Hovered != null)
                Selected = Hovered;

            if (!IsValid(Selected)) Selected = null;
        }

        private static bool IsValid(RegionEntity entity) =>
            entity != null && entity.IsListed && entity.GameObject != null && !entity.Damageable.IsDead;

        private static RegionEntity Pick(Vector3 world, float radius)
        {
            float best = radius * radius;
            RegionEntity pick = null;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity entity = living[i];
                if (!IsValid(entity) || !IsUnit(entity.Kind)) continue;
                Vector3 at = entity.GameObject.transform.position;
                float dx = at.x - world.x;
                float dy = at.y - world.y;
                float sqr = dx * dx + dy * dy;
                if (sqr > best) continue;
                best = sqr;
                pick = entity;
            }
            return pick;
        }

        private static bool IsUnit(UnitKind kind) =>
            kind is UnitKind.Character or UnitKind.Demon or UnitKind.Mortal or UnitKind.Wildlife;

        private static bool TryMouse(out Vector2 screen, out Vector3 world)
        {
            screen = default;
            world = default;
            Mouse mouse = Mouse.current;
            if (mouse == null) return false;
            screen = mouse.position.ReadValue();
            if (PixelCamera.Main != null)
            {
                world = PixelCamera.Main.ScreenToWorld(screen);
                return true;
            }

            Camera cam = Camera.main;
            if (cam == null) return false;
            world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            return true;
        }
    }
}
