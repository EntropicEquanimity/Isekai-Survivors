using System;
using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Isekai
{
    /// <summary>
    /// Plays the sound of every <see cref="Feedback"/> cue, across scenes. A fixed pool of <see cref="VoiceCap"/> sources
    /// is all it ever uses: a cue still in its cooldown, out of the player's sight and hearing (<see cref="SfxAudibility"/>),
    /// or arriving when every voice is busy is dropped, so a horde fight never stacks hundreds of sounds. Each sound gets
    /// a little random pitch and volume. Volume follows Blondie Core's <see cref="GameAudio"/> Sfx bus (the settings
    /// screen's sound slider; the interface cues use the Ui bus), and Master through the listener.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        public const int VoiceCap = 16;

        private readonly float[] _lastPlayed = new float[Enum.GetValues(typeof(Cue)).Length];
        private readonly Cue[] _voiceCue = new Cue[VoiceCap];
        private readonly float[] _voiceEnds = new float[VoiceCap];
        private AudioSource[] _sources;
        private SfxLibrary _library;

        public static SfxPlayer Current { get; private set; }

        /// <summary>Sounds playing now (counted from when each was started and how long its clip lasts).</summary>
        public int ActiveVoices
        {
            get
            {
                int n = 0;
                float now = Time.unscaledTime;
                for (int i = 0; i < VoiceCap; i++)
                    if (_voiceEnds[i] > now) n++;
                return n;
            }
        }

        /// <summary>Sounds started, and cues dropped because they were out of sight, in cooldown or had no free voice.</summary>
        public int Played { get; private set; }
        public int DroppedOutOfSight { get; private set; }
        public int DroppedByCooldown { get; private set; }
        public int DroppedByCap { get; private set; }

        public SfxLibrary Library => _library;

        /// <summary>The pool's sources (their clip, volume and pitch are what the last sound set).</summary>
        public IReadOnlyList<AudioSource> Sources => _sources;

        /// <summary>The clip of the sound of <paramref name="cue"/> that is playing now, or null.</summary>
        public AudioClip PlayingClipOf(Cue cue)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < VoiceCap; i++)
                if (_voiceEnds[i] > now && _voiceCue[i] == cue) return _sources[i].clip;
            return null;
        }

        /// <summary>The source that started the sound of <paramref name="cue"/> last (null before any).</summary>
        public AudioSource SourceOf(Cue cue)
        {
            AudioSource best = null;
            float latest = float.NegativeInfinity;
            for (int i = 0; i < VoiceCap; i++)
                if (_voiceCue[i] == cue && _voiceEnds[i] > latest) { latest = _voiceEnds[i]; best = _sources[i]; }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Current != null) return;
            var go = new GameObject("Sfx Player");
            DontDestroyOnLoad(go);
            go.AddComponent<SfxPlayer>();
        }

        private void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this;
            _library = Resources.Load<SfxLibrary>("SfxLibrary");
            if (_library == null) BLog.Warning("Audio", "No SfxLibrary in Resources: there will be no sound effects.");
            _sources = new AudioSource[VoiceCap];
            for (int i = 0; i < VoiceCap; i++)
            {
                var child = new GameObject($"Sfx Voice {i}");
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true;
                _sources[i] = source;
            }
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = float.NegativeInfinity;
            UiFeedback.Register();
        }

        private void OnEnable() => Feedback.Raised += OnCue;

        private void OnDisable() => Feedback.Raised -= OnCue;

        private void OnDestroy()
        {
            if (Current != this) return;
            Current = null;
            UiFeedback.Unregister();
        }

        private void OnCue(FeedbackCue cue) => Play(cue);

        /// <summary>Plays the sound of a cue unless it is in its cooldown, unseen, silent (volume 0) or every voice is busy.</summary>
        public bool Play(FeedbackCue cue)
        {
            if (_library == null) return false;
            SfxCue setting = SfxCues.For(cue.Kind);
            float now = Time.unscaledTime;
            int k = (int)cue.Kind;
            if (now - _lastPlayed[k] < setting.Cooldown) { DroppedByCooldown++; return false; }
            float bus = GameAudio.GetVolume(setting.Bus);
            if (bus <= 0f) return false;
            if (setting.World && cue.HasPosition && !SfxAudibility.IsAudible(cue.Position)) { DroppedOutOfSight++; return false; }
            AudioClip[] clips = _library.ClipsFor(cue.Kind);
            if (clips.Length == 0) return false;

            int voice = FreeVoice(now);
            if (voice < 0) { DroppedByCap++; return false; }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return false;
            float pitch = Random.Range(setting.PitchMin, setting.PitchMax);
            AudioSource source = _sources[voice];
            source.clip = clip;
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(setting.Volume * Random.Range(0.85f, 1f) * bus);
            source.Play();
            _voiceCue[voice] = cue.Kind;
            _voiceEnds[voice] = now + clip.length / Mathf.Max(0.1f, pitch);
            _lastPlayed[k] = now;
            Played++;
            return true;
        }

        private int FreeVoice(float now)
        {
            for (int i = 0; i < VoiceCap; i++)
                if (_voiceEnds[i] <= now) return i;
            return -1;
        }
    }
}
