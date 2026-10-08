using HarmonyLib;

namespace Paradox.Roles.Collector;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class CollectorEndGamePatch
{
    public static bool WasCollectorWin { get; private set; }
    [HarmonyPrefix]
    public static void OnGameEndPrefix(ref EndGameResult endGameResult)
    {
        WasCollectorWin=(int)endGameResult.GameOverReason==CollectorRole.CustomWinReason;
        if(WasCollectorWin)endGameResult.GameOverReason=GameOverReason.ImpostorsByKill;
    }
    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if(!WasCollectorWin)return;
        EndGameResult.CachedWinners=new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
        var winnerId = NeutralWinnerResolver.Resolve(RoleId.Collector, CollectorRole.WinnerPlayerId);
        foreach(var p in PlayerControl.AllPlayerControls)
            if(p!=null&&p.Data!=null&&p.PlayerId==winnerId)
                EndGameResult.CachedWinners.Add(new CachedPlayerData(p.Data));
    }
}
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class CollectorEndGamePresentationPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix(EndGameManager __instance)
    {
        if(CollectorEndGamePatch.WasCollectorWin&&__instance?.WinText!=null)
            __instance.WinText.text=ParadoxPlugin.Localizer.Get("role.Collector.win");
    }
}