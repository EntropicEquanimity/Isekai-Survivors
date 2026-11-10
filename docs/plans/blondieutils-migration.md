# Plan: move Isekai Survivors onto BlondieUtils

Goal: replace the hand-rolled architecture in `Assets/Scripts` with BlondieUtils (BU) packages, keeping the art, enemy roster, weapons and Levels, and end with a game that plays at least as well as today.

## Where we start

- About 60 scripts in the global namespace with no asmdef. Six never-reset `Instance` singletons, and a `GameManager` god object holding about 25 global `EntityStat`s that every weapon reads directly.
- Old Input Manager, uGUI + TextMeshPro, Built-in render pipeline, Odin attributes in almost every file, DOTween, AllIn1SpriteShader.
- Four separate ad-hoc pools. Enemies are `Instantiate`/`Destroy`d and leak on wave change.
- No save system. `SettingsController` isn't wired up. Runtime state lives in mutated ScriptableObject assets.
- Known bugs that should **not** be carried over:
  - Crit damage is ignored (always x2).
  - `XpGain` and `GoldGain` return `pickupRadius`.
  - Mirror adds 0 projectiles.
  - The wave index runs out of range after the last wave.
  - `using UnityEditor` breaks player builds.
  - Equipment can never be offered (the Catalogue only lists weapons).

The old code is too entangled through `GameManager.Instance` to swap one system at a time. So the plan is a **parallel rebuild**:
- New code goes in a new assembly and a new scene, and reuses the existing sprites, prefabs' visuals and data.
- The old scene keeps working until the new one reaches parity.
- Then the old code is deleted in one go.

## Decisions (recommended answers in bold)

1. **Rebuild alongside, then delete the old code** — or delete `Assets/Scripts` first and rebuild from an empty project.
2. **Adopt URP + `com.blondie.pixelperfect`.**
   - BU's pixel UI rules and PixelFx both assume it.
   - The game is already pixel art (PPU 32, 640×360).
   - AllIn1SpriteShader has URP support.
   - The cost: one pipeline conversion, plus checking every material.
3. **Horde movement: `com.blondie.flowfields` `FlowFieldCrowd`.**
   - Green Pastures is a bounded tilemap with an Obstacles layer, which is exactly what `BakeFromPhysics2D` wants.
   - It gives Burst movement with separation.
   - The alternative is a `FollowBrain` per enemy, which is simpler but heavier and has no separation.
4. **Generic gaps go upstream into BU, game-specific ones stay here.** BU's AGENTS.md says cleaner architecture wins, and several gaps are reusable. Candidates for BU:
   - Orbit, boomerang, bounce and follow-caster area deliveries (abilities).
   - A pooled, game-clock `FloatingText` (ui).
   - Kinematic-aware knockback (combat).
   - An option for `DraftWindow` to pause the `GameClock` (roguelike).
   - Maybe a run-timer spawn director and magnet pickups.
5. **Drop Odin Inspector** from game code. BU doesn't use it, and it's a paid hard dependency. Keep DOTween (BU requires it) and AllIn1SpriteShader (visuals only).
6. **Pin BU by commit hash**, since the repo has no tags. For example, `…BlondieUtils.git?path=/Packages/com.blondie.core#0bf4618`. Bump on purpose.

## Packages to install

| Install | Why |
|---|---|
| `core`, `utils`, `utils2d` | Foundation, `EventCenter`, singletons, `GameClock`, formatting, sprite animator, camera shake |
| `stats` | Player and enemy stats, the passives as permanent modifiers, `Health` vital |
| `combat` (via abilities) | `Damageable`, `Team`, `DamageRules` (armor, crit), status effects (freeze), knockback |
| `abilities` | Weapons as `AbilityDefinition` + `AutoCaster`; projectile and area deliveries |
| `pooling` | Enemies, projectiles, gems and VFX through `Pools.Spawn/Despawn`, also used by ability visuals |
| `controllers` | `TopDownMotor2D` + `PlayerBrain` for the player, `FollowCamera2D` |
| `flowfields` | Horde movement (pulls in Burst, Collections, Mathematics) |
| `roguelike` | `Run`, seeded random, luck, `Offerable` + `RunDrafts` level-up draft, `RunMeta` for meta upgrades and character unlocks |
| `gamesetup` | Boot / MainMenu / Game scenes, pause, settings, credits, transitions. It also brings in ui, input, localization, settings, juice and save |
| `pixelperfect`, `pixelfx` | `PixelCamera`, pixel font, hit, death and pickup VFX (if decision 2 is yes) |

Skip:
- `inventory` and `gridinventory`: the wrong model for survivors passives.
- `debugtools`: its Esc pause clashes with gamesetup.
- `dialogue` and the online packages: not needed.
- `analytics` and `achievements`: later, optional.

## What the game still builds itself

BU has nothing for these:
- **XP and level-up:** XP gems (pooled, magnet pull, merging), the level curve and the level-up trigger.
- **Wave director:** keyed on run time, driven by the existing `LevelSO` wave data, with off-screen spawn rings, bosses and despawn/recycle of far-away enemies.
- **Contact damage** from enemies.
- **Weapon level tables as data** (today they're hardcoded per class), and mapping the Amount, Area, Duration and Magnet stats onto deliveries and pickups.
- **HUD:** XP bar, timer, level, kills, weapon and passive slots. Built in UI Toolkit from `SmoothBar` and friends.
- **Screens:** game over / results with per-weapon damage share, and character select.
- **Run end:** the run timer and the win condition.

## Phases

Each phase ends with something playable or testable, and its own commit.

### Phase 0 — Prepare the project
- Work on a branch, e.g. `bu-migration`.
- Install the Input System and set Active Input Handling to *Both*. It stays on *Both* until the old code is deleted.
- Convert to URP: renderer asset, 2D renderer, material upgrade. Check the AllIn1 materials and the tilemap.
- Run *Setup DOTween* again on Unity 6000.6.
- Install the BU packages from the table, pinned to a commit. Create the settings assets through `Tools/Blondie/<Name>/Create Settings Asset` only where defaults need changing.
- **Done when:** the old GameScene still plays on URP with BU installed and nothing using it yet.

### Phase 1 — Game skeleton
- Create `Assets/Game/` with an `Isekai.Game` asmdef (plus `Isekai.Game.Tests` Pure/EditMode/PlayMode) referencing the BU `Blondie.X` and `Blondie.X.Unity` assemblies. Use the `Isekai` namespace.
- Declare the stat ids once in a `[StatIdEnum]` enum:
  - Health, Speed, Armor, DamageBonus, CritChance, CritMultiplier
  - Area, Amount, Duration, ProjectileSpeed, Cooldown
  - PickupRadius, Luck, XpGain, GoldGain, Revives
- Run *Tools → Blondie → GameSetup → Create Game Scenes* for Boot, MainMenu and Game. The new Game scene reuses the Green Pastures Grid and tilemaps.
- **Done when:** Boot → MainMenu → Game loads, and pause and settings work.

### Phase 2 — Player
- Character data: either convert `PlayerSO` (Knight, Rogue, Samurai) into a new `CharacterDefinition` SO (sprites, base stats, starting weapon), or keep `PlayerSO` and adapt it.
- The player prefab gets:
  - `TopDownMotor2D` + `PlayerBrain` reading `GameInput.MoveVector`
  - `StatBlock` with a Health vital
  - `Damageable` + `Team`
  - the idle/walk flipbook through `SpriteFrameAnimator`
- Camera: `FollowCamera2D` + `PixelCamera`, which replaces the camera parented inside each character prefab.
- **Done when:** you can pick a character and walk around Green Pastures.

### Phase 3 — Enemies and waves
- Enemy prefab: `StatBlock`, `Damageable` (`DespawnOnDeath` through pooling), `Team`, a trigger collider for ability hits, and a `ContactDamage` component on a game-clock cooldown.
- Movement: one `FlowFieldCrowd` following a field toward the player, baked from the Obstacles tilemap. `UnitCircleCombatSpawner`'s ring formation becomes a director spawn pattern.
- `WaveDirector`: reads `LevelSO.enemyWaves` and spawns with a correct weighted pick, through `Pools.Spawn` at off-screen positions around the player. Enemies far from the player are recycled. The last wave is clamped, and the win fires at the end of the timer.
  - Pure tests cover wave selection, the weighted pick and spawn counts over time.
- Exploders: delayed `AreaDelivery` / `DamageEffect` on death or on contact. Elite: a flag with extra XP.
- **Done when:** waves chase and hurt the player and die when hit by a debug ability.

### Phase 4 — Weapons and passives

Each weapon is an `AbilityDefinition` on the player's `AbilityCaster`, fired by `AutoCaster`. Level tables live in data, not code.

| Weapon | BU mapping |
|---|---|
| Shortbow | `ProjectileDelivery` at the nearest enemy |
| Sai | `ProjectileDelivery`, `Count` 2, `Spread` |
| Pirate Cutlass | `AreaDelivery` Cone, centred on the caster, aimed at the nearest enemy |
| Rapier | `AreaDelivery` Line; the optional lunge is a dash |
| Ice Wand | **custom bounce delivery** + a Freeze status (`Rooted`/`Stunned` tag) |
| Throwing Axe | **custom boomerang delivery** |
| Earth's Pulse | **custom follow-caster aura** (permanent, ticking) |
| Lantern | **custom follow-caster cone** that rotates with the facing direction |

| Passive | BU mapping |
|---|---|
| Magnet | `PickupRadius` flat modifier, read by the gem magnet |
| Duplication Mirror | `Amount` modifier, read by deliveries for `Count` |
| Iron Briefs | invulnerability buff on hit (`Damageable.InvulnerableAfterHit` or a `BuffAsset`) |

Also in this phase:
- Map the Area, Amount, Duration, Cooldown and ProjectileSpeed stats onto delivery fields at cast time, in one place.
- Damage, crit and armor come from `DamageRules`, which fixes the ignored crit multiplier.
- **Done when:** all 8 weapons and 3 passives work on a debug "give item" key, and their levels scale them.

### Phase 5 — XP, level-up and the run
- `XpGem`: pooled. It drops on enemy death, is magnet-pulled within `PickupRadius`, and merges past a count. Gains are scaled by `XpGain`.
- Level curve: keep the `AnimationCurve` shape, with Pure tests.
- `RunManager.StartRun` per Game scene. Weapons and passives become `Offerable` subclasses whose `OnTaken` learns or levels them up. They carry `MaxCopies` and requires/excludes, so you get max-level removal and future evolutions.
- Level-up: `RunDrafts.Present` with 3–4 choices, luck from the `Luck` stat, and rerolls and skip. Pause `GameClock.Default` while the `DraftWindow` is open. Rarity curves stay flat, or the game calls `EnterStage` per level.
- **Done when:** a full 10–11 minute Green Pastures run is playable, from start to win or death.

### Phase 6 — UI and feel
- HUD in UI Toolkit: XP bar, timer, level, kills, and weapon/passive slots with cooldown fill.
- Damage numbers: a pooled, game-clock floating text (in BU if decision 4, otherwise in game code), respecting the settings toggle.
- Game over / results: per-weapon damage and kills (`DamageTaken` events tallied by ability source). Restart / Main Menu.
- Character select screen in MainMenu, gated by `RunMeta` unlocks.
- Juice: `SpriteJuice.Flash` on hit, light `HitStop`, camera shake respecting the accessibility settings, and PixelFx for death, hit and pickup.
- **Done when:** the new scenes cover everything the old ones did.

### Phase 7 — Meta and save
- Gold from runs goes to `RunMeta` currency. Meta power-ups are `RunMeta.Buy` items applied as permanent stat modifiers at run start (this was the empty `UpgradeButton`). Character unlocks also go through `RunMeta`.
- Settings persist through gamesetup and settings, which replaces `SettingsController` and the mutated `SettingsSO`.
- **Done when:** progress and settings survive a restart.

### Phase 8 — Remove the old architecture
- Delete:
  - `Assets/Scripts`, the old scenes and the GameController/HUD/FadeCanvas prefabs
  - the old SO types that were replaced
  - Odin (`Assets/Plugins/Sirenix`) and ConsolePro
  - TextMesh Pro, if nothing uses it any more
- Set Active Input Handling to *Input System Package* only. Remove `ai.navigation` and other unused packages.
- Make a Windows player build to prove no editor-only code leaked in.
- Update `CONTEXT.md` with the new terms, and record the big decisions (URP, flowfields, BU-upstream policy) as ADRs.
- **Done when:** the project contains only `Assets/Game` plus art and data, the build runs, and all tests pass.

## Risks
- **The URP conversion** can break AllIn1 materials and lighting (Lantern). Check this first, in Phase 0.
- **Hit detection at scale:**
  - Every in-flight projectile does a Physics2D overlap per tick, against hundreds of trigger colliders.
  - Profile at 300–500 enemies in Phase 4.
  - Fallback: a spatial hash behind a custom `IAbilityWorld`.
- **Flowfield latency:** fields lag the moving player by a frame or two and recompute on movement. Fine for a horde, but watch for jitter.
- **Private repo:** UPM git installs need git credentials on every machine and CI that opens the project. They work on this machine today.
- **Scope:** Phases 4–6 are most of the work. If the BU-upstream route is taken, those changes land in the BU repo first, and the game bumps its pinned commit.
