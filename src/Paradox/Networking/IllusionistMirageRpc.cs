using Hazel;
using ParadoxIllusionistRole = Paradox.Roles.Illusionist.IllusionistRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.IllusionistMirage)]
public sealed class IllusionistMirageRpc : PlayerCustomRpc<ParadoxPlugin, IllusionistMirageRpc.Data>
{
    public IllusionistMirageRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte VictimPlayerId,
        byte DisguisePlayerId,
        float DurationSeconds,
        float CooldownSeconds,
        byte State);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.VictimPlayerId);
        writer.Write(data.DisguisePlayerId);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.State);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
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

            if (data.State != 2 ||
                data.SourcePlayerId != player.PlayerId ||
                !ParadoxIllusionistRole.IsIllusionist(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Illusionist request from player {player.PlayerId}.");
                return;
            }

            var victim = FindPlayer(data.VictimPlayerId);
            if (victim == null)
                return;

            ParadoxIllusionistRole.TryCast(player, victim);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.State > 1 ||
            !ParadoxIllusionistRole.IsIllusionist(data.SourcePlayerId))
            return;

        ParadoxIllusionistRole.ApplySyncedState(
            data.SourcePlayerId,
            data.VictimPlayerId,
            data.DisguisePlayerId,
            data.DurationSeconds,
            data.CooldownSeconds,
            data.State == 1);
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
