using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Saboteur;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class SaboteurHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!SaboteurRole.IsSaboteur(local.PlayerId))
            return;

        var canShow =
            local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected &&
            MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);

        if (!canShow)
            return;

        if (SaboteurRole.IsArmed(local.PlayerId))
        {
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer.Get(
                    "role.Saboteur.armed"));

            __instance.AbilityButton.SetCoolDown(0f, 1f);
            __instance.AbilityButton.SetDisabled();
            return;
        }

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get(
                "role.Saboteur.ability"));

        var remaining = SaboteurRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.SaboteurCooldownSeconds);

        var canUse =
            local.CanMove &&
            !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            !CorruptorRole.IsCorrupted(local.PlayerId) &&
            remaining <= 0f;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
