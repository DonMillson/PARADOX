using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Riftmaker;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class RiftmakerAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !RiftmakerRole.IsRiftmaker(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            RiftmakerRole.TryUse(local);
        }
        else
        {
            Rpc<RiftmakerWarpRpc>.Instance.Send(
                local,
                new RiftmakerWarpRpc.Data(
                    local.PlayerId,
                    0,
                    0f,
                    0f,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
