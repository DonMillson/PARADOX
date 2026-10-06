using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Doppelganger;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class DoppelgangerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !DoppelgangerRole.IsDoppelganger(local.PlayerId))
            return true;

        if (HudManager.Instance == null || __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = DoppelgangerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance.AmHost)
        {
            DoppelgangerRole.TryDisguise(local, target);
        }
        else
        {
            Rpc<UseRoleAbilityRpc>.Instance.Send(
                local,
                new UseRoleAbilityRpc.Data(target.PlayerId),
                immediately: true);
        }

        return false;
    }
}
