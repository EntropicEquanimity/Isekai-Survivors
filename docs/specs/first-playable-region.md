# Spec: the first playable Region, the demon-invasion way

Words follow `CONTEXT.md`. Decisions come from the grill of 2026-11-19. Where this spec and the older sections of `docs/plans/isekai-conquerors.md` disagree, this spec wins.

## Problem

The game still plays like the survivors prototype it grew from. Enemies arrive on a timer and crowd one base. A Region is won by destroying nests and surviving a final Wave. The player sees everything on the map. Every unit fights the same way. Losing the Main Base loses the whole Run. None of the redesign's tension exists yet:
- staying hidden;
- mortals noticing you and growing stronger the longer you take;
- demons that act on their own;
- Jack's soul being the real stake.

## Solution

One outer-ring Region, generated from the Run's seed, that plays the new way from start to capture:
- **The start:** Jack starts in a far corner with the Anchor he picked (Shrine or War Throne), in a Region covered by fog of war.
- **What he sees:** his vision and his demons' vision reveal ground through line of sight, cut by rocks, walls and cliffs, and halved at night.
- **The mortals:** they live in generated farms, villages, camps and a small fort, which is the Seat.
- **Pressure:** killing mortals, being witnessed, and alarm riders raise Vigilance. Each new stage sends a Wave from the map edge or a spawn circle, up to the Region's cap. Towns raise troops from Supply once alerted.
- **Demons:** summoned at Portals with standing orders and a bones policy. They fight by their traits, grow in level, and can go rogue.
- **Corruption:** demons spread it, buildings need it, and priests scrub it away.
- **Jack's soul:** when his body dies he is a draining wisp that must reach the Anchor (cheap) or a demon (expensive). Running out of Souls ends the Run.
- **Capture:** destroying the Seat captures the Region.

The trade-off is depth over breadth. One ring, one faction (humans) and two Anchors, all built the way the deeper rings will reuse. That way the core loop gets proved before content is multiplied.

## User stories

1. As a player, I want to choose my Anchor before a Run, so that I decide between a hidden start and a visible, empowering one.
2. As a player, I want the Region hidden under fog of war that my units' sight lines reveal, so that scouting and stealth matter.
3. As a player, I want the mortals to notice my actions and grow more dangerous in stages, so that my choices about when and where to strike have consequences.
4. As a player, I want to stop alarms by killing riders and witnesses before they report, so that a careful approach is rewarded.
5. As a player, I want to set each Portal's order, rally point and bones policy, so that I steer my demons without controlling them one by one.
6. As a player, I want demons to act on their own personalities and grow stronger, so that my army feels alive, even when that risks a revolt.
7. As a player, I want to spread corruption and build only on it, so that taking ground is part of the strategy.
8. As a player, I want to survive my body's death by steering Jack's soul to my Anchor or a demon, so that death is a costly setback rather than the end.
9. As a player, I want to capture the Region by destroying its Seat, so that a bold strike can win without clearing every mortal.
10. As a player, I want workers to gather Stone and Wood for me, so that I can focus on the fight once my economy is going.
11. As a player, I want to tell sides apart at a glance in a crowd, with colours I can change, so that the battlefield stays readable for everyone, colour blindness included.

## Decisions

**Region and generation**
- **Generated outer-ring Region, about 160×110 cells, from the Run's seed:** the same seed gives the same Region, which makes testing and debugging repeatable. A test recipe with a fixed seed and a small map replaces the hand-built scene in tests.
- **Handcrafted pieces placed in a fixed order:**
  - What a piece holds: a tile patch, building spots, spawn markers and road ends.
  - The order: Seat, then villages, farms and camps (3–10 in total), roads, walls, then rocks, nodes and wildlife.
  - Why: pieces give designed-looking places while the layout varies, and walls go last so they wrap what's already placed.
  - Cost: authoring a small piece set for the outer ring.
- **Jack starts in a far corner, with the Seat toward the opposite side:** the Region is a journey from your corner to the ruler. Map size and Seat distance are tuned against the 10–20 minute target.
- **Startup goes through the Region bootstrap's setup and populate steps:** the generator is a set of setup and populate steps, not a new startup path.

**Anchor and Jack's soul**
- **Two Anchors at first, picked before a Run:**
  - **Shrine:** small, and hidden until a mortal patrol sees it. Being found raises Vigilance a full stage and makes it the mortals' target from then on.
  - **War Throne:** visible from the start, Vigilance starts at Uneasy, it gives demons in range a strong aura, and it spreads corruption faster.

  The two cover the stealth-or-strength choice. Other Anchors wait for meta progression. The Anchor takes the Main Base's role (cheap revive, summoning Demon Kings and workers, corruption source), and the code keeps the Main Base name for now.
- **Losing the Anchor doesn't end the Run:** you lose cheap revives until a new Anchor is raised (one at a time, on corrupted ground, at a high cost). It replaces "losing the Main Base loses the Run", which made the whole Run hang on one building.
- **The soul economy.** While Jack has no body he is a wisp the player steers. Starting numbers, all to tune:

  | Rule | Starting value |
  |---|---|
  | Drain per second while bodiless | 1 + 0.2 × soul level + deaths this Run |
  | Revive at the Anchor | about 20 + 2 × soul level |
  | Possess one of his demons (on touch) | about 100 + 10 × soul level |
  | Possess while still alive | 25% less than the above |
  | Near priests or on holy ground | drain doubled |
  | Souls reach zero while bodiless | the Run is lost |

  The wisp passes over walls and ordinary units can't hit it. This makes Souls both currency and life, so spending is a risk. The cost is that losses become less obvious, so the HUD must show the drain clearly.
- **Soul level is raised by spending Souls** (about 50 × level). It raises Jack's stats and his revive, possession and drain costs. Soul paths and their points are out of scope; this spec only needs the level itself, because the costs depend on it.
- **Demon Kings are Jack's normal bodies:** Jack starts in Demon_D. Jack in an ordinary demon is weaker and spreads no corruption.

**Mortals, Vigilance and Waves**
- **Vigilance is a stage machine:** Unaware → Uneasy → Alarmed → Mobilized → Crusade.
  - What raises it: any mortal kill a little (a witnessed kill more), a rider reaching the Seat by a full stage, and the Anchor or Jack being spotted.
  - It decays slowly while nothing is seen, but never below the highest stage reached minus one.
  - Wildlife kills never raise it.

  This puts the pressure in the player's hands instead of a timer.
- **Alarms:** a mortal who sees 5 or more bone piles marks its area alerted and sends a rider (Lancer art) to the Seat. The rider can be seen and killed; killing the witness first stops the alarm. That gives stealth something to do besides staying out of sight.
- **Waves:**
  - One Wave per new Vigilance stage, then one every few minutes at the top stage.
  - The outer ring's cap is 2.
  - Vigilance keeps rising during a Wave, so Waves can cascade within the cap.
  - Waves arrive from the map edge or a spawn circle, never out of nowhere.
  - Spawn circles stay marked once discovered, and destroying one stops it.

  The timed schedule goes away.
- **Supply and troops:**
  - Farms and villages produce Supply, and razing them cuts it.
  - Once the Region is alerted, the fort and any town raise troops from Supply, faster at each stage up to a limit. An undetected Region raises none.

  This is where the time pressure comes from: the longer you wait, the stronger they get.
- **Mortal behaviour:** peasants work and flee to the nearest stronghold when attacked, spreading the alarm. Guards patrol, more of them at higher Vigilance. At night patrols pull back toward settlements.
- **Heroes:** the outer ring has its ruler at the Seat and no other heroes. The ruler has 2–3 traits. An escaped ruler is noted as retreating to a deeper Region or the Capital, and the player sees their traits. There is no memory of the player and no rank ladder; that keeps clear of the Nemesis-style systems.

**Capture**
- **Destroying the Seat captures the Region:** taking out the rulers is enough, and killing everything is not required. It replaces nests plus a final Wave. Nests become ordinary mortal camps.

**Factions and wildlife**
- **Humans only, plus wildlife:**
  - Mortal units: Soldier, Swordsman, Archer, Armored Axeman, Knight, Lancer, Priest.
  - Wildlife: Werebear, Werewolf, Slime. Wildlife are neutral or hostile and give a little Souls, without Vigilance.

  Orcs wait for the second faction.

**Demons, Portals and bones**
- **Portal orders:** each Portal has a standing order (Guard its area, Raid a target marked on the map, or Follow Jack), a rally point and a bones policy (Harvest, Destroy or Ignore). This is macro without micro, as the design requires.
- **Demons act on their own:** they decide with the trait-weighted behaviour AI from the architecture notes, inside their Portal's order. Unruly demons ignore orders but stay on your side.
- **Levels and revolt:** demons level from kills. Loyalty drops while a demon's level is above Jack's soul level. At zero it turns rogue: its own team, hostile to everyone, possibly taking low-Loyalty demons along. Jack wins it back by defeating or possessing it.
- **The first demon roster:** Imp (Demon_A) as the basic Portal demon, a bone-using demon that raises skeletons (they crumble after a few minutes), and one worker type. Bones are used once.

**Traits**
- **Random, data-driven traits, each with a behaviour weight and a buff:**

  | Trait | Behaviour | Buff |
  |---|---|---|
  | Cowardly | Flees below 75% health | +30% max health |
  | Berserk | Never flees | +20% damage |
  | Brave | Doesn't flee until 10% health | +10% armour |
  | Sneaky | Invisible until it attacks | +25% damage on its first hit |
  | Ambusher | Backs off after 3 kills | +30% move speed while hidden |
  | Unruly | Ignores orders, wanders off to cause trouble | +15% attack speed |
  | Vigilant | Raises alarms faster | +50% vision |
  | Protector | Stays near its settlement or post | +20% health near it |
  | Purifier | Removes corruption | Heals nearby allies slowly |

  - **Odds:** ordinary units have a 10% chance of one trait, elites 25%, heroes always 2–3.
  - **Eligibility:** each trait says which sides and unit types may have it (no Purifier demons).

  Traits are weights on shared behaviours, not separate AIs, so adding a trait is data rather than code.

**Vision and fog of war**
- **Vision ranges in daylight:** Jack 14 cells, demons 8, mortals 7, wildlife 6.
- **Line of sight:** rocks, walls and cliffs block it for both sides.
- **Day and night:** an 8-minute cycle with 3 minutes of night, when every vision range is halved and demons get a small bonus.
- **Fog memory:**
  - Explored ground shows terrain, rocks, nodes and buildings as they were last seen; a destroyed building stays until seen again.
  - Units only show while in sight.
  - The map screen obeys the same rules.

  Hidden information drives scouting, stealth and the alarm game. The cost is a per-frame vision update, which must not bring back the lag fixed earlier. Vision cells update incrementally, and fog draws as an overlay.
- **Sneaky units:** invisible until they attack, then visible for 3 seconds.

**Corruption**
- **A level per ground cell, 0–4, as a layer on the Region grid:**
  - **Spreaders:** Demon Kings, the Anchor and Outposts (Spires don't), and a dying demon (25% chance, one level).
  - **Purifying:** priests near their settlement lower a cell one level every 20 seconds or so.
  - **Building:** only on level 2 or higher, with the HUD saying "not corrupted enough".
  - **Weakness:** demons are about 20% weaker on cells below level 2.
  - **Look:** an overlay over the ground tiles, using the placeholder corrupted tiles.

  This replaces the Base-radius rule and makes territory the strategy.

**Resources and workers**
- **Stone, Wood and Souls only:** Iron and Gold leave the stockpile and the HUD. Fewer numbers keeps the game about macro.
- **Workers:** summoned at the Anchor, they mine the nearest node on corrupted ground and carry the load to the Anchor or an Outpost. Jack still mines by walking up to a node.

**Readability**
- **Team outlines:** each unit, hero and building gets a 1 px outline in its side's colour, replacing the dark outline.
  - Defaults: yours green, mortals red, neutral white, hostile wildlife yellow, rogue purple.
  - The colours are adjustable in settings, with colour-blind presets.
  - A small custom sprite shader does the recolouring, with one material per side, so thousands of units still batch.
  - Rocks, nodes and UI icons keep their dark outline.

**What goes away**
- **Removed:**
  - the Legacy tree, Shards and their window, saves and rewards;
  - Cloning Pods;
  - nests as the capture rule;
  - the timed Wave schedule and the HUD tied to it;
  - Run-time escalation.

  Meta progression comes back later as Sigils and unlocks. The Region buff on capture stays.
- **What must keep working:**
  - the Planet map, Run travel between captured Regions, and the grid inventory with Items;
  - Region startup order;
  - the horde's performance (no new map-wide path recomputes);
  - pixel-perfect 16 px rendering;
  - the map screen.

## Testing

- **Seam: engine-free rules** (stories 2, 3, 4, 6, 7, 8, 9, 11), Pure tests on the Rules assembly.
  - **What it covers:**
    - line of sight and fog memory over a small grid;
    - the Vigilance stage machine (rises, decay floor, cascades) and Wave scheduling against caps;
    - the soul economy (drain growth with level and deaths, revive and possession costs including the 25% discount, losing at zero);
    - Loyalty and revolt;
    - trait odds, eligibility and buffs;
    - corruption spread, purify and the build rule;
    - Region layout from a seed (same seed, same layout; all pieces placed; Seat opposite the start);
    - the Seat capture rule.
  - **Edge and failure cases:**
    - zero Souls exactly when the body dies;
    - possessing while alive versus bodiless;
    - Vigilance at the top stage with the cap reached;
    - a rider killed one cell from the Seat;
    - corruption at levels 0 and 4;
    - a Purifier rolled for a demon (must not happen);
    - a generator seed where a piece doesn't fit.
  - **Fixtures:** small hand-built grids and fixed seeds.
- **Seam: generated Region in Play Mode** (stories 1, 3, 4, 5, 8, 9, 10), tests in a generated outer Region from a fixed test recipe, using the shared test loader and real-time waits.
  - **What it covers:**
    - picking each Anchor changes the start (hidden versus Uneasy plus aura);
    - fog hides mortals until a demon sees them;
    - a witnessed kill raises Vigilance;
    - a rider reaching the Seat raises a stage, and killing it doesn't;
    - a Portal's Raid order sends its demons to the mark;
    - Jack's body dying, then reviving at the Anchor or possessing a demon, with the right Souls spent;
    - destroying the Seat captures the Region;
    - a worker delivers Stone;
    - building is refused off corruption with the right reason.
  - **Edge and failure cases:**
    - the Anchor destroyed while Jack is bodiless;
    - a rogue demon attacking your own side;
    - night halving vision.
  - **Fixtures:** the test Region recipe, the debug menu's entries, and the existing perf probe for a frame-time check.
- **Not tested:**
  - the outline shader's look and the corrupted-ground art (checked in screenshots);
  - balance numbers (tuned in play);
  - story scenes (out of scope).

## Out of scope

- the middle, inner and Capital rings, and their heroes beyond the ruler;
- the orc faction;
- Soul paths and their trees;
- meta progression (Sigils and unlocks);
- the Ossuary, Wandering Altar, Phylactery and Hellmouth Anchors;
- story scenes as the loading screen;
- the Suit that commands nearby demons;
- the phase 9 HUD beyond what these stories need (Souls, drain, Vigilance, the "not corrupted enough" reason).

## Domain changes

- `CONTEXT.md` (not committed yet):
  - **Added:** Seat, Demon King/Suit, Possession, Soul level, Soul path, Anchor, Worker, Outpost, Loyalty/Rogue, Bones, Settlement, Spawn circle, Team outline.
  - **Changed:** Capture (rulers), Run (start anywhere, travel), Wave (caps by ring, cascades), Trait (random, buffs, eligibility), Corruption (sources, purifying, build rule), Fog of war (line of sight, night, ranges), Resource (Stone and Wood), Hero (escaped rulers), Portal (orders and bones policy).
  - **Removed:** Shard. Citadel is retired in favour of Anchor.
