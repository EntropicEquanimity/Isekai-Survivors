using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// The body the player controls now. Today that is the one Character; with possession (Jack moving between Suits)
    /// it changes, and everything that follows the player (HUD, stat targets, the map) listens to <see cref="Changed"/>
    /// instead of finding the Character itself.
    /// </summary>
    public static class Player
    {
        static Player() => StaticReset.Register(() => { Character = null; Changed = null; });

        public static CharacterControl Character { get; private set; }

        /// <summary>The controlled body changed (or went away: null).</summary>
        public static event Action<CharacterControl> Changed;

        internal static void Control(CharacterControl body)
        {
            if (Character == body) return;
            Character = body;
            Changed?.Invoke(body);
        }

        internal static void Release(CharacterControl body)
        {
            if (Character != body) return;
            Character = null;
            Changed?.Invoke(null);
        }
    }
}
