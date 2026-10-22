# Doppelgänger: gameplay & HUD verification

Status: hardened role implementation + testable state machine. Compile / CI is not a live Among Us multiplayer test.

## Gameplay rules

- Faction: Impostor, role: Doppelgänger (Sobowtór in Polish).
- Ability: copy a valid living player's name, color, hat, skin and pet.
- Range: vanilla player's report distance; closest valid nearby player is chosen.
- Disguise: 12 seconds by default; cooldown: 30 seconds from initial activation.
- A target may not be the caster, dead or disconnected.
- An active disguise cannot be overwritten, even if the cooldown is configured shorter than the disguise.
- The host rejects requests during meetings, while dead/disconnected, immobilized, while role abilities are jammed or under Corruptor's block.
- Host restores appearance when the effect expires, a meeting begins, or the caster dies/disconnects.
- Host sends start/stop timers to clients; on cancellation clients adjust to the authoritative remaining cooldown.
- Shared meter/Observer ability side effects happen only after outfit copy succeeds.
- The button shows **COPY / KOPIUJ**, **MASK Ns / MASKA Ns**, **NO TARGET / BRAK CELU**, and **BLOCKED / BLOKADA**, as applicable. The timer wedge remains on the native HUD button.

## Automated tests

GitHub Actions now runs a standalone .NET 6 role-state test executable before compiling the mod.
It covers readiness, no self-copy, active disguise, cooldowns, overlap prevention, synchronized cancellation, next cast and rollback.

## Live game checklist

1. Start a match with 2+ modded clients and assign Doppelgänger to an Impostor.
2. With no living target in range, verify **NO TARGET** and a disabled ability button.
3. Get near a living target; **COPY** should become clickable.
4. Trigger once. All clients should see the target's appearance copied (name, color, hat, skin, pet).
5. Verify **MASK 12s**, a draining effect timer, and inability to spam a second cast.
6. Confirm original appearance returns after 12 seconds. Cooldown should continue until 30 seconds after cast.
7. Start another disguise after cooldown, then call a meeting. Confirm immediate restoration and no client cooldown jump.
8. Repeat with Corruptor block, Paradox Meter ability jam, death, disconnect, and target moving out of range before click.
9. Change PL/EN; verify translated ability texts and that text fits on the button.
10. Repeat with remote host and 2+ clients, checking BepInEx logs for rejected invalid requests and outfit desync.

## Known limits

This change does **not** establish a stable multiplayer release, add role icons/VFX, or resolve appearance conflicts if Doppelgänger and another role that rewrites outfits affect one player simultaneously. Those interactions need coordinated appearance ownership and a separate integration test.
