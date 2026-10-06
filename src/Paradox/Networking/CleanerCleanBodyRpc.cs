using Hazel;
using Paradox.Roles.Cleaner;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.CleanerCleanBody)]
public sealed class CleanerCleanBodyRpc : PlayerCustomRpc<ParadoxPlugin, CleanerCleanBodyRpc.Data>
{
    public CleanerCleanBodyRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte CleanerPlayerId,
        byte BodyPlayerId,
        float CooldownSeconds);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.CleanerPlayerId);
        writer.Write(data.BodyPlayerId);
        writer.Write(data.CooldownSeconds);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        // Host receives a request from a remote Cleaner, validates it against
        // authoritative role/cooldown/distance state and then broadcasts the
        // accepted result as a host-authored RPC.
        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.CleanerPlayerId != player.PlayerId ||
                !CleanerRole.IsCleaner(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Cleaner request from player {player.PlayerId}.");
                return;
            }

            var body = FindBody(data.BodyPlayerId);
            if (body == null)
            {
                Plugin.Log.LogWarning(
                    $"Rejected Cleaner request: body {data.BodyPlayerId} not found.");
                return;
            }

            CleanerRole.TryClean(player, body);
            return;
        }

        // Non-host clients only trust the host-authored accepted result.
        if (player.OwnerId != AmongUsClient.Instance.HostId)
            return;

        CleanerRole.ApplySyncedClean(
            data.CleanerPlayerId,
            data.BodyPlayerId,
            data.CooldownSeconds);
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
