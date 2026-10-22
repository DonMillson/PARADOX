# PARADOX: experimental 16–50 player lobby support

## Status: NOT a finished 50-player multiplayer release

PARADOX now contains an **opt-in, fail-closed 15–50 player create-room capacity adapter**.
This first milestone only enables the local native **Create Game** player-count picker
on a manually selected **custom/modded server**. It does not modify the official
Innersloth server and does **not** guarantee that such a server accepts 50
connections or that a 50-person match can complete.

The vanilla 15 player experience remains the default.

## How to opt in (host only, Windows PC, dev.5)

1. Download the **PARADOX AMONG US Online Installer dev.5**.
2. At installation stage, check **Eksperymentalne duże lobby** and choose
   `15 / 20 / 30 / 40 / 50` in the dropdown; leave unchecked for vanilla 15.
3. The installer saves the selection into the correct BepInEx configuration
   file **automatically**, with a backup. No manual .cfg editing is required.
4. Set up and select a **separate, compatible custom Among Us server** capable
   of hosting 16–50 players. On official/unknown regions the cap stays vanilla.
5. Open **Create Game**. PARADOX tries to expand the native player-count picker
   to the requested max. For incremental tests start at 20, then 30/40/50.
6. Use the same PARADOX dev.5 / protocol 45 on ALL clients, and retain logs.

Advanced manual alternative: set `Enable = true` and `MaxPlayers = 50`
under `[ExperimentalLargeLobby]` in `BepInEx/config/gg.paradox.mod.cfg`.

No hardcoded third-party server address is injected by PARADOX.
No official server validation is bypassed, and no automatic client installation
of an external custom server is included.

## Experimental voting layout in code

- Meeting voting cards are split into 15-card pages (50 players = 4 pages).
- PageUp/PageDown and Left/Right arrows switch pages.
- **Vitals** and **Shapeshifter targets** also have 15-slot paging overlays.
  These are client-side presentation changes; actual targets/votes remain native.
  Runtime HUD positions and click hitboxes must still be verified.
- A small page counter is added to the meeting timer; Swapper voting markers
  remain attached to their native player cards.
- This layout logic and the last partially filled page have unit tests. It
  still needs a real 50-player Among Us run to confirm mouse hitboxes,
  voting/ejection behavior and text placements.

## Tests completed by CI

- Opt-in disabled -> 15 max.
- Official/unknown region -> 15 max.
- Custom-server gate enabled -> selectable 15–50 max policy.
- Values below 15 and above 50 safely clamped.
- Compiles against pinned Among Us `2026.9.29` game libraries,
  Reactor `2.5.1-ci.400` and BepInEx package references.

## CRITICAL remaining blockers before advertising "50 players supported"

- [ ] Runtime verify Harmony patch `CreateGameOptions.Show` on Among Us 19.0.0.
- [ ] Confirm external custom server accepts 16/20/30/40/50 clients without
      packet drops/disconnects and that region selection survives restarts.
- [ ] Verify the **experimental** 15-card meeting paging in the real game,
      including Swapper arrows, chat, tie votes, exile and native button input.
      Basic paging code exists, but runtime QA is not complete.
- [ ] Runtime-test the now-implemented Vitals/Shapeshifter pages, confirm
      inactive views clean up correctly, and verify input with 50 real clients.
- [ ] Review player count assumptions, byte ID sentinels (253–255),
      impostor counts, role assignment and network RPC rates for 50 players.
- [ ] Load/stress test host: joining/leaving, 50-player role assignment,
      kill/body reporting, sabotage, custom neutral wins, emergency meetings,
      reconnects and latency/bandwidth.
- [ ] Balancing for 50: number of impostors, tasks, kill cooldowns and
      meeting length, especially custom map capacity.
- [ ] Test at least 50 separate clients on the supported server or use
      realistic, protocol-correct, instrumented load clients.
- [ ] Only after green runtime/QA results make the control on by default
      or advertise 50-player multiplayer as released.

Reference inspirations, not copied:
[Overloaded](https://github.com/All-Of-Us-Mods/Overloaded) and
[AleLuduMod](https://github.com/townofus-pl/AleLuduMod). Both document
official-server restrictions; their own builds do not verify PARADOX v19.
