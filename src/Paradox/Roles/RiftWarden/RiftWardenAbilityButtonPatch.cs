using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.RiftWarden;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class RiftWardenAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton button)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !RiftWardenRole.IsRiftWarden(local.PlayerId) ||
            HudManager.Instance == null || button != HudManager.Instance.AbilityButton)
            return true;

        if (AmongUsClient.Instance == null || !local.CanMove ||
            !RiftWardenRole.CanBurst(local.PlayerId))
            return false;

        if (AmongUsClient.Instance.AmHost)
            RiftWardenRole.TryBurst(local);
        else
            Rpc<RiftWardenBurstRpc>.Instance.Send(local,
                new RiftWardenBurstRpc.Data(local.PlayerId, 0), immediately: true);
        return false;
    }
}
