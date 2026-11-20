using System.Collections;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>
    /// The music follows the screen and the Region's state. Batch mode may have no audio device, so these read the player's
    /// state and the music AudioSources' clip and volume, never real output.
    /// </summary>
    public class MusicPlayModeTests
    {
        private const float Seconds = 6f;

        [SetUp]
        public void SetUp()
        {
            GameAudio.SetVolume(AudioBuses.Music, 1f);
            GameAudio.SetVolume(AudioBuses.Master, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            GameAudio.SetVolume(AudioBuses.Music, 1f);
            GameAudio.SetVolume(AudioBuses.Master, 1f);
            Time.timeScale = 1f;
        }

        private static MusicPlayer Player => MusicPlayer.Current;

        private static AudioSource[] Sources() =>
            Object.FindAnyObjectByType<GameAudioPlayer>().GetComponentsInChildren<AudioSource>(true)
                .Where(s => s.name.StartsWith("Music")).ToArray();

        private static AudioSource SourceOf(AudioClip clip) => Sources().FirstOrDefault(s => s.clip == clip);

        private static IEnumerator WaitForState(MusicState state) =>
            TestWait.Until(() => Player != null && Player.State == state, $"music state {state}", Seconds);

        // The track has finished fading in: it is the only one heard, at the music volume.
        private static IEnumerator WaitForSettled(AudioClip clip) =>
            TestWait.Until(() =>
            {
                AudioSource source = SourceOf(clip);
                return source != null && Mathf.Approximately(source.volume, GameAudio.GetVolume(AudioBuses.Music))
                       && Sources().All(s => s == source || s.volume == 0f);
            }, $"{(clip != null ? clip.name : "no track")} fully faded in", Seconds);

        [Test]
        public void The_state_follows_the_screen_and_the_Region()
        {
            Assert.AreEqual(MusicState.Menu, MusicStates.Pick("Boot", null, null, false, default));
            Assert.AreEqual(MusicState.Menu, MusicStates.Pick("Menu", null, null, false, default));
            Assert.AreEqual(MusicState.Planet, MusicStates.Pick(RunSession.PlanetScene, null, null, false, default));
            Assert.AreEqual(MusicState.RegionCalm, MusicStates.Pick(RunSession.RegionScene, null, null, false, default));
            Assert.AreEqual(MusicState.Captured, MusicStates.Pick(RunSession.RegionScene, null, null, true, RegionEnd.Captured));
            Assert.AreEqual(MusicState.Defeat, MusicStates.Pick(RunSession.RegionScene, null, null, true, RegionEnd.Defeated));
        }

        [UnityTest]
        public IEnumerator The_Planet_screen_has_its_own_track()
        {
            RunSession.StartNew(1);
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return WaitForState(MusicState.Planet);
            Assert.IsNotNull(Player.Clip);
            Assert.AreSame(Player.Library.Planet, Player.Clip);
            yield return WaitForSettled(Player.Library.Planet);
        }

        [UnityTest]
        public IEnumerator A_Region_starts_calm_and_turns_tense_with_Vigilance_with_a_crossfade()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return WaitForState(MusicState.RegionCalm);
            MusicLibrary library = Player.Library;
            Assert.AreNotSame(library.RegionCalm, library.RegionTense);
            yield return WaitForSettled(library.RegionCalm);

            VigilanceDirector director = VigilanceDirector.Current;
            director.RaiseStage();
            director.RaiseStage();
            Assert.GreaterOrEqual(director.Stage, VigilanceStage.Alarmed);
            yield return WaitForState(MusicState.RegionTense);

            // Crossfading: the calm track is still audible while the tense one comes in.
            AudioSource calm = SourceOf(library.RegionCalm);
            AudioSource tense = SourceOf(library.RegionTense);
            Assert.IsNotNull(tense, "the tense track is on a music source");
            Assert.Less(tense.volume, GameAudio.GetVolume(AudioBuses.Music), "the new track fades in");
            Assert.IsTrue(calm != null && calm.volume > 0f, "the old track fades out");

            yield return WaitForSettled(library.RegionTense);
        }

        [UnityTest]
        public IEnumerator A_Wave_alive_is_tense_even_at_a_low_Vigilance()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            VigilanceDirector director = VigilanceDirector.Current;
            yield return WaitForState(MusicState.RegionCalm);
            Assert.Less(director.Stage, VigilanceStage.Alarmed);
            Assert.IsTrue(director.SendWaveNow(), "a Wave was sent");
            yield return WaitForState(MusicState.RegionTense);
        }

        [UnityTest]
        public IEnumerator The_Region_ending_plays_the_capture_or_defeat_track()
        {
            yield return RegionTestScene.Load();
            yield return WaitForState(MusicState.RegionCalm);
            RegionEvents.End(RegionEnd.Defeated);
            yield return WaitForState(MusicState.Defeat);
            Assert.AreSame(Player.Library.Defeat, Player.Clip);
            yield return WaitForSettled(Player.Library.Defeat);
            Assert.AreNotSame(Player.Library.Defeat, Player.Library.Capture);
        }

        [UnityTest]
        public IEnumerator Capturing_plays_the_capture_track()
        {
            yield return RegionTestScene.Load();
            yield return WaitForState(MusicState.RegionCalm);
            RegionEvents.End(RegionEnd.Captured);
            yield return WaitForState(MusicState.Captured);
            Assert.AreSame(Player.Library.Capture, Player.Clip);
        }

        [UnityTest]
        public IEnumerator Music_volume_zero_silences_the_music_and_master_follows()
        {
            yield return RegionTestScene.Load();
            yield return WaitForState(MusicState.RegionCalm);
            AudioClip clip = Player.Clip;
            yield return WaitForSettled(clip);
            Assert.Greater(SourceOf(clip).volume, 0f);

            GameAudio.SetVolume(AudioBuses.Music, 0f);
            yield return TestWait.Until(() => Sources().All(s => s.volume == 0f), "the music sources are silent", Seconds);

            GameAudio.SetVolume(AudioBuses.Music, 0.5f);
            yield return TestWait.Until(() => Mathf.Approximately(SourceOf(clip).volume, 0.5f), "the music follows its volume", Seconds);

            GameAudio.SetVolume(AudioBuses.Master, 0f);
            Assert.AreEqual(0f, GameAudio.MasterOutput, "master volume 0 silences everything");
        }
    }
}
