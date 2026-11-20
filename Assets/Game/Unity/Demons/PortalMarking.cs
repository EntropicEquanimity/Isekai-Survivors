using Blondie.Core;
using Blondie.GameSetup;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>What the next click sets on the selected Portal.</summary>
    public enum PortalMark { None, Raid, Rally }

    /// <summary>
    /// Marks points for the selected Portal. Armed from the Portal panel (<see cref="Arm"/>), the next left click in the
    /// world sets its raid target (and orders a Raid) or its rally point; right click or Escape cancels. On the map
    /// screen a click sets the raid target at once, with no arming, so a far target is marked where the whole Region
    /// can be seen. Runs before <see cref="BuildMode"/>, so the click that marks does not also select or deselect.
    /// </summary>
    [RegionSystem]
    [DefaultExecutionOrder(-100)]
    public sealed class PortalMarking : MonoBehaviour
    {
        private const string MenuName = "PortalMarking";

        private BuildMode _mode;
        private MapScreen _map;

        /// <summary>What the next world click sets.</summary>
        public static PortalMark Armed { get; private set; }

        /// <summary>A click was used to mark a point this frame, so a building must not take it as a selection.</summary>
        public static bool TookClick => s_clickFrame == Time.frameCount;

        private static int s_clickFrame = -1;

        static PortalMarking() => StaticReset.Register(() => { Armed = PortalMark.None; s_clickFrame = -1; });

        /// <summary>The next click in the world sets <paramref name="mark"/> on the selected Portal.</summary>
        public static void Arm(PortalMark mark)
        {
            if (mark == Armed) { Disarm(); return; }
            if (Armed == PortalMark.None) MenuStack.Push(MenuName, Disarm);
            Armed = mark;
        }

        public static void Disarm()
        {
            if (Armed == PortalMark.None) return;
            Armed = PortalMark.None;
            MenuStack.Pop(MenuName);
        }

        private void OnDisable() => Disarm();

        private void Update()
        {
            _mode ??= FindAnyObjectByType<BuildMode>();
            _map ??= FindAnyObjectByType<MapScreen>();
            Mouse mouse = Mouse.current;
            if (_mode == null || mouse == null || GameSetup.IsPaused) return;
            UnitProducer portal = SelectedPortal();
            if (portal == null) { Disarm(); return; }
            if (!mouse.leftButton.wasPressedThisFrame) return;
            Vector2 screen = mouse.position.ReadValue();

            if (_map != null && _map.IsOpen)
            {
                if (_map.Current == MapScreen.Tab.Region && _map.RegionMap != null && _map.RegionMap.TryWorldAt(screen, out Vector3 onMap))
                    Mark(portal, Armed == PortalMark.Rally ? PortalMark.Rally : PortalMark.Raid, onMap);
                return;
            }
            if (Armed == PortalMark.None || UIPanels.IsPointerOverUI(screen)) return;
            Mark(portal, Armed, ScreenToWorld(screen));
        }

        /// <summary>Sets a raid target or rally point on a Portal (the grid's bounds apply).</summary>
        public static bool Mark(UnitProducer portal, PortalMark mark, Vector3 world)
        {
            RegionGrid grid = RegionGrid.Current;
            if (portal == null || mark == PortalMark.None || grid == null || !grid.Contains(grid.CellAt(world))) return false;
            if (mark == PortalMark.Raid) portal.MarkRaid(world);
            else portal.SetRally(world);
            s_clickFrame = Time.frameCount;
            Disarm();
            return true;
        }

        private UnitProducer SelectedPortal()
        {
            Building selected = _mode.Selected;
            return selected != null && selected.TryGetComponent(out UnitProducer portal) ? portal : null;
        }

        private static Vector3 ScreenToWorld(Vector2 screen)
        {
            if (PixelCamera.Main != null) return PixelCamera.Main.ScreenToWorld(screen);
            Camera cam = Camera.main;
            return cam != null ? cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z)) : Vector3.zero;
        }
    }
}
