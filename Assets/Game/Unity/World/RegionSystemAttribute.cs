using System;

namespace Isekai
{
    /// <summary>
    /// Puts a MonoBehaviour into every Region without a scene object: <see cref="RegionBootstrap"/> adds one of it to a
    /// "Region Systems" GameObject before anything else wakes (unless the scene already has one). A new system is one
    /// class in a new file: implement <see cref="IRegionSetup"/> / <see cref="IRegionPopulate"/> (with a
    /// <see cref="RegionOrder"/> slot) and mark it with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RegionSystemAttribute : Attribute
    {
    }
}
