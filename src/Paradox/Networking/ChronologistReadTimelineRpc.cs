using Hazel;
using ParadoxChronologistRole = Paradox.Roles.Chronologist.ChronologistRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ChronologistReadTimeline)]
public sealed class ChronologistReadTimelineRpc : PlayerCustomRpc<ParadoxPlugin, ChronologistReadTimelineRpc.Data>
{
    public ChronologistReadTimelineRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        float CooldownSeconds,
        byte HasDeath,
        float AgeSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.HasDeath);
        writer.Write(data.AgeSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadByte(),
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
                !ParadoxChronologistRole.IsChronologist(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Chronologist request from player {player.PlayerId}.");
                return;
            }

            ParadoxChronologistRole.TryReadTimeline(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxChronologistRole.IsChronologist(data.SourcePlayerId))
            return;

        ParadoxChronologistRole.ApplySyncedResult(
            data.SourcePlayerId,
            data.CooldownSeconds,
            data.HasDeath == 1,
            data.AgeSeconds);
    }
}
