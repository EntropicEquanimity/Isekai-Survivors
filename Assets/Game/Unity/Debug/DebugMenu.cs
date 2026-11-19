using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// The developer menu (F1), in the Editor and development builds only. It only draws what systems registered
    /// through <see cref="DebugActions"/> (resources, god mode, spawning, capturing the Region, speed, readouts): a new
    /// mechanic brings its own rows without editing the menu. It creates itself on the first scene load and survives
    /// scene changes.
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        private const Key ToggleKey = Key.F1;

        private bool _visible;
        private Vector2 _scroll;
        private GUIStyle _title;

        public static DebugMenu Instance { get; private set; }

        public bool Visible { get => _visible; set => _visible = value; }

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
            DrawRegistered();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // Entries systems added through DebugActions: the known sections first, then the rest in the order they appeared.
        private void DrawRegistered()
        {
            var sections = new List<string>();
            foreach (string known in DebugActions.SectionOrder)
                foreach (DebugActions.Entry e in DebugActions.All)
                    if (e.Section == known) { sections.Add(known); break; }
            foreach (DebugActions.Entry e in DebugActions.All)
                if (!sections.Contains(e.Section)) sections.Add(e.Section);
            foreach (string section in sections)
            {
                GUILayout.Space(6f);
                bool titled = false;
                string row = null;
                foreach (DebugActions.Entry e in DebugActions.All)
                {
                    if (e.Section != section) continue;
                    if (e.Kind == DebugActions.Kind.Info && string.IsNullOrEmpty(e.Text?.Invoke())) continue;
                    if (!titled) { titled = true; GUILayout.Label(section, _title); }
                    if (row != null && e.Row != row) { GUILayout.EndHorizontal(); row = null; }
                    if (row == null && e.Row != null) { GUILayout.BeginHorizontal(); row = e.Row; }
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
                            GUILayout.Label(e.Text.Invoke());
                            break;
                    }
                }
                if (row != null) GUILayout.EndHorizontal();
            }
        }
    }
}
