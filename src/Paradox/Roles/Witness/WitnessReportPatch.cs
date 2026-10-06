using HarmonyLib;

namespace Paradox.Roles.Witness;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
public static class WitnessReportPatch
{
    [HarmonyPrefix]
    public static void ReportDeadBodyPrefix(PlayerControl __instance, NetworkedPlayerInfo target)
    {
        if (__instance == null || target == null || !WitnessRole.IsWitness(__instance.PlayerId))
            return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.PlayerId != __instance.PlayerId)
            return;

        DeadBody? body = null;
        foreach (var candidate in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (candidate != null && candidate.ParentId == target.PlayerId)
            {
                body = candidate;
                break;
            }
        }

        if (body == null || !WitnessRole.TryCreateClue(__instance, body, out var clue))
            return;

        var localizer = Localization.DefaultTranslations.Create();
        var message = localizer.Get(clue.LocalizationKey);

        ParadoxPlugin.Instance.Log.LogInfo($"Witness clue: {message}");
        // Current Among Us no longer exposes NotificationPopper.AddItem.
        // Keep the clue in the log until the 2026-compatible HUD adapter is wired.
    }
}
