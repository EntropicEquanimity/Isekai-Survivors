# Isekai Conquerors

A survivors-like horde strategy game. Jack, isekai'd as a demon, invades a Planet Region by Region with hordes of demons while the mortals defend. The player makes the macro decisions (building, Resources, where to push); the demons decide for themselves. Between Runs, Shards buy Legacy upgrades. The full design is in `docs/plans/isekai-conquerors.md`; this file fixes the words.

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
Taking a Region by taking out its rulers (its seat of power). Killing everything is not required.
_Avoid_: clear, conquer (as the rule)

**Run**:
One attempt at a Planet, from landing until the Capital falls or the Main Base does.
_Avoid_: session, match, game

### The player's side

**Jack**:
The player: a soul in a demon body, who can move between Suits.
_Avoid_: hero (heroes are mortal), avatar

**Suit**:
A demon body Jack can inhabit (Demon_D, the Black Knights, the Warlock...).
_Avoid_: skin, class

**Character**:
In code and tests today, the body Jack controls (one Suit until possession lands).
_Avoid_: player unit

**Demon** / **Unit**:
A demon Jack doesn't inhabit, summoned through a Portal. Acts on its own; no micro. Weakened off corrupted ground.
_Avoid_: minion, soldier (soldiers are mortal)

**Imp**:
The first demon Unit (Demon_A), summoned by a Portal.

**Building**:
Anything placed: the Main Base, Portals, Spires, Outposts. Goes up over its build time, gaining health as it does.

**Main Base**:
The Citadel the Run depends on. Losing it loses the Run.
_Avoid_: HQ, castle

**Portal**:
The Building that summons demons, paid in Resources and Souls.
_Avoid_: barracks

**Spire**:
A defensive tower Building.

### The defenders

**Mortal** / **Denizen**:
Anyone living on the Planet who isn't a demon: humans in villages and castles, orcs, beasts, others. Mortals belong to factions.
_Avoid_: enemy (too broad), NPC

**Neutral**:
A creature that attacks no one by default; some fight back, some flee.

**Hostile**:
A creature that attacks everything on sight; some flee when hurt, some fight to the death.

**Hero**:
A named mortal defender of a Region with personality-driven AI: may flee, call reinforcements, build, or hold its castle.
_Avoid_: boss, champion

**Trait**:
Data that weights an AI's decisions: Cowardly, Berserk, Sneaky, Ambusher and others.
_Avoid_: perk (perks are Items), modifier

**Vigilance**:
How alert a Region's mortals are, rising Unaware → Uneasy → Alarmed → Mobilized → Crusade. Drives patrols, detection range, enemy strength and Waves.
_Avoid_: alert, aggro, threat level

**Wave**:
Mortal reinforcements arriving from the map edge or a spawn circle once Vigilance is high enough. Each Region has a Wave cap.
_Avoid_: spawn, attack (as this idea)

**Nest**:
Today's enemy camp: a roaming pack that regrows around it.

### The world

**Corruption**:
How demonic a ground cell has become, level 0 (untouched) to 4 (fully converted). Spreads from demon territory.
_Avoid_: taint, creep

**Obstacle**:
Something that fills its ground cell like a wall (rocks, dead trees, Stone deposits) until mined out.

**Fog of war**:
Unseen ground. Explored ground keeps a snapshot; enemy units only show while in sight. Walls and cliffs block vision; night shortens it.

**Resource**:
Wood, Stone, Iron and Gold, mined from nodes into the Region's stockpile.

**Soul**:
Harvested from mortals; Jack harvests best. Pays for Portals and levels Jack's soul for the Run.

**Shard**:
The meta currency, earned when a Run ends, spent in the Legacy tree.

### Items

**Item**:
Anything in the inventory: weapons, augments, perks, boons.

**Active inventory** / **Storage**:
Items in Active take effect; Items in Storage only sit there.

**Lifetime**:
Region (gone after this Region) or Run (kept until the Run ends).
