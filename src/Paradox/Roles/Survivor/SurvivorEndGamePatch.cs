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

            EndGameResult.CachedWinners.Add(
                new CachedPlayerData(player.Data));
        }
    }
}
