using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Phantom;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class PhantomAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !PhantomRole.IsPhantom(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            PhantomRole.TryPhase(local);
        }
        else
        {
            Rpc<PhantomPhaseRpc>.Instance.Send(
                local,
                new PhantomPhaseRpc.Data(
                    local.PlayerId,
                    0,
                    0f,
                    0f,
                    0,
                    0),
                immediately: true);
        }

        return false;
    }
}
