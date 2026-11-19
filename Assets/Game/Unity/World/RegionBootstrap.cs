using System;
using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Starts a Region in a fixed order, whatever order Unity wakes the scene in:
    /// <list type="number">
    /// <item>Awake: builds the <see cref="RegionContext"/> for the Run's current Region.</item>
    /// <item>Start (before every other Start): every <see cref="IRegionSetup"/> in the scene, by order.</item>
    /// <item>Once <see cref="RegionGrid"/> has baked the walls: every <see cref="IRegionPopulate"/>, by order.</item>
    /// </list>
    /// A procedural Region generator plugs in as setup and populate steps.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class RegionBootstrap : MonoBehaviour
    {
        public static RegionBootstrap Current { get; private set; }

        public RegionContext Context { get; private set; }
        public bool IsSetUp { get; private set; }
        public bool IsPopulated { get; private set; }

        /// <summary>The Region is filled and playing.</summary>
        public event Action Populated;

        private void Awake()
        {
            Current = this;
            RegionEvents.Reset();
            Context = new RegionContext(RunSession.EnsureInRegion(), RunSession.Run, RunSession.Seed);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (RegionGrid.Current != null) RegionGrid.Current.Baked -= Populate;
        }

        private void Start()
        {
            foreach (IRegionSetup step in Find<IRegionSetup>().OrderBy(s => s.SetupOrder)) step.Setup(Context);
            IsSetUp = true;
            RegionGrid grid = RegionGrid.Current;
            if (grid == null || grid.IsBaked) Populate();
            else grid.Baked += Populate;
        }

        private void Populate()
        {
            if (IsPopulated) return;
            if (RegionGrid.Current != null) RegionGrid.Current.Baked -= Populate;
            foreach (IRegionPopulate step in Find<IRegionPopulate>().OrderBy(s => s.PopulateOrder)) step.Populate(Context);
            IsPopulated = true;
            BLog.Info("Region", $"{Context.Region?.Name} ready (seed {Context.Seed}).");
            Populated?.Invoke();
        }

        private static IEnumerable<T> Find<T>() where T : class =>
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).OfType<T>().ToArray();
    }
}
