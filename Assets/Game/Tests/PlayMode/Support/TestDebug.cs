using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>Pressing the debug menu's rows by label, the way the menu does.</summary>
    public static class TestDebug
    {
        public static void Press(string label)
        {
            DebugActions.Entry e = DebugActions.Find(label);
            Assert.IsNotNull(e, $"a debug row \"{label}\"");
            e.Run();
        }

        public static void Toggle(string label, bool on)
        {
            DebugActions.Entry e = DebugActions.Find(label);
            Assert.IsNotNull(e, $"a debug toggle \"{label}\"");
            e.Set(on);
        }
    }
}
