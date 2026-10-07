using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Saboteur;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class SaboteurAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !SaboteurRole.IsSaboteur(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (SaboteurRole.IsArmed(local.PlayerId))
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            SaboteurRole.TryArm(local);
        }
        else
        {
            Rpc<SaboteurOverloadRpc>.Instance.Send(
                local,
                new SaboteurOverloadRpc.Data(
                    local.PlayerId,
                    0,
                    0f,
                    0,
                    0),
                immediately: true);
        }

        return false;
    }
}
