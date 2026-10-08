using Paradox.Roles.Observer;
using Paradox.Roles.Doppelganger;
using Paradox.Roles.Parasite;
using Paradox.Roles.Cleaner;
using ParadoxDevourerRole = Paradox.Roles.Devourer.DevourerRole;
using ParadoxCorruptorRole = Paradox.Roles.Corruptor.CorruptorRole;
using ParadoxNightmareRole = Paradox.Roles.Nightmare.NightmareRole;
using ParadoxRiftmakerRole = Paradox.Roles.Riftmaker.RiftmakerRole;
using ParadoxSaboteurRole = Paradox.Roles.Saboteur.SaboteurRole;
using ParadoxUndertakerRole = Paradox.Roles.Undertaker.UndertakerRole;
using ParadoxTimebreakerRole = Paradox.Roles.Timebreaker.TimebreakerRole;
using ParadoxIllusionistRole = Paradox.Roles.Illusionist.IllusionistRole;
using ParadoxPuppeteerRole = Paradox.Roles.Puppeteer.PuppeteerRole;
using ParadoxBlackmailerRole = Paradox.Roles.Blackmailer.BlackmailerRole;
using ParadoxSilencerRole = Paradox.Roles.Silencer.SilencerRole;
using ParadoxShapeshifterXRole = Paradox.Roles.ShapeshifterX.ShapeshifterXRole;
using Paradox.Roles.Witness;
using Paradox.Roles.Guardian;
using ParadoxChronologistRole = Paradox.Roles.Chronologist.ChronologistRole;
using ParadoxDetectiveRole = Paradox.Roles.Detective.DetectiveRole;
using Paradox.Roles.Medic;
using ParadoxTrackerRole = Paradox.Roles.Tracker.TrackerRole;
using ParadoxAnalystRole = Paradox.Roles.Analyst.AnalystRole;
using ParadoxSeerRole = Paradox.Roles.Seer.SeerRole;
using Paradox.Roles.Forensic;
using Paradox.Roles.Stabilizer;
using Paradox.Roles.Anomaly;
using ParadoxBountyHunterRole = Paradox.Roles.BountyHunter.BountyHunterRole;
using ParadoxHarbingerRole = Paradox.Roles.Harbinger.HarbingerRole;
using ParadoxCollectorRole = Paradox.Roles.Collector.CollectorRole;
using ParadoxForgottenRole = Paradox.Roles.Forgotten.ForgottenRole;
using ParadoxRevenantRole = Paradox.Roles.Revenant.RevenantRole;
using ParadoxJesterXRole = Paradox.Roles.JesterX.JesterXRole;
using ParadoxPhantomRole = Paradox.Roles.Phantom.PhantomRole;
using ParadoxEngineerXRole = Paradox.Roles.EngineerX.EngineerXRole;
using ParadoxDispatcherRole = Paradox.Roles.Dispatcher.DispatcherRole;
using ParadoxLocksmithRole = Paradox.Roles.Locksmith.LocksmithRole;
using ParadoxTechnicianRole = Paradox.Roles.Technician.TechnicianRole;
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

        if (ParadoxPhantomRole.TryBlockMurder(target))
            return false;

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

        // Count an actual death only once. A revived Revenant clears its prior
        // evidence before a later, distinct death can be recorded.
        if (!DeathEvidenceRegistry.Record(
            target.PlayerId,
            __instance.PlayerId,
            UnityEngine.Time.time))
            return;

        ParadoxGame.AddFrom(ParadoxMeterSource.Kill);
        ParadoxRevenantRole.OnKilled(target);
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
    [HarmonyPostfix]
    public static void EndGameStartPostfix()
    {
        Paradox.Maps.ParadoxStationMapHudPatch.Close();
        ParadoxGame.Reset();
        DeathEvidenceRegistry.Reset();
        RoleAssignment.Reset();
        ObserverRole.Reset();
        DoppelgangerRole.Reset();
        ParasiteRole.Reset();
        CleanerRole.Reset();
        ParadoxDevourerRole.ResetRuntime();
        ParadoxCorruptorRole.ResetRuntime();
        ParadoxNightmareRole.ResetRuntime();
        ParadoxRiftmakerRole.ResetRuntime();
        ParadoxSaboteurRole.ResetRuntime();
        ParadoxUndertakerRole.ResetRuntime();
        ParadoxTimebreakerRole.ResetRuntime();
        ParadoxIllusionistRole.ResetRuntime();
        ParadoxPuppeteerRole.ResetRuntime();
        ParadoxBlackmailerRole.ResetRuntime();
        ParadoxSilencerRole.ResetRuntime();
        ParadoxShapeshifterXRole.ResetRuntime();
        WitnessRole.Reset();
        GuardianRole.ResetRuntime();
        ParadoxChronologistRole.ResetRuntime();
        ParadoxDetectiveRole.ResetRuntime();
        MedicRole.ResetRuntime();
        ParadoxTrackerRole.ResetRuntime();
        ParadoxAnalystRole.ResetRuntime();
        ParadoxSeerRole.ResetRuntime();
        ForensicRole.ResetRuntime();
        StabilizerRole.ResetRuntime();
        AnomalyRole.ResetRuntime();
        ParadoxBountyHunterRole.ResetRuntime();
        ParadoxHarbingerRole.ResetRuntime();
        ParadoxCollectorRole.ResetRuntime();
        ParadoxForgottenRole.ResetRuntime();
        ParadoxRevenantRole.ResetRuntime();
        ParadoxJesterXRole.ResetRuntime();
        ParadoxPhantomRole.ResetRuntime();
        ParadoxEngineerXRole.ResetRuntime();
        ParadoxDispatcherRole.ResetRuntime();
        ParadoxLocksmithRole.ResetRuntime();
        ParadoxTechnicianRole.ResetRuntime();
    }
}
