using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using Blondie.Stats;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Jack changing bodies, all on one key (<see cref="BodyKey"/>, R). In a body, R leaves it: Jack becomes a wisp and
    /// the body stays an ordinary demon of his. As a wisp, R over one of his demons possesses it (25% less while the
    /// body he left still lives), and R over the Anchor revives him there (his dead body, or a new Demon King when he
    /// left a living one). Touching alone does nothing. The demon becomes the Player's Character
    /// (it gets a <see cref="CharacterControl"/> if it has none, its <see cref="DemonBrain"/> stops), the camera and
    /// HUD follow, and the body he left becomes an ordinary unit of his again. In a body that is not a
    /// <see cref="DemonKing"/> Jack is weaker (<see cref="SoulTuning.OrdinaryDemonPenalty"/>) and spreads no corruption
    /// (<see cref="RegionCorruption"/> asks the marker). Demon Kings are summoned at the Anchor for Souls
    /// (<see cref="TrySummonDemonKing"/>). A refusal says why on screen and in <see cref="Refusal"/>.
    /// </summary>
    [RegionSystem]
    public sealed class RegionPossession : MonoBehaviour
    {
        public const string KingPrefab = "Demon King";

        private const float WispReach = 1f;
        private const float SayEvery = 2f;

        private static readonly object Source = new();

        private float _saidAt = -100f;
        private string _said;

        // The body Jack is in now: what possession changed on it, so it can be undone when he leaves.
        private bool _addedControl;
        private bool _restoreDespawn;
        private StatBlock _penalised;

        public static RegionPossession Current { get; private set; }

        /// <summary>Why the last possession or summon was refused (null after one that worked).</summary>
        public string Refusal { get; private set; }

        private static Stockpile Stockpile => RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (BodyKey.Consume()) Act();
        }

        /// <summary>
        /// What R does now: leave a living body, or as a wisp revive at the Anchor (else enter the demon) under it. False, with
        /// <see cref="Refusal"/>, when nothing could be done.
        /// </summary>
        public bool Act()
        {
            RegionSouls souls = RegionSouls.Current;
            if (souls == null || souls.IsLost) return false;
            if (!souls.IsBodiless) return TryDetach();
            if (souls.Wisp == null) return false;
            // Over the Anchor the (cheaper) revive comes first, even with demons crowding round it.
            bool atAnchor = souls.WispAtAnchor;
            string blocker = atAnchor ? souls.ReviveBlocker() : null;
            if (atAnchor && blocker == null) return souls.IsDetached ? TryReviveInNewKing(souls) : souls.TryRevive();
            GameObject demon = Nearest(souls.Wisp.transform.position, WispReach);
            if (demon != null) return TryPossess(demon);
            return Refuse(blocker ?? $"Fly onto one of your demons or the Anchor, then press {BodyKey.Name}");
        }

        /// <summary>Jack leaves the living body he is in: it becomes an ordinary demon of his again and he is a wisp.</summary>
        public bool TryDetach()
        {
            RegionSouls souls = RegionSouls.Current;
            CharacterControl body = Player.Character;
            if (souls == null || souls.IsLost || souls.IsBodiless) return false;
            if (body == null || body.IsDead) return Refuse("No body to leave");
            Vector3 at = body.transform.position;
            float speed = body.TryGetComponent(out CharacterMotor motor) ? motor.MaxSpeed : 0f;
            Leave(body, _addedControl, _restoreDespawn, _penalised);
            _addedControl = false;
            _restoreDespawn = false;
            _penalised = null;
            souls.Detach(at, speed);
            Refusal = null;
            BLog.Info("Souls", $"Jack leaves {body.name}; it fights on as an ordinary demon of his.");
            return true;
        }

        // Back at the Anchor after leaving a living body: a new Demon King rises there for the revive cost.
        private bool TryReviveInNewKing(RegionSouls souls)
        {
            Stockpile pile = Stockpile;
            Building anchor = souls.Anchor;
            var prefab = Resources.Load<GameObject>(KingPrefab);
            if (pile == null || anchor == null || prefab == null) return Refuse("No Demon King to revive in");
            int cost = souls.Ledger.ReviveCost;
            if (!pile.TrySpend(ResourceType.Souls, cost)) return Refuse($"Revive needs {cost} Souls");
            Bounds bounds = RegionSouls.BoundsOf(anchor);
            GameObject king = Instantiate(prefab, new Vector3(bounds.center.x, bounds.min.y - 0.75f, 0f), Quaternion.identity);
            CharacterControl body = Enter(king);
            souls.Adopt(body);
            Refusal = null;
            BLog.Info("Souls", $"Jack revives at the Anchor in a new Demon King for {cost} Souls.");
            return true;
        }

        /// <summary>The nearest living demon of Jack's side (not the body he is in) within <paramref name="reach"/> of a point; null when none.</summary>
        public GameObject Nearest(Vector3 from, float reach)
        {
            GameObject best = null;
            float bestSqr = reach * reach;
            CharacterControl mine = Player.Character;
            foreach (RegionEntity u in RegionEntities.Living)
            {
                bool rogue = u.Side == Side.Rogue && u.Kind == UnitKind.Demon && DemonLoyalty.Of(u.GameObject) != null;   // a rogue demon can be won back by possessing it
                if ((u.Side != Side.Yours && !rogue) || (u.Kind != UnitKind.Demon && u.Kind != UnitKind.Character)) continue;
                GameObject go = u.GameObject;
                if (mine != null && go == mine.gameObject) continue;
                float sqr = ((Vector2)(go.transform.position - from)).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = go;
            }
            return best;
        }

        /// <summary>What possessing costs now: full after a death, 25% less while the body he is in (or left) lives.</summary>
        public int Cost()
        {
            RegionSouls souls = RegionSouls.Current;
            CharacterControl body = Player.Character;
            bool alive = souls != null && (souls.IsDetached || (!souls.IsBodiless && body != null && !body.IsDead));
            return SoulRules.PossessCost(souls != null ? souls.Ledger.Level : SoulTuning.StartingLevel, alive);
        }

        /// <summary>Possesses <paramref name="demon"/>, paying the cost. False (with <see cref="Refusal"/>) when it can't.</summary>
        public bool TryPossess(GameObject demon)
        {
            RegionSouls souls = RegionSouls.Current;
            Stockpile pile = Stockpile;
            if (souls == null || pile == null || souls.IsLost) return false;
            if (demon == null || !demon.activeInHierarchy || !demon.TryGetComponent(out Damageable d) || d.IsDead)
                return Refuse("That demon cannot be possessed");
            int cost = Cost();
            if (!pile.TrySpend(ResourceType.Souls, cost)) return Refuse($"Possession needs {cost} Souls");

            CharacterControl from = Player.Character;
            bool wasAdded = _addedControl, wasRestore = _restoreDespawn;
            StatBlock penalised = _penalised;
            CharacterControl body = Enter(demon);
            if (from != null && from != body) Leave(from, wasAdded, wasRestore, penalised);
            souls.Adopt(body);
            DemonLoyalty.Of(demon)?.Reclaim();   // possessing a rogue demon wins it back
            Refusal = null;
            BLog.Info("Souls", $"Jack possesses {demon.name} for {cost} Souls{(DemonKing.IsKing(body) ? " (a Demon King)" : ", weaker in an ordinary demon")}.");
            return true;
        }

        /// <summary>Summons a Demon King beside the Anchor for Souls; null (with <see cref="Refusal"/>) when it can't.</summary>
        public GameObject TrySummonDemonKing()
        {
            RegionSouls souls = RegionSouls.Current;
            Stockpile pile = Stockpile;
            if (souls == null || pile == null) return null;
            Building anchor = souls.Anchor;
            if (anchor == null) { Refuse("No Anchor: Demon Kings are summoned there"); return null; }
            var prefab = Resources.Load<GameObject>(KingPrefab);
            if (prefab == null) { Refuse("No Demon King to summon"); return null; }
            int cost = SoulRules.SummonKingCost(souls.Ledger.Level);
            if (!pile.TrySpend(ResourceType.Souls, cost)) { Refuse($"A Demon King needs {cost} Souls"); return null; }

            Bounds bounds = RegionSouls.BoundsOf(anchor);
            var spot = new Vector3(bounds.center.x + Random.Range(-1f, 1f), bounds.min.y - 0.75f, 0f);
            GameObject king = Instantiate(prefab, spot, Quaternion.identity);
            Refusal = null;
            BLog.Info("Souls", $"A Demon King is summoned at the Anchor for {cost} Souls.");
            return king;
        }

        // Jack takes the demon: the player steers it, its own brain stands down.
        private CharacterControl Enter(GameObject demon)
        {
            if (demon.TryGetComponent(out DemonBrain brain)) brain.enabled = false;
            Damageable damageable = demon.GetComponent<Damageable>();
            _restoreDespawn = damageable.DespawnOnDeath;
            damageable.DespawnOnDeath = false;   // a dead body waits for a revive; it isn't pooled with Jack's controls on it
            _addedControl = !demon.TryGetComponent(out CharacterControl control);
            if (_addedControl) control = demon.AddComponent<CharacterControl>();   // its Awake makes it the Player's Character
            else control.TakeControl();
            _penalised = null;
            if (!DemonKing.IsKing(demon.transform) && demon.TryGetComponent(out StatBlock stats))
            {
                _penalised = stats;
                float penalty = 1f - SoulRules.BodyMultiplier(false);
                if (!stats.HasStat(nameof(StatId.DamageBonus))) stats.AddStat(nameof(StatId.DamageBonus), 0f);
                stats.AddModifier(nameof(StatId.DamageBonus), new StatModifier(-penalty, StatModifierType.Flat, Source));
                stats.AddModifier(StatId.HealthMax.ToString(), new StatModifier(-penalty, StatModifierType.PercentAdd, Source));
            }
            return control;
        }

        // The body Jack leaves is an ordinary unit of his again.
        private static void Leave(CharacterControl from, bool addedControl, bool restoreDespawn, StatBlock penalised)
        {
            GameObject go = from.gameObject;
            bool dead = from.IsDead;
            from.Vacate();
            if (penalised != null) penalised.RemoveModifiersFromSource(Source);
            RegionSouls.Current?.StripSoulStats(go);   // soul level's stats go with Jack, not with the body he left
            if (!dead && go.TryGetComponent(out Damageable d)) d.DespawnOnDeath = restoreDespawn;
            if (addedControl)
            {
                // Possession's own components come off, so a pooled demon comes back clean.
                PlayerBrain player = go.GetComponent<PlayerBrain>();
                DestroyImmediate(from);
                if (player != null) DestroyImmediate(player);
                if (dead) { PrefabSpawning.Despawn(go); return; }
            }
            if (dead) return;
            // A Demon King's body keeps its inventory and everything else; it just stands and fights where it was left.
            if (!go.TryGetComponent(out DemonBrain brain)) brain = go.AddComponent<DemonBrain>();
            brain.enabled = true;
        }

        private bool Refuse(string reason)
        {
            Refusal = reason;
            if (reason != _said || Time.unscaledTime - _saidAt > SayEvery)
            {
                _said = reason;
                _saidAt = Time.unscaledTime;
                Camera cam = Camera.main;
                if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 1f, 10f), reason, Color.red, 3f);
            }
            return false;
        }
    }
}
