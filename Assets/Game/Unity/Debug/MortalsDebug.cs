using UnityEngine;

namespace Isekai
{
    /// <summary>The debug rows for the mortals with brains: spawn some across the settlements, and what they are doing.</summary>
    [RegionSystem]
    public sealed class MortalsDebug : MonoBehaviour
    {
        private readonly int[] _doing = new int[BehaviourProfile.ActionCount];

        private void OnEnable()
        {
            DebugActions.Button(this, "Mortals", "Spawn 100 townsfolk", () => RegionMortals.Current?.SpawnExtra(100), "townsfolk");
            DebugActions.Info(this, "Info", Readout);
        }

        private void OnDisable() => DebugActions.Remove(this);

        private string Readout()
        {
            RegionMortals mortals = RegionMortals.Current;
            if (mortals == null) return null;
            System.Array.Clear(_doing, 0, _doing.Length);
            foreach (MortalBrain b in mortals.Brains) _doing[(int)b.Action]++;
            return $"Townsfolk {mortals.Brains.Count} · work {_doing[(int)BehaviourAction.Work]} patrol {_doing[(int)BehaviourAction.Patrol]} "
                 + $"fight {_doing[(int)BehaviourAction.Fight]} flee {_doing[(int)BehaviourAction.Flee]} home {_doing[(int)BehaviourAction.ReturnHome]} "
                 + $"purify {_doing[(int)BehaviourAction.Purify]} · fields {mortals.HeldFields}";
        }
    }
}
