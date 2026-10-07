using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Locksmith;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class LocksmithHudPatch
{
    public static int CurrentDoorIndex { get; private set; } = -1;

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!LocksmithRole.IsLocksmith(local.PlayerId))
        {
            CurrentDoorIndex = -1;
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
            CurrentDoorIndex = -1;
            return;
        }

        LocksmithRole.TryFindNearestClosedDoor(
            local,
            ParadoxRoleSettings.LocksmithUseRange,
            out var doorIndex);

        CurrentDoorIndex = doorIndex;

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Locksmith.ability"));

        var remaining = LocksmithRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.LocksmithCooldownSeconds);

        var canUse =
            local.CanMove &&
            !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            remaining <= 0f &&
            CurrentDoorIndex >= 0;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
