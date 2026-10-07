using HarmonyLib;

namespace Paradox.Roles.Revenant;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class RevenantHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix() =>
        RevenantRole.UpdateHost();
}
