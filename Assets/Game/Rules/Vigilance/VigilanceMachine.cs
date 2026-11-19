using System;

namespace Isekai
{
    /// <summary>
    /// Vigilance as points on a stage machine (Unaware, Uneasy, Alarmed, Mobilized, Crusade). Kills, riders and being
    /// spotted raise it; it decays slowly while nothing is seen, but never below the highest stage reached minus one.
    /// </summary>
    public sealed class VigilanceMachine
    {
        private float _points;
        private float _unseenSeconds;

        public float Points => _points;
        public VigilanceStage Stage => VigilanceTuning.StageAt(_points);

        /// <summary>The highest stage reached so far (the floor for decay is one below it).</summary>
        public VigilanceStage Highest { get; private set; }

        /// <summary>Where decay stops: the start of the highest stage reached minus one.</summary>
        public float Floor => VigilanceTuning.Threshold(VigilanceTuning.Clamp((int)Highest - 1));

        /// <summary>0..1 progress from this stage to the next, 1 at Crusade.</summary>
        public float ProgressToNext
        {
            get
            {
                VigilanceStage stage = Stage;
                if (stage == VigilanceStage.Crusade) return 1f;
                float from = VigilanceTuning.Threshold(stage);
                float to = VigilanceTuning.Threshold(stage + 1);
                return Math.Clamp((_points - from) / (to - from), 0f, 1f);
            }
        }

        /// <summary>A unit died. Only a mortal's death counts (never wildlife); a witnessed kill is worth more.</summary>
        public bool OnKill(UnitKind kind, bool witnessed)
        {
            if (kind != UnitKind.Mortal) return false;
            return Add(witnessed ? VigilanceTuning.WitnessedKillPoints : VigilanceTuning.KillPoints);
        }

        /// <summary>A rider reached the Seat: one full stage up (to the start of the next).</summary>
        public bool OnRiderReachedSeat() => RaiseStage();

        /// <summary>Jack or the Anchor was spotted.</summary>
        public bool OnSpotted() => Add(VigilanceTuning.SpottedPoints);

        /// <summary>Raises to the start of the next stage (a rider, the debug menu); false at Crusade.</summary>
        public bool RaiseStage()
        {
            VigilanceStage stage = Stage;
            if (stage == VigilanceStage.Crusade) return false;
            return Add(VigilanceTuning.Threshold(stage + 1) - _points);
        }

        /// <summary>Debug: drops to the start of the stage below and forgets having reached higher.</summary>
        public bool LowerStage()
        {
            VigilanceStage stage = Stage;
            if (stage == VigilanceStage.Unaware) return false;
            _points = VigilanceTuning.Threshold(stage - 1);
            Highest = stage - 1;
            _unseenSeconds = 0f;
            return true;
        }

        /// <summary>Time passes; once nothing has been seen for a while Vigilance decays, down to the floor.</summary>
        public void Tick(float seconds)
        {
            if (seconds <= 0f) return;
            _unseenSeconds += seconds;
            float decaying = Math.Min(seconds, _unseenSeconds - VigilanceTuning.DecayGraceSeconds);
            if (decaying <= 0f || _points <= Floor) return;
            _points = Math.Max(Floor, _points - VigilanceTuning.DecayPointsPerSecond * decaying);
        }

        private bool Add(float points)
        {
            if (points <= 0f) return false;
            _points += points;
            _unseenSeconds = 0f;
            if (Stage > Highest) Highest = Stage;
            return true;
        }
    }
}
