using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>Why a Building can or can't go somewhere.</summary>
    public enum Verdict { Ok, Blocked, TooExpensive, NotAllowed, NotCorrupted }

    /// <summary>A placement check's answer: the verdict and the reason to show the player.</summary>
    public readonly struct Placement
    {
        public static readonly Placement Ok = new(Verdict.Ok, "");

        public Placement(Verdict verdict, string reason)
        {
            Verdict = verdict;
            Reason = reason;
        }

        public Verdict Verdict { get; }
        public string Reason { get; }
        public bool IsOk => Verdict == Verdict.Ok;

        public static Placement No(Verdict verdict, string reason) => new(verdict, reason);
    }

    /// <summary>Where a Building is being placed.</summary>
    public readonly struct PlacementQuery
    {
        public PlacementQuery(BuildingAsset asset, Int2 origin, RegionGrid grid, RegionBuildings buildings)
        {
            Asset = asset;
            Origin = origin;
            Grid = grid;
            Buildings = buildings;
        }

        public BuildingAsset Asset { get; }
        public Int2 Origin { get; }
        public RegionGrid Grid { get; }
        public RegionBuildings Buildings { get; }
        public Vector2Int Size => Asset.Size;
        public Vector3 Centre => Grid.CentreOf(Origin, Asset.Size);
    }

    /// <summary>
    /// One condition for placing a Building. <see cref="RegionBuildings"/> checks its rules in order and stops at the
    /// first that says no; new conditions (only on corrupted ground, not near enemies) are new rules, not edits.
    /// </summary>
    public interface IPlacementRule
    {
        Placement Check(in PlacementQuery query);
    }

    /// <summary>Every footprint cell is open ground with no other Building on it.</summary>
    public sealed class FreeGroundRule : IPlacementRule
    {
        public Placement Check(in PlacementQuery q) =>
            q.Buildings.IsFree(q.Origin, q.Size) ? Placement.Ok : Placement.No(Verdict.Blocked, "Blocked");
    }

    /// <summary>The Region's stockpile can pay for it.</summary>
    public sealed class AffordableRule : IPlacementRule
    {
        public Placement Check(in PlacementQuery q)
        {
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            return pile != null && pile.CanAfford(q.Asset.Costs) ? Placement.Ok : Placement.No(Verdict.TooExpensive, "Not enough Resources");
        }
    }

    /// <summary>Every footprint cell is corrupted to level 2 or higher: Buildings go up only on ground the demons have taken.</summary>
    public sealed class CorruptedGroundRule : IPlacementRule
    {
        public const string Reason = "not corrupted enough";

        public Placement Check(in PlacementQuery q)
        {
            RegionCorruption corruption = RegionCorruption.Current;
            if (corruption == null || corruption.Field.CanBuild(q.Origin, new Int2(q.Size.x, q.Size.y))) return Placement.Ok;
            return Placement.No(Verdict.NotCorrupted, Reason);
        }
    }
}
