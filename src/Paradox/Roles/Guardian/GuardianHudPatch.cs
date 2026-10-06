using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Guardian;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class GuardianHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!GuardianRole.IsGuardian(local.PlayerId))
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

        CurrentTarget = GuardianTargeting.FindClosestValidTarget(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Guardian.ability"));

        var now = Time.time;
        var remaining = GuardianRole.CooldownRemaining(local.PlayerId, now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.GuardianCooldownSeconds);

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
