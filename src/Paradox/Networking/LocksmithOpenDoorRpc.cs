using Hazel;
using ParadoxLocksmithRole = Paradox.Roles.Locksmith.LocksmithRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.LocksmithOpenDoor)]
public sealed class LocksmithOpenDoorRpc :
    PlayerCustomRpc<ParadoxPlugin, LocksmithOpenDoorRpc.Data>
{
    public LocksmithOpenDoorRpc(ParadoxPlugin plugin, uint id) :
        base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        int DoorIndex,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling =>
        RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.DoorIndex);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadInt32(),
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
                !ParadoxLocksmithRole.IsLocksmith(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Locksmith request from player {player.PlayerId}.");
                return;
            }

            ParadoxLocksmithRole.TryOpenDoor(
                player,
                data.DoorIndex);

            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxLocksmithRole.IsLocksmith(data.SourcePlayerId))
            return;

        ParadoxLocksmithRole.ApplySyncedOpen(
            data.SourcePlayerId,
            data.DoorIndex,
            data.CooldownSeconds);
    }
}
