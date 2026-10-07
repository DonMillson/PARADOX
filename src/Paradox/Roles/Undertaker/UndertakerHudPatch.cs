using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Undertaker;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class UndertakerHudPatch
{
    public static DeadBody? CurrentBody { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        UndertakerRole.UpdateCarriedBodies();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!UndertakerRole.IsUndertaker(local.PlayerId))
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

        if (UndertakerRole.IsCarrying(local.PlayerId))
        {
            CurrentBody = null;
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer.Get("role.Undertaker.drop"));
            __instance.AbilityButton.SetCoolDown(0f, 1f);

            if (local.CanMove)
                __instance.AbilityButton.SetEnabled();
            else
                __instance.AbilityButton.SetDisabled();

            return;
        }

        CurrentBody = UndertakerRole.FindClosestBody(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Undertaker.pickup"));

        var remaining = UndertakerRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.UndertakerCooldownSeconds);

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
