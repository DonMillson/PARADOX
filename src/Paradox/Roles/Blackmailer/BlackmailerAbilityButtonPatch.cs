using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Blackmailer;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class BlackmailerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !BlackmailerRole.IsBlackmailer(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = BlackmailerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            BlackmailerRole.TryBlackmail(local, target);
        }
        else
        {
            Rpc<BlackmailerMarkRpc>.Instance.Send(
                local,
                new BlackmailerMarkRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
