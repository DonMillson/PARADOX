# PARADOX visual direction

**Status:** lobby UI polish implemented in source on the visual-polish branch. This is not a claim of in-game testing or a complete replacement of the HUD.

## Style

PARADOX should feel like part of Among Us, but with a distinct, restrained science-fiction identity:

- Midnight navy background with a soft cyan-to-violet reflected glow.
- Cyan PARADOX branding, red/coral Impostor indicators, cool aqua Crewmate indicators, warm amber Neutral indicators.
- White text for gameplay information; secondary labels in cool gray-blue.
- No giant full-screen overlays over tasks, voting, Customize or the game map.
- No use of color alone for state: OFF and PLANNED remain explicit.
- Polish and English labels remain supported.
- Branding credit: **BY DONMILLSON**.

## Implemented in this phase

1. `ParadoxVisualTheme` centralizes faction colors, label colors and state colors.
2. A 64 × 64 rounded, shaded panel sprite is generated at runtime once, with soft corner transparency. There is no external bundle required for this UI element.
3. The lobby card uses the new panel instead of a flat fill and adds subtle header/footer hairlines and corner notches. The existing six-role pagination remains intact.
4. The settings tab inherits the color palette: faction-coded headings and role strips, readable pale titles and state-dependent value colors.
5. Existing button click handlers, role behavior, synchronization, fonts and host-only settings are unchanged.

## Acceptance test

- Open an Among Us lobby on the target Reactor / BepInEx-compatible 2026 build.
- Confirm the small role summary renders with the new gradient and **does not overlap Customize**.
- Click the card repeatedly: impostor, crew and neutral pages wrap correctly, including factions with no enabled roles.
- As host, open **PARADOX** in lobby settings and verify the list scrolls, row arrows change settings, and labels stay inside the masked scroller.
- Switch PL/EN; check glyphs and legibility.
- Join with a second client and confirm settings remain host-authoritative.
- Leave and rejoin a lobby: no duplicated card, controls or leaking objects.
- Verify the mod also runs on low UI scaling and wide aspect ratios.

## Follow-up visual production (not yet implemented)

- Distinct illustrated role icons, with an image atlas rather than one giant generic ability icon.
- Role-reveal visual identity, ability cooldown skins and restrained status VFX.
- Accessible settings presets for low-spec PCs, reduced motion and color-blind contrast.
- Gameplay test footage/screenshots before locking spacing and animation curves.

**Build passing is not equivalent to successful game runtime verification.**
