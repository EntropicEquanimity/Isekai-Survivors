# Plan: Isekai Conquerors on BlondieUtils

Supersedes `blondieutils-migration.md`. That plan moved the *survivors* game onto BlondieUtils (BU). This one changes the game itself, so most of the survivors code will be replaced, not ported.

## The game in one paragraph

You land on a Planet as your Character with one Main Base. The Planet is split into Regions. You pick which neighbouring Region to take next. Inside a Region you mine, build, and capture or place Bases while thousands of enemies roam, and Waves hit your Bases on a timer. Buildings spawn defenders that fight on their own based on a Stance. The Character fights on their own when you are not steering them. Taking a Region gives Items, Resources and a Region buff. Taking the Capital wins the Run. The longer the Run lasts, the stronger the enemies. Losing the Main Base loses the Run. Losing the Character does not. Between Runs, meta upgrades carry over (more Cloning Pods, rebuildable pods, and so on). Inside a Run, Items go into a limited Active inventory, which grows through Items and buffs, plus a Storage area. Each Item has its own rules for how long it lasts and when it can be swapped out.

Genre neighbours: They Are Billions (horde RTS), a survivors-like (auto-fighting hero), Magicraft (inventory that grows), Slay the Spire / FTL (choosing a route across a map).

## Proposed words (to go into `CONTEXT.md` once agreed)

| Term | Meaning | Note |
|---|---|---|
| **Planet** | One Run's whole map, made of Regions | Replaces **Level** |
| **Region** | One section of a Planet: a playable map with its own difficulty, Resources and capture reward | |
| **Capital** | The Region at the centre of the Planet. Capturing it wins the Run | |
| **Capture** | Taking a Region: destroy every enemy Base in it and defeat its final Wave | |
| **Character** | The unit the player controls. Mines faster than Peons, auto-fights when idle | Kept from `CONTEXT.md` (avoid "hero") |
| **Base** | A building that claims ground. You start with the **Main Base** | |
| **Cloning Pod** | Part of the Main Base. It brings the Character back. You start with one | |
| **Building** | Anything placed: Bases, producers, extractors, defences | |
| **Unit** / **Peon** | A unit spawned by a Building. Peons gather, the others defend | |
| **Stance** | A setting on a Building or Unit that decides how its Units engage (hold, guard area, hunt) | |
| **Wave** | A timed attack on your Bases. The existing Wave Types (Horde, Flight, Squad, Mix) still fit | |
| **Item** | Anything in the inventory: augments, perks, boons, weapons, accessories | |
| **Active inventory** / **Storage** | Items in Active take effect. Items in Storage only sit there | |
| **Lifetime** | Region (gone after this Region) or Run (kept until the Run ends) | |
| **Lock** | Can't be removed until the Region is done. **Swap Cooldown** means it can be removed again after N seconds | |

## Packages

The pin moves from `0bf4618` to the current BU main, `b275a07`.

| Package | Role in this game | Asked for? |
|---|---|---|
| `pixelperfect` | Already installed. Pixel camera, which the zoom levels must respect (whole-number zoom steps) | yes |
| `gamesetup` | Boot, main menu, pause, settings, credits, scene transitions. Brings in `ui`, `input`, `localization`, `settings`, `juice` and `save` | yes |
| `roguelike` | `Run` (seed, random streams, currencies, luck), `RunMeta` (meta currency, unlocks, run history), Boons (Region buffs), Drafts (pick 1 of N Items after a capture), Shop if wanted | yes |
| `inventory` + **`gridinventory`** | Active inventory = an **active grid**, Storage = a storage grid. Bag growth = `ResizeGrid` / blocked cells opened by Items. Synergies and triggers come free | yes (grid is decision 2) |
| `juice` | Hit flash, hit stop, shake, pop/squash, transitions | yes |
| `flowfields` | The horde. One field per target set (all player Buildings), with a Burst `FlowFieldCrowd` moving thousands of enemies. Recomputes when Buildings are placed | yes ("thousands of enemies") |
| `controllers` | The closest thing to "Strategy": `TopDownMotor2D` + `PlayerBrain` for the Character, `UnitSelection` / `SelectableUnit` / formations / move orders for the light RTS layer, `FollowCamera2D`, Square Grid for Building placement | **"Strategy" does not exist in BU (decision 1)** |
| `stats` | Character, Unit, enemy and Building stats, Item and buff modifiers, Health vitals | add |
| `combat` | `Damageable`, teams, the damage formula (armor, crit), statuses | add |
| `abilities` | Character weapons and tower attacks as `AbilityDefinition` + `AutoCaster` (the Character's auto-fight) | add |
| `pooling` | Enemies, projectiles, pickups and VFX. Required at this scale | add |
| `pixelfx` | Death, hit, capture and build effects | add |
| `skilltrees` | The meta upgrade tree between Runs (pods, starting Items, Region perks). `RunMeta` holds the currency, the tree spends it | add (decision 7) |
| `dialogue` | "Special events": Slay the Spire-style choice events that give Items | optional |
| `achievements`, `analytics` | Later | later |

Skip: `debugtools` (its Escape key clashes with gamesetup), `utils3d`, the online packages, `quiz`, `giftkit`, `hiddenobject`.

## What BU does not have, so the game builds it (or BU gets it)

1. **Targeting at thousands of units.** `PhysicsAbilityWorld` does a Physics2D overlap per query, which won't hold up with 2–5k enemies, hundreds of defenders and towers. Plan: a spatial hash that the crowd fills each frame, behind a custom `IAbilityWorld`. *Good upstream candidate.*
2. **Building placement and construction.** Grid footprint, valid-ground check, build time, a construction site that Peons or the Character finish, and a `SetCostRect` update to the flow field. *Upstream candidate: a new `com.blondie.strategy`, if that is what decision 1 means.*
3. **Economy.** Resource nodes, mining by Character and Peons, carry-and-return or tick income, stockpiles, Building costs.
4. **Unit production and Stances.** Buildings spawn Units on a timer up to a cap. Units follow the Stance on their Building (leash radius, hunt range).
5. **Wave director.** Timed Waves aimed at your Bases, plus roaming enemies the whole time. Strength scales with the Run clock and Region distance from the Capital. The existing `LevelSO` wave data and Wave Types carry over as data.
6. **Region capture and the Planet.** The Region graph (which Region touches which, distance to the Capital), choosing the next Region, the zoomable Planet map, and Region rewards. BU's `RunMaps` builds Slay the Spire-style layered paths. That doesn't match a map of touching Regions, so the graph is game code. `Run`, `Boons` and `Drafts` still do the rest.
7. **Item rules on top of the grid inventory.** Lifetime (Region/Run), Lock until Region done, Swap Cooldown, "cannot move once placed". Built as a rule the grid asks before it lets an Item leave the active grid. *Upstream candidate: a removal filter / lock on `ItemGrid`, since BU already has `Filter` for placing.*
8. **Zoom.** The scroll wheel steps through whole-number pixel zooms inside a Region, then zooms out to the Planet map. This needs a check of how `PixelCamera` handles zoom (BU change if it can't).
9. **Lose and win.** Main Base destroyed means a loss. Character death means respawning through a Cloning Pod, if one is left.
10. **Fog of war**, if wanted (decision 8).

## What carries over from the survivors code

- All art, enemy sprites, the tilemaps and the Green Pastures map (as the first Region).
- The enemy Roles (Chaser, Exploder, Flyer, Ranged, Buffer, Summoner, Hoarder, Elite) and the Wave Types.
- The 8 weapons, rebuilt as Character abilities, as the old plan's Phase 4 table mapped them.
- Knight, Samurai and Rogue as the first Characters.
- Nothing structural. `GameManager`, `ObjectPool`, `InventoryController` and the rest are replaced. As in the old plan: new code goes in `Assets/Game` (`Isekai` namespace, own asmdef plus tests), and the old code is deleted at the end.

## Phases

Each phase ends with something playable and its own commit. The order puts the biggest risk (thousands of units) first.

0. **Prepare.** ✅ Done 2026-11-18 on branch `conquerors`. BU pinned to `b275a07`, 22 BU packages + Input System 1.20.0, input handling *Both*, DOTween metas upgraded. GameSetup is switched off (`Assets/Resources/BlondieSettings/GameSetupSettings.asset`, `Enabled: 0`) so it does not cover the old menu; phase 1 turns it on. Old MainMenu and GameScene play with no errors.
1. **Skeleton.** ✅ Done 2026-11-18. `Isekai.Rules` (engine-free, `StatId`) and `Isekai.Game` (Unity) assemblies with Pure, EditMode and PlayMode tests in `Assets/Game`. GameSetup scenes `Boot`, `Menu` and `Region` in `Assets/Game/Scenes`, first in the build. GameSetup's `Enabled` stays off: the runner in `Boot`/`Menu` starts it, so the old scenes stay as they were. The old `SceneManager` loads its scenes by path. A PlayMode test covers Boot → Menu → Play → Region → Pause → Main Menu.
2. **Character in one Region.** ✅ Done 2026-11-18. The Samurai as `Character.prefab` (`TopDownMotor2D` + `PlayerBrain`, `StatBlock`, `Damageable`, Shortbow `AbilityAsset` on an `AutoCaster`). `CharacterControl` hands control to the Character after 1.5 s idle (`AutoFight`: hold, approach within a 6 m leash, return). `ScrollZoom` steps `PixelCamera`'s reference resolution in whole scales (`ZoomLevels`). Holding Interact (E) by a `ResourceNode` mines at MiningSpeed into `RegionEconomy`'s `Stockpile` (`MiningProgress`), shown by a placeholder `StockpileHud`. The Region scene has Green Pastures, three Stone nodes and five training Skeletons. Target finding is a physics query for now; phase 3 replaces it.
3. **The horde (risk spike).** Pooled enemies on a `FlowFieldCrowd` heading for a dummy Building, the spatial-hash `IAbilityWorld`, contact and melee damage. *Done when* 3,000 enemies run at 60 fps in a build and the Character can cut through them. If this fails, the design changes here, not in phase 8.
4. **Base and Buildings.** Main Base + Cloning Pod, placement grid, construction, Resource costs, one producer that spawns defenders with a Stance, one tower. Losing the Main Base ends the Run, losing the Character respawns them.
5. **Waves and pressure.** Wave director with a Run clock, escalation, roaming enemies, a Region capture condition.
6. **Inventory.** Active and Storage grids, bag-growth Items, Lifetime/Lock/Swap Cooldown rules, Items from capture drafts. About 10 Items to prove the rules.
7. **Planet and Run.** Region graph, picking a Region, Region buffs (Boons), distance-scaled difficulty, the Capital as the win, the Planet map zoom. A full Run from landing to Capital.
8. **Meta.** `RunMeta` currency from Runs, a skill tree with extra Cloning Pods / rebuildable pods / starting Items. Saved.
9. **Feel and UI.** HUD (Resources, Wave timer, Region progress), Juice, PixelFx, inventory window styling, results screen.
10. **Clean up and rename.** Delete `Assets/Scripts`, the old scenes, Odin and ConsolePro. Input System only. Rename to Isekai Conquerors (product name, `CONTEXT.md`, repo if wanted). Windows build.

## Decisions

Settled (2026-11-18):

1. **"Strategy" package:** use `controllers`' RTS pieces now. Grow a `com.blondie.strategy` in BU out of the generic parts (placement, construction, production, Stances, spatial hash) once they work here.
2. **Inventory shape:** grid (Tetris) via `gridinventory`.
3. **Word for the player's unit:** keep **Character**, as `CONTEXT.md` already says.
4. **Rename:** at the end (phase 10).
5. **Rebuild alongside, then delete the old code:** yes.
6. **Region capture:** destroy every enemy Base in the Region and defeat its final Wave.
7. **Issue #1 (Expedition Extraction Loop):** closed as superseded.

Still open (recommended answers in bold):

8. **Meta progression shape.** **A `skilltrees` tree bought with `RunMeta` currency**, or a flat `RunMeta` unlock list.
9. **Fog of war.** **Not in the first slice**, add it if the exploring feels flat.

## Risks

- **Scale.** Thousands of GameObjects with SpriteRenderers, plus targeting. Phase 3 exists to prove it early. The fallback is GPU-instanced sprites with no GameObject per enemy, which is a much bigger change.
- **Pixel-perfect zoom.** Smooth scroll zoom fights whole-number upscaling. Expect zoom in whole steps, or a BU change.
- **Scope.** This is a new game, not a port. Phases 3–7 are most of the work. Cut the slice to one Region, one Building type and five Items before adding breadth.
- **Private BU repo.** Git installs need credentials on every machine, same as today.
