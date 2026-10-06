using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Cleaner;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class CleanerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !CleanerRole.IsCleaner(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var body = CleanerHudPatch.CurrentBody;
        if (body == null)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            CleanerRole.TryClean(local, body);
        }
        else
        {
            Rpc<CleanerCleanBodyRpc>.Instance.Send(
                local,
                new CleanerCleanBodyRpc.Data(
                    local.PlayerId,
                    body.ParentId,
                    0f),
                immediately: true);
        }

        return false;
    }
}
