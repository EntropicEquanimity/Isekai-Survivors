using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Isekai.Dev.Editor
{
    /// <summary>Scenes staged for <see cref="PlayModeProbe"/> screenshots (<c>scene|Isekai.Dev.Editor.ProbeSetups.Method</c>).</summary>
    public static class ProbeSetups
    {
        /// <summary>Zoomed all the way out: the map screen, with Buildings, demons and a crowd near the Character.</summary>
        public static void MapShowcase()
        {
            Showcase();
            BuildingsDebug.FinishBuildings();
            HordeDebug.SpawnNearCharacter(60);
            var zoom = Object.FindAnyObjectByType<ScrollZoom>();
            for (int i = 0; i < 10; i++) zoom.Zoom(-1);
        }

        /// <summary>The Character pressed against the top edge of the Main Base, zoomed in.</summary>
        public static void BehindBase()
        {
            Object.FindAnyObjectByType<ScrollZoom>().Zoom(3);
            Player.Character.transform.position = MainBase().transform.position + new Vector3(0.2f, 0.8f, 0f);
        }

        /// <summary>Close up: two Portals side by side, a ghost beside them, hurt enemies with health bars.</summary>
        public static void Showcase()
        {
            Object.FindAnyObjectByType<ScrollZoom>().Zoom(2);
            EconomyDebug.AddResources(500);
            var mode = Object.FindAnyObjectByType<BuildMode>();
            BuildingAsset portal = mode.Catalogue.First(a => a.DisplayName == "Portal");
            Int2 origin = MainBase().Origin;
            foreach (int dx in new[] { 3, 5 }) RegionBuildings.Current.Place(portal, new Int2(origin.x + dx, origin.y));
            mode.Pick(portal);

            CharacterControl character = Player.Character;
            GameObject prefab = Object.FindAnyObjectByType<HordeSpawner>().Prefab;
            for (int i = 0; i < 12; i++)
            {
                Vector3 at = character.transform.position + new Vector3(-3f + (i % 4) * 0.7f, 2.5f + (i / 4) * 0.45f, 0f);
                GameObject enemy = Object.Instantiate(prefab, at, Quaternion.identity);
                enemy.GetComponent<HordeMember>().SetRoaming(true);
                var d = enemy.GetComponent<Damageable>();
                d.TakeDamage(d.MaxHealth * (0.1f + 0.07f * i), DamageRules.TrueType);
            }
        }

        private static Building MainBase() => Building.All.First(b => b.IsMainBase && b.IsPlayers);
    }
}
