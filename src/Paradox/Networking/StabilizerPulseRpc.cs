using Hazel;
using Paradox.Roles.Stabilizer;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.StabilizerPulse)]
public sealed class StabilizerPulseRpc : PlayerCustomRpc<ParadoxPlugin, StabilizerPulseRpc.Data>
{
    public StabilizerPulseRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        float CooldownSeconds,
        float ReducedAmount,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.ReducedAmount);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
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
                !StabilizerRole.IsStabilizer(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Stabilizer request from player {player.PlayerId}.");
                return;
            }

            StabilizerRole.TryStabilize(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !StabilizerRole.IsStabilizer(data.SourcePlayerId))
            return;

        StabilizerRole.ApplySyncedCooldown(
            data.SourcePlayerId,
            data.CooldownSeconds);

        StabilizerRole.ShowFeedback(
            data.SourcePlayerId,
            data.ReducedAmount);
    }
}
