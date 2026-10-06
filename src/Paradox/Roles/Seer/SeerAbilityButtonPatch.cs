using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Seer;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class SeerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !SeerRole.IsSeer(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = SeerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            SeerRole.TryReadAura(local, target);
        }
        else
        {
            Rpc<SeerReadAuraRpc>.Instance.Send(
                local,
                new SeerReadAuraRpc.Data(
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
