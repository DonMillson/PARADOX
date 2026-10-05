namespace Paradox.Roles;

public static class RoleRegistry
{
    public static IReadOnlyList<RoleDefinition> All { get; } =
        Enum.GetValues<RoleId>()
            .Select(id => new RoleDefinition(
                id,
                GetFaction(id),
                $"role.{id}.name",
                $"role.{id}.description"))
            .ToArray();

    public static RoleFaction GetFaction(RoleId id) => id switch
    {
        >= RoleId.Doppelganger and <= RoleId.Riftmaker => RoleFaction.Impostor,
        >= RoleId.Observer and <= RoleId.Stabilizer => RoleFaction.Crewmate,
        _ => RoleFaction.Neutral
    };
}
