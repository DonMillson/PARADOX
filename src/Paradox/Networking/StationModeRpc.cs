using Hazel;
using Paradox.Maps;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.StationMode)]
public sealed class StationModeRpc : PlayerCustomRpc<ParadoxPlugin, StationModeRpc.Data>
{
    public StationModeRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(bool Enabled);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
    public override void Write(MessageWriter writer, Data data) => writer.Write(data.Enabled);
    public override Data Read(MessageReader reader) => new(reader.ReadBoolean());

    public override void Handle(PlayerControl player, Data data)
    {
        if (player == null || AmongUsClient.Instance == null ||
            player.OwnerId != AmongUsClient.Instance.HostId)
        {
            Plugin.Log.LogWarning("Rejected unauthorized station mode change.");
            return;
        }

        try
        {
            ParadoxStationRuntime.ApplyMode(data.Enabled, isHost: false);
        }
        catch (Exception e)
        {
            ParadoxStationRuntime.Reset();
            Plugin.Log.LogError($"PARADOX STATION remote transition failed: {e}");
        }
    }
}
