# Isekai Conquerors

A survivors-like horde strategy game. Jack, isekai'd as a demon, invades a Planet Region by Region with hordes of demons while the mortals defend. The player makes the macro decisions (building, Resources, where to push); the demons decide for themselves. Between Runs, the demons travel from world to world: Sigils (earned per captured Region, more for deeper rings, and per Capital) unlock anchor buildings, Soul path slots, mechanics, units and stats. Story plays as the loading screen between Regions: the next Region loads while the player clicks through it. The full design is in `docs/plans/isekai-conquerors.md`; this file fixes the words.

## Language

### The run

**Planet**:
One Run's whole map, made of Regions around the Capital.
_Avoid_: level, world map

**Region**:
One playable map of a Planet, with its own mortals, Resources, Vigilance and rulers.
_Avoid_: level, stage, zone

**Capital**:
The Region at the centre of the Planet. Capturing it wins the Run.

**Capture**:
Taking a Region by taking out its rulers (its Seat). Killing everything is not required. A Region takes 10–20 minutes; the Capital up to 30.
_Avoid_: clear, conquer (as the rule)

**Seat**:
A Region's seat of power and biggest stronghold, holding its ruling Hero: a small fort (outer ring), fort (middle), large fort (inner), castle (Capital). The player starts in a far corner of the Region.

**Run**:
One attempt at a Planet, about 1.5 hours: start in any Region, then take any Region touching a captured one, travelling freely between captured Regions, until the Capital falls (or the Run is lost).
_Avoid_: session, match, game

**Loadout**:
The screen a New Run opens on, before the Planet map: the player picks the Run's Anchor and its Soul paths.
_Avoid_: lobby

### The player's side

**Jack**:
The player: a soul in a demon body, who can move between Suits.
_Avoid_: hero (heroes are mortal), avatar

**Demon King** / **Suit**:
A strong demon body Jack normally inhabits (Demon_D, Demon_E, both Demonesses, the three Black Knights, Flame Golem, Warlock). Kings spread corruption and are summoned only at the Anchor (about 20 + 2 × level Souls, like a revive). Jack in an ordinary demon is weak and spreads none.
_Avoid_: skin, class

**Possession**:
Jack taking over a body. When his body dies he is a bodiless wisp the player steers (over walls, untouchable by ordinary units; priests and holy ground double the drain), draining Souls per second (1 + 0.2 × level + deaths this Run, numbers to tune) until he revives at the Anchor in a new body of his Demon King (cheap: about 20 + 2 × level) or possesses one of his demons (expensive: about 100 + 10 × level). One key does all of it (R): in a body it leaves the body (it fights on as an ordinary demon), and as a wisp it takes the Anchor or the demon he is over; touching alone does nothing. Possessing while the body he left still lives costs 25% less. Souls running out while bodiless loses the Run.

**Soul level**:
Jack's level, his soul size: raised any time by spending Souls (about 50 × level). Each level gives stats and a point in a Soul path, and raises revive, possession and drain costs.

**Soul path**:
A branch of Jack's per-Run soul tree, chosen before a Run (2 at first, more through meta progression): Brawler (Jack and the body he possesses: an ordinary demon becomes a super-powered one, plus skills), Overlord (all demons), Warlord (demons near Jack, rallying), Corruptor (corruption), Soulweaver (spells and abilities), Assassin (hiding, and speed and damage while hidden), Reaper (Souls harvested, cheaper possession, slower drain), Bonelord (bones and skeletons). Plaguebringer and Architect come later through meta progression. Points go only into the chosen paths.

**Character**:
In code and tests today, the body Jack controls (one Suit until possession lands).
_Avoid_: player unit

**Demon** / **Unit**:
A demon Jack doesn't inhabit, summoned through a Portal. Acts on its own; no micro. About 20% weaker off corrupted ground. Gains levels from kills.
_Avoid_: minion, soldier (soldiers are mortal)

**Loyalty** / **Rogue**:
A demon's Loyalty drops while its level is above Jack's; at zero it revolts and turns Rogue (its own team, hostile to all) until Jack defeats or possesses it.

**Imp**:
The first demon Unit (Demon_A), summoned by a Portal.

**Building**:
Anything placed: the Main Base, Portals, Spires, Outposts. Goes up over its build time, gaining health as it does.

**Anchor**:
The building Jack's soul is bound to in a Region, chosen before each Run from those unlocked: where he revives cheaply, where Demon Kings and workers are summoned, and a corruption source. Starting anchors: Shrine (small, hidden until a patrol finds it, no aura) and War Throne (visible from the start, Vigilance starts at Uneasy, a strong aura for demons, faster corruption). Unlocked later: Ossuary, Wandering Altar, Phylactery, Hellmouth. Losing it doesn't end the Run; a new one can be raised (one at a time, on corrupted ground, costly). The code still calls it the Main Base.
_Avoid_: Citadel (retired), HQ, castle

**Worker**:
A demon summoned at the Anchor that mines nodes on corrupted ground and carries the load to the Anchor or an Outpost.

**Portal**:
The Building that summons ordinary demons, paid in Stone, Wood and Souls; its roster grows with Jack's soul level and soul tree. Each Portal has its own standing order (Guard, Raid a marked target, Follow Jack), rally point and bones policy (Harvest, Destroy, Ignore).
_Avoid_: barracks

**Spire**:
A defensive tower Building.

**Outpost**:
A Building that extends demon territory and is a drop-off point for workers.

### The defenders

**Mortal** / **Denizen**:
Anyone living on the Planet who isn't a demon: humans in villages and castles, orcs, beasts, others. Mortals belong to factions.
_Avoid_: enemy (too broad), NPC

**Neutral**:
A creature that attacks no one by default; some fight back, some flee. Jack's demons, Spires and Jack himself hunt them on sight; mortals leave them alone.

**Hostile**:
A creature that attacks everything on sight; some flee when hurt, some fight to the death.

**Hero**:
A named mortal defender of a Region with personality-driven AI and 2–3 Traits: may flee, call reinforcements, build, or hold its castle. Every Region has its ruler at the Seat, plus 0–2 more by ring (Capital 3+). An escaped ruler retreats to the Capital or a deeper Region of their faction; the player learns their Traits (no memory of the player, no rank ladder).
_Avoid_: boss, champion

**Trait**:
Random data on a unit that weights its AI's decisions and comes with a buff: Cowardly, Berserk, Sneaky, Ambusher, Unruly (ignores orders, goes off causing trouble), Brave, Vigilant, Protector, Purifier. Most units have none (10% chance; elites 25%; Heroes always 2–3). Each trait says who can have it (no Purifier demons).
_Avoid_: perk (perks are Items), modifier

**Vigilance**:
How alert a Region's mortals are, rising Unaware → Uneasy → Alarmed → Mobilized → Crusade. Drives patrols, detection range, enemy strength and Waves.
_Avoid_: alert, aggro, threat level

**Wave**:
Mortal reinforcements arriving from the map edge or a spawn circle: one per new Vigilance stage (Vigilance keeps rising during a Wave, so Waves can cascade), then every few minutes at the top stage, until the Region's cap. The cap grows toward the Capital: outer ring 2, middle 3–4, inner 5–7. The Capital starts Alarmed and has one Wave per Region not yet captured (at least 3).
_Avoid_: spawn, attack (as this idea)

**Nest**:
Today's enemy camp: a roaming pack that regrows around it.

### The world

**Corruption**:
How demonic a ground cell has become, level 0 (untouched) to 4 (fully converted). Demon Kings spread it; so do the Anchor and Outposts in a radius (Spires are for defence), and a dying demon sometimes does (25% chance, one level). Demons are about 20% weaker below level 2. Priests and healers remove it over time. Buildings need corrupted ground ("not corrupted enough" otherwise).
_Avoid_: taint, creep

**Obstacle**:
Something that fills its ground cell like a wall (rocks, dead trees, Stone deposits) until mined out.

**Fog of war**:
Unseen ground. Explored ground keeps terrain, rocks, nodes and buildings as last seen; units only show while in sight. Walls, cliffs, rocks, trees and Buildings block sight of what lies behind them, but a Building in sight is seen whole; night halves vision (day: Jack 14 cells, demons 8, mortals 7, wildlife 6). Sneaky units are invisible until they attack.

**Bones**:
A pile left by a mortal or wildlife death. Some demons use bones (raise skeletons that crumble after a while, heal, empower abilities), set per Portal. A mortal who sees 5+ piles alerts its area and sends a rider to the Seat; if the rider arrives, Vigilance jumps a stage. Killing any mortal raises Vigilance slightly (more if witnessed); wildlife never does.

**Settlement**:
A mortal place in a Region: farms, villages and camps (3–10), small towns (wood-walled; 1–2 in the middle ring, 3–4 inner), forts and castles. Peasants flee to strongholds when attacked; guards patrol. The mortals' Supply (from farms and towns) pays for their soldiers, defences and raised troops; razing farms slows them. Towns and castles raise troops over time once the Region is alerted, faster at each Vigilance stage (up to a limit); an undetected Region raises none. Regions are generated from the Run's seed out of handcrafted pieces (small prefabs with tiles, building spots, spawn markers and road ends), placed in order: Seat, towns, villages and camps, roads, walls, then rocks, nodes and wildlife; each ring has its own pieces.

**Spawn circle**:
A mortal summoning circle Waves can arrive through; stays marked once discovered; destroying it stops its Waves.

**Team outline**:
The 1 px outline on units and buildings, coloured by side (defaults: yours green, mortals red, neutral white, hostile yellow, rogue purple; player-adjustable).

**Resource**:
Stone and Wood for now (plus Souls), mined from nodes into the Region's stockpile.

**Soul**:
Harvested from mortals; Jack harvests best. Pays for Portals and levels Jack's soul for the Run.

### Items

**Item**:
Anything in the inventory: weapons, augments, perks, boons.

**Active inventory** / **Storage**:
Items in Active take effect; Items in Storage only sit there.

**Lifetime**:
Region (gone after this Region) or Run (kept until the Run ends).
