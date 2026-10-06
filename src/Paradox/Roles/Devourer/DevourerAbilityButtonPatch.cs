using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Devourer;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class DevourerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !DevourerRole.IsDevourer(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = DevourerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            DevourerRole.TryDevour(local, target);
        }
        else
        {
            Rpc<DevourerConsumeRpc>.Instance.Send(
                local,
                new DevourerConsumeRpc.Data(
                    local.PlayerId,
                    target.PlayerId,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
