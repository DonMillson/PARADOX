# PARADOX — multiplayer QA plan (0.6.0-alpha.5)

This is a **test plan**, not a record of passed runtime tests. Use a supported PC Among Us build, matching BepInEx/Reactor and the same PARADOX DLL on every client. Start with one host and two clients, then repeat with at least four players.

## Required build gate

- GitHub Actions completes **Verify role roster and RPC identifiers**, **Build**, and **Upload Paradox DLL** successfully.
- All clients load the same PARADOX version and protocol; no missing-RPC or Harmony patch errors appear in the logs.
- Host settings are synchronized and all 40 roles are visible under their correct faction.
- With all 40 roles enabled at 100%, run at least 10 consecutive matches with the same players. Confirm assignments vary (not fixed to roster/join order), no special role repeats within a faction until its eligible pool is exhausted, and each match assigns at most one Neutral role.
- Set a test role to 0% and confirm it never spawns; set it back to 100% and verify it becomes eligible. Intermediate chance values are independent eligibility rolls, not guarantees of a specific player share.

## PARADOX STATION solo lobby smoke test

- Open a private lobby as the only player. Press F7 **before** starting a match; station floor, walls, rooms and consoles should be displayed, with the camera centered on Arrival Bay. Walk with WASD and use F8 beside a yellow console.
- Press F7 to exit. Confirm the player returns to the previous lobby position and the scene is removed.
- Reenter, then have another player join. The solo preview must close safely; no custom station mode RPC should have been sent.
- Solo preview is intentionally not a playable one-player Among Us game: roles, votes, task victory and standard game systems cannot be fully tested here.

## PARADOX STATION walkable scene alpha QA

- With host + two same-build PC clients start a match on a vanilla map, press F7 as host. Verify all clients show the procedural room/corridor floor and barriers and all players are placed at Arrival Bay. F7 again restores each player to their prior position.
- Attempt to use F7 in lobby, or with a PARADOX-incompatible client: station must not start.
- Move through all 12 rooms; walls must block movement and corridors must be traversable on host and clients. Verify no physics-layer or camera errors.
- Stand at a yellow console and press F8 three times with short intervals. Verify host validates range and 12-room shared console completion state stays consistent.
- Test the 3 orange vent pairs using F9 as an Impostor. Confirm crew cannot warp, host checks proximity and enforces the 3-second cooldown, and player positions synchronize after teleport.
- Trigger an emergency meeting during station mode. Host should close the experimental scene and return players; vanilla meeting must work.
- Disconnect, restart or finish the match; the scene must not leave colliders behind. Validate both with and without the F6 blueprint overlay open.
- Prototype does NOT replace ShipStatus; vanilla tasks, impostor vent travel, and selectable map integration remain unimplemented.

## PARADOX STATION blueprint QA (not a playable map)

- Press F6 in the lobby and in a running match, verify all 12 translated rooms, green spawn, 16 corridor links, 3 dashed vent links and 12 planned-task markers.
- Open a meeting with the viewer visible and verify it is hidden; close the meeting and use F6 to close the viewer.
- Disconnect or end a match; ensure no leftover blueprint or Unity/TMP errors. The station is not registered as an Among Us selectable level.

## Lobby role card regression

- With all roles enabled, confirm every Impostor, Crewmate and Neutral is reachable by clicking through the paginated summary (six roles per page, one compact header and credit).
- Confirm no role names extend beyond the cyan role card boundary, and no PARADOX credit overlaps the native Customize button at 16:9 and 16:10 resolutions.
- Toggle role enabled settings; confirm pagination adapts without empty or duplicate role pages.

## Match/lobby lifecycle

- Open/close settings and switch Polish/English; confirm role names, abilities and notifications translate.
- Activate Doppelgänger and Illusionist disguises, then call an emergency meeting before their timers expire. Confirm immediate restoration on all clients and again at match end; the affected players must not retain copied outfits into the next round.
- Start consecutive matches without restarting the game; confirm cooldowns, role assignments, evidence, traces, overlays and winners reset cleanly.
- Disconnect/rejoin a client during lobby and while in-game; validate graceful recovery or explicit incompatibility handling.
- Test meetings, skips, tie votes, body reports, sudden deaths and game endings in succession.

## Role and interaction test matrix

| Scenario | Expected outcome |
|---|---|
| Parasite infects, then Medic scans | Cure removes infection before lethal timer |
| Guardian protects vs ordinary murder | One successful protection; no death or +10 meter |
| Guardian protects vs Devourer/Bounty Hunter | Protected target is not killed and cannot yield a solo win |
| Phantom phases during murder | Murder is blocked; one escape credit; phase ends |
| Phantom escapes a second **separate** attempt | Phantom solo win with Phantom as winner on all clients |
| Revenant dies and returns | Returns once at the recorded death position; body removed; valid alive state |
| Revenant is killed again | No third life; the second death is recorded as new evidence |
| Corruptor debuffs Stabilizer/Anomaly and other roles | Ability unusable throughout corruption; usable after expiry |
| Timebreaker freezes during Puppeteer control | No premature movement restoration or lasting immobilization |
| Illusionist / Doppelgänger / Shapeshifter X | Name/appearance returns correctly after expiry, death or meeting |
| Riftmaker anchors and teleports | Host and clients agree on position; no invalid map placement |
| Undertaker carries body, Cleaner/Devourer removes bodies | No orphaned bodies, double removal or duplicated evidence |
| Blackmailer targets before meeting | Marked player's outgoing text is blocked only in the next meeting |
| Silencer targets before meeting | Marked vote is rejected on both host and clients |
| Jester X is voted out | Exactly the Jester wins; a skip or tie does not trigger a win |
| Forgotten remembers two unique bodies | Each body counts once; solo win shown on each client |
| Collector samples three unique living players | Repeated target does not count; correct solo winner |
| Bounty Hunter kills the designated target | Target validation and solo win agree across clients |
| Harbinger completes omens at required Meter | Correct win timing and winner display |
| Anomaly reaches 100% | Synchronized final event and own win timing |
| Survivor lives to normal match end | Joins winners only once |
| Opportunist survives with Meter at least 50 | Joins winners only if eligible |
| Witness / Observer / Detective / Forensic / Tracker | Traces, body timing and clues match actual host events |
| Engineer X / Stabilizer / Technician / Saboteur | Meter gains, reductions and shield handling match on all clients |

## Paradox Meter

- Confirm +10 on each new successful death, **not** on protected attempts or repeated death callbacks.
- Confirm expected ability/sabotage contributions; verify Stabilizer/Engineer X reductions are synchronized.
- Trigger 25%, 50%, 75% and 100% effects and verify they occur once, with matching duration and visuals.
- Trigger an end game during an effect; no overlay or blocked ability state survives the next match.

## Acceptance criteria

A feature should remain marked **Partial** until at least host + two clients reproduce its expected outcome, a meeting/endgame transition is tested, and logs show no runtime exceptions. Track failures with exact Among Us version, PARADOX build SHA, logs and reproduction steps.
