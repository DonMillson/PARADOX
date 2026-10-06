using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Devourer;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class DevourerHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        DevourerRole.UpdatePendingBodyRemoval();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!DevourerRole.IsDevourer(local.PlayerId))
        {
            CurrentTarget = null;
            return;
        }

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
        {
            CurrentTarget = null;
            return;
        }

        CurrentTarget = DevourerTargeting.FindClosestValidTarget(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Devourer.ability"));

        var remaining = DevourerRole.CooldownRemaining(local.PlayerId, Time.time);
        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.DevourerCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     remaining <= 0f &&
                     CurrentTarget != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
