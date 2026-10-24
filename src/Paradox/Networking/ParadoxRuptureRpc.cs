using Hazel;
using Paradox.Roles.ParadoxTeam;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ParadoxRupture)]
public sealed class ParadoxRuptureRpc : PlayerCustomRpc<ParadoxPlugin, ParadoxRuptureRpc.Data>
{
    public ParadoxRuptureRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }
    public readonly record struct Data(byte SourcePlayerId, float CooldownSeconds, byte Accepted);
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }
    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadSingle(), reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;
        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId ||
                data.Accepted != 0 || data.SourcePlayerId != player.PlayerId ||
                !ParadoxRole.IsParadox(player.PlayerId))
                return;
            ParadoxRole.TryRupture(player);
            return;
        }
        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 || !ParadoxRole.IsParadox(data.SourcePlayerId))
            return;
        ParadoxRole.ApplySyncedCooldown(data.SourcePlayerId, data.CooldownSeconds);
    }
}
