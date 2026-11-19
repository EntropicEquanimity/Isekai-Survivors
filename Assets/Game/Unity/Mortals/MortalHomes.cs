using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The path fields mortals follow when they are not in the horde's crowd: one static field per settlement,
    /// leading to its spawn markers. Its targets never move, so each is requested once, only when a mortal first needs
    /// it (running to a stronghold, or walking home from far away), kept while it keeps being used and released after a
    /// while. Nothing here asks for a new field because something moved: that is the horde's crowd, and stays its alone.
    /// Short local walks do not use fields at all (a mortal steers straight at its spot).
    /// </summary>
    internal sealed class MortalHomes
    {
        private const float KeepSeconds = 45f;

        private sealed class Entry
        {
            public FlowFieldHandle Handle;
            public float LastUsed;
        }

        private readonly RegionGrid _grid;
        private readonly Dictionary<int, Entry> _fields = new();
        private readonly List<int> _release = new();
        private readonly List<RegionSettlements.Settlement> _strongholds = new();

        public MortalHomes(RegionGrid grid, IReadOnlyList<RegionSettlements.Settlement> settlements)
        {
            _grid = grid;
            foreach (RegionSettlements.Settlement s in settlements)
                if (IsStronghold(s)) _strongholds.Add(s);
        }

        /// <summary>How many fields are held now (the perf test watches this).</summary>
        public int Held => _fields.Count;

        /// <summary>The Seat and every walled settlement: where peasants run to.</summary>
        public static bool IsStronghold(RegionSettlements.Settlement s) => s.IsSeat || s.Plan.Piece.Wall != WallStyle.None;

        /// <summary>The standing stronghold nearest <paramref name="from"/> in a straight line, or null when none stands.</summary>
        public RegionSettlements.Settlement NearestStronghold(Vector3 from)
        {
            RegionSettlements.Settlement best = null;
            float bestD = float.MaxValue;
            foreach (RegionSettlements.Settlement s in _strongholds)
            {
                if (s.IsRazed) continue;
                Vector3 c = Centre(s);
                float d = (c.x - from.x) * (c.x - from.x) + (c.y - from.y) * (c.y - from.y);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        public Vector3 Centre(RegionSettlements.Settlement s)
        {
            CellRect r = s.Plan.Interior;
            return _grid.CentreOf(new Int2((r.MinX + r.MaxX) / 2, (r.MinY + r.MaxY) / 2));
        }

        /// <summary>
        /// The unit direction (on the map plane) toward <paramref name="settlement"/> from <paramref name="from"/>. False
        /// while the field is still being computed, or when the settlement cannot be reached from there.
        /// </summary>
        public bool TryDirection(RegionSettlements.Settlement settlement, Vector3 from, float now, out Vector2 direction)
        {
            direction = default;
            if (!_fields.TryGetValue(settlement.Plan.Index, out Entry entry))
            {
                entry = new Entry { Handle = Request(settlement) };
                _fields[settlement.Plan.Index] = entry;
            }
            entry.LastUsed = now;
            if (!entry.Handle.IsReady) return false;
            Float2 d = entry.Handle.Field.SampleDirection2D(new Float3(from.x, from.y, 0f));
            if (d.x == 0f && d.y == 0f) return false;
            direction = new Vector2(d.x, d.y);
            return true;
        }

        /// <summary>Lets go of the fields nobody has used for a while.</summary>
        public void ReleaseIdle(float now)
        {
            _release.Clear();
            foreach (KeyValuePair<int, Entry> e in _fields)
                if (now - e.Value.LastUsed > KeepSeconds) _release.Add(e.Key);
            foreach (int key in _release)
            {
                _fields[key].Handle.Release();
                _fields.Remove(key);
            }
        }

        public void Dispose()
        {
            foreach (Entry e in _fields.Values) e.Handle.Release();
            _fields.Clear();
        }

        /// <summary>Whether a cell counts as being at the settlement: inside its walls or right at them (a shut gate keeps mortals on the outside of it).</summary>
        public static bool IsAt(RegionSettlements.Settlement settlement, Int2 cell) => settlement.Plan.Interior.Inflate(2).Contains(cell);

        // Toward the settlement's markers (open ground inside it) and its gaps in the wall (a gate is blocked, but a goal
        // is seeded even when blocked, so the field reaches the ground outside it too); its middle cell when it has none.
        private FlowFieldHandle Request(RegionSettlements.Settlement settlement)
        {
            var cells = new List<Int2>();
            foreach (PlacedMarker m in settlement.Plan.Markers) cells.Add(m.Cell);
            foreach (PlacedRoadEnd end in settlement.Plan.RoadEnds)
                if (end.InUse || settlement.IsSeat)
                    foreach (Int2 g in end.Gap) cells.Add(g);
            if (cells.Count == 0)
            {
                CellRect r = settlement.Plan.Interior;
                cells.Add(new Int2((r.MinX + r.MaxX) / 2, (r.MinY + r.MaxY) / 2));
            }
            return FlowFields.Request(_grid.Field, cells);
        }
    }
}
