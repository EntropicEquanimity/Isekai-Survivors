using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Starts a Region in a fixed order, whatever order Unity wakes the scene in:
    /// <list type="number">
    /// <item>Awake: sets the sides' relations, builds the <see cref="RegionContext"/> for the Run's current Region (from
    /// the pending <see cref="RegionRecipe"/> if a test set one) and adds every <see cref="RegionSystemAttribute"/> system.</item>
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

        private static Type[] s_systemTypes;

        private void Awake()
        {
            Current = this;
            RegionEvents.Reset();
            RegionEntities.Reset();
            MapPainters.Reset();
            RegionSides.Apply();
            RegionRecipe recipe = RegionRecipes.Take();
            if (recipe != null && RegionGrid.Current != null) RegionGrid.Current.Resize(recipe.Width, recipe.Height);
            Context = new RegionContext(RunSession.EnsureInRegion(), RunSession.Run, RunSession.Seed, recipe);
            AddSystems();
        }

        // Every [RegionSystem] class the scene doesn't already have, on one object of their own.
        private void AddSystems()
        {
            GameObject host = null;
            foreach (Type type in SystemTypes())
            {
                if (FindAnyObjectByType(type, FindObjectsInactive.Include) != null) continue;
                host ??= new GameObject("Region Systems");
                host.AddComponent(type);
            }
        }

        private static Type[] SystemTypes()
        {
            if (s_systemTypes != null) return s_systemTypes;
            var found = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.StartsWith("Isekai", StringComparison.Ordinal)) continue;
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                foreach (Type t in types)
                    if (!t.IsAbstract && typeof(MonoBehaviour).IsAssignableFrom(t) && t.IsDefined(typeof(RegionSystemAttribute), false)) found.Add(t);
            }
            found.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            return s_systemTypes = found.ToArray();
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
