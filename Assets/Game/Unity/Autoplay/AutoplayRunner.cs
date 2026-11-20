using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai
{
    /// <summary>
    /// Runs a batch of autoplay Runs: for each, a new Run on its seed, the Anchor picked on the Planet screen, a Region
    /// chosen and entered from the map, the <see cref="AutoplayBot"/> playing it, and on through the Regions until the
    /// Capital falls or the Run is lost (or the bot gives up on a Region that takes too long). It goes through the game's own
    /// flow: the Planet screen, the Region scene, the end screen's way back to the main menu, then a new Run. Every Run writes
    /// a report (<see cref="RunReport"/>) and the batch a summary, then quits (or just stops, from the debug menu).
    /// While it plays the game runs at the batch's speed, capped and eased by <see cref="AutoplaySpeed"/>. Saves go to memory
    /// for the batch, so the developer's own save is never touched.
    /// </summary>
    public sealed class AutoplayRunner : MonoBehaviour
    {
        private const string EditorArgsKey = "Isekai.Autoplay.Args";
        private const float PhaseTimeout = 150f;

        private enum Phase { Booting, WaitPlanet, WaitRegion, Playing, WaitLeave, Done }

        private readonly List<float> _frameMillis = new(20000);
        private readonly List<string> _errorKeys = new();
        private readonly BatchSummary _summary = new();

        private AutoplayOptions _options;
        private string _batchId;
        private string _directory;
        private Phase _phase = Phase.Booting;
        private float _phaseSince;
        private int _runIndex;
        private float _runStart;
        private float _smoothDelta;
        private bool _anchorPicked;

        private RunReport _report;
        private RegionInfo _entering;
        private RegionReport _region;
        private AutoplayBot _bot;
        private Stockpile _pile;
        private int _lastSouls;
        private int _startDeaths;
        private int _startSlain;
        private int _startEscaped;
        private float _regionStart;
        private int _regionsEntered;
        private int _previousEnd = -1;
        private int _baselineItems = -1;
        private bool _inventoryChecked;

        private ISaveStorage _savedStorage;
        private float _savedFixedStep;
        private float _savedTimeScale;
        private bool _savedRunInBackground;
        private int _savedVSync;
        private int _savedFrameRate;

        /// <summary>The batch in progress (null when none).</summary>
        public static AutoplayRunner Current { get; private set; }

        public AutoplayOptions Options => _options;
        public BatchSummary Summary => _summary;
        public bool IsFinished => _phase == Phase.Done;

        /// <summary>The folder the reports are written to.</summary>
        public string Directory => _directory;

        /// <summary>What the batch is doing now, for the debug menu.</summary>
        public string Status => IsFinished ? $"Autoplay done: {_summary.Runs.Count} Runs" : $"Autoplay {_options.Strategy.Name}: Run {_runIndex + 1}/{_options.Runs}, {_phase}"
                                                                                           + (_region != null ? $", {_region.Name} {(_bot != null ? _bot.RegionSeconds : 0f):0}s" : string.Empty);

        /// <summary>A Run finished and its report was written.</summary>
        public event Action<RunReport> RunFinished;

        /// <summary>Starts a batch. Null (and a warning) when one is already going.</summary>
        public static AutoplayRunner Begin(AutoplayOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (Current != null && !Current.IsFinished)
            {
                BLog.Warning("Autoplay", "A batch is already running.");
                return null;
            }
            if (Current != null) Destroy(Current.gameObject);
            var go = new GameObject("Autoplay Runner");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<AutoplayRunner>();
            runner.Init(options);
            Current = runner;
            return runner;
        }

        /// <summary>Gives a running batch up: no more Runs, the game's own settings back (nothing is quit).</summary>
        public static void Stop()
        {
            if (Current == null) return;
            Current.Restore();
            Destroy(Current.gameObject);
            Current = null;
        }

        private void Init(AutoplayOptions options)
        {
            _options = options;
            _batchId = AutoplayReportFiles.NewBatchId();
            _directory = string.IsNullOrEmpty(options.OutputDirectory) ? AutoplayReportFiles.DefaultDirectory : options.OutputDirectory;
            _summary.Strategy = options.Strategy.Name;
            _summary.Anchor = options.Anchor.ToString();
            _summary.Speed = options.Speed;
            _savedStorage = SaveSystem.Storage;
            SaveSystem.Storage = new MemorySaveStorage();
            _savedFixedStep = Time.fixedDeltaTime;
            _savedTimeScale = 1f;
            _savedRunInBackground = Application.runInBackground;
            _savedVSync = QualitySettings.vSyncCount;
            _savedFrameRate = Application.targetFrameRate;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.logMessageReceived += OnLog;
            BotInput.Install();
            _phaseSince = Time.realtimeSinceStartup;
            BLog.Info("Autoplay", $"Batch {_batchId}: {options.Runs} Runs, {options.Strategy.Name}, {options.Anchor}, speed {options.Speed}, reports in {_directory}.");
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            Restore();
        }

        // Everything the batch changed, back to what it was.
        private void Restore()
        {
            if (!_restorable) return;
            Application.logMessageReceived -= OnLog;
            BotInput.Uninstall();
            if (_savedStorage != null) SaveSystem.Storage = _savedStorage;
            Time.timeScale = _savedTimeScale;
            Time.fixedDeltaTime = _savedFixedStep;
            Application.runInBackground = _savedRunInBackground;
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedFrameRate;
            _restorable = false;
        }

        private bool _restorable = true;

        // ---------------------------------------------------------------- the loop

        private void Update()
        {
            ApplySpeed();
            float now = Time.realtimeSinceStartup;
            switch (_phase)
            {
                case Phase.Booting: UpdateBooting(now); break;
                case Phase.WaitPlanet: UpdateWaitPlanet(now); break;
                case Phase.WaitRegion: UpdateWaitRegion(now); break;
                case Phase.Playing: UpdatePlaying(now); break;
                case Phase.WaitLeave: UpdateWaitLeave(now); break;
            }
        }

        // The game runs at the batch's speed while a Region plays, and normally between them. Pausing (a draft, an end screen) still stops it.
        private void ApplySpeed()
        {
            _smoothDelta = _smoothDelta <= 0f ? Time.unscaledDeltaTime : Mathf.Lerp(_smoothDelta, Time.unscaledDeltaTime, 0.1f);
            float scale = _phase == Phase.Playing ? AutoplaySpeed.Scale(_options.Speed, _smoothDelta) : 1f;
            bool paused = GamePause.IsPaused || GameSetup.IsPaused;
            Time.timeScale = paused ? 0f : scale;
            Time.fixedDeltaTime = AutoplaySpeed.FixedStep(scale);
        }

        private void UpdateBooting(float now)
        {
            bool booted = SceneManager.GetActiveScene().name != "Boot";
            if (!booted && now - _phaseSince < 30f) return;
            BeginRun();
        }

        private void BeginRun()
        {
            int seed = _options.SeedOf(_runIndex);
            _report = new RunReport
            {
                Index = _runIndex,
                Seed = seed,
                Anchor = _options.Anchor.ToString(),
                Strategy = _options.Strategy.Name,
                Speed = _options.Speed,
                Version = Application.version,
                StartedAt = DateTime.UtcNow.ToString("o"),
            };
            _frameMillis.Clear();
            _errorKeys.Clear();
            _regionsEntered = 0;
            _previousEnd = -1;
            _anchorPicked = false;
            _region = null;
            _bot = null;
            if (_options.Rings != null) RunSession.StartNew(seed, _options.Rings);
            else RunSession.StartNew(seed);
            _report.RegionsTotal = RunSession.Run.Planet.Regions.Count;
            _runStart = Time.realtimeSinceStartup;
            RunSession.GoToPlanet();
            SetPhase(Phase.WaitPlanet);
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _phaseSince = Time.realtimeSinceStartup;
        }

        // On the Planet map: pick the Anchor the first time, then the Region to enter next.
        private void UpdateWaitPlanet(float now)
        {
            if (now - _phaseSince > PhaseTimeout) { EndRun(RunResult.Stalled, "the Planet screen never came up"); return; }
            if (SceneManager.GetActiveScene().name != RunSession.PlanetScene) return;
            PlanetScreen screen = FindAnyObjectByType<PlanetScreen>();
            if (screen == null || screen.Map == null) return;
            PlanetRun run = RunSession.Run;
            if (run == null) { EndRun(RunResult.Stalled, "no Run on the Planet screen"); return; }
            if (run.IsOver) { EndRun(run.State == PlanetRunState.Won ? RunResult.Won : RunResult.Lost, null); return; }
            if (!_anchorPicked)
            {
                screen.ChooseAnchor(_options.Anchor);
                _anchorPicked = true;
            }
            RegionInfo next = BotRules.NextRegion(run.Available());
            if (next == null || !RunSession.GoToRegion(next)) { EndRun(RunResult.Stalled, "no Region to enter"); return; }
            _entering = next;
            SetPhase(Phase.WaitRegion);
        }

        private void UpdateWaitRegion(float now)
        {
            if (now - _phaseSince > PhaseTimeout) { EndRun(RunResult.Stalled, $"{_entering.Name} never finished loading"); return; }
            if (SceneManager.GetActiveScene().name != RunSession.RegionScene) return;
            RegionRun regionRun = FindAnyObjectByType<RegionRun>();
            RegionBootstrap bootstrap = RegionBootstrap.Current;
            CharacterInventory inventory = CharacterInventory.Current;
            if (regionRun == null || regionRun.Region != _entering || bootstrap == null || !bootstrap.IsPopulated) return;
            if (inventory == null || inventory.Rules == null || RegionEconomy.Current == null || RunEndScreen.Current == null) return;
            if (Player.Character == null || VigilanceDirector.Current == null || BotInput.Current == null) return;
            StartRegion(inventory);
        }

        private void StartRegion(CharacterInventory inventory)
        {
            PlanetRun run = RunSession.Run;
            _region = new RegionReport { Id = _entering.Id, Name = _entering.Name, Depth = _entering.Depth, IsCapital = _entering.IsCapital };
            _bot = new AutoplayBot(_options.Strategy, BotInput.Current) { Log = _options.Verbose ? Say : null };
            if (_options.Verbose) RegionEntities.Died += OnDied;
            _pile = RegionEconomy.Current.Stockpile;
            _lastSouls = _pile.Get(ResourceType.Souls);
            _pile.Changed += OnPileChanged;
            _regionStart = Time.realtimeSinceStartup;
            _startDeaths = SoulRun.Ledger.Deaths;
            _startSlain = run.RulersSlain;
            _startEscaped = run.EscapedRulers.Count;
            _region.ItemsAtStart = ItemCount(inventory);
            CheckInventoryAtStart(_region.ItemsAtStart);
            _regionsEntered++;
            SetPhase(Phase.Playing);
        }

        // The Character brings the Run's Items from Region to Region, no more and no fewer: what a new Run starts with at its
        // first Region, and what the last Region ended with at the next. More would be Items duplicated by a Region left loaded.
        private void CheckInventoryAtStart(int items)
        {
            if (_regionsEntered == 0)
            {
                _report.StartItems = items;
                if (_baselineItems < 0) _baselineItems = items;
                else if (items != _baselineItems) NoteInventory($"Run {_runIndex} started with {items} Items, a new Run starts with {_baselineItems}");
            }
            else if (_previousEnd >= 0 && items != _previousEnd)
                NoteInventory($"{_region.Name} started with {items} Items, the last Region ended with {_previousEnd}");
        }

        private void NoteInventory(string note)
        {
            _report.InventoryStable = false;
            _report.InventoryNotes.Add(note);
            BLog.Warning("Autoplay", note);
        }

        private static int ItemCount(CharacterInventory inventory) => inventory.Active.Items.Count + inventory.Storage.Items.Count;

        private void Say(string text) => BLog.Info("Autoplay", $"[{_runIndex}] {text}");

        // What was around when Jack's body fell (only with verbose on).
        private void OnDied(UnitDeath death)
        {
            CharacterControl jack = Player.Character;
            if (jack == null || death.Unit.GameObject != jack.gameObject || _bot == null) return;
            var near = new System.Text.StringBuilder();
            Vector3 at = jack.transform.position;
            foreach (RegionEntity e in RegionEntities.Living)
            {
                if (e.Side == Side.Yours || Vector2.Distance(e.GameObject.transform.position, at) > 9f) continue;
                near.Append(e.GameObject.name).Append(" (").Append(e.Side).Append(") ");
            }
            Say($"{_bot.RegionSeconds:0}s Jack died at {at.x:0},{at.y:0}, killer side {death.KillerSide}, around him: {near}");
        }

        private void OnPileChanged(ResourceType type, int amount)
        {
            if (type != ResourceType.Souls || _region == null) return;
            int delta = amount - _lastSouls;
            _lastSouls = amount;
            if (delta > 0) _region.SoulsEarned += delta;
            else _region.SoulsSpent -= delta;
        }

        private void UpdatePlaying(float now)
        {
            if (SceneManager.GetActiveScene().name != RunSession.RegionScene)
            {
                // The Region went away by itself (the Planet screen after a capture): nothing is playing.
                if (!RegionEvents.HasEnded) { CloseRegion("Abandoned"); EndRun(RunResult.Stalled, "the Region was left unexpectedly"); }
                return;
            }
            if (Time.timeScale > 0f) _frameMillis.Add(Time.unscaledDeltaTime * 1000f);
            if (VigilanceDirector.Current != null && VigilanceDirector.Current.Stage > _region.HighestVigilance) _region.HighestVigilance = VigilanceDirector.Current.Stage;
            _bot.Tick(Time.deltaTime);

            if (RegionEvents.HasEnded)
            {
                CloseRegion(RegionEvents.Outcome == RegionEnd.Captured ? "Captured" : "Defeated");
                PlanetRun run = RunSession.Run;
                if (run != null && run.IsOver) EndRun(run.State == PlanetRunState.Won ? RunResult.Won : RunResult.Lost, null);
                else SetPhase(Phase.WaitPlanet);   // the Region sends the player back to the Planet map a moment after a capture
                return;
            }
            if (_bot.RegionSeconds > _options.RegionSecondsCap)
            {
                string name = _region.Name;
                CloseRegion("Abandoned");
                EndRun(RunResult.TimedOut, $"{name} took more than {_options.RegionSecondsCap:0} game seconds");
                return;
            }
            if (now - _runStart > _options.RunRealSecondsCap)
            {
                CloseRegion("Abandoned");
                EndRun(RunResult.TimedOut, $"the Run took more than {_options.RunRealSecondsCap:0} real seconds");
            }
        }

        private void CloseRegion(string outcome)
        {
            if (_region == null) return;
            PlanetRun run = RunSession.Run;
            _region.Outcome = outcome;
            _region.RealSeconds = Time.realtimeSinceStartup - _regionStart;
            _region.GameSeconds = _bot.RegionSeconds;
            _region.Deaths = SoulRun.Ledger.Deaths - _startDeaths;
            _region.WavesFaced = VigilanceDirector.Current != null ? VigilanceDirector.Current.WavesSent : 0;
            _region.RulersSlain = run != null ? run.RulersSlain - _startSlain : 0;
            _region.RulersEscaped = run != null ? run.EscapedRulers.Count - _startEscaped : 0;
            _region.PortalsBuilt = _bot.Tally.PortalsBuilt;
            _region.WorkersSummoned = _bot.Tally.WorkersSummoned;
            _region.Samples = _bot.Tally.Samples;
            _region.TunnelSamples = _bot.Tally.TunnelSamples;
            CharacterInventory inventory = CharacterInventory.Current;
            _region.ItemsAtEnd = inventory != null && inventory.Rules != null ? ItemCount(inventory) : _region.ItemsAtStart;
            _previousEnd = _region.ItemsAtEnd;
            if (_pile != null) _pile.Changed -= OnPileChanged;
            RegionEntities.Died -= OnDied;
            _pile = null;
            _bot.Release();
            _report.Regions.Add(_region);
            BLog.Info("Autoplay", $"Run {_runIndex}: {_region.Name} {outcome} after {_region.GameSeconds:0}s of play ({_region.RealSeconds:0}s real), {_region.PortalsBuilt} Portals, {_region.SoulsEarned} Souls earned.");
            _region = null;
            _bot = null;
        }

        // The Run is over (or given up): its report is written and the game goes back through the end screen to the menu.
        private void EndRun(RunResult result, string why)
        {
            if (_report == null) return;
            _report.Result = result;
            if (!string.IsNullOrEmpty(why)) BLog.Warning("Autoplay", $"Run {_runIndex} {result}: {why}.");
            _report.RealSeconds = Time.realtimeSinceStartup - _runStart;
            _report.Frames = FrameTimes.Of(_frameMillis);
            _report.Deaths = SoulRun.Ledger.Deaths;
            _report.SoulLevel = SoulRun.Ledger.Level;
            string file = AutoplayReportFiles.WriteRun(_directory, _batchId, _report);
            _summary.Runs.Add(_report);
            BLog.Info("Autoplay", $"Run {_runIndex} {result}: {_report.RegionsCaptured}/{_report.RegionsTotal} Regions, report {file}");
            RunReport finished = _report;
            _report = null;
            RunFinished?.Invoke(finished);

            if (result == RunResult.TimedOut && RunSession.Run != null && !RunSession.Run.IsOver) RunSession.Abandon();
            RunEndScreen screen = RunEndScreen.Current;
            if (screen != null) screen.ToMainMenu();   // the end screen's own way out, as the player takes it
            else
            {
                RunSession.Clear();
                RunSession.GoToPlanet();
            }
            SetPhase(Phase.WaitLeave);
        }

        private void UpdateWaitLeave(float now)
        {
            bool left = SceneManager.GetActiveScene().name != RunSession.RegionScene;
            if (!left && now - _phaseSince < PhaseTimeout) return;
            if (now - _phaseSince < 0.5f) return;   // let the scene settle
            _runIndex++;
            if (_runIndex < _options.Runs) BeginRun();
            else FinishBatch();
        }

        private void FinishBatch()
        {
            string path = AutoplayReportFiles.WriteSummary(_directory, _batchId, _summary);
            BLog.Info("Autoplay", $"Batch done: {_summary.Count(RunResult.Won)} won, {_summary.Count(RunResult.Lost)} lost, {_summary.Count(RunResult.TimedOut)} timed out, {_summary.Count(RunResult.Stalled)} stalled. Summary {path}");
            SetPhase(Phase.Done);
            Restore();
            if (_options.QuitWhenDone) Quit(_summary.ExitCode);
        }

        private static void Quit(int code)
        {
#if UNITY_EDITOR
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(code);
            else UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(code);
#endif
        }

        // Errors the game logs while the bot plays are counted for the Run (the first few are kept in its report).
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (_report == null || IsEditorNoise(stack)) return;
            _report.ErrorsLogged++;
            if (_region != null) _region.Errors++;
            string first = message.Split('\n')[0];
            if (_report.ErrorMessages.Count < 10 && !_errorKeys.Contains(first))
            {
                _errorKeys.Add(first);
                _report.ErrorMessages.Add(first);
            }
        }

        // An exception from inside the Editor's own tools (search indexing on startup) says nothing about the game.
        private static bool IsEditorNoise(string stack) =>
            !string.IsNullOrEmpty(stack) && stack.Contains("UnityEditor.") && !stack.Contains("Isekai") && !stack.Contains("Blondie");

        // ---------------------------------------------------------------- starting from outside

        /// <summary>Hands the Editor's menu items and the batch method the arguments to start with once Play Mode begins.</summary>
        public static void SetEditorArgs(string args)
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetString(EditorArgsKey, args ?? string.Empty);
#endif
        }

        // A build (or the Editor in a batch) started with -autoplay: play the batch as soon as the game is up.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
#if UNITY_EDITOR
            string handed = UnityEditor.SessionState.GetString(EditorArgsKey, string.Empty);
            if (!string.IsNullOrEmpty(handed))
            {
                UnityEditor.SessionState.EraseString(EditorArgsKey);
                args = handed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
#endif
            if (Array.FindIndex(args, a => string.Equals(a, AutoplayOptions.Flag, StringComparison.OrdinalIgnoreCase)) < 0) return;
            if (!AutoplayOptions.TryParse(args, out AutoplayOptions options, out string error))
            {
                Debug.LogError(error);
                Quit(3);
                return;
            }
            if (options != null)
            {
#if UNITY_EDITOR
                options.QuitWhenDone = options.QuitWhenDone && Application.isBatchMode;
#endif
                Begin(options);
            }
        }
    }
}
