using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.ParadoxTeam;

public static class ParadoxRole
{
    // 120 was previously the Anomaly solo-win reason; protocol 48 changes it.
    public const int CustomWinReason = 120;
    private static readonly ParadoxTeamRules Rules = new();
    private static readonly Dictionary<byte, float> Cooldowns = new();
    private static byte _leaderId = byte.MaxValue;
    private static byte _sidekickId = byte.MaxValue;
    private static bool _active;

    public static bool IsParadox(byte id) =>
        PlayerRoleRegistry.TryGet(id, out var role) && role == RoleId.Paradox;

    public static void InitializePairHost(byte leaderId, byte sidekickId)
    {
        _leaderId = leaderId;
        _sidekickId = sidekickId;
        _active = leaderId != sidekickId;
        Rules.Reset();
    }

    public static float CooldownRemaining(byte id, float now) =>
        Cooldowns.TryGetValue(id, out var until) ? Math.Max(0f, until - now) : 0f;
    public static void ApplySyncedCooldown(byte id, float seconds) =>
        Cooldowns[id] = Time.time + Math.Max(0f, seconds);

    public static bool TryRupture(PlayerControl player)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
            player == null || !IsParadox(player.PlayerId) ||
            player.Data == null || player.Data.IsDead || player.Data.Disconnected ||
            !player.CanMove || MeetingHud.Instance != null ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(player.PlayerId) ||
            ParadoxGame.State.Meter >= 100f ||
            CooldownRemaining(player.PlayerId, Time.time) > 0f)
            return false;

        // The new ability gives exactly +8 instead of the generic +2 plus +8.
        if (!RoleAbilityService.Use(player, RoleId.Paradox, chargeMeter: false))
            return false;
        ParadoxGame.AddMeter(ParadoxRoleSettings.ParadoxRuptureMeterGain);
        ObserverNetwork.BroadcastTrace(player);
        ApplySyncedCooldown(player.PlayerId, ParadoxRoleSettings.ParadoxRuptureCooldownSeconds);
        if (PlayerControl.LocalPlayer != null)
            Rpc<ParadoxRuptureRpc>.Instance.Send(PlayerControl.LocalPlayer,
                new ParadoxRuptureRpc.Data(player.PlayerId,
                    ParadoxRoleSettings.ParadoxRuptureCooldownSeconds, 1),
                immediately: true);
        return true;
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || !_active)
            return;

        var leader = FindPlayer(_leaderId);
        var sidekick = FindPlayer(_sidekickId);
        var leaderAlive = IsAlive(leader);
        var sidekickAlive = IsAlive(sidekick);

        // Poll death state to cover ejections and disconnects as well as murders.
        if (Rules.TryPromote(leaderAlive, sidekickAlive) && sidekick != null)
        {
            if (leader != null)
                RoleAssignment.Assign(leader, RoleId.Anomaly);
            RoleAssignment.Assign(sidekick, RoleId.Paradox);
            _leaderId = sidekick.PlayerId;
            _sidekickId = byte.MaxValue;
            ParadoxGame.ReduceMeter(ParadoxTeamRules.SuccessionMeterPenalty);
            ParadoxPlugin.Instance.Log.LogInfo(
                "PARADOX: Anomaly inherited the leader role; meter reduced by 20.");
        }

        if (Rules.Tick(Time.time, ParadoxGame.State.Meter,
                       IsAlive(FindPlayer(_leaderId))) && GameManager.Instance != null)
            GameManager.Instance.RpcEndGame((GameOverReason)CustomWinReason, false);
    }

    public static bool IsAlive(PlayerControl? player) =>
        player != null && player.Data != null &&
        !player.Data.IsDead && !player.Data.Disconnected;

    public static PlayerControl? FindPlayer(byte id)
    {
        if (id == byte.MaxValue)
            return null;
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player != null && player.PlayerId == id)
                return player;
        return null;
    }

    public static PlayerControl? FindAliveRole(RoleId role)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
            if (IsAlive(player) && PlayerRoleRegistry.TryGet(player.PlayerId, out var assigned) &&
                assigned == role)
                return player;
        return null;
    }

    public static void ResetRuntime()
    {
        Rules.Reset();
        Cooldowns.Clear();
        _active = false;
        _leaderId = byte.MaxValue;
        _sidekickId = byte.MaxValue;
        ParadoxTeamHudPatch.ResetLocal();
    }
}
