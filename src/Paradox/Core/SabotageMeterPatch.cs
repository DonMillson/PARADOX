using HarmonyLib;
using Paradox.Roles.Technician;

namespace Paradox.Core;

[HarmonyPatch(
    typeof(ShipStatus),
    nameof(ShipStatus.UpdateSystem),
    typeof(SystemTypes),
    typeof(PlayerControl),
    typeof(byte))]
public static class SabotageMeterPatch
{
    [HarmonyPostfix]
    public static void UpdateSystemPostfix(
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        if (systemType != SystemTypes.Sabotage)
            return;

        TechnicianRole.HandleSabotageMeter(
            player,
            amount);
    }
}
