using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Illusionist;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class IllusionistAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !IllusionistRole.IsIllusionist(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = IllusionistHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            IllusionistRole.TryCast(local, target);
        }
        else
        {
            Rpc<IllusionistMirageRpc>.Instance.Send(
                local,
                new IllusionistMirageRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    byte.MaxValue,
                    0f,
                    0f,
                    2),
                immediately: true);
        }

        return false;
    }
}
