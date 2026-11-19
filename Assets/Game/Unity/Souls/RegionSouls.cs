using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using Blondie.Stats;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The soul economy in a Region. Killing mortals and wildlife gives Souls. When Jack's body dies he becomes a
    /// <see cref="Wisp"/> and Souls drain every second; touching the Anchor (the player's living Main Base) revives the
    /// body there for the revive cost; Souls reaching zero while bodiless loses the Run (<see cref="IsLost"/>, read by
    /// <see cref="RegionDefeat"/>). Losing the Anchor ends nothing: there is just no revive until another stands.
    /// Soul level (kept for the Run in <see cref="SoulRun"/>) is raised by spending Souls and raises the body's stats.
    /// </summary>
    [RegionSystem]
    public sealed class RegionSouls : MonoBehaviour
    {
        private static readonly object StatSource = new();
        private const float AnchorReach = 0.4f;

        private readonly SoulDrain _drain = new();
        private CharacterControl _appliedBody;
        private int _appliedLevel;
        private Wisp _wisp;
        private FollowCamera2D _camera;
        private bool _hooked;

        public static RegionSouls Current { get; private set; }

        /// <summary>Jack has no body: he is a wisp.</summary>
        public bool IsBodiless => _wisp != null;

        public Wisp Wisp => _wisp;

        /// <summary>What the camera follows now: the wisp, else the body.</summary>
        public Transform CameraTarget => _camera != null ? _camera.Target : null;

        /// <summary>Souls ran out while bodiless: the Run is lost.</summary>
        public bool IsLost { get; private set; }

        /// <summary>The wisp is near a living priest or on holy ground (the Seat's cells), which doubles the drain. Asked of the mortals each time.</summary>
        public bool DrainDoubled
        {
            get
            {
                RegionMortals mortals = RegionMortals.Current;
                if (_wisp == null || mortals == null) return false;
                Vector3 at = _wisp.transform.position;
                return mortals.IsHolyGround(at) || mortals.PriestNear(at);
            }
        }

        public SoulLedger Ledger => SoulRun.Ledger;

        public int Souls => Stockpile != null ? Stockpile.Get(ResourceType.Souls) : 0;

        /// <summary>Souls lost per second now (0 while he has a body).</summary>
        public float DrainPerSecond => IsBodiless ? Ledger.DrainPerSecond(DrainDoubled) : 0f;

        /// <summary>The Anchor Jack revives at: the player's living, finished Main Base (null when there is none).</summary>
        public Building Anchor
        {
            get
            {
                foreach (Building b in Building.All)
                    if (b.IsMainBase && b.IsPlayers && b.IsAlive && b.IsBuilt) return b;
                return null;
            }
        }

        private static Stockpile Stockpile => RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            RegionEntities.Died += OnDied;
            _hooked = true;
        }

        private void OnDestroy()
        {
            if (_hooked) RegionEntities.Died -= OnDied;
        }

        // Mortals and wildlife killed by Jack's side give Souls.
        private void OnDied(UnitDeath death)
        {
            if (death.KillerSide != Side.Yours || Stockpile == null) return;
            int souls = SoulRules.SoulsForKill(death.Kind);
            if (souls > 0) Stockpile.Add(ResourceType.Souls, souls);
        }

        private void Update()
        {
            CharacterControl body = Player.Character;
            if (body == null || IsLost) return;
            ApplyStats(body);
            if (body.IsDead && _wisp == null) BecomeWisp(body);
            else if (!body.IsDead && _wisp != null) EndWisp(body);   // revived some other way (the debug menu)
            if (_wisp == null) return;
            DrainSouls();
            if (!IsLost) TryRevive(body);
        }

        private void BecomeWisp(CharacterControl body)
        {
            SoulRun.Ledger.RecordDeath();
            _drain.Reset();
            float speed = body.TryGetComponent(out CharacterMotor motor) ? motor.MaxSpeed : 0f;
            _wisp = Wisp.Spawn(body.transform.position, speed);
            Follow(_wisp.transform);
            BLog.Info("Souls", $"Jack's body died (death {Ledger.Deaths}): he is a wisp, draining {DrainPerSecond:0.0} Souls a second.");
            // Dying with no Souls left ends it at once.
            IsLost = SoulRules.IsLost(Souls, bodiless: true);
        }

        private void EndWisp(CharacterControl body)
        {
            if (_wisp != null) RemoveWisp();
            _wisp = null;
            Follow(body.transform);
        }

        /// <summary>Jack has a new body (possession): the wisp is gone, the camera and the soul level's stats follow it.</summary>
        internal void Adopt(CharacterControl body)
        {
            if (_wisp != null) RemoveWisp();
            _wisp = null;
            _appliedBody = null;
            Follow(body.transform);
        }

        // Gone at once (the Region finds no wisp this frame), destroyed at the end of it.
        private void RemoveWisp()
        {
            _wisp.gameObject.SetActive(false);
            Destroy(_wisp.gameObject);
        }

        private void DrainSouls()
        {
            Stockpile pile = Stockpile;
            if (pile == null) return;
            int due = _drain.Tick(Time.deltaTime, DrainPerSecond);
            if (due > 0) pile.TrySpend(ResourceType.Souls, Mathf.Min(due, pile.Get(ResourceType.Souls)));
            IsLost = SoulRules.IsLost(pile.Get(ResourceType.Souls), bodiless: true);
        }

        // Touching the Anchor with the Souls to pay revives the body beside it.
        private void TryRevive(CharacterControl body)
        {
            Building anchor = Anchor;
            if (anchor == null || !Touches(anchor, _wisp.transform.position)) return;
            Stockpile pile = Stockpile;
            if (pile == null || !SoulRules.CanRevive(pile.Get(ResourceType.Souls), Ledger.Level, anchorStands: true)) return;
            int cost = Ledger.ReviveCost;
            if (!pile.TrySpend(ResourceType.Souls, cost)) return;

            Bounds bounds = BoundsOf(anchor);
            Vector3 spot = new(bounds.center.x, bounds.min.y - 0.75f, body.transform.position.z);
            body.Teleport(spot);
            body.GetComponent<Damageable>().Revive(1f);
            EndWisp(body);
            BLog.Info("Souls", $"Jack revives at the Anchor for {cost} Souls.");
        }

        /// <summary>Spends Souls to raise soul level; its stats follow at once.</summary>
        public bool TryRaiseSoulLevel()
        {
            Stockpile pile = Stockpile;
            if (pile == null) return false;
            bool raised = Ledger.TryRaiseLevel(cost => pile.TrySpend(ResourceType.Souls, cost));
            if (raised && Player.Character != null) ApplyStats(Player.Character);
            return raised;
        }

        /// <summary>Why Jack can't revive right now, or null when he can (or has a body).</summary>
        public string ReviveBlocker()
        {
            if (!IsBodiless) return null;
            if (Anchor == null) return "No Anchor: he cannot revive";
            return Souls < Ledger.ReviveCost ? $"Revive needs {Ledger.ReviveCost} Souls" : null;
        }

        // What soul level gives the body: a little of everything per level above the first.
        private void ApplyStats(CharacterControl body)
        {
            int level = Ledger.Level;
            if (body == _appliedBody && level == _appliedLevel) return;
            _appliedBody = body;
            _appliedLevel = level;
            if (!body.TryGetComponent(out StatBlock stats)) return;
            stats.RemoveModifiersFromSource(StatSource);
            int steps = level - SoulTuning.StartingLevel;
            if (steps <= 0) return;
            stats.AddModifier(StatId.HealthMax.ToString(), new StatModifier(SoulTuning.HealthPerLevel * steps, StatModifierType.PercentAdd, StatSource));
            stats.AddModifier(StatId.DamageBonus.ToString(), new StatModifier(SoulTuning.DamagePerLevel * steps, StatModifierType.Flat, StatSource));
            stats.AddModifier(StatId.Speed.ToString(), new StatModifier(SoulTuning.SpeedPerLevel * steps, StatModifierType.PercentAdd, StatSource));
        }

        private void Follow(Transform target)
        {
            if (_camera == null) _camera = FindAnyObjectByType<FollowCamera2D>();
            if (_camera != null) _camera.Target = target;
        }

        private static bool Touches(Building anchor, Vector3 point)
        {
            Bounds bounds = BoundsOf(anchor);
            bounds.Expand(AnchorReach * 2f);
            point.z = bounds.center.z;
            return bounds.Contains(point);
        }

        internal static Bounds BoundsOf(Building building)
        {
            Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(building.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
