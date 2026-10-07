using Hazel;
using ParadoxCollectorRole = Paradox.Roles.Collector.CollectorRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.CollectorSample)]
public sealed class CollectorSampleRpc : PlayerCustomRpc<ParadoxPlugin, CollectorSampleRpc.Data>
{
    public CollectorSampleRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }
    public readonly record struct Data(byte CollectorPlayerId, byte TargetPlayerId, int SampleCount, float CooldownSeconds, byte Accepted);
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.CollectorPlayerId); writer.Write(data.TargetPlayerId);
        writer.Write(data.SampleCount); writer.Write(data.CooldownSeconds); writer.Write(data.Accepted);
    }
    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadByte(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadByte());
    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null) return;
        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId) return;
            if (data.Accepted != 0 || data.CollectorPlayerId != player.PlayerId || !ParadoxCollectorRole.IsCollector(player.PlayerId))
            { Plugin.Log.LogWarning($"Rejected Collector request from player {player.PlayerId}."); return; }
            var target = FindPlayer(data.TargetPlayerId);
            if (target != null) ParadoxCollectorRole.TryCollect(player, target);
            return;
        }
        if (player.OwnerId != AmongUsClient.Instance.HostId || data.Accepted != 1 || !ParadoxCollectorRole.IsCollector(data.CollectorPlayerId)) return;
        ParadoxCollectorRole.ApplySyncedState(data.CollectorPlayerId, data.TargetPlayerId, data.SampleCount, data.CooldownSeconds);
    }
    private static PlayerControl? FindPlayer(byte id)
    {
        foreach (var p in PlayerControl.AllPlayerControls) if (p != null && p.PlayerId == id) return p;
        return null;
    }
}