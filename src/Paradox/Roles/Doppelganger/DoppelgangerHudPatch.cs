using HarmonyLib;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Doppelganger;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class DoppelgangerHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!DoppelgangerRole.IsDoppelganger(local.PlayerId))
        {
            CurrentTarget = null;
            return;
        }

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
        {
            CurrentTarget = null;
            return;
        }

        var state = DoppelgangerRole.GetOrCreate(local.PlayerId);
        CurrentTarget = DoppelgangerTargeting.FindClosestValidTarget(
            local,
            local.MaxReportDistance);

        var canUse = local.CanMove &&
                     !state.IsDisguised(Time.time) &&
                     state.IsReady(Time.time) &&
                     CurrentTarget != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
