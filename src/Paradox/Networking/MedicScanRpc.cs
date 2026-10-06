using Hazel;
using Paradox.Roles.Medic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.MedicScan)]
public sealed class MedicScanRpc : PlayerCustomRpc<ParadoxPlugin, MedicScanRpc.Data>
{
    public MedicScanRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float CooldownSeconds,
        byte Cured,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Cured);
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
                !MedicRole.IsMedic(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Medic scan request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
            {
                Plugin.Log.LogWarning(
                    $"Rejected Medic request: target {data.TargetPlayerId} not found.");
                return;
            }

            MedicRole.TryScan(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !MedicRole.IsMedic(data.SourcePlayerId))
            return;

        MedicRole.ApplySyncedResult(
            data.SourcePlayerId,
            data.TargetPlayerId,
            data.CooldownSeconds,
            data.Cured == 1);
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
