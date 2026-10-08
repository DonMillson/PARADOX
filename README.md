# PARADOX

Original Among Us mod project. Current PC baseline: Reactor 2.5.1 (2026 release), targeting its supported Among Us 16.0.5–17.4 range.

## Development status

**Current testing milestone: PARADOX 0.6.0-alpha.6.** The project has 40 registered roles with gameplay MVP/foundation: 15 Impostor, 15 Crewmate and 10 Neutral roles. Compilation and manifest checks are automated, but this is **not yet a multiplayer-verified stable release**.

Status legend: **Partial** = code/foundation exists but the role is not yet end-to-end runtime-verified. All 40 registered roles now have an MVP/foundation; none remain Planned.

Current Paradox threshold runtime: 25%, 50%, 75% and a host-selected synchronized randomized 100% final event are implemented in code; in-game runtime verification is still required.

## Role table

| Role | Faction | Intended gameplay / ability | Status | Still required |
|---|---|---|---|---|
| Doppelgänger | Impostor | Copies another player's appearance temporarily, then restores the original outfit. | Partial | Final multiplayer/runtime verification and balance tuning |
| Parasite | Impostor | Infects a target; after the infection timer completes, the victim is killed. | Partial | Multiplayer/runtime verification and balance tuning |
| Puppeteer | Impostor | Turns a nearby player into a marionette, freezing their own input and dragging them behind the Puppeteer for 6 seconds. | Partial | Multiplayer/runtime verification and movement edge cases |
| Cleaner | Impostor | Removes a dead body to deny report/evidence. | Partial | Runtime verification and balance tuning |
| Blackmailer | Impostor | Marks a nearby player and blocks their outgoing chat during the next meeting. | Partial | Multiplayer/runtime verification and quick-chat edge cases |
| Illusionist | Impostor | Temporarily forces a nearby player to appear as another living player. | Partial | Multiplayer/runtime verification and appearance-conflict edge cases |
| Corruptor | Impostor | Blocks a nearby player’s special role ability for 10 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Devourer | Impostor | Consumes a nearby victim and removes the resulting body. | Partial | Multiplayer/runtime verification and balance tuning |
| Timebreaker | Impostor | Places a nearby player in synchronized movement stasis for 4 seconds. | Partial | Multiplayer/runtime verification and movement-state edge cases |
| Nightmare | Impostor | Haunts a nearby player with a 7-second synchronized fear/vision distortion. | Partial | Multiplayer/runtime verification and balance tuning |
| Shapeshifter X | Impostor | Swaps the visible identities of the Shapeshifter and a nearby player for 12 seconds. | Partial | Multiplayer/runtime verification and appearance edge cases |
| Saboteur | Impostor | Arms the next real sabotage to add +4 extra Paradox instability. | Partial | Multiplayer/runtime verification and balance tuning |
| Undertaker | Impostor | Picks up a nearby body, carries it and drops it elsewhere. | Partial | Multiplayer/runtime verification and body-position edge cases |
| Silencer | Impostor | Marks a nearby player and rejects their vote during the next meeting. | Partial | Multiplayer/runtime verification and meeting UI feedback |
| Riftmaker | Impostor | Places a personal anchor and warps back to it on the next use. | Partial | Multiplayer/runtime verification and map-edge validation |
| Observer | Crewmate | Detects/tracks role-ability activity through Observer traces. | Partial | Spatial trace visualization and runtime verification |
| Witness | Crewmate | Receives clues when reporting/examining a body. | Partial | Real body-age/activity evidence and runtime verification |
| Engineer X | Crewmate | Uses a host-authoritative Emergency Stabilize ability to reduce dangerous Paradox instability. | Partial | Multiplayer/runtime verification and map-specific repair extensions |
| Guardian | Crewmate | Protects another player from one murder for 10 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Chronologist | Crewmate | Reads the match timeline and reports how long ago the latest recorded death occurred. | Partial | Multiplayer/runtime verification and balance tuning |
| Detective | Crewmate | Scans a nearby player for violent activity linked to deaths from the last 30 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Medic | Crewmate | Scans nearby players and cures active Parasite infections. | Partial | Multiplayer/runtime verification and balance tuning |
| Tracker | Crewmate | Tracks a selected player’s live position for 18 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Locksmith | Crewmate | Opens the nearest closed door within range using a synchronized bypass. | Partial | Multiplayer/runtime verification and map-specific door edge cases |
| Analyst | Crewmate | Reads limited live telemetry: alive players, recorded deaths, active ability traces and Paradox Meter. | Partial | Multiplayer/runtime verification and balance tuning |
| Technician | Crewmate | Deploys a temporary system shield that prevents sabotage from adding +4 Paradox instability. | Partial | Multiplayer/runtime verification and balance tuning |
| Seer | Crewmate | Reads whether a nearby player has a fresh special-ability trace. | Partial | Multiplayer/runtime verification and balance tuning |
| Dispatcher | Crewmate | Runs a synchronized status sweep reporting living players, deaths and current Paradox instability. | Partial | Multiplayer/runtime verification and balance tuning |
| Forensic | Crewmate | Examines bodies for real death age and recent ability residue. | Partial | Multiplayer/runtime verification and balance tuning |
| Stabilizer | Crewmate | Reduces the Paradox Meter by up to 10 per use. | Partial | Multiplayer/runtime verification and balance tuning |
| Anomaly | Neutral | Raises the Paradox Meter with Reality Rupture and wins if a living Anomaly survives the 100% event. | Partial | Multiplayer/runtime verification and balance tuning |
| Forgotten | Neutral | Recovers memories from two unique dead bodies and wins immediately after completing the memory objective. | Partial | Multiplayer/runtime verification and balance tuning |
| Collector | Neutral | Collects unique samples from three different living players and wins immediately after completing the collection. | Partial | Multiplayer/runtime verification and balance tuning |
| Bounty Hunter | Neutral | Receives a synchronized bounty target and wins immediately by eliminating that exact target. | Partial | Multiplayer/runtime verification and balance tuning |
| Survivor | Neutral | Joins the winners if still alive when the match ends. | Partial | Multiplayer/runtime verification and end-screen presentation |
| Revenant | Neutral | Returns once after death, then wins alone by surviving 20 seconds in the second life. | Partial | Multiplayer/runtime verification and end-condition edge cases |
| Jester X | Neutral | Wins immediately when voted out by the meeting. | Partial | Multiplayer/runtime verification and end-screen timing |
| Phantom | Neutral | Phases for 8 seconds, becoming hidden and immune to murder; wins after evading two murder attempts while phased. | Partial | Multiplayer/runtime verification and visibility edge cases |
| Opportunist | Neutral | Joins the winners if alive at match end while the Paradox Meter is at least 50%. | Partial | Multiplayer/runtime verification and balance tuning |
| Harbinger | Neutral | Invokes three Omens, pushes the Paradox Meter and wins while alive once the Meter reaches at least 75%. | Partial | Multiplayer/runtime verification and balance tuning |

## Implemented foundation

- Paradox Meter 0–100 with one-time thresholds at 25/50/75/100.
- Runtime threshold effects implemented for 25% Reality Disturbance, 50% Reality Distortion and 75% Critical Instability; the 50% and 75% events temporarily jam special role abilities.
- At 100%, the host selects and synchronizes one final event for every client: Reality Storm, Blackout or Null Field. Null Field jams special role abilities for 12 seconds.
- Meter sources include successful kills and role/anomaly actions; failed/protected murder attempts no longer count as kills.
- Devourer can perform a synchronized consume kill and remove the resulting body.
- Corruptor can temporarily disable a nearby player’s special role ability.
- Saboteur can arm the next real sabotage for an extra +4 Paradox Meter; Technician shielding can absorb the overloaded instability.
- Undertaker can carry and relocate a synchronized dead body.
- Timebreaker can lock a nearby target in a synchronized 4-second temporal stasis.
- Puppeteer can temporarily seize a nearby player's movement and drag the victim behind the controller.
- Illusionist can temporarily replace a target’s visible identity with another living player’s appearance.
- Shapeshifter X can temporarily swap two players’ visible identities instead of performing a one-way copy.
- Nightmare can apply a synchronized 7-second fear/vision distortion to a nearby player.
- Riftmaker can place a synchronized personal anchor and warp back to it.
- Guardian can place a synchronized one-hit protection on a nearby living player.
- Chronologist can query the authoritative death timeline for the age of the latest death.
- Detective can scan nearby players for recent violent activity based on authoritative death records.
- Medic can scan nearby living players and remove an active Parasite infection.
- Tracker can lock onto a nearby living player and display live distance for 18 seconds.
- Analyst can read limited host-authoritative live match telemetry without identifying players.
- Locksmith can open the nearest closed door using a host-validated synchronized door index.
- Technician can deploy a synchronized sabotage-instability shield; sabotage now has a real host-side +4 Paradox Meter hook.
- Seer can inspect a nearby player for a fresh special-ability trace without revealing the exact role.
- Forensic can examine a nearby body using authoritative death-time evidence and recent ability traces.
- Stabilizer can reduce the Paradox Meter by up to 10 with a synchronized cooldown.
- Reactor custom RPC foundation, handshake/client registry, role assignment and meter synchronization.
- Host can now tune Paradox Meter gains for kills, sabotages, role abilities and Anomaly from the native lobby tab. Values persist and synchronize via host-verified RPC 43 (protocol v40).
- Host-side initial role assignment covers all 40 registered roles. Eligible Impostor/Crewmate role pools and players are shuffled every match instead of following a fixed roster order. One enabled Neutral role is selected for the neutral slot and keeps its own objective/win flow. A role's spawn chance controls its eligibility roll, not the percentage of players who receive it.
- English/Polish localization foundation.
- Lobby UI: role summary now shows six roles per page, cycles through faction pages with a click, and carries the PARADOX by DonMillson credit without a separate overlay obscuring Customize.
- **Solo map test:** while alone as the host in a lobby, click the cyan **TESTUJ MAPĘ / TEST STATION** button below the PARADOX role card (or press **F7**) to explore PARADOX STATION without starting a match. WASD moves, F8 advances yellow prototype consoles and F7 returns to the lobby. The solo preview is local-only (no station RPCs), moves the lobby camera to the station, restores the old view on exit, and closes if another player joins. This is a geometry test, not a solo Among Us match.
- PARADOX STATION experimental WALKABLE SCENE: during a match, host presses **F7** to create a procedural 12-room station with corridors, walls, labels, and 12 yellow console objectives (F8 near a console advances it; 3 steps each), plus F9 impostor-only host-validated travel between the three orange vent pairs. Host teleports players and can return them with F7; state/task progress is synchronized by host RPC. **F6** still shows the bilingual blueprint. This is an opt-in test arena within a vanilla ShipStatus, not a separately registered playable Among Us map; vanilla tasks, standard vent objects/animations and official map selection are not yet integrated. See [Station milestones](docs/PARADOX_STATION.md).
- Match-end state reset for implemented MVP roles.
- GitHub Actions checks all 40 registered roles/RPC identifiers and the station room graph, then compiles and packages Paradox.dll.

## Next milestones

1. Runtime-test Doppelgänger ability UX, host validation, synchronized disguise timer/cooldown and restoration.
2. Runtime-test Parasite, Guardian, Medic and Stabilizer interactions in multiplayer.
3. Runtime-test Witness/Observer HUD and replace placeholder clues/traces with real match evidence.
4. Runtime-test Anomaly neutral assignment, Reality Rupture ability and 100% custom win condition.
5. Add host settings UI for role pool, spawn chances, cooldowns, Meter and events.
6. Complete PARADOX STATION gameplay map and tasks.
7. Expand the Paradox event pool and continue implementing registered roles.
8. Harden multiplayer RPC validation and perform multi-client runtime tests.
9. Android adapter and cosmetic-only Founder/Premium features after the PC gameplay core is stable.

## Testing build (Windows)

1. In GitHub Actions, open the latest green **Build PARADOX** workflow run and download its `PARADOX-v0.6.0-alpha.6` artifact.
2. Use a matching supported Among Us PC build with the required BepInEx 6 IL2CPP and Reactor 2.5.1 dependencies installed. Place `Paradox.dll` in `BepInEx/plugins/`.
3. All participating clients should use the same mod build and protocol (currently v40). Avoid mixing different PARADOX builds in one room.
4. Before public release, follow [Multiplayer QA plan](docs/MULTIPLAYER_QA.md) and capture client/host logs for any desync, role or meeting crash.

The DLL being compiled by GitHub Actions **does not** establish that gameplay works correctly in a live Among Us match. Map content, assets and end-to-end multiplayer testing remain separate milestones.
