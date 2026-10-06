using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Stabilizer;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class StabilizerHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!StabilizerRole.IsStabilizer(local.PlayerId))
            return;

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
            return;

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Stabilizer.ability"));

        var now = Time.time;
        var remaining = StabilizerRole.CooldownRemaining(local.PlayerId, now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.StabilizerCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     ParadoxGame.State.Meter > 0f &&
                     remaining <= 0f;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
