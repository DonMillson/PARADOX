using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Silencer;

public static class SilencerRole
{
    private static readonly Dictionary<byte, byte> MarksBySource = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsSilencer(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Silencer;

    public static bool IsSilenced(byte playerId) =>
        MarksBySource.Values.Any(targetId => targetId == playerId);

    public static float CooldownRemaining(byte playerId, float now) =>
        CooldownEndsAt.TryGetValue(playerId, out var end)
            ? Math.Max(0f, end - now)
            : 0f;

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

    public static bool TrySilence(
        PlayerControl source,
        PlayerControl target)
    {
        if (source == null ||
            target == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        var now = Time.time;

        if (!IsSilencer(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            target.Data == null ||
            target.Data.IsDead ||
            target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            MeetingHud.Instance != null ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId) ||
            CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Silencer))
            return false;

        ApplySyncedMark(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.SilencerCooldownSeconds);

        BroadcastAccepted(
            source.PlayerId,
            target.PlayerId);

        return true;
    }

    public static void ApplySyncedMark(
        byte sourcePlayerId,
        byte targetPlayerId,
        float cooldownSeconds)
    {
        MarksBySource[sourcePlayerId] = targetPlayerId;

        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowSourceFeedback(sourcePlayerId);
    }

    public static void ClearMeetingTargets() =>
        MarksBySource.Clear();

    public static void ShowTargetMeetingFeedback()
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                !IsSilenced(local.PlayerId) ||
                hud == null ||
                hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    "role.Silencer.feedback.target"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Silencer target feedback failed: {e.Message}");
        }
    }

    public static void ShowBlockedFeedback(byte playerId)
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

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    "role.Silencer.blocked"));
        }
        catch { }
    }

    private static void ShowSourceFeedback(byte sourcePlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                local.PlayerId != sourcePlayerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    "role.Silencer.feedback.source"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Silencer source feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<SilencerMarkRpc>.Instance.Send(
            sender,
            new SilencerMarkRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.SilencerCooldownSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime()
    {
        MarksBySource.Clear();
        CooldownEndsAt.Clear();
    }
}
