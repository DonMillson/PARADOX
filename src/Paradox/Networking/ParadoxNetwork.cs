using Reactor.Networking.Rpc;

namespace Paradox.Networking;

public static class ParadoxNetwork
{
    public static void SendHandshake(PlayerControl player)
    {
        var local = ParadoxHandshake.Local;

        Rpc<HandshakeRpc>.Instance.Send(
            player,
            new HandshakeRpc.Data(
                local.ModName,
                local.ModVersion,
                local.ProtocolVersion),
            immediately: true);
    }
}
