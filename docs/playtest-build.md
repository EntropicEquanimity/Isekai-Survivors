# Playtest build and autoplay batches

How to build a Windows player of the whole game and how to play it, or the Editor, with the autoplay bot (ticket #32) to check
a build or measure balance (ticket #35, [balance.md](balance.md)).

## Build the Windows player

The build script is `Assets/Dev/Editor/BuildPlayer.cs`. It builds the enabled scenes of the Build Settings (Boot, Menu, Planet,
Region, in that order; `BuildScenesTests` checks them) into `Builds/Windows/IsekaiConquerors.exe`. `Builds/` is ignored by git.

With the project closed in the Editor (one Unity per project):

```
"C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe" -batchmode -projectPath <project> -logFile build.log ^
    -executeMethod Isekai.Dev.Editor.BuildPlayer.Windows
```

It exits 0 when the build succeeded and 1 when it did not; the log has a `BuildPlayer: Succeeded, 0 errors, 9 warnings, 135 MB`
line. From the Editor it is the menu item Tools / Isekai / Dev / Build / Windows player. A build takes 1 to 3 minutes (after Unity's
3 to 4 minutes of start-up) and is 135 MB. The nine warnings are Unity's own (shader and obsolete API notes in the packages, and
`FindObjectsByType` / `AppDomain.GetAssemblies` in `RegionDayNight` and `RegionBootstrap`); there are no errors.

## Autoplay batches

The bot plays whole Runs through the game's own flow (Planet screen, Region, end screen, a new Run) and writes a JSON report per
Run, a CSV row per Run and a summary per batch. Options after `-autoplay` are `key=value` words:

| Option | Meaning |
| --- | --- |
| `runs=N` | Runs to play (default: one per seed, times the Anchors and strategies listed) |
| `seeds=a,b,c` | Planet seeds, used in turn |
| `anchor=Shrine,WarThrone` | one or more Anchors; every pairing is played, on every seed |
| `strategy=stealth,rush,balanced` | one or more strategies, as above |
| `speed=8` | game speed (capped at 16, eased when frames are long) |
| `rings=6,4,3` | Planet ring sizes (default: the game's own) |
| `regioncap=2400` | game seconds one Region may take before the Run is given up as timed out |
| `verbose=true` | log what the bot decides, a status line every 30 game seconds and what kills Jack |
| `out=<folder>` | where reports go (default: `Logs/Autoplay` in the project; `persistentDataPath/Logs/Autoplay` in a build) |

The process exits 0 for a clean batch, 2 when errors were logged or an Item inventory leaked between Regions or Runs, and 3 when
a Run stalled.

### In the Editor, from the command line

Take a Unity slot first if other Unity runs share the machine (`mkdir D:/GameDev/.unity-slots/1` or `2`, remove it afterwards), then:

```
Unity.exe -batchmode -projectPath <project> -logFile batch.log -executeMethod Isekai.Dev.Editor.AutoplayMenu.RunBatch ^
    -autoplay seeds=1,2,3 anchor=Shrine,WarThrone strategy=stealth,rush,balanced speed=8 out=<folder>
```

A Run takes about 1 to 2 real minutes at speed 8 (a Run timed out at `regioncap` takes 5), and Unity needs 3 to 4 minutes to start
and import. Read the results with a few lines of script over `<folder>/run-*.json` (see the table in [balance.md](balance.md)).

### In the built player

```
Builds\Windows\IsekaiConquerors.exe -batchmode -nographics -logFile player.log -autoplay runs=1 seeds=7 anchor=Shrine strategy=balanced speed=8
```

`-batchmode -nographics` renders nothing and still plays the whole Run (the game's screen transitions run on clocks, not on
what is drawn); leave them out to watch it in a window. Reports go to
`%USERPROFILE%\AppData\LocalLow\<company>\<product>\Logs\Autoplay`. Read `player.log` for lines that start with `[Autoplay]`
and for errors (`grep -i "error\|exception" player.log`).

### What was checked in the build

Built from this branch and played with `-batchmode -nographics -autoplay` (the game runs headless, no window):

| Batch | Result | `Player.log` |
| --- | --- | --- |
| balanced, Shrine, seed 2 | Lost at the second Region after 44 game minutes, one Region captured, back at the main menu | 0 lines with `error`, `exception` or `assert`; exit 0 |
| balanced, WarThrone, seed 2 | **Won**: four Regions captured, the Capital taken after 69 game minutes (about 9 real minutes), the victory screen, back at the main menu | 0 lines with `error`, `exception` or `assert`; exit 0 |

Both reports have `errorsLogged` 0, a stable inventory and a frame time with a 99th percentile of 1.6 to 2.3 ms (a build without
rendering). The only lines in `Player.log` that look like complaints are the video decoder's missing `Hidden/VideoDecode`
material notes at start-up and the `BlondieJuiceTransitionsPanelSettings` theme note, which the player prints without a window.

Playing the build also found a bug in the autoplay runner, fixed in this ticket: the game loads scenes behind a screen
transition and ignores a scene asked for while one is running; the Editor starts on the Planet scene and has none. The runner asked
for the Region while the Planet screen was still uncovering, so the Region never came (`Highspire never finished loading`).
`AutoplayFlow.MayAskForScene` now makes it wait (a Pure test), and the runner asks again for a scene that does not come.

The tutorial and the credits are menu screens the bot does not drive; the smoke tour below plays them in the build. They are
also covered by the PlayMode tests (`TutorialPlayModeTests`, `MenuFlowTests`, `CreditsPlayModeTests`, `SmokeTourPlayModeTests`).

## The smoke tour (`-smoke`)

`-autoplay` skips the menus, so a second command line walks the real front end. `SmokeTour` (`Assets/Game/Unity/Autoplay/SmokeTour.cs`)
waits for the main menu, presses Tutorial, lets the tutorial Region run, opens the pause menu and leaves by its Main Menu button
(answering the question), presses Credits, closes them with Back, and quits. Saves go to memory, so your own Run is untouched.

```
Builds\Windows\IsekaiConquerors.exe -batchmode -nographics -logFile tour.log -smoke seconds=30 speed=4
```

`seconds` is the real time the tutorial Region runs (default 20) and `speed` the game speed while it does (default 4, at most 16).
`[Smoke] Step: ...` lines in the log name each step reached. Exit code: 0 for a clean tour, 2 when errors were logged, 3 when a
step never came (the log says which, after 60 s). The tour does not play the tutorial's steps; it lets the Region run.
`SmokeTourPlayModeTests` covers the same path in the Editor.

### What was checked with the smoke tour

Built with `-executeMethod Isekai.Dev.Editor.BuildPlayer.Windows` (`BuildPlayer: Succeeded, 0 errors, 9 warnings, 135 MB, 92 s`) and
run with `-batchmode -nographics -smoke seconds=30 speed=4` (no window was needed; the UI runs without rendering): 55 s in all,
all seven steps (main menu, tutorial Region, ran 30 s, pause menu, back at the main menu, credits, credits closed), exit 0,
`[Smoke] Tour done: 7 steps, 0 errors logged, no stall, exit 0.`, and no line with `error`, `exception` or `assert` in the log
other than that summary line.

The same build then played one full Run (`-batchmode -nographics -autoplay runs=1 seeds=2 anchor=WarThrone strategy=balanced speed=8`):
**Won**, four Regions captured and the Capital taken (4 of 14 Regions on this seed's Planet), 521 s (about 9 minutes) real time, exit 0,
`errorsLogged` 0, inventory stable, and 0 lines with `error`, `exception` or `assert` in the log. Together the tour and the Run
cover the main menu, the tutorial, a full Run to the Capital, the end screen back to the menu, and the credits.

## What a clean batch looks like

- exit code 0, `errorsLogged` 0 in every Run's report and the summary;
- `inventory.stable` true in every report (the Character brings the Run's Items from Region to Region, no more and no fewer);
- no `Stalled` Runs;
- `tunnelSamples` 0 (Jack's body is always on walkable ground).
