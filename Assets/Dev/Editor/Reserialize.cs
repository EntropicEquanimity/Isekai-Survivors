using System;
using UnityEditor;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Dev-only: rewrites assets in the current serialization format, e.g. after a Unity upgrade warns
    /// "Open and re-save the file to upgrade". <c>exec Isekai.Dev.Editor.Reserialize.Paths a.dll;b.prefab</c>.
    /// </summary>
    public static class Reserialize
    {
        public static string Paths(string semicolonSeparated)
        {
            string[] paths = Array.ConvertAll(semicolonSeparated.Split(';', StringSplitOptions.RemoveEmptyEntries), p => p.Trim());
            AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            return $"reserialized {paths.Length}";
        }
    }
}
