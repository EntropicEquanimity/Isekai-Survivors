using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Turns what the Region already announces into <see cref="Feedback"/> cues, so no gameplay file raises cues itself:
    /// deaths and hits (<see cref="RegionEntities"/>), the Character's body (wisp, revive, possession), demons arriving,
    /// Buildings placed and finished, mining, Souls gained, the corruption layer, the Region's end, and what the
    /// Vigilance director and the alarms count (Waves, stages, alarms, riders) are checked each frame. Nothing is raised
    /// while the Region fills up, so its first demons and corrupted ground make no noise.
    /// </summary>
    [RegionSystem]
    public sealed class FeedbackRaiser : MonoBehaviour
    {
        private const float CorruptionEvery = 0.5f;

        private bool _ready;
        private bool _wasWisp;
        private Stockpile _stockpile;
        private int _souls;
        private CellLayer _corruption;
        private float _corruptionAt = -100f;

        private VigilanceDirector _director;
        private VigilanceStage _stage;
        private int _waves;
        private RegionAlarms _alarms;
        private int _alarmsRaised;
        private int _ridersArrived;

        private void OnEnable()
        {
            RegionEntities.Died += OnDied;
            RegionEntities.Entered += OnEntered;
            RegionEntities.Damaged += OnDamaged;
            Player.Changed += OnPlayerChanged;
            ResourceNode.Taken += OnMined;
            RegionEvents.Ended += OnEnded;
        }

        private void OnDisable()
        {
            RegionEntities.Died -= OnDied;
            RegionEntities.Entered -= OnEntered;
            RegionEntities.Damaged -= OnDamaged;
            Player.Changed -= OnPlayerChanged;
            ResourceNode.Taken -= OnMined;
            RegionEvents.Ended -= OnEnded;
            if (_stockpile != null) _stockpile.Changed -= OnStockpile;
            if (_corruption != null) _corruption.Changed -= OnCorruption;
            if (RegionBuildings.Current != null) RegionBuildings.Current.Placed -= OnPlaced;
            if (RegionBootstrap.Current != null) RegionBootstrap.Current.Populated -= OnPopulated;
        }

        private void Start()
        {
            if (RegionBuildings.Current != null) RegionBuildings.Current.Placed += OnPlaced;
            if (RegionEconomy.Current != null)
            {
                _stockpile = RegionEconomy.Current.Stockpile;
                _souls = _stockpile.Get(ResourceType.Souls);
                _stockpile.Changed += OnStockpile;
            }
            RegionGrid grid = RegionGrid.Current;
            if (grid != null)
            {
                _corruption = grid.Layer(RegionCorruption.LayerName);
                _corruption.Changed += OnCorruption;
            }
            RegionBootstrap bootstrap = RegionBootstrap.Current;
            if (bootstrap == null) _ready = true;
            else if (bootstrap.IsPopulated) _ready = true;
            else bootstrap.Populated += OnPopulated;
        }

        private void OnPopulated() => _ready = true;

        private static Vector3 Where(RegionEntity unit) => unit.GameObject.transform.position;

        private void OnDied(UnitDeath death)
        {
            if (!_ready) return;
            if (death.Kind == UnitKind.Character && death.Side == Side.Yours)
            {
                _wasWisp = true;
                Feedback.Raise(Cue.Wisp, Where(death.Unit));
                return;
            }
            Feedback.Raise(Cue.Death, Where(death.Unit));
        }

        private void OnEntered(RegionEntity unit)
        {
            if (!_ready || unit.Side != Side.Yours) return;
            if (unit.Kind == UnitKind.Character && _wasWisp)
            {
                _wasWisp = false;
                Feedback.Raise(Cue.Revive, Where(unit));
            }
            else if (unit.Kind == UnitKind.Demon) Feedback.Raise(Cue.Summon, Where(unit));
        }

        private void OnDamaged(RegionEntity unit, DamageResult result)
        {
            // Construction wears a site down with no attacker: that is not a hit.
            if (!_ready || result.Info?.Source == null || result.Dealt <= 0f) return;
            Feedback.Raise(Cue.Hit, Where(unit));
        }

        private void OnPlayerChanged(CharacterControl body)
        {
            if (!_ready || body == null) return;
            _wasWisp = false;
            Feedback.Raise(Cue.Possess, body.transform.position);
        }

        private void OnPlaced(Building building)
        {
            building.Completed += OnBuilt;
            Feedback.Raise(Cue.BuildPlaced, building.transform.position);
        }

        private void OnBuilt(Building building)
        {
            building.Completed -= OnBuilt;
            Feedback.Raise(Cue.BuildDone, building.transform.position);
        }

        private void OnMined(ResourceNode node, int units) => Feedback.Raise(Cue.Mined, node.transform.position);

        private void OnStockpile(ResourceType type, int amount)
        {
            if (type != ResourceType.Souls) return;
            bool gained = amount > _souls;
            _souls = amount;
            if (!gained || !_ready) return;
            CharacterControl jack = Player.Character;
            RegionSouls souls = RegionSouls.Current;
            if (souls != null && souls.Wisp != null) Feedback.Raise(Cue.SoulsGained, souls.Wisp.transform.position);
            else if (jack != null) Feedback.Raise(Cue.SoulsGained, jack.transform.position);
            else Feedback.Raise(Cue.SoulsGained);
        }

        // The corruption spreading: thinned out, because one Anchor's spread changes many cells.
        private void OnCorruption(Int2 cell, byte before, byte after)
        {
            if (!_ready || after <= before || Time.unscaledTime - _corruptionAt < CorruptionEvery) return;
            _corruptionAt = Time.unscaledTime;
            Feedback.Raise(Cue.CorruptionSpread, RegionGrid.Current.CentreOf(cell));
        }

        private void OnEnded(RegionEnd end)
        {
            if (end == RegionEnd.Captured) Feedback.Raise(Cue.Capture);
        }

        // What has no event is read once a frame: the Vigilance stage, Waves sent, alarms raised, riders arrived.
        private void Update()
        {
            VigilanceDirector director = VigilanceDirector.Current;
            if (director != _director)
            {
                _director = director;
                if (director != null) { _stage = director.Stage; _waves = director.WavesSent; }
            }
            else if (director != null)
            {
                if (director.Stage > _stage && _ready) Feedback.Raise(Cue.VigilanceStage);
                _stage = director.Stage;
                if (director.WavesSent > _waves && _ready)
                {
                    if (director.LastWave != null) Feedback.Raise(Cue.Wave, director.LastWave.Origin);
                    else Feedback.Raise(Cue.Wave);
                }
                _waves = director.WavesSent;
            }

            RegionAlarms alarms = RegionAlarms.Current;
            if (alarms != _alarms)
            {
                _alarms = alarms;
                if (alarms != null) { _alarmsRaised = alarms.AlarmsRaised; _ridersArrived = alarms.RidersArrived; }
                return;
            }
            if (alarms == null) return;
            if (alarms.AlarmsRaised > _alarmsRaised && _ready)
            {
                int n = alarms.Riders.Count;
                if (n > 0) Feedback.Raise(Cue.Alarm, alarms.Riders[n - 1].transform.position);
                else Feedback.Raise(Cue.Alarm);
            }
            _alarmsRaised = alarms.AlarmsRaised;
            if (alarms.RidersArrived > _ridersArrived && _ready) RaiseRider();
            _ridersArrived = alarms.RidersArrived;
        }

        // A rider reached the Seat: it happens at the Seat.
        private static void RaiseRider()
        {
            RegionSettlements.Settlement seat = RegionSettlements.Current != null ? RegionSettlements.Current.Seat : null;
            if (seat != null)
                foreach (Building b in seat.Buildings)
                    if (b != null) { Feedback.Raise(Cue.Rider, b.transform.position); return; }
            Feedback.Raise(Cue.Rider);
        }
    }
}
