using Hazel;
using ParadoxBlackmailerRole = Paradox.Roles.Blackmailer.BlackmailerRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.BlackmailerMark)]
public sealed class BlackmailerMarkRpc : PlayerCustomRpc<ParadoxPlugin, BlackmailerMarkRpc.Data>
{
    public BlackmailerMarkRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

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
        if (AmongUsClient.Instance == null ||
            player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.Accepted != 0 ||
                data.SourcePlayerId != player.PlayerId ||
                !ParadoxBlackmailerRole.IsBlackmailer(
                    player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Blackmailer request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target != null)
                ParadoxBlackmailerRole.TryBlackmail(
                    player,
                    target);

            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxBlackmailerRole.IsBlackmailer(
                data.SourcePlayerId))
            return;

        ParadoxBlackmailerRole.ApplySyncedMark(
            data.SourcePlayerId,
            data.TargetPlayerId,
            data.CooldownSeconds);
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
