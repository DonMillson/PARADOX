namespace Paradox.Roles.JesterX;

public static class JesterXRole
{
    public const int CustomWinReason = 126;
    public const byte NoWinner = byte.MaxValue;

    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsJesterX(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.JesterX;

    public static void TryTriggerWin(NetworkedPlayerInfo? exiled)
    {
        if (exiled == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent ||
            !IsJesterX(exiled.PlayerId))
            return;

        WinnerPlayerId = exiled.PlayerId;
        _endSent = true;

        ParadoxPlugin.Instance.Log.LogInfo(
            $"Jester X {WinnerPlayerId} was voted out and won.");

        GameManager.Instance.RpcEndGame(
            (GameOverReason)CustomWinReason,
            false);
    }

    public static void ResetRuntime()
    {
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }
}
