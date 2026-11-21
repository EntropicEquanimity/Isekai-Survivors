using Blondie.Controllers;
using Blondie.Core;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// The R key, Jack's one key for bodies: in a body it leaves it (Jack becomes a wisp and the body stays an ordinary
    /// demon of his); as a wisp it enters the demon or the Anchor he is over. Nothing happens on touch alone.
    /// <see cref="RegionPossession"/> acts on it. Bots and tests press it with <see cref="Press"/>.
    /// </summary>
    public static class BodyKey
    {
        public const string Name = "R";

        private static bool s_pressed;

        static BodyKey() => StaticReset.Register(() => s_pressed = false);

        /// <summary>Presses the key for the next frame (the autoplay bot, tests).</summary>
        public static void Press() => s_pressed = true;

        /// <summary>Whether the key went down this frame (the keyboard's R, or a <see cref="Press"/>); a press is used up.</summary>
        internal static bool Consume()
        {
            bool pressed = s_pressed;
            s_pressed = false;
            if (pressed) return true;
            return !PlayerBrain.BlockInput && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        }
    }
}
