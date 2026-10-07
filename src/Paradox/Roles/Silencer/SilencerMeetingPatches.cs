using HarmonyLib;

namespace Paradox.Roles.Silencer;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class SilencerMeetingStartPatch
{
    [HarmonyPostfix]
    public static void MeetingStartPostfix() =>
        SilencerRole.ShowTargetMeetingFeedback();
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
public static class SilencerMeetingClosePatch
{
    [HarmonyPostfix]
    public static void MeetingClosePostfix() =>
        SilencerRole.ClearMeetingTargets();
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
public static class SilencerCastVotePatch
{
    [HarmonyPrefix]
    public static bool CastVotePrefix(
        [HarmonyArgument(0)] byte srcPlayerId,
        [HarmonyArgument(1)] byte suspectPlayerId)
    {
        if (!SilencerRole.IsSilenced(srcPlayerId))
            return true;

        SilencerRole.ShowBlockedFeedback(srcPlayerId);

        ParadoxPlugin.Instance.Log.LogInfo(
            $"Silencer rejected vote from player {srcPlayerId} against {suspectPlayerId}.");

        return false;
    }
}
