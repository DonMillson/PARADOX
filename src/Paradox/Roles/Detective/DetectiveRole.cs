using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Detective;

public static class DetectiveRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsDetective(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Detective;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryInvestigate(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsDetective(source.PlayerId) ||
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

        if (!RoleAbilityService.Use(source, RoleId.Detective))
            return false;

        var window = Math.Max(1f, ParadoxRoleSettings.DetectiveRecentActivitySeconds);
        var violentResidue = DeathEvidenceRegistry.All.Any(record =>
            record.KillerPlayerId == target.PlayerId &&
            now - record.TimeOfDeath >= 0f &&
            now - record.TimeOfDeath <= window);

        ApplySyncedResult(
            source.PlayerId,
            ParadoxRoleSettings.DetectiveCooldownSeconds,
            violentResidue);

        BroadcastAccepted(source.PlayerId, target.PlayerId, violentResidue);
        return true;
    }

    public static void ApplySyncedResult(
        byte sourcePlayerId,
        float cooldownSeconds,
        bool violentResidue)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(sourcePlayerId, violentResidue);
    }

    private static void ShowFeedback(byte sourcePlayerId, bool violentResidue)
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
                    violentResidue
                        ? "role.Detective.result.hot"
                        : "role.Detective.result.clear"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Detective feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte targetPlayerId,
        bool violentResidue)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<DetectiveScanRpc>.Instance.Send(
            sender,
            new DetectiveScanRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.DetectiveCooldownSeconds,
                violentResidue ? (byte)1 : (byte)0,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
