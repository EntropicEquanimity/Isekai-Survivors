using NUnit.Framework;

namespace Isekai.Tests
{
    public class SoulTests
    {
        [Test]
        public void Drain_is_one_plus_a_fifth_per_soul_level_plus_deaths_this_Run()
        {
            Assert.AreEqual(1.2f, SoulRules.DrainPerSecond(1, 0, false), 1e-4f);
            Assert.AreEqual(3f, SoulRules.DrainPerSecond(5, 1, false), 1e-4f);
            Assert.AreEqual(2f * 3f, SoulRules.DrainPerSecond(5, 1, true), 1e-4f, "doubled near priests or on holy ground");
        }

        [Test]
        public void A_Region_starts_with_Souls_for_two_Portals_and_a_revive()
        {
            const int PortalSouls = 5;
            Assert.GreaterOrEqual(SoulTuning.RegionStartingSouls, 2 * PortalSouls + SoulRules.ReviveCost(SoulTuning.StartingLevel),
                "the first mistakes are not the end of the Run");
            Assert.Less(SoulTuning.RegionStartingSouls, SoulRules.LevelUpCost(SoulTuning.StartingLevel) + SoulRules.ReviveCost(SoulTuning.StartingLevel),
                "but it is not a free soul level");
        }

        [Test]
        public void Revive_costs_twenty_plus_two_per_soul_level()
        {
            Assert.AreEqual(22, SoulRules.ReviveCost(1));
            Assert.AreEqual(30, SoulRules.ReviveCost(5));
        }

        [Test]
        public void Possessing_needs_the_Souls_the_cost_asks()
        {
            Assert.IsFalse(SoulRules.CanPossess(82, 1, bodyAlive: true));
            Assert.IsTrue(SoulRules.CanPossess(83, 1, bodyAlive: true));
            Assert.IsFalse(SoulRules.CanPossess(109, 1, bodyAlive: false), "bodiless pays the full price");
            Assert.IsTrue(SoulRules.CanPossess(110, 1, bodyAlive: false));
        }

        [Test]
        public void Only_a_living_Demon_King_body_spreads_and_an_ordinary_demon_is_weaker()
        {
            Assert.IsTrue(SoulRules.BodySpreads(demonKing: true, alive: true));
            Assert.IsFalse(SoulRules.BodySpreads(demonKing: true, alive: false));
            Assert.IsFalse(SoulRules.BodySpreads(demonKing: false, alive: true), "Jack in an ordinary demon spreads none");
            Assert.AreEqual(1f, SoulRules.BodyMultiplier(true));
            Assert.AreEqual(0.7f, SoulRules.BodyMultiplier(false), 1e-5f);
        }

        [Test]
        public void Summoning_a_Demon_King_costs_more_than_possessing()
        {
            Assert.AreEqual(220, SoulRules.SummonKingCost(1));
            Assert.Greater(SoulRules.SummonKingCost(1), SoulRules.PossessCost(1, bodyAlive: false));
        }

        [Test]
        public void Possession_costs_a_hundred_plus_ten_per_soul_level_and_a_quarter_less_while_alive()
        {
            Assert.AreEqual(110, SoulRules.PossessCost(1, bodyAlive: false));
            Assert.AreEqual(83, SoulRules.PossessCost(1, bodyAlive: true), "82.5 rounds up");
            Assert.AreEqual(150, SoulRules.PossessCost(5, bodyAlive: false));
            Assert.AreEqual(113, SoulRules.PossessCost(5, bodyAlive: true));
        }

        [Test]
        public void Raising_soul_level_costs_fifty_times_the_level()
        {
            Assert.AreEqual(50, SoulRules.LevelUpCost(1));
            Assert.AreEqual(200, SoulRules.LevelUpCost(4));
        }

        [Test]
        public void Zero_Souls_while_bodiless_loses_the_Run_and_not_otherwise()
        {
            Assert.IsTrue(SoulRules.IsLost(0, bodiless: true));
            Assert.IsFalse(SoulRules.IsLost(1, bodiless: true));
            Assert.IsFalse(SoulRules.IsLost(0, bodiless: false), "with a body there is nothing to drain");
        }

        [Test]
        public void A_body_dying_with_zero_Souls_loses_at_once()
        {
            var ledger = new SoulLedger();
            ledger.RecordDeath();
            Assert.IsTrue(SoulRules.IsLost(0, bodiless: true));
            Assert.AreEqual(1, ledger.Deaths);
        }

        [Test]
        public void A_revive_needs_an_Anchor_and_enough_Souls()
        {
            Assert.IsTrue(SoulRules.CanRevive(22, 1, anchorStands: true));
            Assert.IsFalse(SoulRules.CanRevive(21, 1, anchorStands: true));
            Assert.IsFalse(SoulRules.CanRevive(500, 1, anchorStands: false), "no Anchor, no revive");
        }

        [Test]
        public void Only_mortals_and_wildlife_give_Souls()
        {
            Assert.Greater(SoulRules.SoulsForKill(UnitKind.Mortal), 0);
            Assert.Greater(SoulRules.SoulsForKill(UnitKind.Wildlife), 0);
            Assert.AreEqual(0, SoulRules.SoulsForKill(UnitKind.Demon));
            Assert.AreEqual(0, SoulRules.SoulsForKill(UnitKind.Building));
        }

        [Test]
        public void The_drain_pays_whole_Souls_and_keeps_the_fraction()
        {
            var drain = new SoulDrain();
            int paid = 0;
            for (int i = 0; i < 10; i++) paid += drain.Tick(0.25f, 1.2f);
            Assert.AreEqual(3, paid, "2.5 seconds at 1.2 a second");
            paid += drain.Tick(0.5f, 1.2f);
            Assert.AreEqual(3, paid, "3.1 so far: the extra tenth waits");
            paid += drain.Tick(1f, 1.2f);
            Assert.AreEqual(4, paid);
        }

        [Test]
        public void Soul_level_is_bought_with_Souls_and_a_refused_payment_changes_nothing()
        {
            var ledger = new SoulLedger();
            int souls = 120;
            Assert.IsTrue(ledger.TryRaiseLevel(c => { if (c > souls) return false; souls -= c; return true; }));
            Assert.AreEqual(2, ledger.Level);
            Assert.AreEqual(70, souls);
            Assert.IsFalse(ledger.TryRaiseLevel(c => { if (c > souls) return false; souls -= c; return true; }), "100 needed");
            Assert.AreEqual(2, ledger.Level);
            Assert.AreEqual(70, souls);
        }

        [Test]
        public void Soul_level_raises_revive_possession_and_drain()
        {
            var ledger = new SoulLedger();
            int revive = ledger.ReviveCost, possess = ledger.PossessCost(false);
            float drain = ledger.DrainPerSecond(false);
            ledger.TryRaiseLevel(_ => true);
            Assert.Greater(ledger.ReviveCost, revive);
            Assert.Greater(ledger.PossessCost(false), possess);
            Assert.Greater(ledger.DrainPerSecond(false), drain);
        }
    }
}
