using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using Blondie.Stats;
using Blondie.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// Jack changing bodies. While bodiless, the wisp touching one of his demons possesses it for the possession cost;
    /// while his body lives, P next to a demon does the same for 25% less. The demon becomes the Player's Character
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

        private const float WispReach = 0.6f;
        private const float AliveReach = 2.5f;
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
            RegionSouls souls = RegionSouls.Current;
            if (souls == null || souls.IsLost) return;
            if (souls.IsBodiless)
            {
                if (souls.Wisp == null) return;
                GameObject touched = Nearest(souls.Wisp.transform.position, WispReach);
                if (touched != null) TryPossess(touched);
                return;
            }
            CharacterControl body = Player.Character;
            if (body == null || body.IsDead || !PressedPossess()) return;
            GameObject near = Nearest(body.transform.position, AliveReach);
            if (near != null) TryPossess(near);
            else Refuse("No demon of yours within reach");
        }

        private static bool PressedPossess() =>
            !PlayerBrain.BlockInput && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;

        /// <summary>The nearest living demon of Jack's side (not the body he is in) within <paramref name="reach"/> of a point; null when none.</summary>
        public GameObject Nearest(Vector3 from, float reach)
        {
            GameObject best = null;
            float bestSqr = reach * reach;
            CharacterControl mine = Player.Character;
            foreach (RegionEntity u in RegionEntities.Living)
            {
                if (u.Side != Side.Yours || (u.Kind != UnitKind.Demon && u.Kind != UnitKind.Character)) continue;
                GameObject go = u.GameObject;
                if (mine != null && go == mine.gameObject) continue;
                float sqr = ((Vector2)(go.transform.position - from)).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = go;
            }
            return best;
        }

        /// <summary>What possessing costs now: full while bodiless, 25% less while his body lives.</summary>
        public int Cost()
        {
            RegionSouls souls = RegionSouls.Current;
            CharacterControl body = Player.Character;
            bool alive = souls != null && !souls.IsBodiless && body != null && !body.IsDead;
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
