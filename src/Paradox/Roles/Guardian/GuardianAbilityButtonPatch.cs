using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Guardian;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class GuardianAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !GuardianRole.IsGuardian(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = GuardianHudPatch.CurrentTarget;
        if (target == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            GuardianRole.TryProtect(local, target);
        }
        else
        {
            Rpc<GuardianProtectRpc>.Instance.Send(
                local,
                new GuardianProtectRpc.Data(
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
