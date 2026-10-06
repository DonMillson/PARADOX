using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Tracker;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class TrackerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !TrackerRole.IsTracker(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (TrackerRole.TrackingRemaining(local.PlayerId, Time.time) > 0f)
            return false;

        var target = TrackerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            TrackerRole.TryTrack(local, target);
        }
        else
        {
            Rpc<TrackerTrackRpc>.Instance.Send(
                local,
                new TrackerTrackRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
