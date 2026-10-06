using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Seer;

public static class SeerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsSeer(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Seer;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryReadAura(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsSeer(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), target.GetTruePosition());
        if (distance > source.MaxReportDistance)
            return false;

        ObserverState.RemoveExpired(now);
        var disturbed = ObserverState.Active.Any(trace =>
            trace.SourcePlayerId == target.PlayerId);

        if (!RoleAbilityService.Use(source, RoleId.Seer))
            return false;

        ApplySyncedResult(
            source.PlayerId,
            ParadoxRoleSettings.SeerCooldownSeconds,
            disturbed);

        BroadcastAccepted(source.PlayerId, target.PlayerId, disturbed);
        return true;
    }

    public static void ApplySyncedResult(
        byte sourcePlayerId,
        float cooldownSeconds,
        bool disturbed)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(sourcePlayerId, disturbed);
    }

    private static void ShowFeedback(byte sourcePlayerId, bool disturbed)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    disturbed
                        ? "role.Seer.result.disturbed"
                        : "role.Seer.result.calm"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Seer feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte targetPlayerId,
        bool disturbed)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<SeerReadAuraRpc>.Instance.Send(
            sender,
            new SeerReadAuraRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.SeerCooldownSeconds,
                disturbed ? (byte)1 : (byte)0,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
