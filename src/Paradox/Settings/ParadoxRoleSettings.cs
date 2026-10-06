using Paradox.Roles;

namespace Paradox.Settings;

public static class ParadoxRoleSettings
{
    private static readonly Dictionary<RoleId, bool> Enabled = new()
    {
        [RoleId.Doppelganger] = true,
        [RoleId.Parasite] = true,
        [RoleId.Observer] = true,
        [RoleId.Witness] = true
    };

    private static readonly Dictionary<RoleId, int> SpawnChance = new()
    {
        [RoleId.Doppelganger] = 100,
        [RoleId.Parasite] = 100,
        [RoleId.Observer] = 100,
        [RoleId.Witness] = 100
    };

    public static bool DoppelgangerEnabled
    {
        get => IsEnabled(RoleId.Doppelganger);
        set => SetEnabled(RoleId.Doppelganger, value);
    }

    public static int DoppelgangerSpawnChancePercent
    {
        get => GetSpawnChance(RoleId.Doppelganger);
        set => SetSpawnChance(RoleId.Doppelganger, value);
    }

    public static float DoppelgangerDisguiseDurationSeconds { get; set; } = 12f;
    public static float DoppelgangerCooldownSeconds { get; set; } = 30f;

    public static bool ParasiteEnabled
    {
        get => IsEnabled(RoleId.Parasite);
        set => SetEnabled(RoleId.Parasite, value);
    }

    public static int ParasiteSpawnChancePercent
    {
        get => GetSpawnChance(RoleId.Parasite);
        set => SetSpawnChance(RoleId.Parasite, value);
    }

    public static float ParasiteInfectionDurationSeconds { get; set; } = 15f;
    public static float ParasiteCooldownSeconds { get; set; } = 30f;

    public static bool IsImplemented(RoleId role) => role is
        RoleId.Doppelganger or
        RoleId.Parasite or
        RoleId.Observer or
        RoleId.Witness;

    public static bool IsEnabled(RoleId role) =>
        IsImplemented(role) && Enabled.TryGetValue(role, out var enabled) && enabled;

    public static int GetSpawnChance(RoleId role) =>
        SpawnChance.TryGetValue(role, out var chance) ? chance : 0;

    public static void SetEnabled(RoleId role, bool value)
    {
        if (!IsImplemented(role))
            return;

        Enabled[role] = value;
    }

    public static void ToggleEnabled(RoleId role) =>
        SetEnabled(role, !IsEnabled(role));

    public static void SetSpawnChance(RoleId role, int value)
    {
        if (!IsImplemented(role))
            return;

        SpawnChance[role] = Math.Clamp(value, 0, 100);
    }

    public static void CycleSpawnChance(RoleId role)
    {
        var current = GetSpawnChance(role);
        var next = current switch
        {
            < 25 => 25,
            < 50 => 50,
            < 75 => 75,
            < 100 => 100,
            _ => 0
        };

        SetSpawnChance(role, next);
    }

    public static void SetParasiteSpawnChance(int value) =>
        SetSpawnChance(RoleId.Parasite, value);

    public static void SetDoppelgangerSpawnChance(int value) =>
        SetSpawnChance(RoleId.Doppelganger, value);

    public static void ResetDefaults()
    {
        foreach (var role in Enabled.Keys.ToArray())
        {
            Enabled[role] = true;
            SpawnChance[role] = 100;
        }

        DoppelgangerDisguiseDurationSeconds = 12f;
        DoppelgangerCooldownSeconds = 30f;
        ParasiteInfectionDurationSeconds = 15f;
        ParasiteCooldownSeconds = 30f;
    }
}
