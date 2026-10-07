using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Technician;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class TechnicianHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!TechnicianRole.IsTechnician(local.PlayerId))
            return;

        var canShow =
            local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected &&
            MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);

        if (!canShow)
            return;

        var now = Time.time;
        var shieldRemaining =
            TechnicianRole.ShieldRemaining(now);

        if (shieldRemaining > 0f)
        {
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer
                    .Get("role.Technician.active")
                    .Replace(
                        "{seconds}",
                        Math.Ceiling(shieldRemaining).ToString()));

            __instance.AbilityButton.SetCoolDown(
                shieldRemaining,
                ParadoxRoleSettings.TechnicianShieldDurationSeconds);

            __instance.AbilityButton.SetDisabled();
            return;
        }

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get(
                "role.Technician.ability"));

        var remaining = TechnicianRole.CooldownRemaining(
            local.PlayerId,
            now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.TechnicianCooldownSeconds);

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
