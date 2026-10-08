using HarmonyLib;

namespace Paradox.Roles.Survivor;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class SurvivorEndGamePatch
{
    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        if (EndGameResult.CachedWinners == null)
            return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected ||
                !PlayerRoleRegistry.TryGet(player.PlayerId, out var role) ||
                role != RoleId.Survivor)
                continue;

            // CachedPlayerData exposes PlayerName in the current game API;
            // avoid duplicate winner cards when Survivor already belongs to the vanilla winners.
            var alreadyWinner = EndGameResult.CachedWinners.ToArray()
                .Any(winner => winner.PlayerName == player.Data.PlayerName);

            if (!alreadyWinner)
                EndGameResult.CachedWinners.Add(new CachedPlayerData(player.Data));
        }
    }
}
