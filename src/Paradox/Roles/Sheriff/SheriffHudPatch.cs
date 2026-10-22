using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Sheriff;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class SheriffHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager hud)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || hud.AbilityButton == null)
            return;

        if (!SheriffRole.IsSheriff(local.PlayerId))
        {
            CurrentTarget = null;
            return;
        }

        var visible = local.Data != null && !local.Data.IsDead &&
                      !local.Data.Disconnected && MeetingHud.Instance == null;
        hud.AbilityButton.ToggleVisible(visible);
        if (!visible)
        {
            CurrentTarget = null;
            return;
        }

        CurrentTarget = SheriffRole.ClosestTarget(local);
        var remaining = SheriffRole.Remaining(local.PlayerId, Time.time);
        hud.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Sheriff.ability"));
        hud.AbilityButton.SetCoolDown(remaining, ParadoxRoleSettings.SheriffCooldownSeconds);

        if (local.CanMove && CurrentTarget != null && remaining <= 0f &&
            !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            !CorruptorRole.IsCorrupted(local.PlayerId))
            hud.AbilityButton.SetEnabled();
        else
            hud.AbilityButton.SetDisabled();
    }
}
