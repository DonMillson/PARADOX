using Hazel;
using Paradox.Roles.Observer;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ObserverTrace)]
public sealed class ObserverTraceRpc : PlayerCustomRpc<ParadoxPlugin, ObserverTraceRpc.Data>
{
    public ObserverTraceRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte SourcePlayerId, float CreatedAt);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.CreatedAt);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadSingle());

    public override void Handle(PlayerControl player, Data data)
    {
        ObserverRole.RecordAbilityTrace(data.SourcePlayerId, data.CreatedAt);
    }
}
