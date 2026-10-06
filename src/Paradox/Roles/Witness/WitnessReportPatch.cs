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

        var message = ParadoxPlugin.Localizer.Get(clue.LocalizationKey);
        var title = ParadoxPlugin.Localizer.Get("role.Witness.clue.title");

        ParadoxPlugin.Instance.Log.LogInfo($"Witness clue: {message}");

        var hud = HudManager.Instance;
        if (hud != null)
            hud.ShowPopUp($"PARADOX — {title}: {message}");
    }
}
