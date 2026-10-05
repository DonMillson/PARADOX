using Hazel;
using Paradox.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.AssignRole)]
public sealed class AssignRoleRpc : PlayerCustomRpc<ParadoxPlugin, AssignRoleRpc.Data>
{
    public AssignRoleRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte PlayerId, int Role);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.PlayerId);
        writer.Write(data.Role);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadInt32());

    public override void Handle(PlayerControl player, Data data)
    {
        if (!Enum.IsDefined(typeof(RoleId), data.Role))
        {
            Plugin.Log.LogWarning($"Rejected unknown PARADOX role id {data.Role} for player {data.PlayerId}");
            return;
        }

        var role = (RoleId)data.Role;
        PlayerRoleRegistry.Assign(data.PlayerId, role);
        Plugin.Log.LogInfo($"PARADOX role synchronized: player {data.PlayerId} -> {role}");
    }
}
