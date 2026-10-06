using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Chronologist;

public static class ChronologistRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsChronologist(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Chronologist;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryReadTimeline(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsChronologist(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Chronologist))
            return false;

        var latest = DeathEvidenceRegistry.All
            .OrderByDescending(record => record.TimeOfDeath)
            .FirstOrDefault();

        var hasDeath = latest != null;
        var ageSeconds = hasDeath
            ? Math.Max(0f, now - latest!.TimeOfDeath)
            : 0f;

        ApplySyncedResult(
            source.PlayerId,
            ParadoxRoleSettings.ChronologistCooldownSeconds,
            hasDeath,
            ageSeconds);

        BroadcastAccepted(source.PlayerId, hasDeath, ageSeconds);
        return true;
    }

    public static void ApplySyncedResult(
        byte sourcePlayerId,
        float cooldownSeconds,
        bool hasDeath,
        float ageSeconds)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(sourcePlayerId, hasDeath, ageSeconds);
    }

    private static void ShowFeedback(
        byte sourcePlayerId,
        bool hasDeath,
        float ageSeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            if (!hasDeath)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Chronologist.none"));
                return;
            }

            var text = ParadoxPlugin.Localizer.Get("role.Chronologist.result")
                .Replace("{seconds}", Math.Floor(ageSeconds).ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Chronologist feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        bool hasDeath,
        float ageSeconds)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<ChronologistReadTimelineRpc>.Instance.Send(
            sender,
            new ChronologistReadTimelineRpc.Data(
                sourcePlayerId,
                ParadoxRoleSettings.ChronologistCooldownSeconds,
                hasDeath ? (byte)1 : (byte)0,
                ageSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
