using System.Collections.Generic;
using System.Linq;

namespace Isekai
{
    /// <summary>
    /// Every third-party asset in the build, with author, licence and link. The credits screen shows it and docs/LICENSES.md
    /// repeats it; keep them in step. Assets whose licence is not known are marked unconfirmed, never guessed.
    /// </summary>
    public static class CreditsList
    {
        public const string UnconfirmedMark = "Licence unconfirmed";

        public const string MusicGroup = "Music";
        public const string SoundGroup = "Sound effects";
        public const string VfxGroup = "Visual effects";
        public const string CharacterGroup = "Characters";
        public const string ArtGroup = "Art";
        public const string FontGroup = "Fonts";
        public const string CodeGroup = "Code and tools";

        private const string Composer = "Clement Panchout";
        private const string ComposerSite = "https://www.clementpanchout.com/";
        private const string ComposerLicence = UnconfirmedMark + ": no licence file came with the tracks; credit is given";

        private const string TinyRpgLicence = "Free for personal and commercial games, edits allowed, credit appreciated (author's itch.io page)";
        private const string SourceUnknown = "Author not recorded";
        private const string NoLicence = UnconfirmedMark + ": source not recorded";

        private static readonly CreditEntry[] All =
        {
            new(MusicGroup, "Another Light (Intro)", Composer, ComposerLicence, ComposerSite, false, "Main menu"),
            new(MusicGroup, "Journey", Composer, ComposerLicence, ComposerSite, false, "Planet map"),
            new(MusicGroup, "Forest Of A Thousand Masks", Composer, ComposerLicence, ComposerSite, false, "Calm Region"),
            new(MusicGroup, "Revenge", Composer, ComposerLicence, ComposerSite, false, "Tense Region"),
            new(MusicGroup, "Union of the Damned", Composer, ComposerLicence, ComposerSite, false, "Capture and victory"),
            new(MusicGroup, "Time Flies", Composer, ComposerLicence, ComposerSite, false, "Defeat"),

            new(SoundGroup, "400 Sounds Pack", "Author not stated in the pack", UnconfirmedMark + ": no licence or readme file, source unknown",
                "", false, "34 sounds"),
            new(SoundGroup, "Shapeforms Audio Free Sound Effects (Arcane Activations and Dystopia previews)", "Shapeforms Audio",
                UnconfirmedMark + ": free sampler, the licence agreement is not in the folder (a sister sampler says commercial use is allowed)",
                "https://shapeforms.com", false, "6 sounds"),

            new(VfxGroup, "Super Pixel Effects Gigapack (Free Version)", "Will Tice / unTied Games",
                "unTied Games licence: commercial use in a game is fine, credit asked for, no redistribution on its own, no AI training",
                "https://untiedgames.itch.io/super-pixel-effects-gigapack", true, "15 effect sheets"),
            new(VfxGroup, "Fog cloud texture (fogcloud.png)", SourceUnknown, NoLicence, "", false, "Fog of war"),

            new(CharacterGroup, "Tiny RPG Character Asset Pack v1.03 (Full 20 Characters)", "Zerie", TinyRpgLicence,
                "https://zerie.itch.io/tiny-rpg-character-asset-pack", true, "Mortals"),
            new(CharacterGroup, "Tiny RPG Character Asset Pack 02 (Full 20 Characters)", "Zerie", TinyRpgLicence,
                "https://zerie.itch.io/tiny-rpg-character-asset-pack-02", true, "Demons"),

            new(ArtGroup, "Placeholder art (ground, buildings, props, icons)", "Generated for this project (Art Forge pipeline)",
                "Generated for the project with Cursor CLI image generation (grok-4.7-medium), then pixel-finished", "", true,
                "Assets/Game/Art/Placeholder"),
            new(ArtGroup, "Item icon sheet (Items.png)", SourceUnknown, NoLicence, "", false, "Items"),
            new(ArtGroup, "Arrow projectile (Arrow.png)", SourceUnknown, NoLicence, "", false, "Arrow"),
            new(ArtGroup, "Stone Node.png", SourceUnknown, NoLicence, "", false, "Stone node"),

            new(FontGroup, "Fusion Pixel Font 12px (menu and HUD text)", "TakWolf", "SIL Open Font License 1.1",
                "https://github.com/TakWolf/fusion-pixel-font", true,
                "Bundled with Blondie PixelPerfect; built from Ark Pixel, Cubic 11 and Galmuri, whose OFL files ship with it"),
            new(FontGroup, "monogram", "Vinicius Menezio", "CC0 (public domain)", "https://datagoblin.itch.io/monogram", true,
                "TextMesh Pro asset, not used by the UI"),
            new(FontGroup, "Silver", "Poppy Works (Itou Hiro, Yoon Design, Wolfgang Wozniak)", UnconfirmedMark + ": no licence file with the font",
                "https://poppy.works/", false, "TextMesh Pro asset, not used by the UI"),
            new(FontGroup, "Liberation Sans", "Red Hat / Ascender (Liberation Fonts)", "SIL Open Font License 1.1",
                "https://github.com/liberationfonts/liberation-fonts", true, "TextMesh Pro default font"),

            new(CodeGroup, "BlondieUtils packages (com.blondie.*)", "Entropy (the project owner)", "Own library, no licence file",
                "", true, "Private repository"),
            new(CodeGroup, "DOTween", "Daniele Giardini / Demigiant", "DOTween licence (free to use, see link)",
                "http://dotween.demigiant.com/license.php", true, "Used by Blondie Juice"),
            new(CodeGroup, "Unity packages (Input System, 2D animation and tilemap, AI Navigation, uGUI, Timeline, Recorder, Test Framework)",
                "Unity Technologies", "Unity Companion License", "https://unity.com/legal/licenses/unity-companion-license", true, ""),
        };

        /// <summary>Every entry, grouped in the order the screen shows them.</summary>
        public static IReadOnlyList<CreditEntry> Entries => All;

        /// <summary>The group names in order.</summary>
        public static IEnumerable<string> Groups => All.Select(e => e.Group).Distinct();

        /// <summary>The entries the owner must confirm before release.</summary>
        public static IEnumerable<CreditEntry> Unconfirmed => All.Where(e => !e.Confirmed);
    }
}
