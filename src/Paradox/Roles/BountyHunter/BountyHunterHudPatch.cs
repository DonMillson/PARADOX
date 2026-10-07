using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.BountyHunter;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class BountyHunterHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        BountyHunterRole.UpdateHost();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!BountyHunterRole.IsBountyHunter(local.PlayerId))
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

        BountyHunterRole.TryGetTarget(local.PlayerId, out var target);
        CurrentTarget = target;

        var targetName = target?.Data?.PlayerName ?? "?";
        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.BountyHunter.target")
                .Replace("{player}", targetName));

        var remaining = BountyHunterRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.BountyHunterCooldownSeconds);

        var inRange = target != null &&
                      Vector2.Distance(
                          local.GetTruePosition(),
                          target.GetTruePosition()) <= local.MaxReportDistance;

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     !CorruptorRole.IsCorrupted(local.PlayerId) &&
                     remaining <= 0f &&
                     target != null &&
                     inRange;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}
