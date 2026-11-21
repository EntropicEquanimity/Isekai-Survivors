using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Roguelike.Drafts;
using UnityEngine;

namespace Isekai
{
    /// <summary>What the bot did in the Region it is playing, for the report.</summary>
    internal sealed class BotTally
    {
        public int PortalsBuilt;
        public int WorkersSummoned;
        public int LevelsRaised;
        public int DraftsTaken;
        public int Possessions;
        public int Samples;
        public int TunnelSamples;
    }

    /// <summary>
    /// Plays Jack through one Region with the game's own means: it steers the Move input (<see cref="BotInput"/>) along
    /// paths over the Region's cells, lets his weapons and auto-fight do the killing, walks into Stone nodes to mine them,
    /// places Portals through <see cref="RegionBuildings"/> and sets their orders, summons workers, raises soul level,
    /// steers the wisp to the Anchor or a demon when he has no body, takes the Item drafts it is offered and heads for
    /// the Seat's keep. What to do and when is <see cref="BotRules"/>, read from the <see cref="AutoplayStrategy"/>; this
    /// class only looks at the Region and carries the decisions out. It cheats nowhere: no debug rows.
    /// </summary>
    internal sealed class AutoplayBot
    {
        private const float ThinkEvery = 0.25f;
        private const float OrdersEvery = 1.5f;
        private const float ReplanEvery = 8f;
        private const float MovedReplanEvery = 2f;
        private const int WorkerSouls = 25;
        private const float JackReach = 1.1f;
        private const float AttackStandOff = 3f;
        private const float SeatZone = 20f;
        private const float HomeRadius = 7f;
        private const float WitnessRadius = 12f;
        private const float CompanyRadius = 8f;
        private const int PreyCandidates = 24;

        private readonly AutoplayStrategy _strategy;
        private readonly BotInput _input;
        private readonly PathWalker _walker = new();
        private readonly List<Int2> _cells = new();
        private readonly List<(float x, float y)> _points = new();
        private readonly List<RegionEntity> _prey = new();
        private readonly List<PreyCandidate> _preyCandidates = new();
        private readonly List<RegionEntity> _preyShortlist = new();
        private readonly Dictionary<Object, float> _badTargets = new();
        private readonly HashSet<UnitProducer> _ordered = new();
        private readonly List<UnitProducer> _producers = new();
        private readonly List<CombatUnit> _probe = new();

        private BuildingAsset _portalAsset;
        private float _clock;
        private float _nextThink;
        private float _nextOrders;
        private float _replanAt;
        private float _movedReplanAt;
        private bool _hasGoal;
        private Vector3 _goal;
        private Object _target;
        private float _targetBestDistance = float.MaxValue;
        private float _targetSince;
        private bool _portalSpot = true;
        private float _spotRetryAt;
        private bool _raiding;
        private bool _ordersDirty = true;
        private int _lastSouls;
        private float _nextStatus;
        private bool _watching;
        private int _demonsLostAtSeat, _mortalsLostAtSeat, _buildingsLostAtSeat;
        private float _lastGain;

        public AutoplayBot(AutoplayStrategy strategy, BotInput input)
        {
            _strategy = strategy;
            _input = input;
        }

        public BotTally Tally { get; } = new();

        /// <summary>Where the bot says what it decides (null: nowhere).</summary>
        public System.Action<string> Log { get; set; }

        /// <summary>Game seconds the bot has played this Region.</summary>
        public float RegionSeconds => _clock;

        public BotMove Move { get; private set; } = BotMove.Hold;

        /// <summary>Call every frame (also while the game is paused, so a draft gets answered).</summary>
        public void Tick(float deltaTime)
        {
            TakeDraft();
            _clock += deltaTime;
            Steer(deltaTime);
            if (_clock < _nextThink) return;
            _nextThink = _clock + ThinkEvery;
            Think();
        }

        /// <summary>The bot lets go of the stick.</summary>
        public void Release()
        {
            if (_watching) { RegionEntities.Died -= OnDied; _watching = false; }
            _input.Move = Vector2.zero;
            _walker.Clear();
        }

        // An Item draft pauses the game until one is taken: take the first offer, as a player who just wants to go on would.
        private void TakeDraft()
        {
            if (RunDrafts.Current == null) return;
            if (RunDrafts.Take(0) || RunDrafts.Skip()) Tally.DraftsTaken++;
        }

        private void Think()
        {
            RegionSouls souls = RegionSouls.Current;
            RegionEconomy economy = RegionEconomy.Current;
            if (souls == null || economy == null || souls.IsLost) { Release(); return; }
            _portalAsset ??= FindPortalAsset();

            if (souls.IsBodiless)
            {
                FlyWisp(souls, economy);
                return;
            }
            CharacterControl jack = Player.Character;
            if (jack == null || jack.IsDead) { Release(); return; }
            SampleGround(jack);
            CollectPrey();
            int souls0 = economy.Stockpile.Get(ResourceType.Souls);
            if (souls0 > _lastSouls) _lastGain = _clock;
            _lastSouls = souls0;

            BotSnapshot snapshot = Snapshot(jack, souls, economy);
            for (int i = 0; i < 2; i++)
            {
                if (!Buy(BotRules.NextBuy(_strategy, snapshot), souls)) break;
                snapshot = Snapshot(jack, souls, economy);
            }

            bool raid = BotRules.ShouldRaid(_strategy, snapshot);
            if (raid != _raiding) { _raiding = raid; _ordersDirty = true; }
            if (_clock >= _nextOrders || _ordersDirty) GiveOrders(snapshot);

            BotMove move = BotRules.NextMove(_strategy, snapshot);
            if (move != Move && Log != null)
                Log($"{_clock:0}s {Move} -> {move}: health {snapshot.HealthFraction:0.00}, Souls {snapshot.Souls}, Stone {snapshot.Stone}, Portals {snapshot.Portals}, demons {snapshot.Units}, {snapshot.Stage}, at {jack.transform.position.x:0},{jack.transform.position.y:0}");
            Move = move;
            Walk(jack, souls, snapshot);
            if (Log != null && _clock >= _nextStatus) SayStatus(jack, snapshot);
        }

        // A line every half minute of what the Region looks like (verbose only): the numbers a balance pass reads.
        private void OnDied(UnitDeath death)
        {
            Building keep = SeatKeep();
            if (keep == null || death.Unit.GameObject == null || Vector2.Distance(death.Unit.GameObject.transform.position, keep.transform.position) >= SeatZone) return;
            if (death.Side == Side.Yours && death.Kind == UnitKind.Demon) _demonsLostAtSeat++;
            else if (death.Side == Side.Mortals && death.Kind == UnitKind.Mortal) _mortalsLostAtSeat++;
            else if (death.Side == Side.Mortals && death.Kind == UnitKind.Building) _buildingsLostAtSeat++;
        }

        private void SayStatus(CharacterControl jack, in BotSnapshot snapshot)
        {
            if (!_watching) { RegionEntities.Died += OnDied; _watching = true; }
            _nextStatus = _clock + 30f;
            Building keep = SeatKeep();
            int demons = 0, mortals = 0, atSeat = 0, beasts = 0, demonsAtSeat = 0;
            var actions = new System.Text.StringBuilder();
            int probed = 0;
            foreach (RegionEntity e in RegionEntities.Living)
            {
                if (e.Side == Side.Yours && e.Kind == UnitKind.Demon)
                {
                    demons++;
                    if (keep != null && Vector2.Distance(e.GameObject.transform.position, keep.transform.position) < SeatZone)
                    {
                        demonsAtSeat++;
                        if (e.GameObject.TryGetComponent(out DemonBrain brain)) {
                            actions.Append(brain.Action.ToString()[0]).Append((int)Vector2.Distance(e.GameObject.transform.position, keep.transform.position));
                            if (probed++ < 4)
                            {
                                Vector3 at = e.GameObject.transform.position;
                                CombatUnit target = Targeting.NearestHostile(e.Damageable.Unit, at, 4f, at, _probe, out float d);
                                actions.Append("->").Append(target != null ? (target.Owner is Component c ? c.name : target.Name) : "none").Append('@').Append(d.ToString("0.0"));
                            }
                            actions.Append(' ');
                        }
                    }
                }
                else if (e.Side == Side.Mortals && e.Kind == UnitKind.Mortal)
                {
                    mortals++;
                    if (keep != null && Vector2.Distance(e.GameObject.transform.position, keep.transform.position) < SeatZone) atSeat++;
                }
                else if (e.Kind == UnitKind.Wildlife) beasts++;
            }
            string keepText = keep != null && keep.Damageable != null ? $"keep {keep.Damageable.Unit.Health:0}/{keep.Damageable.MaxHealth:0} [{actions}] seat losses: {_demonsLostAtSeat} demons, {_mortalsLostAtSeat} mortals, {_buildingsLostAtSeat} buildings" : "keep gone";
            Log($"{_clock:0}s status: {snapshot.Stage}, health {snapshot.HealthFraction:0.00}, Souls {snapshot.Souls}, Portals {snapshot.Portals}, demons {demons} ({demonsAtSeat} at the Seat), mortals {mortals} ({atSeat} at the Seat), beasts {beasts}, {keepText}, {Move}{(_raiding ? ", raiding" : string.Empty)}");
        }

        // ---------------------------------------------------------------- what the bot sees

        private BotSnapshot Snapshot(CharacterControl jack, RegionSouls souls, RegionEconomy economy)
        {
            Stockpile pile = economy.Stockpile;
            ResourceCost[] costs = _portalAsset != null ? _portalAsset.Costs : System.Array.Empty<ResourceCost>();
            int portals = 0, units = 0;
            foreach (Building b in Building.All)
            {
                if (b == null || !b.IsPlayers || !b.IsAlive || b.Asset != _portalAsset || _portalAsset == null) continue;
                portals++;
                if (b.TryGetComponent(out UnitProducer producer)) units += producer.Units.Count;
            }
            Building keep = SeatKeep();
            int demonsAtSeat = 0, seatDefenders = 0;
            if (keep != null)
            {
                Vector3 seat = keep.transform.position;
                foreach (RegionEntity e in RegionEntities.Living)
                {
                    if (e.Kind != UnitKind.Demon && e.Kind != UnitKind.Mortal) continue;
                    if (e.Side != Side.Yours && e.Side != Side.Mortals) continue;
                    if (Vector2.Distance(e.GameObject.transform.position, seat) >= SeatZone) continue;
                    if (e.Side == Side.Yours) demonsAtSeat++;
                    else seatDefenders++;
                }
            }
            var snapshot = new BotSnapshot
            {
                RegionSeconds = _clock,
                Souls = pile.Get(ResourceType.Souls),
                Stone = pile.Get(ResourceType.Stone),
                SoulLevel = souls.Ledger.Level,
                LevelUpCost = souls.Ledger.LevelUpCost,
                ReviveCost = souls.Ledger.ReviveCost,
                PossessCost = SoulRules.PossessCost(souls.Ledger.Level, bodyAlive: false),
                Stage = VigilanceDirector.Current != null ? VigilanceDirector.Current.Stage : VigilanceStage.Unaware,
                HealthFraction = jack != null && jack.TryGetComponent(out Damageable d) ? d.Normalized : 1f,
                Bodiless = souls.IsBodiless,
                AnchorStands = souls.Anchor != null,
                Workers = WorkerBrain.All.Count,
                WorkerSouls = WorkerSouls,
                PortalStone = CostOf(costs, ResourceType.Stone),
                PortalSouls = CostOf(costs, ResourceType.Souls),
                Portals = portals,
                Units = units,
                StoneLeft = NearestStone(jack != null ? jack.transform.position : Vector3.zero, out _),
                PreyLeft = _prey.Count > 0,
                PortalSpot = _portalSpot,
                SeatKnown = keep != null,
                Raiding = _raiding,
                DemonsAtSeat = demonsAtSeat,
                SeatDefenders = seatDefenders,
                SeatDistance = keep != null && jack != null ? Vector2.Distance(jack.transform.position, keep.transform.position) : 0f,
                DeathDrainPerSecond = SoulRules.DrainPerSecond(souls.Ledger.Level, souls.Ledger.Deaths + 1, doubled: true),
            };
            return snapshot;
        }

        private static int CostOf(ResourceCost[] costs, ResourceType type)
        {
            int total = 0;
            foreach (ResourceCost c in costs)
                if (c.Type == type) total += c.Amount;
            return total;
        }

        private static BuildingAsset FindPortalAsset()
        {
            BuildMode mode = Object.FindAnyObjectByType<BuildMode>();
            if (mode == null) return null;
            foreach (BuildingAsset asset in mode.Catalogue)
                if (asset != null && asset.DisplayName == "Portal") return asset;
            return null;
        }

        private static Building SeatKeep()
        {
            Building keep = RegionSettlements.Current?.Seat?.Keep;
            return keep != null && keep.IsAlive ? keep : null;
        }

        // Jack's body should always be on ground that can be stood on; one that is not has stepped through something.
        private void SampleGround(CharacterControl jack)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null || !grid.IsBaked) return;
            Tally.Samples++;
            if (!grid.IsWalkable(jack.transform.position)) Tally.TunnelSamples++;
        }

        // ---------------------------------------------------------------- buying

        private bool Buy(BotBuy buy, RegionSouls souls)
        {
            switch (buy)
            {
                case BotBuy.Portal: return PlacePortal();
                case BotBuy.Worker:
                    if (RegionWorkers.Current == null || !RegionWorkers.Current.TrySummonWorker()) return false;
                    Tally.WorkersSummoned++;
                    return true;
                case BotBuy.SoulLevel:
                    if (!souls.TryRaiseSoulLevel()) return false;
                    Tally.LevelsRaised++;
                    return true;
                default: return false;
            }
        }

        // A Portal goes on corrupted ground near the Anchor, on the side of the Seat so its demons meet what comes.
        private bool PlacePortal()
        {
            RegionBuildings buildings = RegionBuildings.Current;
            if (buildings == null || _portalAsset == null) return false;
            if (!_portalSpot && _clock < _spotRetryAt) return false;
            if (!TryFindPortalSpot(buildings, out Int2 origin))
            {
                _portalSpot = false;
                _spotRetryAt = _clock + 15f;
                return false;
            }
            _portalSpot = true;
            Building built = buildings.Place(_portalAsset, origin);
            if (built == null) return false;
            Tally.PortalsBuilt++;
            _ordersDirty = true;
            Log?.Invoke($"{_clock:0}s placed a Portal at {built.transform.position.x:0},{built.transform.position.y:0}");
            return true;
        }

        private bool TryFindPortalSpot(RegionBuildings buildings, out Int2 best)
        {
            best = default;
            Building anchor = RegionSouls.Current != null ? RegionSouls.Current.Anchor : null;
            if (anchor == null) return false;
            Vector3 from = anchor.transform.position;
            Building keep = SeatKeep();
            Vector2 toward = keep != null ? ((Vector2)(keep.transform.position - from)).normalized : Vector2.up;
            Vector3 wanted = from + (Vector3)(toward * 6f);
            Int2 centre = buildings.CellAt(from);
            float bestScore = float.MaxValue;
            bool found = false;
            const int Range = 11;
            for (int dx = -Range; dx <= Range; dx++)
            for (int dy = -Range; dy <= Range; dy++)
            {
                var origin = new Int2(centre.x + dx, centre.y + dy);
                Vector3 at = buildings.CentreOf(origin, _portalAsset.Size);
                if (Vector2.Distance(at, from) < 3.5f) continue;   // room to walk between the Anchor and the Portal
                float score = Vector2.Distance(at, wanted);
                if (score >= bestScore || !buildings.Check(_portalAsset, origin).IsOk) continue;
                bestScore = score;
                best = origin;
                found = true;
            }
            return found;
        }

        // ---------------------------------------------------------------- Portal orders

        // Guard near the Anchor while building up; Raid the Seat's keep once the strategy says so.
        private void GiveOrders(in BotSnapshot snapshot)
        {
            _nextOrders = _clock + OrdersEvery;
            _ordersDirty = false;
            Building keep = SeatKeep();
            Building anchor = RegionSouls.Current != null ? RegionSouls.Current.Anchor : null;
            _producers.Clear();
            foreach (UnitProducer producer in Object.FindObjectsByType<UnitProducer>(FindObjectsInactive.Exclude))
                if (producer.TryGetComponent(out Building b) && b.IsPlayers) _producers.Add(producer);
            _producers.Sort((a, b) => a.transform.position.x != b.transform.position.x
                ? a.transform.position.x.CompareTo(b.transform.position.x)
                : a.transform.position.y.CompareTo(b.transform.position.y));
            int raiders = _raiding && keep != null ? BotRules.RaidingPortals(_strategy, _producers.Count) : 0;
            for (int i = 0; i < _producers.Count; i++)
            {
                UnitProducer producer = _producers[i];
                if (producer.Policy != _strategy.Bones) producer.Policy = _strategy.Bones;
                if (i < raiders)
                {
                    bool marked = producer.HasRaidTarget && Vector2.Distance(producer.RaidTarget, keep.transform.position) < 1f;
                    if (producer.Order != PortalOrder.Raid || !marked) producer.MarkRaid(keep.transform.position);
                }
                else
                {
                    if (producer.Order != PortalOrder.Guard) producer.Order = PortalOrder.Guard;
                    if (!_ordered.Contains(producer) && anchor != null)
                    {
                        producer.SetRally(RallyPoint(anchor, keep));
                        _ordered.Add(producer);
                    }
                }
            }
        }

        private static Vector3 RallyPoint(Building anchor, Building keep)
        {
            Vector3 from = anchor.transform.position;
            Vector2 toward = keep != null ? ((Vector2)(keep.transform.position - from)).normalized : Vector2.up;
            return from + (Vector3)(toward * 6f);
        }

        // ---------------------------------------------------------------- walking

        private void Walk(CharacterControl jack, RegionSouls souls, in BotSnapshot snapshot)
        {
            Vector3 at = jack.transform.position;
            switch (Move)
            {
                case BotMove.Mine:
                    if (!NearestStone(at, out ResourceNode node)) { Stand(); break; }
                    Follow(node, node.transform.position, JackReach);
                    break;
                case BotMove.Hunt:
                    RegionEntity prey = PickPrey(at, snapshot.Stage);
                    if (prey == null) { Stand(); break; }
                    if (prey.Damageable != _target && Log != null) Log($"{_clock:0}s hunting {prey.GameObject.name} at {Vector2.Distance(at, prey.GameObject.transform.position):0} units");
                    Follow(prey.Damageable, prey.GameObject.transform.position, AttackStandOff);
                    break;
                case BotMove.Retreat:
                    Building anchor = souls.Anchor;
                    if (anchor == null) { Stand(); break; }
                    Follow(anchor, anchor.transform.position, 4f);
                    break;
                case BotMove.Assault:
                    Building keep = SeatKeep();
                    if (keep == null) { Stand(); break; }
                    Follow(keep, keep.transform.position, AttackStandOff + 1.5f);
                    break;
                default:
                    HoldAtHome(souls);
                    break;
            }
        }

        // "Hold" is at home: the demons guard the Anchor, so that is where Jack waits, not wherever the last hunt left him.
        private void HoldAtHome(RegionSouls souls)
        {
            Building anchor = souls.Anchor;
            CharacterControl jack = Player.Character;
            if (anchor == null || jack == null || Vector2.Distance(jack.transform.position, anchor.transform.position) <= HomeRadius) { Stand(); return; }
            Follow(anchor, anchor.transform.position, HomeRadius - 2f);
        }

        private void Stand()
        {
            _target = null;
            _hasGoal = false;
            _walker.Clear();
            _input.Move = Vector2.zero;
        }

        // Walks toward a target and stops once within reach of it; gives a target up that it cannot get nearer to.
        private void Follow(Object target, Vector3 position, float reach)
        {
            CharacterControl jack = Player.Character;
            if (jack == null) return;
            Vector3 at = jack.transform.position;
            float distance = Vector2.Distance(at, position);
            if (target != _target)
            {
                _target = target;
                _targetBestDistance = float.MaxValue;
                _targetSince = _clock;
                _hasGoal = false;
            }
            if (distance <= reach)
            {
                _walker.Clear();
                _input.Move = Vector2.zero;   // there: his weapons and auto-fight do the rest
                _targetBestDistance = distance;
                _targetSince = _clock;
                return;
            }
            if (distance < _targetBestDistance - 0.5f)
            {
                _targetBestDistance = distance;
                _targetSince = _clock;
            }
            else if (_clock - _targetSince > 8f && target is ResourceNode or Damageable)
            {
                _badTargets[target] = _clock + 40f;   // no nearer in eight seconds: leave it for a while
                Stand();
                return;
            }
            bool moved = ((Vector2)(position - _goal)).sqrMagnitude > 9f;
            if (!_hasGoal || (moved && _clock >= _movedReplanAt) || _walker.IsStuck || _clock >= _replanAt) Plan(at, position);
            _goal = position;
            _hasGoal = true;
        }

        private void Plan(Vector3 from, Vector3 to)
        {
            RegionGrid grid = RegionGrid.Current;
            _replanAt = _clock + ReplanEvery;
            _movedReplanAt = _clock + MovedReplanEvery;
            _points.Clear();
            if (grid != null && grid.IsBaked)
            {
                System.Func<Int2, bool> walkable = grid.IsWalkable;
                if (GridPath.NearestWalkable(grid.Width, grid.Height, walkable, grid.CellAt(from), 3, out Int2 start)
                    && GridPath.NearestWalkable(grid.Width, grid.Height, walkable, grid.CellAt(to), 4, out Int2 goal)
                    && GridPath.Find(grid.Width, grid.Height, walkable, start, goal, _cells))
                {
                    foreach (Int2 cell in _cells)
                    {
                        Vector3 centre = grid.CentreOf(cell);
                        _points.Add((centre.x, centre.y));
                    }
                }
            }
            _points.Add((to.x, to.y));   // the last step is straight at it, over the cell it stands on
            _walker.Set(_points);
        }

        private void Steer(float deltaTime)
        {
            CharacterControl jack = Player.Character;
            RegionSouls souls = RegionSouls.Current;
            if (souls != null && souls.IsBodiless) return;   // the wisp is steered from Think
            if (jack == null || jack.IsDead || !_walker.HasPath)
            {
                if (!_walker.HasPath) _input.Move = Vector2.zero;
                return;
            }
            Vector3 at = jack.transform.position;
            if (_walker.Steer(at.x, at.y, deltaTime, out float dx, out float dy)) _input.Move = new Vector2(dx, dy);
            else _input.Move = Vector2.zero;
        }

        // ---------------------------------------------------------------- without a body

        private void FlyWisp(RegionSouls souls, RegionEconomy economy)
        {
            Wisp wisp = souls.Wisp;
            if (wisp == null) { _input.Move = Vector2.zero; return; }
            BotSnapshot s = new()
            {
                Souls = economy.Stockpile.Get(ResourceType.Souls),
                ReviveCost = souls.Ledger.ReviveCost,
                PossessCost = SoulRules.PossessCost(souls.Ledger.Level, bodyAlive: false),
                AnchorStands = souls.Anchor != null,
                DrainPerSecond = souls.DrainPerSecond,
            };
            Vector3 at = wisp.transform.position;
            GameObject demon = RegionPossession.Current != null ? RegionPossession.Current.Nearest(at, 1000f) : null;
            Vector3 target = at;
            float toAnchor = souls.Anchor != null ? Vector2.Distance(at, souls.Anchor.transform.position) : -1f;
            float toDemon = demon != null ? Vector2.Distance(at, demon.transform.position) : -1f;
            switch (BotRules.WhenBodiless(s, toAnchor, toDemon))
            {
                case BodilessPlan.ToDemon when demon != null: target = demon.transform.position; break;
                default:
                    Building anchor = souls.Anchor;
                    if (anchor != null) target = anchor.transform.position;
                    else if (demon != null) target = demon.transform.position;
                    break;
            }
            Vector2 toward = target - at;
            // Over the demon or the Anchor it flew to: R takes the body (nothing happens on touch alone).
            bool overDemon = demon != null && target == demon.transform.position && toward.sqrMagnitude < 0.6f * 0.6f;
            if (overDemon || (souls.WispAtAnchor && souls.ReviveBlocker() == null)) BodyKey.Press();
            _walker.Clear();
            _hasGoal = false;
            _input.Move = toward.sqrMagnitude > 0.04f ? toward.normalized : Vector2.zero;   // a wisp flies straight over walls
        }

        // ---------------------------------------------------------------- finding things

        private bool NearestStone(Vector3 from, out ResourceNode nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            foreach (ResourceNode node in ResourceNode.All)
            {
                if (node == null || node.Type != ResourceType.Stone || node.IsEmpty || !node.isActiveAndEnabled) continue;
                if (_badTargets.TryGetValue(node, out float until) && _clock < until) continue;
                float d = ((Vector2)(node.transform.position - from)).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                nearest = node;
            }
            return nearest != null;
        }

        // The mortals and beasts worth walking to: not the Seat's guards (the raid is for those).
        private void CollectPrey()
        {
            _prey.Clear();
            Building keep = SeatKeep();
            Vector3 seat = keep != null ? keep.transform.position : Vector3.zero;
            Building anchor = RegionSouls.Current != null ? RegionSouls.Current.Anchor : null;
            bool homeKnown = anchor != null;
            Vector3 home = homeKnown ? anchor.transform.position : Vector3.zero;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity e = living[i];
                if (e.Kind != UnitKind.Mortal && e.Kind != UnitKind.Wildlife) continue;
                if (e.Side != Side.Mortals || e.Kind != UnitKind.Mortal) continue;   // the beasts are left alone: one werewolf is more than Jack can take
                GameObject go = e.GameObject;
                if (go == null || !go.activeInHierarchy) continue;
                if (_badTargets.TryGetValue(e.Damageable, out float until) && _clock < until) continue;
                if (keep != null && Vector2.Distance(go.transform.position, seat) < SeatZone) continue;
                if (homeKnown && Vector2.Distance(go.transform.position, home) > BotRules.HuntRadius) continue;
                _prey.Add(e);
            }
        }

        // The nearest few prey; the witnesses are counted for those only (when sneaking), and the strategy picks.
        private RegionEntity PickPrey(Vector3 from, VigilanceStage stage)
        {
            if (_prey.Count == 0) return null;

            // The nearest few; the witnesses are counted for those only.
            _preyShortlist.Clear();
            _preyCandidates.Clear();
            Building anchor = RegionSouls.Current != null ? RegionSouls.Current.Anchor : null;
            Vector3 home = anchor != null ? anchor.transform.position : from;
            var taken = new HashSet<int>();
            for (int n = 0; n < PreyCandidates && n < _prey.Count; n++)
            {
                int bestIndex = -1;
                float best = float.MaxValue;
                for (int i = 0; i < _prey.Count; i++)
                {
                    if (taken.Contains(i)) continue;
                    float d = ((Vector2)(_prey[i].GameObject.transform.position - from)).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    bestIndex = i;
                }
                taken.Add(bestIndex);
                RegionEntity chosen = _prey[bestIndex];
                _preyShortlist.Add(chosen);
                int danger = chosen.GameObject.TryGetComponent(out MortalBrain brain) ? BotRules.DangerOf(brain.Type) : 3;
                _preyCandidates.Add(new PreyCandidate(Mathf.Sqrt(best), Vector2.Distance(chosen.GameObject.transform.position, home),
                    _strategy.AvoidWitnesses ? WitnessesAround(chosen) : 0, danger, CompanyAround(chosen)));
            }
            int pick = BotRules.PickPrey(_strategy, _preyCandidates, BotRules.Desperation(_clock - _lastGain));
            return pick >= 0 ? _preyShortlist[pick] : null;
        }

        // The other mortals and beasts close enough to join the fight.
        private int CompanyAround(RegionEntity prey)
        {
            int company = 0;
            Vector3 at = prey.GameObject.transform.position;
            foreach (RegionEntity other in RegionEntities.Living)
            {
                if (other == prey || (other.Side != Side.Mortals && other.Side != Side.Hostile)) continue;
                if (other.Kind != UnitKind.Mortal && other.Kind != UnitKind.Wildlife) continue;
                if (Vector2.Distance(other.GameObject.transform.position, at) <= CompanyRadius) company++;
            }
            return company;
        }

        private int WitnessesAround(RegionEntity prey)
        {
            int witnesses = 0;
            Vector3 at = prey.GameObject.transform.position;
            foreach (RegionEntity other in RegionEntities.Living)
            {
                if (other == prey || other.Side != Side.Mortals || other.Kind != UnitKind.Mortal) continue;
                if (Vector2.Distance(other.GameObject.transform.position, at) <= WitnessRadius) witnesses++;
            }
            return witnesses;
        }
    }
}
