using System.Collections;
using System.Linq;
using Blondie.Core;
using Blondie.Pooling;
using Blondie.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The Run's Anchor in the real Region: the Planet pick, the Shrine hidden until found, the War Throne's start, aura and spread, and raising a new one.</summary>
    public class AnchorPlayModeTests
    {
        private RegionGrid _grid;
        private Stockpile _pile;

        [TearDown]
        public void Forget()
        {
            RunSession.Anchor = null;
            Time.timeScale = 1f;
        }

        private IEnumerator Land(AnchorKind kind)
        {
            RunSession.StartNew();
            RunSession.Anchor = kind;
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return TestWait.Until(() => Anchor.All.Count > 0 && RegionAnchor.Current != null, "the Anchor stands");
            TestDebug.Toggle("God mode", true);
            _grid = RegionGrid.Current;
            _pile = RegionEconomy.Current.Stockpile;
            yield return TestWait.Frames(3);
        }

        private static Anchor TheAnchor => Anchor.All.First(a => a.Building.IsMainBase && a.Building.IsPlayers);

        private static RegionEntity JackEntity => RegionEntities.Living.First(e => e.GameObject == Player.Character.gameObject);

        private static float DamageBonus(RegionEntity e) => e.Damageable.Stats.GetStatValue(nameof(StatId.DamageBonus), 0f);

        private void Give(int souls, int stone)
        {
            _pile.Add(ResourceType.Souls, souls);
            _pile.Add(ResourceType.Stone, stone);
        }

        private void EmptyPile()
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType))) _pile.TrySpend(type, _pile.Get(type));
        }

        // A walkable cell a few cells from the Anchor, in the first direction that has one.
        private Vector3 SpotNear(Anchor anchor, int distance)
        {
            Int2 from = _grid.CellAt(anchor.transform.position);
            for (int turn = 0; turn < 32; turn++)
            {
                float a = turn * Mathf.PI / 16f;
                var cell = new Int2(from.x + Mathf.RoundToInt(Mathf.Cos(a) * distance), from.y + Mathf.RoundToInt(Mathf.Sin(a) * distance));
                if (_grid.Contains(cell) && _grid.IsWalkable(cell)) return _grid.CentreOf(cell);
            }
            Assert.Fail("no open ground near the Anchor");
            return default;
        }

        [UnityTest]
        public IEnumerator The_Planet_screen_picks_the_Anchor_and_the_Region_starts_with_it()
        {
            RunSession.Clear();
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet map shows", 10f);
            Assert.AreEqual(AnchorKind.Shrine, RunSession.Anchor, "a Shrine is picked until the player says otherwise");

            Assert.IsTrue(Object.FindAnyObjectByType<PlanetScreen>().ChooseAnchor(AnchorKind.WarThrone));
            Assert.AreEqual(AnchorKind.WarThrone, RunSession.Anchor);

            Assert.IsTrue(RunSession.GoToRegion(RunSession.Run.Available().First()));
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.RegionScene, "the Region loads", 10f);
            yield return TestWait.Until(() => RegionBootstrap.Current != null && RegionBootstrap.Current.IsPopulated, "the Region is populated", 10f);
            Assert.AreEqual(AnchorKind.WarThrone, TheAnchor.Kind);
        }

        [UnityTest]
        public IEnumerator A_Shrine_is_small_and_hidden_with_Vigilance_unaware_and_no_aura()
        {
            yield return Land(AnchorKind.Shrine);
            Anchor shrine = TheAnchor;
            Assert.AreEqual(AnchorKind.Shrine, shrine.Kind);
            Assert.IsTrue(shrine.IsHidden);
            Assert.IsFalse(HordeTarget.All.Any(t => t.gameObject == shrine.gameObject), "the horde has no Shrine to go for");
            Assert.AreEqual(VigilanceStage.Unaware, VigilanceDirector.Current.Stage);
            Assert.Less(shrine.Building.Asset.Size.x, AnchorCatalog.Asset(AnchorKind.WarThrone).Size.x, "smaller than the War Throne");
            Assert.Less(shrine.Building.Asset.Size.x, RegionLayout.AnchorSize, "smaller than the Main Base was");

            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(RegionAnchor.Current.IsBuffed(JackEntity), "no aura");
            Assert.IsTrue(shrine.IsHidden, "nobody has seen it");
            Assert.AreEqual(VigilanceStage.Unaware, VigilanceDirector.Current.Stage);
        }

        [UnityTest]
        public IEnumerator A_mortal_seeing_the_Shrine_finds_it_once_raising_Vigilance_a_full_stage_and_making_it_a_target()
        {
            yield return Land(AnchorKind.Shrine);
            Anchor shrine = TheAnchor;
            VigilanceDirector director = VigilanceDirector.Current;
            float before = director.Points;

            GameObject go = Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, SpotNear(shrine, 3), Quaternion.identity);
            go.GetComponent<HordeMember>().SetRoaming(true);
            yield return TestWait.Until(() => !shrine.IsHidden, "a mortal sees the Shrine", 10f);

            Assert.AreEqual(VigilanceStage.Uneasy, director.Stage, "a full stage up");
            Assert.AreEqual(VigilanceTuning.Threshold(VigilanceStage.Uneasy), director.Points, 0.01f);
            Assert.AreEqual(1, RegionAnchor.Current.TimesFound);
            HordeTarget target = shrine.GetComponent<HordeTarget>();
            Assert.IsTrue(HordeTarget.All.Contains(target) && target.IsAlive && target.AttractsHorde, "the mortals' target from now on");

            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, RegionAnchor.Current.TimesFound, "found once");
            Assert.Less(director.Points, VigilanceTuning.Threshold(VigilanceStage.Alarmed), "not raised again");
        }

        [UnityTest]
        public IEnumerator The_debug_row_reveals_the_Shrine()
        {
            yield return Land(AnchorKind.Shrine);
            TestDebug.Press("Reveal the Shrine");
            Assert.IsFalse(TheAnchor.IsHidden);
            Assert.AreEqual(VigilanceStage.Uneasy, VigilanceDirector.Current.Stage);
            TestDebug.Press("Reveal the Shrine");
            Assert.AreEqual(VigilanceStage.Uneasy, VigilanceDirector.Current.Stage, "an Anchor already found is not found again");
        }

        [UnityTest]
        public IEnumerator A_War_Throne_is_big_and_visible_with_Vigilance_at_Uneasy_and_no_Wave_owed()
        {
            yield return Land(AnchorKind.WarThrone);
            Anchor throne = TheAnchor;
            Assert.AreEqual(AnchorKind.WarThrone, throne.Kind);
            Assert.IsFalse(throne.IsHidden);
            HordeTarget target = throne.GetComponent<HordeTarget>();
            Assert.IsTrue(HordeTarget.All.Contains(target) && target.IsAlive && target.AttractsHorde, "a horde target from the start");
            Assert.AreEqual(VigilanceStage.Uneasy, VigilanceDirector.Current.Stage);
            Assert.AreEqual(0, VigilanceDirector.Current.PendingWaves, "starting at Uneasy sends no Wave");
            Assert.Greater(throne.Building.Asset.Size.x, RegionLayout.AnchorSize, "bigger than the Main Base was");
        }

        [UnityTest]
        public IEnumerator A_War_Throne_gives_demons_in_range_a_Damage_Bonus_and_takes_it_off_when_they_leave()
        {
            yield return Land(AnchorKind.WarThrone);
            Anchor throne = TheAnchor;
            Player.Character.Teleport(throne.transform.position + new Vector3(0f, 3f, 0f));
            yield return TestWait.Until(() => RegionAnchor.Current.IsBuffed(JackEntity), "the aura reaches Jack", 3f);
            yield return new WaitForSeconds(0.5f);   // the corruption weakness settles too
            float inside = DamageBonus(JackEntity);
            float weakInside = CorruptionWeakness.Current.IsWeak(JackEntity) ? CorruptionWeakness.Penalty : 0f;

            Player.Character.Teleport(throne.transform.position + new Vector3(AnchorTuning.AuraRadius + 8f, 0f, 0f));
            yield return TestWait.Until(() => !RegionAnchor.Current.IsBuffed(JackEntity), "Jack leaves the aura", 3f);
            yield return new WaitForSeconds(0.5f);
            float outside = DamageBonus(JackEntity);
            float weakOutside = CorruptionWeakness.Current.IsWeak(JackEntity) ? CorruptionWeakness.Penalty : 0f;
            Assert.AreEqual(AnchorTuning.AuraDamageBonus + weakInside - weakOutside, inside - outside, 0.01f, "the aura's Damage Bonus is on inside and off outside");
        }

        [UnityTest]
        public IEnumerator A_Shrine_gives_no_aura_even_in_range()
        {
            yield return Land(AnchorKind.Shrine);
            TestDebug.Press("Reveal the Shrine");
            Player.Character.Teleport(TheAnchor.transform.position + new Vector3(0f, 3f, 0f));
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(RegionAnchor.Current.IsBuffed(JackEntity));
        }

        [UnityTest]
        public IEnumerator A_War_Throne_spreads_corruption_beyond_the_reach_of_an_ordinary_Anchor()
        {
            yield return Land(AnchorKind.WarThrone);
            Anchor throne = TheAnchor;
            Int2 at = _grid.CellAt(throne.transform.position);
            int beyond = CorruptionTuning.Anchor.Radius + 1;
            int sign = at.x < _grid.Width / 2 ? 1 : -1;
            var far = new Int2(at.x + sign * beyond, at.y);
            Assert.AreEqual(0, RegionCorruption.Current.Level(far), "starts unspread");
            Time.timeScale = 4f;
            yield return TestWait.Until(() => RegionCorruption.Current.Level(far) >= 1, "the War Throne's corruption reaches past an ordinary Anchor's", 40f);
        }

        [UnityTest]
        public IEnumerator Losing_the_Anchor_lets_a_new_one_be_raised_on_corrupted_ground_once_and_at_a_cost()
        {
            yield return Land(AnchorKind.WarThrone);
            Building raised = RegionAnchor.Current.TryRaise(out string reason);
            Assert.IsNull(raised);
            StringAssert.Contains("already stands", reason, "one at a time");

            TestDebug.Press("Destroy the Anchor");
            yield return TestWait.Until(() => RegionAnchor.Standing() == null, "the Anchor falls");
            yield return TestWait.Frames(3);

            EmptyPile();
            Assert.IsNull(RegionAnchor.Current.TryRaise(out reason), "no Souls or Stone");
            StringAssert.Contains("Resources", reason);

            AnchorTuningCosts(out int souls, out int stone);
            Give(souls, stone);
            raised = RegionAnchor.Current.TryRaise(out reason);
            Assert.IsNotNull(raised, reason);
            Assert.AreEqual(0, _pile.Get(ResourceType.Souls));
            Assert.AreEqual(0, _pile.Get(ResourceType.Stone));
            Assert.AreEqual(AnchorKind.WarThrone, AnchorCatalog.KindOf(raised), "the Run's kind");
            Assert.IsTrue(raised.IsMainBase && raised.IsPlayers);

            Give(souls, stone);
            Assert.IsNull(RegionAnchor.Current.TryRaise(out reason), "one at a time, even while it goes up");
            StringAssert.Contains("already stands", reason);

            Assert.IsNull(RegionSouls.Current.Anchor, "nothing to revive at until it is finished");
            raised.FinishConstruction();
            Assert.AreSame(raised, RegionSouls.Current.Anchor, "Jack revives there");
        }

        [UnityTest]
        public IEnumerator A_new_Anchor_is_refused_off_corrupted_ground_with_the_reason()
        {
            yield return Land(AnchorKind.Shrine);
            TestDebug.Press("Destroy the Anchor");
            yield return TestWait.Until(() => RegionAnchor.Standing() == null, "the Anchor falls");
            AnchorTuningCosts(out int souls, out int stone);
            Give(souls, stone);

            var middle = new Int2(_grid.Width / 2, _grid.Height / 2);
            Assert.AreEqual(0, RegionCorruption.Current.Level(middle), "the middle of the map is clean");
            Player.Character.Teleport(_grid.CentreOf(middle));
            yield return TestWait.Frames(2);

            int held = _pile.Get(ResourceType.Souls);
            Assert.IsNull(RegionAnchor.Current.TryRaise(out string reason));
            StringAssert.Contains("not corrupted enough", reason);
            Assert.AreEqual(held, _pile.Get(ResourceType.Souls), "nothing was spent");
        }

        [UnityTest]
        public IEnumerator A_Shrine_raised_after_losing_the_first_is_hidden_again()
        {
            yield return Land(AnchorKind.Shrine);
            TestDebug.Press("Destroy the Anchor");
            yield return TestWait.Until(() => RegionAnchor.Standing() == null, "the Anchor falls");
            yield return TestWait.Frames(3);
            AnchorTuningCosts(out int souls, out int stone);
            Give(souls, stone);
            Building raised = RegionAnchor.Current.TryRaise(out string reason);
            Assert.IsNotNull(raised, reason);
            Assert.IsTrue(raised.GetComponent<Anchor>().IsHidden);
        }

        private static void AnchorTuningCosts(out int souls, out int stone)
        {
            souls = stone = 0;
            foreach (ResourceCost c in AnchorCatalog.Asset(AnchorCatalog.Chosen).Costs)
            {
                if (c.Type == ResourceType.Souls) souls = c.Amount;
                if (c.Type == ResourceType.Stone) stone = c.Amount;
            }
        }
    }
}
