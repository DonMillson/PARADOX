using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Sheriff;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class SheriffAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton button)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !SheriffRole.IsSheriff(local.PlayerId) ||
            HudManager.Instance == null || button != HudManager.Instance.AbilityButton)
            return true;

        var target = SheriffHudPatch.CurrentTarget;
        if (target == null || AmongUsClient.Instance == null ||
            local.Data == null || local.Data.IsDead || !local.CanMove ||
            SheriffRole.Remaining(local.PlayerId, UnityEngine.Time.time) > 0f)
            return false;

        if (AmongUsClient.Instance.AmHost)
            SheriffRole.TryShoot(local, target);
        else
            Rpc<SheriffShotRpc>.Instance.Send(local,
                new SheriffShotRpc.Data(local.PlayerId, target.PlayerId, 0f, 0, 0),
                immediately: true);

        return false;
    }
}
