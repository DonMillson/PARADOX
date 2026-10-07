using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Undertaker;

public static class UndertakerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, byte> CarriedBodyByPlayer = new();

    public static bool IsUndertaker(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Undertaker;

    public static bool IsCarrying(byte playerId) =>
        CarriedBodyByPlayer.ContainsKey(playerId);

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static DeadBody? FindClosestBody(PlayerControl source, float maxDistance)
    {
        if (source == null || source.Data == null || source.Data.IsDead)
            return null;

        DeadBody? closest = null;
        var closestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();

        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null || body.Reported || IsBodyCarried(body.ParentId))
                continue;

            var distance = Vector2.Distance(sourcePosition, body.TruePosition);
            if (distance > closestDistance)
                continue;

            closest = body;
            closestDistance = distance;
        }

        return closest;
    }

    public static bool TryPickup(PlayerControl source, DeadBody body)
    {
        if (source == null || body == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsUndertaker(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            body.Reported ||
            IsCarrying(source.PlayerId) ||
            IsBodyCarried(body.ParentId) ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), body.TruePosition);
        if (distance > source.MaxReportDistance + 0.15f)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Undertaker))
            return false;

        ApplySyncedPickup(source.PlayerId, body.ParentId);
        Broadcast(source.PlayerId, body.ParentId, 1, 0f);
        return true;
    }

    public static bool TryDrop(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            !IsUndertaker(source.PlayerId) ||
            !CarriedBodyByPlayer.TryGetValue(source.PlayerId, out var bodyPlayerId))
            return false;

        UpdateCarriedBodies();

        ApplySyncedDrop(
            source.PlayerId,
            bodyPlayerId,
            ParadoxRoleSettings.UndertakerCooldownSeconds);

        Broadcast(
            source.PlayerId,
            bodyPlayerId,
            2,
            ParadoxRoleSettings.UndertakerCooldownSeconds);

        return true;
    }

    public static void ApplySyncedPickup(byte sourcePlayerId, byte bodyPlayerId)
    {
        CarriedBodyByPlayer[sourcePlayerId] = bodyPlayerId;
        ShowFeedback(sourcePlayerId, false);
        UpdateCarriedBodies();
    }

    public static void ApplySyncedDrop(
        byte sourcePlayerId,
        byte bodyPlayerId,
        float cooldownSeconds)
    {
        UpdateCarriedBodies();
        CarriedBodyByPlayer.Remove(sourcePlayerId);

        if (cooldownSeconds > 0f)
        {
            CooldownEndsAt[sourcePlayerId] =
                Time.time + cooldownSeconds;
        }

        ShowFeedback(sourcePlayerId, true);
    }

    public static void UpdateCarriedBodies()
    {
        if (CarriedBodyByPlayer.Count == 0)
            return;

        foreach (var entry in CarriedBodyByPlayer.ToArray())
        {
            var carrier = FindPlayer(entry.Key);
            var body = FindBody(entry.Value);

            if (carrier == null ||
                carrier.Data == null ||
                carrier.Data.IsDead ||
                carrier.Data.Disconnected ||
                body == null)
            {
                CarriedBodyByPlayer.Remove(entry.Key);
                continue;
            }

            var position = carrier.GetTruePosition();
            body.transform.position = new Vector3(
                position.x,
                position.y + ParadoxRoleSettings.UndertakerCarryOffsetY,
                body.transform.position.z);
        }
    }

    private static bool IsBodyCarried(byte bodyPlayerId) =>
        CarriedBodyByPlayer.Values.Contains(bodyPlayerId);

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.PlayerId == playerId)
                return player;
        }

        return null;
    }

    private static DeadBody? FindBody(byte bodyPlayerId)
    {
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body != null && body.ParentId == bodyPlayerId)
                return body;
        }

        return null;
    }

    private static void Broadcast(
        byte sourcePlayerId,
        byte bodyPlayerId,
        byte action,
        float cooldownSeconds)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<UndertakerCarryBodyRpc>.Instance.Send(
            sender,
            new UndertakerCarryBodyRpc.Data(
                sourcePlayerId,
                bodyPlayerId,
                action,
                cooldownSeconds,
                1),
            immediately: true);
    }

    private static void ShowFeedback(byte sourcePlayerId, bool dropped)
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
                    dropped
                        ? "role.Undertaker.feedback.drop"
                        : "role.Undertaker.feedback.pickup"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Undertaker feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        CarriedBodyByPlayer.Clear();
    }
}
