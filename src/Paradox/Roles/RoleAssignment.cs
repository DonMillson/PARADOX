using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles;

public static class RoleAssignment
{
    public static void Assign(PlayerControl player, RoleId role)
    {
        if (player == null || !AmongUsClient.Instance.AmHost)
            return;

        PlayerRoleRegistry.Assign(player.PlayerId, role);

        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<AssignRoleRpc>.Instance.Send(
            sender,
            new AssignRoleRpc.Data(player.PlayerId, (int)role),
            immediately: true);
    }

    public static void Reset() => PlayerRoleRegistry.Clear();
}
