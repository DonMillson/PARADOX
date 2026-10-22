using Paradox.Roles.Doppelganger;

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
Check(shortCooldown.TryStart(6, 12f, 12f, 3f), "cast possible after disguise ends");

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
