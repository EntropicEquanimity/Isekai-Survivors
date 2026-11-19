using System;
using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// The Region's corruption: a "Corruption" layer on the grid (a level 0 to 4 per cell, rules in
    /// <see cref="CorruptionField"/>) and what changes it. Spreaders are found from code, never set up on prefabs: the Main
    /// Base (the Anchor), Outposts and the Character's body (Spires don't spread); a dying demon of yours raises its cell
    /// with a chance; <see cref="Purify"/> is for priests. The Anchor's ground starts corrupted so building can begin
    /// beside it, and Buildings need level 2 (<see cref="CorruptedGroundRule"/>, added to <see cref="RegionBuildings.Rules"/> here).
    /// </summary>
    [RegionSystem]
    public sealed class RegionCorruption : MonoBehaviour, IRegionSetup, IRegionPopulate
    {
        public const string LayerName = "Corruption";
        public const string OutpostName = "Outpost";

        private const float ScanSeconds = 1f;

        private readonly List<BuildingSpreader> _buildings = new();
        private readonly HashSet<Building> _tracked = new();
        private CorruptionSpreader _body;
        private Random _random = new(0);
        private float _nextScan;

        public static RegionCorruption Current { get; private set; }

        /// <summary>The corruption rules on the Region's layer.</summary>
        public CorruptionField Field { get; private set; }

        /// <summary>A dying demon of yours raises its cell with this chance (a test sets it to 0 or 1).</summary>
        public float DyingDemonChance { get; set; } = CorruptionTuning.DyingDemonChance;

        public int SetupOrder => RegionOrder.Settings;
        public int PopulateOrder => RegionOrder.Settlements;

        private void Awake()
        {
            Current = this;
            Field = new CorruptionField(RegionGrid.Current.Layer(LayerName));
        }

        private void OnEnable() => RegionEntities.Died += OnDied;

        private void OnDisable() => RegionEntities.Died -= OnDied;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        /// <summary>The level of corruption at a cell.</summary>
        public int Level(Int2 cell) => Field.Level(cell);

        /// <summary>Whether a Building's footprint is corrupted enough to build on.</summary>
        public bool CanBuild(Int2 origin, Vector2Int size) => Field.CanBuild(origin, new Int2(size.x, size.y));

        /// <summary>Lowers a cell one level (a priest at work); false when it is clean already.</summary>
        public bool Purify(Int2 cell) => Field.Purify(cell);

        /// <summary>Raises a cell one level (a dying demon, a debug button); false at level 4.</summary>
        public bool Raise(Int2 cell) => Field.Raise(cell);

        public void Setup(RegionContext region)
        {
            _random = region.Random("corruption");
            RegionBuildings buildings = RegionBuildings.Current;
            if (buildings == null) return;
            foreach (IPlacementRule rule in buildings.Rules)
                if (rule is CorruptedGroundRule) return;
            // After "blocked", before "can't afford": a refused spot says why it is refused even when you are poor.
            buildings.Rules.Insert(Math.Min(1, buildings.Rules.Count), new CorruptedGroundRule());
        }

        public void Populate(RegionContext region)
        {
            RegionGrid grid = RegionGrid.Current;
            foreach (Building b in Building.All)
            {
                if (!b.IsMainBase || !b.IsPlayers) continue;
                Field.Seed(grid.CellAt(b.transform.position), CorruptionTuning.AnchorSeedRadius, CorruptionTuning.AnchorSeedLevel);
            }
            Scan();
        }

        private void Update()
        {
            if (Field == null || RegionGrid.Current == null || !RegionGrid.Current.IsBaked) return;
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + ScanSeconds;
                Scan();
            }
            float dt = Time.deltaTime;
            for (int i = _buildings.Count - 1; i >= 0; i--)
            {
                BuildingSpreader s = _buildings[i];
                if (s.Building == null || !s.Building.IsAlive)
                {
                    _tracked.Remove(s.Building);
                    _buildings.RemoveAt(i);
                    continue;
                }
                s.Spreader.Tick(dt, Field);
            }
            TickBody(dt);
        }

        // Spreading Buildings that are finished and ours: the Anchor and Outposts. Towers (Spires) never are.
        private void Scan()
        {
            IReadOnlyList<Building> all = Building.All;
            for (int i = 0; i < all.Count; i++)
            {
                Building b = all[i];
                if (_tracked.Contains(b) || !b.IsAlive || !b.IsBuilt || !b.IsPlayers) continue;
                if (!TryGetSource(b, out CorruptionSource source)) continue;
                _tracked.Add(b);
                _buildings.Add(new BuildingSpreader(b, new CorruptionSpreader(source, RegionGrid.Current.CellAt(b.transform.position))));
            }
        }

        private static bool TryGetSource(Building b, out CorruptionSource source)
        {
            if (b.IsMainBase)
            {
                source = CorruptionTuning.Anchor;
                return true;
            }
            if (b.Asset != null && b.Asset.DisplayName == OutpostName)
            {
                source = CorruptionTuning.Outpost;
                return true;
            }
            source = default;
            return false;
        }

        /// <summary>
        /// Whether a body spreads corruption as it walks. Today any Character does; ticket #16 limits this to Demon
        /// Kings (Jack in an ordinary demon spreads none).
        /// </summary>
        private static bool BodySpreads(CharacterControl body) => !body.IsDead;

        private void TickBody(float dt)
        {
            CharacterControl body = Player.Character;
            if (body == null || !BodySpreads(body)) return;
            Int2 cell = RegionGrid.Current.CellAt(body.transform.position);
            _body ??= new CorruptionSpreader(CorruptionTuning.Body, cell);
            _body.Cell = cell;
            _body.Tick(dt, Field);
        }

        private void OnDied(UnitDeath death)
        {
            if (death.Kind != UnitKind.Demon || death.Side != Side.Yours) return;
            if (CorruptionRules.DyingDemonSpreads(_random, DyingDemonChance)) Field.Raise(death.Cell);
        }

        private readonly struct BuildingSpreader
        {
            public BuildingSpreader(Building building, CorruptionSpreader spreader)
            {
                Building = building;
                Spreader = spreader;
            }

            public Building Building { get; }
            public CorruptionSpreader Spreader { get; }
        }
    }
}
