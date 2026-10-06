using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Detective;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class DetectiveAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !DetectiveRole.IsDetective(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = DetectiveHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            DetectiveRole.TryInvestigate(local, target);
        }
        else
        {
            Rpc<DetectiveScanRpc>.Instance.Send(
                local,
                new DetectiveScanRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0,
                    0),
                immediately: true);
        }

        return false;
    }
}
