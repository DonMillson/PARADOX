using HarmonyLib;

namespace Paradox.Roles.Revenant;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class RevenantEndGamePatch
{
    public static bool WasRevenantWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(
        ref EndGameResult endGameResult)
    {
        WasRevenantWin =
            (int)endGameResult.GameOverReason ==
            RevenantRole.CustomWinReason;

        if (WasRevenantWin)
            endGameResult.GameOverReason =
                GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasRevenantWin)
            return;

        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();

        var winnerId = NeutralWinnerResolver.Resolve(
            RoleId.Revenant, RevenantRole.WinnerPlayerId);

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
public static class RevenantEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(
        EndGameManager __instance)
    {
        if (!RevenantEndGamePatch.WasRevenantWin ||
            __instance == null ||
            __instance.WinText == null)
            return;

        __instance.WinText.text =
            ParadoxPlugin.Localizer.Get(
                "role.Revenant.win");
    }
}
