namespace Paradox.Roles;

public static class InitialRolePool
{
    public static IReadOnlyList<RoleId> Impostor { get; } = new[]
    {
        RoleId.Doppelganger,
        RoleId.Parasite,
        RoleId.Cleaner
    };

    public static IReadOnlyList<RoleId> Crewmate { get; } = new[]
    {
        RoleId.Observer,
        RoleId.Witness,
        RoleId.Guardian,
        RoleId.Medic,
        RoleId.Tracker,
        RoleId.Stabilizer
    };

    public static IReadOnlyList<RoleId> Neutral { get; } = new[]
    {
        RoleId.Anomaly
    };
}
