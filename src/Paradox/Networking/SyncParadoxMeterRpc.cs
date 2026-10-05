using Hazel;
using Paradox.Core;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.SyncParadoxMeter)]
public sealed class SyncParadoxMeterRpc : PlayerCustomRpc<ParadoxPlugin, SyncParadoxMeterRpc.Data>
{
    public SyncParadoxMeterRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(float Meter);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data) => writer.Write(data.Meter);

    public override Data Read(MessageReader reader) => new(reader.ReadSingle());

    public override void Handle(PlayerControl player, Data data)
    {
        ParadoxGame.ApplySyncedMeter(data.Meter);
    }
}
