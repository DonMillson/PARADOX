using Hazel;
using Paradox.Roles;
using Paradox.Settings;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.SyncRoleSetting)]
public sealed class SyncRoleSettingRpc : PlayerCustomRpc<ParadoxPlugin, SyncRoleSettingRpc.Data>
{
    public SyncRoleSettingRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(int Role, bool Enabled, int Chance);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.Role);
        writer.Write(data.Enabled);
        writer.Write(data.Chance);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadInt32(), reader.ReadBoolean(), reader.ReadInt32());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (player.OwnerId != AmongUsClient.Instance.HostId)
        {
            Plugin.Log.LogWarning(
                $"Rejected PARADOX role-setting sync from non-host client {player.OwnerId}.");
            return;
        }

        if (!Enum.IsDefined(typeof(RoleId), data.Role))
            return;

        var role = (RoleId)data.Role;
        if (!ParadoxRoleSettings.IsImplemented(role))
            return;

        ParadoxRoleSettings.SetEnabled(role, data.Enabled);
        ParadoxRoleSettings.SetSpawnChance(role, Math.Clamp(data.Chance, 0, 100));

        Plugin.Log.LogInfo(
            $"PARADOX lobby role setting synchronized: {role}, enabled={data.Enabled}, chance={data.Chance}%");
    }
}
