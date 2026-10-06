namespace Paradox.Roles.Doppelganger;

public static class DoppelgangerRole
{
    public const float DisguiseDurationSeconds = 12f;
    public const float CooldownSeconds = 30f;

    private static readonly Dictionary<byte, DoppelgangerState> States = new();

    public static bool IsDoppelganger(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Doppelganger;

    public static DoppelgangerState GetOrCreate(byte playerId)
    {
        if (!States.TryGetValue(playerId, out var state))
            States[playerId] = state = new DoppelgangerState(playerId);

        return state;
    }

    public static void Reset() => States.Clear();
}
