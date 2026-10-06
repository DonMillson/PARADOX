using Hazel;
using ParadoxDevourerRole = Paradox.Roles.Devourer.DevourerRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.DevourerConsume)]
public sealed class DevourerConsumeRpc : PlayerCustomRpc<ParadoxPlugin, DevourerConsumeRpc.Data>
{
    public DevourerConsumeRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
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
                !ParadoxDevourerRole.IsDevourer(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Devourer request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
                return;

            ParadoxDevourerRole.TryDevour(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxDevourerRole.IsDevourer(data.SourcePlayerId))
            return;

        ParadoxDevourerRole.ApplySyncedConsume(
            data.SourcePlayerId,
            data.TargetPlayerId,
            data.CooldownSeconds);
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
