using Hazel;
using ParadoxHarbingerRole = Paradox.Roles.Harbinger.HarbingerRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.HarbingerOmen)]
public sealed class HarbingerOmenRpc : PlayerCustomRpc<ParadoxPlugin, HarbingerOmenRpc.Data>
{
    public HarbingerOmenRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte PlayerId,
        int OmenCount,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.PlayerId);
        writer.Write(data.OmenCount);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadInt32(),
            reader.ReadSingle(),
            reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.Accepted != 0 ||
                data.PlayerId != player.PlayerId ||
                !ParadoxHarbingerRole.IsHarbinger(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Harbinger request from player {player.PlayerId}.");
                return;
            }

            ParadoxHarbingerRole.TryInvokeOmen(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxHarbingerRole.IsHarbinger(data.PlayerId))
            return;

        ParadoxHarbingerRole.ApplySyncedState(
            data.PlayerId,
            data.OmenCount,
            data.CooldownSeconds);
    }
}
