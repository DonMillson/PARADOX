using Hazel;
using ParadoxRevenantRole = Paradox.Roles.Revenant.RevenantRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.RevenantReturn)]
public sealed class RevenantReturnRpc : PlayerCustomRpc<ParadoxPlugin, RevenantReturnRpc.Data>
{
    public RevenantReturnRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte RevenantPlayerId,
        byte Action,
        float X,
        float Y,
        float Seconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.RevenantPlayerId);
        writer.Write(data.Action);
        writer.Write(data.X);
        writer.Write(data.Y);
        writer.Write(data.Seconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null ||
            player == null)
            return;

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            data.Action > 1 ||
            !ParadoxRevenantRole.IsRevenant(
                data.RevenantPlayerId))
            return;

        if (data.Action == 0)
        {
            ParadoxRevenantRole.ApplyPendingState(
                data.RevenantPlayerId,
                data.X,
                data.Y,
                data.Seconds);
            return;
        }

        ParadoxRevenantRole.ApplyReturnedState(
            data.RevenantPlayerId,
            data.X,
            data.Y,
            data.Seconds);
    }
}
