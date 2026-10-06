using HarmonyLib;

namespace Paradox.Roles.Anomaly;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class AnomalyEndGamePatch
{
    public static bool WasAnomalyWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(ref EndGameResult endGameResult)
    {
        WasAnomalyWin =
            (int)endGameResult.GameOverReason == AnomalyRole.CustomWinReason;

        if (WasAnomalyWin)
            endGameResult.GameOverReason = GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasAnomalyWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null ||
                !AnomalyRole.IsAnomaly(player.PlayerId))
                continue;

            EndGameResult.CachedWinners.Add(
                new CachedPlayerData(player.Data));
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class AnomalyEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(EndGameManager __instance)
    {
        if (!AnomalyEndGamePatch.WasAnomalyWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get("role.Anomaly.win");
    }
}
