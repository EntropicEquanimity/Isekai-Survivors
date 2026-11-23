# UI rules

Read this before building or changing any screen, panel, HUD or button.

## The user's rules (playtest, 2026-11-21)

1. **Minimal text on the UI.**
2. **Icons always first, if possible. Then numbers. Last, always, text**, explanations and other info.
3. A choice is an icon button that shows its text once selected. At most, its name under or over the icon.
4. Text must be readable: never black on black.
5. UI is much smaller by default, so there is space to expand. (HUD)
6. The settings panel is the size of the screen, not a static size. Medium text in it is what Large used to be.
7. Changing Text size changes the whole game, not only the settings popup.
8. Clicking a button must not make the screen flash.
9. Buttons keep their size when the page behind them changes.

## How we keep them (our working rules, not the user's words)

- Costs, counts, timers and hotkeys are numbers next to an icon: "20 [stone icon]", not "20 Stone". Names and explanations go in the tooltip. `IconButton`, `GameStyle.IconNumber` and `GameIcons` do this; a missing icon falls back to a short name.
- What is chosen is framed (`IconButton.Picked`), not greyed out and not marked "[x]".
- Light text on dark: every game panel is mounted through `GamePanels.Mount`, which adds `IsekaiHud.uss`. Never Unity's default grey button.
- Pixel font sizes only (12, 24, 36, 48), from the stylesheet and never inline, and no bold: that is how Text size reaches them. The HUD is 12 px at Medium, full-screen pages (`isekai-menu`) 24 px.
- Icons are drawn at whole scales only (`GameStyle.Icon`).
- HUD boxes go into a `HudStack` corner and never place themselves, so they cannot overlap at any text size.
- Full-screen pages (settings, loadout, Planet, map, end screen) fill the screen.
- A click updates controls in place or shows another page that is already built.
- Help is behind H. Check a new screen by looking at it at Medium and Large, not only by tests.
