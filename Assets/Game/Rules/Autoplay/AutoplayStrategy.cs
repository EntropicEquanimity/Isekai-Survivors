using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>
    /// How the autoplay bot plays Jack, as data: how much it avoids being seen, when it stops provoking the mortals, how
    /// many Portals it builds before it raids, and when it sends the demons (and Jack) at the Seat. The bot reads these
    /// through <see cref="BotRules"/>; changing a number changes the play, not the bot.
    /// </summary>
    public sealed class AutoplayStrategy
    {
        public AutoplayStrategy(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        /// <summary>Hunts only mortals with few witnesses around, and keeps its Portals' demons from leaving bones.</summary>
        public bool AvoidWitnesses { get; set; }

        /// <summary>Above this Vigilance stage the bot stops hunting and holds at the Anchor (it still mines).</summary>
        public VigilanceStage VigilanceCeiling { get; set; } = VigilanceStage.Crusade;

        /// <summary>Portals built (placed) before the demons raid.</summary>
        public int PortalsBeforeRaid { get; set; } = 2;

        /// <summary>Portals built in all, raid or not (the more, the bigger the force; each costs Stone to mine). At least one more than <see cref="PortalsBeforeRaid"/>.</summary>
        public int PortalsTotal { get; set; }

        /// <summary>Demons alive before a raid is worth sending.</summary>
        public int UnitsToRaid { get; set; } = 4;

        /// <summary>Portals that stay on Guard at the Anchor while the rest raid, so a Wave that comes while the force is away finds something at home.</summary>
        public int HomeGuardPortals { get; set; }

        /// <summary>Demons at the Seat before Jack goes with the raid himself (see <see cref="BotRules.JackJoinsRaid"/>); the waiting being over also sends him.</summary>
        public int JackJoinsAtDemons { get; set; } = 6;

        /// <summary>Game seconds into the Region before the demons are sent, however many there are ("raid late").</summary>
        public float RaidNotBeforeSeconds { get; set; }

        /// <summary>Game seconds into the Region after which the Seat is raided with whatever there is, Jack included.</summary>
        public float AssaultAfterSeconds { get; set; } = 600f;

        /// <summary>What the Portals' demons do about bone piles.</summary>
        public BonesPolicy Bones { get; set; } = BonesPolicy.Harvest;

        /// <summary>
        /// The most threat a mortal may carry for the bot to still go and kill it: how dangerous its own kind is
        /// (<see cref="BotRules.DangerOf"/>) plus the mortals and beasts keeping it company. A band is too much for Jack alone.
        /// </summary>
        public int MaxPreyThreat { get; set; } = 3;

        /// <summary>Workers summoned at the Anchor.</summary>
        public int WorkersWanted { get; set; }

        /// <summary>The soul level the bot raises Jack to when it can afford to.</summary>
        public int SoulLevelTarget { get; set; } = 2;

        /// <summary>Below this fraction of health Jack falls back to the Anchor.</summary>
        public float RetreatHealth { get; set; } = 0.35f;

        public override string ToString() => Name;
    }

    /// <summary>The strategies the bot knows by name.</summary>
    public static class AutoplayStrategies
    {
        /// <summary>Sneaks: few witnesses, a low Vigilance ceiling, bones destroyed, and a late raid with a big force.</summary>
        public static readonly AutoplayStrategy Stealth = new("stealth")
        {
            AvoidWitnesses = true,
            VigilanceCeiling = VigilanceStage.Uneasy,
            PortalsBeforeRaid = 3,
            PortalsTotal = 4,
            UnitsToRaid = 8,
            RaidNotBeforeSeconds = 420f,
            AssaultAfterSeconds = 900f,
            HomeGuardPortals = 1,
            JackJoinsAtDemons = 8,
            Bones = BonesPolicy.Destroy,
            MaxPreyThreat = 1,
            WorkersWanted = 2,
            SoulLevelTarget = 3,
            RetreatHealth = 0.5f,
        };

        /// <summary>Portals fast and an early raid, whatever the mortals notice.</summary>
        public static readonly AutoplayStrategy Rush = new("rush")
        {
            AvoidWitnesses = false,
            VigilanceCeiling = VigilanceStage.Crusade,
            PortalsBeforeRaid = 2,
            PortalsTotal = 4,
            UnitsToRaid = 3,
            RaidNotBeforeSeconds = 90f,
            AssaultAfterSeconds = 480f,
            JackJoinsAtDemons = 4,
            Bones = BonesPolicy.Ignore,
            MaxPreyThreat = 4,
            WorkersWanted = 0,
            SoulLevelTarget = 2,
            RetreatHealth = 0.3f,
        };

        /// <summary>In between: some caution, a few Portals, a raid once there is a small force.</summary>
        public static readonly AutoplayStrategy Balanced = new("balanced")
        {
            AvoidWitnesses = false,
            VigilanceCeiling = VigilanceStage.Alarmed,
            PortalsBeforeRaid = 3,
            PortalsTotal = 5,
            UnitsToRaid = 5,
            RaidNotBeforeSeconds = 390f,
            AssaultAfterSeconds = 900f,
            HomeGuardPortals = 1,
            Bones = BonesPolicy.Destroy,
            WorkersWanted = 1,
            SoulLevelTarget = 2,
            RetreatHealth = 0.45f,
        };

        public static IReadOnlyList<AutoplayStrategy> All { get; } = new[] { Stealth, Rush, Balanced };

        /// <summary>The strategy called <paramref name="name"/> (any case); null when there is none.</summary>
        public static AutoplayStrategy Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (AutoplayStrategy s in All)
                if (string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }
    }
}
