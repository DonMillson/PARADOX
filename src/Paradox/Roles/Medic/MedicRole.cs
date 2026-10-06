using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Parasite;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Medic;

public static class MedicRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsMedic(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Medic;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var cooldownEnd))
            return 0f;

        return Math.Max(0f, cooldownEnd - now);
    }

    public static bool TryScan(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsMedic(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            target.PlayerId == source.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownEndsAt.TryGetValue(source.PlayerId, out var cooldownEnd) &&
            now < cooldownEnd)
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Medic))
            return false;

        var cured = ParasiteRole.RemoveSyncedInfection(target.PlayerId);

        ApplySyncedResult(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.MedicCooldownSeconds,
            cured);

        BroadcastAccepted(source.PlayerId, target.PlayerId, cured);
        return true;
    }

    public static void ApplySyncedResult(
        byte sourcePlayerId,
        byte targetPlayerId,
        float cooldownSeconds,
        bool cured)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        if (cured)
            ParasiteRole.RemoveSyncedInfection(targetPlayerId);

        ShowFeedback(sourcePlayerId, targetPlayerId, cured);
    }

    private static void ShowFeedback(
        byte sourcePlayerId,
        byte targetPlayerId,
        bool cured)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || hud == null || hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get(
                        cured ? "role.Medic.feedback.cured" : "role.Medic.feedback.healthy"));
            }
            else if (cured && local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Medic.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Medic scan feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte targetPlayerId,
        bool cured)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<MedicScanRpc>.Instance.Send(
            sender,
            new MedicScanRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.MedicCooldownSeconds,
                cured ? (byte)1 : (byte)0,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
