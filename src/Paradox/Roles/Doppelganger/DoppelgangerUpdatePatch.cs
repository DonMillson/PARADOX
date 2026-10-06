using HarmonyLib;

namespace Paradox.Roles.Doppelganger;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class DoppelgangerUpdatePatch
{
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        if (__instance == null || !DoppelgangerRole.IsDoppelganger(__instance.PlayerId))
            return;

        DoppelgangerRole.Update(__instance);
    }
}
