using HarmonyLib;

namespace Paradox.Roles.Forgotten;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class ForgottenEndGamePatch
{
    public static bool WasForgottenWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(ref EndGameResult endGameResult)
    {
        WasForgottenWin =
            (int)endGameResult.GameOverReason ==
            ForgottenRole.CustomWinReason;

        if (WasForgottenWin)
            endGameResult.GameOverReason =
                GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasForgottenWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        var winnerId = NeutralWinnerResolver.Resolve(
            RoleId.Forgotten, ForgottenRole.WinnerPlayerId);

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.PlayerId != winnerId)
                continue;

            EndGameResult.CachedWinners.Add(
                new CachedPlayerData(player.Data));
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class ForgottenEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(
        EndGameManager __instance)
    {
        if (!ForgottenEndGamePatch.WasForgottenWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get(
                "role.Forgotten.win");
    }
}
