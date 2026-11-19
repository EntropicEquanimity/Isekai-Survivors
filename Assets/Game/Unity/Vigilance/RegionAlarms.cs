using System.Collections.Generic;
using Blondie.Core;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>Who can be spotted to raise Vigilance.</summary>
    public enum SpotTarget
    {
        Jack,
        /// <summary>The Anchor: ticket #23 raises this when mortals spot it (see <see cref="RegionAlarms.NoteSpotted"/>).</summary>
        Anchor,
    }

    /// <summary>
    /// What mortals notice and report. A mortal that sees five or more bone piles is a witness: a few seconds later it
    /// marks its area alerted and a rider (a Lancer, <see cref="AlarmRider"/>) sets off for the Seat. Kill the witness
    /// in those seconds and the alarm never goes out; kill the rider and Vigilance does not rise, but a rider that
    /// arrives raises it a full stage. Peasants fleeing to a stronghold alert it. Mortals that see Jack raise Vigilance,
    /// once in a while (<see cref="SpotLimiter"/>), and <see cref="Witnessed"/> tells the director whether a kill was seen.
    /// <para>"Sees" is what <see cref="MortalSenses"/> uses: within the mortals' sight range of the thing, on ground the
    /// Mortals' side sees (<see cref="RegionVision"/>, so walls block it and night shortens it).</para>
    /// </summary>
    [RegionSystem]
    public sealed class RegionAlarms : MonoBehaviour
    {
        private struct Witness
        {
            public MortalBrain Brain;
            public float SendAt;
        }

        private const float ScanSeconds = 0.5f;
        private const float SpotScanSeconds = 0.25f;

        private readonly List<Witness> _pending = new();
        private readonly List<AlarmRider> _riders = new();
        private readonly HashSet<GameObject> _riderObjects = new();
        private readonly AlertedAreas _alerted = new();
        private readonly Dictionary<SpotTarget, SpotLimiter> _limiters = new();
        private GameObject _riderPrefab;
        private int _cursor;
        private float _nextSpotScan;
        private float _jackSeenFor;

        public static RegionAlarms Current { get; private set; }

        /// <summary>Witnesses waiting to send their rider.</summary>
        public int PendingWitnesses => _pending.Count;

        /// <summary>Riders on the road now.</summary>
        public IReadOnlyList<AlarmRider> Riders => _riders;

        public int AlarmsRaised { get; private set; }
        public int AlarmsStopped { get; private set; }
        public int RidersSent { get; private set; }
        public int RidersArrived { get; private set; }
        public int RidersKilled { get; private set; }
        public int StrongholdsAlerted { get; private set; }
        public int SpottedRaises { get; private set; }

        private void OnEnable()
        {
            Current = this;
            foreach (SpotTarget t in System.Enum.GetValues(typeof(SpotTarget)))
                _limiters[t] = new SpotLimiter(AlarmTuning.SpottedCooldownSeconds);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            _pending.Clear();
            _riders.Clear();
            _riderObjects.Clear();
            _alerted.Clear();
        }

        // ---- Alerted areas ----

        /// <summary>Whether mortals at <paramref name="world"/> are in an area already alerted.</summary>
        public bool IsAlerted(Vector3 world) => RegionGrid.Current != null && _alerted.Contains(RegionGrid.Current.CellAt(world), Time.time);

        /// <summary>A fleeing peasant reached <paramref name="stronghold"/>: the mortals there are alerted (the Alarm factor goes on).</summary>
        public void AlertStronghold(RegionSettlements.Settlement stronghold)
        {
            RegionMortals mortals = RegionMortals.Current;
            RegionGrid grid = RegionGrid.Current;
            if (mortals == null || mortals.Homes == null || grid == null) return;
            Vector3 centre = mortals.Homes.Centre(stronghold);
            if (_alerted.Contains(grid.CellAt(centre), Time.time)) return;
            Mark(centre, AlarmTuning.StrongholdAlertRadiusCells);
            StrongholdsAlerted++;
        }

        private void Mark(Vector3 at, int radiusCells)
        {
            _alerted.Mark(RegionGrid.Current.CellAt(at), radiusCells, Time.time + AlarmTuning.AlertSeconds);
            RegionMortals.Current?.RaiseAlarm(at, radiusCells, AlarmTuning.AlarmFactorSeconds);
        }

        // ---- Seeing bones ----

        /// <summary>How many bone piles <paramref name="mortal"/> sees now (counting stops at the number that raises the alarm).</summary>
        public int PilesSeenBy(MortalBrain mortal)
        {
            RegionBones bones = RegionBones.Current;
            RegionMortals mortals = RegionMortals.Current;
            if (bones == null || mortals == null || mortals.Grid == null) return 0;
            RegionVision vision = RegionVision.IsOn ? RegionVision.Current : null;
            bool checkSight = vision != null && vision.IsReady;
            float range = mortals.SightCells * mortals.CellSize;
            Vector3 p = mortal.transform.position;
            int seen = 0;
            IReadOnlyList<BonePile> piles = bones.Piles;
            for (int i = 0; i < piles.Count && !AlarmRules.Raises(seen); i++)
            {
                Vector3 q = piles[i].transform.position;
                float dx = q.x - p.x, dy = q.y - p.y;
                if (dx * dx + dy * dy > range * range) continue;
                if (checkSight && !vision.CanSee(Side.Mortals, piles[i].Cell)) continue;
                seen++;
            }
            return seen;
        }

        private void Update()
        {
            RegionMortals mortals = RegionMortals.Current;
            if (mortals == null || mortals.Grid == null) return;
            float now = Time.time;
            ScanForWitnesses(mortals, now);
            SendDueRiders(now);
            if (now >= _nextSpotScan)
            {
                _nextSpotScan = now + SpotScanSeconds;
                ScanForJack(mortals);
            }
        }

        // A share of the mortals look at the bones each frame, so all of them do twice a second.
        private void ScanForWitnesses(RegionMortals mortals, float now)
        {
            IReadOnlyList<MortalBrain> brains = mortals.Brains;
            int n = brains.Count;
            if (n == 0 || RegionBones.Current == null || RegionBones.Current.Count < AlarmTuning.PilesToAlarm) return;
            int share = Mathf.CeilToInt(n * Time.deltaTime / ScanSeconds);
            for (int k = 0; k < share && n > 0; k++)
            {
                if (_cursor >= brains.Count) _cursor = 0;
                MortalBrain brain = brains[_cursor++];
                if (!CanWitness(brain, now) || !AlarmRules.Raises(PilesSeenBy(brain))) continue;
                _pending.Add(new Witness { Brain = brain, SendAt = now + AlarmTuning.WitnessDelaySeconds });
            }
        }

        private bool CanWitness(MortalBrain brain, float now)
        {
            if (!brain.Member.IsAlive || _riderObjects.Contains(brain.gameObject)) return false;
            foreach (Witness w in _pending)
                if (w.Brain == brain) return false;
            return !_alerted.Contains(RegionGrid.Current.CellAt(brain.transform.position), now);
        }

        // A witness that dies (or is gone) before its time never sends the rider: that alarm is stopped.
        private void SendDueRiders(float now)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Witness w = _pending[i];
                bool alive = w.Brain != null && w.Brain.isActiveAndEnabled && w.Brain.Member.IsAlive;
                if (alive && now < w.SendAt) continue;
                _pending.RemoveAt(i);
                if (!alive) { AlarmsStopped++; continue; }
                Vector3 at = w.Brain.transform.position;
                if (_alerted.Contains(RegionGrid.Current.CellAt(at), now)) continue;   // another witness already sent it
                Mark(at, AlarmTuning.AlertRadiusCells);
                AlarmsRaised++;
                SendRider(at);
            }
        }

        // ---- Riders ----

        /// <summary>Sends a rider from <paramref name="from"/> to the Seat. Null when the Region has no Seat or no rider prefab.</summary>
        public AlarmRider SendRider(Vector3 from)
        {
            RegionSettlements.Settlement seat = RegionSettlements.Current != null ? RegionSettlements.Current.Seat : null;
            if (seat == null) return null;
            if (_riderPrefab == null) _riderPrefab = Resources.Load<MortalRoster>(MortalRoster.ResourceName)?.PrefabOf(MortalType.Lancer);
            if (_riderPrefab == null) return null;
            GameObject go = Pools.Spawn(_riderPrefab, from, Quaternion.identity);
            if (!go.TryGetComponent(out AlarmRider rider)) rider = go.AddComponent<AlarmRider>();
            _riders.Add(rider);
            _riderObjects.Add(go);
            RidersSent++;
            rider.Launch(this, seat);
            return rider;
        }

        internal void RiderEnded(AlarmRider rider, bool arrived)
        {
            _riders.Remove(rider);
            _riderObjects.Remove(rider.gameObject);
            if (arrived)
            {
                RidersArrived++;
                VigilanceDirector.Current?.RiderReachedSeat();
            }
            else RidersKilled++;
        }

        // ---- Being seen ----

        /// <summary>
        /// Something was spotted: raises Vigilance, at most once per cooldown for each target (so standing in sight is
        /// not a raise per frame). Jack is spotted here; ticket #23 calls this with <see cref="SpotTarget.Anchor"/>.
        /// </summary>
        public bool NoteSpotted(SpotTarget target)
        {
            VigilanceDirector director = VigilanceDirector.Current;
            if (director == null || !_limiters[target].TryRaise(Time.time)) return false;
            SpottedRaises++;
            director.Spotted();
            return true;
        }

        // A settlement's mortal that has had Jack in sight for a moment has spotted him.
        private void ScanForJack(RegionMortals mortals)
        {
            CharacterControl jack = Player.Character;
            bool seen = jack != null && !jack.IsDead && AnyMortalSees(mortals, jack.transform.position);
            if (!seen) { _jackSeenFor = 0f; return; }
            _jackSeenFor += SpotScanSeconds;
            if (_jackSeenFor >= AlarmTuning.SpotConfirmSeconds && NoteSpotted(SpotTarget.Jack)) _jackSeenFor = 0f;
        }

        private static bool AnyMortalSees(RegionMortals mortals, Vector3 at)
        {
            RegionVision vision = RegionVision.IsOn ? RegionVision.Current : null;
            if (vision != null && vision.IsReady && !vision.CanSee(Side.Mortals, at)) return false;
            float range = mortals.SightCells * mortals.CellSize;
            IReadOnlyList<MortalBrain> brains = mortals.Brains;
            for (int i = 0; i < brains.Count; i++)
            {
                MortalBrain b = brains[i];
                if (b.Settlement == null || !b.Member.IsAlive) continue;
                Vector3 p = b.transform.position;
                if ((p.x - at.x) * (p.x - at.x) + (p.y - at.y) * (p.y - at.y) <= range * range) return true;
            }
            return false;
        }

        // ---- Witnessed kills ----

        /// <summary>
        /// Whether a mortal saw <paramref name="death"/>: another living mortal within sight range of where it fell, on
        /// ground the Mortals' side sees. The kill still goes through the one <see cref="RegionEntities.Died"/> event;
        /// the director only asks this when it counts it.
        /// </summary>
        public static bool Witnessed(UnitDeath death)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return false;
            RegionVision vision = RegionVision.IsOn ? RegionVision.Current : null;
            if (vision != null && vision.IsReady && !vision.CanSee(Side.Mortals, death.Cell)) return false;
            int sight = RegionMortals.Current != null ? RegionMortals.Current.SightCells : VisionRanges.Mortal;
            float range = sight * grid.CellSize;
            Vector3 at = grid.CentreOf(death.Cell);
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity other = living[i];
                if (other == death.Unit || other.Kind != UnitKind.Mortal || other.Side != Side.Mortals) continue;
                Vector3 p = other.GameObject.transform.position;
                if ((p.x - at.x) * (p.x - at.x) + (p.y - at.y) * (p.y - at.y) <= range * range) return true;
            }
            return false;
        }
    }
}
