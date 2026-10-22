using Hazel;
using Paradox.Roles.Swapper;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.SwapperMeeting)]
public sealed class SwapperMeetingRpc : PlayerCustomRpc<ParadoxPlugin, SwapperMeetingRpc.Data>
{
    public SwapperMeetingRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    // Kind: 0 = request (Swapper -> host), 1 = host acknowledgement,
    //       2 = host-validated punishment broadcast.
    public readonly record struct Data(byte Kind, byte SwapperId, byte First, byte Second);
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.Kind);
        writer.Write(data.SwapperId);
        writer.Write(data.First);
        writer.Write(data.Second);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (data.Kind != 0 ||
                player.OwnerId == AmongUsClient.Instance.HostId ||
                data.SwapperId != player.PlayerId ||
                !SwapperRole.IsSwapper(player.PlayerId))
                return;

            if (!SwapperRole.TrySetSwap(player, data.First, data.Second))
                Plugin.Log.LogWarning(
                    $"Swapper denied vote swap request from {player.PlayerId}.");
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId)
        {
            Plugin.Log.LogWarning("Rejected non-host Swapper event.");
            return;
        }

        if (data.Kind == 1)
        {
            if (SwapperRole.IsSwapper(data.SwapperId))
                SwapperRole.NotifyOwnSwapper(data.SwapperId, data.First, data.Second);
        }
        else if (data.Kind == 2 && SwapperRole.IsSwapper(data.SwapperId))
        {
            SwapperRole.ApplyPunishment(data.SwapperId);
        }
    }
}
