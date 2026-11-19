using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Isekai.Dev.Editor
{
    /// <summary>Scenes staged for <see cref="PlayModeProbe"/> screenshots (<c>scene|Isekai.Dev.Editor.ProbeSetups.Method</c>). Game types by name.</summary>
    public static class ProbeSetups
    {
        /// <summary>Zoomed all the way out.</summary>
        public static void FarZoom() => Call("Isekai.ScrollZoom", "Zoom", -10);

        /// <summary>Halfway out: the first far level.</summary>
        public static void MidZoom()
        {
            Call("Isekai.ScrollZoom", "Zoom", -10);
            Call("Isekai.ScrollZoom", "Zoom", 1);
        }

        /// <summary>Close up: two Barracks side by side, a ghost beside them, hurt enemies with health bars.</summary>
        public static void Showcase()
        {
            Call("Isekai.ScrollZoom", "Zoom", 2);
            Type debug = GameType("Isekai.DebugMenu");
            debug.GetMethod("AddResources").Invoke(null, new object[] { 500 });
            Component buildings = Find("Isekai.RegionBuildings");
            Component mode = Find("Isekai.BuildMode");
            var catalogue = (System.Collections.IEnumerable)mode.GetType().GetProperty("Catalogue").GetValue(mode);
            object barracks = catalogue.Cast<object>().First(a => (string)a.GetType().GetProperty("DisplayName").GetValue(a) == "Barracks");
            Component home = Object.FindObjectsByType(GameType("Isekai.Building"), FindObjectsInactive.Exclude).Cast<Component>()
                .First(b => (bool)b.GetType().GetProperty("IsMainBase").GetValue(b));
            object origin = home.GetType().GetProperty("Origin").GetValue(home);
            int ox = (int)origin.GetType().GetField("x").GetValue(origin), oy = (int)origin.GetType().GetField("y").GetValue(origin);
            Type int2 = origin.GetType();
            foreach (int dx in new[] { 3, 5 })
                buildings.GetType().GetMethod("Place").Invoke(buildings, new[] { barracks, Activator.CreateInstance(int2, ox + dx, oy) });
            mode.GetType().GetMethod("Pick").Invoke(mode, new[] { barracks });

            Component character = Find("Isekai.CharacterControl");
            Component spawner = Find("Isekai.HordeSpawner");
            var prefab = (GameObject)spawner.GetType().GetProperty("Prefab").GetValue(spawner);
            for (int i = 0; i < 12; i++)
            {
                Vector3 at = character.transform.position + new Vector3(-3f + (i % 4) * 0.7f, 2.5f + (i / 4) * 0.45f, 0f);
                GameObject enemy = Object.Instantiate(prefab, at, Quaternion.identity);
                Component member = enemy.GetComponent(GameType("Isekai.HordeMember"));
                member.GetType().GetMethod("SetRoaming").Invoke(member, new object[] { true });
                Component d = enemy.GetComponent(GameType("Blondie.Combat.Damageable"));
                float max = (float)d.GetType().GetProperty("MaxHealth").GetValue(d);
                d.GetType().GetMethod("TakeDamage", new[] { typeof(float), typeof(string), typeof(GameObject) })
                    .Invoke(d, new object[] { max * (0.1f + 0.07f * i), "True", null });
            }
        }

        private static void Call(string type, string method, params object[] args)
        {
            Component c = Find(type);
            c.GetType().GetMethod(method).Invoke(c, args);
        }

        private static Component Find(string type) => (Component)Object.FindAnyObjectByType(GameType(type));

        private static Type GameType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName)).FirstOrDefault(t => t != null)
            ?? throw new InvalidOperationException("No type " + fullName);
    }
}
