# PARADOX CrewUI — art direction, diagnostic workflow

This is the **launcher/installer presentation redesign** for PARADOX
`0.7.0-dev.6`, not a claim that the mod is runtime-certified.

## Visual direction

- Original black-outlined spacesuits, star field and ship bridge rendered in
  Tkinter Canvas; entirely source/vector based with no copyrighted sprites,
  no external image CDN and no automatically added watermark.
- Dark metal bulkheads, red impostor, cyan visor, yellow/black safety tape and
  physical-feeling buttons, inspired by the playful Among Us spaceship UI.
- Large `PARADOX AMONG US` sign with `BY DONMILLSON` credit.
- Replaced cyan/purple neon office-style dashboard cards with thick dark
  strokes, warm lettering and simple stage labels.
- All game interactions remain intact: catch the impostor five times, enter
  the arithmetic access code, then install through the verified backend.
- Resizing the launcher redraws vector artwork without stacking duplicates.

## What happens after installation

The installer **cannot certify game play**: it copies verified BepInEx,
Reactor and `Paradox.dll` and inspects the runtime log *after* Among Us
starts. The initial successful file-copy message is **not** a runtime success.

The launcher's first screen now offers:

- **SPRAWDŹ MODY** — verify required files and plugin markers.
- **OTWÓRZ LOG** — open the real BepInEx startup log.
- **KOPIUJ RAPORT DIAGNOSTYCZNY** — copy a short, safe summary with
  no arbitrary raw log lines or player/session secrets.

**Where the PARADOX settings tab actually belongs:**
Enter Among Us → create your own private room **as HOST** → open the
room/game options → switch to the **PARADOX** tab. It is deliberately
not in the game's main menu and not visible to ordinary clients.

If there is no `PARADOX SETTINGS TAB READY` event in the BepInEx log,
the host UI has not been verified, and the report should never claim it is.

## Release and QA

The public installable build uses the distinct `CrewUI` release identifier
but keeps PARADOX DLL/protocol `0.7.0-dev.6`; no incompatible multiplayer
protocol bump just for a cosmetic launcher redesign.

Headless CI tests cover illustration redraw/tag integrity, installed-file
health, automatic dependency downloads and GUI syntax. The real Among Us
runtime on the user's PC requires a separate test and examination of its
`BepInEx/LogOutput.log` (or `.txt`) file. **Never announce mod compatibility
only from a green GitHub Actions build.**
