using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Puppeteer;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class PuppeteerHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        PuppeteerRole.UpdateHost();
        PuppeteerRole.UpdateLocalControl();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!PuppeteerRole.IsPuppeteer(local.PlayerId))
        {
            CurrentTarget = null;
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
            CurrentTarget = null;
            return;
        }

        var now = Time.time;
        var active = PuppeteerRole.HasActiveControl(
            local.PlayerId,
            now);

        CurrentTarget = active
            ? null
            : PuppeteerRole.FindClosestTarget(
                local,
                local.MaxReportDistance);

        if (active)
        {
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer
                    .Get("role.Puppeteer.active")
                    .Replace(
                        "{seconds}",
                        Math.Ceiling(
                            Math.Max(
                                0f,
                                ParadoxRoleSettings.PuppeteerDurationSeconds))
                            .ToString()));
        }
        else
        {
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer.Get(
                    "role.Puppeteer.ability"));
        }

        var remaining = PuppeteerRole.CooldownRemaining(
            local.PlayerId,
            now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.PuppeteerCooldownSeconds);

        var canUse =
            local.CanMove &&
            !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            !CorruptorRole.IsCorrupted(local.PlayerId) &&
            !active &&
            remaining <= 0f &&
            CurrentTarget != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
