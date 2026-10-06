using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.HandshakeHello)]
public sealed class HandshakeRpc : PlayerCustomRpc<ParadoxPlugin, HandshakeRpc.Data>
{
    public HandshakeRpc(ParadoxPlugin plugin, uint id) : base(plugin, id)
    {
    }

    public readonly record struct Data(string ModName, string ModVersion, int ProtocolVersion);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.ModName);
        writer.Write(data.ModVersion);
        writer.Write(data.ProtocolVersion);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadString(), reader.ReadString(), reader.ReadInt32());

    public override void Handle(PlayerControl player, Data data)
    {
        var handshake = new ParadoxHandshake(data.ModName, data.ModVersion, data.ProtocolVersion);
        ParadoxPlugin.Clients.Register(player.OwnerId, handshake);

        var compatibility = ParadoxPlugin.Clients.GetCompatibility(player.OwnerId);
        Plugin.Log.LogInfo(
            $"PARADOX handshake from client {player.OwnerId}: {data.ModVersion}, protocol {data.ProtocolVersion}, {compatibility}");

        // A newly joined modded client has just announced itself. If we are the
        // host, rebroadcast the current role pool/chances so every PARADOX client
        // renders the same lobby summary.
        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            player.OwnerId != AmongUsClient.Instance.HostId)
        {
            ParadoxNetwork.BroadcastRoleSettings();
        }
    }
}
