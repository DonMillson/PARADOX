using HarmonyLib;

namespace Paradox.Roles.JesterX;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class JesterXEndGamePatch
{
    public static bool WasJesterXWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(
        ref EndGameResult endGameResult)
    {
        WasJesterXWin =
            (int)endGameResult.GameOverReason ==
            JesterXRole.CustomWinReason;

        if (WasJesterXWin)
            endGameResult.GameOverReason =
                GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasJesterXWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        var winnerId = NeutralWinnerResolver.Resolve(
            RoleId.JesterX, JesterXRole.WinnerPlayerId);

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
public static class JesterXEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(
        EndGameManager __instance)
    {
        if (!JesterXEndGamePatch.WasJesterXWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get("role.JesterX.win");
    }
}
