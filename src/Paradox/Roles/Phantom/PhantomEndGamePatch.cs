using HarmonyLib;

namespace Paradox.Roles.Phantom;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class PhantomEndGamePatch
{
    public static bool WasPhantomWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(
        ref EndGameResult endGameResult)
    {
        WasPhantomWin =
            (int)endGameResult.GameOverReason ==
            PhantomRole.CustomWinReason;

        if (WasPhantomWin)
            endGameResult.GameOverReason =
                GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasPhantomWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.PlayerId != PhantomRole.WinnerPlayerId)
                continue;

            EndGameResult.CachedWinners.Add(
                new CachedPlayerData(player.Data));
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class PhantomEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(
        EndGameManager __instance)
    {
        if (!PhantomEndGamePatch.WasPhantomWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get("role.Phantom.win");
    }
}
