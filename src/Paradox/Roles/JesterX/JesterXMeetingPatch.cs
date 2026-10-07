using HarmonyLib;

namespace Paradox.Roles.JesterX;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
public static class JesterXVotingCompletePatch
{
    [HarmonyPostfix]
    public static void VotingCompletePostfix(
        NetworkedPlayerInfo exiled)
    {
        JesterXRole.TryTriggerWin(exiled);
    }
}
