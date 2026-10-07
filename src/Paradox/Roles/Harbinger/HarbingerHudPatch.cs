using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Harbinger;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HarbingerHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        HarbingerRole.UpdateHost();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!HarbingerRole.IsHarbinger(local.PlayerId))
            return;

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
            return;

        var count = HarbingerRole.GetOmenCount(local.PlayerId);
        var required = ParadoxRoleSettings.HarbingerRequiredOmens;

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Harbinger.ability")
                .Replace("{count}", count.ToString())
                .Replace("{required}", required.ToString()));

        var remaining = HarbingerRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.HarbingerCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     !CorruptorRole.IsCorrupted(local.PlayerId) &&
                     remaining <= 0f &&
                     count < required;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
