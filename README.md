# PARADOX

Original Among Us mod project. Current PC baseline: Reactor 2.5.1 (2026 release), targeting its supported Among Us 16.0.5–17.4 range.

## Development status

The project has 40 registered roles: 15 Impostor, 15 Crewmate and 10 Neutral. The first gameplay MVP focuses on Doppelgänger, Parasite, Anomaly, Observer, Witness and the Paradox Meter.

Status legend: **Partial** = code/foundation exists but the role is not yet end-to-end playable; **Planned** = registered design slot, gameplay implementation still required.

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
| Corruptor | Impostor | Corrupts players or systems to create harmful effects. | Planned | Final mechanic, implementation and sync |
| Devourer | Impostor | Removes/consumes a victim with a specialized kill mechanic. | Planned | Final mechanic and implementation |
| Timebreaker | Impostor | Temporarily disrupts time-related gameplay. | Planned | Final mechanic, safe networking model |
| Nightmare | Impostor | Applies fear/vision disruption to selected players. | Planned | Full implementation and effects |
| Shapeshifter X | Impostor | Advanced transformation-oriented impostor role distinct from Doppelgänger. | Planned | Final unique mechanic and implementation |
| Saboteur | Impostor | Enhanced sabotage-focused role. | Planned | Sabotage extensions and balancing |
| Undertaker | Impostor | Interacts with/moves bodies to conceal evidence. | Planned | Body interaction and synchronization |
| Silencer | Impostor | Restricts a target during discussion/meeting phases. | Planned | Meeting integration and sync |
| Riftmaker | Impostor | Creates temporary rifts/portals for repositioning. | Planned | Portal system, map validation and sync |
| Observer | Crewmate | Detects/tracks role-ability activity through Observer traces. | Partial | Spatial trace visualization and runtime verification |
| Witness | Crewmate | Receives clues when reporting/examining a body. | Partial | Real body-age/activity evidence and runtime verification |
| Engineer X | Crewmate | Advanced technical/repair-oriented crewmate. | Planned | Final mechanic and implementation |
| Guardian | Crewmate | Protects another player from danger. | Planned | Protection ability, cooldown and sync |
| Chronologist | Crewmate | Uses timing/history information to investigate events. | Planned | Event history system and UI |
| Detective | Crewmate | Investigates players/events for evidence. | Planned | Evidence model, UI and balancing |
| Medic | Crewmate | Protects or medically inspects players. | Planned | Final mechanic and implementation |
| Tracker | Crewmate | Tracks a selected player's position/activity. | Planned | Tracking UI and synchronization |
| Locksmith | Crewmate | Interacts with doors/locks and map access. | Planned | Door/map integration |
| Analyst | Crewmate | Analyses match data to reveal limited information. | Planned | Data sources, UI and balancing |
| Technician | Crewmate | Gains enhanced interaction with systems/sabotages. | Planned | System hooks and balancing |
| Seer | Crewmate | Receives limited supernatural/information clues. | Planned | Final clue rules and implementation |
| Dispatcher | Crewmate | Provides team-oriented information/coordination tools. | Planned | Final mechanic, HUD and sync |
| Forensic | Crewmate | Examines bodies/scenes for stronger forensic evidence. | Planned | Death/evidence history and UI |
| Stabilizer | Crewmate | Counteracts Paradox/anomaly effects and helps control the Meter. | Planned | Meter interaction and balancing |
| Anomaly | Neutral | Raises the Paradox Meter with Reality Rupture and wins if a living Anomaly survives the 100% event. | Partial | Multiplayer/runtime verification and balance tuning |
| Forgotten | Neutral | Progresses through a hidden/forgotten identity objective. | Planned | Final objective and win condition |
| Collector | Neutral | Collects designated objectives/resources to win. | Planned | Collectible system and win condition |
| Bounty Hunter | Neutral | Receives targets/bounties and progresses by eliminating/completing them. | Planned | Target selection, win condition and sync |
| Survivor | Neutral | Wins primarily by surviving until the required end state. | Planned | Win-condition integration |
| Revenant | Neutral | Death/return-themed neutral role with a second-state mechanic. | Planned | Revival/state rules and win condition |
| Jester X | Neutral | Attempts to get voted out under its own victory rules. | Planned | Meeting/vote win-condition integration |
| Phantom | Neutral | Uses stealth/intangibility-oriented mechanics. | Planned | Final ability, visibility rules and sync |
| Opportunist | Neutral | Wins by satisfying an opportunistic end-game survival condition. | Planned | End-game win-condition integration |
| Harbinger | Neutral | Advances a dangerous Paradox-related objective toward a special victory. | Planned | Objective, Meter interaction and win condition |

## Implemented foundation

- Paradox Meter 0–100 with one-time thresholds at 25/50/75/100.
- Runtime threshold effects implemented for 25% Reality Disturbance, 50% Reality Distortion and 75% Critical Instability; the 50% and 75% events temporarily jam special role abilities.
- At 100%, the host selects and synchronizes one final event for every client: Reality Storm, Blackout or Null Field. Null Field jams special role abilities for 12 seconds.
- Meter sources include successful kills and role/anomaly actions; failed/protected murder attempts no longer count as kills.
- Reactor custom RPC foundation, handshake/client registry, role assignment and meter synchronization.
- Host-side initial role assignment for implemented Impostor, Crewmate and Neutral roles. Anomaly can occupy one vanilla crewmate slot while keeping its own neutral objective and custom win flow.
- English/Polish localization foundation.
- PARADOX STATION map architecture/skeleton.
- Match-end state reset for implemented MVP roles.
- GitHub Actions Release build producing Paradox.dll.

## Next milestones

1. Runtime-test Doppelgänger ability UX, host validation, synchronized disguise timer/cooldown and restoration.
2. Runtime-test Parasite target/ability UX, infection feedback and host-side RPC validation.
3. Runtime-test Witness/Observer HUD and replace placeholder clues/traces with real match evidence.
4. Runtime-test Anomaly neutral assignment, Reality Rupture ability and 100% custom win condition.
5. Add host settings UI for role pool, spawn chances, cooldowns, Meter and events.
6. Complete PARADOX STATION gameplay map and tasks.
7. Expand the Paradox event pool and continue implementing registered roles after runtime verification of the synchronized 100% event.
8. Harden multiplayer RPC validation and perform multi-client runtime tests.
9. Android adapter and cosmetic-only Founder/Premium features after the PC gameplay core is stable.
