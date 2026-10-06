using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Corruptor;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class CorruptorAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !CorruptorRole.IsCorruptor(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = CorruptorHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            CorruptorRole.TryCorrupt(local, target);
        }
        else
        {
            Rpc<CorruptorCorruptRpc>.Instance.Send(
                local,
                new CorruptorCorruptRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
