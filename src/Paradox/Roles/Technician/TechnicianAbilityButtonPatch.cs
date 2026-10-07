using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Technician;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class TechnicianAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !TechnicianRole.IsTechnician(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            TechnicianRole.TryActivateShield(local);
        }
        else
        {
            Rpc<TechnicianShieldRpc>.Instance.Send(
                local,
                new TechnicianShieldRpc.Data(
                    local.PlayerId,
                    0f,
                    0f,
                    0,
                    0),
                immediately: true);
        }

        return false;
    }
}
