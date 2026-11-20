using System;
using System.IO;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>
    /// The team-outline shader's pixel rule: only the 1 px outline changes colour, never detail inside a sprite.
    /// The shader runs on the GPU, so the rule is mirrored here on a small alpha grid, and the shader's source is
    /// checked to still be that rule in every pass that recolours.
    /// </summary>
    public class SideOutlineRuleTests
    {
        private const string ShaderPath = "Assets/Game/Shaders/SpriteLitSideOutline.shader";

        // Mirror of IsOutlinePixel in the shader: opaque, with at least one transparent 4-neighbour.
        private static bool IsOutline(bool[,] opaque, int x, int y)
        {
            if (!opaque[x, y]) return false;
            int w = opaque.GetLength(0), h = opaque.GetLength(1);
            bool Clear(int nx, int ny) => nx < 0 || ny < 0 || nx >= w || ny >= h || !opaque[nx, ny];
            return Clear(x + 1, y) || Clear(x - 1, y) || Clear(x, y + 1) || Clear(x, y - 1);
        }

        private static bool[,] Block(int w, int h, int pad)
        {
            var a = new bool[w, h];
            for (int x = pad; x < w - pad; x++)
            for (int y = pad; y < h - pad; y++) a[x, y] = true;
            return a;
        }

        [Test]
        public void Opaque_pixels_beside_transparent_ones_are_the_outline()
        {
            bool[,] a = Block(7, 7, 1);   // a 5x5 body inside a transparent frame
            Assert.IsTrue(IsOutline(a, 1, 1), "corner");
            Assert.IsTrue(IsOutline(a, 3, 1), "edge");
            Assert.IsTrue(IsOutline(a, 5, 3), "edge");
        }

        [Test]
        public void Pixels_inside_the_sprite_are_never_the_outline_whatever_their_colour()
        {
            bool[,] a = Block(7, 7, 1);
            for (int x = 2; x <= 4; x++)
            for (int y = 2; y <= 4; y++) Assert.IsFalse(IsOutline(a, x, y), $"inside ({x},{y})");
        }

        [Test]
        public void Transparent_pixels_are_not_the_outline_and_a_diagonal_gap_does_not_count()
        {
            bool[,] a = Block(7, 7, 1);
            Assert.IsFalse(IsOutline(a, 0, 0));
            a[3, 3] = false;   // a hole: its four neighbours are now outline, the diagonals stay inside
            Assert.IsTrue(IsOutline(a, 3, 2));
            Assert.IsFalse(IsOutline(a, 2, 2), "only a diagonal neighbour is clear");
        }

        [Test]
        public void The_shader_recolours_by_the_neighbour_rule_alone_in_every_pass_that_recolours()
        {
            string source = File.ReadAllText(ShaderPath);
            int passes = 0, at = 0;
            while ((at = source.IndexOf("bool IsOutlinePixel", at, StringComparison.Ordinal)) >= 0)
            {
                int end = source.IndexOf("Varyings ", at, StringComparison.Ordinal);
                string body = source.Substring(at, end - at);
                StringAssert.Contains("TexelOf(_MainTex)", body, "samples neighbours by the bound texture's own texel size, not a uniform the SRP batcher leaves stale");
                StringAssert.DoesNotContain("_MainTex_TexelSize", body);
                StringAssert.Contains("return nearTransparent;", body, "the rule is only the neighbour test");
                StringAssert.DoesNotContain("luminance", body, "no colour test");
                StringAssert.DoesNotContain("_OutlineSourceColor", body, "no colour test");
                passes++;
                at = end;
            }
            Assert.AreEqual(2, passes, "the lit and the unlit pass both recolour");
        }
    }
}
