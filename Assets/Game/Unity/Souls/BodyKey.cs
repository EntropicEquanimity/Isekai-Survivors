using Blondie.Controllers;
using Blondie.Core;
using Blondie.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// Jack's one key for bodies, R unless the player rebinds it (Settings, Controls): in a body it leaves it (Jack becomes a
    /// wisp and the body stays an ordinary demon of his); as a wisp it enters the demon or the Anchor he is over. Nothing
    /// happens on touch alone. <see cref="RegionPossession"/> acts on it. Bots and tests press it with <see cref="Press"/>.
    /// </summary>
    public static class BodyKey
    {
        /// <summary>The input action's name, which is also its label in the key bindings.</summary>
        public const string ActionName = "Change Body";

        private static bool s_pressed;

        static BodyKey() => StaticReset.Register(() => s_pressed = false);

        /// <summary>The key as the player sees it now ("R" until rebound).</summary>
        public static string Name
        {
            get
            {
                string text = InputBindings.BindingText(ActionName, InputScheme.KeyboardMouse);
                return string.IsNullOrEmpty(text) ? "R" : text;
            }
        }

        // The game's actions are Blondie's defaults plus this one. R is taken off the defaults' Next, which the game doesn't use.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallAction()
        {
            if (GameInput.Find(ActionName) != null) return;
            InputActionAsset actions = DefaultActions.Create();
            InputActionMap map = actions.FindActionMap(GameAction.PlayerMap);
            InputAction next = map.FindAction(GameAction.Next);
            for (int i = next.bindings.Count - 1; i >= 0; i--)
                if (next.bindings[i].path == "<Keyboard>/r") next.ChangeBinding(i).Erase();
            InputAction body = map.AddAction(ActionName, InputActionType.Button);
            body.AddBinding("<Keyboard>/r", groups: InputSchemes.KeyboardMouseGroup);
            body.AddBinding("<Gamepad>/buttonNorth", groups: InputSchemes.GamepadGroup);
            GameInput.UseActions(actions);
        }

        /// <summary>Presses the key for the next frame (the autoplay bot, tests).</summary>
        public static void Press() => s_pressed = true;

        /// <summary>Whether the key went down this frame (the player's binding, or a <see cref="Press"/>); a press is used up.</summary>
        internal static bool Consume()
        {
            bool pressed = s_pressed;
            s_pressed = false;
            if (pressed) return true;
            return !PlayerBrain.BlockInput && ActionInput.HasAction(ActionName) && ActionInput.WasPressed(ActionName);
        }
    }
}
