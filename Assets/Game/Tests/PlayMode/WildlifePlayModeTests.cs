using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Wildlife in a generated Region: where it spawns, which side it is on, who it attacks, and what killing it gives.</summary>
    public class WildlifePlayModeTests
    {
        private RegionWildlife _wildlife;
        private RegionGrid _grid;
        private CharacterControl _jack;
        private CombatUnit _jackUnit;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            _wildlife = RegionWildlife.Current;
            _grid = RegionGrid.Current;
            _jack = Player.Character;
            _jackUnit = _jack.GetComponent<Damageable>().Unit;
            TestDebug.Toggle("God mode", true);   // Jack must outlive the fights below
            _jack.GetComponent<Blondie.Abilities.AutoCaster>().enabled = false;   // the tests do the killing
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // Takes the Region's own wildlife away, so a test's creatures meet only what the test puts in front of them.
        private void ClearWildlife()
        {
            foreach (WildlifeBrain b in _wildlife.Brains.ToArray()) Pools.Despawn(b.gameObject);
        }

        // Open ground far from Jack, the settlements' mortals and every target, so nothing but the test is near.
        private Vector3 QuietSpot()
        {
            for (int y = 8; y < _grid.Height - 8; y += 3)
            for (int x = 8; x < _grid.Width - 8; x += 3)
            {
                Vector3 at = _grid.CentreOf(new Int2(x, y));
                if (!IsOpen(at, 3f) || Far(at, _jack.transform.position, 30f)) continue;
                if (RegionMortals.Current.Brains.Any(m => !Far(at, m.transform.position, 25f))) continue;
                if (HordeTarget.All.Any(t => t.IsAlive && !Far(at, t.transform.position, 25f))) continue;
                return at;
            }
            Assert.Fail("no quiet ground");
            return default;
        }

        private bool IsOpen(Vector3 at, float radius)
        {
            for (float dx = -radius; dx <= radius; dx += 1f)
            for (float dy = -radius; dy <= radius; dy += 1f)
                if (!_grid.IsWalkable(at + new Vector3(dx, dy, 0f))) return false;
            return true;
        }

        private static bool Far(Vector3 a, Vector3 b, float distance) => (a - b).sqrMagnitude >= distance * distance;

        private WildlifeBrain Spawn(WildlifeType type, Vector3 at)
        {
            WildlifeBrain brain = _wildlife.Spawn(type, at);
            Assert.IsNotNull(brain, "a " + type + " spawns");
            return brain;
        }

        private RegionEntity EntityOf(GameObject go) => RegionEntities.Living.First(e => e.GameObject == go);

        [UnityTest]
        public IEnumerator Wildlife_spawns_in_groups_at_the_wildlife_spots()
        {
            Assert.Greater(RegionTerrain.Current.WildlifeSpots.Count, 0, "the Region has wildlife spots");
            Assert.Greater(_wildlife.Brains.Count, 0, "wildlife spawned");
            foreach (WildlifeBrain b in _wildlife.Brains)
            {
                Vector3 spot = RegionTerrain.Current.WildlifeSpots.OrderBy(s => (s - b.Home).sqrMagnitude).First();
                Assert.Less((spot - b.Home).magnitude, 2f * _grid.CellSize, b.Type + " starts at a wildlife spot");
            }
            Assert.GreaterOrEqual(_wildlife.Brains.Count, RegionTerrain.Current.WildlifeSpots.Count, "at least one creature a spot");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Each_kind_is_wildlife_on_its_own_side()
        {
            Vector3 at = QuietSpot();
            foreach (WildlifeType type in WildlifeProfiles.Roster)
            {
                WildlifeBrain b = Spawn(type, at);
                yield return null;
                RegionEntity e = EntityOf(b.gameObject);
                Assert.AreEqual(UnitKind.Wildlife, e.Kind, type.ToString());
                Assert.AreEqual(WildlifeProfiles.For(type).Side, e.Side, type.ToString());
                Assert.AreEqual(WildlifeProfiles.For(type).Side, b.Side, type.ToString());
            }
        }

        [UnityTest]
        public IEnumerator Wildlife_sees_six_cells()
        {
            Assert.AreEqual(6, VisionRanges.Wildlife);
            Assert.AreEqual(6, _wildlife.SightCells);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Killing_wildlife_gives_a_little_Souls_and_never_raises_Vigilance()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            Stockpile pile = RegionEconomy.Current.Stockpile;
            VigilanceDirector vigilance = VigilanceDirector.Current;
            Assert.Greater(SoulRules.SoulsForKill(UnitKind.Wildlife), 0, "wildlife give Souls");
            Assert.Less(SoulRules.SoulsForKill(UnitKind.Wildlife), SoulRules.SoulsForKill(UnitKind.Mortal), "a little: less than a mortal");

            foreach (WildlifeType type in WildlifeProfiles.Roster)
            {
                WildlifeBrain b = Spawn(type, at);
                yield return null;
                int souls = pile.Get(ResourceType.Souls);
                float points = vigilance.Points;
                VigilanceStage stage = vigilance.Stage;
                b.Member.Unit.TakeDamage(99999f, DamageRules.PhysicalType, _jackUnit);
                yield return null;
                Assert.AreEqual(souls + SoulRules.SoulsForKill(UnitKind.Wildlife), pile.Get(ResourceType.Souls), type + " gives Souls");
                Assert.AreEqual(points, vigilance.Points, type + " does not raise Vigilance");
                Assert.AreEqual(stage, vigilance.Stage, type + " does not change the stage");
            }
        }

        [UnityTest]
        public IEnumerator Wildlife_killed_by_anyone_else_gives_no_Souls()
        {
            ClearWildlife();
            Stockpile pile = RegionEconomy.Current.Stockpile;
            WildlifeBrain b = Spawn(WildlifeType.Slime, QuietSpot());
            yield return null;
            int souls = pile.Get(ResourceType.Souls);
            b.Member.Unit.TakeDamage(99999f, DamageRules.PhysicalType);
            yield return null;
            Assert.AreEqual(souls, pile.Get(ResourceType.Souls));
        }

        [UnityTest]
        public IEnumerator Neutral_wildlife_ignores_you_until_it_is_hit()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            WildlifeBrain slime = Spawn(WildlifeType.Slime, at);
            _jack.Teleport(at + new Vector3(3f, 0f, 0f));   // well inside its sight
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(Side.Neutral, slime.Side);
            Assert.IsFalse(slime.HasTarget, "it has no quarrel with you");
            Assert.AreNotEqual(BehaviourAction.Fight, slime.Action);

            slime.Member.Unit.TakeDamage(1f, DamageRules.PhysicalType, _jackUnit);
            yield return TestWait.Until(() => slime.Action == BehaviourAction.Fight && slime.HasTarget, "it fights back", 3f);
        }

        [UnityTest]
        public IEnumerator A_neutral_creature_stops_fighting_once_the_grudge_fades()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            WildlifeBrain slime = Spawn(WildlifeType.Slime, at);
            _jack.Teleport(at + new Vector3(3f, 0f, 0f));
            slime.Member.Unit.TakeDamage(1f, DamageRules.PhysicalType, _jackUnit);
            yield return TestWait.Until(() => slime.Action == BehaviourAction.Fight, "it fights back", 3f);
            yield return TestWait.Until(() => !slime.HoldsGrudge && slime.Action != BehaviourAction.Fight, "it lets it go", WildlifeTuning.GrudgeSeconds + 4f);
        }

        [UnityTest]
        public IEnumerator Hostile_wildlife_attacks_a_mortal_nearby()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            WildlifeBrain wolf = Spawn(WildlifeType.Werewolf, at);
            MortalBrain peasant = RegionMortals.Current.Spawn(MortalType.Peasant, RegionSettlements.Current.Settlements.First(), at + new Vector3(3f, 0f, 0f));
            Assert.IsNotNull(peasant);
            float max = peasant.Member.Damageable.MaxHealth;
            yield return TestWait.Until(() => wolf.Action == BehaviourAction.Fight, "the wolf goes for the mortal", 5f);
            yield return TestWait.Until(() => !peasant.Member.IsAlive || peasant.Member.Unit.Health < max, "the mortal is hurt", 8f);
        }

        [UnityTest]
        public IEnumerator Hostile_wildlife_does_not_notice_what_is_beyond_its_sight()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            WildlifeBrain wolf = Spawn(WildlifeType.Werewolf, at);
            RegionMortals.Current.Spawn(MortalType.Peasant, RegionSettlements.Current.Settlements.First(), at + new Vector3(_wildlife.SightCells + 3f, 0f, 0f));
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(wolf.HasTarget);
            Assert.AreNotEqual(BehaviourAction.Fight, wolf.Action);
        }

        [UnityTest]
        public IEnumerator Hostile_wildlife_attacks_Jacks_side_on_sight()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            WildlifeBrain bear = Spawn(WildlifeType.Werebear, at);
            _jack.Teleport(at + new Vector3(4f, 0f, 0f));
            yield return TestWait.Until(() => bear.Action == BehaviourAction.Fight && bear.HasTarget, "the bear goes for Jack", 5f);
        }

        [UnityTest]
        public IEnumerator A_hurt_werewolf_runs_but_a_werebear_fights_on()
        {
            ClearWildlife();
            Vector3 at = QuietSpot();
            WildlifeBrain wolf = Spawn(WildlifeType.Werewolf, at);
            WildlifeBrain bear = Spawn(WildlifeType.Werebear, at + new Vector3(0f, 6f, 0f));
            _jack.Teleport(at + new Vector3(3f, 3f, 0f));
            yield return TestWait.Until(() => wolf.Action == BehaviourAction.Fight && bear.Action == BehaviourAction.Fight, "both go for Jack", 5f);
            wolf.Member.Unit.TakeDamage(wolf.Member.Damageable.MaxHealth * 0.85f, DamageRules.TrueType);
            bear.Member.Unit.TakeDamage(bear.Member.Damageable.MaxHealth * 0.85f, DamageRules.TrueType);
            yield return TestWait.Until(() => wolf.Action == BehaviourAction.Flee, "the wolf runs", 3f);
            Assert.AreEqual(BehaviourAction.Fight, bear.Action, "the bear fights to the death");
        }
    }
}
