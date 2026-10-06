using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Riftmaker;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class RiftmakerHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!RiftmakerRole.IsRiftmaker(local.PlayerId))
            return;

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
            return;

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get(
                RiftmakerRole.HasAnchor(local.PlayerId)
                    ? "role.Riftmaker.rift"
                    : "role.Riftmaker.anchor"));

        var remaining = RiftmakerRole.CooldownRemaining(local.PlayerId, Time.time);
        var maximum = RiftmakerRole.HasAnchor(local.PlayerId)
            ? ParadoxRoleSettings.RiftmakerAnchorDelaySeconds
            : ParadoxRoleSettings.RiftmakerCooldownSeconds;

        __instance.AbilityButton.SetCoolDown(remaining, maximum);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     remaining <= 0f;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
