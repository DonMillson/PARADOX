using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Phantom;

public static class PhantomRole
{
    public const int CustomWinReason = 127;
    public const byte NoWinner = byte.MaxValue;

    private static readonly Dictionary<byte, float> PhaseEndsAt = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, int> EscapeCounts = new();

    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsPhantom(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Phantom;

    public static bool IsPhased(byte playerId) =>
        PhaseEndsAt.TryGetValue(playerId, out var end) &&
        Time.time < end;

    public static float PhaseRemaining(byte playerId, float now) =>
        PhaseEndsAt.TryGetValue(playerId, out var end)
            ? Math.Max(0f, end - now)
            : 0f;

    public static float CooldownRemaining(byte playerId, float now) =>
        CooldownEndsAt.TryGetValue(playerId, out var end)
            ? Math.Max(0f, end - now)
            : 0f;

    public static int GetEscapeCount(byte playerId) =>
        EscapeCounts.TryGetValue(playerId, out var count)
            ? count
            : 0;

    public static bool TryPhase(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return false;

        var now = Time.time;

        if (!IsPhantom(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            MeetingHud.Instance != null ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId) ||
            IsPhased(source.PlayerId) ||
            CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Phantom))
            return false;

        ApplySyncedState(
            source.PlayerId,
            1,
            ParadoxRoleSettings.PhantomPhaseDurationSeconds,
            ParadoxRoleSettings.PhantomCooldownSeconds,
            GetEscapeCount(source.PlayerId));

        BroadcastState(
            source.PlayerId,
            1,
            ParadoxRoleSettings.PhantomPhaseDurationSeconds,
            ParadoxRoleSettings.PhantomCooldownSeconds,
            GetEscapeCount(source.PlayerId));

        return true;
    }

    public static bool TryBlockMurder(PlayerControl target)
    {
        if (target == null ||
            !IsPhased(target.PlayerId))
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            RegisterEscapeHost(target.PlayerId);
        }

        return true;
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        var now = Time.time;

        foreach (var pair in PhaseEndsAt.ToArray())
        {
            var player = FindPlayer(pair.Key);

            var shouldEnd =
                now >= pair.Value ||
                MeetingHud.Instance != null ||
                player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected;

            if (!shouldEnd)
                continue;

            EndPhaseHost(pair.Key);
        }
    }

    private static void RegisterEscapeHost(byte playerId)
    {
        if (!IsPhased(playerId) || _endSent)
            return;

        var count = GetEscapeCount(playerId) + 1;
        EscapeCounts[playerId] = count;

        EndPhaseHost(playerId);

        ApplySyncedState(
            playerId,
            3,
            0f,
            CooldownRemaining(playerId, Time.time),
            count);

        BroadcastState(
            playerId,
            3,
            0f,
            CooldownRemaining(playerId, Time.time),
            count);

        if (count < ParadoxRoleSettings.PhantomRequiredEscapes)
            return;

        WinnerPlayerId = playerId;
        _endSent = true;

        GameManager.Instance.RpcEndGame(
            (GameOverReason)CustomWinReason,
            false);
    }

    private static void EndPhaseHost(byte playerId)
    {
        if (!PhaseEndsAt.ContainsKey(playerId))
            return;

        PhaseEndsAt.Remove(playerId);
        SetVisible(playerId, true);

        BroadcastState(
            playerId,
            2,
            0f,
            CooldownRemaining(playerId, Time.time),
            GetEscapeCount(playerId));
    }

    public static void ApplySyncedState(
        byte playerId,
        byte action,
        float durationSeconds,
        float cooldownSeconds,
        int escapeCount)
    {
        if (action == 1)
        {
            PhaseEndsAt[playerId] =
                Time.time + Math.Max(0f, durationSeconds);

            CooldownEndsAt[playerId] =
                Time.time + Math.Max(0f, cooldownSeconds);

            EscapeCounts[playerId] =
                Math.Max(0, escapeCount);

            SetVisible(playerId, false);
            ShowPhaseFeedback(playerId, durationSeconds);
            return;
        }

        if (action == 2)
        {
            PhaseEndsAt.Remove(playerId);
            SetVisible(playerId, true);
            return;
        }

        if (action == 3)
        {
            EscapeCounts[playerId] =
                Math.Max(0, escapeCount);

            ShowEscapeFeedback(playerId, escapeCount);
        }
    }

    private static void BroadcastState(
        byte playerId,
        byte action,
        float durationSeconds,
        float cooldownSeconds,
        int escapeCount)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<PhantomPhaseRpc>.Instance.Send(
            sender,
            new PhantomPhaseRpc.Data(
                playerId,
                action,
                durationSeconds,
                cooldownSeconds,
                escapeCount,
                1),
            immediately: true);
    }

    private static void SetVisible(byte playerId, bool visible)
    {
        var player = FindPlayer(playerId);
        if (player == null || player.cosmetics == null)
            return;

        try
        {
            if (player.cosmetics.currentBodySprite != null)
                player.cosmetics.currentBodySprite.Visible = visible;

            player.cosmetics.SetBodyCosmeticsVisible(visible);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Phantom visibility update failed: {e.Message}");
        }
    }

    private static void ShowPhaseFeedback(
        byte playerId,
        float durationSeconds)
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
                .Get("role.Phantom.feedback")
                .Replace(
                    "{seconds}",
                    Math.Ceiling(durationSeconds).ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch { }
    }

    private static void ShowEscapeFeedback(
        byte playerId,
        int count)
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
                .Get("role.Phantom.escape")
                .Replace("{count}", count.ToString())
                .Replace(
                    "{required}",
                    ParadoxRoleSettings.PhantomRequiredEscapes.ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch { }
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null &&
                player.PlayerId == playerId)
                return player;
        }

        return null;
    }

    public static void ResetRuntime()
    {
        foreach (var playerId in PhaseEndsAt.Keys.ToArray())
            SetVisible(playerId, true);

        PhaseEndsAt.Clear();
        CooldownEndsAt.Clear();
        EscapeCounts.Clear();
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }
}
