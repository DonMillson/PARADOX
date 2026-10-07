using Hazel;
using ParadoxShapeshifterXRole = Paradox.Roles.ShapeshifterX.ShapeshifterXRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ShapeshifterXSwap)]
public sealed class ShapeshifterXSwapRpc : PlayerCustomRpc<ParadoxPlugin, ShapeshifterXSwapRpc.Data>
{
    public ShapeshifterXSwapRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float DurationSeconds,
        float CooldownSeconds,
        byte State);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.State);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null ||
            player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.State != 2 ||
                data.SourcePlayerId != player.PlayerId ||
                !ParadoxShapeshifterXRole.IsShapeshifterX(
                    player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Shapeshifter X request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
                return;

            ParadoxShapeshifterXRole.TrySwap(
                player,
                target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.State > 1 ||
            !ParadoxShapeshifterXRole.IsShapeshifterX(
                data.SourcePlayerId))
            return;

        ParadoxShapeshifterXRole.ApplySyncedState(
            data.SourcePlayerId,
            data.TargetPlayerId,
            data.DurationSeconds,
            data.CooldownSeconds,
            data.State == 1);
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null &&
                candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }
}
