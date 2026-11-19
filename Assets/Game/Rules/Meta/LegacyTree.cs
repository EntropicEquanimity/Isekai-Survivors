using System.Collections.Generic;
using Blondie.Core;
using Blondie.SkillTrees;

namespace Isekai
{
    /// <summary>
    /// The meta upgrades bought with Shards between Runs: a Blondie skill tree whose shared points are the Shards. Nodes
    /// grant unlock ids the game reads (<see cref="ExtraPods"/>, <see cref="StartingStone"/>, <see cref="StartingItems"/>)
    /// or stat changes on the Character.
    /// </summary>
    public static class LegacyTree
    {
        public const string Id = "legacy";
        public const string PodsUnlock = "meta.pods";
        public const string RebuildUnlock = "meta.rebuild";
        public const string StoneUnlock = "meta.stone";
        public const string StartItemPrefix = "start:";

        public const int StonePerRank = 15;
        public const float PodRebuildSeconds = 120f;

        /// <summary>A new definition of the tree (positions are grid steps, y up).</summary>
        public static SkillTreeDefinition Create()
        {
            SkillTreeDefinition tree = SkillTreeDefinition.Create(Id);
            tree.DisplayName = "Legacy";
            tree.Add(Node("pods", "Spare Pod", "+1 Cloning Pod in the Main Base", 2, 20, 0, 0).Unlocking(PodsUnlock));
            tree.Add(Node("rebuild", "Pod Rebuilding", $"Used Cloning Pods are rebuilt after {PodRebuildSeconds:0} s", 1, 40, 0, 1).Requires("pods").Unlocking(RebuildUnlock));
            tree.Add(Node("supplies", "Supply Cache", $"+{StonePerRank} Stone at the start of every Region", 3, 10, 1, 0).Unlocking(StoneUnlock));
            tree.Add(Node("satchel", "Packed Satchel", "Start every Run with a Satchel in Storage", 1, 25, 1, 1).Requires("supplies").Unlocking(StartItemPrefix + "satchel"));
            tree.Add(Node("boots", "Travel Boots", "Start every Run with Swift Boots in Storage", 1, 25, 2, 1).Requires("supplies").Unlocking(StartItemPrefix + "swift_boots"));
            tree.Add(Node("hardy", "Hardy", "+10 Character max health", 3, 15, 3, 0).Gives(new NodeModifier(StatId.HealthMax, 10f)));
            tree.Add(Node("might", "Might", "+5% Character damage", 2, 15, 3, 1).Requires("hardy").Gives(new NodeModifier(StatId.DamageBonus, 0.05f)));
            return tree;
        }

        private static SkillNode Node(string id, string name, string description, int maxRank, int cost, float x, float y) =>
            new(id, maxRank, cost) { RawName = name, RawDescription = description, Position = new Float2(x, y) };

        public static int ExtraPods(IUnlockProvider unlocks) => unlocks?.GetUnlockLevel(PodsUnlock) ?? 0;

        public static bool RebuildsPods(IUnlockProvider unlocks) => unlocks != null && unlocks.GetUnlockLevel(RebuildUnlock) > 0;

        public static int StartingStone(IUnlockProvider unlocks) => StonePerRank * (unlocks?.GetUnlockLevel(StoneUnlock) ?? 0);

        /// <summary>Ids of the Items a new Run starts with in Storage.</summary>
        public static List<string> StartingItems(SkillTreeDefinition tree, IUnlockProvider unlocks)
        {
            var items = new List<string>();
            if (tree == null || unlocks == null) return items;
            foreach (SkillNode node in tree.Nodes)
                foreach (string unlock in node.Unlocks)
                    if (unlock.StartsWith(StartItemPrefix) && unlocks.GetUnlockLevel(unlock) > 0 && !items.Contains(unlock.Substring(StartItemPrefix.Length)))
                        items.Add(unlock.Substring(StartItemPrefix.Length));
            return items;
        }
    }
}
