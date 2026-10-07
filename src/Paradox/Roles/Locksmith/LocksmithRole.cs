using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Locksmith;

public static class LocksmithRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsLocksmith(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Locksmith;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryFindNearestClosedDoor(
        PlayerControl source,
        float maxDistance,
        out int doorIndex)
    {
        doorIndex = -1;

        if (source == null ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            ShipStatus.Instance == null ||
            ShipStatus.Instance.AllDoors == null)
            return false;

        var allDoors = ShipStatus.Instance.AllDoors;
        var sourcePosition = source.GetTruePosition();
        var bestDistance = Math.Max(0f, maxDistance);

        for (var i = 0; i < allDoors.Length; i++)
        {
            var door = allDoors[i];
            if (door == null || door.IsOpen)
                continue;

            var doorPosition = (Vector2)door.transform.position;
            var distance = Vector2.Distance(sourcePosition, doorPosition);
            if (distance > bestDistance)
                continue;

            bestDistance = distance;
            doorIndex = i;
        }

        return doorIndex >= 0;
    }

    public static bool TryOpenDoor(PlayerControl source, int doorIndex)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsLocksmith(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            !source.CanMove ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        if (!TryGetDoor(doorIndex, out var door) ||
            door == null ||
            door.IsOpen)
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            (Vector2)door.transform.position);

        if (distance > ParadoxRoleSettings.LocksmithUseRange)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Locksmith))
            return false;

        ApplySyncedOpen(
            source.PlayerId,
            doorIndex,
            ParadoxRoleSettings.LocksmithCooldownSeconds);

        BroadcastAccepted(source.PlayerId, doorIndex);
        return true;
    }

    public static void ApplySyncedOpen(
        byte sourcePlayerId,
        int doorIndex,
        float cooldownSeconds)
    {
        if (TryGetDoor(doorIndex, out var door) &&
            door != null &&
            !door.IsOpen)
        {
            door.SetDoorway(true);
        }

        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(sourcePlayerId);
    }

    private static bool TryGetDoor(int doorIndex, out OpenableDoor? door)
    {
        door = null;

        var ship = ShipStatus.Instance;
        var allDoors = ship?.AllDoors;
        if (allDoors == null ||
            doorIndex < 0 ||
            doorIndex >= allDoors.Length)
            return false;

        door = allDoors[doorIndex];
        return door != null;
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        int doorIndex)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<LocksmithOpenDoorRpc>.Instance.Send(
            sender,
            new LocksmithOpenDoorRpc.Data(
                sourcePlayerId,
                doorIndex,
                ParadoxRoleSettings.LocksmithCooldownSeconds,
                1),
            immediately: true);
    }

    private static void ShowFeedback(byte sourcePlayerId)
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
                ParadoxPlugin.Localizer.Get("role.Locksmith.feedback"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Locksmith feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime() =>
        CooldownEndsAt.Clear();
}
