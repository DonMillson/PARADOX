namespace Paradox.Roles;

public sealed record RoleDefinition(
    RoleId Id,
    RoleFaction Faction,
    string NameKey,
    string DescriptionKey,
    bool EnabledByDefault = false
);
