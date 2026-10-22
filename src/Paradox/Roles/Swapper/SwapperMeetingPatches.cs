using HarmonyLib;

namespace Paradox.Roles.Swapper;

/// <summary>
/// The host changes the recorded ballots just before vanilla tallies votes,
/// keeping its own exile calculation and standard victory path intact.
/// </summary>
[HarmonyPatch]
public static class SwapperMeetingPatches
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void StartPostfix()
    {
        SwapperRole.OnMeetingStart();
        var local = PlayerControl.LocalPlayer;
        if (local != null && SwapperRole.IsSwapper(local.PlayerId))
            SwapperRole.Notify(local.PlayerId, "role.Swapper.help");
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    [HarmonyPrefix]
    public static void CountPrefix(MeetingHud __instance) =>
        SwapperRole.ApplySwapBeforeCount(__instance);

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    [HarmonyPostfix]
    public static void VotingCompletePostfix(NetworkedPlayerInfo exiled) =>
        SwapperRole.OnVotingComplete(exiled);

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    public static void ClosePostfix() => SwapperRole.OnMeetingClose();

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudPostfix() => SwapperRole.ProcessPendingPunishment();
}
