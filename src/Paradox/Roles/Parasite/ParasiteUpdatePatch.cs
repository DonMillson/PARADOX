using HarmonyLib;
using UnityEngine;

namespace Paradox.Roles.Parasite;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class ParasiteUpdatePatch
{
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        if (__instance == null || !AmongUsClient.Instance.AmHost)
            return;

        if (!ParasiteRole.TryTakeCompleted(__instance.PlayerId, Time.time, out var infection))
            return;

        PlayerControl? source = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.PlayerId == infection.SourcePlayerId)
            {
                source = player;
                break;
            }
        }

        if (source == null || source.Data == null || source.Data.IsDead ||
            __instance.Data == null || __instance.Data.IsDead)
            return;

        source.MurderPlayer(__instance, MurderResultFlags.Succeeded);
    }
}
