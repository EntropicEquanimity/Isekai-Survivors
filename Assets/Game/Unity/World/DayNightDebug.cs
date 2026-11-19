namespace Isekai
{
    /// <summary>Debug rows for the day-night cycle.</summary>
    [RegionSystem]
    public sealed class DayNightDebug : UnityEngine.MonoBehaviour
    {
        private bool _fast;

        private void OnEnable()
        {
            DebugActions.Button(this, "Character", "Skip to night", () => RegionDayNight.Current?.SkipToNight(), "day-night");
            DebugActions.Button(this, "Character", "Skip to day", () => RegionDayNight.Current?.SkipToDay(), "day-night");
            DebugActions.Toggle(this, "Speed", "Day/night ×10", () => _fast, SetFast);
        }

        private void OnDisable() => DebugActions.Remove(this);

        private void SetFast(bool on)
        {
            _fast = on;
            RegionDayNight dayNight = RegionDayNight.Current;
            if (dayNight != null) dayNight.CycleSpeedMultiplier = on ? 10f : 1f;
        }
    }
}
