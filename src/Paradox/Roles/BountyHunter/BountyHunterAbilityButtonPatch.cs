using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.BountyHunter;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class BountyHunterAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !BountyHunterRole.IsBountyHunter(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = BountyHunterHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            BountyHunterRole.TryExecute(local, target);
        }
        else
        {
            Rpc<BountyHunterStateRpc>.Instance.Send(
                local,
                new BountyHunterStateRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    2,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
