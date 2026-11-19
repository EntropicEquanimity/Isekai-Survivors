using System;
using System.Reflection;
using Blondie.GameSetup;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai
{
    /// <summary>Run-level debug rows: seed and Anchor setup, region jumping, and ending the Run.</summary>
    [RegionSystem]
    public sealed class RunDebugSection : MonoBehaviour
    {
        private static readonly PropertyInfo s_stateProperty = typeof(PlanetRun).GetProperty(nameof(PlanetRun.State));
        private static readonly PropertyInfo s_currentProperty = typeof(PlanetRun).GetProperty(nameof(PlanetRun.Current));

        private int _seed;
        private AnchorKind _anchor = AnchorKind.Shrine;
        private int _regionIndex;

        private void OnEnable()
        {
            _seed = RunSession.Seed != 0 ? RunSession.Seed : Environment.TickCount;
            _anchor = RunSession.Anchor ?? AnchorKind.Shrine;
            DebugActions.Button(this, "Run", "Run seed -1000", () => _seed -= 1000, "seed");
            DebugActions.Button(this, "Run", "Run seed -1", () => _seed -= 1, "seed");
            DebugActions.Button(this, "Run", "Run seed +1", () => _seed += 1, "seed");
            DebugActions.Button(this, "Run", "Run seed +1000", () => _seed += 1000, "seed");
            DebugActions.Button(this, "Run", "Run seed: random", () => _seed = Environment.TickCount, "seed");
            DebugActions.Button(this, "Run", "Run anchor: Shrine", () => _anchor = AnchorKind.Shrine, "anchor");
            DebugActions.Button(this, "Run", "Run anchor: War Throne", () => _anchor = AnchorKind.WarThrone, "anchor");
            DebugActions.Button(this, "Run", "Start new Run (seed + Anchor)", StartNewRun);
            DebugActions.Button(this, "Run", "Run region prev", () => StepRegion(-1), "region");
            DebugActions.Button(this, "Run", "Run region next", () => StepRegion(+1), "region");
            DebugActions.Button(this, "Run", "Jump to selected Region", JumpToSelectedRegion);
            DebugActions.Button(this, "Run", "Capture this Region", CaptureRegionNow);
            DebugActions.Button(this, "Run", "Win the Run now", WinNow);
            DebugActions.Button(this, "Run", "Lose the Run now", LoseNow);
            DebugActions.Info(this, "Run", Readout);
        }

        private void OnDisable() => DebugActions.Remove(this);

        private void StartNewRun()
        {
            RunSession.StartNew(_seed);
            RunSession.Anchor = _anchor;
            RunSession.GoToPlanet();
        }

        private void StepRegion(int delta)
        {
            PlanetRun run = RunSession.Run;
            if (run == null || run.Planet == null || run.Planet.Regions.Count == 0) return;
            int count = run.Planet.Regions.Count;
            _regionIndex = (_regionIndex + delta) % count;
            if (_regionIndex < 0) _regionIndex += count;
        }

        private void JumpToSelectedRegion()
        {
            PlanetRun run = RunSession.Run;
            if (run == null || run.Planet == null || run.Planet.Regions.Count == 0) return;
            RegionInfo region = run.Planet.Regions[Mathf.Clamp(_regionIndex, 0, run.Planet.Regions.Count - 1)];
            SetRunState(run, PlanetRunState.InRegion);
            SetCurrent(run, region);
            if (GameSetup.IsRunning) GameScenes.Load(RunSession.RegionScene);
            else SceneManager.LoadScene(RunSession.RegionScene);
        }

        private static void CaptureRegionNow()
        {
            if (!RegionEvents.HasEnded) RegionEvents.End(RegionEnd.Captured);
            else if (RunSession.Run != null && RunSession.Run.State == PlanetRunState.InRegion) RunSession.Run.CaptureCurrent();
        }

        private static void WinNow()
        {
            PlanetRun run = RunSession.Run;
            if (run == null) return;
            SetRunState(run, PlanetRunState.InRegion);
            SetCurrent(run, run.Planet.Capital);
            RunSession.End(won: true);
            RunSession.GoToPlanet();
        }

        private static void LoseNow()
        {
            if (RunSession.Run == null) return;
            RunSession.End(won: false);
            RunSession.GoToPlanet();
        }

        private string Readout()
        {
            PlanetRun run = RunSession.Run;
            if (run == null || run.Planet == null) return $"Seed {_seed} · Anchor {AnchorTuning.Name(_anchor)} · no Run";
            if (_regionIndex < 0 || _regionIndex >= run.Planet.Regions.Count) _regionIndex = 0;
            RegionInfo picked = run.Planet.Regions[_regionIndex];
            string state = run.IsCaptured(picked) ? "captured" : run.CanEnter(picked) ? "open" : "locked";
            string current = run.Current != null ? run.Current.Name : "none";
            return $"Seed {_seed} · Anchor {AnchorTuning.Name(_anchor)} · selected {picked.Name} ({state}) · current {current} · Run {run.State}";
        }

        private static void SetRunState(PlanetRun run, PlanetRunState state)
        {
            MethodInfo setter = s_stateProperty?.GetSetMethod(nonPublic: true);
            setter?.Invoke(run, new object[] { state });
        }

        private static void SetCurrent(PlanetRun run, RegionInfo current)
        {
            MethodInfo setter = s_currentProperty?.GetSetMethod(nonPublic: true);
            setter?.Invoke(run, new object[] { current });
        }
    }
}
