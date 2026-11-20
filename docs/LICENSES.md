# Third-party assets and licences

Everything third-party that is in the build, with author, licence and link. The in-game Credits screen shows the same list
(`Assets/Game/Rules/Credits/CreditsList.cs`); keep the two in step. Where the terms are not known they are marked
**licence unconfirmed**, not guessed, and nothing has been removed: the owner decides.

## Licence unconfirmed: the owner must confirm before release

1. **Music, 6 tracks by Clement Panchout** (Another Light (Intro), Journey, Forest Of A Thousand Masks, Revenge, Union of the Damned, Time Flies): the source folder `D:/GameDev/Assets/BGM` has no licence or readme file. Credit is given; the terms (commercial use in a game, credit wording) are unconfirmed. Ask or check https://www.clementpanchout.com/.
2. **Sound effects, 400 Sounds Pack** (34 files): no licence or readme file, author and source unknown. Find where it came from and what it allows, or replace it.
3. **Sound effects, Shapeforms Audio Free Sound Effects** (6 files from the Arcane Activations and Dystopia previews): free samplers, and the licence agreement is not in the folder. A sister sampler (Future UI) says commercial and non-commercial use are allowed "under the license agreement"; the agreement itself must be found. https://shapeforms.com
4. **Fog cloud texture** (`Vfx_Fog_Cloud.png`, copied from `D:/GameDev/Assets/VFX/fogcloud.png`): author and licence not recorded.
5. **Item icon sheet** (`Assets/Game/Art/Items/Items.png`, 512x867, sprites such as "Rune Of Earth"): carried over from the earlier survivors prototype; author and source not recorded, and it is not a byte copy of any PNG in `D:/GameDev/Assets/16x16PixelArt` or `32x32PixelArt`.
6. **Arrow projectile** (`Assets/Game/Art/Projectiles/Arrow.png`): author and source not recorded (not a byte copy of any PNG in the 16x16 or 32x32 pixel-art folders).
7. **Stone node** (`Assets/Game/Art/Resources/Stone Node.png`): author and source not recorded.
8. **Font Silver** (Poppy Works; `Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/Silver.ttf`): no licence file with it. It is a TextMesh Pro asset the UI does not use, but it sits in a `Resources` folder so it ships; confirm its licence or delete it.

Also worth a decision, not a licence gap: the Placeholder art is AI-generated (Cursor CLI image generation, model grok-4.7-medium); the generator's terms and any store disclosure about AI-generated assets were not reviewed here.

## Music

All six tracks by **Clement Panchout** (https://www.clementpanchout.com/), in `Assets/Game/Audio/Music`. Licence unconfirmed (item 1).

| Track | Used for |
|---|---|
| Another Light (Intro) | Main menu |
| Journey | Planet map |
| Forest Of A Thousand Masks | Calm Region |
| Revenge | Tense Region |
| Union of the Damned | Capture and victory |
| Time Flies | Defeat |

## Sound effects

Files and the cue they play for are in `Assets/Game/Audio/Sfx/CREDITS.txt`.

| Title | Author | Licence | Link |
|---|---|---|---|
| 400 Sounds Pack (34 sounds) | not stated in the pack | **licence unconfirmed** (item 2) | none known |
| Shapeforms Audio Free Sound Effects (6 sounds) | Shapeforms Audio | **licence unconfirmed** (item 3) | https://shapeforms.com |

## Visual effects

| Title | Author | Licence | Link |
|---|---|---|---|
| Super Pixel Effects Gigapack (Free Version), 15 sheets | Will Tice / unTied Games | unTied Games licence (`license.txt` in the pack, http://untiedgames.com/files/license.txt): commercial and non-commercial use in a game is fine; credit in the game is asked for ("Super Pixel Effects Gigapack - Will Tice / unTied Games"); the assets may not be redistributed on their own or bundled with a tool or engine; no use as AI training material | https://untiedgames.itch.io/super-pixel-effects-gigapack |
| Fog cloud texture | not recorded | **licence unconfirmed** (item 4) | none known |

## Characters

Both packs have no licence file in their folders; the terms below are from the author's itch.io pages (read 2026-11-20): "You may use these assets in both personal and commercial video game projects. You may edit or modify these assets... Credit is appreciated but not required."

| Title | Author | Licence | Link |
|---|---|---|---|
| Tiny RPG Character Asset Pack v1.03 (Full 20 Characters): the mortals | Zerie | as above | https://zerie.itch.io/tiny-rpg-character-asset-pack |
| Tiny RPG Character Asset Pack 02 (Full 20 Characters): the demons | Zerie | as above | https://zerie.itch.io/tiny-rpg-character-asset-pack-02 |

## Art

| Title | Author | Licence | Link |
|---|---|---|---|
| Placeholder art: ground, buildings, props, icons (`Assets/Game/Art/Placeholder`) | Generated for this project through the Art Forge pipeline (Cursor CLI image generation, grok-4.7-medium, pixel-finished) | Generated for the project; see the note above | none |
| Item icon sheet (`Items.png`) | not recorded | **licence unconfirmed** (item 5) | none known |
| Arrow projectile (`Arrow.png`) | not recorded | **licence unconfirmed** (item 6) | none known |
| Stone node (`Stone Node.png`) | not recorded | **licence unconfirmed** (item 7) | none known |

## Fonts

| Title | Author | Licence | Link |
|---|---|---|---|
| Fusion Pixel Font 12px (all menu and HUD text; bundled with Blondie PixelPerfect, built from Ark Pixel, Cubic 11 and Galmuri) | TakWolf | SIL Open Font License 1.1 (the `OFL.txt` and `LICENSES/` files next to the font must ship, and do) | https://github.com/TakWolf/fusion-pixel-font |
| monogram (TextMesh Pro asset, unused by the UI) | Vinicius Menezio | CC0 | https://datagoblin.itch.io/monogram |
| Silver (TextMesh Pro asset, unused by the UI) | Poppy Works (Itou Hiro, Yoon Design, Wolfgang Wozniak) | **licence unconfirmed** (item 8) | https://poppy.works/ |
| Liberation Sans (TextMesh Pro default) | Red Hat / Ascender (Liberation Fonts) | SIL Open Font License 1.1 | https://github.com/liberationfonts/liberation-fonts |

## Code and tools

| Title | Author | Licence | Link |
|---|---|---|---|
| BlondieUtils packages (`com.blondie.*`) | Entropy (the project owner) | own library, no licence file | private repository |
| DOTween (used by Blondie Juice) | Daniele Giardini / Demigiant | DOTween licence, free to use | http://dotween.demigiant.com/license.php |
| Unity packages (Input System, 2D animation, spriteshape, tilemap and psdimporter, AI Navigation, uGUI, Timeline, Recorder, Test Framework, and the rest of `Packages/manifest.json`) | Unity Technologies | Unity Companion License | https://unity.com/legal/licenses/unity-companion-license |

Not in the project: Odin Inspector (removed with the legacy game) and NaughtyAttributes. Sound packs in `D:/GameDev/Assets/Sound Effects` other than the two above (JDSherbert, Chords, UI, Short - Low, unicae keyboard, rfxgen) are not used.
