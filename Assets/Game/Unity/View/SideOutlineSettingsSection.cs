using System.Text;
using UnityEngine;

namespace Isekai
{
    /// <summary>Minimal outline colour settings controls until the full settings screen lands.</summary>
    [RegionSystem]
    public sealed class SideOutlineSettingsSection : MonoBehaviour
    {
        private static readonly OutlineColor[] Cycle =
        {
            new(61, 220, 100),
            new(255, 74, 74),
            new(255, 255, 255),
            new(242, 232, 75),
            new(183, 92, 255),
            new(64, 190, 255),
            new(255, 148, 63),
        };

        private void OnEnable()
        {
            DebugActions.Info(this, "Outlines", () => $"Preset: {SideOutlineSettingsStore.Preset}");
            DebugActions.Button(this, "Outlines", "Preset Default", () => SideOutlineSettingsStore.SetPreset(OutlinePreset.Default), "preset");
            DebugActions.Button(this, "Outlines", "Preset Deuteranopia", () => SideOutlineSettingsStore.SetPreset(OutlinePreset.Deuteranopia), "preset");
            DebugActions.Button(this, "Outlines", "Preset Protanopia", () => SideOutlineSettingsStore.SetPreset(OutlinePreset.Protanopia), "preset");
            DebugActions.Button(this, "Outlines", "Preset Tritanopia", () => SideOutlineSettingsStore.SetPreset(OutlinePreset.Tritanopia), "preset");
            DebugActions.Button(this, "Outlines", "Preset High Contrast", () => SideOutlineSettingsStore.SetPreset(OutlinePreset.HighContrast), "preset");
            DebugActions.Button(this, "Outlines", "Clear Outline Overrides", SideOutlineSettingsStore.ClearAllOverrides, "preset");

            foreach (Side side in SideRelations.All)
            {
                Side captured = side;
                DebugActions.Button(this, "Outlines", $"Cycle {captured}", () => CycleOverride(captured), "override");
            }
            DebugActions.Info(this, "Outlines", CurrentColours);
        }

        private void OnDisable() => DebugActions.Remove(this);

        private static void CycleOverride(Side side)
        {
            OutlineColor now = SideOutlineSettingsStore.ColorOf(side);
            int index = 0;
            for (int i = 0; i < Cycle.Length; i++)
            {
                if (Cycle[i] != now) continue;
                index = (i + 1) % Cycle.Length;
                break;
            }
            SideOutlineSettingsStore.SetOverride(side, Cycle[index]);
        }

        private static string CurrentColours()
        {
            var text = new StringBuilder();
            foreach (Side side in SideRelations.All)
            {
                if (text.Length > 0) text.Append("  ");
                OutlineColor c = SideOutlineSettingsStore.ColorOf(side);
                text.Append(side).Append(" #").Append(c.R.ToString("X2")).Append(c.G.ToString("X2")).Append(c.B.ToString("X2"));
            }
            return text.ToString();
        }
    }
}
