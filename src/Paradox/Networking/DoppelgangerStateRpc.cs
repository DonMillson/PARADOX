using Hazel;
using Paradox.Roles.Doppelganger;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.DoppelgangerState)]
public sealed class DoppelgangerStateRpc : PlayerCustomRpc<ParadoxPlugin, DoppelgangerStateRpc.Data>
{
    public DoppelgangerStateRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte PlayerId,
        byte TargetPlayerId,
        float DisguiseDurationSeconds,
        float CooldownSeconds,
        byte Active);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.PlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.DisguiseDurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Active);
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
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (player.OwnerId != AmongUsClient.Instance.HostId)
        {
            Plugin.Log.LogWarning(
                $"Rejected Doppelganger state sync from non-host player {player.PlayerId}.");
            return;
        }

        if (data.Active > 1)
        {
            Plugin.Log.LogWarning(
                $"Rejected invalid Doppelganger active flag {data.Active}.");
            return;
        }

        DoppelgangerRole.ApplySyncedState(
            data.PlayerId,
            data.TargetPlayerId,
            Math.Max(0f, data.DisguiseDurationSeconds),
            Math.Max(0f, data.CooldownSeconds),
            data.Active == 1);
    }
}
