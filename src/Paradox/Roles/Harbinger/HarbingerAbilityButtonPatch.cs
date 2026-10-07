using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Harbinger;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class HarbingerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !HarbingerRole.IsHarbinger(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            HarbingerRole.TryInvokeOmen(local);
        }
        else
        {
            Rpc<HarbingerOmenRpc>.Instance.Send(
                local,
                new HarbingerOmenRpc.Data(
                    local.PlayerId,
                    0,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
