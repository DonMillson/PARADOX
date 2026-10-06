using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Guardian;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Devourer;

public static class DevourerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, float> PendingBodyRemoval = new();

    public static bool IsDevourer(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Devourer;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryDevour(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsDevourer(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            GuardianRole.IsProtected(target.PlayerId) ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), target.GetTruePosition());
        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Devourer))
            return false;

        source.MurderPlayer(target, MurderResultFlags.Succeeded);

        if (target.Data == null || !target.Data.IsDead)
            return false;

        ApplySyncedConsume(
            source.PlayerId,
            target.PlayerId,
            ParadoxRoleSettings.DevourerCooldownSeconds);

        BroadcastAccepted(source.PlayerId, target.PlayerId);
        ShowFeedback(source.PlayerId);
        return true;
    }

    public static void ApplySyncedConsume(
        byte sourcePlayerId,
        byte targetPlayerId,
        float cooldownSeconds)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        PendingBodyRemoval[targetPlayerId] = Time.time + 5f;
        UpdatePendingBodyRemoval();
    }

    public static void UpdatePendingBodyRemoval()
    {
        if (PendingBodyRemoval.Count == 0)
            return;

        var now = Time.time;
        foreach (var entry in PendingBodyRemoval.ToArray())
        {
            if (TryRemoveBody(entry.Key) || now >= entry.Value)
                PendingBodyRemoval.Remove(entry.Key);
        }
    }

    private static bool TryRemoveBody(byte bodyPlayerId)
    {
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null || body.ParentId != bodyPlayerId)
                continue;

            body.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(body.gameObject);
            return true;
        }

        return false;
    }

    private static void ShowFeedback(byte sourcePlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get("role.Devourer.feedback"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Devourer feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(byte sourcePlayerId, byte targetPlayerId)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<DevourerConsumeRpc>.Instance.Send(
            sender,
            new DevourerConsumeRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                ParadoxRoleSettings.DevourerCooldownSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        PendingBodyRemoval.Clear();
    }
}
