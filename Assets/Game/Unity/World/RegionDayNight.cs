using System;
using System.Reflection;
using Blondie.Combat;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Stats;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Region day-night driver: advances the cycle, scales vision, tells mortals when it is night, gives your demons a
    /// small night damage bonus, and darkens the world with a global 2D light.
    /// </summary>
    [RegionSystem]
    public sealed class RegionDayNight : MonoBehaviour
    {
        private static readonly string DamageBonus = nameof(StatId.DamageBonus);
        private static readonly object NightBonusSource = new();

        [SerializeField, Min(1f)] private float _cycleSeconds = DayNightCycle.DefaultCycleSeconds;
        [SerializeField, Min(0f)] private float _nightSeconds = DayNightCycle.DefaultNightSeconds;
        [SerializeField, Min(0f)] private float _transitionSeconds = DayNightCycle.DefaultTransitionSeconds;
        [SerializeField, Min(0f)] private float _demonNightDamageBonus = 0.1f;
        [SerializeField, Range(0.1f, 1f)] private float _nightLightIntensityScale = 0.55f;
        [SerializeField] private Color _nightLightColor = new(0.62f, 0.7f, 0.9f, 1f);
        [SerializeField] private Color _nightOverlayColor = new(0.05f, 0.08f, 0.14f, 0.36f);

        private DayNightCycle _cycle;
        private float _cycleSpeed = 1f;
        private bool _nightBonusOn;
        private Component _globalLight;
        private PropertyInfo _lightTypeProperty;
        private PropertyInfo _intensityProperty;
        private PropertyInfo _colorProperty;
        private object _globalLightType;
        private float _baseIntensity = 1f;
        private Color _baseColor = Color.white;
        private bool _createdLight;
        private SpriteRenderer _overlay;
        private static Sprite s_overlaySprite;

        public static RegionDayNight Current { get; private set; }

        public bool IsNight => _cycle != null && _cycle.IsNight;
        public float TimeInCycle => _cycle != null ? _cycle.TimeInCycle : 0f;
        public float Darkness01 => _cycle != null ? _cycle.Darkness01 : 0f;
        public float VisionMultiplier => _cycle != null ? _cycle.VisionMultiplier : 1f;
        public float SecondsUntilChange => _cycle != null ? _cycle.SecondsUntilChange : 0f;
        public float CycleSeconds => _cycle != null ? _cycle.CycleSeconds : _cycleSeconds;
        public float NightSeconds => _cycle != null ? _cycle.NightSeconds : _nightSeconds;
        public float NightDamageBonus => _demonNightDamageBonus;

        public float CycleSpeedMultiplier
        {
            get => _cycleSpeed;
            set => _cycleSpeed = Mathf.Max(0f, value);
        }

        private void OnEnable()
        {
            Current = this;
            _cycle = new DayNightCycle(_cycleSeconds, _nightSeconds, _transitionSeconds);
            _cycle.JumpToDay();
            RegionEntities.Entered += OnEntered;
            RegionEntities.Died += OnDied;
            BindDarkening();
            ApplyState(forceLight: true);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            RegionEntities.Entered -= OnEntered;
            RegionEntities.Died -= OnDied;
            _nightBonusOn = false;
            RemoveNightBonusFromAll();
            RegionVision.DayNightMultiplier = 1f;
            if (RegionMortals.Current != null) RegionMortals.Current.Night = false;
            RestoreLight();
        }

        private void Update()
        {
            if (_cycle == null) return;
            if (!GameSetup.IsPaused)
            {
                float delta = Time.deltaTime * _cycleSpeed;
                if (delta > 0f) _cycle.Advance(delta);
            }
            ApplyState(forceLight: false);
        }

        public void ConfigureForTests(float cycleSeconds, float nightSeconds, float transitionSeconds = 0f)
        {
            _cycle = new DayNightCycle(cycleSeconds, nightSeconds, transitionSeconds, _cycle != null ? _cycle.ElapsedSeconds : 0f);
            ApplyState(forceLight: true);
        }

        public void SetTimeInCycle(float seconds)
        {
            if (_cycle == null) return;
            _cycle.SetTimeInCycle(seconds);
            ApplyState(forceLight: true);
        }

        public void SkipToNight()
        {
            if (_cycle == null) return;
            _cycle.JumpToNight();
            ApplyState(forceLight: true);
        }

        public void SkipToDay()
        {
            if (_cycle == null) return;
            _cycle.JumpToDay();
            ApplyState(forceLight: true);
        }

        private void ApplyState(bool forceLight)
        {
            bool night = _cycle.IsNight;
            RegionVision.DayNightMultiplier = _cycle.VisionMultiplier;

            RegionMortals mortals = RegionMortals.Current;
            if (mortals != null) mortals.Night = night;

            if (night != _nightBonusOn)
            {
                _nightBonusOn = night;
                if (night) ApplyNightBonusToAll();
                else RemoveNightBonusFromAll();
            }

            ApplyLight(_cycle.Darkness01, forceLight);
        }

        private static bool IsYoursDemon(RegionEntity unit) =>
            unit.Side == Side.Yours && (unit.Kind == UnitKind.Demon || unit.Kind == UnitKind.Character);

        private void ApplyNightBonusToAll()
        {
            foreach (RegionEntity unit in RegionEntities.Living)
                if (IsYoursDemon(unit)) ApplyNightBonus(unit);
        }

        private void RemoveNightBonusFromAll()
        {
            foreach (RegionEntity unit in RegionEntities.Living)
                if (IsYoursDemon(unit)) RemoveNightBonus(unit);
        }

        private void OnEntered(RegionEntity unit)
        {
            if (_nightBonusOn && IsYoursDemon(unit)) ApplyNightBonus(unit);
        }

        private void OnDied(UnitDeath death)
        {
            if (IsYoursDemon(death.Unit)) RemoveNightBonus(death.Unit);
        }

        private void ApplyNightBonus(RegionEntity unit)
        {
            IStatModifierTarget stats = unit.Damageable.Stats;
            if (stats == null) return;
            stats.RemoveStatModifiers(NightBonusSource);
            if (!stats.HasStat(DamageBonus) && stats is StatBlock block) block.AddStat(DamageBonus, 0f);
            stats.AddStatModifier(DamageBonus, StatModifierKind.Flat, _demonNightDamageBonus, NightBonusSource);
        }

        private static void RemoveNightBonus(RegionEntity unit) => unit.Damageable.Stats?.RemoveStatModifiers(NightBonusSource);

        private void BindDarkening()
        {
            if (TryBindGlobalLight2D()) return;
            BindOverlay();
        }

        private bool TryBindGlobalLight2D()
        {
            Type light2DType = FindLight2DType();
            if (light2DType == null) return false;
            _lightTypeProperty = light2DType.GetProperty("lightType", BindingFlags.Instance | BindingFlags.Public);
            _intensityProperty = light2DType.GetProperty("intensity", BindingFlags.Instance | BindingFlags.Public);
            _colorProperty = light2DType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);
            Type kind = light2DType.GetNestedType("LightType", BindingFlags.Public);
            if (_lightTypeProperty == null || _intensityProperty == null || _colorProperty == null || kind == null) return false;
            _globalLightType = Enum.Parse(kind, "Global");

            UnityEngine.Object[] lights = FindObjectsByType(light2DType, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (UnityEngine.Object obj in lights)
                if (obj is Component component && Equals(_lightTypeProperty.GetValue(component), _globalLightType))
                {
                    _globalLight = component;
                    break;
                }

            if (_globalLight == null)
            {
                var go = new GameObject("Day Night Global Light");
                _globalLight = go.AddComponent(light2DType);
                _lightTypeProperty.SetValue(_globalLight, _globalLightType);
                _createdLight = true;
            }

            _baseIntensity = Mathf.Max(0.01f, ReadIntensity(_globalLight));
            _baseColor = ReadColor(_globalLight);
            return true;
        }

        private void BindOverlay()
        {
            var go = new GameObject("Day Night Tint");
            _overlay = go.AddComponent<SpriteRenderer>();
            _overlay.sprite = OverlaySprite();
            _overlay.sortingOrder = 2000;
            _overlay.color = Color.clear;
        }

        private void ApplyLight(float darkness, bool force)
        {
            if (_globalLight != null)
            {
                float targetIntensity = Mathf.Lerp(_baseIntensity, _baseIntensity * _nightLightIntensityScale, darkness);
                Color targetColor = Color.Lerp(_baseColor, _nightLightColor, darkness);
                if (force || Mathf.Abs(ReadIntensity(_globalLight) - targetIntensity) > 0.001f) _intensityProperty.SetValue(_globalLight, targetIntensity);
                if (force || ColorDeltaSqr(ReadColor(_globalLight), targetColor) > 0.0001f) _colorProperty.SetValue(_globalLight, targetColor);
                return;
            }

            if (_overlay == null) return;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 at = cam.transform.position;
                at.z = 0f;
                _overlay.transform.position = at;
                float h = cam.orthographicSize * 2f;
                float w = h * cam.aspect;
                _overlay.transform.localScale = new Vector3(w + 2f, h + 2f, 1f);
            }
            _overlay.color = Color.Lerp(Color.clear, _nightOverlayColor, darkness);
        }

        private void RestoreLight()
        {
            if (_globalLight != null)
            {
                if (_createdLight) Destroy(_globalLight.gameObject);
                else
                {
                    _intensityProperty?.SetValue(_globalLight, _baseIntensity);
                    _colorProperty?.SetValue(_globalLight, _baseColor);
                }
                _globalLight = null;
                _createdLight = false;
            }

            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        private float ReadIntensity(Component light) =>
            _intensityProperty?.GetValue(light) is float value ? value : 1f;

        private Color ReadColor(Component light) =>
            _colorProperty?.GetValue(light) is Color value ? value : Color.white;

        private static float ColorDeltaSqr(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b, da = a.a - b.a;
            return dr * dr + dg * dg + db * db + da * da;
        }

        private static Sprite OverlaySprite()
        {
            if (s_overlaySprite != null) return s_overlaySprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            s_overlaySprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return s_overlaySprite;
        }

        private static Type FindLight2DType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("UnityEngine.Rendering.Universal.Light2D", throwOnError: false);
                if (type != null) return type;
            }
            return null;
        }
    }
}
