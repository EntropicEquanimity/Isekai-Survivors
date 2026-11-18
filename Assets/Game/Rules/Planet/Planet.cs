using System;
using System.Collections.Generic;
using System.Linq;

namespace Isekai
{
    /// <summary>One section of a Planet.</summary>
    public sealed class RegionInfo
    {
        public string Id;
        public string Name;

        /// <summary>Rings in from the Planet's edge: 0 at the edge (easiest), highest at the Capital.</summary>
        public int Depth;

        public bool IsCapital;

        /// <summary>The buff its capture gives for the rest of the Run (an id the game maps to a Boon).</summary>
        public string BuffId;

        /// <summary>Extra Stone every later Region starts with, once this one is captured.</summary>
        public int BonusStone;

        /// <summary>Where it is drawn on the Planet map: angle (radians) and distance from the centre (0 to 1).</summary>
        public float Angle, Radius;

        public readonly List<string> Neighbours = new();

        public override string ToString() => $"{Name} (depth {Depth})";
    }

    /// <summary>
    /// A Planet: rings of Regions around a Capital at the centre. Each Region touches the Regions beside it in its
    /// ring and the nearest ones in the rings either side; only the innermost ring touches the Capital.
    /// </summary>
    public sealed class Planet
    {
        private static readonly string[] Names =
        {
            "Green Pastures", "Amber Downs", "Mirefen", "Saltreach", "Ashwood", "Gloomhollow", "Thornvale", "Cinder Flats",
            "Frostmere", "Copperridge", "Wyrmrest", "Duskmoor", "Highspire", "Bramblegate", "Stonewatch", "Sunken Reach",
        };

        private readonly Dictionary<string, RegionInfo> _byId = new();

        public IReadOnlyList<RegionInfo> Regions { get; }
        public RegionInfo Capital { get; }
        public int Rings { get; }

        private Planet(List<RegionInfo> regions, int rings)
        {
            Regions = regions;
            Rings = rings;
            foreach (RegionInfo r in regions) _byId[r.Id] = r;
            Capital = regions.First(r => r.IsCapital);
        }

        public RegionInfo this[string id] => id != null && _byId.TryGetValue(id, out RegionInfo r) ? r : null;

        /// <summary>The Regions on the Planet's edge: where a Run may land.</summary>
        public IEnumerable<RegionInfo> Edge => Regions.Where(r => r.Depth == 0);

        /// <param name="ringSizes">Regions per ring from the edge inward, e.g. {6, 4, 3}; the Capital is added inside.</param>
        /// <param name="buffs">Buff ids handed out to Regions in turn (shuffled by the seed).</param>
        public static Planet Generate(int seed, IReadOnlyList<int> ringSizes, IReadOnlyList<string> buffs)
        {
            if (ringSizes == null || ringSizes.Count == 0 || ringSizes.Any(n => n < 1)) throw new ArgumentException("Every ring needs a Region.", nameof(ringSizes));
            var random = new Random(seed);
            var names = Names.OrderBy(_ => random.Next()).ToList();
            var buffOrder = (buffs ?? Array.Empty<string>()).OrderBy(_ => random.Next()).ToList();
            var regions = new List<RegionInfo>();
            var rings = new List<List<RegionInfo>>();
            int n = 0;
            for (int ring = 0; ring < ringSizes.Count; ring++)
            {
                var list = new List<RegionInfo>();
                float offset = (float)random.NextDouble() * (float)(Math.PI * 2);
                for (int i = 0; i < ringSizes[ring]; i++, n++)
                {
                    var r = new RegionInfo
                    {
                        Id = $"r{n}",
                        Name = n < names.Count ? names[n] : $"Region {n + 1}",
                        Depth = ring,
                        BuffId = buffOrder.Count > 0 ? buffOrder[n % buffOrder.Count] : null,
                        BonusStone = 10 + 5 * ring,
                        Angle = offset + i * (float)(Math.PI * 2) / ringSizes[ring],
                        Radius = 1f - ring * (0.7f / ringSizes.Count),   // evenly spaced rings, the Capital at the centre
                    };
                    list.Add(r);
                    regions.Add(r);
                }
                rings.Add(list);
            }
            var capital = new RegionInfo { Id = "capital", Name = "The Capital", Depth = ringSizes.Count, IsCapital = true, BonusStone = 0 };
            regions.Add(capital);

            for (int ring = 0; ring < rings.Count; ring++)
            {
                List<RegionInfo> list = rings[ring];
                if (list.Count > 1)
                    for (int i = 0; i < list.Count; i++) Link(list[i], list[(i + 1) % list.Count]);
                if (ring + 1 < rings.Count)
                {
                    // Each Region links to the nearest-angled Region one ring in, and every inner Region gets at least one.
                    List<RegionInfo> inner = rings[ring + 1];
                    foreach (RegionInfo r in list) Link(r, Nearest(inner, r.Angle));
                    foreach (RegionInfo r in inner)
                        if (!r.Neighbours.Any(id => list.Any(o => o.Id == id))) Link(r, Nearest(list, r.Angle));
                }
                else foreach (RegionInfo r in list) Link(r, capital);
            }
            return new Planet(regions, ringSizes.Count);
        }

        private static RegionInfo Nearest(List<RegionInfo> candidates, float angle) =>
            candidates.OrderBy(c => AngleBetween(c.Angle, angle)).First();

        private static float AngleBetween(float a, float b)
        {
            float d = Math.Abs(a - b) % (float)(Math.PI * 2);
            return d > Math.PI ? (float)(Math.PI * 2) - d : d;
        }

        private static void Link(RegionInfo a, RegionInfo b)
        {
            if (a == b || a.Neighbours.Contains(b.Id)) return;
            a.Neighbours.Add(b.Id);
            b.Neighbours.Add(a.Id);
        }
    }
}
