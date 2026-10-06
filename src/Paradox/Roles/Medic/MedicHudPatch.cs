using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Medic;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class MedicHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!MedicRole.IsMedic(local.PlayerId))
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

        CurrentTarget = MedicTargeting.FindClosestValidTarget(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Medic.ability"));

        var now = Time.time;
        var remaining = MedicRole.CooldownRemaining(local.PlayerId, now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.MedicCooldownSeconds);

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
