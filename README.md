# PARADOX

Original Among Us mod project. Current PC baseline: Reactor 2.5.1 (2026 release), targeting its supported Among Us 16.0.5–17.4 range.

## Development status

The project has 40 registered roles: 15 Impostor, 15 Crewmate and 10 Neutral. Current gameplay MVP roles include Doppelgänger, Parasite, Cleaner, Devourer, Corruptor, Nightmare, Riftmaker, Observer, Witness, Guardian, Chronologist, Detective, Medic, Tracker, Analyst, Seer, Forensic, Stabilizer, Anomaly, Bounty Hunter, Survivor and Opportunist.

Status legend: **Partial** = code/foundation exists but the role is not yet end-to-end runtime-verified; **Planned** = registered design slot, gameplay implementation still required.

Current Paradox threshold runtime: 25%, 50%, 75% and a host-selected synchronized randomized 100% final event are implemented in code; in-game runtime verification is still required.

## Role table

| Role | Faction | Intended gameplay / ability | Status | Still required |
|---|---|---|---|---|
| Doppelgänger | Impostor | Copies another player's appearance temporarily, then restores the original outfit. | Partial | Final multiplayer/runtime verification and balance tuning |
| Parasite | Impostor | Infects a target; after the infection timer completes, the victim is killed. | Partial | Multiplayer/runtime verification and balance tuning |
| Puppeteer | Impostor | Temporarily manipulates another player's actions/movement. | Planned | Full implementation and synchronization |
| Cleaner | Impostor | Removes a dead body to deny report/evidence. | Partial | Runtime verification and balance tuning |
| Blackmailer | Impostor | Temporarily prevents a selected player from communicating normally. | Planned | Full implementation, meeting integration |
| Illusionist | Impostor | Creates deceptive visual information/decoys. | Planned | Full implementation and client sync |
| Corruptor | Impostor | Blocks a nearby player’s special role ability for 10 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Devourer | Impostor | Consumes a nearby victim and removes the resulting body. | Partial | Multiplayer/runtime verification and balance tuning |
| Timebreaker | Impostor | Temporarily disrupts time-related gameplay. | Planned | Final mechanic, safe networking model |
| Nightmare | Impostor | Haunts a nearby player with a 7-second synchronized fear/vision distortion. | Partial | Multiplayer/runtime verification and balance tuning |
| Shapeshifter X | Impostor | Advanced transformation-oriented impostor role distinct from Doppelgänger. | Planned | Final unique mechanic and implementation |
| Saboteur | Impostor | Enhanced sabotage-focused role. | Planned | Sabotage extensions and balancing |
| Undertaker | Impostor | Interacts with/moves bodies to conceal evidence. | Planned | Body interaction and synchronization |
| Silencer | Impostor | Restricts a target during discussion/meeting phases. | Planned | Meeting integration and sync |
| Riftmaker | Impostor | Places a personal anchor and warps back to it on the next use. | Partial | Multiplayer/runtime verification and map-edge validation |
| Observer | Crewmate | Detects/tracks role-ability activity through Observer traces. | Partial | Spatial trace visualization and runtime verification |
| Witness | Crewmate | Receives clues when reporting/examining a body. | Partial | Real body-age/activity evidence and runtime verification |
| Engineer X | Crewmate | Advanced technical/repair-oriented crewmate. | Planned | Final mechanic and implementation |
| Guardian | Crewmate | Protects another player from one murder for 10 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Chronologist | Crewmate | Reads the match timeline and reports how long ago the latest recorded death occurred. | Partial | Multiplayer/runtime verification and balance tuning |
| Detective | Crewmate | Scans a nearby player for violent activity linked to deaths from the last 30 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Medic | Crewmate | Scans nearby players and cures active Parasite infections. | Partial | Multiplayer/runtime verification and balance tuning |
| Tracker | Crewmate | Tracks a selected player’s live position for 18 seconds. | Partial | Multiplayer/runtime verification and balance tuning |
| Locksmith | Crewmate | Interacts with doors/locks and map access. | Planned | Door/map integration |
| Analyst | Crewmate | Reads limited live telemetry: alive players, recorded deaths, active ability traces and Paradox Meter. | Partial | Multiplayer/runtime verification and balance tuning |
| Technician | Crewmate | Gains enhanced interaction with systems/sabotages. | Planned | System hooks and balancing |
| Seer | Crewmate | Reads whether a nearby player has a fresh special-ability trace. | Partial | Multiplayer/runtime verification and balance tuning |
| Dispatcher | Crewmate | Provides team-oriented information/coordination tools. | Planned | Final mechanic, HUD and sync |
| Forensic | Crewmate | Examines bodies for real death age and recent ability residue. | Partial | Multiplayer/runtime verification and balance tuning |
| Stabilizer | Crewmate | Reduces the Paradox Meter by up to 10 per use. | Partial | Multiplayer/runtime verification and balance tuning |
| Anomaly | Neutral | Raises the Paradox Meter with Reality Rupture and wins if a living Anomaly survives the 100% event. | Partial | Multiplayer/runtime verification and balance tuning |
| Forgotten | Neutral | Progresses through a hidden/forgotten identity objective. | Planned | Final objective and win condition |
| Collector | Neutral | Collects designated objectives/resources to win. | Planned | Collectible system and win condition |
| Bounty Hunter | Neutral | Receives a synchronized bounty target and wins immediately by eliminating that exact target. | Partial | Multiplayer/runtime verification and balance tuning |
| Survivor | Neutral | Joins the winners if still alive when the match ends. | Partial | Multiplayer/runtime verification and end-screen presentation |
| Revenant | Neutral | Death/return-themed neutral role with a second-state mechanic. | Planned | Revival/state rules and win condition |
| Jester X | Neutral | Attempts to get voted out under its own victory rules. | Planned | Meeting/vote win-condition integration |
| Phantom | Neutral | Uses stealth/intangibility-oriented mechanics. | Planned | Final ability, visibility rules and sync |
| Opportunist | Neutral | Joins the winners if alive at match end while the Paradox Meter is at least 50%. | Partial | Multiplayer/runtime verification and balance tuning |
| Harbinger | Neutral | Advances a dangerous Paradox-related objective toward a special victory. | Planned | Objective, Meter interaction and win condition |

## Implemented foundation

- Paradox Meter 0–100 with one-time thresholds at 25/50/75/100.
- Runtime threshold effects implemented for 25% Reality Disturbance, 50% Reality Distortion and 75% Critical Instability; the 50% and 75% events temporarily jam special role abilities.
- At 100%, the host selects and synchronizes one final event for every client: Reality Storm, Blackout or Null Field. Null Field jams special role abilities for 12 seconds.
- Meter sources include successful kills and role/anomaly actions; failed/protected murder attempts no longer count as kills.
- Devourer can perform a synchronized consume kill and remove the resulting body.
- Corruptor can temporarily disable a nearby player’s special role ability.
- Nightmare can apply a synchronized 7-second fear/vision distortion to a nearby player.
- Riftmaker can place a synchronized personal anchor and warp back to it.
- Guardian can place a synchronized one-hit protection on a nearby living player.
- Chronologist can query the authoritative death timeline for the age of the latest death.
- Detective can scan nearby players for recent violent activity based on authoritative death records.
- Medic can scan nearby living players and remove an active Parasite infection.
- Tracker can lock onto a nearby living player and display live distance for 18 seconds.
- Analyst can read limited host-authoritative live match telemetry without identifying players.
- Seer can inspect a nearby player for a fresh special-ability trace without revealing the exact role.
- Forensic can examine a nearby body using authoritative death-time evidence and recent ability traces.
- Stabilizer can reduce the Paradox Meter by up to 10 with a synchronized cooldown.
- Reactor custom RPC foundation, handshake/client registry, role assignment and meter synchronization.
- Host-side initial role assignment for implemented Impostor, Crewmate and Neutral roles. Anomaly, Bounty Hunter, Survivor or Opportunist can occupy the neutral slot while keeping their own neutral objective/win flow.
- English/Polish localization foundation.
- PARADOX STATION map architecture/skeleton.
- Match-end state reset for implemented MVP roles.
- GitHub Actions Release build producing Paradox.dll.

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
