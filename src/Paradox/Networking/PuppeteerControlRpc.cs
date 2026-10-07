using Hazel;
using ParadoxPuppeteerRole = Paradox.Roles.Puppeteer.PuppeteerRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.PuppeteerControl)]
public sealed class PuppeteerControlRpc : PlayerCustomRpc<ParadoxPlugin, PuppeteerControlRpc.Data>
{
    public PuppeteerControlRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(
        byte SourcePlayerId,
        byte TargetPlayerId,
        float OffsetX,
        float OffsetY,
        float DurationSeconds,
        float CooldownSeconds,
        byte State);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.OffsetX);
        writer.Write(data.OffsetY);
        writer.Write(data.DurationSeconds);
        writer.Write(data.CooldownSeconds);
        writer.Write(data.State);
    }

    public override Data Read(MessageReader reader) =>
        new(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
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
                !ParadoxPuppeteerRole.IsPuppeteer(player.PlayerId))
            {
                Plugin.Log.LogWarning(
                    $"Rejected Puppeteer request from player {player.PlayerId}.");
                return;
            }

            var target = FindPlayer(data.TargetPlayerId);
            if (target == null)
                return;

            ParadoxPuppeteerRole.TryControl(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.State > 1 ||
            !ParadoxPuppeteerRole.IsPuppeteer(data.SourcePlayerId))
            return;

        ParadoxPuppeteerRole.ApplySyncedControl(
            data.SourcePlayerId,
            data.TargetPlayerId,
            data.OffsetX,
            data.OffsetY,
            data.DurationSeconds,
            data.CooldownSeconds,
            data.State == 1);
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null &&
                candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }
}
