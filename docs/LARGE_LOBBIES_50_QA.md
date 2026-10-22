# PARADOX: experimental 16–50 player lobby support

## Status: NOT a finished 50-player multiplayer release

PARADOX now contains an **opt-in, fail-closed 15–50 player create-room capacity adapter**.
This first milestone only enables the local native **Create Game** player-count picker
on a manually selected **custom/modded server**. It does not modify the official
Innersloth server and does **not** guarantee that such a server accepts 50
connections or that a 50-person match can complete.

The vanilla 15 player experience remains the default.

## How to opt in (host only, Windows PC)

1. Install and start PARADOX on Windows. Exit the game.
2. Edit `BepInEx/config/gg.paradox.mod.cfg` (exact generated filename depends on BepInEx's plugin ID).
3. In section `[ExperimentalLargeLobby]`, set:
   ```ini
   Enable = true
   MaxPlayers = 50
   ```
   `MaxPlayers` can be between 15 and 50. For incremental tests, use 20,
   then 30, 40 and finally 50.
4. Set up a **separate, trustworthy, compatible custom Among Us server** that
   explicitly supports expanded lobbies on the game's target version. Select
   that custom region in the Among Us server selector.
5. Open **Create Game**. PARADOX tries to expand the standard player-count
   picker to the requested cap. If the region is official or unknown, it leaves
   the vanilla picker untouched.
6. Require all clients to run matching PARADOX versions and verify host/client
   logs before increasing test load.

No hardcoded third-party server address is injected by PARADOX.
No official server validation is bypassed, and no automatic client installation
of an external custom server is included.

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
- [ ] Implement and verify meeting voting card **pagination/search** for up to
      50 players, including Swapper arrows, chat, tie votes and exile.
- [ ] Paginate the shapeshifter/target picker and vitals display.
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
