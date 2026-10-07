using Hazel;
using ParadoxPhantomRole = Paradox.Roles.Phantom.PhantomRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.PhantomPhase)]
public sealed class PhantomPhaseRpc : PlayerCustomRpc<ParadoxPlugin, PhantomPhaseRpc.Data>
{
    public PhantomPhaseRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte PlayerId,
        byte Action,
        float DurationSeconds,
        float CooldownSeconds,
        int EscapeCount,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.PlayerId);
        writer.Write(data.Action);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.EscapeCount);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadInt32(),
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
                data.Action != 0 ||
                data.PlayerId != player.PlayerId ||
                !ParadoxPhantomRole.IsPhantom(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Phantom request from player {player.PlayerId}.");
                return;
            }

            ParadoxPhantomRole.TryPhase(player);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            data.Action is < 1 or > 3 ||
            !ParadoxPhantomRole.IsPhantom(data.PlayerId))
            return;

        ParadoxPhantomRole.ApplySyncedState(
            data.PlayerId,
            data.Action,
            data.DurationSeconds,
            data.CooldownSeconds,
            data.EscapeCount);
    }
}
