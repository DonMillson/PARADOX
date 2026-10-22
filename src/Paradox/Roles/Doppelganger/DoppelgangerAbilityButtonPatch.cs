using HarmonyLib;
using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Doppelganger;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class DoppelgangerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !DoppelgangerRole.IsDoppelganger(local.PlayerId))
            return true;

        if (HudManager.Instance == null || __instance != HudManager.Instance.AbilityButton)
            return true;

        // Suppress duplicate requests and stale-target clicks on the client.
        // This is a UX guard; the host repeats all security-critical validation.
        if (AmongUsClient.Instance == null ||
            local.Data == null || local.Data.IsDead || local.Data.Disconnected ||
            !local.CanMove || MeetingHud.Instance != null ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(local.PlayerId))
            return false;

        var state = DoppelgangerRole.GetOrCreate(local.PlayerId);
        var now = Time.time;
        if (state.IsDisguised(now) || !state.IsReady(now))
            return false;

        var target = DoppelgangerHudPatch.CurrentTarget;
        if (!DoppelgangerTargeting.IsValidTarget(local, target))
            return false;

        if (Vector2.Distance(local.GetTruePosition(), target!.GetTruePosition()) >
            local.MaxReportDistance)
            return false;

        if (AmongUsClient.Instance.AmHost)
        {
            DoppelgangerRole.TryDisguise(local, target);
        }
        else
        {
            Rpc<UseRoleAbilityRpc>.Instance.Send(
                local,
                new UseRoleAbilityRpc.Data(target.PlayerId),
                immediately: true);
        }

        return false;
    }
}
