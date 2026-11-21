# UI rules

Read this before building or changing any screen, panel, HUD or button. They are the user's rules (2026-11-21 playtest).

## What a control shows, in this order

1. **Icon first.** Every choice, action, resource and building is an icon whenever one can exist. A button is an icon with a frame.
2. **Then numbers.** Costs, counts, timers and hotkeys sit on or beside the icon as numbers, with a resource icon instead of a resource name ("20 [stone icon]", not "20 Stone").
3. **Text last.** Names, explanations and other info go in the tooltip, or appear once the thing is selected. At most a short name under or over an icon.

Minimal text on screen. If a line of help is needed, it is one short line, and the full list lives behind a key (H: controls).

## Readability

- Text is always light on dark (the menus' palette: `GameStyle.Text` on `GameStyle.Panel` / button colours). Never Unity's default grey button with black text, and never dark text on a dark panel.
- Every HUD panel gets the game's stylesheet through `GamePanels.Mount`, so buttons look like the menus' buttons.
- Pixel font sizes only: 12, 24, 36, 48.

## Size

- HUD is small by default: it leaves the world visible and room to grow.
- Menus (settings, loadout, pause) fill most of the screen, not a fixed box.
- The player's Text size setting changes all text, HUD included.

## Behaviour

- Changing a choice updates the controls in place. Never clear and rebuild a whole panel on a click (it flashes the screen).
- Selection shows on the icon itself (frame, highlight), not as "[x]" text.
