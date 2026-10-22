using Hazel;
using Paradox.Roles.RiftWarden;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.RiftWardenBurst)]
public sealed class RiftWardenBurstRpc :
    PlayerCustomRpc<ParadoxPlugin, RiftWardenBurstRpc.Data>
{
    public RiftWardenBurstRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte PlayerId, byte Accepted);
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.PlayerId);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (data.Accepted != 0 ||
                player.OwnerId == AmongUsClient.Instance.HostId ||
                data.PlayerId != player.PlayerId ||
                !RiftWardenRole.IsRiftWarden(player.PlayerId))
                return;

            RiftWardenRole.TryBurst(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 || !RiftWardenRole.IsRiftWarden(data.PlayerId))
            return;
        RiftWardenRole.ApplySyncedBurst(data.PlayerId);
    }
}
