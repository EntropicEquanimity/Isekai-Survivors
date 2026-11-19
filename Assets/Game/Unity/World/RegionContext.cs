using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// What a Region is being played with: which Region of which Run, its seed and how dangerous it is now. Built by
    /// <see cref="RegionBootstrap"/> and handed to every <see cref="IRegionSetup"/> and <see cref="IRegionPopulate"/>,
    /// so systems read their settings from here instead of from statics or each other. A Region generator adds what it
    /// decides (faction, settlements, hero) here.
    /// </summary>
    public sealed class RegionContext
    {
        public RegionContext(RegionInfo region, PlanetRun run, int runSeed)
        {
            Region = region;
            Run = run;
            Seed = StableHash.Seed(runSeed, "region/" + (region?.Id ?? "none"));
        }

        public RegionInfo Region { get; }
        public PlanetRun Run { get; }

        /// <summary>This Region's seed in this Run: the same Run lays the same Region out the same way.</summary>
        public int Seed { get; }

        /// <summary>
        /// How dangerous the Region is now: today the Waves' escalation, later Vigilance. Set by whichever system owns
        /// it during setup; enemies spawned outside it read their health from here.
        /// </summary>
        public IThreat Threat { get; set; }

        public float HealthMultiplier => Threat?.HealthMultiplier ?? 1f;

        /// <summary>A random stream of its own for one system (e.g. "waves", "wilds"), so systems don't shift each other's rolls.</summary>
        public Random Random(string stream) => new(StableHash.Seed(Seed, stream));
    }

    /// <summary>The Region's danger level as other systems see it.</summary>
    public interface IThreat
    {
        /// <summary>What new enemies' health is multiplied by.</summary>
        float HealthMultiplier { get; }
    }

    /// <summary>
    /// A scene component that sets itself up from the <see cref="RegionContext"/> before any other Start runs, in
    /// <see cref="SetupOrder"/> (lower first). For settings: depth, starting Resources, stat targets.
    /// </summary>
    public interface IRegionSetup
    {
        int SetupOrder => 0;
        void Setup(RegionContext region);
    }

    /// <summary>
    /// A scene component that fills the Region once its ground is baked (walls known), in <see cref="PopulateOrder"/>:
    /// spawning wanderers, Resource nodes, later settlements and patrols.
    /// </summary>
    public interface IRegionPopulate
    {
        int PopulateOrder => 0;
        void Populate(RegionContext region);
    }
}
