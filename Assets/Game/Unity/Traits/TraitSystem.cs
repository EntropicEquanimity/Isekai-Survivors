using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using Blondie.Stats;
using UnityEngine;
using Random = System.Random;
using BlondieDied = Blondie.Combat.Died;

namespace Isekai
{
    /// <summary>
    /// Rolls the Traits of every mortal, wildlife creature and demon that has a brain, and puts them on: the rows of
    /// <see cref="TraitTable"/> are the data, this is the one place that reads them.
    /// <list type="bullet">
    /// <item><b>When:</b> a unit is rolled each time it enters the Region (<see cref="RegionEntities.Entered"/>, so a pooled
    /// unit gets a fresh roll for its new life, and no unit has to know about Traits at spawn). The roll is applied in the
    /// next LateUpdate, after the spawner and the brain have set the unit up (a brain resets its weights when it enables).
    /// Odds come from the rank (<see cref="HordeMember.IsElite"/>); heroes are given theirs with <see cref="Roll"/>.</item>
    /// <item><b>Weights:</b> the Traits' rows applied to a copy of the brain's profile (<c>SetBehaviour</c>). Traits are weights
    /// on the shared behaviours, so a Cowardly demon flees through the same scoring as a Cowardly guard.</item>
    /// <item><b>Buffs:</b> Blondie Stats modifiers (a unit without a Stat Block gets one at runtime, so damage and armour
    /// reach Combat's rules). Max health of a unit without a health vital is scaled on its Damageable; vision is
    /// <see cref="Concealment.SetSightFactor"/>. Conditional buffs (hidden, near the post) are put on and taken off as it changes.</item>
    /// <item><b>Timed effects</b> run a few times a second over the units that have one (Sneaky, Ambusher, Protector, Purifier).</item>
    /// </list>
    /// </summary>
    [RegionSystem]
    public sealed class TraitSystem : MonoBehaviour
    {
        private const int AppliedPerFrame = 200;

        private readonly List<RegionEntity> _pending = new();
        private readonly List<UnitTraits> _tracked = new();
        private readonly Dictionary<CombatUnit, UnitTraits> _watched = new();
        private readonly List<RegionEntity> _allies = new();
        private Random _random;
        private float _nextTick;

        public static TraitSystem Current { get; private set; }

        /// <summary>Whether units that enter are rolled (tests turn it off to place Traits by hand with <see cref="Give"/>).</summary>
        public static bool AutoRoll { get; set; } = true;

        /// <summary>Units with a Trait that needs watching now.</summary>
        public int TrackedCount => _tracked.Count;

        private void OnEnable()
        {
            Current = this;
            _random = RegionBootstrap.Current != null && RegionBootstrap.Current.Context != null ? RegionBootstrap.Current.Context.Random("traits") : new Random(1);
            RegionEntities.Entered += OnEntered;
            EventCenter.Subscribe<DamageTaken>(OnDamage);
            EventCenter.Subscribe<BlondieDied>(OnKilled);
            foreach (RegionEntity e in RegionEntities.Living) _pending.Add(e);   // units that entered before this woke
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            RegionEntities.Entered -= OnEntered;
            EventCenter.Unsubscribe<DamageTaken>(OnDamage);
            EventCenter.Unsubscribe<BlondieDied>(OnKilled);
            _pending.Clear();
            _tracked.Clear();
            _watched.Clear();
            Concealment.Reset();
        }

        // ---- Giving Traits ----

        /// <summary>
        /// Rolls <paramref name="unit"/>'s Traits for <paramref name="rank"/> and puts them on (a Hero's 2 or 3 for #24's ruler).
        /// Returns what it rolled; empty when it has none or has no brain.
        /// </summary>
        public IReadOnlyList<TraitId> Roll(GameObject unit, TraitRank rank)
        {
            RegionEntity entity = EntityOf(unit);
            if (entity == null || !HasBrain(unit)) return System.Array.Empty<TraitId>();
            IReadOnlyList<TraitId> traits = TraitRoller.Roll(_random, rank, entity.Side, entity.Kind);
            Apply(entity, traits, rank);
            return traits;
        }

        /// <summary>Puts exactly these Traits on <paramref name="unit"/> (replacing any it had), whether or not the side may have them. False when it has no brain or is not in the Region.</summary>
        public bool Give(GameObject unit, params TraitId[] traits)
        {
            RegionEntity entity = EntityOf(unit);
            if (entity == null || !HasBrain(unit)) return false;
            _pending.RemoveAll(e => e.GameObject == unit);   // an entry still waiting for its roll must not overwrite what was placed
            Apply(entity, traits, TraitRank.Ordinary);
            return true;
        }

        /// <summary>Takes every Trait off <paramref name="unit"/>.</summary>
        public void Strip(GameObject unit)
        {
            if (unit != null && unit.TryGetComponent(out UnitTraits t)) Clear(t);
        }

        private static RegionEntity EntityOf(GameObject unit)
        {
            if (unit == null) return null;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
                if (living[i].GameObject == unit) return living[i];
            return null;
        }

        private static bool HasBrain(GameObject unit) => unit.GetComponent<MortalBrain>() != null || unit.GetComponent<WildlifeBrain>() != null || unit.GetComponent<DemonBrain>() != null;

        private void OnEntered(RegionEntity entity)
        {
            // A pooled unit coming back is not hidden or buffed for its old life while it waits for its new roll.
            if (entity.GameObject.TryGetComponent(out UnitTraits old)) Clear(old);
            _pending.Add(entity);
        }

        private void LateUpdate()
        {
            for (int n = 0; n < AppliedPerFrame && _pending.Count > 0; n++)
            {
                RegionEntity entity = _pending[_pending.Count - 1];
                _pending.RemoveAt(_pending.Count - 1);
                if (!entity.IsListed || !AutoRoll || !HasBrain(entity.GameObject)) continue;
                TraitRank rank = entity.GameObject.TryGetComponent(out HordeMember member) && member.IsElite ? TraitRank.Elite : TraitRank.Ordinary;
                IReadOnlyList<TraitId> traits = TraitRoller.Roll(_random, rank, entity.Side, entity.Kind);
                if (traits.Count > 0) Apply(entity, traits, rank);
            }
            if (Time.time >= _nextTick)
            {
                _nextTick = Time.time + TraitTuning.TickSeconds;
                Tick(Time.time);
            }
        }

        // ---- Putting Traits on and taking them off ----

        private void Apply(RegionEntity entity, IReadOnlyList<TraitId> traits, TraitRank rank)
        {
            GameObject go = entity.GameObject;
            UnitTraits t = go.TryGetComponent(out UnitTraits own) ? own : go.AddComponent<UnitTraits>();
            Clear(t);
            if (traits.Count == 0) return;
            t.Entity = entity;
            t.Set(traits, rank);
            Damageable damageable = entity.Damageable;
            t.BaseMaxHealth = damageable.MaxHealth;
            t.AppliedMaxHealth = t.BaseMaxHealth;
            if (go.TryGetComponent(out HordeMember member))
            {
                t.BaseInterval = member.Attack.Interval;
                t.AppliedInterval = t.BaseInterval;
            }

            BehaviourProfile weighted = TraitTable.Apply(BaseProfile(go), traits);
            t.Weighted = weighted;
            SetProfile(go, weighted);
            if (t.Is(TraitEffect.IgnoresOrders) && go.TryGetComponent(out DemonBrain demon)) demon.IgnoresOrders = true;
            if (t.Is(TraitEffect.QuickAlarm)) t.AlarmDelay = TraitTuning.VigilantAlarmDelayShare;

            bool timed = t.Is(TraitEffect.Concealed) || t.Is(TraitEffect.BacksOff) || t.Is(TraitEffect.HealsAllies);
            foreach (TraitId id in traits)
                foreach (TraitBuff b in TraitTable.Get(id).Buffs)
                    if (b.When != BuffWhen.Always) timed = true;
            if (t.Is(TraitEffect.Concealed))
            {
                t.IsConcealed = true;
                Concealment.SetHidden(go, true);
            }
            if (t.Is(TraitEffect.Concealed) || t.Is(TraitEffect.BacksOff)) _watched[damageable.Unit] = t;
            if (timed) _tracked.Add(t);
            Refresh(t);
        }

        private void Clear(UnitTraits t)
        {
            GameObject go = t.gameObject;
            if (t.Entity != null)
            {
                Damageable damageable = t.Entity.Damageable;
                if (damageable != null)
                {
                    _watched.Remove(damageable.Unit);
                    damageable.Stats?.RemoveStatModifiers(t);
                    // Back to the type's health and attack rate, unless the unit has since been set up anew (a pooled respawn).
                    if (!damageable.UsesStats && Mathf.Approximately(damageable.MaxHealth, t.AppliedMaxHealth)) damageable.MaxHealth = t.BaseMaxHealth;
                    if (go.TryGetComponent(out HordeMember member) && Mathf.Approximately(member.Attack.Interval, t.AppliedInterval)) member.Attack.Interval = t.BaseInterval;
                }
            }
            _tracked.Remove(t);
            Concealment.SetHidden(go, false);
            Concealment.SetSightFactor(go, 1f);
            if (t.Any)
            {
                SetProfile(go, null);
                if (go.TryGetComponent(out DemonBrain demon)) demon.IgnoresOrders = false;
            }
            t.Set(System.Array.Empty<TraitId>(), TraitRank.Ordinary);
            t.Weighted = null;
            t.Entity = null;
            t.IsConcealed = t.IsUnseen = t.IsNearPost = false;
            t.Kills = 0;
            t.BackOffUntil = 0f;
            t.VisibleUntil = 0f;
            t.SpeedFactor = t.SightFactor = t.AlarmDelay = 1f;
        }

        private static BehaviourProfile BaseProfile(GameObject go)
        {
            if (go.TryGetComponent(out MortalBrain mortal)) return mortal.Profile.Behaviour;
            if (go.TryGetComponent(out WildlifeBrain wild)) return wild.Profile.Behaviour;
            go.TryGetComponent(out DemonBrain demon);
            return DemonProfiles.For(demon.Type);
        }

        private static void SetProfile(GameObject go, BehaviourProfile profile)
        {
            if (go.TryGetComponent(out MortalBrain mortal)) mortal.SetBehaviour(profile);
            else if (go.TryGetComponent(out WildlifeBrain wild)) wild.SetBehaviour(profile);
            else if (go.TryGetComponent(out DemonBrain demon)) demon.SetBehaviour(profile);
        }

        // ---- Buffs ----

        private static bool Holds(BuffWhen when, UnitTraits t) => when switch
        {
            BuffWhen.Always => true,
            BuffWhen.WhileConcealed => t.IsConcealed,
            BuffWhen.WhileUnseen => t.IsUnseen,
            BuffWhen.NearPost => t.IsNearPost,
            _ => false,
        };

        // Puts on the buffs that hold now and takes off the rest: one pass over the unit's Traits.
        private static void Refresh(UnitTraits t)
        {
            GameObject go = t.gameObject;
            Damageable damageable = t.Entity.Damageable;
            IStatModifierTarget stats = StatsOf(damageable, go);
            stats.RemoveStatModifiers(t);
            go.TryGetComponent(out CharacterMotor motor);
            float healthPercent = 0f, visionPercent = 0f;
            bool vital = damageable.UsesStats;
            foreach (TraitId id in t.Traits)
                foreach (TraitBuff b in TraitTable.Get(id).Buffs)
                {
                    if (!Holds(b.When, t)) continue;
                    switch (b.Stat)
                    {
                        case TraitStat.Vision:
                            visionPercent += b.Value;
                            break;
                        case TraitStat.HealthMax when !vital:
                            healthPercent += b.Value;
                            break;
                        default:
                            string stat = StatName(b.Stat);
                            if (!stats.HasStat(stat) && stats is StatBlock block) block.AddStat(stat, StatBase(b.Stat, motor));
                            stats.AddStatModifier(stat, b.Kind == BuffKind.Flat ? StatModifierKind.Flat : StatModifierKind.PercentAdd, b.Value, t);
                            break;
                    }
                }

            if (!vital)
            {
                float max = t.BaseMaxHealth * (1f + healthPercent);
                float before = damageable.MaxHealth;
                damageable.MaxHealth = max;
                t.AppliedMaxHealth = damageable.MaxHealth;
                if (max > before) damageable.Heal(max - before);   // it comes in at its new full health
            }
            if (motor == null && stats.HasStat(nameof(StatId.Speed))) t.SpeedFactor = stats.GetStatValue(nameof(StatId.Speed), 1f);
            t.SightFactor = 1f + visionPercent;
            Concealment.SetSightFactor(go, t.SightFactor);
            if (go.TryGetComponent(out HordeMember member) && t.BaseInterval > 0f && stats.HasStat(nameof(StatId.AttackSpeed)))
            {
                member.Attack.Interval = t.BaseInterval / Mathf.Max(0.1f, stats.GetStatValue(nameof(StatId.AttackSpeed), 1f));
                t.AppliedInterval = member.Attack.Interval;
            }
        }

        // The unit's stats; a mortal or creature has none, so it gets a block of its own (Combat reads armour and damage bonus from it).
        private static IStatModifierTarget StatsOf(Damageable damageable, GameObject go)
        {
            IStatModifierTarget stats = damageable.Stats;
            if (stats != null) return stats;
            StatBlock block = go.TryGetComponent(out StatBlock own) ? own : go.AddComponent<StatBlock>();
            damageable.Stats = block;
            return block;
        }

        private static string StatName(TraitStat stat) => stat switch
        {
            TraitStat.HealthMax => nameof(StatId.HealthMax),
            TraitStat.Armor => nameof(StatId.Armor),
            TraitStat.DamageBonus => nameof(StatId.DamageBonus),
            TraitStat.AttackSpeed => nameof(StatId.AttackSpeed),
            _ => nameof(StatId.Speed),
        };

        // A stat the unit lacks starts at the value that changes nothing (a motor's own speed, 1 for a rate, 0 for a bonus).
        private static float StatBase(TraitStat stat, CharacterMotor motor) => stat switch
        {
            TraitStat.Speed => motor != null ? motor.BaseSpeed : 1f,
            TraitStat.AttackSpeed => 1f,
            _ => 0f,
        };

        // ---- Timed effects ----

        private void Tick(float now)
        {
            RegionVision vision = RegionVision.IsOn ? RegionVision.Current : null;
            bool sees = vision != null && vision.IsReady;
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                UnitTraits t = _tracked[i];
                if (t == null || t.Entity == null || !t.Entity.IsListed)
                {
                    if (t != null) Clear(t);
                    else _tracked.RemoveAt(i);
                    continue;
                }
                bool changed = false;
                if (t.Is(TraitEffect.Concealed) && !t.IsConcealed && now >= t.VisibleUntil)
                {
                    t.IsConcealed = true;   // seen for its 3 seconds; hidden again until it next attacks
                    Concealment.SetHidden(t.gameObject, true);
                    changed = true;
                }
                if (t.IsBackingOff && now >= t.BackOffUntil) EndBackOff(t);
                bool unseen = t.IsConcealed || (sees && !vision.CanSee(Enemy(t.Entity.Side), t.Entity.Cell));
                if (unseen != t.IsUnseen) { t.IsUnseen = unseen; changed = true; }
                bool near = IsNear(t);
                if (near != t.IsNearPost) { t.IsNearPost = near; changed = true; }
                if (t.Is(TraitEffect.HealsAllies) && now >= t.NextHeal) HealAllies(t, now);
                if (changed) Refresh(t);
            }
        }

        private static Side Enemy(Side side) => side == Side.Yours ? Side.Mortals : Side.Yours;

        // A Protector is near its post when within a few cells of its settlement's spot (a mortal's home) or its Portal's post (a demon's).
        private static bool IsNear(UnitTraits t)
        {
            if (!t.Has(TraitId.Protector)) return false;
            Vector3 at = t.transform.position;
            Vector3 post;
            if (t.TryGetComponent(out MortalBrain mortal)) post = mortal.Home;
            else if (t.TryGetComponent(out DemonBrain demon)) post = demon.PostAt;
            else return false;
            float r = TraitTuning.ProtectorNearCells;
            return (post.x - at.x) * (post.x - at.x) + (post.y - at.y) * (post.y - at.y) <= r * r;
        }

        // A Purifier mends each living ally near it, slowly, once a second.
        private void HealAllies(UnitTraits t, float now)
        {
            t.NextHeal = now + 1f;
            Vector3 at = t.transform.position;
            float r2 = TraitTuning.PurifierHealCells * TraitTuning.PurifierHealCells;
            Side side = t.Entity.Side;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            _allies.Clear();
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity other = living[i];
                if (other == t.Entity || other.Side != side || other.Kind == UnitKind.Building) continue;
                Vector3 p = other.GameObject.transform.position;
                if ((p.x - at.x) * (p.x - at.x) + (p.y - at.y) * (p.y - at.y) <= r2) _allies.Add(other);
            }
            foreach (RegionEntity ally in _allies) ally.Damageable.Heal(TraitTuning.PurifierHealPerSecond, t.gameObject);
            _allies.Clear();
        }

        // ---- Attacks and kills ----

        // A Sneaky unit that hurt something is seen, and stays seen for 3 seconds after each attack.
        private void OnDamage(DamageTaken e)
        {
            CombatUnit source = e.Result.Info?.Source;
            if (source == null || !_watched.TryGetValue(source, out UnitTraits t) || !t.Is(TraitEffect.Concealed)) return;
            t.VisibleUntil = Time.time + TraitTuning.VisibleSeconds;
            if (!t.IsConcealed) return;
            t.IsConcealed = false;
            Concealment.SetHidden(t.gameObject, false);
            t.IsUnseen = false;
            Refresh(t);
        }

        // An Ambusher that has killed enough backs off for a while: it flees (its weights say so) and does not fight.
        private void OnKilled(BlondieDied e)
        {
            if (e.Killer == null || !_watched.TryGetValue(e.Killer, out UnitTraits t) || !t.Is(TraitEffect.BacksOff) || t.IsBackingOff) return;
            if (++t.Kills < TraitTuning.BackOffKills) return;
            t.BackOffUntil = Time.time + TraitTuning.BackOffSeconds;
            SetProfile(t.gameObject, t.Weighted.Allow(BehaviourAction.Flee, 5f).With(BehaviourChange.Off(BehaviourAction.Fight)));
        }

        private static void EndBackOff(UnitTraits t)
        {
            t.Kills = 0;
            t.BackOffUntil = 0f;
            SetProfile(t.gameObject, t.Weighted);
        }
    }
}
