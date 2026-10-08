using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using Paradox.Roles.Corruptor;
using UnityEngine;

namespace Paradox.Roles.Anomaly;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class AnomalyHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        AnomalyRole.UpdateHost();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!AnomalyRole.IsAnomaly(local.PlayerId))
            return;

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);
        if (!canShow)
            return;

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Anomaly.ability"));

        var now = Time.time;
        var remaining = AnomalyRole.CooldownRemaining(local.PlayerId, now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.AnomalyCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     !CorruptorRole.IsCorrupted(local.PlayerId) &&
                     ParadoxGame.State.Meter < 100f &&
                     remaining <= 0f;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
