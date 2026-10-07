using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Roles.Guardian;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.BountyHunter;

public static class BountyHunterRole
{
    public const int CustomWinReason = 121;
    public const byte NoWinner = byte.MaxValue;

    private static readonly Dictionary<byte, byte> Targets = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsBountyHunter(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.BountyHunter;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryGetTarget(byte hunterPlayerId, out PlayerControl? target)
    {
        target = null;

        if (!Targets.TryGetValue(hunterPlayerId, out var targetId))
            return false;

        target = FindPlayer(targetId);
        return target != null &&
               target.Data != null &&
               !target.Data.IsDead &&
               !target.Data.Disconnected;
    }

    public static void InitializeTargetsHost()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return;

        WinnerPlayerId = NoWinner;
        _endSent = false;
        Targets.Clear();
        CooldownEndsAt.Clear();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected ||
                !IsBountyHunter(player.PlayerId))
                continue;

            AssignNewTargetHost(player);
        }
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return;

        foreach (var hunter in PlayerControl.AllPlayerControls)
        {
            if (hunter == null ||
                hunter.Data == null ||
                hunter.Data.IsDead ||
                hunter.Data.Disconnected ||
                !IsBountyHunter(hunter.PlayerId))
                continue;

            if (!TryGetTarget(hunter.PlayerId, out _))
                AssignNewTargetHost(hunter);
        }
    }

    public static bool TryExecute(PlayerControl source, PlayerControl target)
    {
        if (source == null ||
            target == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return false;

        if (!IsBountyHunter(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            target.Data == null ||
            target.Data.IsDead ||
            target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId))
            return false;

        if (!Targets.TryGetValue(source.PlayerId, out var targetId) ||
            targetId != target.PlayerId)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        if (GuardianRole.IsProtected(target.PlayerId))
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.BountyHunter))
            return false;

        source.MurderPlayer(target, MurderResultFlags.Succeeded);

        if (target.Data == null || !target.Data.IsDead)
            return false;

        CooldownEndsAt[source.PlayerId] =
            now + ParadoxRoleSettings.BountyHunterCooldownSeconds;

        WinnerPlayerId = source.PlayerId;
        _endSent = true;

        BroadcastState(
            source.PlayerId,
            target.PlayerId,
            1,
            ParadoxRoleSettings.BountyHunterCooldownSeconds,
            1);

        GameManager.Instance.RpcEndGame(
            (GameOverReason)CustomWinReason,
            false);

        return true;
    }

    public static void ApplySyncedState(
        byte hunterPlayerId,
        byte targetPlayerId,
        byte action,
        float cooldownSeconds)
    {
        if (action == 0)
        {
            Targets[hunterPlayerId] = targetPlayerId;
            ShowTargetFeedback(hunterPlayerId, targetPlayerId);
            return;
        }

        if (action == 1)
        {
            WinnerPlayerId = hunterPlayerId;
            CooldownEndsAt[hunterPlayerId] =
                Time.time + Math.Max(0f, cooldownSeconds);
        }
    }

    private static void AssignNewTargetHost(PlayerControl hunter)
    {
        var candidates = new List<PlayerControl>();

        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate == null ||
                candidate.PlayerId == hunter.PlayerId ||
                candidate.Data == null ||
                candidate.Data.IsDead ||
                candidate.Data.Disconnected)
                continue;

            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
        {
            Targets.Remove(hunter.PlayerId);
            return;
        }

        var target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        Targets[hunter.PlayerId] = target.PlayerId;

        BroadcastState(
            hunter.PlayerId,
            target.PlayerId,
            0,
            0f,
            1);

        ShowTargetFeedback(hunter.PlayerId, target.PlayerId);
    }

    private static void BroadcastState(
        byte hunterPlayerId,
        byte targetPlayerId,
        byte action,
        float cooldownSeconds,
        byte accepted)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<BountyHunterStateRpc>.Instance.Send(
            sender,
            new BountyHunterStateRpc.Data(
                hunterPlayerId,
                targetPlayerId,
                action,
                cooldownSeconds,
                accepted),
            immediately: true);
    }

    private static void ShowTargetFeedback(
        byte hunterPlayerId,
        byte targetPlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null ||
                local.PlayerId != hunterPlayerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            var target = FindPlayer(targetPlayerId);
            var name = target?.Data?.PlayerName ?? $"#{targetPlayerId}";

            var text = ParadoxPlugin.Localizer
                .Get("role.BountyHunter.feedback.target")
                .Replace("{player}", name);

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Bounty Hunter target feedback failed: {e.Message}");
        }
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null && candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }

    public static void ResetRuntime()
    {
        Targets.Clear();
        CooldownEndsAt.Clear();
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }
}
