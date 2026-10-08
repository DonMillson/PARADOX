using Hazel;
using Paradox.Core;
using Paradox.Settings;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.SyncParadoxGameplaySetting)]
public sealed class SyncParadoxGameplaySettingRpc
    : PlayerCustomRpc<ParadoxPlugin, SyncParadoxGameplaySettingRpc.Data>
{
    public SyncParadoxGameplaySettingRpc(ParadoxPlugin plugin, uint id)
        : base(plugin, id) { }

    public readonly record struct Data(int Source, int Amount);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.Source);
        writer.Write(data.Amount);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt32());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null ||
            player.OwnerId != AmongUsClient.Instance.HostId)
            return;

        if (!Enum.IsDefined(typeof(ParadoxMeterSource), data.Source) ||
            data.Amount < 0 || data.Amount > 20)
        {
            Plugin.Log.LogWarning("Rejected invalid PARADOX gameplay setting.");
            return;
        }

        ParadoxGameplaySettings.SetGain((ParadoxMeterSource)data.Source, data.Amount);
    }
}
