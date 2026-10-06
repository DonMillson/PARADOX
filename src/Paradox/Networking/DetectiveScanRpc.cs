using Hazel;
using ParadoxDetectiveRole = Paradox.Roles.Detective.DetectiveRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.DetectiveScan)]
public sealed class DetectiveScanRpc : PlayerCustomRpc<ParadoxPlugin, DetectiveScanRpc.Data>
{
    public DetectiveScanRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float CooldownSeconds,
        byte ViolentResidue,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.ViolentResidue);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadByte(),
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
                !ParadoxDetectiveRole.IsDetective(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Detective request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
                return;

            ParadoxDetectiveRole.TryInvestigate(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxDetectiveRole.IsDetective(data.SourcePlayerId))
            return;

        ParadoxDetectiveRole.ApplySyncedResult(
            data.SourcePlayerId,
            data.CooldownSeconds,
            data.ViolentResidue == 1);
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null && candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }
}
