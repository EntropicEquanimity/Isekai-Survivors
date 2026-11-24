using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Shared runtime materials for side outlines (one per side). Your side's ignores the 2D lights, so Jack, his demons
    /// and his Buildings stay clearly visible when night darkens the Region.
    /// </summary>
    public static class SideOutlineMaterials
    {
        private const string ShaderName = "Isekai/Sprite Lit Side Outline";
        private static readonly Dictionary<Side, Material> s_runtime = new();
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int UnlitId = Shader.PropertyToID("_Unlit");

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

            material.SetFloat(UnlitId, side == Side.Yours ? 1f : 0f);
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

        // In Resources, so a built player has them (and with them the outline shader) too.
        private static Material LoadTemplate(Side side) => Resources.Load<Material>("Outlines/Outline_" + side);

        private static Material s_unlit;

        /// <summary>A plain sprite material the 2D lights don't touch (health bars, popups).</summary>
        public static Material Unlit => s_unlit != null ? s_unlit : s_unlit = Resources.Load<Material>("Outlines/Unlit");
    }
}
