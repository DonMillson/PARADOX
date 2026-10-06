using Hazel;
using ParadoxRiftmakerRole = Paradox.Roles.Riftmaker.RiftmakerRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.RiftmakerWarp)]
public sealed class RiftmakerWarpRpc : PlayerCustomRpc<ParadoxPlugin, RiftmakerWarpRpc.Data>
{
    public RiftmakerWarpRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte Action,
        float X,
        float Y,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.Action);
        writer.Write(data.X);
        writer.Write(data.Y);
        writer.Write(data.CooldownSeconds);
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
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.Accepted != 0 ||
                data.SourcePlayerId != player.PlayerId ||
                !ParadoxRiftmakerRole.IsRiftmaker(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Riftmaker request from player {player.PlayerId}.");
                return;
            }

            ParadoxRiftmakerRole.TryUse(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxRiftmakerRole.IsRiftmaker(data.SourcePlayerId))
            return;

        ParadoxRiftmakerRole.ApplySyncedAction(
            data.SourcePlayerId,
            data.Action,
            data.X,
            data.Y,
            data.CooldownSeconds);
    }
}
