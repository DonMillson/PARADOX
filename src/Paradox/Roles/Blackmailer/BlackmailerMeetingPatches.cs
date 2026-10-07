using HarmonyLib;

namespace Paradox.Roles.Blackmailer;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class BlackmailerMeetingStartPatch
{
    [HarmonyPostfix]
    public static void MeetingStartPostfix() =>
        BlackmailerRole.ShowTargetMeetingFeedback();
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
public static class BlackmailerMeetingClosePatch
{
    [HarmonyPostfix]
    public static void MeetingClosePostfix() =>
        BlackmailerRole.ClearMeetingTargets();
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class BlackmailerSendChatPatch
{
    [HarmonyPrefix]
    public static bool SendChatPrefix()
    {
        var local = PlayerControl.LocalPlayer;

        if (MeetingHud.Instance == null ||
            local == null ||
            !BlackmailerRole.IsBlackmailed(local.PlayerId))
            return true;

        BlackmailerRole.ShowBlockedFeedback();
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
public static class BlackmailerRpcSendChatPatch
{
    [HarmonyPrefix]
    public static bool RpcSendChatPrefix(PlayerControl __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (MeetingHud.Instance == null ||
            local == null ||
            __instance == null ||
            __instance.PlayerId != local.PlayerId ||
            !BlackmailerRole.IsBlackmailed(local.PlayerId))
            return true;

        BlackmailerRole.ShowBlockedFeedback();
        return false;
    }
}
