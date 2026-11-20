using UnityEngine;

namespace Isekai
{
    /// <summary>Puts the Run's chosen Anchor where the scene's own Main Base stands, at Region start.</summary>
    public static class AnchorPlacement
    {
        /// <summary>
        /// The Building to use as the Region's Anchor: <paramref name="scene"/> itself when the Run picked none or
        /// already picked its kind, else a new one of the picked kind in its place (the scene's goes away).
        /// </summary>
        public static Building ChosenFor(Building scene)
        {
            AnchorKind? picked = RunSession.Anchor;
            if (picked == null || AnchorCatalog.KindOf(scene) == picked) return scene;
            BuildingAsset asset = AnchorCatalog.Asset(picked.Value);
            if (asset == null || asset.Prefab == null) return scene;
            GameObject go = Object.Instantiate(asset.Prefab, scene.transform.position, Quaternion.identity);
            scene.gameObject.SetActive(false);
            Object.Destroy(scene.gameObject);
            return go.GetComponent<Building>();
        }
    }
}
