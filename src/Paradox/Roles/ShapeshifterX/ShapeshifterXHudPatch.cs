using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.ShapeshifterX;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ShapeshifterXHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        ShapeshifterXRole.UpdateHost();

        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            __instance.AbilityButton == null)
            return;

        if (!ShapeshifterXRole.IsShapeshifterX(local.PlayerId))
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
        var active = ShapeshifterXRole.IsActive(
            local.PlayerId,
            now);

        CurrentTarget = active
            ? null
            : ShapeshifterXRole.FindClosestTarget(
                local,
                local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get(
                "role.ShapeshifterX.ability"));

        var remaining = ShapeshifterXRole.CooldownRemaining(
            local.PlayerId,
            now);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.ShapeshifterXCooldownSeconds);

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
