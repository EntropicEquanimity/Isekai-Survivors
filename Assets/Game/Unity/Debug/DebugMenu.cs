using System;
using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Pooling;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Isekai
{
    /// <summary>
    /// The developer menu (F1), in the Editor and development builds only: resources, god mode, spawning and killing
    /// enemies, Waves, nests, instant construction and game speed. It creates itself on the first scene load
    /// and survives scene changes. Buttons that need a Region do nothing on the Planet screen.
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        private const Key ToggleKey = Key.F1;
        private static readonly float[] Speeds = { 0.25f, 1f, 2f, 4f, 8f };

        private bool _visible;
        private bool _godMode;
        private Vector2 _scroll;
        private GUIStyle _title;

        public static DebugMenu Instance { get; private set; }

        public bool Visible { get => _visible; set => _visible = value; }

        /// <summary>The Character can't be hurt while on.</summary>
        public bool GodMode { get => _godMode; set { _godMode = value; ApplyGodMode(); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (Instance != null) return;
            var go = new GameObject("Debug Menu");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DebugMenu>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[ToggleKey].wasPressedThisFrame) _visible = !_visible;
            if (_godMode) ApplyGodMode();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.BeginArea(new Rect(10f, 10f, 300f, Screen.height - 20f), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label($"Debug ({ToggleKey} to hide)", _title);

            Section("Resources");
            Row(("+100 each", () => AddResources(100)), ("+1000 each", () => AddResources(1000)));

            Section("Character");
            bool god = GUILayout.Toggle(_godMode, " God mode");
            if (god != _godMode) GodMode = god;
            Row(("Heal", HealCharacter), ("Finish buildings", FinishBuildings));

            Section("Enemies");
            Row(("Spawn 20 here", () => SpawnNearCharacter(20)), ("Spawn 200", () => SpawnNearCharacter(200)));
            Row(("Kill all", KillAllEnemies), ("Destroy all nests", DestroyNests));

            Section($"Speed ×{Time.timeScale:0.##}");
            GUILayout.BeginHorizontal();
            foreach (float s in Speeds)
                if (GUILayout.Button($"×{s:0.##}")) Time.timeScale = s;
            GUILayout.EndHorizontal();

            Section("Info");
            HordeDirector horde = HordeDirector.Current;
            if (horde != null) GUILayout.Label($"Enemies {horde.Count} ({horde.Attacking} attacking) · kills {horde.Kills}");
            var wilds = Object.FindAnyObjectByType<RegionWilds>();
            if (wilds != null) GUILayout.Label($"Wanderers roaming {wilds.Roaming} in {wilds.GroupCount} groups");
            GUILayout.Label($"Nests left {CountNests()} · {1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):0} fps");

            DrawRegistered();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // Entries systems added through DebugActions, grouped by section in the order they were added.
        private void DrawRegistered()
        {
            var sections = new List<string>();
            foreach (DebugActions.Entry e in DebugActions.All)
                if (!sections.Contains(e.Section)) sections.Add(e.Section);
            foreach (string section in sections)
            {
                Section(section);
                foreach (DebugActions.Entry e in DebugActions.All)
                {
                    if (e.Section != section) continue;
                    switch (e.Kind)
                    {
                        case DebugActions.Kind.Button:
                            if (GUILayout.Button(e.Label)) e.Run?.Invoke();
                            break;
                        case DebugActions.Kind.Toggle:
                            bool on = e.Get?.Invoke() ?? false;
                            bool now = GUILayout.Toggle(on, " " + e.Label);
                            if (now != on) e.Set?.Invoke(now);
                            break;
                        default:
                            GUILayout.Label(e.Text?.Invoke() ?? "");
                            break;
                    }
                }
            }
        }

        private void Section(string title)
        {
            GUILayout.Space(6f);
            GUILayout.Label(title, _title);
        }

        private static void Row(params (string label, Action action)[] buttons)
        {
            GUILayout.BeginHorizontal();
            foreach ((string label, Action action) in buttons)
                if (GUILayout.Button(label)) action();
            GUILayout.EndHorizontal();
        }

        public static void AddResources(int amount)
        {
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (pile == null) return;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType))) pile.Add(type, amount);
        }

        private void ApplyGodMode()
        {
            CharacterControl character = Player.Character;
            if (character != null && character.TryGetComponent(out Damageable d)) d.Invulnerable = _godMode;
        }

        private static void HealCharacter()
        {
            CharacterControl character = Player.Character;
            if (character != null && character.TryGetComponent(out Damageable d) && !d.IsDead) d.Heal(d.MaxHealth);
        }

        public static void FinishBuildings()
        {
            foreach (Building b in Object.FindObjectsByType<Building>(FindObjectsInactive.Exclude)) b.FinishConstruction();
        }

        public static void SpawnNearCharacter(int count)
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            CharacterControl character = Player.Character;
            if (spawner == null || spawner.Prefab == null || character == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(8f, 12f);
                Pools.Spawn(spawner.Prefab, character.transform.position + (Vector3)ring, Quaternion.identity);
            }
        }

        public static void KillAllEnemies()
        {
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude))
                if (m.IsAlive) m.Damageable.Kill();
        }

        public static void DestroyNests()
        {
            foreach (EnemyNest nest in Object.FindObjectsByType<EnemyNest>(FindObjectsInactive.Exclude))
                if (!nest.IsDestroyed) nest.GetComponent<Damageable>().Kill();
        }

        private static int CountNests()
        {
            int n = 0;
            foreach (EnemyNest nest in EnemyNest.All)
                if (!nest.IsDestroyed) n++;
            return n;
        }
    }
}
