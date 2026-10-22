using Paradox.Core;
using HarmonyLib;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Doppelganger;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class DoppelgangerHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!DoppelgangerRole.IsDoppelganger(local.PlayerId))
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

        var state = DoppelgangerRole.GetOrCreate(local.PlayerId);
        var now = Time.time;
        var disguised = state.IsDisguised(now);
        var coolingDown = !state.IsReady(now);
        var blocked = ParadoxEventRuntime.RoleAbilitiesBlocked ||
                      CorruptorRole.IsCorrupted(local.PlayerId);
        CurrentTarget = disguised
            ? null
            : DoppelgangerTargeting.FindClosestValidTarget(
                local, local.MaxReportDistance);

        var remaining = disguised
            ? state.DisguiseRemaining(now)
            : state.CooldownRemaining(now);
        var maximum = disguised
            ? ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds
            : ParadoxRoleSettings.DoppelgangerCooldownSeconds;

        __instance.AbilityButton.SetCoolDown(remaining, Math.Max(0.1f, maximum));

        // Compact bilingual role-specific labels make the active phase and
        // targeting state legible without a separate screen-space overlay.
        var label = disguised
            ? ParadoxPlugin.Localizer.Get("role.Doppelganger.active")
                .Replace("{seconds}", Math.Ceiling(remaining).ToString())
            : blocked
                ? ParadoxPlugin.Localizer.Get("role.Doppelganger.blocked")
                : !coolingDown && CurrentTarget == null
                    ? ParadoxPlugin.Localizer.Get("role.Doppelganger.noTarget")
                    : ParadoxPlugin.Localizer.Get("role.Doppelganger.ability");

        __instance.AbilityButton.OverrideText(label);

        var canUse = local.CanMove &&
                     !blocked &&
                     !disguised &&
                     !coolingDown &&
                     CurrentTarget != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
