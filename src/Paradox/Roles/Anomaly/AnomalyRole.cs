using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Anomaly;

public static class AnomalyRole
{
    public const int CustomWinReason = 120;

    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static bool _winArmed;
    private static bool _endSent;
    private static float _winAt;

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
            ParadoxEventRuntime.RoleAbilitiesBlocked)
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

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || _endSent)
            return;

        var anomaly = FindLivingAnomaly();
        if (anomaly == null)
        {
            _winArmed = false;
            _winAt = 0f;
            return;
        }

        if (ParadoxGame.State.Meter < 100f)
            return;

        if (!_winArmed)
        {
            _winArmed = true;
            _winAt = Time.time + 12f;
            ParadoxPlugin.Instance.Log.LogInfo(
                $"Anomaly win armed for player {anomaly.PlayerId}; resolving after the 100% event.");
            return;
        }

        if (Time.time < _winAt)
            return;

        _endSent = true;
        GameManager.Instance.RpcEndGame(
            (GameOverReason)CustomWinReason,
            false);
    }

    private static PlayerControl? FindLivingAnomaly()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                !IsAnomaly(player.PlayerId) ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected)
                continue;

            return player;
        }

        return null;
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

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        _winArmed = false;
        _endSent = false;
        _winAt = 0f;
    }
}
