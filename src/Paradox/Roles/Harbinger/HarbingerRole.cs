using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Harbinger;

public static class HarbingerRole
{
    public const int CustomWinReason = 122;
    public const byte NoWinner = byte.MaxValue;

    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, int> OmenCounts = new();

    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsHarbinger(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Harbinger;

    public static int GetOmenCount(byte playerId) =>
        OmenCounts.TryGetValue(playerId, out var count) ? count : 0;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryInvokeOmen(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return false;

        if (!IsHarbinger(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId))
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var current = GetOmenCount(source.PlayerId);
        if (current >= ParadoxRoleSettings.HarbingerRequiredOmens)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Harbinger))
            return false;

        if (ParadoxRoleSettings.HarbingerBonusMeter > 0f)
            ParadoxGame.AddMeter(ParadoxRoleSettings.HarbingerBonusMeter);

        var next = Math.Min(
            current + 1,
            ParadoxRoleSettings.HarbingerRequiredOmens);

        ApplySyncedState(
            source.PlayerId,
            next,
            ParadoxRoleSettings.HarbingerCooldownSeconds);

        BroadcastAccepted(source.PlayerId, next);
        UpdateHost();
        return true;
    }

    public static void ApplySyncedState(
        byte playerId,
        int omenCount,
        float cooldownSeconds)
    {
        OmenCounts[playerId] = Math.Max(0, omenCount);
        CooldownEndsAt[playerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(playerId, omenCount);
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return;

        if (ParadoxGame.State.Meter <
            ParadoxRoleSettings.HarbingerWinMeterThreshold)
            return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected ||
                !IsHarbinger(player.PlayerId) ||
                GetOmenCount(player.PlayerId) <
                    ParadoxRoleSettings.HarbingerRequiredOmens)
                continue;

            WinnerPlayerId = player.PlayerId;
            _endSent = true;

            GameManager.Instance.RpcEndGame(
                (GameOverReason)CustomWinReason,
                false);
            return;
        }
    }

    private static void BroadcastAccepted(byte playerId, int omenCount)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<HarbingerOmenRpc>.Instance.Send(
            sender,
            new HarbingerOmenRpc.Data(
                playerId,
                omenCount,
                ParadoxRoleSettings.HarbingerCooldownSeconds,
                1),
            immediately: true);
    }

    private static void ShowFeedback(byte playerId, int omenCount)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null ||
                local.PlayerId != playerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer
                .Get("role.Harbinger.feedback")
                .Replace("{count}", omenCount.ToString())
                .Replace("{required}", ParadoxRoleSettings.HarbingerRequiredOmens.ToString())
                .Replace("{amount}", (2f + ParadoxRoleSettings.HarbingerBonusMeter).ToString("0.#"));

            hud.Notifier.AddDisconnectMessage(text);

            if (omenCount >= ParadoxRoleSettings.HarbingerRequiredOmens)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Harbinger.ready"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Harbinger feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        OmenCounts.Clear();
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }
}
