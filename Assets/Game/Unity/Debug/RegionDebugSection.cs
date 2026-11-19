using Blondie.Core;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>Region-level debug rows that are not owned by another system's own debug file.</summary>
    [RegionSystem]
    public sealed class RegionDebugSection : MonoBehaviour
    {
        private bool _paintCorruption;
        private int _brushLevel = CorruptionTuning.MaxLevel;

        private void OnEnable()
        {
            DebugActions.Toggle(this, "Region", "Corruption brush", () => _paintCorruption, on => _paintCorruption = on);
            DebugActions.Button(this, "Region", "Brush level -", () => _brushLevel = Mathf.Max(0, _brushLevel - 1), "brush");
            DebugActions.Button(this, "Region", "Brush level +", () => _brushLevel = Mathf.Min(CorruptionTuning.MaxLevel, _brushLevel + 1), "brush");
            DebugActions.Button(this, "Region", "Clear corruption under mouse", ClearUnderMouse);
            DebugActions.Info(this, "Region", () => $"Corruption brush {( _paintCorruption ? "on" : "off")} · level {_brushLevel}");
        }

        private void OnDisable() => DebugActions.Remove(this);

        private void Update()
        {
            if (!_paintCorruption || RegionCorruption.Current == null || Mouse.current == null) return;
            Vector2 screen = Mouse.current.position.ReadValue();
            if (UIPanels.IsPointerOverUI(screen)) return;
            if (!TryMouseWorld(screen, out Vector3 world)) return;
            Int2 cell = RegionGrid.Current.CellAt(world);
            if (Mouse.current.leftButton.isPressed) RegionCorruption.Current.Field.Set(cell, _brushLevel);
            if (Mouse.current.rightButton.isPressed) RegionCorruption.Current.Field.Set(cell, 0);
        }

        private static void ClearUnderMouse()
        {
            if (RegionCorruption.Current == null || Mouse.current == null || !TryMouseWorld(Mouse.current.position.ReadValue(), out Vector3 world)) return;
            RegionCorruption.Current.Field.Set(RegionGrid.Current.CellAt(world), 0);
        }

        private static bool TryMouseWorld(Vector2 screen, out Vector3 world)
        {
            if (PixelCamera.Main != null)
            {
                world = PixelCamera.Main.ScreenToWorld(screen);
                return true;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                world = default;
                return false;
            }

            world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            return true;
        }
    }
}
