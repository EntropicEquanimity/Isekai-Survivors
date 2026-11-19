using Blondie.GameSetup;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The pause menu in the game's shape: Resume, Settings, Abandon Run and Quit to menu. It draws Game Setup's
    /// <see cref="PauseMenuPresenter"/> (whose buttons are Resume, Settings and the way back to the main menu, in that
    /// order) and adds Abandon Run before the way out. Game Setup pauses the game and its clock around it.
    /// </summary>
    public sealed class GamePauseScreen : MenuPanel
    {
        private const int LeaveIndex = 2;

        private readonly PauseMenuPresenter _menu;
        private readonly VisualElement _buttons = new();

        public GamePauseScreen(PauseMenuPresenter menu) : base(menu)
        {
            _menu = menu;
            _buttons.AddToClassList("blondie-gs-menu-buttons");
            Body.Add(_buttons);
            if (GameSetupSettings.Current.ShowButtonPrompts) Add(new ButtonPromptBar());
            Draw();
        }

        protected override void OnOpened() => Stagger.Play(_buttons);

        protected override void Redraw()
        {
            base.Redraw();
            Draw();
        }

        private void Draw()
        {
            _buttons.Clear();
            for (int i = 0; i < _menu.Buttons.Count; i++)
            {
                int index = i;
                MenuButtonModel model = _menu.Buttons[i];
                if (i == LeaveIndex) _buttons.Add(new MenuButton(GameMenus.AbandonText, GameMenus.AskAbandonRun));
                string text = i == LeaveIndex ? GameMenus.QuitToMenuText : model.Text;
                _buttons.Add(new MenuButton(text, () => _menu.Press(index), model.IsPrimary));
            }
            if (_menu.Buttons.Count <= LeaveIndex) _buttons.Add(new MenuButton(GameMenus.AbandonText, GameMenus.AskAbandonRun));
        }
    }
}
