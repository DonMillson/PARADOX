using Hazel;
using ParadoxTechnicianRole = Paradox.Roles.Technician.TechnicianRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.TechnicianShield)]
public sealed class TechnicianShieldRpc :
    PlayerCustomRpc<ParadoxPlugin, TechnicianShieldRpc.Data>
{
    public TechnicianShieldRpc(ParadoxPlugin plugin, uint id) :
        base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        float DurationSeconds,
        float CooldownSeconds,
        byte Action,
        byte Accepted);

    public override RpcLocalHandling LocalHandling =>
        RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Action);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadSingle(),
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
                !ParadoxTechnicianRole.IsTechnician(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Technician request from player {player.PlayerId}.");
                return;
            }

            ParadoxTechnicianRole.TryActivateShield(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1)
            return;

        if (data.Action == 1 &&
            ParadoxTechnicianRole.IsTechnician(data.SourcePlayerId))
        {
            ParadoxTechnicianRole.ApplySyncedShield(
                data.SourcePlayerId,
                data.DurationSeconds,
                data.CooldownSeconds);
            return;
        }

        if (data.Action == 2)
        {
            ParadoxTechnicianRole.ApplySyncedSabotageBlocked(
                data.SourcePlayerId);
        }
    }
}
