using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using Blondie.Roguelike.Boons;

namespace Isekai
{
    /// <summary>
    /// The buffs Regions give when captured: Blondie Roguelike Boons on the Character's stats for the rest of the Run.
    /// Each Region on the Planet is given one of these ids.
    /// </summary>
    public static class RegionBuffs
    {
        private static Dictionary<string, Boon> s_boons;

        private static Dictionary<string, Boon> Boons => s_boons ??= new[]
        {
            Make("ironhide", "Ironhide", "+5 armor", StatId.Armor, StatModifierKind.Flat, 5f),
            Make("fleet", "Fleet-footed", "+10% speed", StatId.Speed, StatModifierKind.PercentAdd, 0.1f),
            Make("prospector", "Prospector", "+50% mining speed", StatId.MiningSpeed, StatModifierKind.PercentAdd, 0.5f),
            Make("bloodlust", "Bloodlust", "+15% damage", StatId.DamageBonus, StatModifierKind.Flat, 0.15f),
            Make("vigor", "Vigor", "+30 max health", StatId.HealthMax, StatModifierKind.Flat, 30f),
            Make("keen", "Keen Eye", "+5% critical chance", StatId.CritChance, StatModifierKind.Flat, 0.05f),
        }.ToDictionary(b => b.Id);

        public static IReadOnlyList<string> Ids => Boons.Keys.ToList();

        public static Boon Get(string id) => id != null && Boons.TryGetValue(id, out Boon b) ? b : null;

        public static string Describe(string id)
        {
            Boon b = Get(id);
            return b == null ? "" : $"{b.Name}: {b.Description}";
        }

        private static Boon Make(string id, string name, string description, StatId stat, StatModifierKind kind, float value)
        {
            Boon b = Boon.Create(id, name, description: description);
            b.Modifiers.Add(new BoonModifier(stat.ToString(), kind, value));
            return b;
        }
    }
}
