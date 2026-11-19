using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>Why a Building can or can't go somewhere.</summary>
    public enum Verdict { Ok, Blocked, OutsideBase, TooExpensive, NotAllowed }

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

    /// <summary>The footprint's centre is inside a working Base's build area (<see cref="BaseArea"/>).</summary>
    public sealed class InsideBaseRule : IPlacementRule
    {
        public Placement Check(in PlacementQuery q) =>
            BaseArea.Covers(q.Centre) ? Placement.Ok : Placement.No(Verdict.OutsideBase, "Too far from a Base");
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
}
