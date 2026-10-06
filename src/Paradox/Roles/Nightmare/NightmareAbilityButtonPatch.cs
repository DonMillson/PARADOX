using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Nightmare;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class NightmareAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !NightmareRole.IsNightmare(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = NightmareHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            NightmareRole.TryHaunt(local, target);
        }
        else
        {
            Rpc<NightmareHauntRpc>.Instance.Send(
                local,
                new NightmareHauntRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
