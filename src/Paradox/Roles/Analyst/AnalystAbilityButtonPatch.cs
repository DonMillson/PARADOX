using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Analyst;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class AnalystAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !AnalystRole.IsAnalyst(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            AnalystRole.TryAnalyze(local);
        }
        else
        {
            Rpc<AnalystScanRpc>.Instance.Send(
                local,
                new AnalystScanRpc.Data(
                    local.PlayerId,
                    0f,
                    0,
                    0,
                    0,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
