# PARADOX — multiplayer QA plan (0.5.0-beta.1)

This is a **test plan**, not a record of passed runtime tests. Use a supported PC Among Us build, matching BepInEx/Reactor and the same PARADOX DLL on every client. Start with one host and two clients, then repeat with at least four players.

## Required build gate

- GitHub Actions completes **Verify role roster and RPC identifiers**, **Build**, and **Upload Paradox DLL** successfully.
- All clients load the same PARADOX version and protocol; no missing-RPC or Harmony patch errors appear in the logs.
- Host settings are synchronized and all 40 roles are visible under their correct faction.

## Match/lobby lifecycle

- Open/close settings and switch Polish/English; confirm role names, abilities and notifications translate.
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
