using HarmonyLib;
using Paradox.Core;
using Paradox.Settings;

namespace Paradox.Roles.Opportunist;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class OpportunistEndGameCapturePatch
{
    private static readonly HashSet<byte> EligiblePlayers = new();

    [HarmonyPrefix]
    public static void OnGameEndPrefix()
    {
        EligiblePlayers.Clear();

        if (ParadoxGame.State.Meter < ParadoxRoleSettings.OpportunistMeterThreshold)
            return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected ||
                !PlayerRoleRegistry.TryGet(player.PlayerId, out var role) ||
                role != RoleId.Opportunist)
                continue;

            EligiblePlayers.Add(player.PlayerId);
        }
    }

    public static IReadOnlyCollection<byte> Eligible => EligiblePlayers;

    public static void Clear() => EligiblePlayers.Clear();
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class OpportunistEndGameWinnerPatch
{
    [HarmonyPostfix]
    public static void EndGameStartPostfix()
    {
        if (EndGameResult.CachedWinners == null ||
            OpportunistEndGameCapturePatch.Eligible.Count == 0)
            return;

        foreach (var playerId in OpportunistEndGameCapturePatch.Eligible)
        {
            var player = FindPlayer(playerId);
            if (player?.Data == null)
                continue;

            var alreadyWinner = EndGameResult.CachedWinners
                .ToArray()
                .Any(winner => winner.PlayerName == player.Data.PlayerName);

            if (!alreadyWinner)
            {
                EndGameResult.CachedWinners.Add(
                    new CachedPlayerData(player.Data));
            }
        }

        OpportunistEndGameCapturePatch.Clear();
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.PlayerId == playerId)
                return player;
        }

        return null;
    }
}
