using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Nightmare;

public static class NightmareRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsNightmare(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Nightmare;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryHaunt(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsNightmare(source.PlayerId) ||
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

        if (!RoleAbilityService.Use(source, RoleId.Nightmare))
            return false;

        ApplySyncedHaunt(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.NightmareDurationSeconds,
            ParadoxRoleSettings.NightmareCooldownSeconds);

        BroadcastAccepted(source.PlayerId, target.PlayerId);
        return true;
    }

    public static void ApplySyncedHaunt(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        NightmareEffectRuntime.ApplyIfLocal(targetPlayerId, durationSeconds);
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
                var text = ParadoxPlugin.Localizer.Get("role.Nightmare.feedback.source")
                    .Replace("{seconds}", Math.Ceiling(durationSeconds).ToString());
                hud.Notifier.AddDisconnectMessage(text);
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Nightmare.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Nightmare feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(byte sourcePlayerId, byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<NightmareHauntRpc>.Instance.Send(
            sender,
            new NightmareHauntRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.NightmareDurationSeconds,
                ParadoxRoleSettings.NightmareCooldownSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        NightmareEffectRuntime.Reset();
    }
}
