using Blondie.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>Placeholder top-centre readout (phase 9 replaces it): next Wave, enemy Bases left, the final Wave's progress.</summary>
    public sealed class RegionHud : MonoBehaviour
    {
        private const string PanelName = "Region HUD";

        [SerializeField] private PanelSettings _panelSettings;

        private Label _label;
        private float _next;

        private void Start()
        {
            PanelRenderer panel = UIPanels.FindOrCreateRenderer(this, PanelName);
            if (_panelSettings != null) UIPanels.SetPanelSettings(panel, _panelSettings);
            else if (panel.panelSettings == null) UIPanels.SetPanelSettings(panel, UIPanels.CreatePanelSettings(PanelName, null, 50f));
            VisualElement root = UIPanels.Mount(panel);
            root.pickingMode = PickingMode.Ignore;
            _label = new Label
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, top = 4, left = 0, right = 0, unityTextAlign = TextAnchor.UpperCenter, color = Color.white },
            };
            root.Add(_label);
            UIPanels.Created(panel);
        }

        private void Update()
        {
            if (_label == null || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            var text = new System.Text.StringBuilder();
            WaveDirector waves = FindAnyObjectByType<WaveDirector>();
            if (waves != null && waves.enabled)
            {
                WaveSchedule s = waves.Schedule;
                if (s.TryPeekNext(out Wave next))
                {
                    int secs = Mathf.CeilToInt(waves.SecondsUntilNext);
                    text.Append(next.IsFinal ? "Final Wave" : $"Wave {s.Released + 1}/{s.Count}").Append(" in ").Append(secs / 60).Append(':').Append((secs % 60).ToString("00"));
                }
                else text.Append("All Waves sent");
                text.Append("   x").Append(waves.Multiplier.ToString("F2"));
            }
            RegionGoal goal = RegionGoal.Current;
            if (goal != null)
            {
                RegionCapture c = goal.Capture;
                text.Append("   Enemy Bases ").Append(c.EnemyBases - c.EnemyBasesDestroyed).Append('/').Append(c.EnemyBases);
                if (c.FinalWaveReleased) text.Append("   Final Wave: ").Append(c.FinalWaveRemaining).Append(" left");
                if (c.IsCaptured) text.Append("   CAPTURED");
            }
            HordeDirector horde = HordeDirector.Current;
            if (horde != null) text.Append("   Enemies ").Append(horde.Count).Append(" (").Append(horde.Attacking).Append(" attacking)");
            _label.text = text.ToString();
        }
    }
}
