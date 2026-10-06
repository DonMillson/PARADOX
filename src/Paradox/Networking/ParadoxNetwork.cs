using Paradox.Roles;
using Paradox.Settings;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

public static class ParadoxNetwork
{
    public static void SendHandshake(PlayerControl player)
    {
        var local = ParadoxHandshake.Local;

        Rpc<HandshakeRpc>.Instance.Send(
            player,
            new HandshakeRpc.Data(
                local.ModName,
                local.ModVersion,
                local.ProtocolVersion),
            immediately: true);
    }

    public static void BroadcastRoleSettings()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            PlayerControl.LocalPlayer == null)
            return;

        foreach (var definition in RoleRegistry.All)
        {
            if (!ParadoxRoleSettings.IsImplemented(definition.Id))
                continue;

            BroadcastRoleSetting(definition.Id);
        }
    }

    public static void BroadcastRoleSetting(RoleId role)
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            PlayerControl.LocalPlayer == null ||
            !ParadoxRoleSettings.IsImplemented(role))
            return;

        Rpc<SyncRoleSettingRpc>.Instance.Send(
            PlayerControl.LocalPlayer,
            new SyncRoleSettingRpc.Data(
                (int)role,
                ParadoxRoleSettings.IsEnabled(role),
                ParadoxRoleSettings.GetSpawnChance(role)),
            immediately: true);
    }
}
