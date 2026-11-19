using Blondie.GameSetup;
using Blondie.Roguelike.Boons;
using Blondie.Stats;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Ties a Region scene to the Run (<see cref="RunSession"/>). On start it adds
    /// the Stone earlier captures give, points the Run's buffs at this Region's Character and brings back its
    /// inventory. While playing it advances the Run clock. On capture it unlocks Items, grants the Region's buff, keeps
    /// the inventory and returns to the Planet map, or ends the Run as won at the Capital. When Jack's Souls run out
    /// while he is bodiless the Run is lost.
    /// </summary>
    public sealed class RegionRun : MonoBehaviour, IRegionSetup, IRegionEndListener
    {
        [Tooltip("Seconds between capture and going back to the Planet map.")]
        [SerializeField, Min(0f)] private float _returnDelay = 3f;

        private RegionInfo _region;
        private float _returnAt = -1f;

        public RegionInfo Region => _region;

        void IRegionSetup.Setup(RegionContext region)
        {
            _region = region.Region;
            PlanetRun run = region.Run;
            int stone = run.BonusStone;
            if (RegionEconomy.Current != null && stone > 0) RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, stone);
            Player.Changed += TargetStats;
            TargetStats(Player.Character);
            Say($"{_region.Name}" + (_region.IsCapital ? " - take the Capital!" : $" (depth {_region.Depth})"), Color.white);
        }

        // The Run's buffs go to whichever body the player controls.
        private static void TargetStats(CharacterControl body)
        {
            StatBlock stats = body != null ? body.GetComponent<StatBlock>() : null;
            RunBoons.SetTarget(stats);
        }

        private void OnDestroy()
        {
            Player.Changed -= TargetStats;
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end == RegionEnd.Captured) OnCaptured();
            else OnDefeated();
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
                ConfirmPresenter.Ask("Jack's soul has faded", "The Run is lost. Start a new Run?",
                    () => { RunSession.StartNew(); RunSession.GoToPlanet(); }, GameSetup.GoToMainMenu);
        }

        private static void Say(string text, Color color)
        {
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 1f, 10f), text, color, 4f);
        }
    }
}
