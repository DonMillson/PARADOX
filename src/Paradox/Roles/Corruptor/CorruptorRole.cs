using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Corruptor;

public static class CorruptorRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, float> CorruptedUntil = new();

    public static bool IsCorruptor(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Corruptor;

    public static bool IsCorrupted(byte playerId)
    {
        if (!CorruptedUntil.TryGetValue(playerId, out var end))
            return false;

        if (Time.time < end)
            return true;

        CorruptedUntil.Remove(playerId);
        return false;
    }

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryCorrupt(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsCorruptor(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f ||
            IsCorrupted(target.PlayerId))
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), target.GetTruePosition());
        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Corruptor))
            return false;

        ApplySyncedCorruption(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.CorruptorDurationSeconds,
            ParadoxRoleSettings.CorruptorCooldownSeconds);

        BroadcastAccepted(source.PlayerId, target.PlayerId);
        return true;
    }

    public static void ApplySyncedCorruption(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        CorruptedUntil[targetPlayerId] =
            Time.time + Math.Max(0f, durationSeconds);

        ShowFeedback(sourcePlayerId, targetPlayerId, durationSeconds);
    }

    private static void ShowFeedback(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || hud == null || hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                var text = ParadoxPlugin.Localizer.Get("role.Corruptor.feedback.source")
                    .Replace("{seconds}", Math.Ceiling(durationSeconds).ToString());
                hud.Notifier.AddDisconnectMessage(text);
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Corruptor.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Corruptor feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(byte sourcePlayerId, byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<CorruptorCorruptRpc>.Instance.Send(
            sender,
            new CorruptorCorruptRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.CorruptorDurationSeconds,
                ParadoxRoleSettings.CorruptorCooldownSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        CorruptedUntil.Clear();
    }
}
