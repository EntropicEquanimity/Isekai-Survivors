using System.Collections.Generic;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>Line of sight, sight ranges and fog memory over small hand-built grids ('#' blocks sight).</summary>
    public class VisionTests
    {
        private static VisionMap MapOf(params string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            // Rows are written top first; y counts up from the bottom.
            return new VisionMap(w, h, c => c.x >= 0 && c.y >= 0 && c.x < w && c.y < h && rows[h - 1 - c.y][c.x] == '#');
        }

        private static VisionMap Open(int w, int h) => new(w, h, null);

        private static Int2 At(int x, int y) => new(x, y);

        [Test]
        public void A_rock_between_two_cells_blocks_the_line_and_open_ground_does_not()
        {
            VisionMap map = MapOf(
                ".......",
                "...#...",
                ".......");
            bool Blocks(Int2 c) => c.x == 3 && c.y == 1;
            Assert.IsFalse(LineOfSight.Clear(At(0, 1), At(6, 1), Blocks), "the rock stands in the way");
            Assert.IsTrue(LineOfSight.Clear(At(0, 1), At(3, 1), Blocks), "the rock's own face is seen");
            Assert.IsTrue(LineOfSight.Clear(At(0, 0), At(6, 0), Blocks), "a clear row");
            Assert.IsTrue(LineOfSight.Clear(At(2, 2), At(2, 2), Blocks), "a cell sees itself");
            Assert.IsNotNull(map);
        }

        [Test]
        public void A_Building_is_seen_whole_but_still_hides_what_is_behind_it()
        {
            // A 3-wide Building in cells x 3..5 of row 1 (group 7), a rock at x 8; the viewer stands at x 0.
            bool Blocks(Int2 c) => c.y == 1 && ((c.x >= 3 && c.x <= 5) || c.x == 8);
            int GroupOf(Int2 c) => c.y == 1 && c.x >= 3 && c.x <= 5 ? 7 : 0;
            Assert.IsTrue(LineOfSight.Clear(At(0, 1), At(5, 1), Blocks, GroupOf), "its far side is seen through its near side");
            Assert.IsFalse(LineOfSight.Clear(At(0, 1), At(6, 1), Blocks, GroupOf), "the ground behind it stays hidden");
            Assert.IsFalse(LineOfSight.Clear(At(0, 1), At(8, 1), Blocks, GroupOf), "another blocker behind it stays hidden");
            Assert.IsFalse(LineOfSight.Clear(At(0, 1), At(5, 1), Blocks), "without groups only its front face shows");
        }

        [Test]
        public void Sight_is_the_same_both_ways_along_a_straight_line()
        {
            bool Blocks(Int2 c) => c.x == 3 && c.y == 1;
            Assert.AreEqual(LineOfSight.Clear(At(0, 1), At(6, 1), Blocks), LineOfSight.Clear(At(6, 1), At(0, 1), Blocks));
        }

        [Test]
        public void Daylight_ranges_are_Jack_14_demons_8_mortals_7_wildlife_6()
        {
            Assert.AreEqual(14, VisionRanges.For(UnitKind.Character));
            Assert.AreEqual(8, VisionRanges.For(UnitKind.Demon));
            Assert.AreEqual(7, VisionRanges.For(UnitKind.Mortal));
            Assert.AreEqual(6, VisionRanges.For(UnitKind.Wildlife));
            Assert.AreEqual(0, VisionRanges.For(UnitKind.Building), "a Building doesn't see");
            Assert.AreEqual(6, VisionRanges.ForBuilding(true), "the Anchor looks around itself 6 cells");
            Assert.AreEqual(4, VisionRanges.ForBuilding(false), "any other of your Buildings, 4");
        }

        [Test]
        public void A_range_multiplier_scales_every_range_and_a_viewer_always_sees_its_own_surroundings()
        {
            Assert.AreEqual(7, VisionRanges.Scaled(14, 0.5f));
            Assert.AreEqual(4, VisionRanges.Scaled(7, 0.5f));
            Assert.AreEqual(1, VisionRanges.Scaled(1, 0.1f));
            Assert.AreEqual(0, VisionRanges.Scaled(0, 2f));
        }

        [Test]
        public void A_viewer_sees_cells_within_its_range_and_no_further()
        {
            VisionMap map = Open(40, 40);
            map.Add(At(20, 20), 7);
            Assert.IsTrue(map.IsVisible(At(20, 20)));
            Assert.IsTrue(map.IsVisible(At(27, 20)), "7 cells east");
            Assert.IsFalse(map.IsVisible(At(28, 20)), "8 cells east");
            Assert.IsTrue(map.IsVisible(At(20, 13)), "7 cells south");
            Assert.IsFalse(map.IsVisible(At(26, 26)), "the corner of the square is out of a round range");
        }

        [Test]
        public void A_viewer_at_the_edge_of_the_map_sees_only_what_is_on_it()
        {
            VisionMap map = Open(10, 10);
            VisionViewer v = map.Add(At(0, 0), 14);
            Assert.AreEqual(100, v.Count);
        }

        [Test]
        public void A_wall_hides_the_ground_behind_it_but_not_beside_it()
        {
            VisionMap map = MapOf(
                ".........",
                ".........",
                "....#....",
                ".........",
                ".........");
            map.Add(At(1, 2), 8);
            Assert.IsTrue(map.IsVisible(At(4, 2)), "the wall itself");
            Assert.IsFalse(map.IsVisible(At(7, 2)), "behind the wall");
            Assert.IsTrue(map.IsVisible(At(7, 0)), "round the wall");
            Assert.AreEqual(FogState.Visible, map.StateOf(At(7, 4)));
        }

        [Test]
        public void Rocks_and_walls_block_sight_for_every_side_the_same_way()
        {
            var yours = new VisionMap(9, 1, c => c.x == 4);
            var mortals = new VisionMap(9, 1, c => c.x == 4);
            yours.Add(At(0, 0), 8);
            mortals.Add(At(8, 0), 8);
            Assert.IsFalse(yours.IsVisible(At(6, 0)));
            Assert.IsFalse(mortals.IsVisible(At(2, 0)));
        }

        [Test]
        public void Ground_is_unseen_then_visible_then_explored_and_stays_explored()
        {
            VisionMap map = Open(30, 5);
            Assert.AreEqual(FogState.Unseen, map.StateOf(At(10, 2)));
            VisionViewer v = map.Add(At(8, 2), 4);
            Assert.AreEqual(FogState.Visible, map.StateOf(At(10, 2)));
            map.Move(v, At(25, 2), 4);
            Assert.AreEqual(FogState.Explored, map.StateOf(At(10, 2)), "out of sight but remembered");
            Assert.AreEqual(FogState.Unseen, map.StateOf(At(17, 2)), "never seen");
            map.Move(v, At(8, 2), 4);
            Assert.AreEqual(FogState.Visible, map.StateOf(At(10, 2)));
            map.Remove(v);
            Assert.AreEqual(FogState.Explored, map.StateOf(At(10, 2)), "a viewer that is gone leaves what it saw explored");
        }

        [Test]
        public void Cells_seen_for_the_first_time_are_announced_once()
        {
            VisionMap map = Open(30, 5);
            var firsts = new List<Int2>();
            map.Explored += firsts.Add;
            VisionViewer v = map.Add(At(5, 2), 2);
            int first = firsts.Count;
            Assert.Greater(first, 0);
            map.Move(v, At(6, 2), 2);
            Assert.AreEqual(first + 5, firsts.Count, "only the cells just east of the old reach are new");
            map.Move(v, At(5, 2), 2);
            Assert.AreEqual(first + 5, firsts.Count, "going back reveals nothing new");
        }

        [Test]
        public void Moving_a_viewer_one_cell_changes_only_the_cells_that_enter_or_leave_its_sight()
        {
            VisionMap map = Open(60, 60);
            VisionViewer v = map.Add(At(30, 30), 8);
            int total = v.Count;
            var lit = new List<Int2>();
            var dark = new List<Int2>();
            map.VisibleChanged += (c, on) => (on ? lit : dark).Add(c);

            map.Move(v, At(31, 30), 8);

            Assert.AreEqual(total, v.Count, "the same disc, shifted");
            Assert.AreEqual(lit.Count, dark.Count, "as many cells enter as leave");
            Assert.Less(lit.Count + dark.Count, total / 2, "a step touches a thin crescent, not the whole disc");
            foreach (Int2 c in lit) Assert.IsFalse(Contains(30, 30, 8, c), "a lit cell was out of the old disc");
            foreach (Int2 c in dark) Assert.IsFalse(Contains(31, 30, 8, c), "a dark cell is out of the new disc");
        }

        private static bool Contains(int cx, int cy, int r, Int2 c) => (c.x - cx) * (c.x - cx) + (c.y - cy) * (c.y - cy) <= r * r;

        [Test]
        public void A_viewer_that_stays_in_its_cell_does_no_work()
        {
            VisionMap map = Open(20, 20);
            VisionViewer v = map.Add(At(10, 10), 5);
            int events = 0;
            map.VisibleChanged += (_, __) => events++;
            Assert.IsFalse(map.Move(v, At(10, 10), 5));
            Assert.AreEqual(0, events);
        }

        [Test]
        public void A_cell_two_viewers_see_stays_visible_until_both_look_away()
        {
            VisionMap map = Open(40, 5);
            VisionViewer a = map.Add(At(10, 2), 5);
            VisionViewer b = map.Add(At(14, 2), 5);
            Assert.IsTrue(map.IsVisible(At(12, 2)));
            map.Remove(a);
            Assert.IsTrue(map.IsVisible(At(12, 2)), "the other viewer still sees it");
            Assert.IsFalse(map.IsVisible(At(6, 2)), "only the first one saw this");
            map.Remove(b);
            Assert.IsFalse(map.IsVisible(At(12, 2)));
        }

        [Test]
        public void Many_viewers_on_one_cell_do_not_overflow_the_count()
        {
            VisionMap map = Open(10, 10);
            var viewers = new List<VisionViewer>();
            for (int i = 0; i < 400; i++) viewers.Add(map.Add(At(5, 5), 3));
            foreach (VisionViewer v in viewers) map.Remove(v);
            Assert.IsFalse(map.IsVisible(At(5, 5)));
        }

        [Test]
        public void Refreshing_a_viewer_picks_up_a_wall_that_went_up_or_a_rock_that_went_away()
        {
            bool wall = false;
            var map = new VisionMap(12, 1, c => wall && c.x == 4);
            VisionViewer v = map.Add(At(0, 0), 10);
            Assert.IsTrue(map.IsVisible(At(8, 0)));
            wall = true;
            map.Refresh(v);
            Assert.IsFalse(map.IsVisible(At(8, 0)), "the new wall hides what is behind it");
            Assert.AreEqual(FogState.Explored, map.StateOf(At(8, 0)));
            wall = false;
            map.Refresh(v);
            Assert.IsTrue(map.IsVisible(At(8, 0)));
        }

        [Test]
        public void A_halved_range_shrinks_what_a_viewer_sees()
        {
            VisionMap map = Open(40, 40);
            VisionViewer v = map.Add(At(20, 20), VisionRanges.Scaled(VisionRanges.Character, 1f));
            Assert.IsTrue(map.IsVisible(At(34, 20)));
            map.Move(v, At(20, 20), VisionRanges.Scaled(VisionRanges.Character, 0.5f));
            Assert.IsFalse(map.IsVisible(At(34, 20)), "out of the night range");
            Assert.AreEqual(FogState.Explored, map.StateOf(At(34, 20)));
            Assert.IsTrue(map.IsVisible(At(27, 20)));
        }
    }
}
