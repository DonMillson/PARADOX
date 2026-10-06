using Hazel;
using Paradox.Roles.Guardian;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.GuardianProtect)]
public sealed class GuardianProtectRpc : PlayerCustomRpc<ParadoxPlugin, GuardianProtectRpc.Data>
{
    public GuardianProtectRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float DurationSeconds,
        float CooldownSeconds,
        byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.Accepted);
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

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.Accepted != 0 ||
                data.SourcePlayerId != player.PlayerId ||
                !GuardianRole.IsGuardian(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Guardian protection request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
            {
                Plugin.Log.LogWarning(
                    $"Rejected Guardian request: target {data.TargetPlayerId} not found.");
                return;
            }

            GuardianRole.TryProtect(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 ||
            !GuardianRole.IsGuardian(data.SourcePlayerId))
            return;

        GuardianRole.ApplySyncedProtection(
            data.SourcePlayerId,
            data.TargetPlayerId,
            data.DurationSeconds,
            data.CooldownSeconds);

        GuardianRole.ShowSyncedActivationFeedback(
            data.SourcePlayerId,
            data.TargetPlayerId);
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null && candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }
}
