namespace Paradox.Roles;

public static class InitialRolePool
{
    public static IReadOnlyList<RoleId> Impostor { get; } = new[]
    {
        RoleId.Doppelganger,
        RoleId.Parasite,
        RoleId.Puppeteer,
        RoleId.Cleaner,
        RoleId.Blackmailer,
        RoleId.Devourer,
        RoleId.Corruptor,
        RoleId.Nightmare,
        RoleId.ShapeshifterX,
        RoleId.Riftmaker,
        RoleId.Saboteur,
        RoleId.Undertaker,
        RoleId.Silencer,
        RoleId.Timebreaker,
        RoleId.Illusionist
    };

    public static IReadOnlyList<RoleId> Crewmate { get; } = new[]
    {
        RoleId.EngineerX,
        RoleId.Dispatcher,
        RoleId.Locksmith,
        RoleId.Technician,
        RoleId.Observer,
        RoleId.Witness,
        RoleId.Guardian,
        RoleId.Chronologist,
        RoleId.Detective,
        RoleId.Medic,
        RoleId.Tracker,
        RoleId.Analyst,
        RoleId.Seer,
        RoleId.Forensic,
        RoleId.Stabilizer
    };

    public static IReadOnlyList<RoleId> Neutral { get; } = new[]
    {
        RoleId.Anomaly,
        RoleId.Forgotten,
        RoleId.BountyHunter,
        RoleId.Survivor,
        RoleId.Revenant,
        RoleId.JesterX,
        RoleId.Phantom,
        RoleId.Opportunist,
        RoleId.Harbinger,
        RoleId.Collector
    };
}
