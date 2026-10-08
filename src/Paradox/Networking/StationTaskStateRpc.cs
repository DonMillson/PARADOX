using Hazel;
using Paradox.Maps;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.StationTaskState)]
public sealed class StationTaskStateRpc
    : PlayerCustomRpc<ParadoxPlugin, StationTaskStateRpc.Data>
{
    public StationTaskStateRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte RoomIndex, byte Stage);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.RoomIndex);
        writer.Write(data.Stage);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (player == null || AmongUsClient.Instance == null ||
            player.OwnerId != AmongUsClient.Instance.HostId)
        {
            Plugin.Log.LogWarning("Rejected unauthorized PARADOX STATION task state.");
            return;
        }

        ParadoxStationRuntime.SetTaskStep(data.RoomIndex, data.Stage);
    }
}
