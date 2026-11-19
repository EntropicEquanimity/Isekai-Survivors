using System.Collections.Generic;
using Blondie.Core;
using Blondie.SkillTrees;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Progress kept between Runs: the <see cref="LegacyTree"/> on a <see cref="SkillTreeHolder"/> that lives across
    /// scenes, whose shared points are the Shards. Saved to <see cref="Store"/> (PlayerPrefs by default) on every change.
    /// The Character's stat nodes apply through <see cref="ApplyTo"/>.
    /// </summary>
    public static class MetaProgress
    {
        public const string SaveKey = "isekai.meta";

        private static SkillTreeHolder s_holder;
        private static SkillTreeDefinition s_tree;
        private static IKeyValueStore s_store;

        static MetaProgress() => StaticReset.Register(() =>
        {
            s_holder = null;   // its GameObject goes with Play Mode
            LastEarned = 0;
        });

        /// <summary>Where progress is saved: the game's key-value store unless set (tests use an in-memory one).</summary>
        public static IKeyValueStore Store
        {
            get => s_store ?? KeyValueStorage.Current;
            set => s_store = value;
        }

        public static SkillTreeDefinition Tree => s_tree ??= LegacyTree.Create();

        /// <summary>The holder the skill tree window shows (made, and loaded, on first use).</summary>
        public static SkillTreeHolder Holder
        {
            get
            {
                if (s_holder == null) Build();
                return s_holder;
            }
        }

        public static SkillTreeUnit Unit => Holder.Unit;

        public static int Shards => Unit.SharedPoints;

        /// <summary>Shards the last finished Run earned (shown on the Planet map).</summary>
        public static int LastEarned { get; private set; }

        public static void Earn(int shards)
        {
            LastEarned = Mathf.Max(0, shards);
            if (shards <= 0) return;
            Unit.AddPoints(shards);
            Save();
        }

        public static int Level(string unlockId) => Unit.GetUnlockLevel(unlockId);

        public static int Rank(string node) => Unit.RankOf(Tree, node);

        public static List<string> StartingItems() => LegacyTree.StartingItems(Tree, Unit);

        /// <summary>Puts the stat nodes on <paramref name="stats"/> (the Region's Character), or takes them off with null.</summary>
        public static void ApplyTo(IStatModifierTarget stats)
        {
            if (stats == null && s_holder == null) return;
            Holder.Stats = stats;
        }

        /// <summary>Forgets all progress: no Shards, no upgrades.</summary>
        public static void Wipe()
        {
            Store.DeleteKey(SaveKey);
            LastEarned = 0;
            if (s_holder != null) s_holder.Unit.Restore(new SkillTreeSaveData());
            Save();
        }

        public static void Save()
        {
            if (s_holder == null) return;
            Store.SetString(SaveKey, JsonUtility.ToJson(s_holder.Unit.Capture()));
            Store.Save();
        }

        private static void Build()
        {
            var go = new GameObject("Meta Progress");
            Object.DontDestroyOnLoad(go);
            s_holder = go.AddComponent<SkillTreeHolder>();
            s_holder.Stats = null;   // only ever the Character's, through ApplyTo
            s_holder.AddTree(Tree);
            string json = Store.GetString(SaveKey);
            if (!string.IsNullOrEmpty(json)) s_holder.Unit.Restore(JsonUtility.FromJson<SkillTreeSaveData>(json));
            s_holder.Unit.Changed += Save;
        }
    }
}
