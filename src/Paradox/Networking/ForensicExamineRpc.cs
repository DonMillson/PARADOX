using Hazel;
using Paradox.Roles.Forensic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ForensicExamine)]
public sealed class ForensicExamineRpc : PlayerCustomRpc<ParadoxPlugin, ForensicExamineRpc.Data>
{
    public ForensicExamineRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte BodyPlayerId,
        float CooldownSeconds,
        byte Known,
        float AgeSeconds,
        byte Residue,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.BodyPlayerId);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Known);
        writer.Write(data.AgeSeconds);
        writer.Write(data.Residue);
        writer.Write(data.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
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
                data.SourcePlayerId != player.PlayerId ||
                !ForensicRole.IsForensic(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Forensic request from player {player.PlayerId}.");
                return;
            }

            var body = FindBody(data.BodyPlayerId);
            if (body == null)
                return;

            ForensicRole.TryExamine(player, body);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !ForensicRole.IsForensic(data.SourcePlayerId))
            return;

        ForensicRole.ApplySyncedResult(
            data.SourcePlayerId,
            data.CooldownSeconds,
            data.Known == 1,
            data.AgeSeconds,
            data.Residue == 1);
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
