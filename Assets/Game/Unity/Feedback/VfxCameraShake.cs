using UnityEngine;
using UnityEngine.Rendering;

namespace Isekai
{
    /// <summary>
    /// A small camera shake for Jack's own hits. After everything that moves the camera has run (and just before the
    /// pixel camera snaps it), it shifts the camera by a random whole number of game pixels; once the frame is drawn it
    /// shifts it back, so no follow code ever sees the shaken position.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public sealed class VfxCameraShake : MonoBehaviour
    {
        /// <summary>Strength (world units) one hit adds, and the most it builds up to: 1 and 3 game pixels.</summary>
        public const float KickStrength = 1f / 16f;
        public const float MaxStrength = 3f / 16f;
        private const float DecayPerSecond = 0.9f;
        private const float PixelSize = 1f / 16f;

        private Vector3 _applied;
        private float _strength;

        public static VfxCameraShake Current { get; private set; }

        /// <summary>How hard the camera is shaking now (world units, 0 = still).</summary>
        public float Strength => _strength;

        /// <summary>How far the camera is moved right now, between the late update and the end of the frame.</summary>
        public Vector3 Offset => _applied;

        /// <summary>Shakes the camera a little more.</summary>
        public static void Kick()
        {
            if (Current != null) Current._strength = Mathf.Min(MaxStrength, Current._strength + KickStrength);
        }

        private void OnEnable()
        {
            Current = this;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            Release();
            if (Current == this) Current = null;
        }

        private void LateUpdate()
        {
            Release();
            if (_strength <= 0f) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            float step = Mathf.Max(PixelSize, Mathf.Round(_strength / PixelSize) * PixelSize);
            Vector2 dir = Random.insideUnitCircle;
            _applied = new Vector3(Mathf.Round(dir.x * step / PixelSize) * PixelSize, Mathf.Round(dir.y * step / PixelSize) * PixelSize, 0f);
            cam.transform.position += _applied;
            _strength = Mathf.Max(0f, _strength - DecayPerSecond * Time.unscaledDeltaTime);
        }

        private void OnEndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == Camera.main) Release();
        }

        private void Release()
        {
            if (_applied == Vector3.zero) return;
            Camera cam = Camera.main;
            if (cam != null) cam.transform.position -= _applied;
            _applied = Vector3.zero;
        }
    }
}
