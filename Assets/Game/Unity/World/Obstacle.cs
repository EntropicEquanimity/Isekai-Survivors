using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Something that fills its ground cell like a wall: rocks, dead trees, Stone deposits. Its collider stops bodies
    /// (the Character, Units) and, while it stands, its cell is blocked for the hordes' paths and for building. When it
    /// goes away (a rock mined out: its <see cref="ResourceNode"/> empties and switches it off) the cell opens again.
    /// It sits on its cell's centre.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Obstacle : MonoBehaviour
    {
        private Int2 _cell;
        private bool _blocking;

        private void OnEnable()
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return;
            _cell = grid.CellAt(transform.position);
            transform.position = grid.CentreOf(_cell);
            // Before the walls are baked the bake finds this collider; after, block the cell directly.
            if (grid.IsBaked) grid.Block(_cell);
            _blocking = true;
        }

        private void OnDisable()
        {
            if (!_blocking) return;
            _blocking = false;
            RegionGrid grid = RegionGrid.Current;
            if (grid != null && grid.IsBaked && gameObject.scene.isLoaded) grid.Unblock(_cell);
        }
    }
}
