using Paradox.Roles.Doppelganger;
using Paradox.Roles.Swapper;
using Paradox.Roles;
using Paradox.Settings;
using Paradox.Localization;
using System.Text.RegularExpressions;

static void Check(bool condition, string description)
{
    if (!condition)
        throw new InvalidOperationException("Doppelganger state test failed: " + description);
}

var state = new DoppelgangerState(1);
Check(state.IsReady(0f), "new role is ready");
Check(!state.IsDisguised(0f), "new role is not disguised");
Check(!state.TryStart(1, 0f, 12f, 30f), "cannot copy own identity");
Check(!state.TryStart(byte.MaxValue, 0f, 12f, 30f), "cannot use empty target ID");
Check(state.TryStart(2, 10f, 12f, 30f), "first cast succeeds");
Check(state.IsDisguised(20f), "disguise active before 12-second expiry");
Check(state.DisguiseRemaining(20f) == 2f, "disguise timer counts down");
Check(state.CooldownRemaining(20f) == 20f, "cooldown begins at activation");
Check(!state.TryStart(3, 20f, 12f, 5f), "active disguise cannot be overwritten");
Check(!state.IsDisguised(22f), "disguise expires at 12 seconds");
Check(!state.IsReady(22f), "cooldown outlives disguise");
Check(!state.TryStart(3, 22f, 12f, 30f), "cast rejected on cooldown");

// Early return (meeting/death) preserves the host-sent remaining cooldown.
state.ApplySyncedStop(23f, 17f);
Check(!state.IsDisguised(23f), "cancellation immediately clears disguise");
Check(state.TargetPlayerId == null, "cancellation clears copied target");
Check(state.CooldownRemaining(30f) == 10f, "synced stop corrects the cooldown");
Check(state.IsReady(40f), "synced cooldown completes");
Check(state.TryStart(3, 40f, 12f, 30f), "new cast works after cooldown");

// A shorter cooldown still cannot start a second disguise before expiry.
var shortCooldown = new DoppelgangerState(4);
Check(shortCooldown.TryStart(5, 0f, 12f, 3f), "short cooldown cast starts");
Check(shortCooldown.IsReady(4f), "short cooldown expires");
Check(!shortCooldown.TryStart(6, 4f, 12f, 3f), "overlapping cast rejected");
Check(!shortCooldown.TryStart(6, 12f, 12f, 3f),
    "no recast until the host has restored the previous outfit");
shortCooldown.ClearDisguise(); // mirrors the host's successful Restore + ClearDisguise
Check(shortCooldown.TryStart(6, 12f, 12f, 3f),
    "cast possible after previous outfit has been restored");

// Synchronization and rejected action rollback remain deterministic.
var remote = new DoppelgangerState(7);
remote.ApplySyncedStart(8, 50f, 12f, 30f);
Check(remote.TargetPlayerId == 8 && remote.CooldownRemaining(55f) == 25f,
    "remote state applies host duration and cooldown");
remote.ApplySyncedStop(55f, 25f);
Check(remote.CooldownRemaining(65f) == 15f, "remote cancellation leaves cooldown");
remote.CancelStart();
Check(remote.TargetPlayerId == null && remote.CooldownRemaining(65f) == 0f,
    "rollback clears both active state and cooldown");

Console.WriteLine("PASS: Doppelganger state tests (timers, overlap, cancel, sync, rollback).");

var original = new byte[] { 1, 1, 1, 2, 2 };
var swapped = SwapperVoteResolver.Swap(original, 1, 2);
Check(swapped.SequenceEqual(new byte[]{ 2, 2, 2, 1, 1 }), "Swapper maps all ballots for both targets");
Check(SwapperVoteResolver.FindExiled(original) == 1, "original winner");
Check(SwapperVoteResolver.FindExiled(swapped) == 2, "swapped winner");
Check(SwapperVoteResolver.ChangedExileTo(original, 1, 2, 2), "actual exile changed");
Check(!SwapperVoteResolver.ChangedExileTo(original, 1, 2, 1), "no false punishment for old winner");
Check(SwapperVoteResolver.FindExiled(new byte[]{ 1, 2, 253 }) == null, "tie with skip");
Check(SwapperVoteResolver.FindExiled(new byte[]{ 253, 253, 1 }) == null, "skip majority");
Check(SwapperVoteResolver.FindExiled(new byte[]{ 254, 255, 255 }) == null, "abstentions");
Check(SwapperVoteResolver.Swap(new byte[]{ 253, 254, 255, 1 }, 1, 2)
    .SequenceEqual(new byte[]{ 253, 254, 255, 2 }), "skip and missed votes untouched");
var rejected = false;
try { SwapperVoteResolver.Swap(original, 2, 2); } catch (ArgumentException) { rejected = true; }
Check(rejected, "reject identical targets");
Console.WriteLine("PASS: Swapper voting tests (transfer, ties, skip, causality, invalid inputs).");

// The actual production localization tables run here, not mocked strings.
var translations = DefaultTranslations.Create();
var englishKeys = translations.Keys(Language.English).ToHashSet();
var polishKeys = translations.Keys(Language.Polish).ToHashSet();
Check(englishKeys.SetEquals(polishKeys), "EN and PL language key coverage match");
Check(Enum.GetValues<RoleId>().Length == 43, "43 unique registered roles");
Check((int)RoleId.Swapper == 40 && (int)RoleId.Sheriff == 41 &&
      (int)RoleId.RiftWarden == 42 && (int)RoleId.Anomaly == 30,
      "network role identifiers are backward compatible");

foreach (var role in Enum.GetNames<RoleId>())
{
    foreach (var language in new[] { Language.English, Language.Polish })
    {
        Check(translations.Has(language, $"role.{role}.name"),
              $"{language} has name for {role}");
        Check(translations.Has(language, $"role.{role}.description"),
              $"{language} has description for {role}");
    }
}

// Detect placeholder mistakes such as {count} versus {seconds}.
foreach (var key in englishKeys)
{
    var en = translations.GetExact(Language.English, key)!;
    var pl = translations.GetExact(Language.Polish, key)!;
    var enTokens = Regex.Matches(en, @"\\{[A-Za-z][A-Za-z0-9]*\\}")
        .Select(match => match.Value).OrderBy(x => x).ToArray();
    var plTokens = Regex.Matches(pl, @"\\{[A-Za-z][A-Za-z0-9]*\\}")
        .Select(match => match.Value).OrderBy(x => x).ToArray();
    Check(enTokens.SequenceEqual(plTokens), $"localization token mismatch: {key}");
}
Console.WriteLine($"PASS: bilingual localization ({englishKeys.Count} EN/PL keys, 43 roles, placeholder parity).");


// 50-seat mode must stay opt-in and must never unlock on official/unknown regions.
Check(LargeLobbyPolicy.VanillaMaxPlayers == 15, "default Among Us cap stays 15");
Check(LargeLobbyPolicy.ExperimentalMaxPlayers == 50, "PARADOX target maximum is 50");
Check(LargeLobbyPolicy.ClampRequestedLimit(10) == 15, "requested minimum stays vanilla");
Check(LargeLobbyPolicy.ClampRequestedLimit(20) == 20, "20 player preset supported");
Check(LargeLobbyPolicy.ClampRequestedLimit(30) == 30, "30 player preset supported");
Check(LargeLobbyPolicy.ClampRequestedLimit(40) == 40, "40 player preset supported");
Check(LargeLobbyPolicy.ClampRequestedLimit(50) == 50, "50 player preset supported");
Check(LargeLobbyPolicy.ClampRequestedLimit(500) == 50, "no unbounded 500 player setting");
Check(LargeLobbyPolicy.EffectiveMaxPlayers(50, false, true) == 15,
      "disabled large lobby mode cannot change default");
Check(LargeLobbyPolicy.EffectiveMaxPlayers(50, true, false) == 15,
      "public and unknown regions remain vanilla 15");
Check(LargeLobbyPolicy.EffectiveMaxPlayers(50, true, true) == 50,
      "private server can unlock experimental 50 in native selector");
Check(LargeLobbyPolicy.IsLargeLobby(16) && !LargeLobbyPolicy.IsLargeLobby(15),
      "only above vanilla 15 is large lobby");
Check(LargeLobbyPolicy.WithinExperimentalTarget(50) &&
      !LargeLobbyPolicy.WithinExperimentalTarget(51) &&
      !LargeLobbyPolicy.WithinExperimentalTarget(-1),
      "capacity target never exceeds signed-safe count or goes negative");
Console.WriteLine("PASS: experimental 15/20/30/40/50 player policy and public-server safety.");

Check(LargeLobbyPagination.PageCount(15) == 1 &&
      LargeLobbyPagination.PageCount(16) == 2 &&
      LargeLobbyPagination.PageCount(30) == 2 &&
      LargeLobbyPagination.PageCount(31) == 3 &&
      LargeLobbyPagination.PageCount(50) == 4,
      "meeting vote paging covers 15 through 50 players");
Check(LargeLobbyPagination.PageCount(0) == 0 &&
      LargeLobbyPagination.CyclePage(0, +1, 0) == 0,
      "empty meetings do not divide by zero");
Check(LargeLobbyPagination.CyclePage(3, +1, 50) == 0 &&
      LargeLobbyPagination.CyclePage(0, -1, 50) == 3,
      "meeting pages wrap correctly in both directions");
Check(LargeLobbyPagination.IsOnPage(49, 3, 50) &&
      !LargeLobbyPagination.IsOnPage(48, 2, 50) &&
      !LargeLobbyPagination.IsOnPage(50, 3, 50),
      "last meeting page includes player 50 and no out-of-bounds card");
Check(LargeLobbyPagination.PositionWithinPage(49) == (1, 1),
      "position index 49 corresponds to page four, row two, column two");
for (var playerCount = 16; playerCount <= 50; playerCount++)
{
    for (var index = 0; index < playerCount; index++)
    {
        var hits = Enumerable.Range(0, LargeLobbyPagination.PageCount(playerCount))
            .Count(page => LargeLobbyPagination.IsOnPage(index, page, playerCount));
        Check(hits == 1, $"meeting card {index} appears on exactly one page with {playerCount} players");
    }
}
Console.WriteLine("PASS: 15 cards/page, wrap, positioning and visibility for 16–50 players.");
