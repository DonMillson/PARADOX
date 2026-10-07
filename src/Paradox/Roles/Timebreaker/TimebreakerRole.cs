using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Timebreaker;

public static class TimebreakerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, float> StasisUntil = new();
    private static bool _localFrozenByParadox;

    public static bool IsTimebreaker(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Timebreaker;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

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

    public static bool TryLockTime(
        PlayerControl source,
        PlayerControl target)
    {
        if (source == null ||
            target == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsTimebreaker(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            target.Data == null ||
            target.Data.IsDead ||
            target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;

        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Timebreaker))
            return false;

        ApplySyncedStasis(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.TimebreakerDurationSeconds,
            ParadoxRoleSettings.TimebreakerCooldownSeconds);

        BroadcastAccepted(source.PlayerId, target.PlayerId);
        return true;
    }

    public static void ApplySyncedStasis(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds)
    {
        StasisUntil[targetPlayerId] =
            Time.time + Math.Max(0f, durationSeconds);

        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(
            sourcePlayerId,
            targetPlayerId,
            durationSeconds);
    }

    public static void UpdateLocalStasis()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null)
            return;

        var now = Time.time;
        var frozen =
            StasisUntil.TryGetValue(local.PlayerId, out var end) &&
            now < end;

        if (frozen)
        {
            local.moveable = false;
            _localFrozenByParadox = true;
            return;
        }

        if (StasisUntil.ContainsKey(local.PlayerId))
            StasisUntil.Remove(local.PlayerId);

        if (!_localFrozenByParadox)
            return;

        _localFrozenByParadox = false;

        if (local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected &&
            MeetingHud.Instance == null)
        {
            local.moveable = true;
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<TimebreakerStasisRpc>.Instance.Send(
            sender,
            new TimebreakerStasisRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.TimebreakerDurationSeconds,
                ParadoxRoleSettings.TimebreakerCooldownSeconds,
                1),
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
                    .Get("role.Timebreaker.feedback.source")
                    .Replace(
                        "{seconds}",
                        Math.Ceiling(durationSeconds).ToString());

                hud.Notifier.AddDisconnectMessage(text);
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get(
                        "role.Timebreaker.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Timebreaker feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        StasisUntil.Clear();

        var local = PlayerControl.LocalPlayer;
        if (_localFrozenByParadox &&
            local != null &&
            local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected)
        {
            local.moveable = true;
        }

        _localFrozenByParadox = false;
    }
}
