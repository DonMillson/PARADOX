using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.ShapeshifterX;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class ShapeshifterXAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !ShapeshifterXRole.IsShapeshifterX(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = ShapeshifterXHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            ShapeshifterXRole.TrySwap(local, target);
        }
        else
        {
            Rpc<ShapeshifterXSwapRpc>.Instance.Send(
                local,
                new ShapeshifterXSwapRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0f,
                    2),
                immediately: true);
        }

        return false;
    }
}
