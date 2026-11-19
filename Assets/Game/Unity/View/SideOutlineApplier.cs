using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>Assigns each living unit/building the shared material of its current side.</summary>
    [RegionSystem]
    public sealed class SideOutlineApplier : MonoBehaviour
    {
        private static readonly List<SpriteRenderer> s_renderers = new(8);
        private static SideOutlineApplier s_current;

        private void OnEnable()
        {
            s_current = this;
            SideOutlineMaterials.Apply(SideOutlineSettingsStore.Settings);
            RegionEntities.Entered += OnEntered;
            ApplyToAllLiving();
        }

        private void OnDisable()
        {
            RegionEntities.Entered -= OnEntered;
            if (ReferenceEquals(s_current, this)) s_current = null;
        }

        /// <summary>Re-applies one entity's side material (for side changes, e.g. rogue turns).</summary>
        public static void Reapply(RegionEntity entity) => s_current?.ApplyTo(entity);

        private void OnEntered(RegionEntity entity) => ApplyTo(entity);

        private void ApplyToAllLiving()
        {
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++) ApplyTo(living[i]);
        }

        private void ApplyTo(RegionEntity entity)
        {
            if (entity == null || entity.GameObject == null) return;
            if (!ShouldOutline(entity.Kind)) return;

            Material material = SideOutlineMaterials.MaterialFor(entity.Side);
            if (material == null) return;

            s_renderers.Clear();
            entity.GameObject.GetComponentsInChildren(true, s_renderers);
            for (int i = 0; i < s_renderers.Count; i++)
            {
                SpriteRenderer renderer = s_renderers[i];
                if (renderer == null) continue;
                renderer.sharedMaterial = material;
            }
            s_renderers.Clear();
        }

        private static bool ShouldOutline(UnitKind kind) =>
            kind is UnitKind.Character or UnitKind.Demon or UnitKind.Mortal or UnitKind.Wildlife or UnitKind.Building;
    }
}
