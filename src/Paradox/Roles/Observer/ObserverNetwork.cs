using Paradox.Networking;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Observer;

public static class ObserverNetwork
{
    public static void BroadcastTrace(PlayerControl source)
    {
        if (source == null || !AmongUsClient.Instance.AmHost)
            return;

        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        var createdAt = Time.time;
        ObserverRole.RecordAbilityTrace(source.PlayerId, createdAt);

        Rpc<ObserverTraceRpc>.Instance.Send(
            sender,
            new ObserverTraceRpc.Data(source.PlayerId, createdAt),
            immediately: true);
    }
}
