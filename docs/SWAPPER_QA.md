# Swapper (Zamieniacz) — PARADOX 0.7.0-dev.2

**Faction:** Crewmate. **State:** compiled MVP; in-game/multiplayer test required.

## How it works

1. During a meeting, Swapper sees a cyan two-way-arrow button next to each eligible living player card.
2. Click one arrow, then another. Selected arrows become violet. Alternatively (Free Chat only), enter `/swap 2 5` using player IDs.
3. The host checks that the source really has the Swapper role, is alive, and the two targets are separate living players other than Swapper.
4. Once all living participants have voted, the host exchanges all votes **cast for** A and **cast for** B. Skip and missing votes remain unchanged; vanilla meeting resolution determines exiled player and ties.
5. If the swap **caused a Crewmate to be exiled**, Swapper is eliminated after the meeting. If the exiled player is an Impostor, Swapper is safe. No change in the actual exiled person means no penalty.
6. One accepted pair per meeting. You cannot select yourself, a dead/disconnected player or the same player twice.

## Technical integration

- Adds `RoleId.Swapper = 40` to the Crewmate roster, preserving prior numeric IDs. Neutral numbering explicitly remains at 30.
- Custom RPC 48 for Swapper selection acknowledgment and punishment. Protocol 43; all clients need the same build.
- Native vote panel icon reuses a game vote-button prefab, replacing the image with a locally generated two-arrow sprite; no asset bundle.
- `MeetingHud.CheckForEndVoting` host prefix changes `PlayerVoteArea.VotedForId` only once after all living players have voted.
- `VotingComplete` observes the actual result, and a host-driven RPC applies punishment after the meeting.
- Standalone CI tests confirm swaps, skip voting, ties, no-vote markers and whether the winner was changed.
- Built-in menu role toggles, spawn chance and PL/EN descriptions remain available.

## Acceptance tests in real Among Us (NOT yet passed)

- Two modded players plus host: verify card icons appear only for Swapper.
- Select two players and confirm color feedback; voting must remain available on those cards.
- Confirm host vote tally is redirected as expected and all clients see identical outcome.
- With swapped Crewmate exile, verify Swapper dies exactly once and nobody else is harmed.
- With swapped Impostor exile, verify Swapper survives.
- Confirm skip/ties and partial/time-expired votes; a timed-out vote meeting is a known edge case.
- Confirm dead/disconnected target, Swapper death, chat disabled/Quick Chat and invalid data from malicious clients fail closed.
- Test repeated meetings, screen resolutions and transition to endgame.

**Compatibility note:** compiled against AmongUs.GameLibs.Steam 2026.9.29 and Reactor 2.5.1. Compilation does not prove compatibility with all Among Us versions, including the current 19.x release.
