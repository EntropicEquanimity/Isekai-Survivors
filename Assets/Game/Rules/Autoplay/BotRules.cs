using System.Collections.Generic;

namespace Isekai
{
    /// <summary>What the bot knows about the Region right now, as plain numbers (the Unity side fills it in).</summary>
    public struct BotSnapshot
    {
        /// <summary>Game seconds since the Region started.</summary>
        public float RegionSeconds;
        public int Souls;
        public int Stone;
        public int SoulLevel;
        public int LevelUpCost;
        public int ReviveCost;
        public int PossessCost;
        public VigilanceStage Stage;
        public float HealthFraction;
        public bool Bodiless;
        public bool AnchorStands;

        /// <summary>Souls the wisp loses a second while bodiless.</summary>
        public float DrainPerSecond;

        /// <summary>Workers summoned so far.</summary>
        public int Workers;

        /// <summary>The Resources one worker and one Portal cost.</summary>
        public int WorkerSouls;
        public int PortalStone;
        public int PortalSouls;

        /// <summary>Portals placed (finished or going up) and demons alive from them.</summary>
        public int Portals;
        public int Units;

        /// <summary>Whether the Region has a Stone node left to mine, a mortal or beast worth hunting, and ground for a Portal.</summary>
        public bool StoneLeft;
        public bool PreyLeft;
        public bool PortalSpot;
        public bool SeatKnown;

        /// <summary>The raid is on (once sent, it stays on while there are demons to send).</summary>
        public bool Raiding;

        /// <summary>Your demons and the mortals within the Seat's zone, and how far the Seat is from Jack.</summary>
        public int DemonsAtSeat;
        public int SeatDefenders;
        public float SeatDistance;

        /// <summary>What a wisp would drain a second were Jack to die now.</summary>
        public float DeathDrainPerSecond;
    }

    /// <summary>What the bot buys next with what it holds.</summary>
    public enum BotBuy { Nothing, Worker, Portal, SoulLevel }

    /// <summary>Where the bot walks Jack.</summary>
    public enum BotMove
    {
        /// <summary>Stand where he is: his weapons and auto-fight take care of what comes near.</summary>
        Hold,

        /// <summary>Walk to a mortal or beast to kill it for Souls.</summary>
        Hunt,

        /// <summary>Walk to a Stone node and mine it.</summary>
        Mine,

        /// <summary>Fall back to the Anchor and hold there.</summary>
        Retreat,

        /// <summary>Walk to the Seat's keep with the raid.</summary>
        Assault,
    }

    /// <summary>A mortal the bot may go and kill, as plain numbers.</summary>
    public readonly struct PreyCandidate
    {
        public PreyCandidate(float distance, float distanceFromAnchor, int witnesses, int danger, int company)
        {
            Distance = distance;
            DistanceFromAnchor = distanceFromAnchor;
            Witnesses = witnesses;
            Danger = danger;
            Company = company;
        }

        /// <summary>From Jack, and from the Anchor.</summary>
        public float Distance { get; }
        public float DistanceFromAnchor { get; }

        /// <summary>Other mortals who would see the kill.</summary>
        public int Witnesses { get; }

        /// <summary>How dangerous its own kind is (<see cref="BotRules.DangerOf"/>), and how many mortals and beasts are around it.</summary>
        public int Danger { get; }
        public int Company { get; }

        public int Threat => Danger + Company;
    }

    /// <summary>What a bodiless Jack steers the wisp to.</summary>
    public enum BodilessPlan { ToAnchor, ToDemon }

    /// <summary>
    /// The bot's decisions, engine-free: what to buy, where to walk, when to raid, when to retreat and what to do without
    /// a body. Each takes the <see cref="AutoplayStrategy"/> and a <see cref="BotSnapshot"/>.
    /// </summary>
    public static class BotRules
    {
        /// <summary>
        /// Whether the demons are sent at the Seat. The strategy wants Portals up, enough demons and its time; or time has
        /// run out on waiting (then it raids with whatever it has). Once sent, a raid goes on while there are demons to send.
        /// </summary>
        public static bool ShouldRaid(AutoplayStrategy strategy, in BotSnapshot s)
        {
            if (!s.SeatKnown) return false;
            if (s.Raiding && s.Units > 0) return true;
            if (s.RegionSeconds >= strategy.AssaultAfterSeconds) return true;
            return s.Portals >= strategy.PortalsBeforeRaid && s.Units >= strategy.UnitsToRaid && s.RegionSeconds >= strategy.RaidNotBeforeSeconds;
        }

        /// <summary>
        /// Whether Jack goes with the raid. A body lost at the Seat is far from the Anchor, so he goes once the demons are there in
        /// number and holding their own, and only when the Souls would see him through a death there; only waiting twice as long as
        /// the strategy meant to (<see cref="AutoplayStrategy.AssaultAfterSeconds"/>) sends him whatever.
        /// </summary>
        public static bool JackJoinsRaid(AutoplayStrategy strategy, in BotSnapshot s)
        {
            if (!ShouldRaid(strategy, s)) return false;
            if (s.RegionSeconds >= strategy.AssaultAfterSeconds * 2f) return true;
            return s.DemonsAtSeat >= strategy.JackJoinsAtDemons && s.DemonsAtSeat * 2 >= s.SeatDefenders && CanSurviveDeathAtSeat(s);
        }

        /// <summary>
        /// Whether the Souls in hand would see Jack through a death at the Seat: they pay for the wisp's flight home and the revive,
        /// or, with demons about, for possessing one of them a few seconds from where the body fell.
        /// </summary>
        public static bool CanSurviveDeathAtSeat(in BotSnapshot s) =>
            s.Souls >= s.ReviveCost + s.DeathDrainPerSecond * s.SeatDistance / WispSpeed
            || (s.DemonsAtSeat > 0 && s.Souls >= s.PossessCost + s.DeathDrainPerSecond * PossessFlightSeconds);

        /// <summary>The seconds a wisp is allowed to fly to a demon at the Seat when working out whether a death there can be survived.</summary>
        public const float PossessFlightSeconds = 4f;

        /// <summary>How many of <paramref name="portals"/> Portals raid while the rest stay on Guard (at least one raids once the raid is on, however many are asked to stay).</summary>
        public static int RaidingPortals(AutoplayStrategy strategy, int portals) =>
            portals <= 0 ? 0 : System.Math.Max(1, portals - System.Math.Max(0, strategy.HomeGuardPortals));

        /// <summary>Whether Jack falls back to the Anchor: hurt, with an Anchor to fall back to.</summary>
        public static bool ShouldRetreat(AutoplayStrategy strategy, in BotSnapshot s) =>
            s.AnchorStands && !s.Bodiless && s.HealthFraction < strategy.RetreatHealth;

        /// <summary>Portals the strategy builds in all: those before the raid and one more at least, to keep the force growing.</summary>
        public static int PortalsWanted(AutoplayStrategy strategy) => System.Math.Max(strategy.PortalsBeforeRaid + 1, strategy.PortalsTotal);

        /// <summary>What to buy now (one thing; the bot asks again next time it thinks).</summary>
        public static BotBuy NextBuy(AutoplayStrategy strategy, in BotSnapshot s)
        {
            if (s.Bodiless) return BotBuy.Nothing;
            int reserve = Reserve(s);
            if (s.Portals < PortalsWanted(strategy) && s.PortalSpot && s.Stone >= s.PortalStone && s.Souls >= s.PortalSouls + reserve) return BotBuy.Portal;
            bool portalsDone = s.Portals >= strategy.PortalsBeforeRaid || !s.PortalSpot;
            if (s.Workers < strategy.WorkersWanted && s.Portals >= 1 && s.Souls >= s.WorkerSouls + reserve) return BotBuy.Worker;
            if (portalsDone && s.SoulLevel < strategy.SoulLevelTarget && s.Souls >= s.LevelUpCost + s.ReviveCost) return BotBuy.SoulLevel;
            return BotBuy.Nothing;
        }

        /// <summary>Where to walk Jack now.</summary>
        public static BotMove NextMove(AutoplayStrategy strategy, in BotSnapshot s)
        {
            bool joins = JackJoinsRaid(strategy, s);
            // Hurt or not, Jack goes when the Souls would see him back from a death at the Seat: the revive makes him whole again.
            if (ShouldRetreat(strategy, s) && !(joins && CanSurviveDeathAtSeat(s))) return BotMove.Retreat;
            if (joins) return BotMove.Assault;
            bool overCeiling = s.Stage > strategy.VigilanceCeiling;
            if (s.Portals < PortalsWanted(strategy) && s.PortalSpot && s.Stone < s.PortalStone && s.StoneLeft) return BotMove.Mine;
            if (overCeiling) return s.AnchorStands ? BotMove.Retreat : BotMove.Hold;
            if (NeedsSouls(strategy, s) && s.PreyLeft && s.Units < DemonsThatHunt) return BotMove.Hunt;
            return BotMove.Hold;
        }

        /// <summary>The Souls kept back for a revive once there is a Portal (the first one is bought with whatever there is).</summary>
        public static int Reserve(in BotSnapshot s) => s.Portals >= 1 ? s.ReviveCost : 0;

        /// <summary>Whether the next thing the bot wants to buy is held back by Souls.</summary>
        public static bool NeedsSouls(AutoplayStrategy strategy, in BotSnapshot s)
        {
            int reserve = Reserve(s);
            if (s.Portals < PortalsWanted(strategy) && s.PortalSpot && s.Souls < s.PortalSouls + reserve) return true;
            if (s.Portals >= 1 && s.Workers < strategy.WorkersWanted && s.Souls < s.WorkerSouls + reserve) return true;
            if (s.SoulLevel < strategy.SoulLevelTarget && s.Souls < s.LevelUpCost + s.ReviveCost) return true;
            // With everything bought, a few Souls in hand still keep a revive or a possession affordable.
            return s.Souls < s.ReviveCost;
        }

        /// <summary>What a bodiless Jack heads for: the Anchor when he can pay to revive, a demon when he can pay to possess it.</summary>
        public static BodilessPlan WhenBodiless(in BotSnapshot s, bool demonsAlive) =>
            WhenBodiless(s, s.AnchorStands ? 0f : -1f, demonsAlive ? 0f : -1f);

        /// <summary>The wisp's speed in cells a second.</summary>
        public const float WispSpeed = 6f;

        /// <summary>
        /// What a bodiless Jack heads for, given how far the Anchor and the nearest demon are (negative: there is none). The
        /// wisp drains on the way, so a revive that costs more than is left on arrival is no use, and a demon near at hand is
        /// the better way back when the Anchor is too far to reach alive.
        /// </summary>
        public static BodilessPlan WhenBodiless(in BotSnapshot s, float anchorDistance, float demonDistance)
        {
            bool anchor = s.AnchorStands && anchorDistance >= 0f;
            bool demon = demonDistance >= 0f;
            if (anchor && s.Souls - s.DrainPerSecond * anchorDistance / WispSpeed >= s.ReviveCost) return BodilessPlan.ToAnchor;
            if (demon && s.Souls - s.DrainPerSecond * demonDistance / WispSpeed >= s.PossessCost) return BodilessPlan.ToDemon;
            if (anchor && demon) return anchorDistance <= demonDistance ? BodilessPlan.ToAnchor : BodilessPlan.ToDemon;
            return anchor || !demon ? BodilessPlan.ToAnchor : BodilessPlan.ToDemon;
        }

        /// <summary>How far from the Anchor the bot goes hunting, and how much further for each stage of desperation.</summary>
        public const float HuntRadius = 45f;
        public const float HuntRadiusPerDesperation = 15f;

        /// <summary>Game seconds without a kill that make the bot one stage more desperate (it hunts further afield and takes on more).</summary>
        public const float SecondsPerDesperation = 30f;
        public const int MaxDesperation = 4;

        /// <summary>Jack hunts for Souls only while he has fewer demons than this: with some, their kills pay for him.</summary>
        public const int DemonsThatHunt = 2;

        /// <summary>The stage of desperation after <paramref name="secondsWithoutKill"/>.</summary>
        public static int Desperation(float secondsWithoutKill) =>
            System.Math.Max(0, System.Math.Min(MaxDesperation, (int)(secondsWithoutKill / SecondsPerDesperation)));

        /// <summary>How dangerous a mortal of this kind is to fight alone: peasants are no danger, priests little, every fighter a good deal.</summary>
        public static int DangerOf(MortalType type) => type switch
        {
            MortalType.Peasant => 0,
            MortalType.Priest => 1,
            _ => 3,
        };

        /// <summary>
        /// The prey worth walking to: the lowest score is the best (distance, plus a penalty per witness when the strategy
        /// avoids being seen, plus a penalty per point of threat). Prey too far from the Anchor, or carrying more threat than the
        /// strategy takes on, is not worth it, until <paramref name="desperation"/> (stages of going without Souls) stretches both.
        /// Returns the index into <paramref name="prey"/>, or -1 when none is.
        /// </summary>
        public static int PickPrey(AutoplayStrategy strategy, IReadOnlyList<PreyCandidate> prey, int desperation = 0)
        {
            const float WitnessPenalty = 8f;
            const float ThreatPenalty = 4f;
            const int MaxWitnesses = 3;
            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < prey.Count; i++)
            {
                PreyCandidate p = prey[i];
                if (p.DistanceFromAnchor > HuntRadius + HuntRadiusPerDesperation * desperation) continue;
                if (strategy.AvoidWitnesses && p.Witnesses > MaxWitnesses) continue;
                if (p.Threat > strategy.MaxPreyThreat + desperation) continue;
                float score = p.Distance + (strategy.AvoidWitnesses ? p.Witnesses * WitnessPenalty : 0f) + p.Threat * ThreatPenalty;
                if (score >= bestScore) continue;
                bestScore = score;
                best = i;
            }
            return best;
        }

        /// <summary>The Region on the Planet map to enter next: the deepest open one (nearest the Capital), by Id on a tie.</summary>
        public static RegionInfo NextRegion(IEnumerable<RegionInfo> available)
        {
            RegionInfo best = null;
            foreach (RegionInfo region in available)
            {
                if (region == null) continue;
                if (best == null || region.Depth > best.Depth || (region.Depth == best.Depth && string.CompareOrdinal(region.Id, best.Id) < 0)) best = region;
            }
            return best;
        }
    }
}
