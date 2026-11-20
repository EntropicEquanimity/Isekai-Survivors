using Blondie.Core;

namespace Isekai
{
    /// <summary>How one cue sounds: volume, pitch spread, the quiet time after it plays, and whether it needs to be seen or near.</summary>
    public readonly struct SfxCue
    {
        public SfxCue(float volume, float pitchMin, float pitchMax, float cooldown, bool world, string bus = AudioBuses.Sfx)
        {
            Volume = volume;
            PitchMin = pitchMin;
            PitchMax = pitchMax;
            Cooldown = cooldown;
            World = world;
            Bus = bus;
        }

        public float Volume { get; }
        public float PitchMin { get; }
        public float PitchMax { get; }

        /// <summary>Seconds (real time) before the same cue may play again, so a horde fight is not a wall of one sound.</summary>
        public float Cooldown { get; }

        /// <summary>Plays only for what the player can see or is near; false plays wherever it happened (a Wave, a click).</summary>
        public bool World { get; }

        /// <summary>The Blondie Core audio bus whose volume it follows: Sfx, or Ui for the interface.</summary>
        public string Bus { get; }
    }

    /// <summary>The sound settings of every <see cref="Cue"/>.</summary>
    public static class SfxCues
    {
        public static SfxCue For(Cue cue) => cue switch
        {
            Cue.Hit => new SfxCue(0.45f, 0.85f, 1.2f, 0.07f, true),
            Cue.Death => new SfxCue(0.65f, 0.85f, 1.15f, 0.06f, true),
            Cue.BuildPlaced => new SfxCue(0.8f, 0.9f, 1.1f, 0.1f, true),
            Cue.BuildDone => new SfxCue(0.85f, 0.95f, 1.05f, 0.2f, true),
            Cue.Mined => new SfxCue(0.55f, 0.9f, 1.15f, 0.2f, true),
            Cue.SoulsGained => new SfxCue(0.45f, 0.9f, 1.3f, 0.12f, true),
            Cue.Wisp => new SfxCue(0.9f, 0.95f, 1.05f, 1f, true),
            Cue.Revive => new SfxCue(0.9f, 0.95f, 1.05f, 1f, true),
            Cue.Possess => new SfxCue(0.9f, 0.95f, 1.05f, 0.5f, true),
            Cue.Summon => new SfxCue(0.75f, 0.9f, 1.1f, 0.15f, true),
            Cue.Alarm => new SfxCue(0.8f, 0.95f, 1.05f, 0.5f, true),
            Cue.Rider => new SfxCue(0.75f, 0.9f, 1.1f, 0.5f, true),
            Cue.Wave => new SfxCue(0.95f, 1f, 1f, 1f, false),
            Cue.VigilanceStage => new SfxCue(0.9f, 1f, 1f, 1f, false),
            Cue.CorruptionSpread => new SfxCue(0.5f, 0.85f, 1.15f, 0.4f, true),
            Cue.Capture => new SfxCue(1f, 1f, 1f, 1f, false),
            Cue.UiClick => new SfxCue(0.7f, 0.95f, 1.05f, 0.04f, false, AudioBuses.Ui),
            Cue.UiHover => new SfxCue(0.4f, 0.95f, 1.1f, 0.06f, false, AudioBuses.Ui),
            _ => new SfxCue(1f, 1f, 1f, 0.1f, false),
        };
    }
}
