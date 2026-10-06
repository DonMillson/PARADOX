using Hazel;
using Paradox.Roles.Parasite;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ParasiteInfection)]
public sealed class ParasiteInfectionRpc : PlayerCustomRpc<ParadoxPlugin, ParasiteInfectionRpc.Data>
{
    public ParasiteInfectionRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float DurationSeconds,
        float CooldownSeconds);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                return;

            if (data.SourcePlayerId != player.PlayerId ||
                !ParasiteRole.IsParasite(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Parasite request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
            {
                Plugin.Log.LogWarning(
                    $"Rejected Parasite request: target {data.TargetPlayerId} not found.");
                return;
            }

            ParasiteRole.TryInfect(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId)
            return;

        ParasiteRole.ApplySyncedInfection(
            data.SourcePlayerId,
            data.TargetPlayerId,
            Time.time,
            data.DurationSeconds,
            data.CooldownSeconds);
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
