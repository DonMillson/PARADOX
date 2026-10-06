using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Anomaly;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class AnomalyAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !AnomalyRole.IsAnomaly(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            AnomalyRole.TryPulse(local);
        }
        else
        {
            Rpc<AnomalyPulseRpc>.Instance.Send(
                local,
                new AnomalyPulseRpc.Data(
                    local.PlayerId,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
