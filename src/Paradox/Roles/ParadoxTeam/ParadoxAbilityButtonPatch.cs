using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.ParadoxTeam;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class ParadoxAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !ParadoxRole.IsParadox(local.PlayerId) ||
            HudManager.Instance == null || __instance != HudManager.Instance.AbilityButton)
            return true;
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            ParadoxRole.TryRupture(local);
        else
            Rpc<ParadoxRuptureRpc>.Instance.Send(local,
                new ParadoxRuptureRpc.Data(local.PlayerId, 0f, 0), immediately: true);
        return false;
    }
}
