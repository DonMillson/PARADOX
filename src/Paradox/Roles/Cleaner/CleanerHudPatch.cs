using Paradox.Core;
using HarmonyLib;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Cleaner;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class CleanerHudPatch
{
    public static DeadBody? CurrentBody { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!CleanerRole.IsCleaner(local.PlayerId))
        {
            CurrentBody = null;
            return;
        }

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
        {
            CurrentBody = null;
            return;
        }

        CurrentBody = CleanerRole.FindClosestBody(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Cleaner.ability"));

        var now = Time.time;
        var remaining = CleanerRole.CooldownRemaining(local.PlayerId, now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.CleanerCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     remaining <= 0f &&
                     CurrentBody != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
