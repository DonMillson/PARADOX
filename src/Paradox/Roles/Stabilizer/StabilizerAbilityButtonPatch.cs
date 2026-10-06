using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Stabilizer;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class StabilizerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !StabilizerRole.IsStabilizer(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            StabilizerRole.TryStabilize(local);
        }
        else
        {
            Rpc<StabilizerPulseRpc>.Instance.Send(
                local,
                new StabilizerPulseRpc.Data(
                    local.PlayerId,
                    0f,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
