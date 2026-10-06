using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Medic;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class MedicAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !MedicRole.IsMedic(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = MedicHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            MedicRole.TryScan(local, target);
        }
        else
        {
            Rpc<MedicScanRpc>.Instance.Send(
                local,
                new MedicScanRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0,
                    0),
                immediately: true);
        }

        return false;
    }
}
