using Hazel;
using Paradox.Core;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.TriggerParadoxFinalEvent)]
public sealed class TriggerParadoxFinalEventRpc : PlayerCustomRpc<ParadoxPlugin, TriggerParadoxFinalEventRpc.Data>
{
    public TriggerParadoxFinalEventRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte EventType);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data) =>
        writer.Write(data.EventType);

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (player.OwnerId != AmongUsClient.Instance.HostId)
        {
            Plugin.Log.LogWarning(
                $"Rejected PARADOX final event from non-host player {player.PlayerId}.");
            return;
        }

        var finalEvent = (ParadoxFinalEventType)data.EventType;
        if (!Enum.IsDefined(typeof(ParadoxFinalEventType), finalEvent))
        {
            Plugin.Log.LogWarning(
                $"Rejected unknown PARADOX final event id {data.EventType}.");
            return;
        }

        ParadoxEventRuntime.TriggerFinal(finalEvent);
    }
}
