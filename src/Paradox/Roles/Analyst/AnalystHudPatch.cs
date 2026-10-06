using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Analyst;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class AnalystHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!AnalystRole.IsAnalyst(local.PlayerId))
            return;

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
            return;

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Analyst.ability"));

        var remaining = AnalystRole.CooldownRemaining(local.PlayerId, Time.time);
        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.AnalystCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     remaining <= 0f;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
