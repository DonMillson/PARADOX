using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Paradox.Roles.Corruptor;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Anomaly;

public static class AnomalyRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsAnomaly(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Anomaly;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var cooldownEnd))
            return 0f;

        return Math.Max(0f, cooldownEnd - now);
    }

    public static bool TryPulse(PlayerControl source)
    {
        if (source == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsAnomaly(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId) ||
            MeetingHud.Instance != null)
            return false;

        var now = Time.time;
        if (CooldownEndsAt.TryGetValue(source.PlayerId, out var cooldownEnd) &&
            now < cooldownEnd)
            return false;

        ApplySyncedCooldown(
            source.PlayerId,
            ParadoxRoleSettings.AnomalyCooldownSeconds);

        ParadoxGame.AddFrom(ParadoxMeterSource.Anomaly);
        ObserverNetwork.BroadcastTrace(source);

        TryShowPulseFeedback(source.PlayerId);
        BroadcastAcceptedPulse(source.PlayerId);

        return true;
    }

    public static void ApplySyncedCooldown(byte playerId, float cooldownSeconds)
    {
        CooldownEndsAt[playerId] =
            Time.time + Math.Max(0f, cooldownSeconds);
    }

    private static void BroadcastAcceptedPulse(byte playerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<AnomalyPulseRpc>.Instance.Send(
            sender,
            new AnomalyPulseRpc.Data(
                playerId,
                ParadoxRoleSettings.AnomalyCooldownSeconds,
                1),
            immediately: true);
    }

    private static void TryShowPulseFeedback(byte sourcePlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get("role.Anomaly.pulse"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Anomaly pulse feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
