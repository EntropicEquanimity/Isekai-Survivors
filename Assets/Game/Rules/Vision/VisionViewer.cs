using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>One unit's eyes on a <see cref="VisionMap"/>: where it stands, how far it sees, and the cells it now keeps in sight.</summary>
    public sealed class VisionViewer
    {
        internal List<int> Cells = new();

        public Int2 Cell { get; internal set; }
        public int Range { get; internal set; }

        /// <summary>Still on its map.</summary>
        public bool IsActive { get; internal set; }

        /// <summary>How many cells it sees now.</summary>
        public int Count => Cells.Count;
    }
}
