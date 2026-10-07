using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Forgotten;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ForgottenHudPatch
{
    public static DeadBody? CurrentBody { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            __instance.AbilityButton == null)
            return;

        if (!ForgottenRole.IsForgotten(local.PlayerId))
        {
            CurrentBody = null;
            return;
        }

        var canShow =
            local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected &&
            MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);

        if (!canShow)
        {
            CurrentBody = null;
            return;
        }

        CurrentBody = ForgottenRole.FindClosestBody(
            local,
            local.MaxReportDistance + 0.15f);

        var count = ForgottenRole.GetMemoryCount(local.PlayerId);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer
                .Get("role.Forgotten.ability")
                .Replace("{count}", count.ToString())
                .Replace(
                    "{required}",
                    ParadoxRoleSettings.ForgottenRequiredMemories.ToString()));

        var remaining = ForgottenRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.ForgottenCooldownSeconds);

        var canUse =
            local.CanMove &&
            !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            !CorruptorRole.IsCorrupted(local.PlayerId) &&
            remaining <= 0f &&
            CurrentBody != null &&
            count < ParadoxRoleSettings.ForgottenRequiredMemories;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
