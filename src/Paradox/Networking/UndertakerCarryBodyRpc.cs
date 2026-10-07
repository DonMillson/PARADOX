using Hazel;
using ParadoxUndertakerRole = Paradox.Roles.Undertaker.UndertakerRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.UndertakerCarryBody)]
public sealed class UndertakerCarryBodyRpc :
    PlayerCustomRpc<ParadoxPlugin, UndertakerCarryBodyRpc.Data>
{
    public UndertakerCarryBodyRpc(ParadoxPlugin plugin, uint id) :
        base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte BodyPlayerId,
        byte Action,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.BodyPlayerId);
        writer.Write(data.Action);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
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
                !ParadoxUndertakerRole.IsUndertaker(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Undertaker request from player {player.PlayerId}.");
                return;
            }

            if (data.Action == 2)
            {
                ParadoxUndertakerRole.TryDrop(player);
                return;
            }

            if (data.Action != 1)
                return;

            var body = FindBody(data.BodyPlayerId);
            if (body != null)
                ParadoxUndertakerRole.TryPickup(player, body);

            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxUndertakerRole.IsUndertaker(data.SourcePlayerId))
            return;

        if (data.Action == 1)
        {
            ParadoxUndertakerRole.ApplySyncedPickup(
                data.SourcePlayerId,
                data.BodyPlayerId);
        }
        else if (data.Action == 2)
        {
            ParadoxUndertakerRole.ApplySyncedDrop(
                data.SourcePlayerId,
                data.BodyPlayerId,
                data.CooldownSeconds);
        }
    }

    private static DeadBody? FindBody(byte bodyPlayerId)
    {
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body != null && body.ParentId == bodyPlayerId)
                return body;
        }

        return null;
    }
}
