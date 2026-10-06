using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Chronologist;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class ChronologistAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !ChronologistRole.IsChronologist(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            ChronologistRole.TryReadTimeline(local);
        }
        else
        {
            Rpc<ChronologistReadTimelineRpc>.Instance.Send(
                local,
                new ChronologistReadTimelineRpc.Data(
                    local.PlayerId,
                    0f,
                    0,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
