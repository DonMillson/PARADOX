using Hazel;
using ParadoxAnalystRole = Paradox.Roles.Analyst.AnalystRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.AnalystScan)]
public sealed class AnalystScanRpc : PlayerCustomRpc<ParadoxPlugin, AnalystScanRpc.Data>
{
    public AnalystScanRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        float CooldownSeconds,
        int Alive,
        int Deaths,
        int Traces,
        float Meter,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Alive);
        writer.Write(data.Deaths);
        writer.Write(data.Traces);
        writer.Write(data.Meter);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadInt32(),
            reader.ReadInt32(),
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
                data.SourcePlayerId != player.PlayerId ||
                !ParadoxAnalystRole.IsAnalyst(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Analyst request from player {player.PlayerId}.");
                return;
            }

            ParadoxAnalystRole.TryAnalyze(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxAnalystRole.IsAnalyst(data.SourcePlayerId))
            return;

        ParadoxAnalystRole.ApplySyncedResult(
            data.SourcePlayerId,
            data.CooldownSeconds,
            data.Alive,
            data.Deaths,
            data.Traces,
            data.Meter);
    }
}
