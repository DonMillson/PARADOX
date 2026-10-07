using HarmonyLib;

namespace Paradox.Roles.Harbinger;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class HarbingerEndGamePatch
{
    public static bool WasHarbingerWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(ref EndGameResult endGameResult)
    {
        WasHarbingerWin =
            (int)endGameResult.GameOverReason == HarbingerRole.CustomWinReason;

        if (WasHarbingerWin)
            endGameResult.GameOverReason = GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasHarbingerWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.PlayerId != HarbingerRole.WinnerPlayerId)
                continue;

            EndGameResult.CachedWinners.Add(
                new CachedPlayerData(player.Data));
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class HarbingerEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(EndGameManager __instance)
    {
        if (!HarbingerEndGamePatch.WasHarbingerWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get("role.Harbinger.win");
    }
}
