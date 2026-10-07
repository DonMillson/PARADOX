using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Puppeteer;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class PuppeteerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !PuppeteerRole.IsPuppeteer(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = PuppeteerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            PuppeteerRole.TryControl(local, target);
        }
        else
        {
            Rpc<PuppeteerControlRpc>.Instance.Send(
                local,
                new PuppeteerControlRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0f,
                    0f,
                    0f,
                    2),
                immediately: true);
        }

        return false;
    }
}
