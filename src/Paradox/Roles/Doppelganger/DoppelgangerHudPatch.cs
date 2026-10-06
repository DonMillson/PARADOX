using Paradox.Core;
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

        var now = Time.time;
        var isDisguised = state.IsDisguised(now);
        var cooldownRemaining = state.CooldownRemaining(now);
        var disguiseRemaining = state.DisguiseRemaining(now);
        var displayedRemaining = isDisguised ? disguiseRemaining : cooldownRemaining;
        var displayedMaximum = isDisguised
            ? ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds
            : ParadoxRoleSettings.DoppelgangerCooldownSeconds;

        __instance.AbilityButton.SetCoolDown(displayedRemaining, displayedMaximum);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     !isDisguised &&
                     state.IsReady(now) &&
                     CurrentTarget != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
