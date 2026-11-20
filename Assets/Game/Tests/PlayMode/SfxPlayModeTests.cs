using System;
using System.Collections;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>
    /// The sound effects: a cue plays its sound, a horde of deaths stays under the voice cap, what the player cannot see
    /// is silent. Batch mode may have no audio device, so these read the player's state and the AudioSources' clip, volume
    /// and pitch, never real output.
    /// </summary>
    public class SfxPlayModeTests
    {
        private SfxPlayer _sfx;
        private Vector3 _jack;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            GameAudio.SetVolume(AudioBuses.Sfx, 1f);
            GameAudio.SetVolume(AudioBuses.Ui, 1f);
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return TestWait.Frames(3);
            _sfx = SfxPlayer.Current;
            Assert.IsNotNull(_sfx, "the Sfx Player lives across scenes");
            Assert.IsNotNull(_sfx.Library, "the library is in Resources");
            _jack = Player.Character.transform.position;
            yield return Quiet();
        }

        [TearDown]
        public void Restore()
        {
            GameAudio.SetVolume(AudioBuses.Sfx, 1f);
            GameAudio.SetVolume(AudioBuses.Ui, 1f);
            Time.timeScale = 1f;
        }

        // Real time for the longest cooldown and every clip to be over.
        private static IEnumerator Quiet()
        {
            float end = Time.realtimeSinceStartup + 1.1f;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [Test]
        public void Every_cue_has_a_sound_and_settings()
        {
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
            {
                AudioClip[] clips = _sfx.Library.ClipsFor(cue);
                Assert.IsNotEmpty(clips, $"{cue} has a sound");
                Assert.IsTrue(clips.All(c => c != null && c.length > 0f), $"{cue}'s clips load");
                SfxCue s = SfxCues.For(cue);
                Assert.Less(s.PitchMin, s.PitchMax + 1e-4f, $"{cue}'s pitch range");
            }
        }

        [Test]
        public void A_cue_plays_one_of_its_sounds_with_pitch_and_volume_variation()
        {
            int before = _sfx.Played;
            Feedback.Raise(Cue.Mined, _jack);
            Assert.AreEqual(before + 1, _sfx.Played);
            AudioSource source = _sfx.SourceOf(Cue.Mined);
            CollectionAssert.Contains(_sfx.Library.ClipsFor(Cue.Mined), source.clip);
            Assert.AreSame(source.clip, _sfx.PlayingClipOf(Cue.Mined));
            SfxCue setting = SfxCues.For(Cue.Mined);
            Assert.That(source.pitch, Is.InRange(setting.PitchMin, setting.PitchMax));
            Assert.That(source.volume, Is.InRange(setting.Volume * 0.8f, setting.Volume));
        }

        [UnityTest]
        public IEnumerator The_pitch_varies_from_one_sound_to_the_next()
        {
            var pitches = new System.Collections.Generic.HashSet<float>();
            for (int i = 0; i < 8; i++)
            {
                float end = Time.realtimeSinceStartup + SfxCues.For(Cue.Summon).Cooldown + 0.02f;
                while (Time.realtimeSinceStartup < end) yield return null;
                Feedback.Raise(Cue.Summon, _jack);
                pitches.Add(_sfx.SourceOf(Cue.Summon).pitch);
            }
            Assert.Greater(pitches.Count, 1, "not the same pitch every time");
        }

        [Test]
        public void A_cue_in_its_cooldown_does_not_play_again()
        {
            Feedback.Raise(Cue.Death, _jack);
            int played = _sfx.Played;
            int dropped = _sfx.DroppedByCooldown;
            Feedback.Raise(Cue.Death, _jack);
            Assert.AreEqual(played, _sfx.Played);
            Assert.AreEqual(dropped + 1, _sfx.DroppedByCooldown);
        }

        [Test]
        public void Every_cue_at_once_stays_under_the_voice_cap()
        {
            foreach (Cue cue in Enum.GetValues(typeof(Cue))) Feedback.Raise(cue, _jack);
            Assert.LessOrEqual(_sfx.ActiveVoices, SfxPlayer.VoiceCap);
            Assert.AreEqual(SfxPlayer.VoiceCap, _sfx.ActiveVoices, "the pool is full");
            Assert.Greater(_sfx.DroppedByCap, 0, "the cues past the cap were dropped");
        }

        [UnityTest]
        public IEnumerator Two_hundred_deaths_in_one_frame_stay_under_the_voice_cap()
        {
            HordeDebug.SpawnNearCharacter(200);
            yield return null;
            var members = Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsAlive).ToArray();
            Assert.GreaterOrEqual(members.Length, 150, "a horde of mortals to kill");
            int deaths = 0;
            void Count(FeedbackCue c) { if (c.Kind == Cue.Death) deaths++; }
            Feedback.Raised += Count;
            int played = _sfx.Played;
            foreach (HordeMember m in members) m.Damageable.Kill();
            Feedback.Raised -= Count;

            Assert.GreaterOrEqual(deaths, members.Length / 2, "the deaths were raised as cues");
            Assert.LessOrEqual(_sfx.ActiveVoices, SfxPlayer.VoiceCap);
            Assert.Greater(_sfx.Played, played, "a death was heard");
            Assert.Less(_sfx.Played - played, 20, "most of the deaths made no sound of their own");
        }

        [Test]
        public void A_cue_out_of_sight_and_far_from_the_camera_does_not_play()
        {
            RegionVision vision = RegionVision.Current;
            RegionGrid grid = RegionGrid.Current;
            Vector3 camera = Camera.main.transform.position;
            Vector3 unseen = default;
            bool found = false;
            for (int x = 0; x < grid.Width && !found; x += 3)
            for (int y = 0; y < grid.Height && !found; y += 3)
            {
                var cell = new Int2(x, y);
                Vector3 at = grid.CentreOf(cell);
                if (vision.CanSee(Side.Yours, cell) || Vector2.Distance(at, camera) < SfxAudibility.HearDistance + 10f) continue;
                unseen = at;
                found = true;
            }
            Assert.IsTrue(found, "some ground is unseen and far from the camera");

            int played = _sfx.Played;
            int dropped = _sfx.DroppedOutOfSight;
            Feedback.Raise(Cue.Death, unseen);
            Assert.AreEqual(played, _sfx.Played, "unseen: no sound");
            Assert.AreEqual(dropped + 1, _sfx.DroppedOutOfSight);

            Feedback.Raise(Cue.Death, _jack);
            Assert.AreEqual(played + 1, _sfx.Played, "what the player sees is heard");
        }

        [Test]
        public void A_cue_with_no_place_plays_wherever_it_happened()
        {
            int played = _sfx.Played;
            Feedback.Raise(Cue.Wave);
            Assert.AreEqual(played + 1, _sfx.Played);
        }

        [Test]
        public void The_Sfx_volume_setting_scales_the_sounds_and_zero_silences_them()
        {
            SfxCue setting = SfxCues.For(Cue.Possess);
            GameAudio.SetVolume(AudioBuses.Sfx, 0.5f);
            Feedback.Raise(Cue.Possess, _jack);
            Assert.LessOrEqual(_sfx.SourceOf(Cue.Possess).volume, setting.Volume * 0.5f + 1e-4f, "half the Sfx bus");
            Assert.Greater(_sfx.SourceOf(Cue.Possess).volume, 0f);

            GameAudio.SetVolume(AudioBuses.Sfx, 0f);
            int played = _sfx.Played;
            Feedback.Raise(Cue.Revive, _jack);
            Assert.AreEqual(played, _sfx.Played, "Sfx at 0 is silent");
        }

        [Test]
        public void Interface_cues_follow_the_Ui_volume()
        {
            GameAudio.SetVolume(AudioBuses.Ui, 0f);
            GameAudio.SetVolume(AudioBuses.Sfx, 1f);
            int played = _sfx.Played;
            Feedback.Raise(Cue.UiClick);
            Assert.AreEqual(played, _sfx.Played, "Ui at 0 silences clicks");
            GameAudio.SetVolume(AudioBuses.Ui, 1f);
            Feedback.Raise(Cue.UiClick);
            Assert.AreEqual(played + 1, _sfx.Played);
        }

        // ---- The Region raises the cues ----

        private static (Action Unhook, Func<Cue, bool> Raised) Listen()
        {
            var seen = new System.Collections.Generic.HashSet<Cue>();
            void On(FeedbackCue c) => seen.Add(c.Kind);
            Feedback.Raised += On;
            return (() => Feedback.Raised -= On, seen.Contains);
        }

        [UnityTest]
        public IEnumerator Killing_a_mortal_raises_Death_and_a_hit_raises_Hit()
        {
            var (unhook, raised) = Listen();
            HordeDebug.SpawnNearCharacter(1);
            yield return null;
            HordeMember mortal = Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).First(m => m.IsAlive);
            mortal.Damageable.Unit.TakeDamage(1f, Blondie.Combat.DamageRules.PhysicalType, Player.Character.GetComponent<Blondie.Combat.Damageable>().Unit);
            Assert.IsTrue(raised(Cue.Hit), "a hit with an attacker");
            mortal.Damageable.Kill();
            Assert.IsTrue(raised(Cue.Death));
            unhook();
        }

        [UnityTest]
        public IEnumerator Mining_Souls_stages_Waves_and_placing_raise_their_cues()
        {
            var (unhook, raised) = Listen();
            ResourceNode node = ResourceNode.All.First(n => !n.IsEmpty);
            node.Take(1);
            Assert.IsTrue(raised(Cue.Mined));

            RegionEconomy.Current.Stockpile.Add(ResourceType.Souls, 7);
            Assert.IsTrue(raised(Cue.SoulsGained));

            VigilanceDirector.Current.RaiseStage();
            VigilanceDirector.Current.SendWaveNow();
            yield return null;
            Assert.IsTrue(raised(Cue.VigilanceStage), "a new stage");
            Assert.IsTrue(raised(Cue.Wave), "a Wave on its way");

            Building mainBase = Building.All.First(b => b.IsMainBase);
            BuildingAsset portal = Object.FindAnyObjectByType<BuildMode>().Catalogue.First(a => a.DisplayName == "Portal");
            Int2 cell = new(mainBase.Origin.x + 4 * RegionTerrain.Current.Layout.Interior.x, mainBase.Origin.y);
            Building placed = RegionBuildings.Current.Place(portal, cell);
            Assert.IsNotNull(placed, "the Portal was placed");
            Assert.IsTrue(raised(Cue.BuildPlaced));
            placed.FinishConstruction();
            Assert.IsTrue(raised(Cue.BuildDone));
            unhook();
        }

        [UnityTest]
        public IEnumerator Jack_dying_raises_Wisp()
        {
            var (unhook, raised) = Listen();
            Player.Character.GetComponent<Blondie.Combat.Damageable>().Kill();
            yield return null;
            Assert.IsTrue(raised(Cue.Wisp), "his body fell");
            unhook();
        }
    }
}
