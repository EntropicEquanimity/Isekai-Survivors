using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>Anchor panel (worker count, worker and Demon King icon buttons with their Soul costs) and worker summoning for this Region.</summary>
    [RegionSystem]
    public sealed class RegionWorkers : MonoBehaviour, IRegionEndListener
    {
        private const string PanelName = "Anchor Workers";
        private const string WorkerResourcePath = "Worker";

        private static readonly ResourceCost[] s_workerCost = { new(ResourceType.Souls, 25) };
        private static readonly Vector2[] s_spawnOffsets =
        {
            new(0f, -2.25f), new(2.25f, 0f), new(-2.25f, 0f), new(0f, 2.25f),
            new(2.1f, -2.1f), new(-2.1f, -2.1f), new(2.1f, 2.1f), new(-2.1f, 2.1f),
        };

        private Label _status;
        private Button _summon;
        private Button _summonKing;
        private float _nextRefresh;
        private GameObject _workerPrefab;

        public static RegionWorkers Current { get; private set; }

        /// <summary>The panel's "Summon Demon King" button (null until the panel is built).</summary>
        public Button SummonKingButton => _summonKing;

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        // Icons first (docs/ui-rules.md): the worker count beside a worker, and two icon buttons with their Soul costs.
        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            var box = new VisualElement { style = { position = Position.Absolute, right = 4, top = 4, alignItems = Align.FlexEnd } };
            box.AddToClassList("isekai-panel");
            var count = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            Sprite anchorIcon = GameIcons.Of(FindAnchor() != null ? FindAnchor().Asset : null);
            if (anchorIcon != null) count.Add(GameStyle.Icon(anchorIcon, "large"));
            count.Add(GameStyle.Icon(WorkerIcon()));
            _status = GameStyle.Label(string.Empty, TextRole.Body);
            _status.AddToClassList("isekai-number");
            count.Add(_status);
            count.AddTooltip(TooltipText.Get(TooltipKey.HudWorkers), "Anchor");
            box.Add(count);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2 } };
            var worker = new IconButton(WorkerIcon(), "Summon worker", () => TrySummonWorker()) { Tooltip = TooltipText.Get(TooltipKey.HudWorkers) };
            _summon = worker;
            buttons.Add(worker);
            var king = new IconButton(GameIcons.Of(Resources.Load<GameObject>(RegionPossession.KingPrefab)), "Summon Demon King",
                () => RegionPossession.Current?.TrySummonDemonKing()) { Tooltip = TooltipText.Get(TooltipKey.HudSummonKing) };
            king.style.marginLeft = 2;
            _summonKing = king;
            buttons.Add(king);
            box.Add(buttons);
            root.Add(box);
            UIPanels.Created(panel);
            Refresh();
        }

        private Sprite WorkerIcon() => GameIcons.Worker != null ? GameIcons.Worker : GameIcons.Of(WorkerPrefab());

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        public bool TrySummonWorker()
        {
            Building anchor = FindAnchor();
            if (anchor == null) return false;
            GameObject prefab = WorkerPrefab();
            if (prefab == null) return false;
            Stockpile stockpile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (stockpile == null || !stockpile.TrySpend(s_workerCost)) return false;
            GameObject worker = Instantiate(prefab, SpawnAt(anchor), Quaternion.identity);
            if (!worker.TryGetComponent(out WorkerBrain _)) worker.AddComponent<WorkerBrain>();
            Refresh();
            return true;
        }

        public static Building FindAnchor()
        {
            foreach (Building building in Building.All)
                if (building != null && building.IsMainBase && building.IsPlayers && building.IsAlive && building.IsBuilt) return building;
            return null;
        }

        private GameObject WorkerPrefab()
        {
            if (_workerPrefab == null) _workerPrefab = Resources.Load<GameObject>(WorkerResourcePath);
            return _workerPrefab;
        }

        private void Refresh()
        {
            if (_status == null || _summon == null) return;
            Building anchor = FindAnchor();
            Stockpile stockpile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            GameObject prefab = WorkerPrefab();

            bool canSummon = anchor != null && prefab != null && stockpile != null && stockpile.CanAfford(s_workerCost);
            ((IconButton)_summon).SetCosts(s_workerCost, stockpile);
            _summon.SetEnabled(canSummon);
            if (_summonKing != null)
            {
                ((IconButton)_summonKing).SetCosts(KingCost(), stockpile);
                _summonKing.SetEnabled(anchor != null);   // too few Souls: the click is refused and says so
            }
            string count = prefab == null || anchor == null || stockpile == null ? "-" : WorkerBrain.All.Count.ToString();
            if (_status.text != count) _status.text = count;
        }

        private static ResourceCost[] KingCost()
        {
            RegionSouls souls = RegionSouls.Current;
            return new[] { new ResourceCost(ResourceType.Souls, SoulRules.SummonKingCost(souls != null ? souls.Ledger.Level : SoulTuning.StartingLevel)) };
        }

        private static Vector3 SpawnAt(Building anchor)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return anchor.transform.position + Vector3.down * 2.25f;
            for (int i = 0; i < s_spawnOffsets.Length; i++)
            {
                Vector3 world = anchor.transform.position + (Vector3)s_spawnOffsets[i];
                Int2 cell = grid.CellAt(world);
                if (!grid.Contains(cell) || !grid.IsWalkable(cell)) continue;
                return grid.CentreOf(cell);
            }
            return anchor.transform.position + Vector3.down * 2.25f;
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end == RegionEnd.Defeated) enabled = false;
        }
    }
}
