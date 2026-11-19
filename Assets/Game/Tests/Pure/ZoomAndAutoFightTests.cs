using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class ZoomLevelsTests
    {
        private static readonly Int2 FullHd = new(1920, 1080);

        [Test]
        public void View_is_the_screen_divided_by_the_scale()
        {
            var zoom = new ZoomLevels(1, 6, 3, new Int2(4000, 4000));
            Assert.AreEqual(new Int2(640, 360), zoom.ViewFor(FullHd));
            zoom.Step(+1);
            Assert.AreEqual(new Int2(480, 270), zoom.ViewFor(FullHd), "zoomed in: fewer art pixels on screen");
        }

        [Test]
        public void Steps_stop_at_the_limits()
        {
            var zoom = new ZoomLevels(2, 4, 3, new Int2(4000, 4000));
            Assert.IsTrue(zoom.Step(+5));
            Assert.AreEqual(4, zoom.Scale);
            Assert.IsFalse(zoom.Step(+1), "already fully zoomed in");
            zoom.Step(-10);
            Assert.AreEqual(2, zoom.Scale);
        }

        [Test]
        public void Zooming_out_never_renders_more_than_the_max_view()
        {
            var zoom = new ZoomLevels(1, 6, 1, new Int2(960, 540));
            Assert.AreEqual(2, zoom.EffectiveScale(FullHd), "x1 would render 1920x1080");
            Assert.AreEqual(new Int2(960, 540), zoom.ViewFor(FullHd));
            Assert.AreEqual(4, zoom.EffectiveScale(new Int2(3840, 2160)), "a 4K screen needs x4 for the same view");
        }

        [Test]
        public void Start_scale_is_clamped()
        {
            Assert.AreEqual(6, new ZoomLevels(1, 6, 9, new Int2(4000, 4000)).Scale);
        }
    }

    public class AutoFightTests
    {
        private static AutoFight Idle()
        {
            var fight = new AutoFight { IdleDelay = 1f, Leash = 6f, ReturnTolerance = 0.5f };
            fight.Tick(1.1f, playerInput: false);
            return fight;
        }

        [Test]
        public void Takes_over_only_after_the_idle_delay()
        {
            var fight = new AutoFight { IdleDelay = 1f };
            fight.Tick(0.6f, false);
            Assert.AreEqual(AutoFightOrder.PlayerControl, fight.Decide(1f, 1f, 3f, 0f));
            fight.Tick(0.6f, false);
            Assert.AreEqual(AutoFightOrder.Hold, fight.Decide(1f, 1f, 3f, 0f));
        }

        [Test]
        public void Input_hands_control_back_at_once()
        {
            AutoFight fight = Idle();
            fight.Tick(0.01f, playerInput: true);
            Assert.IsFalse(fight.IsActive);
            Assert.AreEqual(AutoFightOrder.PlayerControl, fight.Decide(1f, 1f, 3f, 0f));
        }

        [Test]
        public void Holds_in_range_and_approaches_out_of_range()
        {
            AutoFight fight = Idle();
            Assert.AreEqual(AutoFightOrder.Hold, fight.Decide(2f, 2f, 3f, 0f));
            Assert.AreEqual(AutoFightOrder.Approach, fight.Decide(5f, 5f, 3f, 0f));
        }

        [Test]
        public void Ignores_enemies_beyond_the_leash_and_walks_back()
        {
            AutoFight fight = Idle();
            Assert.AreEqual(AutoFightOrder.Return, fight.Decide(4f, 7f, 3f, 2f), "enemy is 7 from the anchor, leash is 6");
            Assert.AreEqual(AutoFightOrder.Hold, fight.Decide(null, 0f, 3f, 0.2f), "nothing to fight and already home");
        }
    }
}
