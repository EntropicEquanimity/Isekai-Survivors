using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// Buttons, toggles and readouts for the <see cref="DebugMenu"/>, added by the systems they belong to. A system
    /// registers its entries when it starts (owner = itself) and removes them when it stops, so a new mechanic brings
    /// its own debug controls without editing the menu.
    /// </summary>
    public static class DebugActions
    {
        public enum Kind { Button, Toggle, Info }

        public sealed class Entry
        {
            public object Owner;
            public string Section;
            public string Label;
            public Kind Kind;
            public Action Run;
            public Func<bool> Get;
            public Action<bool> Set;
            public Func<string> Text;
        }

        private static readonly List<Entry> s_entries = new();

        static DebugActions() => StaticReset.Register(s_entries.Clear);

        public static IReadOnlyList<Entry> All => s_entries;

        public static void Button(object owner, string section, string label, Action run) =>
            s_entries.Add(new Entry { Owner = owner, Section = section, Label = label, Kind = Kind.Button, Run = run });

        public static void Toggle(object owner, string section, string label, Func<bool> get, Action<bool> set) =>
            s_entries.Add(new Entry { Owner = owner, Section = section, Label = label, Kind = Kind.Toggle, Get = get, Set = set });

        /// <summary>A line of text, read every frame the menu is open.</summary>
        public static void Info(object owner, string section, Func<string> text) =>
            s_entries.Add(new Entry { Owner = owner, Section = section, Kind = Kind.Info, Text = text });

        /// <summary>Removes everything <paramref name="owner"/> added.</summary>
        public static void Remove(object owner) => s_entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));
    }
}
