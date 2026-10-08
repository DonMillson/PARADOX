using Hazel;
using Paradox.Maps;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.StationVentRequest)]
public sealed class StationVentRequestRpc
    : PlayerCustomRpc<ParadoxPlugin, StationVentRequestRpc.Data>
{
    public StationVentRequestRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte LinkIndex, bool FromFirst);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.LinkIndex);
        writer.Write(data.FromFirst);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadBoolean());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost || player == null)
            return;

        // Requests are intentionally untrusted: range, role, map mode,
        // destination and cooldown are all checked by the host.
        ParadoxStationRuntime.TryUseVentHost(
            player, data.LinkIndex, data.FromFirst);
    }
}
