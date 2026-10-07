using Hazel;
using ParadoxSaboteurRole = Paradox.Roles.Saboteur.SaboteurRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.SaboteurOverload)]
public sealed class SaboteurOverloadRpc :
    PlayerCustomRpc<ParadoxPlugin, SaboteurOverloadRpc.Data>
{
    public SaboteurOverloadRpc(ParadoxPlugin plugin, uint id) :
        base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte Armed,
        float CooldownSeconds,
        byte Action,
        byte Accepted);

    public override RpcLocalHandling LocalHandling =>
        RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.Armed);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Action);
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
                data.Action != 0 ||
                data.SourcePlayerId != player.PlayerId ||
                !ParadoxSaboteurRole.IsSaboteur(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Saboteur request from player {player.PlayerId}.");
                return;
            }

            ParadoxSaboteurRole.TryArm(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxSaboteurRole.IsSaboteur(data.SourcePlayerId))
            return;

        ParadoxSaboteurRole.ApplySyncedState(
            data.SourcePlayerId,
            data.Armed == 1,
            data.CooldownSeconds,
            data.Action == 2);
    }
}
