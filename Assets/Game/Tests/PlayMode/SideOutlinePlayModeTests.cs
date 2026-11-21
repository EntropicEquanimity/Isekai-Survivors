using System.Collections;
using System.Linq;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    public class SideOutlinePlayModeTests
    {
        private const string KeyPreset = "Isekai.View.Outline.Preset";
        private const string KeyOverridePrefix = "Isekai.View.Outline.Override.";

        [UnitySetUp]
        public IEnumerator Load()
        {
            ClearOutlineSettings();
            SideOutlineSettingsStore.ResetForTests();
            yield return RegionTestScene.Load(new RegionRecipe(1212, 64, 48));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ClearOutlineSettings();
            SideOutlineSettingsStore.ResetForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Character_and_mortal_get_their_sides_material_and_rocks_nodes_keep_dark_outline()
        {
            var character = Player.Character;
            SpriteRenderer characterSprite = character.GetComponentInChildren<SpriteRenderer>(true);
            Assert.AreSame(SideOutlineMaterials.MaterialFor(Side.Yours), characterSprite.sharedMaterial);

            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            Vector3 where = character.transform.position + new Vector3(5f, 0f, 0f);
            GameObject mortal = Pools.Spawn(spawner.Prefab, where, Quaternion.identity);
            yield return null;

            SpriteRenderer mortalSprite = mortal.GetComponentInChildren<SpriteRenderer>(true);
            Assert.AreSame(SideOutlineMaterials.MaterialFor(Side.Mortals), mortalSprite.sharedMaterial);

            SpriteRenderer rock = Object.FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude)
                .First(o => o.name.StartsWith("Rock"))
                .GetComponent<SpriteRenderer>();
            Assert.IsFalse(IsSideMaterial(rock.sharedMaterial), "rocks keep their authored dark outline material");

            SpriteRenderer node = ResourceNode.All.First(n => n.Type == ResourceType.Wood).GetComponent<SpriteRenderer>();
            Assert.IsFalse(IsSideMaterial(node.sharedMaterial), "resource nodes keep their authored dark outline material");
        }

        [UnityTest]
        public IEnumerator Your_side_ignores_night_lighting_and_the_others_do_not()
        {
            yield return null;
            Assert.AreEqual(1f, SideOutlineMaterials.MaterialFor(Side.Yours).GetFloat("_Unlit"), "your units stay bright at night");
            Assert.AreEqual(0f, SideOutlineMaterials.MaterialFor(Side.Mortals).GetFloat("_Unlit"));
            Assert.AreEqual(0f, SideOutlineMaterials.MaterialFor(Side.Neutral).GetFloat("_Unlit"));
        }

        [UnityTest]
        public IEnumerator Changing_preset_updates_shared_material_colours_immediately()
        {
            Material mortal = SideOutlineMaterials.MaterialFor(Side.Mortals);
            Color32 before = mortal.GetColor("_OutlineColor");

            SideOutlineSettingsStore.SetPreset(OutlinePreset.HighContrast);
            yield return null;

            OutlineColor expectedRule = SideOutlinePalettes.ColorOf(OutlinePreset.HighContrast, Side.Mortals);
            Color32 expected = new(expectedRule.R, expectedRule.G, expectedRule.B, expectedRule.A);
            Color32 after = mortal.GetColor("_OutlineColor");
            Assert.AreEqual(expected, after);
            Assert.AreNotEqual(before, after);
        }

        private static bool IsSideMaterial(Material material)
        {
            if (material == null) return false;
            foreach (Side side in SideRelations.All)
                if (ReferenceEquals(material, SideOutlineMaterials.MaterialFor(side))) return true;
            return false;
        }

        private static void ClearOutlineSettings()
        {
            PlayerPrefs.DeleteKey(KeyPreset);
            foreach (Side side in SideRelations.All)
                PlayerPrefs.DeleteKey(KeyOverridePrefix + side);
            PlayerPrefs.Save();
        }
    }
}
