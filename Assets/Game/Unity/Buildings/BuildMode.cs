using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// Placing and selecting Buildings with the mouse. Number keys pick a Building from the catalogue; a ghost follows
    /// the mouse on the grid (green: can place, yellow: can't afford, red: blocked or outside a Base). Left click places
    /// (hold Shift to keep placing), right click or Escape cancels. Without a Building picked, left click selects the
    /// Building under the mouse.
    /// </summary>
    public sealed class BuildMode : MonoBehaviour
    {
        private const string MenuName = "BuildMode";
        private static readonly Color Ok = new(0.4f, 1f, 0.4f, 0.55f);
        private static readonly Color Poor = new(1f, 0.9f, 0.3f, 0.55f);
        private static readonly Color Bad = new(1f, 0.3f, 0.3f, 0.55f);

        [SerializeField] private BuildingAsset[] _catalogue = Array.Empty<BuildingAsset>();
        [SerializeField] private SpriteRenderer _ghost;

        public IReadOnlyList<BuildingAsset> Catalogue => _catalogue;
        public BuildingAsset Picked { get; private set; }
        public Building Selected { get; private set; }
        public RegionBuildings.Verdict Verdict { get; private set; }

        /// <summary>What's picked or selected changed.</summary>
        public event Action Changed;

        public void Pick(BuildingAsset asset)
        {
            if (asset == Picked) { Cancel(); return; }
            if (Picked == null) MenuStack.Push(MenuName, Cancel);
            Picked = asset;
            Select(null);
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(true);
                if (asset.Preview != null) _ghost.sprite = asset.Preview;
                FitGhost(asset);
            }
            Changed?.Invoke();
        }

        public void Cancel()
        {
            if (Picked == null) return;
            Picked = null;
            MenuStack.Pop(MenuName);
            if (_ghost != null) _ghost.gameObject.SetActive(false);
            Changed?.Invoke();
        }

        public void Select(Building building)
        {
            if (Selected == building) return;
            Selected = building;
            Changed?.Invoke();
        }

        private void Start()
        {
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Selected != null && !Selected.IsAlive) Select(null);
            RegionBuildings buildings = RegionBuildings.Current;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (buildings == null || mouse == null || PlayerBrainBlocked()) return;

            if (keyboard != null)
                for (int i = 0; i < _catalogue.Length && i < 9; i++)
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame) Pick(_catalogue[i]);

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = ScreenToWorld(screen);
            Int2 cell = buildings.CellAt(world);
            bool overUI = UIPanels.IsPointerOverUI(screen);

            if (Picked == null)
            {
                if (mouse.leftButton.wasPressedThisFrame && !overUI) Select(buildings.At(cell));
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame) { Cancel(); return; }
            Int2 origin = BuildingGrid<Building>.OriginCentredOn(cell, new Int2(Picked.Size.x, Picked.Size.y));
            RegionBuildings.Verdict verdict = buildings.Check(Picked, origin);
            if (verdict != Verdict) { Verdict = verdict; Changed?.Invoke(); }
            if (_ghost != null)
            {
                _ghost.transform.position = buildings.CentreOf(origin, Picked.Size);
                _ghost.color = verdict == RegionBuildings.Verdict.Ok ? Ok : verdict == RegionBuildings.Verdict.TooExpensive ? Poor : Bad;
            }
            if (!mouse.leftButton.wasPressedThisFrame || overUI || verdict != RegionBuildings.Verdict.Ok) return;
            buildings.Place(Picked, origin);
            if (keyboard == null || !keyboard.shiftKey.isPressed) Cancel();
        }

        // The ghost covers exactly the cells the footprint takes, whatever the preview sprite's own size.
        private void FitGhost(BuildingAsset asset)
        {
            float cell = RegionBuildings.Current != null ? RegionBuildings.Current.CellSize : 1f;
            Vector2 sprite = _ghost.sprite != null ? (Vector2)_ghost.sprite.bounds.size : Vector2.one;
            _ghost.transform.localScale = new Vector3(asset.Size.x * cell / Mathf.Max(sprite.x, 1e-4f), asset.Size.y * cell / Mathf.Max(sprite.y, 1e-4f), 1f);
        }

        private static bool PlayerBrainBlocked() => GameSetup.IsPaused;

        private static Vector3 ScreenToWorld(Vector2 screen)
        {
            if (PixelCamera.Main != null) return PixelCamera.Main.ScreenToWorld(screen);
            Camera cam = Camera.main;
            return cam != null ? cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z)) : Vector3.zero;
        }
    }
}
