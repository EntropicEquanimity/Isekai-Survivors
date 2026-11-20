using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;
using BlondieDied = Blondie.Combat.Died;

namespace Isekai
{
    /// <summary>
    /// Demon levels and revolt for the Region: puts a <see cref="DemonLoyalty"/> on every demon of Portals as it enters (not
    /// the Character, Demon Kings, workers or skeletons), credits a demon for the hostile units it kills, lets Loyalty drift a few
    /// times a second against Jack's soul level (<see cref="SoulRun"/>), takes low-Loyalty demons along with a revolt, and
    /// has a rogue demon surrender when Jack's side beats it down to low health (a damage rule keeps that hit from killing it).
    /// The numbers are in <see cref="LoyaltyTuning"/>.
    /// </summary>
    [RegionSystem]
    public sealed class RegionLoyalty : MonoBehaviour
    {
        private const float TickSeconds = 0.5f;
        private const string RuleName = "Isekai.RogueSurrender";

        private readonly List<DemonLoyalty> _tracked = new();
        private float _elapsed;

        public static RegionLoyalty Current { get; private set; }

        /// <summary>Demons with a level and Loyalty alive now (rogue ones too).</summary>
        public IReadOnlyList<DemonLoyalty> Tracked => _tracked;

        private void OnEnable()
        {
            Current = this;
            RegionEntities.Entered += OnEntered;
            EventCenter.Subscribe<BlondieDied>(OnKilled);
            EventCenter.Subscribe<DamageTaken>(OnDamaged);
            DamageRules.Remove(RuleName);
            DamageRules.Add(new DamageRule(RuleName, 600, KeepRogueAlive));
            foreach (RegionEntity e in RegionEntities.Living) OnEntered(e);   // demons that entered before this woke
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            RegionEntities.Entered -= OnEntered;
            EventCenter.Unsubscribe<BlondieDied>(OnKilled);
            EventCenter.Unsubscribe<DamageTaken>(OnDamaged);
            DamageRules.Remove(RuleName);
            _tracked.Clear();
        }

        private static bool HasLoyalty(RegionEntity e)
        {
            if (e.Kind != UnitKind.Demon) return false;
            GameObject go = e.GameObject;
            return go.TryGetComponent(out DemonBrain brain) && brain.Type != DemonType.Skeleton && !DemonKing.IsKing(go.transform);
        }

        private void OnEntered(RegionEntity entity)
        {
            if (!HasLoyalty(entity)) return;
            GameObject go = entity.GameObject;
            DemonLoyalty loyalty = go.TryGetComponent(out DemonLoyalty have) ? have : go.AddComponent<DemonLoyalty>();
            loyalty.StartLife(entity);
            if (!_tracked.Contains(loyalty)) _tracked.Add(loyalty);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed < TickSeconds) return;
            float seconds = _elapsed;
            _elapsed = 0f;
            int soulLevel = SoulRun.Ledger.Level;
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                DemonLoyalty d = _tracked[i];
                if (d == null || d.Entity == null || !d.Entity.IsListed) _tracked.RemoveAt(i);   // dead, or pooled away
                else d.Tick(seconds, soulLevel);
            }
        }

        /// <summary>The demons of Jack's near a revolting demon with low Loyalty turn rogue with it (they do not take others along).</summary>
        internal void Follow(DemonLoyalty rebel)
        {
            Vector2 at = rebel.transform.position;
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                DemonLoyalty d = _tracked[i];
                if (d == null || d == rebel || d.IsRogue || d.Entity == null || !d.Entity.IsListed || d.Entity.Side != Side.Yours) continue;
                if (LoyaltyRules.FollowsRevolt(Vector2.Distance(at, d.transform.position), d.Loyalty)) d.MakeRogue(takeOthers: false);
            }
        }

        // A kill of something hostile to the killer is experience for a demon.
        private static void OnKilled(BlondieDied e)
        {
            if (e.Killer == null || e.Target == null || !Blondie.Combat.Teams.AreHostile(e.Killer, e.Target)) return;
            DemonLoyalty killer = e.Killer.Owner is GameObject go ? DemonLoyalty.Of(go) : null;
            if (killer != null) killer.AddXp(LoyaltyTuning.XpPerKill);
        }

        // Jack's side beating a rogue demon down to low health wins it back.
        private static void OnDamaged(DamageTaken e)
        {
            DamageResult r = e.Result;
            if (r.Target == null || r.Target.Team != TeamIds.Rogue || r.Info?.Source == null || r.Info.Source.Team != TeamIds.Player) return;
            DemonLoyalty rogue = r.Target.Owner is GameObject go ? DemonLoyalty.Of(go) : null;
            if (rogue != null && LoyaltyRules.Surrenders(r.Target.Normalized)) rogue.Reclaim();
        }

        // A hit of Jack's side on a rogue demon never kills it: it surrenders at low health instead (see OnDamaged).
        private static void KeepRogueAlive(DamageContext context)
        {
            CombatUnit target = context.Target;
            if (target.Team != TeamIds.Rogue || context.Info.Source == null || context.Info.Source.Team != TeamIds.Player) return;
            if (!(target.Owner is GameObject go) || DemonLoyalty.Of(go) == null) return;
            float room = target.Health - 1f;
            if (context.Amount > room) context.Amount = Mathf.Max(0f, room);
        }
    }
}
