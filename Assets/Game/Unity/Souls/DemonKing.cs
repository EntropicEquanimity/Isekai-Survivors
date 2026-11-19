using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Marks a body as a Demon King: one of Jack's normal bodies (Demon_D). Jack in a Demon King spreads corruption and
    /// is at full strength; in any other demon he is weaker and spreads none (<see cref="RegionPossession"/>).
    /// </summary>
    public sealed class DemonKing : MonoBehaviour
    {
        /// <summary>Whether <paramref name="body"/> is a Demon King.</summary>
        public static bool IsKing(Component body) => body != null && body.TryGetComponent(out DemonKing _);
    }
}
