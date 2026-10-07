using Hazel;
using ParadoxForgottenRole = Paradox.Roles.Forgotten.ForgottenRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ForgottenMemory)]
public sealed class ForgottenMemoryRpc : PlayerCustomRpc<ParadoxPlugin, ForgottenMemoryRpc.Data>
{
    public ForgottenMemoryRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte ForgottenPlayerId,
        byte BodyPlayerId,
        int MemoryCount,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.ForgottenPlayerId);
        writer.Write(data.BodyPlayerId);
        writer.Write(data.MemoryCount);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadInt32(),
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
                data.ForgottenPlayerId != player.PlayerId ||
                !ParadoxForgottenRole.IsForgotten(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Forgotten request from player {player.PlayerId}.");
                return;
            }

            var body = FindBody(data.BodyPlayerId);
            if (body != null)
                ParadoxForgottenRole.TryRemember(player, body);

            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ParadoxForgottenRole.IsForgotten(
                data.ForgottenPlayerId))
            return;

        ParadoxForgottenRole.ApplySyncedState(
            data.ForgottenPlayerId,
            data.BodyPlayerId,
            data.MemoryCount,
            data.CooldownSeconds);
    }

    private static DeadBody? FindBody(byte bodyPlayerId)
    {
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body != null &&
                body.ParentId == bodyPlayerId &&
                !body.Reported)
                return body;
        }

        return null;
    }
}
