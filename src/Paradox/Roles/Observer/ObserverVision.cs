using UnityEngine;

namespace Paradox.Roles.Observer;

public static class ObserverVision
{
    public static bool LocalPlayerCanSeeTraces()
    {
        var local = PlayerControl.LocalPlayer;
        return local != null && ObserverRole.IsObserver(local.PlayerId);
    }

    public static IReadOnlyList<ObserverTrace> GetVisibleTraces()
    {
        if (!LocalPlayerCanSeeTraces())
            return Array.Empty<ObserverTrace>();

        ObserverState.RemoveExpired(Time.time);
        return ObserverState.Active;
    }
}
