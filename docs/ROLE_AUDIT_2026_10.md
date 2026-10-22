# PARADOX role audit — 22 Oct 2026

## External references reviewed (not copied)

- [Town of Us: Reactivated](https://github.com/amongusclub/Town-Of-Us-Reactivated) — role coverage, modifiers, per-game compatibility and meeting edge cases.
- [Town of Us: Mira](https://au-avengers.github.io/docs.toum.gg/) — maintained role docs, tuning, draft-mode player choice and visual quality.
- [The Other Roles](https://github.com/takada-tsuma/TheOtherRoles-Ver.Takada) — meeting UX, Swapper charges/confirmation and settings principles.
- [Endless Host Roles](https://github.com/Gurge44/EndlessHostRoles) — example of ballot-vs-voter separation, meeting state and native PlayerVoteArea/VoterState; no third-party source copied into PARADOX.

Our production source, not external source, remains the source of truth for PARADOX implementation.

## Changes in dev.3

- **Sheriff** (Crewmate): host decides shot result. Impostor target: shot at the target; anyone else: Sheriff is the victim. The shot is validated for range, motion, meeting state, role, ability jam and Corruptor before use.
- **Rift Warden / Strażnik Szczelin** (Crewmate), a PARADOX-specific mechanic: once per match, clear an active 50/75% reality ability jam or Null Field without reversing Paradox Meter. This role can operate through the global jam; Corruptor still prevents it.
- **Swapper**: no more than one is assigned because the existing meeting code supports one global swap pair.
- **Common validation**: role abilities now require CanMove on host (prevents disabled movement players activating abilities via spoofed RPC).
- **Localization**: EN/PL names, descriptions and controls added. Headless tests validate both full dictionaries, required name/description for every registered role and matching interpolation placeholders.
- Stable role numbering: previous 0-40 unchanged; Sheriff 41, RiftWarden 42. RPC 49/50 added and protocol incremented to v44.

## Important known issues; CI cannot solve these

1. Check that `MeetingHud.CheckForEndVoting` integration applies Swapper votes to **actual exile resolution**, not just UI; compare host logs and on-screen results in multi-client sessions.
2. Swapper's preexisting self-punishment after an exile uses a custom kill RPC; test native death synchronization and death counting; protect against double deaths.
3. Sheriff's `MurderPlayer` and Guardian/Phantom interactions require real host/client testing to ensure a false shot kills only the Sheriff.
4. Rift Warden's burst must appear on every client with consistent jam timers; stress-test when an ability causes Paradox Meter to cross another threshold at the same instant.
5. `SwapperChatCommandPatch` uses reflective Free Chat fields; Quick Chat-only lobbies must use native voting-grid arrows. Test both paths and ensure no command text leaks.
6. On the target Among Us / Reactor build, verify each Harmony method signature at runtime; successful .NET compilation is not enough.
7. `ParadoxRoleSettings` defaults all registered roles to 100% eligible; this is not a balanced distribution. Spawn chance only controls eligibility, not probability of actually receiving a specific role.
8. 43 registered role foundations are **not** 43 runtime-verified complete roles. Smoke-test buttons, corpse state, meeting close, cooldown, RPC authorization, language changes, respawn/reset for each.

## Next candidates — additions not yet implemented

| Role | Why consider it | PARADOX variation |
|---|---|---|
| Mayor / Burmistrz | Social voting gameplay | Spend one bonus vote during instability |
| Snitch / Informator | Task-based information | Collect encrypted Paradox traces |
| Bait / Przynęta | Consequence after a kill | Leave a temporary visible reality echo |
| Arsonist / Podpalacz | Neutral progression | Rift ignition, risk/reward tied to meter |
| Executioner / Egzekutor | Alternate voting goal | A marked target becomes vulnerable only on a threshold |
| Medium / Medium | Dead-body evidence | Replay short fragments of recent ability traces |
| Timekeeper / Strażnik Czasu | Distinctive PARADOX identity | Earn a scarce time-anchor charge by stabilizing threshold events |

Prioritize quality (native UI, balance, safety and reproducible multiplayer testing) over adding dozens of unchecked roles.

## Release standard

A green GitHub Actions check proves code compiles and the unit tests pass; it does not certify gameplay compatibility. Test with a matching Among Us version, Reactor and BepInEx, with at least two modded clients. Keep experimental roles behind host settings until verified.
