using HarmonyLib;

namespace Paradox.Roles.ParadoxTeam;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class ParadoxTeamEndGamePatch
{
    public static bool WasParadoxWin { get; private set; }

    [HarmonyPrefix]
    public static void OnGameEndPrefix(ref EndGameResult endGameResult)
    {
        WasParadoxWin = (int)endGameResult.GameOverReason == ParadoxRole.CustomWinReason;
        if (WasParadoxWin)
            endGameResult.GameOverReason = GameOverReason.ImpostorsByKill;
    }

    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (!WasParadoxWin)
            return;
        EndGameResult.CachedWinners =
            new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!ParadoxRole.IsAlive(player) ||
                !PlayerRoleRegistry.TryGet(player.PlayerId, out var role) ||
                (role != RoleId.Paradox && role != RoleId.Anomaly))
                continue;
            EndGameResult.CachedWinners.Add(new CachedPlayerData(player.Data));
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class ParadoxTeamEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(EndGameManager __instance)
    {
        if (!ParadoxTeamEndGamePatch.WasParadoxWin ||
            __instance == null || __instance.WinText == null)
            return;
        __instance.WinText.text = ParadoxPlugin.Localizer.Get("role.Paradox.win");
    }
}
