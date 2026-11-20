using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Isekai
{
    internal readonly struct UnitAnimationSet
    {
        public UnitAnimationSet(Sprite[] idle, Sprite[] walk, Sprite[] attack, Sprite[] hurt, Sprite[] death, Sprite[] mine, float idleFps, float walkFps, float attackFps, float hurtFps, float deathFps, float mineFps)
        {
            Idle = idle ?? Array.Empty<Sprite>();
            Walk = walk ?? Array.Empty<Sprite>();
            Attack = attack ?? Array.Empty<Sprite>();
            Hurt = hurt ?? Array.Empty<Sprite>();
            Death = death ?? Array.Empty<Sprite>();
            Mine = mine ?? Array.Empty<Sprite>();
            IdleFps = idleFps;
            WalkFps = walkFps;
            AttackFps = attackFps;
            HurtFps = hurtFps;
            DeathFps = deathFps;
            MineFps = mineFps;
        }

        public Sprite[] Idle { get; }
        public Sprite[] Walk { get; }
        public Sprite[] Attack { get; }
        public Sprite[] Hurt { get; }
        public Sprite[] Death { get; }
        public Sprite[] Mine { get; }
        public float IdleFps { get; }
        public float WalkFps { get; }
        public float AttackFps { get; }
        public float HurtFps { get; }
        public float DeathFps { get; }
        public float MineFps { get; }

        public float AttackDuration => Duration(Attack, AttackFps);
        public float HurtDuration => Duration(Hurt, HurtFps);
        public float DeathDuration => Duration(Death, DeathFps);

        private static float Duration(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0 || fps <= 0f) return 0f;
            return frames.Length / fps;
        }
    }

    internal static class UnitFlipbookTable
    {
        private readonly struct Sheet
        {
            public Sheet(string folder, string prefix)
            {
                Folder = folder;
                Prefix = prefix;
            }

            public string Folder { get; }
            public string Prefix { get; }
        }

        private static readonly Sprite[] s_empty = Array.Empty<Sprite>();

        public static UnitAnimationSet Resolve(WalkAnimation binding, Sprite fallbackIdle)
        {
            Sprite[] idle = binding.IdleFrames;
            Sprite[] walk = binding.WalkFrames;
            Sprite[] attack = binding.AttackFrames;
            Sprite[] hurt = binding.HurtFrames;
            Sprite[] death = binding.DeathFrames;
            Sprite[] mine = binding.MineFrames;

            if (TryGetSheet(binding.Key, out Sheet sheet))
            {
                if (idle == null || idle.Length == 0) idle = UnitFlipbookClipCache.Load(sheet, "Idle");
                if (walk == null || walk.Length == 0) walk = UnitFlipbookClipCache.Load(sheet, "Walk");
                if (attack == null || attack.Length == 0) attack = UnitFlipbookClipCache.Load(sheet, "Attack01");
                if (hurt == null || hurt.Length == 0) hurt = UnitFlipbookClipCache.Load(sheet, "Hurt");
                if (death == null || death.Length == 0) death = UnitFlipbookClipCache.Load(sheet, "Death");
            }

            if ((idle == null || idle.Length == 0) && fallbackIdle != null) idle = new[] { fallbackIdle };
            return new UnitAnimationSet(idle ?? s_empty, walk ?? s_empty, attack ?? s_empty, hurt ?? s_empty, death ?? s_empty, mine ?? s_empty, 12f, 12f, 12f, 16f, 10f, 12f);
        }

        private static bool TryGetSheet(UnitAnimationKey key, out Sheet sheet)
        {
            switch (key)
            {
                case UnitAnimationKey.Character:
                case UnitAnimationKey.DemonKing:
                    sheet = new Sheet("Demon_D", "Demon_D");
                    return true;
                case UnitAnimationKey.Imp:
                    sheet = new Sheet("Demon_A", "Demon_A");
                    return true;
                case UnitAnimationKey.BoneDemon:
                    sheet = new Sheet("Demon_C", "Demon_C");
                    return true;
                case UnitAnimationKey.Skeleton:
                    sheet = new Sheet("Skeleton", "Skeleton");
                    return true;
                case UnitAnimationKey.Worker:
                    sheet = new Sheet("Demon_B", "Demon_B");
                    return true;
                case UnitAnimationKey.Peasant:
                    sheet = new Sheet("Wizard", "Wizard");
                    return true;
                case UnitAnimationKey.Soldier:
                    sheet = new Sheet("Soldier", "Soldier");
                    return true;
                case UnitAnimationKey.Swordsman:
                    sheet = new Sheet("Swordsman", "Swordsman");
                    return true;
                case UnitAnimationKey.Archer:
                    sheet = new Sheet("Archer", "Archer");
                    return true;
                case UnitAnimationKey.ArmoredAxeman:
                    sheet = new Sheet("Armored_Axeman", "Armored_Axeman");
                    return true;
                case UnitAnimationKey.Knight:
                    sheet = new Sheet("Knight", "Knight");
                    return true;
                case UnitAnimationKey.Lancer:
                    sheet = new Sheet("Lancer", "Lancer");
                    return true;
                case UnitAnimationKey.Priest:
                    sheet = new Sheet("Priest", "Priest");
                    return true;
                case UnitAnimationKey.Slime:
                    sheet = new Sheet("Slime", "Slime");
                    return true;
                case UnitAnimationKey.Werewolf:
                    sheet = new Sheet("Werewolf", "Werewolf");
                    return true;
                case UnitAnimationKey.Werebear:
                    sheet = new Sheet("Werebear", "Werebear");
                    return true;
                default:
                    sheet = default;
                    return false;
            }
        }

        private static class UnitFlipbookClipCache
        {
            private static readonly Dictionary<string, Sprite[]> s_loaded = new();

            public static Sprite[] Load(Sheet sheet, string clip)
            {
                string path = $"Assets/Game/Art/Characters/{sheet.Folder}/{sheet.Prefix}_{clip}.png";
                if (s_loaded.TryGetValue(path, out Sprite[] cached)) return cached;

                Sprite[] loaded = LoadSprites(path, $"{sheet.Prefix}_{clip}_");
                s_loaded[path] = loaded;
                return loaded;
            }

            private static Sprite[] LoadSprites(string path, string prefix)
            {
#if UNITY_EDITOR
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null || assets.Length == 0) return s_empty;
                var sprites = new List<Sprite>(assets.Length);
                for (int i = 0; i < assets.Length; i++)
                {
                    Sprite sprite = assets[i] as Sprite;
                    if (sprite == null || !sprite.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    sprites.Add(sprite);
                }

                sprites.Sort((a, b) => ExtractFrame(a.name).CompareTo(ExtractFrame(b.name)));
                return sprites.Count == 0 ? s_empty : sprites.ToArray();
#else
                return s_empty;
#endif
            }

            private static int ExtractFrame(string name)
            {
                int split = name.LastIndexOf('_');
                if (split < 0 || split + 1 >= name.Length) return 0;
                return int.TryParse(name.Substring(split + 1), out int frame) ? frame : 0;
            }
        }
    }
}
