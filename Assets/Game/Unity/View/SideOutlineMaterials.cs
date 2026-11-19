using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>Shared runtime materials for side outlines (one per side).</summary>
    public static class SideOutlineMaterials
    {
        private const string ShaderName = "Isekai/Sprite Lit Side Outline";
        private static readonly Dictionary<Side, Material> s_runtime = new();
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        public static Material MaterialFor(Side side)
        {
            if (s_runtime.TryGetValue(side, out Material material) && material != null) return material;

            Material template = LoadTemplate(side);
            if (template != null)
            {
                material = Object.Instantiate(template);
                material.name = template.name + " (Runtime)";
                material.hideFlags = HideFlags.DontSave;
            }
            else
            {
                Shader shader = Shader.Find(ShaderName);
                if (shader == null) return null;
                material = new Material(shader)
                {
                    name = $"Outline_{side} (Runtime)",
                    hideFlags = HideFlags.DontSave,
                };
            }

            s_runtime[side] = material;
            return material;
        }

        public static void Apply(SideOutlineSettings settings)
        {
            if (settings == null) return;
            foreach (Side side in SideRelations.All)
            {
                Material material = MaterialFor(side);
                if (material == null) continue;
                OutlineColor c = settings.ColorOf(side);
                material.SetColor(OutlineColorId, new Color32(c.R, c.G, c.B, c.A));
            }
        }

        private static Material LoadTemplate(Side side)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(PathFor(side));
#else
            return null;
#endif
        }

        private static string PathFor(Side side) => side switch
        {
            Side.Yours => "Assets/Game/Materials/Outline_Yours.mat",
            Side.Mortals => "Assets/Game/Materials/Outline_Mortals.mat",
            Side.Neutral => "Assets/Game/Materials/Outline_Neutral.mat",
            Side.Hostile => "Assets/Game/Materials/Outline_Hostile.mat",
            Side.Rogue => "Assets/Game/Materials/Outline_Rogue.mat",
            _ => "Assets/Game/Materials/Outline_Neutral.mat",
        };
    }
}
