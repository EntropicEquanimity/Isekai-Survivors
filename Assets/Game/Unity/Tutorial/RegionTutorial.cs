using System.Collections.Generic;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Runs the guided first Region tutorial in play: prompts, pointers, step progression from gameplay events, and
    /// skip/finish handling.
    /// </summary>
    [RegionSystem]
    public sealed class RegionTutorial : MonoBehaviour, IRegionEndListener
    {
        private const string MarkerObject = "Tutorial Marker";

        private readonly TutorialProgress _progress = new();
        private VisualElement _box;
        private Label _step;
        private Label _prompt;
        private VisualElement _checks;
        private readonly List<(VisualElement row, VisualElement box, Label label)> _checkRows = new();
        private Button _skip;
        private GameObject _marker;
        private SpriteRenderer _markerRenderer;
        private UnitProducer _portal;
        private RegionAlarms _alarms;
        private Vector3 _start;
        private Vector3 _witnessSpot;
        private bool _sawWisp;
        private int _shownStep = -1;
        private int _ridersSentAtStep;
        private int _ridersKilledAtStep;
        private int _ridersArrivedAtStep;
        private bool _listening;
        private bool _portalHooked;
        private bool _buildingsHooked;
        private bool _corruptionHooked;
        private CellLayer _corruption;

        public bool IsRunning => TutorialRun.IsActive && !_progress.IsComplete;
        public int StepIndex => _progress.StepIndex;
        public TutorialStepId? CurrentStep => _progress.IsComplete ? null : _progress.Current.Id;
        public bool HasFact(TutorialFact fact) => _progress.Has(fact);

        private void OnEnable()
        {
            if (!IsTutorialRegion())
            {
                enabled = false;
                return;
            }

            RegionEntities.Died += OnDied;
            ResourceNode.Taken += OnTaken;
            Feedback.Raised += OnCue;
            RegionEvents.Ended += OnEnded;
            EnsureBuildingHook();
            _listening = true;
        }

        private void Start()
        {
            if (!IsTutorialRegion())
            {
                enabled = false;
                return;
            }

            _start = Player.Character != null ? Player.Character.transform.position : Vector3.zero;
            BuildPanel();
            BuildMarker();
            EnsureTutorialSetup();
            EnsureBuildingHook();
            EnsureCorruptionHook();
            RefreshStep(force: true);
            DebugActions.Button(this, "Run", "Skip tutorial", SkipTutorial);
            DebugActions.Info(this, "Run", Readout);
        }

        private void OnDisable()
        {
            if (_listening)
            {
                RegionEntities.Died -= OnDied;
                ResourceNode.Taken -= OnTaken;
                Feedback.Raised -= OnCue;
                RegionEvents.Ended -= OnEnded;
                if (_buildingsHooked && RegionBuildings.Current != null) RegionBuildings.Current.Placed -= OnPlaced;
                _buildingsHooked = false;
                if (_corruptionHooked && _corruption != null) _corruption.Changed -= OnCorruptionChanged;
                _corruptionHooked = false;
                _corruption = null;
                _listening = false;
            }

            if (_portalHooked && _portal != null) _portal.Changed -= OnPortalChanged;
            _portalHooked = false;
            if (_marker != null) Destroy(_marker);
            DebugActions.Remove(this);
        }

        private void Update()
        {
            if (!TutorialRun.IsActive)
            {
                HideTutorial();
                enabled = false;
                return;
            }

            EnsureBuildingHook();
            EnsureCorruptionHook();
            ObserveState();
            TickMarker();
            RefreshStep(force: false);
            RefreshChecks();
        }

        // One row per thing a step waits for: a small box, filled once it is done, and its name.
        private void RefreshChecks()
        {
            if (_checks == null) return;
            IReadOnlyList<(string Label, bool Done)> list = _progress.Checklist();
            while (_checkRows.Count < list.Count)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var box = new VisualElement();
                box.AddToClassList("isekai-pip");
                row.Add(box);
                Label label = GameStyle.Label(string.Empty, TextRole.Body);
                row.Add(label);
                _checks.Add(row);
                _checkRows.Add((row, box, label));
            }
            for (int i = 0; i < _checkRows.Count; i++)
            {
                (VisualElement row, VisualElement box, Label label) = _checkRows[i];
                row.style.display = i < list.Count ? DisplayStyle.Flex : DisplayStyle.None;
                if (i >= list.Count) continue;
                if (label.text != list[i].Label) label.text = list[i].Label;
                box.EnableInClassList("isekai-pip--on", list[i].Done);
                label.style.opacity = list[i].Done ? 0.55f : 1f;
            }
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end == RegionEnd.Captured) Note(TutorialFact.SeatDestroyed);
        }

        private void ObserveState()
        {
            if (_portal == null)
            {
                foreach (Building building in Building.All)
                {
                    if (building == null || !building.IsPlayers || !building.IsAlive || !building.TryGetComponent(out UnitProducer producer)) continue;
                    _portal = producer;
                    HookPortal();
                    break;
                }
            }
            if (_portal != null)
            {
                if (_portal.Order != PortalOrder.Guard) Note(TutorialFact.PortalOrderSet);
                if (_portal.HasRally) Note(TutorialFact.PortalRallySet);
            }

            CharacterControl jack = Player.Character;
            if (jack != null && Vector2.Distance(jack.transform.position, _start) >= 4f) Note(TutorialFact.Moved);

            VigilanceDirector vigilance = VigilanceDirector.Current;
            if (vigilance != null && (vigilance.Stage > VigilanceStage.Unaware || vigilance.Points > 0f)) Note(TutorialFact.VigilanceRaised);

            _alarms ??= RegionAlarms.Current;
            if (_alarms != null)
            {
                if (_alarms.RidersSent > _ridersSentAtStep) Note(TutorialFact.RiderSent);
                if (_alarms.RidersKilled > _ridersKilledAtStep && _alarms.RidersArrived <= _ridersArrivedAtStep) Note(TutorialFact.RiderStopped);
            }

            RegionSouls souls = RegionSouls.Current;
            if (souls == null) return;
            if (souls.IsBodiless && !_sawWisp)
            {
                _sawWisp = true;
                Note(TutorialFact.BecameWisp);
            }
            else if (_sawWisp && !souls.IsBodiless) Note(TutorialFact.ReturnedFromWisp);
        }

        private void OnDied(UnitDeath death)
        {
            if (death.KillerSide == Side.Yours && (death.Kind == UnitKind.Mortal || death.Kind == UnitKind.Wildlife)) Note(TutorialFact.Fought);
        }

        private void OnTaken(ResourceNode node, int units)
        {
            if (node == null || units <= 0) return;
            if (node.Type == ResourceType.Stone) Note(TutorialFact.MinedStone);
            if (node.Type == ResourceType.Wood) Note(TutorialFact.MinedWood);
        }

        private void OnCue(FeedbackCue cue)
        {
            if (cue.Kind == Cue.CorruptionSpread) Note(TutorialFact.CorruptionSpread);
        }

        private void OnCorruptionChanged(Int2 cell, byte before, byte after)
        {
            if (after > before) Note(TutorialFact.CorruptionSpread);
        }

        private void OnPlaced(Building building)
        {
            if (building == null || !building.TryGetComponent(out UnitProducer producer)) return;
            _portal = producer;
            HookPortal();
            Note(TutorialFact.PortalBuilt);
        }

        private void OnPortalChanged()
        {
            if (_portal == null) return;
            if (_portal.Order != PortalOrder.Guard) Note(TutorialFact.PortalOrderSet);
            if (_portal.HasRally) Note(TutorialFact.PortalRallySet);
        }

        private void OnEnded(RegionEnd end)
        {
            if (end == RegionEnd.Captured) Note(TutorialFact.SeatDestroyed);
        }

        private void Note(TutorialFact fact)
        {
            if (!_progress.Note(fact)) return;
            RefreshStep(force: true);
            if (_progress.IsComplete)
            {
                TutorialRun.Complete();
                if (_prompt != null) _prompt.text = "Tutorial complete.";
                if (_skip != null) _skip.SetEnabled(false);
            }
        }

        private void RefreshStep(bool force)
        {
            if (_progress.IsComplete || _step == null || _prompt == null) return;
            int index = _progress.StepIndex;
            if (!force && index == _shownStep) return;
            _shownStep = index;
            TutorialStepDef now = _progress.Current;
            _step.text = $"{index + 1}/{TutorialSteps.Count}";
            _prompt.text = now.Prompt.Replace("{body}", BodyKey.Name);   // the key as it is bound now
            CaptureRiderBaselines(now.Id);
        }

        private void CaptureRiderBaselines(TutorialStepId step)
        {
            if (step != TutorialStepId.StopWitnessRider || _alarms == null) return;
            _ridersSentAtStep = _alarms.RidersSent;
            _ridersKilledAtStep = _alarms.RidersKilled;
            _ridersArrivedAtStep = _alarms.RidersArrived;
        }

        private void SkipTutorial()
        {
            if (!TutorialRun.Skip()) return;
            HideTutorial();
            enabled = false;
        }

        private void BuildPanel()
        {
            var box = new VisualElement { style = { maxWidth = 420 } };
            box.AddToClassList("isekai-panel");
            _step = GameStyle.Label(string.Empty, TextRole.Muted);
            _prompt = GameStyle.Label(string.Empty, TextRole.Body);
            _prompt.style.whiteSpace = WhiteSpace.Normal;
            _checks = new VisualElement { style = { marginBottom = 4 } };
            _skip = new Button(SkipTutorial) { text = "Skip tutorial" };
            box.Add(_step);
            box.Add(_prompt);
            box.Add(_checks);
            box.Add(_skip);
            _box = box;
            HudStack.Add(HudCorner.TopLeft, box, 1);
        }

        private void BuildMarker()
        {
            _marker = new GameObject(MarkerObject);
            _markerRenderer = _marker.AddComponent<SpriteRenderer>();
            _markerRenderer.sprite = BuildSprite();
            _markerRenderer.color = new Color(1f, 0.84f, 0.28f, 0.95f);
            _markerRenderer.sortingOrder = 5000;
        }

        private static Sprite BuildSprite()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color clear = new(0f, 0f, 0f, 0f);
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                texture.SetPixel(x, y, clear);
            for (int i = 1; i < 7; i++)
            {
                texture.SetPixel(3, i, Color.white);
                texture.SetPixel(4, i, Color.white);
                texture.SetPixel(i, 1, Color.white);
                texture.SetPixel(i, 2, Color.white);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0f), 16f);
        }

        private void TickMarker()
        {
            if (_marker == null) return;
            if (!TryMarkerTarget(out Vector3 at))
            {
                _marker.SetActive(false);
                return;
            }

            _marker.SetActive(true);
            float bob = 1.6f + Mathf.Sin(Time.unscaledTime * 4f) * 0.16f;
            _marker.transform.position = new Vector3(at.x, at.y + bob, 0f);
        }

        private bool TryMarkerTarget(out Vector3 at)
        {
            at = default;
            if (_progress.IsComplete) return false;
            TutorialStepId step = _progress.Current.Id;
            switch (step)
            {
                case TutorialStepId.MoveAndFight:
                    if (TryNearestMortal(out at)) return true;
                    return TryJack(out at);
                case TutorialStepId.MineStoneAndWood:
                    return TryNode(_progress.Has(TutorialFact.MinedStone) ? ResourceType.Wood : ResourceType.Stone, out at);
                case TutorialStepId.BuildPortalOnCorruption:
                    return TryAnchor(out at);
                case TutorialStepId.SetPortalOrderAndRally:
                    if (_portal != null) { at = _portal.transform.position; return true; }
                    return TryAnchor(out at);
                case TutorialStepId.WatchCorruptionSpread:
                    if (_portal != null) { at = _portal.transform.position; return true; }
                    return TryJack(out at);
                case TutorialStepId.LearnVigilance:
                    return TryNearestMortal(out at);
                case TutorialStepId.StopWitnessRider:
                    if (_alarms != null && _alarms.Riders.Count > 0 && _alarms.Riders[0] != null)
                    {
                        at = _alarms.Riders[0].transform.position;
                        return true;
                    }
                    if (_witnessSpot != default) { at = _witnessSpot; return true; }
                    return TryNearestMortal(out at);
                case TutorialStepId.RecoverFromDeath:
                    RegionSouls souls = RegionSouls.Current;
                    if (souls != null && souls.Wisp != null) { at = souls.Wisp.transform.position; return true; }
                    return TryAnchor(out at);
                case TutorialStepId.DestroySeat:
                    Building keep = RegionSettlements.Current?.Seat?.Keep;
                    if (keep != null) { at = keep.transform.position; return true; }
                    return false;
                default:
                    return false;
            }
        }

        private static bool TryNode(ResourceType type, out Vector3 at)
        {
            at = default;
            ResourceNode best = null;
            float bestSqr = float.MaxValue;
            Vector3 from = Player.Character != null ? Player.Character.transform.position : Vector3.zero;
            foreach (ResourceNode node in ResourceNode.All)
            {
                if (node == null || !node.isActiveAndEnabled || node.Type != type || node.IsEmpty) continue;
                float sqr = (node.transform.position - from).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = node;
            }
            if (best == null) return false;
            at = best.transform.position;
            return true;
        }

        private static bool TryNearestMortal(out Vector3 at)
        {
            at = default;
            RegionEntity best = null;
            float bestSqr = float.MaxValue;
            Vector3 from = Player.Character != null ? Player.Character.transform.position : Vector3.zero;
            foreach (RegionEntity entity in RegionEntities.Living)
            {
                if (entity.Side != Side.Mortals || entity.Kind != UnitKind.Mortal) continue;
                float sqr = (entity.GameObject.transform.position - from).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = entity;
            }
            if (best == null) return false;
            at = best.GameObject.transform.position;
            return true;
        }

        private static bool TryAnchor(out Vector3 at)
        {
            at = default;
            Building anchor = Building.All.FirstOrDefault(b => b != null && b.IsMainBase && b.IsPlayers && b.IsAlive);
            if (anchor == null) return false;
            at = anchor.transform.position;
            return true;
        }

        private static bool TryJack(out Vector3 at)
        {
            at = default;
            if (Player.Character == null) return false;
            at = Player.Character.transform.position;
            return true;
        }

        private void EnsureTutorialSetup()
        {
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (pile != null)
            {
                EnsureAtLeast(pile, ResourceType.Stone, 120);
                EnsureAtLeast(pile, ResourceType.Wood, 120);
                EnsureAtLeast(pile, ResourceType.Souls, 220);
            }

            Building keep = RegionSettlements.Current?.Seat?.Keep;
            if (keep != null && keep.IsAlive)
            {
                CombatUnit seat = keep.Damageable.Unit;
                seat.TakeDamage(seat.MaxHealth * 0.75f, DamageRules.TrueType);
            }

            RegionMortals mortals = RegionMortals.Current;
            RegionGrid grid = RegionGrid.Current;
            RegionBones bones = RegionBones.Current;
            CharacterControl jack = Player.Character;
            if (mortals == null || grid == null || bones == null || jack == null) return;
            if (!TrySpotNear(jack.transform.position, 14f, 26f, out Vector3 witnessAt)) return;

            RegionSettlements.Settlement home = RegionSettlements.Current?.Seat;
            MortalBrain witness = mortals.Spawn(MortalType.Peasant, home, witnessAt);
            if (witness != null) _witnessSpot = witness.transform.position;

            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Vector3 at = witnessAt + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 2.4f;
                if (grid.IsWalkable(at)) bones.Drop(at, BoneKind.Human);
            }

            if (TrySpotNear(jack.transform.position, 9f, 18f, out Vector3 guardAt)) mortals.Spawn(MortalType.Soldier, home, guardAt);
        }

        private static void EnsureAtLeast(Stockpile pile, ResourceType type, int want)
        {
            int now = pile.Get(type);
            if (now < want) pile.Add(type, want - now);
        }

        private static bool TrySpotNear(Vector3 centre, float minDistance, float maxDistance, out Vector3 spot)
        {
            spot = default;
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return false;
            for (int i = 0; i < 80; i++)
            {
                float angle = i * 0.62f;
                float radius = Mathf.Lerp(minDistance, maxDistance, (i % 10) / 10f);
                Vector3 at = centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                if (!grid.IsWalkable(at)) continue;
                spot = at;
                return true;
            }
            return false;
        }

        private void HookPortal()
        {
            if (_portal == null || _portalHooked) return;
            _portal.Changed += OnPortalChanged;
            _portalHooked = true;
            OnPortalChanged();
        }

        private void EnsureBuildingHook()
        {
            if (_buildingsHooked) return;
            RegionBuildings buildings = RegionBuildings.Current;
            if (buildings == null) return;
            buildings.Placed += OnPlaced;
            _buildingsHooked = true;
        }

        private void EnsureCorruptionHook()
        {
            if (_corruptionHooked || RegionGrid.Current == null) return;
            _corruption = RegionGrid.Current.Layer(RegionCorruption.LayerName);
            if (_corruption == null) return;
            _corruption.Changed += OnCorruptionChanged;
            _corruptionHooked = true;
        }

        private void HideTutorial()
        {
            if (_box != null) _box.style.display = DisplayStyle.None;   // the whole box, not an empty frame with its title
            if (_step != null) _step.style.display = DisplayStyle.None;
            if (_prompt != null) _prompt.style.display = DisplayStyle.None;
            if (_skip != null) _skip.style.display = DisplayStyle.None;
            if (_marker != null) _marker.SetActive(false);
        }

        private string Readout() =>
            TutorialRun.IsActive && !_progress.IsComplete
                ? $"Tutorial: {_progress.Current.Id} ({_progress.StepIndex + 1}/{TutorialSteps.Count})"
                : null;

        private static bool IsTutorialRegion()
        {
            if (!TutorialRun.IsActive) return false;
            RegionRecipe recipe = RegionBootstrap.Current?.Context?.Recipe;
            RegionRecipe want = TutorialRun.Recipe;
            return recipe != null && recipe.Seed == want.Seed && recipe.Width == want.Width && recipe.Height == want.Height;
        }
    }
}
