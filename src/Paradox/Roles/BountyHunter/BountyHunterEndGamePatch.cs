using HarmonyLib;

namespace Paradox.Roles.BountyHunter;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class BountyHunterEndGamePatch
{
    public static bool WasBountyHunterWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(ref EndGameResult endGameResult)
    {
        WasBountyHunterWin =
            (int)endGameResult.GameOverReason == BountyHunterRole.CustomWinReason;

        if (WasBountyHunterWin)
            endGameResult.GameOverReason = GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasBountyHunterWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.PlayerId != BountyHunterRole.WinnerPlayerId)
                continue;

            EndGameResult.CachedWinners.Add(
                new CachedPlayerData(player.Data));
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class BountyHunterEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(EndGameManager __instance)
    {
        if (!BountyHunterEndGamePatch.WasBountyHunterWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get("role.BountyHunter.win");
    }
}
