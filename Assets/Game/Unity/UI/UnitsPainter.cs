using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The living: the horde's enemies as red squares (darker while roaming, bigger for elites), demons from the
    /// <see cref="RegionUnits"/> registry in blue, and the Character in white.
    /// </summary>
    [RegionSystem]
    public sealed class UnitsPainter : MonoBehaviour, IMapPainter
    {
        private readonly List<HordeMember> _elites = new();

        public int Order => MapOrder.Units;

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            _elites.Clear();
            if (HordeDirector.Current != null)
            {
                // Roamers first so attackers draw over them; elites last and bigger.
                foreach (HordeMember m in HordeDirector.Current.Members)
                    if (m.IsAlive && m.IsRoaming) { if (m.IsElite) _elites.Add(m); else canvas.Square(canvas.ToPixel(m.transform.position), 2, RegionMapView.Roamer); }
                foreach (HordeMember m in HordeDirector.Current.Members)
                    if (m.IsAlive && !m.IsRoaming) { if (m.IsElite) _elites.Add(m); else canvas.Square(canvas.ToPixel(m.transform.position), 2, RegionMapView.Attacker); }
            }
            foreach (HordeMember m in _elites) canvas.Outlined(canvas.ToPixel(m.transform.position), 4, m.IsRoaming ? RegionMapView.Roamer : RegionMapView.Attacker);
            foreach (RegionUnit unit in RegionUnits.Living)
                if (unit.Kind == UnitKind.Demon) canvas.Square(canvas.ToPixel(unit.GameObject.transform.position), 2, RegionMapView.Militia);
            CharacterControl character = Player.Character;
            if (character != null && !character.IsDead) canvas.Outlined(canvas.ToPixel(character.transform.position), 4, RegionMapView.You);
        }
    }
}
