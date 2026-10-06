using Paradox.Roles.Observer;
using Paradox.Roles.Doppelganger;
using Paradox.Roles.Parasite;
using Paradox.Roles.Cleaner;
using Paradox.Roles.Witness;
using Paradox.Roles.Guardian;
using Paradox.Roles.Medic;
using Paradox.Roles.Tracker;
using Paradox.Roles.Stabilizer;
using Paradox.Roles.Anomaly;
using Paradox.Roles;
using HarmonyLib;

namespace Paradox.Core;

[HarmonyPatch]
public static class ParadoxGameplayPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPrefix]
    public static bool MurderPlayerPrefix(PlayerControl target, MurderResultFlags resultFlags)
    {
        if ((resultFlags & MurderResultFlags.Succeeded) == 0)
            return true;

        return !GuardianRole.TryBlockMurder(target);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    public static void MurderPlayerPostfix(PlayerControl __instance, PlayerControl target, MurderResultFlags resultFlags)
    {
        if (!AmongUsClient.Instance.AmHost || target == null)
            return;

        if ((resultFlags & MurderResultFlags.Succeeded) == 0)
            return;

        if (target.Data == null || !target.Data.IsDead)
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
        CleanerRole.Reset();
        WitnessRole.Reset();
        GuardianRole.ResetRuntime();
        MedicRole.ResetRuntime();
        TrackerRole.ResetRuntime();
        StabilizerRole.ResetRuntime();
        AnomalyRole.ResetRuntime();
    }
}
