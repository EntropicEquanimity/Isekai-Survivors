using System;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class StatIdTests
    {
        // Blondie Combat's damage formula and Blondie Controllers' motors look these stats up by name.
        [TestCase("Armor")]
        [TestCase("DamageBonus")]
        [TestCase("CritChance")]
        [TestCase("CritMultiplier")]
        [TestCase("Speed")]
        public void Names_read_by_Blondie_packages_exist(string name)
        {
            Assert.That(Enum.IsDefined(typeof(StatId), name), $"StatId.{name} is missing; a Blondie package reads it by name.");
        }

        // Blondie Stats finds a vital's max as its id + "Max".
        [TestCase(StatId.Health)]
        public void Vitals_have_their_max_stat(StatId vital)
        {
            Assert.That(Enum.IsDefined(typeof(StatId), vital + "Max"), $"StatId.{vital}Max is missing.");
        }
    }
}
