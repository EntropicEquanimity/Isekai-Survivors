using Blondie.Controllers;
using Blondie.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// Jack without a body: a small soul the player steers with the same move input as the Character. It passes over walls
    /// and obstacles (no collider, no flow field) and ordinary units can't hit it (no Damageable, not a horde target,
    /// not in the Region's list of units). Its art is the Soul_Swim sheet (4 frames of 10x7 px), head to the right.
    /// </summary>
    public sealed class Wisp : MonoBehaviour
    {
        private const string Art = "Soul_Swim";
        private const int FrameWidth = 10, FrameHeight = 7;
        private const int Frames = 4;
        private const float FramesPerSecond = 10f;

        private static Sprite[] s_frames;

        private SpriteRenderer _renderer;
        private InputAction _fallback;
        private float _clock;

        /// <summary>Units per second.</summary>
        public float Speed { get; set; } = 6f;

        /// <summary>Steers instead of the player's input while set (tests and scripted moves).</summary>
        public Vector2? Steer { get; set; }

        public static Wisp Spawn(Vector3 position, float speed)
        {
            var go = new GameObject("Wisp");
            go.transform.position = position;
            var wisp = go.AddComponent<Wisp>();
            if (speed > 0f) wisp.Speed = speed;
            return wisp;
        }

        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 20;
            Material yours = SideOutlineMaterials.MaterialFor(Side.Yours);   // outlined, and bright at night like the rest of your side
            if (yours != null) _renderer.sharedMaterial = yours;
            Sprite[] frames = LoadFrames();
            if (frames.Length > 0) _renderer.sprite = frames[0];
        }

        private void OnDestroy() => _fallback?.Dispose();

        private void Update()
        {
            Vector2 move = Steer ?? ReadMove();
            if (move.sqrMagnitude > 1f) move.Normalize();
            if (move.sqrMagnitude > 1e-4f)
            {
                transform.position += (Vector3)(move * (Speed * Time.deltaTime));
                float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            Animate();
        }

        private void Animate()
        {
            Sprite[] frames = LoadFrames();
            if (frames.Length == 0) return;
            _clock += Time.deltaTime * FramesPerSecond;
            _renderer.sprite = frames[(int)_clock % frames.Length];
        }

        // The Character's own move input: Core's ActionInput when the game defines Move, else the default keys.
        private Vector2 ReadMove()
        {
            if (PlayerBrain.BlockInput) return Vector2.zero;
            if (ActionInput.HasAction(PlayerBrain.MoveId)) return ActionInput.ReadVector(PlayerBrain.MoveId).ToUnity();
            if (_fallback == null)
            {
                _fallback = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
                _fallback.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                _fallback.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                _fallback.AddBinding("<Gamepad>/leftStick");
                _fallback.Enable();
            }
            return _fallback.ReadValue<Vector2>();
        }

        // The four frames cut from the sheet; the pivot is the round head (x 7, y 3 of each frame).
        private static Sprite[] LoadFrames()
        {
            if (s_frames != null && s_frames.Length > 0 && s_frames[0] != null) return s_frames;
            var sheet = Resources.Load<Texture2D>(Art);
            if (sheet == null) return s_frames = System.Array.Empty<Sprite>();
            var frames = new Sprite[Frames];
            for (int i = 0; i < Frames; i++)
                frames[i] = Sprite.Create(sheet, new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight), new Vector2(7f / FrameWidth, 3.5f / FrameHeight), 16f);
            return s_frames = frames;
        }
    }
}
