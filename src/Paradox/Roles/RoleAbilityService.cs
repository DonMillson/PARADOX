using Paradox.Core;
using Paradox.Roles.Observer;

namespace Paradox.Roles;

public static class RoleAbilityService
{
    public static bool Use(PlayerControl source, RoleId role)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            MeetingHud.Instance != null)
            return false;

        // Rift Warden is PARADOX's emergency ability: its only purpose is
        // to break an active reality jam. All other roles remain blocked.
        if ((ParadoxEventRuntime.RoleAbilitiesBlocked && role != RoleId.RiftWarden) ||
            Paradox.Roles.Corruptor.CorruptorRole.IsCorrupted(source.PlayerId))
        {
            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX ability blocked: player {source.PlayerId}, role {role}.");
            return false;
        }

        if (!PlayerRoleRegistry.TryGet(source.PlayerId, out var assignedRole) ||
            assignedRole != role)
            return false;

        ParadoxGame.AddFrom(ParadoxMeterSource.RoleAbility);
        ObserverNetwork.BroadcastTrace(source);

        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX ability used: player {source.PlayerId}, role {role}.");

        return true;
    }
}
