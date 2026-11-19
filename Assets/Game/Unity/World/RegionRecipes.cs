using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// The recipe the next Region is made from. A test sets <see cref="Pending"/> before loading the Region scene
    /// (<c>RegionTestScene.Load(recipe)</c>); <see cref="RegionBootstrap"/> takes it when the Region wakes, so it
    /// applies to that one Region only.
    /// </summary>
    public static class RegionRecipes
    {
        static RegionRecipes() => StaticReset.Register(() => Pending = null);

        public static RegionRecipe Pending { get; set; }

        /// <summary>Hands out the pending recipe and clears it.</summary>
        public static RegionRecipe Take()
        {
            RegionRecipe recipe = Pending;
            Pending = null;
            return recipe;
        }
    }
}
