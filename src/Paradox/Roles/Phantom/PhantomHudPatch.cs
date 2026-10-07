using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Phantom;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class PhantomHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        PhantomRole.UpdateHost();

        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            __instance.AbilityButton == null)
            return;

        if (!PhantomRole.IsPhantom(local.PlayerId))
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
        var phased = PhantomRole.IsPhased(local.PlayerId);

        if (phased)
        {
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer
                    .Get("role.Phantom.active")
                    .Replace(
                        "{seconds}",
                        Math.Ceiling(
                            PhantomRole.PhaseRemaining(
                                local.PlayerId,
                                now)).ToString()));
        }
        else
        {
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer.Get(
                    "role.Phantom.ability"));
        }

        var remaining = PhantomRole.CooldownRemaining(
            local.PlayerId,
            now);

        __instance.AbilityButton.SetCoolDown(
            phased
                ? PhantomRole.PhaseRemaining(local.PlayerId, now)
                : remaining,
            phased
                ? ParadoxRoleSettings.PhantomPhaseDurationSeconds
                : ParadoxRoleSettings.PhantomCooldownSeconds);

        var canUse =
            local.CanMove &&
            !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            !CorruptorRole.IsCorrupted(local.PlayerId) &&
            !phased &&
            remaining <= 0f;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
