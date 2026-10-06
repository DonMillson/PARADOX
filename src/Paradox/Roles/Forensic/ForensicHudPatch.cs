using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Forensic;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ForensicHudPatch
{
    public static DeadBody? CurrentBody { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!ForensicRole.IsForensic(local.PlayerId))
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

        CurrentBody = ForensicRole.FindClosestBody(local, local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Forensic.ability"));

        var remaining = ForensicRole.CooldownRemaining(local.PlayerId, Time.time);
        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.ForensicCooldownSeconds);

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
