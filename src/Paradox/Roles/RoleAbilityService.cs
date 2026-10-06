using Paradox.Core;
using Paradox.Roles.Observer;

namespace Paradox.Roles;

public static class RoleAbilityService
{
    public static bool Use(PlayerControl source, RoleId role)
    {
        if (source == null || !AmongUsClient.Instance.AmHost)
            return false;

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
