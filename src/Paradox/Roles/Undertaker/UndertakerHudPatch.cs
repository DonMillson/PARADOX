using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Undertaker;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class UndertakerHudPatch
{
    public static DeadBody? CurrentBody { get; private set; }

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        UndertakerRole.UpdateCarriedBodies();

        var local = PlayerControl.LocalPlayer;
        if (local == null || __instance.AbilityButton == null)
            return;

        if (!UndertakerRole.IsUndertaker(local.PlayerId))
        {
            CurrentBody = null;
            return;
        }

        var canShow = local.Data != null &&
                      !local.Data.IsDead &&
                      !local.Data.Disconnected &&
                      MeetingHud.Instance == null;

        __instance.AbilityButton.ToggleVisible(canShow);

        if (!canShow)
        {
            CurrentBody = null;
            return;
        }

        if (UndertakerRole.IsCarrying(local.PlayerId))
        {
            CurrentBody = null;
            __instance.AbilityButton.OverrideText(
                ParadoxPlugin.Localizer.Get("role.Undertaker.drop"));
            __instance.AbilityButton.SetCoolDown(0f, 1f);

            if (local.CanMove)
                __instance.AbilityButton.SetEnabled();
            else
                __instance.AbilityButton.SetDisabled();

            return;
        }

        CurrentBody = UndertakerRole.FindClosestBody(
            local,
            local.MaxReportDistance);

        __instance.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get("role.Undertaker.pickup"));

        var remaining = UndertakerRole.CooldownRemaining(
            local.PlayerId,
            Time.time);

        __instance.AbilityButton.SetCoolDown(
            remaining,
            ParadoxRoleSettings.UndertakerCooldownSeconds);

        var canUse = local.CanMove &&
                     !ParadoxEventRuntime.RoleAbilitiesBlocked &&
                     remaining <= 0f &&
                     CurrentBody != null;

        if (canUse)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }
}


[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class UndertakerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !UndertakerRole.IsUndertaker(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (UndertakerRole.IsCarrying(local.PlayerId))
        {
            if (AmongUsClient.Instance != null &&
                AmongUsClient.Instance.AmHost)
            {
                UndertakerRole.TryDrop(local);
            }
            else
            {
                Reactor.Networking.Rpc.Rpc<Paradox.Networking.UndertakerCarryBodyRpc>.Instance.Send(
                    local,
                    new Paradox.Networking.UndertakerCarryBodyRpc.Data(
                        local.PlayerId,
                        0,
                        2,
                        0f,
                        0),
                    immediately: true);
            }

            return false;
        }

        var targetBody = UndertakerHudPatch.CurrentBody;
        if (targetBody == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            UndertakerRole.TryPickup(local, targetBody);
        }
        else
        {
            Reactor.Networking.Rpc.Rpc<Paradox.Networking.UndertakerCarryBodyRpc>.Instance.Send(
                local,
                new Paradox.Networking.UndertakerCarryBodyRpc.Data(
                    local.PlayerId,
                    targetBody.ParentId,
                    1,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
