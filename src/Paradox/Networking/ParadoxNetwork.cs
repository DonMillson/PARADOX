using Reactor.Networking.Rpc;
using Reactor.Utilities;

namespace Paradox.Networking;

public static class ParadoxNetwork
{
    public static void SendHandshake(PlayerControl player)
    {
        var local = ParadoxHandshake.Local;
        var rpc = PluginSingleton<ParadoxPlugin>.Instance
            .GetRpc<HandshakeRpc>();

        rpc.Send(player, new HandshakeRpc.Data(
            local.ModName,
            local.ModVersion,
            local.ProtocolVersion),
            immediately: true);
    }
}
