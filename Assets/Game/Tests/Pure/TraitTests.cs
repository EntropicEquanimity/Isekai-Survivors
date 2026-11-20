using System;
using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>The Traits as data: the table, who may have which, the roll by rank, and how each weights the shared behaviours.</summary>
    public class TraitTests
    {
        private static readonly Side[] Sides = { Side.Yours, Side.Mortals, Side.Neutral, Side.Hostile, Side.Rogue };
        private static readonly UnitKind[] Kinds = { UnitKind.Demon, UnitKind.Mortal, UnitKind.Wildlife };

        private static BehaviourContext Hurt(float health, bool threat = true)
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.Health = health;
            c.ThreatSeen = threat;
            c.ThreatCloseness = 0.5f;
            return c;
        }

        private static BehaviourAction Decide(BehaviourProfile p, BehaviourContext c, BehaviourAction current = BehaviourAction.Fight) => BehaviourScoring.Choose(p, c, current);

        [Test]
        public void Every_trait_of_the_spec_has_a_row_with_a_description_and_something_to_do()
        {
            Assert.AreEqual(9, TraitTable.All.Count);
            foreach (TraitId id in Enum.GetValues(typeof(TraitId)))
            {
                TraitDef def = TraitTable.Get(id);
                Assert.AreEqual(id, def.Id);
                Assert.IsNotEmpty(def.Description, id + " has a tooltip line");
                Assert.IsTrue(def.Weights.Length > 0 || def.Effects != TraitEffect.None, id + " weights a behaviour or switches something on");
            }
        }

        [Test]
        public void Each_trait_comes_with_the_buff_of_the_spec_table()
        {
            TraitBuff Only(TraitId id) => TraitTable.Get(id).Buffs.Single();
            Assert.AreEqual((TraitStat.HealthMax, 0.30f), (Only(TraitId.Cowardly).Stat, Only(TraitId.Cowardly).Value));
            Assert.AreEqual((TraitStat.DamageBonus, 0.20f), (Only(TraitId.Berserk).Stat, Only(TraitId.Berserk).Value));
            Assert.AreEqual(TraitStat.Armor, Only(TraitId.Brave).Stat);
            Assert.AreEqual((TraitStat.DamageBonus, BuffWhen.WhileConcealed, 0.25f), (Only(TraitId.Sneaky).Stat, Only(TraitId.Sneaky).When, Only(TraitId.Sneaky).Value));
            Assert.AreEqual((TraitStat.Speed, BuffWhen.WhileUnseen, 0.30f), (Only(TraitId.Ambusher).Stat, Only(TraitId.Ambusher).When, Only(TraitId.Ambusher).Value));
            Assert.AreEqual((TraitStat.AttackSpeed, 0.15f), (Only(TraitId.Unruly).Stat, Only(TraitId.Unruly).Value));
            Assert.AreEqual((TraitStat.Vision, 0.50f), (Only(TraitId.Vigilant).Stat, Only(TraitId.Vigilant).Value));
            Assert.AreEqual((TraitStat.HealthMax, BuffWhen.NearPost, 0.20f), (Only(TraitId.Protector).Stat, Only(TraitId.Protector).When, Only(TraitId.Protector).Value));
            Assert.IsTrue(TraitTable.Get(TraitId.Purifier).Has(TraitEffect.HealsAllies));
        }

        [Test]
        public void Odds_are_ten_percent_for_ordinary_units_and_twenty_five_for_elites()
        {
            Assert.AreEqual(0.10f, TraitRoller.Odds(TraitRank.Ordinary));
            Assert.AreEqual(0.25f, TraitRoller.Odds(TraitRank.Elite));
            Assert.AreEqual(1f, TraitRoller.Odds(TraitRank.Hero));
        }

        [TestCase(TraitRank.Ordinary, 0.10)]
        [TestCase(TraitRank.Elite, 0.25)]
        public void Rolling_gives_a_trait_at_those_odds_and_never_more_than_one(TraitRank rank, double odds)
        {
            var random = new Random(7);
            const int Rolls = 20000;
            int with = 0;
            for (int i = 0; i < Rolls; i++)
            {
                var traits = TraitRoller.Roll(random, rank, Side.Mortals, UnitKind.Mortal);
                Assert.LessOrEqual(traits.Count, 1);
                if (traits.Count == 1) with++;
            }
            Assert.AreEqual(odds, with / (double)Rolls, 0.015);
        }

        [Test]
        public void A_hero_always_has_two_or_three_distinct_traits_that_do_not_contradict()
        {
            var counts = new int[4];
            for (int seed = 0; seed < 3000; seed++)
            {
                foreach (UnitKind kind in Kinds)
                {
                    var traits = TraitRoller.Roll(new Random(seed), TraitRank.Hero, Side.Mortals, kind);
                    Assert.That(traits.Count, Is.InRange(2, 3), "seed " + seed + " " + kind);
                    Assert.AreEqual(traits.Count, traits.Distinct().Count(), "no duplicates");
                    Assert.LessOrEqual(traits.Count(t => TraitTable.Get(t).Group == 1), 1, "a unit has one way of fleeing");
                    if (kind == UnitKind.Mortal) counts[traits.Count]++;
                }
            }
            Assert.Greater(counts[2], 0);
            Assert.Greater(counts[3], 0);
        }

        [Test]
        public void The_same_seed_rolls_the_same_traits()
        {
            var a = TraitRoller.Roll(new Random(99), TraitRank.Hero, Side.Mortals, UnitKind.Mortal);
            var b = TraitRoller.Roll(new Random(99), TraitRank.Hero, Side.Mortals, UnitKind.Mortal);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void Rolling_respects_eligibility_for_every_side_kind_and_rank()
        {
            for (int seed = 0; seed < 600; seed++)
                foreach (Side side in Sides)
                    foreach (UnitKind kind in Kinds)
                        foreach (TraitRank rank in Enum.GetValues(typeof(TraitRank)))
                            foreach (TraitId id in TraitRoller.Roll(new Random(seed), rank, side, kind))
                                Assert.IsTrue(TraitTable.Get(id).CanHave(side, kind), id + " rolled for " + side + " " + kind);
        }

        [Test]
        public void A_purifier_is_never_rolled_for_a_demon_whatever_the_seed()
        {
            Assert.IsFalse(TraitTable.Eligible(Side.Yours, UnitKind.Demon).Any(d => d.Id == TraitId.Purifier));
            foreach (Side side in Sides)
                Assert.IsFalse(TraitTable.Get(TraitId.Purifier).CanHave(side, UnitKind.Demon), "no Purifier demon on " + side);
            for (int seed = 0; seed < 5000; seed++)
                foreach (TraitRank rank in Enum.GetValues(typeof(TraitRank)))
                    CollectionAssert.DoesNotContain(TraitRoller.Roll(new Random(seed), rank, Side.Yours, UnitKind.Demon), TraitId.Purifier);
        }

        [Test]
        public void Each_trait_names_who_can_have_it()
        {
            Assert.IsTrue(TraitTable.Get(TraitId.Purifier).CanHave(Side.Mortals, UnitKind.Mortal));
            Assert.IsFalse(TraitTable.Get(TraitId.Purifier).CanHave(Side.Hostile, UnitKind.Wildlife));
            Assert.IsTrue(TraitTable.Get(TraitId.Unruly).CanHave(Side.Yours, UnitKind.Demon), "orders are a demon's");
            Assert.IsFalse(TraitTable.Get(TraitId.Unruly).CanHave(Side.Mortals, UnitKind.Mortal));
            Assert.IsTrue(TraitTable.Get(TraitId.Cowardly).CanHave(Side.Yours, UnitKind.Demon));
            Assert.IsFalse(TraitTable.Get(TraitId.Cowardly).CanHave(Side.Mortals, UnitKind.Building));
        }

        [TestCase(MortalType.Soldier)]
        [TestCase(MortalType.Knight)]
        [TestCase(MortalType.Peasant)]
        public void A_cowardly_mortal_flees_below_75_percent_health_and_not_above(MortalType type)
        {
            BehaviourProfile cowardly = TraitTable.Get(TraitId.Cowardly).Apply(MortalProfiles.For(type).Behaviour);
            Assert.AreEqual(BehaviourAction.Flee, Decide(cowardly, Hurt(0.70f)));
            Assert.AreNotEqual(BehaviourAction.Flee, Decide(cowardly, Hurt(0.80f, threat: false), BehaviourAction.Idle));
            if (type != MortalType.Peasant) Assert.AreEqual(BehaviourAction.Fight, Decide(cowardly, Hurt(0.80f)), "above the line it fights as before");
        }

        [Test]
        public void A_cowardly_demon_can_flee_though_its_type_never_does()
        {
            BehaviourProfile imp = DemonProfiles.For(DemonType.Imp);
            Assert.IsFalse(imp.Of(BehaviourAction.Flee).Enabled);
            BehaviourProfile cowardly = TraitTable.Get(TraitId.Cowardly).Apply(imp);
            Assert.AreEqual(BehaviourAction.Flee, Decide(cowardly, Hurt(0.5f)));
            Assert.AreEqual(BehaviourAction.Fight, Decide(cowardly, Hurt(0.9f)));
        }

        [TestCase(MortalType.Soldier)]
        [TestCase(MortalType.Peasant)]
        [TestCase(MortalType.Knight)]
        public void A_berserk_mortal_never_flees_however_hurt_or_afraid(MortalType type)
        {
            BehaviourProfile berserk = TraitTable.Get(TraitId.Berserk).Apply(MortalProfiles.For(type).Behaviour);
            BehaviourContext c = Hurt(0.02f);
            c.Fear = 1f;
            c.AlarmSeen = true;
            Assert.AreNotEqual(BehaviourAction.Flee, Decide(berserk, c));
            Assert.IsFalse(berserk.Of(BehaviourAction.Flee).Enabled);
        }

        [Test]
        public void A_brave_mortal_holds_until_ten_percent_health()
        {
            BehaviourProfile brave = TraitTable.Get(TraitId.Brave).Apply(MortalProfiles.For(MortalType.Soldier).Behaviour);
            BehaviourContext c = Hurt(0.3f);
            c.Fear = 1f;
            Assert.AreEqual(BehaviourAction.Fight, Decide(brave, c), "a soldier would flee at 30% when afraid; a brave one fights on");
            Assert.AreEqual(BehaviourAction.Fight, Decide(brave, Hurt(0.12f)));
            Assert.AreEqual(BehaviourAction.Flee, Decide(brave, Hurt(0.08f)));
        }

        [Test]
        public void A_purifier_removes_corruption_which_a_priest_already_does_and_a_soldier_does_not()
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.CorruptionNear = true;
            BehaviourProfile soldier = MortalProfiles.For(MortalType.Soldier).Behaviour;
            Assert.IsFalse(soldier.Of(BehaviourAction.Purify).Enabled);
            BehaviourProfile purifier = TraitTable.Get(TraitId.Purifier).Apply(soldier);
            Assert.AreEqual(BehaviourAction.Purify, Decide(purifier, c, BehaviourAction.Idle));

            BehaviourProfile priest = MortalProfiles.For(MortalType.Priest).Behaviour;
            Assert.AreEqual(priest.Of(BehaviourAction.Purify).Terms.Length, TraitTable.Get(TraitId.Purifier).Apply(priest).Of(BehaviourAction.Purify).Terms.Length, "a priest keeps its own weights");
        }

        [Test]
        public void A_protector_wants_home_sooner_than_others()
        {
            BehaviourContext c = BehaviourContext.Calm;
            c.Leash = 10f;
            c.DistanceHome = 4.5f;
            BehaviourProfile guard = MortalProfiles.For(MortalType.Soldier).Behaviour;
            BehaviourProfile protector = TraitTable.Get(TraitId.Protector).Apply(guard);
            Assert.Greater(BehaviourScoring.Score(protector, BehaviourAction.ReturnHome, c), BehaviourScoring.Score(guard, BehaviourAction.ReturnHome, c));
        }

        [Test]
        public void A_trait_makes_its_own_copy_and_leaves_the_types_profile_alone()
        {
            BehaviourProfile soldier = MortalProfiles.For(MortalType.Soldier).Behaviour;
            float before = BehaviourScoring.Score(soldier, BehaviourAction.Flee, Hurt(0.5f));
            TraitTable.Apply(soldier, new[] { TraitId.Cowardly, TraitId.Protector });
            TraitTable.Get(TraitId.Berserk).Apply(soldier);
            Assert.AreEqual(before, BehaviourScoring.Score(soldier, BehaviourAction.Flee, Hurt(0.5f)));
            Assert.IsTrue(soldier.Of(BehaviourAction.Flee).Enabled);
        }
    }
}
