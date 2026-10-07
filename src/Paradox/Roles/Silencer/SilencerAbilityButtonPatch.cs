using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Silencer;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class SilencerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !SilencerRole.IsSilencer(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = SilencerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            SilencerRole.TrySilence(local, target);
        }
        else
        {
            Rpc<SilencerMarkRpc>.Instance.Send(
                local,
                new SilencerMarkRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
