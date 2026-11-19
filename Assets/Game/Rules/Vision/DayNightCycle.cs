using System;

namespace Isekai
{
    /// <summary>
    /// Region day-night timing: an 8-minute cycle by default with 3 minutes of night. It says whether it is night,
    /// how dark it is (0 day .. 1 night) and what vision multiplier to use (1 day .. 0.5 night).
    /// </summary>
    public sealed class DayNightCycle
    {
        public const float DefaultCycleSeconds = 8f * 60f;
        public const float DefaultNightSeconds = 3f * 60f;
        public const float DefaultTransitionSeconds = 20f;

        private readonly float _cycleSeconds;
        private readonly float _nightSeconds;
        private readonly float _transitionSeconds;
        private float _elapsedSeconds;

        public DayNightCycle(
            float cycleSeconds = DefaultCycleSeconds,
            float nightSeconds = DefaultNightSeconds,
            float transitionSeconds = DefaultTransitionSeconds,
            float elapsedSeconds = 0f)
        {
            _cycleSeconds = Math.Max(0.01f, cycleSeconds);
            _nightSeconds = Math.Max(0f, Math.Min(nightSeconds, _cycleSeconds));
            _transitionSeconds = Math.Max(0f, transitionSeconds);
            _elapsedSeconds = Math.Max(0f, elapsedSeconds);
        }

        public float CycleSeconds => _cycleSeconds;
        public float NightSeconds => _nightSeconds;
        public float DaySeconds => _cycleSeconds - _nightSeconds;
        public float NightStart => DaySeconds;
        public float ElapsedSeconds => _elapsedSeconds;
        public float TimeInCycle => Wrap(_elapsedSeconds, _cycleSeconds);

        public bool IsNight => _nightSeconds > 0f && (DaySeconds <= 0f || TimeInCycle >= NightStart);

        public float SecondsUntilChange
        {
            get
            {
                if (_nightSeconds <= 0f || DaySeconds <= 0f) return _cycleSeconds;
                float now = TimeInCycle;
                return IsNight ? _cycleSeconds - now : NightStart - now;
            }
        }

        /// <summary>0 by day, 1 at full night, eased through dusk and dawn.</summary>
        public float Darkness01
        {
            get
            {
                if (_nightSeconds <= 0f) return 0f;
                if (DaySeconds <= 0f) return 1f;
                float transition = EffectiveTransition();
                float now = TimeInCycle;

                if (transition <= 0f) return IsNight ? 1f : 0f;

                if (now < transition) return 1f - Smooth01(now / transition);   // dawn
                float duskStart = NightStart - transition;
                if (now >= duskStart && now < NightStart) return Smooth01((now - duskStart) / transition);   // dusk
                return IsNight ? 1f : 0f;
            }
        }

        /// <summary>Scales vision ranges from day (1) to night (0.5).</summary>
        public float VisionMultiplier => 1f - 0.5f * Darkness01;

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            _elapsedSeconds += deltaSeconds;
        }

        public void SetElapsed(float elapsedSeconds) => _elapsedSeconds = Math.Max(0f, elapsedSeconds);

        public void SetTimeInCycle(float seconds) => _elapsedSeconds = Wrap(Math.Max(0f, seconds), _cycleSeconds);

        public void JumpToNight()
        {
            if (_nightSeconds <= 0f) return;
            SetTimeInCycle(NightStart + 0.01f);
        }

        public void JumpToDay()
        {
            if (DaySeconds <= 0f) return;
            float transition = EffectiveTransition();
            float at = transition > 0f ? Math.Min(transition + 0.01f, Math.Max(0.01f, DaySeconds * 0.5f)) : 0.01f;
            SetTimeInCycle(at);
        }

        private float EffectiveTransition()
        {
            float max = Math.Min(DaySeconds, _nightSeconds) * 0.5f;
            return max <= 0f ? 0f : Math.Min(_transitionSeconds, max);
        }

        private static float Smooth01(float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return t * t * (3f - 2f * t);
        }

        private static float Wrap(float value, float period)
        {
            if (period <= 0f) return 0f;
            float wrapped = value % period;
            return wrapped < 0f ? wrapped + period : wrapped;
        }
    }
}
