using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Doppelganger;

public static class DoppelgangerRole
{
    private static readonly Dictionary<byte, DoppelgangerState> States = new();

    public static bool IsDoppelganger(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Doppelganger;

    public static DoppelgangerState GetOrCreate(byte playerId)
    {
        if (!States.TryGetValue(playerId, out var state))
            States[playerId] = state = new DoppelgangerState(playerId);

        return state;
    }

    public static bool TryDisguise(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsDoppelganger(player.PlayerId) ||
            player.Data == null || player.Data.IsDead || player.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            player.PlayerId == target.PlayerId)
            return false;

        var distance = Vector2.Distance(player.GetTruePosition(), target.GetTruePosition());
        if (distance > player.MaxReportDistance)
            return false;

        var state = GetOrCreate(player.PlayerId);
        if (!state.TryStart(
                target.PlayerId,
                Time.time,
                ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds,
                ParadoxRoleSettings.DoppelgangerCooldownSeconds))
            return false;

        // Apply the gameplay effect before charging the shared ability side effects.
        // A failed appearance copy must not increase the Paradox Meter or emit an Observer trace.
        if (!DoppelgangerAppearance.Copy(player, target))
        {
            state.CancelStart();
            return false;
        }

        if (!RoleAbilityService.Use(player, RoleId.Doppelganger))
        {
            DoppelgangerAppearance.Restore(player);
            state.CancelStart();
            return false;
        }

        BroadcastState(
            player.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds,
            ParadoxRoleSettings.DoppelgangerCooldownSeconds,
            active: true);

        return true;
    }

    public static void Update(PlayerControl player)
    {
        if (player == null || !AmongUsClient.Instance.AmHost)
            return;

        if (!States.TryGetValue(player.PlayerId, out var state) ||
            !state.TargetPlayerId.HasValue)
            return;

        // Never carry a disguise through a meeting or into a later match.
        var disguiseExpired = !state.IsDisguised(Time.time);
        var playerUnavailable = player.Data == null ||
                                player.Data.IsDead ||
                                player.Data.Disconnected;
        var meetingStarted = MeetingHud.Instance != null;
        if (!disguiseExpired && !playerUnavailable && !meetingStarted)
            return;

        DoppelgangerAppearance.Restore(player);
        state.ClearDisguise();

        BroadcastState(
            player.PlayerId,
            byte.MaxValue,
            0f,
            state.CooldownRemaining(Time.time),
            active: false);
    }

    public static void ApplySyncedState(
        byte playerId,
        byte targetPlayerId,
        float disguiseDurationSeconds,
        float cooldownSeconds,
        bool active)
    {
        var state = GetOrCreate(playerId);
        if (active)
        {
            state.ApplySyncedStart(
                targetPlayerId,
                Time.time,
                disguiseDurationSeconds,
                cooldownSeconds);
        }
        else
        {
            state.ClearDisguise();
        }
    }

    private static void BroadcastState(
        byte playerId,
        byte targetPlayerId,
        float disguiseDurationSeconds,
        float cooldownSeconds,
        bool active)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null || !AmongUsClient.Instance.AmHost)
            return;

        Rpc<DoppelgangerStateRpc>.Instance.Send(
            sender,
            new DoppelgangerStateRpc.Data(
                playerId,
                targetPlayerId,
                disguiseDurationSeconds,
                cooldownSeconds,
                active ? (byte)1 : (byte)0),
            immediately: true);
    }

    public static void Reset()
    {
        // Only the host may issue outfit restoration RPCs. The local cache must
        // still be cleared on every client between matches.
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player != null && States.TryGetValue(player.PlayerId, out var state) &&
                    state.TargetPlayerId.HasValue)
                    DoppelgangerAppearance.Restore(player);
            }
        }

        States.Clear();
        DoppelgangerAppearance.Reset();
    }
}
