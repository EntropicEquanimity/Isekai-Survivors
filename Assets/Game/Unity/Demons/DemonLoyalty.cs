using System.Linq;
using System.Text;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A demon's level and Loyalty (put on a demon of Portals by <see cref="RegionLoyalty"/>, never on the Character: Jack's
    /// body does not revolt). The numbers are <see cref="LoyaltyState"/>; this component does what a revolt means in the
    /// Region: the demon switches to the Rogue team (hostile to everyone, your side included), leaves its Portal, takes the
    /// low-Loyalty demons near it along, and its outline turns rogue purple. Jack wins it back (<see cref="Reclaim"/>) by
    /// beating it down to low health (it surrenders instead of dying) or by possessing it.
    /// </summary>
    public sealed class DemonLoyalty : MonoBehaviour
    {
        private readonly LoyaltyState _state = new();
        private DemonBrain _brain;
        private Team _team;

        internal RegionEntity Entity { get; set; }

        public int Level => _state.Level;
        public int Xp => _state.Xp;
        public float Loyalty => _state.Loyalty;
        public bool IsRogue => _state.IsRogue;

        /// <summary>Jack is in this body now: it neither drifts nor revolts.</summary>
        public bool IsJacksBody => Player.Character != null && Player.Character.gameObject == gameObject;

        /// <summary>The loyalty component of a demon, or null.</summary>
        public static DemonLoyalty Of(GameObject unit) => unit != null && unit.TryGetComponent(out DemonLoyalty l) ? l : null;

        private DemonBrain Brain => _brain != null ? _brain : _brain = GetComponent<DemonBrain>();

        private Team Team => _team != null ? _team : _team = GetComponent<Team>();

        /// <summary>A new life of a pooled demon: level 1, full Loyalty, back on your side if it died rogue.</summary>
        internal void StartLife(RegionEntity entity)
        {
            Entity = entity;
            bool wasRogue = _state.IsRogue || (Team != null && Team.Id == TeamIds.Rogue);
            _state.Reset();
            if (wasRogue) SetSide(Side.Yours);
        }

        /// <summary>Gives experience (a kill is <see cref="LoyaltyTuning.XpPerKill"/>); true on a level up.</summary>
        public bool AddXp(int xp)
        {
            bool up = _state.AddXp(xp);
            if (up) BLog.Info("Demons", $"{name} reaches level {_state.Level}.");
            return up;
        }

        /// <summary>Raises the level to <paramref name="level"/> by giving the experience it takes (debug, tests); never lowers it.</summary>
        public void RaiseToLevel(int level) => AddXp(LoyaltyRules.XpToReach(level) - _state.Xp);

        /// <summary>Sets Loyalty (debug, tests). Zero does not revolt by itself; the next tick does.</summary>
        public void SetLoyalty(float loyalty) => _state.SetLoyalty(loyalty);

        /// <summary>Lets time pass against Jack's soul level; the demon revolts when its Loyalty reaches zero.</summary>
        internal void Tick(float seconds, int soulLevel)
        {
            if (IsJacksBody || Entity == null || !Entity.IsListed) return;
            if (_state.Tick(seconds, soulLevel)) Rebel(true);
        }

        /// <summary>Turns the demon rogue now (a debug row, a follower). False when it already is or Jack is in it.</summary>
        public bool MakeRogue(bool takeOthers = true)
        {
            if (IsJacksBody || Entity == null || !_state.GoRogue()) return false;
            Rebel(takeOthers);
            return true;
        }

        private void Rebel(bool takeOthers)
        {
            SetSide(Side.Rogue);
            if (Brain != null) Brain.Detach(true);
            BLog.Info("Demons", $"{name} (level {_state.Level}) turns rogue.");
            if (takeOthers) RegionLoyalty.Current?.Follow(this);
        }

        /// <summary>
        /// Jack wins a rogue demon back (he beat it down or possessed it): it is on your side again, and goes to the nearest Portal
        /// with room under its cap, or guards near Jack when none has room. False when it was not rogue.
        /// </summary>
        public bool Reclaim()
        {
            if (!_state.IsRogue) return false;
            _state.WinBack();
            SetSide(Side.Yours);
            BLog.Info("Demons", $"{name} is won back.");
            if (IsJacksBody || Brain == null) return true;
            UnitProducer portal = NearestPortalWithRoom();
            if (portal != null && portal.TryTake(Brain)) return true;
            CharacterControl jack = Player.Character;
            Brain.GuardAt(jack != null && !jack.IsDead ? jack.transform.position + (Vector3)(Random.insideUnitCircle * 2f) : transform.position);
            return true;
        }

        private UnitProducer NearestPortalWithRoom()
        {
            UnitProducer best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < Building.All.Count; i++)
            {
                Building b = Building.All[i];
                if (b == null || !b.IsPlayers || !b.TryGetComponent(out UnitProducer p) || !p.HasRoom) continue;
                float sqr = ((Vector2)(b.transform.position - transform.position)).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = p;
            }
            return best;
        }

        private void SetSide(Side side)
        {
            if (Team == null) return;
            Team.Id = TeamIds.Of(side);
            if (Entity != null) SideOutlineApplier.Reapply(Entity);
        }

        /// <summary>"Level 2  Loyalty 54/100" (and "rogue", and its Traits), for the selection read-outs.</summary>
        public string Describe(bool withTraits = true)
        {
            var text = new StringBuilder();
            text.Append("Level ").Append(Level).Append("  Loyalty ").Append(Mathf.CeilToInt(Loyalty)).Append('/').Append((int)LoyaltyTuning.MaxLoyalty);
            if (IsRogue) text.Append("  ROGUE");
            UnitTraits traits = withTraits ? UnitTraits.Of(gameObject) : null;
            if (traits != null) text.Append("  ").Append(string.Join(", ", traits.Defs().Select(d => d.Name)));
            return text.ToString();
        }
    }
}
