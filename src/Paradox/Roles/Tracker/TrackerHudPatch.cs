using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Tracker;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class TrackerHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!TrackerRole.IsTracker(local.PlayerId))
        {
            CurrentTarget = null;
            return;
        }

        var canShow = local.Data != null &&
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
        var trackingRemaining = TrackerRole.TrackingRemaining(local.PlayerId, now);

        if (trackingRemaining > 0f &&
            TrackerRole.TryGetActiveTarget(local.PlayerId, out var tracked) &&
            tracked != null)
        {
            CurrentTarget = tracked;
            var distance = Vector2.Distance(local.GetTruePosition(), tracked.GetTruePosition());
            var activeText = ParadoxPlugin.Localizer.Get("role.Tracker.active")
                .Replace("{distance}", $"{distance:0.0}m");

            __instance.AbilityButton.OverrideText(activeText);
            __instance.AbilityButton.SetCoolDown(
                trackingRemaining,
                ParadoxRoleSettings.TrackerDurationSeconds);
            __instance.AbilityButton.SetDisabled();
            return;
        }

        CurrentTarget = TrackerTargeting.FindClosestValidTarget(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Tracker.ability"));

        var remaining = TrackerRole.CooldownRemaining(local.PlayerId, now);
        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.TrackerCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     remaining <= 0f &&
                     CurrentTarget != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
