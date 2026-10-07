using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Forgotten;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class ForgottenAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !ForgottenRole.IsForgotten(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var body = ForgottenHudPatch.CurrentBody;
        if (body == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            ForgottenRole.TryRemember(local, body);
        }
        else
        {
            Rpc<ForgottenMemoryRpc>.Instance.Send(
                local,
                new ForgottenMemoryRpc.Data(
                    local.PlayerId,
                    body.ParentId,
                    0,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
