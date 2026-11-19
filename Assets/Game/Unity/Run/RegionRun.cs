using Blondie.GameSetup;
using Blondie.Roguelike.Boons;
using Blondie.Stats;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Ties a Region scene to the Run (<see cref="RunSession"/>). On start it sets the Waves' depth and Run clock, adds
    /// the Stone earlier captures (and the Legacy tree) give, points the Run's buffs and the Legacy tree's stat
    /// upgrades at this Region's Character and brings back its
    /// inventory. While playing it advances the Run clock. On capture it unlocks Items, grants the Region's buff, keeps
    /// the inventory and returns to the Planet map, or ends the Run as won at the Capital. When the Main Base falls the
    /// Run is lost.
    /// </summary>
    public sealed class RegionRun : MonoBehaviour
    {
        [Tooltip("Seconds between capture and going back to the Planet map.")]
        [SerializeField, Min(0f)] private float _returnDelay = 3f;

        private RegionInfo _region;
        private float _returnAt = -1f;

        public RegionInfo Region => _region;

        private void Awake() => _region = RunSession.EnsureInRegion();

        private void Start()
        {
            PlanetRun run = RunSession.Run;
            WaveDirector waves = FindAnyObjectByType<WaveDirector>();
            if (waves != null) waves.SetRun(_region.Depth, run.RunSeconds);
            int stone = run.BonusStone + LegacyTree.StartingStone(MetaProgress.Unit);
            if (RegionEconomy.Current != null && stone > 0) RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, stone);
            CharacterControl character = FindAnyObjectByType<CharacterControl>();
            if (character != null && character.TryGetComponent(out StatBlock stats))
            {
                RunBoons.SetTarget(stats);
                MetaProgress.ApplyTo(stats);
            }
            if (RegionGoal.Current != null) RegionGoal.Current.Captured += OnCaptured;
            RegionDefeat defeat = FindAnyObjectByType<RegionDefeat>();
            if (defeat != null) defeat.Defeated += OnDefeated;
            Say($"{_region.Name}" + (_region.IsCapital ? " - take the Capital!" : $" (depth {_region.Depth})"), Color.white);
        }

        private void OnDestroy()
        {
            MetaProgress.ApplyTo(null);
            if (RegionGoal.Current != null) RegionGoal.Current.Captured -= OnCaptured;
        }

        private void Update()
        {
            RunSession.Run?.Tick(Time.deltaTime);
            if (_returnAt < 0f || Time.unscaledTime < _returnAt) return;
            _returnAt = -1f;
            RunSession.GoToPlanet();
        }

        private void OnCaptured()
        {
            CharacterInventory inventory = CharacterInventory.Current;
            if (inventory != null) inventory.OnRegionCaptured();
            Boon buff = RegionBuffs.Get(_region.BuffId);
            if (buff != null) RunBoons.Add(buff);

            if (_region.IsCapital)
            {
                RunSession.End(won: true);
                Say("The Planet is yours!", Color.yellow);
                if (GameSetup.IsRunning) ConfirmPresenter.Ask("Planet conquered", "You took the Capital. Back to the menu?", GameSetup.GoToMainMenu, GameSetup.GoToMainMenu);
                return;
            }
            if (inventory != null) RunSession.StoreInventory(inventory.Unit.Capture());
            RunSession.Run.CaptureCurrent();
            Say(buff != null ? $"Captured! {buff.Name}: {buff.Description}" : "Captured!", Color.yellow);
            _returnAt = Time.unscaledTime + _returnDelay;
        }

        private void OnDefeated()
        {
            RunSession.End(won: false);
            if (GameSetup.IsRunning)
                ConfirmPresenter.Ask("Main Base destroyed", "The Run is lost. Start a new Run?",
                    () => { RunSession.StartNew(); RunSession.GoToPlanet(); }, GameSetup.GoToMainMenu);
        }

        private static void Say(string text, Color color)
        {
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 1f, 10f), text, color, 4f);
        }
    }
}
