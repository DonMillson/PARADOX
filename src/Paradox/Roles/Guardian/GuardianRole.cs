using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Guardian;

public static class GuardianRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, float> ProtectedUntil = new();

    public static bool IsGuardian(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Guardian;

    public static bool IsProtected(byte playerId)
    {
        if (!ProtectedUntil.TryGetValue(playerId, out var until))
            return false;

        if (Time.time < until)
            return true;

        ProtectedUntil.Remove(playerId);
        return false;
    }

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var cooldownEnd))
            return 0f;

        return Math.Max(0f, cooldownEnd - now);
    }

    public static bool TryProtect(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsGuardian(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            target.PlayerId == source.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            IsProtected(target.PlayerId))
            return false;

        var now = Time.time;
        if (CooldownEndsAt.TryGetValue(source.PlayerId, out var cooldownEnd) &&
            now < cooldownEnd)
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Guardian))
            return false;

        ApplySyncedProtection(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.GuardianProtectionDurationSeconds,
            ParadoxRoleSettings.GuardianCooldownSeconds);

        ShowActivationFeedback(source.PlayerId, target.PlayerId);
        BroadcastAccepted(source.PlayerId, target.PlayerId);
        return true;
    }

    public static void ApplySyncedProtection(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ProtectedUntil[targetPlayerId] =
            Time.time + Math.Max(0f, durationSeconds);
    }

    public static bool TryBlockMurder(PlayerControl target)
    {
        if (target == null || !IsProtected(target.PlayerId))
            return false;

        ProtectedUntil.Remove(target.PlayerId);

        try
        {
            var local = PlayerControl.LocalPlayer;
            if (local != null && local.PlayerId == target.PlayerId &&
                HudManager.Instance != null && HudManager.Instance.Notifier != null)
            {
                HudManager.Instance.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Guardian.blocked"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Guardian block feedback failed: {e.Message}");
        }

        ParadoxPlugin.Instance.Log.LogInfo(
            $"Guardian protection blocked a murder on player {target.PlayerId}.");

        return true;
    }

    private static void BroadcastAccepted(byte sourcePlayerId, byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<GuardianProtectRpc>.Instance.Send(
            sender,
            new GuardianProtectRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.GuardianProtectionDurationSeconds,
                ParadoxRoleSettings.GuardianCooldownSeconds,
                1),
            immediately: true);
    }

    private static void ShowActivationFeedback(byte sourcePlayerId, byte targetPlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || hud == null || hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Guardian.feedback.source"));
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Guardian.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Guardian activation feedback failed: {e.Message}");
        }
    }

    public static void ShowSyncedActivationFeedback(byte sourcePlayerId, byte targetPlayerId) =>
        ShowActivationFeedback(sourcePlayerId, targetPlayerId);

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        ProtectedUntil.Clear();
    }
}
