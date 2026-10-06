using Paradox.Roles.Observer;
using Paradox.Roles.Doppelganger;
using Paradox.Roles.Parasite;
using Paradox.Roles.Witness;
using Paradox.Roles;
using HarmonyLib;

namespace Paradox.Core;

[HarmonyPatch]
public static class ParadoxGameplayPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    public static void MurderPlayerPostfix(PlayerControl __instance, PlayerControl target, MurderResultFlags resultFlags)
    {
        if (!AmongUsClient.Instance.AmHost || target == null)
            return;

        // Current Among Us reports failed/protected murder attempts through result flags.
        // Only a successful kill is allowed to advance the Paradox Meter.
        if ((resultFlags & MurderResultFlags.Succeeded) == 0)
            return;

        ParadoxGame.AddFrom(ParadoxMeterSource.Kill);
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
    [HarmonyPostfix]
    public static void EndGameStartPostfix()
    {
        ParadoxGame.Reset();
        RoleAssignment.Reset();
        ObserverRole.Reset();
        DoppelgangerRole.Reset();
        ParasiteRole.Reset();
        WitnessRole.Reset();
    }
}
