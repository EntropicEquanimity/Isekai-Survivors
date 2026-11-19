# Placeholder art brief: Isekai Conquerors

A brief for a separate Claude Code session that uses the **agent-sprite-forge** plugin to make placeholder pixel art. Paste everything below the line into that session.

---

You're making placeholder **pixel art** for a Unity game, using the agent-sprite-forge skills (`generate2dsprite`, `generate2dmap`, `generate2dmedia`). Pixel art only: no HD finish, no video and no character animation. The characters already exist (bought asset packs), so you're making the **world, buildings, resources and icons** around them.

## The game

Isekai Conquerors is a top-down horde RTS with a survivors-like hero. The player is Jack, isekai'd as a demon, who leads hordes of demons invading a planet region by region. The defenders are mortal factions: humans in villages, farms, towns and castles, plus orcs, beasts and others. Ground the demons take over becomes corrupted.

## Setup (do this first)

1. Work in `D:\GameDev\Isekai-Art-Forge\` (create it). Run the Forge tools from there so its `.forge/` ledger and work files stay out of the Unity project.
2. Before running any Forge Python script, set `PYTHONUTF8=1` (bash: `export PYTHONUTF8=1`; PowerShell: `$env:PYTHONUTF8=1`). Otherwise this console's encoding crashes its output.
3. Run the capability check (`forge_doctor.py`, from the `generate2dmedia` skill). Then confirm `route_media.py resolve --kind image` picks the **Grok (local CLI)** route. It must use the user's own signed-in Grok Build CLI, never an API key.
4. If there is no image route, **stop and tell the user**. Don't fall back to code-drawn art (`codeart2d`) unless the user says so.

## The style to match

Match these two packs exactly. Look at them before generating anything:
- Demons (the player's side): `D:\GameDev\Assets\16x16PixelArt\Tiny RPG Character Asset Pack 02 -Full 20 Characters\Characters(100x100 split)\`
- Mortals (the defenders): `D:\GameDev\Assets\16x16PixelArt\Tiny RPG Character Asset Pack v1.03 -Full 20 Characters\Characters(100x100)\`

Use them as style references for every generation, for example Demon_D, Warlock and Black Knight_B for the demon side, and Soldier, Knight and Wizard for the mortals. The rules:
- **Scale:** 16 px grid. One world cell is 16×16 px. Characters are 15–30 px tall, so a person stands about 1.5 cells tall.
- **View:** top-down three-quarter view, front-facing, light from the top left. Same perspective as the characters.
- **Lines:** a 1 px dark outline (`#1f1a1a`) around every object. Ground tiles have no outline.
- **Shading:** flat, 2–3 tones per material, chunky readable shapes and little detail. No anti-aliasing, no gradients, no dithering noise, hard binary alpha.
- **Palette:** at most 32 colours per sheet, taken from or close to the packs:
  - Demon side: `#000000 #1f1a1a #32272e #3d3037 #44373d #5b4b53 #7b6a6b #4d122f #6f1a29 #7e222a #972f34 #a22b4b #a6483a`, with ember orange accents.
  - Mortal side: `#1f1a1a #425c6b #607581 #6f828d #7f909a #9badb7 #c5d0d5 #b1a9a6 #d2cecc #2f5854 #3b7d4f #7d984b #a0c537`, plus warm wood browns and straw yellows.
- **Readability:** objects must stand out from the ground at a glance. Resources especially must never look like floor.

## What to make, in order

Start with a **style sample** and show it to the user for approval before the rest: one 16×16 grass tile plus its fully corrupted version, one Stone node and one Portal. Then make the rest.

1. **Ground tiles** (16×16, seamless):
   - Grassland: 4 plain variants, 2 with tufts or flowers, 1 dirt path.
   - Corruption: the same set at **5 levels**, from 0 (untouched) to 4 (fully converted). Corruption shifts toward dark crimson and purple soil with glowing ember cracks, and should feel gradual: level 1 is a light tint, level 4 is hellish ground.
   - Transition edges: grass ↔ dirt and untouched ↔ corrupted. Use a 16-tile blob or 47-tile autotile, whichever the map skill produces cleanly.
2. **Obstacles** (in cells; art may rise up to half a cell above its footprint): boulders 1×1 and 2×1, a dead tree 1×1, a bush 1×1, a cliff or rock-wall piece 1×1 that tiles.
3. **Resource nodes** (1×1, may rise a little above the cell): Stone deposit, iron ore, gold ore, a tree for wood. Give each a clear silhouette and a highlight so it reads as harvestable.
4. **Souls:** a floating soul wisp pickup (8×8 or 12×12, pale cyan-white with a dark outline) and a 16×16 Souls icon.
5. **Demon buildings** (footprint in cells; art may be taller than its footprint):
   - Infernal Citadel (the Main Base), 2×2.
   - Portal, 2×2: a hellish gateway in a dark stone ring with a swirling crimson centre. This replaces the Barracks.
   - Spire tower, 1×1.
   - Outpost, 2×2.
   - Each building in a finished state and a faded under-construction state.
6. **Mortal buildings** (human village and castle style):
   - Peasant house 2×2, farmhouse 2×2, fenced crop field 3×3 that tiles, watchtower 1×1, tent and campfire 2×2.
   - Town hall 3×3.
   - Castle parts on the cell grid: wall straight, wall corner, gate (2 cells wide) and corner tower. Also a wooden palisade straight and corner.
7. **Icons** (16×16, outlined, for the UI): Wood, Stone, Iron, Gold, Souls, Shards (a demonic crystal), Vigilance (a watchful eye) and a Portal icon.

## Delivery

- Final assets: 1× pixel size, transparent PNG, one file per object or tile set.
- Put them in `D:\GameDev\Isekai-Survivors\Assets\Game\Art\Placeholder\`, in subfolders `Ground`, `Obstacles`, `Resources`, `Buildings\Demon`, `Buildings\Mortal` and `Icons`.
- Name files plainly, e.g. `Portal.png`, `Grass_Corrupted_L3_A.png`, `Icon_Souls.png`.
- For each sheet, add a short `.txt` next to it giving the cell size and what each cell is.
- Don't change anything else in the Unity project. No scripts, scenes or prefabs, and no git commits. The main session imports and wires up the art.
- Finish with a contact sheet of everything (scaled ×4) and a short list of what was made, what was skipped, and any retakes worth doing.
