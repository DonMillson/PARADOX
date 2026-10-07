using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.ShapeshifterX;

public static class ShapeshifterXRole
{
    private static readonly Dictionary<byte, SwapState> ActiveSwaps = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsShapeshifterX(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.ShapeshifterX;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool IsActive(byte sourcePlayerId, float now) =>
        ActiveSwaps.TryGetValue(sourcePlayerId, out var state) &&
        now < state.EndsAt;

    public static PlayerControl? FindClosestTarget(
        PlayerControl source,
        float maxDistance)
    {
        if (source == null ||
            source.Data == null ||
            source.Data.IsDead)
            return null;

        PlayerControl? closest = null;
        var bestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();

        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate == null ||
                candidate.PlayerId == source.PlayerId ||
                candidate.Data == null ||
                candidate.Data.IsDead ||
                candidate.Data.Disconnected)
                continue;

            var distance = Vector2.Distance(
                sourcePosition,
                candidate.GetTruePosition());

            if (distance > bestDistance)
                continue;

            closest = candidate;
            bestDistance = distance;
        }

        return closest;
    }

    public static bool TrySwap(
        PlayerControl source,
        PlayerControl target)
    {
        if (source == null ||
            target == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        var now = Time.time;

        if (!IsShapeshifterX(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            target.Data == null ||
            target.Data.IsDead ||
            target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId) ||
            CooldownRemaining(source.PlayerId, now) > 0f ||
            IsActive(source.PlayerId, now))
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        if (!ShapeshifterXAppearance.Swap(source, target))
            return false;

        if (!RoleAbilityService.Use(source, RoleId.ShapeshifterX))
        {
            ShapeshifterXAppearance.RestorePair(source, target);
            return false;
        }

        ApplySyncedState(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.ShapeshifterXDurationSeconds,
            ParadoxRoleSettings.ShapeshifterXCooldownSeconds,
            true);

        BroadcastState(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.ShapeshifterXDurationSeconds,
            ParadoxRoleSettings.ShapeshifterXCooldownSeconds,
            true);

        return true;
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        var now = Time.time;

        foreach (var pair in ActiveSwaps.ToArray())
        {
            var sourceId = pair.Key;
            var state = pair.Value;
            var source = FindPlayer(sourceId);
            var target = FindPlayer(state.TargetPlayerId);

            var shouldEnd =
                now >= state.EndsAt ||
                MeetingHud.Instance != null ||
                source == null ||
                source.Data == null ||
                source.Data.IsDead ||
                source.Data.Disconnected ||
                target == null ||
                target.Data == null ||
                target.Data.IsDead ||
                target.Data.Disconnected;

            if (!shouldEnd)
                continue;

            ShapeshifterXAppearance.RestorePair(source, target);
            ActiveSwaps.Remove(sourceId);

            BroadcastState(
                sourceId,
                state.TargetPlayerId,
                0f,
                CooldownRemaining(sourceId, now),
                false);
        }
    }

    public static void ApplySyncedState(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds,
        bool active)
    {
        if (active)
        {
            ActiveSwaps[sourcePlayerId] = new SwapState(
                targetPlayerId,
                Time.time + Math.Max(0f, durationSeconds));

            CooldownEndsAt[sourcePlayerId] =
                Time.time + Math.Max(0f, cooldownSeconds);

            ShowFeedback(
                sourcePlayerId,
                targetPlayerId,
                durationSeconds);

            return;
        }

        ActiveSwaps.Remove(sourcePlayerId);
    }

    private static void BroadcastState(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds,
        bool active)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<ShapeshifterXSwapRpc>.Instance.Send(
            sender,
            new ShapeshifterXSwapRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                durationSeconds,
                cooldownSeconds,
                active ? (byte)1 : (byte)0),
            immediately: true);
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
            if (local == null ||
                hud == null ||
                hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                var text = ParadoxPlugin.Localizer
                    .Get("role.ShapeshifterX.feedback.source")
                    .Replace(
                        "{seconds}",
                        Math.Ceiling(durationSeconds).ToString());

                hud.Notifier.AddDisconnectMessage(text);
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get(
                        "role.ShapeshifterX.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Shapeshifter X feedback failed: {e.Message}");
        }
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null &&
                candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }

    public static void ResetRuntime()
    {
        foreach (var pair in ActiveSwaps)
        {
            ShapeshifterXAppearance.RestorePair(
                FindPlayer(pair.Key),
                FindPlayer(pair.Value.TargetPlayerId));
        }

        ActiveSwaps.Clear();
        CooldownEndsAt.Clear();
        ShapeshifterXAppearance.Reset();
    }

    private sealed record SwapState(
        byte TargetPlayerId,
        float EndsAt);
}
