using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>Anchor panel and worker summoning for this Region.</summary>
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

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            var box = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    right = 6,
                    top = 6,
                    width = 280,
                    backgroundColor = new Color(0f, 0f, 0f, 0.6f),
                    borderBottomColor = GameStyle.Border,
                    borderBottomWidth = 1,
                    borderTopColor = GameStyle.Border,
                    borderTopWidth = 1,
                    borderLeftColor = GameStyle.Border,
                    borderLeftWidth = 1,
                    borderRightColor = GameStyle.Border,
                    borderRightWidth = 1,
                    paddingBottom = 6,
                    paddingTop = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                },
            };
            box.Add(GameStyle.Label("Anchor", TextRole.Title));
            _status = GameStyle.Label(string.Empty, TextRole.Muted);
            _status.style.marginTop = 2;
            _status.AddTooltip(TooltipText.Get(TooltipKey.HudWorkers));
            box.Add(_status);
            _summon = new Button(() => TrySummonWorker()) { text = SummonText() };
            _summon.style.marginTop = 6;
            _summon.AddTooltip(TooltipText.Get(TooltipKey.HudWorkers));
            box.Add(_summon);
            _summonKing = new Button(() => RegionPossession.Current?.TrySummonDemonKing()) { text = SummonKingText() };
            _summonKing.style.marginTop = 4;
            _summonKing.AddTooltip(TooltipText.Get(TooltipKey.HudSummonKing));
            box.Add(_summonKing);
            root.Add(box);
            UIPanels.Created(panel);
            Refresh();
        }

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
            _summon.text = SummonText();
            _summon.SetEnabled(canSummon);
            if (_summonKing != null)
            {
                _summonKing.text = SummonKingText();
                _summonKing.SetEnabled(anchor != null);   // too few Souls: the click is refused and says so
            }

            if (prefab == null)
            {
                _status.text = "Worker prefab missing.";
                return;
            }

            if (anchor == null)
            {
                _status.text = "No Anchor available.";
                return;
            }

            if (stockpile == null)
            {
                _status.text = "No stockpile.";
                return;
            }

            _status.text = $"Workers: {WorkerBrain.All.Count}";
        }

        private static string SummonKingText()
        {
            RegionSouls souls = RegionSouls.Current;
            int cost = SoulRules.SummonKingCost(souls != null ? souls.Ledger.Level : SoulTuning.StartingLevel);
            return $"Summon Demon King ({cost} Souls)";
        }

        private static string SummonText() => $"Summon worker ({s_workerCost[0].Amount} {s_workerCost[0].Type})";

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
