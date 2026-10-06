using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Tracker;

public static class TrackerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, byte> ActiveTarget = new();
    private static readonly Dictionary<byte, float> TrackingEndsAt = new();

    public static bool IsTracker(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Tracker;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static float TrackingRemaining(byte playerId, float now)
    {
        if (!TrackingEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        if (now < end)
            return end - now;

        ActiveTarget.Remove(playerId);
        TrackingEndsAt.Remove(playerId);
        return 0f;
    }

    public static bool TryGetActiveTarget(byte sourcePlayerId, out PlayerControl? target)
    {
        target = null;
        if (TrackingRemaining(sourcePlayerId, Time.time) <= 0f ||
            !ActiveTarget.TryGetValue(sourcePlayerId, out var targetId))
            return false;

        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate == null || candidate.PlayerId != targetId ||
                candidate.Data == null || candidate.Data.Disconnected)
                continue;

            target = candidate;
            return true;
        }

        return false;
    }

    public static bool TryTrack(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsTracker(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f ||
            TrackingRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), target.GetTruePosition());
        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Tracker))
            return false;

        ApplySyncedTracking(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.TrackerDurationSeconds,
            ParadoxRoleSettings.TrackerCooldownSeconds);

        ShowFeedback(source.PlayerId);
        BroadcastAccepted(source.PlayerId, target.PlayerId);
        return true;
    }

    public static void ApplySyncedTracking(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds,
        float cooldownSeconds)
    {
        ActiveTarget[sourcePlayerId] = targetPlayerId;
        TrackingEndsAt[sourcePlayerId] = Time.time + Math.Max(0f, durationSeconds);
        CooldownEndsAt[sourcePlayerId] = Time.time + Math.Max(0f, cooldownSeconds);
    }

    public static void ShowFeedback(byte sourcePlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer.Get("role.Tracker.feedback")
                .Replace("{seconds}", Math.Ceiling(ParadoxRoleSettings.TrackerDurationSeconds).ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning($"Tracker feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(byte sourcePlayerId, byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<TrackerTrackRpc>.Instance.Send(
            sender,
            new TrackerTrackRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.TrackerDurationSeconds,
                ParadoxRoleSettings.TrackerCooldownSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        ActiveTarget.Clear();
        TrackingEndsAt.Clear();
    }
}
