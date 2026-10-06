using Hazel;
using Paradox.Roles.Anomaly;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.AnomalyPulse)]
public sealed class AnomalyPulseRpc : PlayerCustomRpc<ParadoxPlugin, AnomalyPulseRpc.Data>
{
    public AnomalyPulseRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
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
                !AnomalyRole.IsAnomaly(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Anomaly pulse request from player {player.PlayerId}.");
                return;
            }

            AnomalyRole.TryPulse(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !AnomalyRole.IsAnomaly(data.SourcePlayerId))
            return;

        AnomalyRole.ApplySyncedCooldown(
            data.SourcePlayerId,
            data.CooldownSeconds);
    }
}
