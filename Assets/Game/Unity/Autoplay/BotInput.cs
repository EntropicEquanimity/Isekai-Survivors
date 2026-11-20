using System;
using Blondie.Controllers;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The bot's hands: sits over the game's input (Core's <see cref="ActionInput"/>) and answers the Move action with the
    /// bot's own vector while it is installed, passing everything else to the input underneath. The Character's
    /// <see cref="PlayerBrain"/>, the wisp and <see cref="CharacterControl"/>'s idea of "the player is steering" all read
    /// Move through <see cref="ActionInput"/>, so the bot walks Jack exactly as a stick would, with no change to them.
    /// </summary>
    internal sealed class BotInput : IActionInput
    {
        private readonly IActionInput _inner;

        private BotInput(IActionInput inner) => _inner = inner;

        public static BotInput Current { get; private set; }

        /// <summary>The direction Jack is steered in now (zero: no input, so his auto-fight takes over after a moment).</summary>
        public Vector2 Move { get; set; }

        /// <summary>Puts the bot's input over the game's.</summary>
        public static BotInput Install()
        {
            if (Current != null) return Current;
            Current = new BotInput(ActionInput.Backend);
            ActionInput.Backend = Current;
            return Current;
        }

        /// <summary>Takes the bot's input off and gives the game's back.</summary>
        public static void Uninstall()
        {
            if (Current == null) return;
            if (ActionInput.Backend == Current) ActionInput.Backend = Current._inner;
            Current = null;
        }

        public bool HasAction(string action) => action == PlayerBrain.MoveId || (_inner != null && _inner.HasAction(action));

        public bool WasPressed(string action) => _inner != null && action != PlayerBrain.MoveId && _inner.WasPressed(action);

        public bool IsHeld(string action) => _inner != null && action != PlayerBrain.MoveId && _inner.IsHeld(action);

        public bool WasReleased(string action) => _inner != null && action != PlayerBrain.MoveId && _inner.WasReleased(action);

        public float ReadValue(string action) => _inner != null && action != PlayerBrain.MoveId ? _inner.ReadValue(action) : 0f;

        public Float2 ReadVector(string action)
        {
            if (action == PlayerBrain.MoveId) return new Float2(Move.x, Move.y);
            return _inner != null ? _inner.ReadVector(action) : default;
        }

        public Float2 PointerPosition => _inner != null ? _inner.PointerPosition : default;

        public bool IsPointerHeld(PointerButton button) => _inner != null && _inner.IsPointerHeld(button);

        public bool WasPointerPressed(PointerButton button) => _inner != null && _inner.WasPointerPressed(button);

        public bool WasPointerReleased(PointerButton button) => _inner != null && _inner.WasPointerReleased(button);

        public InputDeviceKind Device => _inner != null ? _inner.Device : InputDeviceKind.KeyboardMouse;

        public event Action<InputDeviceKind> DeviceChanged
        {
            add { if (_inner != null) _inner.DeviceChanged += value; }
            remove { if (_inner != null) _inner.DeviceChanged -= value; }
        }
    }
}
