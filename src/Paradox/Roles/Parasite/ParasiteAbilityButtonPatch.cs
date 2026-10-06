using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Parasite;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class ParasiteAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !ParasiteRole.IsParasite(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = ParasiteHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            ParasiteRole.TryInfect(local, target);
        }
        else
        {
            Rpc<ParasiteInfectionRpc>.Instance.Send(
                local,
                new ParasiteInfectionRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0f),
                immediately: true);
        }

        return false;
    }
}
