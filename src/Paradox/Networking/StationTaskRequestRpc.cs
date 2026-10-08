using Hazel;
using Paradox.Maps;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.StationTaskRequest)]
public sealed class StationTaskRequestRpc
    : PlayerCustomRpc<ParadoxPlugin, StationTaskRequestRpc.Data>
{
    public StationTaskRequestRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte RoomIndex);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
    public override void Write(MessageWriter writer, Data data) => writer.Write(data.RoomIndex);
    public override Data Read(MessageReader reader) => new(reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (player == null || AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        // The caller can request only its own console interaction.
        // Host independently verifies health, activity, position, range and cooldown.
        ParadoxStationRuntime.AcceptTaskHost(player, data.RoomIndex);
    }
}
