using Paradox.Core;

namespace Paradox.Roles.Observer;

public static class ObserverRole
{
    public const float TraceLifetimeSeconds = 12f;

    public static bool IsObserver(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Observer;

    public static void RecordAbilityTrace(byte sourcePlayerId, float createdAt)
    {
        ObserverState.Add(sourcePlayerId, createdAt);
        ParadoxPlugin.Instance.Log.LogInfo(
            $"Observer trace created by player {sourcePlayerId}.");
    }

    public static void Reset() => ObserverState.Clear();
}
