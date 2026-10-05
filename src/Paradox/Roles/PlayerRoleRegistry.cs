namespace Paradox.Roles;

public static class PlayerRoleRegistry
{
    private static readonly Dictionary<byte, RoleId> Roles = new();

    public static IReadOnlyDictionary<byte, RoleId> All => Roles;

    public static void Assign(byte playerId, RoleId role) => Roles[playerId] = role;

    public static bool TryGet(byte playerId, out RoleId role) =>
        Roles.TryGetValue(playerId, out role);

    public static void Clear() => Roles.Clear();
}
