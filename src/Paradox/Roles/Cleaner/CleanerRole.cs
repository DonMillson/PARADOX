using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Cleaner;

public static class CleanerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsCleaner(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Cleaner;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool IsReady(byte playerId, float now) =>
        CooldownRemaining(playerId, now) <= 0f;

    public static DeadBody? FindClosestBody(PlayerControl source, float maxDistance)
    {
        if (source == null || source.Data == null || source.Data.IsDead)
            return null;

        DeadBody? closest = null;
        var closestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();

        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null || body.Reported)
                continue;

            var distance = Vector2.Distance(sourcePosition, body.TruePosition);
            if (distance > closestDistance)
                continue;

            closest = body;
            closestDistance = distance;
        }

        return closest;
    }

    public static bool TryClean(PlayerControl source, DeadBody body)
    {
        if (source == null || body == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsCleaner(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            body.Reported)
            return false;

        var now = Time.time;
        if (!IsReady(source.PlayerId, now))
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), body.TruePosition);
        if (distance > source.MaxReportDistance + 0.15f)
            return false;

        var bodyPlayerId = body.ParentId;

        if (!RoleAbilityService.Use(source, RoleId.Cleaner))
            return false;

        if (!RemoveBody(bodyPlayerId))
            return false;

        CooldownEndsAt[source.PlayerId] =
            now + ParadoxRoleSettings.CleanerCooldownSeconds;

        var sender = PlayerControl.LocalPlayer;
        if (sender != null)
        {
            Rpc<CleanerCleanBodyRpc>.Instance.Send(
                sender,
                new CleanerCleanBodyRpc.Data(
                    source.PlayerId,
                    bodyPlayerId,
                    ParadoxRoleSettings.CleanerCooldownSeconds),
                immediately: true);
        }

        ParadoxPlugin.Instance.Log.LogInfo(
            $"Cleaner {source.PlayerId} removed body {bodyPlayerId}.");

        return true;
    }

    public static void ApplySyncedClean(
        byte cleanerPlayerId,
        byte bodyPlayerId,
        float cooldownSeconds)
    {
        RemoveBody(bodyPlayerId);

        if (cooldownSeconds > 0f)
        {
            CooldownEndsAt[cleanerPlayerId] =
                Time.time + cooldownSeconds;
        }
    }

    private static bool RemoveBody(byte bodyPlayerId)
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

    public static void Reset() => CooldownEndsAt.Clear();
}
