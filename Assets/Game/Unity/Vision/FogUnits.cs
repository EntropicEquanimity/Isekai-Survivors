using System.Collections.Generic;
using System.Diagnostics;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// What fog of war hides besides ground. Units not on your side are drawn only while your side sees the cell they
    /// stand in (their renderers switch off and on; Buildings, rocks and nodes stay on explored ground). A Building
    /// that dies out of your sight leaves a ghost of itself, "as last seen", until your side sees its ground again.
    /// </summary>
    [RegionSystem]
    public sealed class FogUnits : MonoBehaviour
    {
        /// <summary>A destroyed Building your side hasn't seen fall yet.</summary>
        public readonly struct Ghost
        {
            public Ghost(Vector3 position, Vector2Int size, string name, bool players)
            {
                Position = position;
                Size = size;
                Name = name;
                Players = players;
            }

            public Vector3 Position { get; }
            public Vector2Int Size { get; }
            public string Name { get; }
            public bool Players { get; }
        }

        private const int Examined = 256;
        private const int RegatherPerFrame = 4;

        private sealed class Hidden
        {
            public RegionEntity Unit;
            public readonly List<Renderer> Renderers = new();
            public int Number;
        }

        private sealed class Remembered
        {
            public Int2 Cell;
            public GameObject Art;
            public Ghost Info;
        }

        private readonly Dictionary<RegionEntity, Hidden> _hidden = new();
        private readonly List<Hidden> _hiddenList = new();
        private readonly List<Renderer> _buffer = new();
        private readonly List<Remembered> _ghosts = new();
        private readonly List<Ghost> _ghostInfo = new();
        private int _cursor;
        private int _regatherCursor;
        private bool _subscribed;

        public static FogUnits Current { get; private set; }

        /// <summary>Units hidden in the fog right now.</summary>
        public int HiddenCount => _hiddenList.Count;

        /// <summary>Destroyed Buildings remembered on explored ground (for the map screen).</summary>
        public IReadOnlyList<Ghost> Ghosts => _ghostInfo;

        /// <summary>Whether the world draws <paramref name="unit"/> at all: it is hidden in the fog (for tests).</summary>
        public bool IsHidden(RegionEntity unit) => _hidden.ContainsKey(unit);

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
            ShowAll();
            ClearGhosts();
            if (_subscribed) RegionEntities.Died -= OnDied;
            _subscribed = false;
        }

        private void LateUpdate()
        {
            RegionVision vision = RegionVision.Current;
            if (!_subscribed) { RegionEntities.Died += OnDied; _subscribed = true; }
            if (vision == null || !vision.IsReady || !RegionVision.IsOn)
            {
                ShowAll();
                ClearGhosts();
                return;
            }
            long start = Stopwatch.GetTimestamp();
            DropGone();
            // A slice of the registry each frame: a unit's drawn or hidden state follows its cell within a few frames.
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            int n = living.Count;
            for (int k = 0; k < Examined && n > 0; k++)
            {
                if (++_cursor >= n) _cursor = 0;
                RegionEntity unit = living[_cursor];
                UnitKind kind = unit.Kind;
                if (kind == UnitKind.Building || kind == UnitKind.Character) continue;
                bool shouldHide = unit.Side != Side.Yours && (Concealment.IsHidden(unit) || !vision.CanSee(Side.Yours, unit.Cell));
                bool isHidden = _hidden.TryGetValue(unit, out Hidden h);
                if (shouldHide && !isHidden) Hide(unit);
                else if (!shouldHide && isHidden) Show(h);
            }
            RegatherSome();
            DropSeenGhosts(vision);
            VisionCost.Add(Stopwatch.GetTimestamp() - start);
        }

        private void Hide(RegionEntity unit)
        {
            var h = new Hidden { Unit = unit, Number = _hiddenList.Count };
            Gather(h);
            _hidden[unit] = h;
            _hiddenList.Add(h);
        }

        // Switches off what draws now; run again now and then so a health bar made later goes dark too.
        private void Gather(Hidden h)
        {
            h.Unit.GameObject.GetComponentsInChildren(true, _buffer);
            foreach (Renderer r in _buffer)
            {
                if (!r.enabled) continue;
                r.enabled = false;
                h.Renderers.Add(r);
            }
            _buffer.Clear();
        }

        // Hidden units that left the registry (died, despawned) draw again.
        private void DropGone()
        {
            for (int i = _hiddenList.Count - 1; i >= 0; i--)
                if (!_hiddenList[i].Unit.IsListed) Show(_hiddenList[i]);
        }

        // A health bar made after a unit hid would show: look again at a few hidden units each frame.
        private void RegatherSome()
        {
            for (int i = 0; i < RegatherPerFrame && _hiddenList.Count > 0; i++)
            {
                if (++_regatherCursor >= _hiddenList.Count) _regatherCursor = 0;
                Gather(_hiddenList[_regatherCursor]);
            }
        }

        private void Show(Hidden h)
        {
            foreach (Renderer r in h.Renderers)
                if (r != null) r.enabled = true;
            _hidden.Remove(h.Unit);
            int last = _hiddenList.Count - 1;
            _hiddenList[h.Number] = _hiddenList[last];
            _hiddenList[h.Number].Number = h.Number;
            _hiddenList.RemoveAt(last);
        }

        private void ShowAll()
        {
            if (_hiddenList.Count == 0) return;
            foreach (Hidden h in _hiddenList)
                foreach (Renderer r in h.Renderers)
                    if (r != null) r.enabled = true;
            _hidden.Clear();
            _hiddenList.Clear();
        }

        private void OnDied(UnitDeath death)
        {
            if (death.Kind != UnitKind.Building) return;
            RegionVision vision = RegionVision.Current;
            if (vision == null || !vision.IsReady || !RegionVision.IsOn) return;
            FogState state = vision.StateOf(Side.Yours, death.Cell);
            if (state != FogState.Explored) return;   // seen falling, or never seen: nothing to remember
            Remember(death);
        }

        private void Remember(UnitDeath death)
        {
            GameObject building = death.Unit.GameObject;
            var art = new GameObject("Ghost " + building.name);
            art.transform.SetParent(transform, false);
            foreach (SpriteRenderer r in building.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!r.enabled || r.sprite == null || r.GetComponentInParent<HealthBar>() != null) continue;
                var copy = new GameObject(r.name);
                copy.transform.SetParent(art.transform, false);
                copy.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                copy.transform.localScale = r.transform.lossyScale;
                var shown = copy.AddComponent<SpriteRenderer>();
                shown.sprite = r.sprite;
                shown.color = r.color;
                shown.flipX = r.flipX;
                shown.flipY = r.flipY;
                shown.sharedMaterial = r.sharedMaterial;
                shown.sortingLayerID = r.sortingLayerID;
                shown.sortingOrder = r.sortingOrder;
            }
            var b = building.GetComponent<Building>();
            Vector2Int size = b != null && b.Asset != null ? b.Asset.Size : new Vector2Int(1, 1);
            string name = b != null && b.Asset != null ? b.Asset.DisplayName : "Building";
            var info = new Ghost(building.transform.position, size, name, death.Side == Side.Yours);
            _ghosts.Add(new Remembered { Cell = death.Cell, Art = art, Info = info });
            _ghostInfo.Add(info);
        }

        // Seeing the ground again shows the ruin as it is now.
        private void DropSeenGhosts(RegionVision vision)
        {
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                if (!vision.CanSee(Side.Yours, _ghosts[i].Cell)) continue;
                Destroy(_ghosts[i].Art);
                _ghosts.RemoveAt(i);
                _ghostInfo.RemoveAt(i);
            }
        }

        private void ClearGhosts()
        {
            if (_ghosts.Count == 0) return;
            foreach (Remembered g in _ghosts) Destroy(g.Art);
            _ghosts.Clear();
            _ghostInfo.Clear();
        }
    }
}
