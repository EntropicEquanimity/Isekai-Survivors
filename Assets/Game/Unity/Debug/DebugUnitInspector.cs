using System;
using System.Text;
using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>Inspector panel for the hovered or selected unit.</summary>
    [RegionSystem]
    public sealed class DebugUnitInspector : MonoBehaviour
    {
        private bool _show = true;
        private GUIStyle _title;
        private readonly StringBuilder _details = new();

        public static string CurrentSummary { get; private set; }

        private void OnEnable()
        {
            DebugActions.Toggle(this, "Units", "Unit inspector panel", () => _show, on => _show = on);
            DebugActions.Info(this, "Units", () => CurrentSummary);
        }

        private void OnDisable() => DebugActions.Remove(this);

        private void Update()
        {
            RegionEntity entity = Current();
            CurrentSummary = Summary(entity);
        }

        private void OnGUI()
        {
            if (!_show || DebugMenu.Instance == null || !DebugMenu.Instance.Visible) return;
            RegionEntity entity = Current();
            if (entity == null) return;

            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _details.Clear();
            BuildDetails(entity, _details);
            GUILayout.BeginArea(new Rect(Screen.width - 420f, 10f, 410f, 300f), GUI.skin.box);
            GUILayout.Label("Unit Inspector", _title);
            GUILayout.Label(_details.ToString());
            GUILayout.EndArea();
        }

        private static RegionEntity Current()
        {
            if (DebugUnitSelection.TrySelected(out RegionEntity selected)) return selected;
            RegionEntity hovered = DebugUnitSelection.Hovered;
            return hovered != null && hovered.IsListed ? hovered : null;
        }

        private static string Summary(RegionEntity entity)
        {
            if (entity == null) return "Inspector: no unit selected";
            string action = "none";
            if (entity.GameObject.TryGetComponent(out MortalBrain mortal)) action = mortal.Action.ToString();
            else if (entity.GameObject.TryGetComponent(out WildlifeBrain wildlife)) action = wildlife.Action.ToString();
            return $"Inspector: {entity.GameObject.name} · side {entity.Side} · action {action}";
        }

        private static void BuildDetails(RegionEntity entity, StringBuilder text)
        {
            Damageable d = entity.Damageable;
            text.Append("Name: ").Append(entity.GameObject.name).Append('\n');
            text.Append("Side: ").Append(entity.Side).Append("    Kind: ").Append(entity.Kind).Append('\n');
            text.Append("Health: ").Append(d.Health.ToString("0.#")).Append('/').Append(d.MaxHealth.ToString("0.#")).Append('\n');
            text.Append("Traits: n/a    Level: n/a    Loyalty: n/a").Append('\n');
            text.Append("Vigilance contribution: ").Append(VigilanceContribution(entity)).Append('\n');
            AppendBrain(text, entity);
        }

        private static string VigilanceContribution(RegionEntity entity) =>
            entity.Kind == UnitKind.Mortal
                ? $"+{VigilanceTuning.KillPoints:0.#} on kill (+{VigilanceTuning.WitnessedKillPoints:0.#} witnessed)"
                : "+0 (not a mortal)";

        private static void AppendBrain(StringBuilder text, RegionEntity entity)
        {
            if (entity.GameObject.TryGetComponent(out MortalBrain mortal))
            {
                text.Append("Action: ").Append(mortal.Action).Append('\n');
                AppendScores(text, mortal.Behaviour, mortal.DebugContext, mortal.Action);
                return;
            }

            if (entity.GameObject.TryGetComponent(out WildlifeBrain wildlife))
            {
                text.Append("Action: ").Append(wildlife.Action).Append('\n');
                AppendScores(text, wildlife.Profile.Behaviour, wildlife.DebugContext, wildlife.Action);
                return;
            }

            text.Append("Action: n/a\nScores: n/a");
        }

        private static void AppendScores(StringBuilder text, BehaviourProfile profile, in BehaviourContext context, BehaviourAction current)
        {
            text.Append("Scores:");
            for (int i = 0; i < BehaviourProfile.ActionCount; i++)
            {
                var action = (BehaviourAction)i;
                float score = BehaviourScoring.Score(profile, action, context);
                if (float.IsNegativeInfinity(score)) continue;
                if (action == current) score += profile.Inertia;
                text.Append(' ').Append(action).Append('=').Append(score.ToString("0.00"));
            }
        }
    }
}
