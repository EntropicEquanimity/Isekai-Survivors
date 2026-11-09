# Isekai Survivors

A Vampire Survivors-like. Pick a Character, survive waves of enemies on a Level, collect XP, and level up into Weapons and Equipment.

## Language

### The run

**Character**:
A playable body chosen before a Run — Knight, Samurai, Rogue, and later ones. Different starting Weapon and stats.
_Avoid_: class, hero, skin, champion

**Run**:
One attempt at a Level, from spawn until the player dies or the Level ends.
_Avoid_: session, match, game, attempt

**Level**:
A map plus its difficulty settings that a Run is played on — for example Green Pastures - Casual and Green Pastures - Hell.
_Avoid_: stage, world, planet

### Combat

**Wave Type**:
How the game brings enemies at a given time. The types are Horde, Flight, Squad, and Mix.
_Avoid_: spawn table, encounter (as this idea)

**Horde**:
A Wave Type: a spike in count, cheap Chasers, surround.
_Avoid_: swarm, rush

**Flight**:
A Wave Type: Flyers from off-screen. Giant Bat is the prototype.
_Avoid_: air wave, bat wave

**Squad**:
A Wave Type: a small clump of tougher units that spawn together and stay together.
_Avoid_: pack, patrol, elite wave (as this type)

**Mix**:
A Wave Type: weighted random spawns around the player. Default filler.
_Avoid_: normal wave, standard spawn

**Role**:
What an enemy is for in a fight. An enemy has one Role and may also be Elite. The Roles are Chaser, Exploder, Elite, Flyer, Ranged, Buffer, Summoner, and Hoarder.
_Avoid_: enemy type (when you mean this job), archetype, class

**Chaser**:
A Role: walks at the player, contact damage. Most of a Horde.
_Avoid_: melee, grunt, zombie

**Exploder**:
A Role: bursts on death or when it closes.
_Avoid_: bomber, kamikaze, suicide enemy

**Elite**:
A Role (and a flag on other Roles): fat, telegraphed, extra XP.
_Avoid_: champion, mini-boss, rare

**Flyer**:
A Role: airborne, ignores ground clutter. Flight waves bias toward this Role.
_Avoid_: bat, flying enemy

**Ranged**:
A Role: keeps distance and shoots.
_Avoid_: spitter, archer (as the Role)

**Buffer**:
A Role: hangs back and auras allies. The aura is visible.
_Avoid_: support, shaman, healer (unless it actually heals)

**Summoner**:
A Role: spawns extras from itself. Those extras hit a global cap so the screen cannot melt.
_Avoid_: spawner, breeder, necromancer (as the Role)

**Hoarder**:
A Role: the loot-drop enemy. Tanky or fleeing. Extra loot on kill.
_Avoid_: loot bug, treasure enemy, carrier

### Progression within a Run

**XP**:
Dropped by enemies and picked up during a Run. Filling the bar levels the player up.
_Avoid_: essence, soul energy

**Item**:
Anything the player can be offered at level-up and carry during a Run. Weapons and Equipment are both Items.
_Avoid_: relic, loot (as the whole idea)

**Weapon**:
An Item that attacks on its own — Cutlass, Rapier, Shortbow, Sai, and the like. A Character starts the Run holding one.
_Avoid_: gun, spell (as the whole idea)

**Equipment**:
An Item that modifies stats or behaviour instead of attacking — Magnet, Iron Briefs, Duplication Mirror, and the like.
_Avoid_: passive, accessory, relic
