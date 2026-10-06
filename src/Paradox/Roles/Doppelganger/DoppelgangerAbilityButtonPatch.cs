using HarmonyLib;

namespace Paradox.Roles.Doppelganger;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class DoppelgangerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !DoppelgangerRole.IsDoppelganger(local.PlayerId))
            return true;

        // Only intercept the actual HUD ability button used by the local player.
        if (HudManager.Instance == null || __instance != HudManager.Instance.AbilityButton)
            return true;

        var target = DoppelgangerHudPatch.CurrentTarget;
        if (target == null)
            return false;

        // For the host this executes immediately. Client-to-host activation will be
        // routed through the PARADOX RPC layer in the next networking step.
        if (AmongUsClient.Instance.AmHost)
            DoppelgangerRole.TryDisguise(local, target);

        return false;
    }
}
