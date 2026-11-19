using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>The horde's debug rows: spawn enemies round the Character, kill them all, and the enemy counts.</summary>
    [RegionSystem]
    public sealed class HordeDebug : MonoBehaviour
    {
        private RegionWilds _wilds;

        private void OnEnable()
        {
            DebugActions.Button(this, "Enemies", "Spawn 20 here", () => SpawnNearCharacter(20), "spawn");
            DebugActions.Button(this, "Enemies", "Spawn 200", () => SpawnNearCharacter(200), "spawn");
            DebugActions.Button(this, "Enemies", "Kill all", KillAllEnemies);
            DebugActions.Info(this, "Info", () =>
            {
                HordeDirector horde = HordeDirector.Current;
                return horde != null ? $"Enemies {horde.Count} ({horde.Attacking} attacking) · kills {horde.Kills}" : null;
            });
            DebugActions.Info(this, "Info", () =>
            {
                if (_wilds == null) _wilds = FindAnyObjectByType<RegionWilds>();
                return _wilds != null ? $"Wanderers roaming {_wilds.Roaming} in {_wilds.GroupCount} groups" : null;
            });
        }

        private void OnDisable() => DebugActions.Remove(this);

        public static void SpawnNearCharacter(int count)
        {
            var spawner = FindAnyObjectByType<HordeSpawner>();
            CharacterControl character = Player.Character;
            if (spawner == null || spawner.Prefab == null || character == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(8f, 12f);
                Pools.Spawn(spawner.Prefab, character.transform.position + (Vector3)ring, Quaternion.identity);
            }
        }

        private static void KillAllEnemies()
        {
            foreach (HordeMember m in FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude))
                if (m.IsAlive) m.Damageable.Kill();
        }
    }
}
