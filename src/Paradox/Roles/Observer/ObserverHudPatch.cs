using HarmonyLib;
using UnityEngine;

namespace Paradox.Roles.Observer;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ObserverHudPatch
{
    private static int _lastVisibleTraceCount;

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        if (!ObserverVision.LocalPlayerCanSeeTraces())
        {
            _lastVisibleTraceCount = 0;
            return;
        }

        var traces = ObserverVision.GetVisibleTraces();
        if (traces.Count <= _lastVisibleTraceCount)
        {
            _lastVisibleTraceCount = traces.Count;
            return;
        }

        var newest = traces[traces.Count - 1];
        __instance.ShowPopUp(
            $"PARADOX — Observer: ability trace detected (player {newest.SourcePlayerId}).");

        _lastVisibleTraceCount = traces.Count;
    }
}
