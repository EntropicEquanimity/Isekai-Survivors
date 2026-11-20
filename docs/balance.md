# Balance pass (ticket #35)

How long a Region takes, how often a Run is won, and the numbers that were changed to get there. Everything below was measured
by the autoplay bot (ticket #32) playing real Runs through the game's own flow; how to repeat it is in
[playtest-build.md](playtest-build.md).

## Targets

From the spec and ticket #35:

- the outer Region takes roughly **10 to 20 game minutes** for the balanced bot;
- **every strategy can win the outer ring** (stealth, rush, balanced, from either Anchor);
- deeper rings are harder but beatable, and a careless Run can be lost;
- tuned numbers live in data and tuning files (`Rules/*Tuning.cs`, `RingRecipe`, prefab stats), not in code that decides things.

## Method

1. Make the bot a fair measuring tool first (below), because a bot that dies in two minutes measures nothing.
2. Play a batch: seeds 1 to 3 (or 1 to 4), both Anchors, all three strategies, speed 8, the game's own Planet
   (rings 6, 4, 3 and the Capital). One Run is one Planet from the first Region until it is lost, timed out or won; every Region
   it captures adds a line to the report.
3. Read the numbers from `run-*.json` (per Region: outcome, game seconds, deaths, highest Vigilance, Waves, Portals built, Souls
   earned), and for the first Regions a verbose log, which has a status line every 30 game seconds (Vigilance stage, Souls, demons,
   mortals at the Seat, the keep's health, what the demons nearest the keep are doing and what they would hit).
4. Change one cause at a time and measure again. Batches that read an old build's numbers were thrown away.

The Region times below are game minutes of the first Region of the Run (the outer ring, depth 0).

## Before

Seeds 1 and 2, both Anchors, the bot as ticket #32 left it and the numbers as tickets #5 to #34 left them (12 Runs). "Outer Region" columns are the first Region of the Run; none was captured, so the minutes are how long Jack lasted or the Region took before the Run ended.

| Strategy | Anchor | Runs | Outer Region captured | Run won | Median outer Region, minutes (all / captured) | Regions captured per Run | Errors |
| --- | --- | --- | --- | --- | --- | --- | --- |
| stealth | Shrine | 2 | 0 of 2 | 0 | 28.4 / - | 0.0 | 0 |
| stealth | WarThrone | 2 | 0 of 2 | 0 | 30.9 / - | 0.0 | 0 |
| balanced | Shrine | 2 | 0 of 2 | 0 | 11.6 / - | 0.0 | 0 |
| balanced | WarThrone | 2 | 0 of 2 | 0 | 12.5 / - | 0.0 | 0 |
| rush | Shrine | 2 | 0 of 2 | 0 | 3.5 / - | 0.0 | 0 |
| rush | WarThrone | 2 | 0 of 2 | 0 | 5.8 / - | 0.0 | 0 |

No Run captured a single Region: every Run ended in Lost (Jack died with too few Souls to get back) or TimedOut (the Region
passed 40 game minutes: stealth parked at the Anchor for the whole time). The Vigilance of every Run was Crusade within 3 to 4
minutes, because Jack walked out for his first Souls and the Portals' demons killed everything that passed.

## What was wrong, and the fix

### The bot (Rules/Autoplay, Unity/Autoplay)

- Jack **waited wherever his last hunt had left him**, in the open, and a band found him. He now waits at the Anchor.
- He **walked to the Seat first**, alone, died there, and a death at the Seat is a wisp that drains faster than it can fly home: a lost
  Run. The demons now go first; Jack goes only when at least `JackJoinsAtDemons` of them are at the Seat and holding it (two for
  every mortal there), and only when the Souls in hand would see him through a death there: the flight home and the revive, or a
  possession beside the demons (`BotRules.CanSurviveDeathAtSeat`). A hurt Jack still goes then, since a revive makes him whole.
  Only waiting twice as long as the strategy meant to (`AssaultAfterSeconds`) sends him regardless.
- The **raid switched off** whenever a Portal fell, which sent the whole army home. Once begun it goes on while there are demons.
- All Portals raided, so a Wave found nothing at home. One Portal now stays on Guard (`HomeGuardPortals`).
- It built **four Portals at most**; it now builds `PortalsTotal` (4 to 5), which is where the pace of a Run comes from.
- A bodiless Jack went to the Anchor whenever he could pay the revive, even when the drain would have emptied his Souls on the
  way. It now compares what the flight costs: a demon beside him is the way back when the Anchor is too far
  (`BotRules.WhenBodiless`).
- Balanced destroys bone piles like stealth (a pile of five makes a mortal send a rider to the Seat, which is a full Vigilance
  stage).
- A batch can name **several Anchors and strategies** (`anchor=Shrine,WarThrone strategy=stealth,rush,balanced`), so one Unity
  start, which takes 3 to 4 minutes, plays them all.

### A bug the batches found: demons could not capture a Region

Twenty Imps stood against the Seat's keep for minutes with the keep at full health. An ability's range is measured to the middle of
its target, and a 3x3 keep keeps a melee demon (spear range 1.2) about 1.8 cells from its middle. The Spear Jab's range and the
demons' `_attackRange` are now 2.3 and 2.2 (`Data/Abilities/Spear Jab.asset`, the demon prefabs), with the PlayMode test
`Melee_demons_can_hurt_the_Seat_s_keep_which_is_too_wide_for_a_short_spear_to_reach_the_middle_of`. Before the fix only Jack,
whose weapons reach further, could take a keep.

The batches also checked the inventory of the Character from Region to Region and Run to Run (no Items duplicated, in every
report) and counted errors in the log (none).

## The tuned numbers

| Number | Where | Before | After | Why |
| --- | --- | --- | --- | --- |
| Starting Souls of a Region entered from the Planet map | `SoulTuning.RegionStartingSouls` (handed over by `RunSession.GoToRegion`, added by `RegionRun`) | 0 | 40 | a Jack who died before his first kills had no Souls to revive with, which lost the Run at once (one of the twelve first Runs ended 40 game seconds in); 40 is two Portals (5 each) and a revive (22) |
| Vigilance stages begin at | `VigilanceTuning.StageThresholds` | 0, 10, 25, 45, 70 | 0, 12, 30, 55, 90 | Crusade came 3 to 4 minutes into every Run, before the first Portal's demons had a target |
| Points for a kill, a witnessed kill, being spotted | `VigilanceTuning` | 4, 7, 12 | 2.5, 4, 8 | the same: 18 kills made Crusade, now 36 |
| Seat keep health, outer / middle / inner / Capital | `RingRecipe.KeepHealth` (x900) | 1, 1.5, 2, 2.5 | 2.5, 3.5, 4.5, 6 | once the demons could hit it, a Region fell in 4 to 6 minutes |
| Seat elite guard health, same order | `RingRecipe.EliteGuardStrength` | 1, 1.15, 1.3, 2 | 1.5, 1.7, 1.9, 2.4 | the same |
| Ruler health and damage, same order | `RingRecipe.RulerHealth`, `RulerDamage` | 1 and 1, 1.3 and 1.15, 1.6 and 1.3, 2.2 and 1.5 | 2 and 1.3, 2.6 and 1.5, 3.2 and 1.7, 4.4 and 2 | the same |
| Spear Jab range, demon attack range | `Spear Jab.asset`, demon prefabs | 1.2, 1.2 | 2.3, 2.2 | the bug above |
| Balanced bot: raid not before, raid with whatever there is after | `AutoplayStrategies.Balanced` | 240 s, 600 s | 390 s, 900 s | puts the balanced Region in the 10 to 20 minute band |

Left alone because the batches did not call for them: Portal costs (20 Stone, 5 Souls), 6 s and 4 demons per Portal,
Imp and mortal stats, Supply, soul level costs, day and night, the Wave sizes.

## After

Seeds 1 and 2, both Anchors, the final bot and numbers (12 Runs; the third seed was left out for time, see the intermediate batch for it).

| Strategy | Anchor | Runs | Outer Region captured | Run won | Median outer Region, minutes (all / captured) | Regions captured per Run | Errors |
| --- | --- | --- | --- | --- | --- | --- | --- |
| stealth | Shrine | 2 | 2 of 2 | 1 | 12.6 / 12.6 | 3.5 | 0 |
| stealth | WarThrone | 2 | 2 of 2 | 1 | 12.9 / 12.9 | 3.0 | 0 |
| balanced | Shrine | 2 | 2 of 2 | 1 | 11.1 / 11.1 | 3.0 | 0 |
| balanced | WarThrone | 2 | 2 of 2 | 2 | 11.0 / 11.0 | 4.0 | 0 |
| rush | Shrine | 2 | 2 of 2 | 0 | 7.9 / 7.9 | 2.0 | 0 |
| rush | WarThrone | 2 | 2 of 2 | 1 | 9.3 / 9.3 | 2.5 | 0 |

Median game minutes of a captured Region by ring, over all twelve Runs: outer (depth 0) 11.0 (7.1 to 13.7), middle 16.3
(6.2 to 22.4), inner 17.5 (8.4 to 22.8), Capital 24.0 (18.0 to 28.7). Six of the twelve Runs were won (the Capital taken after 48
to 81 game minutes in all), six were lost: one at the middle ring (rush), four at the inner ring (two rush, a balanced and a stealth) and one at the Capital (stealth). 0 errors were logged in any Run, the inventory was stable in every Run (no Items appeared between Regions or Runs),
`tunnelSamples` was 0, the frame time of the batch at speed 8 had a median of 6.2 ms and a worst 99th percentile of 16.5 ms.

The one batch in between, run after the demons could hit the keep and the Seat was made tougher but before the bot kept Jack home
until a death would not lose the Run (18 Runs, seeds 1 to 3, both Anchors):

| Strategy | Anchor | Runs | Outer Region captured | Run won | Median outer Region, minutes (all / captured) | Regions captured per Run | Errors |
| --- | --- | --- | --- | --- | --- | --- | --- |
| stealth | Shrine | 3 | 3 of 3 | 0 | 13.3 / 13.3 | 1.3 | 0 |
| stealth | WarThrone | 3 | 3 of 3 | 0 | 13.0 / 13.0 | 1.3 | 0 |
| balanced | Shrine | 3 | 3 of 3 | 0 | 9.9 / 9.9 | 2.0 | 0 |
| balanced | WarThrone | 3 | 3 of 3 | 0 | 10.5 / 10.5 | 1.7 | 0 |
| rush | Shrine | 3 | 2 of 3 | 0 | 7.0 / 6.4 | 0.7 | 0 |
| rush | WarThrone | 3 | 3 of 3 | 0 | 6.4 / 6.4 | 1.3 | 0 |

That batch is why the bot's rule for Jack changed: the Runs that were lost at the middle and inner rings were lost with
one or two deaths and a calm Vigilance, which is Jack dying far from home with too few Souls.

## The playthrough build

The Windows player (see [playtest-build.md](playtest-build.md)) was built from this branch and played headless by the bot: a Run
from the main menu's flow through the Planet screen and four Regions to the Capital and the victory screen (69 game minutes, balanced,
War Throne, seed 2), and one that was lost at the second Region (balanced, Shrine, seed 2); `Player.log` has no errors or exceptions
in either and the process exits 0. The tutorial and the credits are covered by their PlayMode tests (the bot does not drive menus).

## Known balance risks

- **Jack never heals.** Only a revive makes him whole, and a revive needs Souls and a Wisp that gets home. A Jack who dies at the Seat
  (190 cells from the Anchor at speed 6, draining 2 to 9 Souls a second after a few deaths, doubled near priests) needs about 160
  Souls to survive it, or 110 to possess a demon beside him. This cliff is what loses most of the Runs the bot loses.
  A little regeneration at the Anchor would soften it; it is not in this ticket.
- **Demons are free.** A Portal costs 20 Stone and 5 Souls and then makes Imps for nothing, so the pace of a Region is the number of
  Portals and the Seat's toughness (the rush bot builds 4 and takes the outer Region in 8 to 9 minutes, below the target band).
  A cost per demon would be a mechanic, not a number.
- **Demons march in a straight line** (the motor's default path provider): the demons can jam on terrain between the Anchor and
  the Seat, and the batch seeds had clear enough routes. A new generator seed may not.
- **Raids raise Vigilance fast.** Kills at the Seat, spotted demons and bone piles near witnesses bring Crusade (a Wave of 14 at the
  Anchor) a few minutes into the raid; the bot keeps one Portal at home for it, which is not always enough.
- **The bot is not a player.** It takes the first Item offered, never uses Jack's active weapons beyond auto-fight, never raises a
  Demon King or another Anchor and never loots. Win rates for people may be higher (Items, kiting) or lower (no pause to count Souls).
- **Seeds.** Two seeds per cell is a small sample; the intermediate batch (three seeds) and the first (two) agree on the shape,
  not on every number. A further batch before a release is cheap: see [playtest-build.md](playtest-build.md).
- **Stone.** A bot with no Portal left and no Stone node in reach stops (it timed out a Region at 40 game minutes in an early batch);
  workers mine, and the bot summons one or two, but a Region that runs out of Stone nodes could be unwinnable.
- **Later content.** Orcs, deeper heroes and the Soul paths (all out of scope in the spec) will change the numbers above.
