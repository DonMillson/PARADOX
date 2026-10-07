using Paradox.Roles;

namespace Paradox.Settings;

public static class ParadoxRoleSettings
{
    private static readonly Dictionary<RoleId, bool> Enabled = new()
    {
        [RoleId.Doppelganger] = true,
        [RoleId.Parasite] = true,
        [RoleId.Puppeteer] = true,
        [RoleId.Cleaner] = true,
        [RoleId.Devourer] = true,
        [RoleId.Corruptor] = true,
        [RoleId.Nightmare] = true,
        [RoleId.ShapeshifterX] = true,
        [RoleId.Riftmaker] = true,
        [RoleId.Saboteur] = true,
        [RoleId.Undertaker] = true,
        [RoleId.Timebreaker] = true,
        [RoleId.Illusionist] = true,
        [RoleId.Observer] = true,
        [RoleId.Witness] = true,
        [RoleId.Guardian] = true,
        [RoleId.Chronologist] = true,
        [RoleId.Detective] = true,
        [RoleId.Medic] = true,
        [RoleId.Tracker] = true,
        [RoleId.Analyst] = true,
        [RoleId.Seer] = true,
        [RoleId.Forensic] = true,
        [RoleId.Stabilizer] = true,
        [RoleId.Anomaly] = true,
        [RoleId.Forgotten] = true,
        [RoleId.BountyHunter] = true,
        [RoleId.Survivor] = true,
        [RoleId.Opportunist] = true,
        [RoleId.Harbinger] = true,
        [RoleId.Collector] = true,
        [RoleId.EngineerX] = true,
        [RoleId.Dispatcher] = true,
        [RoleId.Locksmith] = true,
        [RoleId.Technician] = true
    };

    private static readonly Dictionary<RoleId, int> SpawnChance = new()
    {
        [RoleId.Doppelganger] = 100,
        [RoleId.Parasite] = 100,
        [RoleId.Puppeteer] = 100,
        [RoleId.Cleaner] = 100,
        [RoleId.Devourer] = 100,
        [RoleId.Corruptor] = 100,
        [RoleId.Nightmare] = 100,
        [RoleId.ShapeshifterX] = 100,
        [RoleId.Riftmaker] = 100,
        [RoleId.Saboteur] = 100,
        [RoleId.Undertaker] = 100,
        [RoleId.Timebreaker] = 100,
        [RoleId.Illusionist] = 100,
        [RoleId.Observer] = 100,
        [RoleId.Witness] = 100,
        [RoleId.Guardian] = 100,
        [RoleId.Chronologist] = 100,
        [RoleId.Detective] = 100,
        [RoleId.Medic] = 100,
        [RoleId.Tracker] = 100,
        [RoleId.Analyst] = 100,
        [RoleId.Seer] = 100,
        [RoleId.Forensic] = 100,
        [RoleId.Stabilizer] = 100,
        [RoleId.Anomaly] = 100,
        [RoleId.Forgotten] = 100,
        [RoleId.BountyHunter] = 100,
        [RoleId.Survivor] = 100,
        [RoleId.Opportunist] = 100,
        [RoleId.Harbinger] = 100,
        [RoleId.Collector] = 100,
        [RoleId.EngineerX] = 100,
        [RoleId.Dispatcher] = 100,
        [RoleId.Locksmith] = 100,
        [RoleId.Technician] = 100
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
    public static float PuppeteerDurationSeconds { get; set; } = 6f;
    public static float PuppeteerCooldownSeconds { get; set; } = 30f;
    public static float PuppeteerFollowDistance { get; set; } = 0.85f;

    public static bool CleanerEnabled
    {
        get => IsEnabled(RoleId.Cleaner);
        set => SetEnabled(RoleId.Cleaner, value);
    }

    public static int CleanerSpawnChancePercent
    {
        get => GetSpawnChance(RoleId.Cleaner);
        set => SetSpawnChance(RoleId.Cleaner, value);
    }

    public static float CleanerCooldownSeconds { get; set; } = 25f;
    public static float DevourerCooldownSeconds { get; set; } = 35f;
    public static float CorruptorDurationSeconds { get; set; } = 10f;
    public static float CorruptorCooldownSeconds { get; set; } = 30f;
    public static float NightmareDurationSeconds { get; set; } = 7f;
    public static float NightmareCooldownSeconds { get; set; } = 30f;
    public static float ShapeshifterXDurationSeconds { get; set; } = 12f;
    public static float ShapeshifterXCooldownSeconds { get; set; } = 35f;
    public static float RiftmakerAnchorDelaySeconds { get; set; } = 2f;
    public static float RiftmakerCooldownSeconds { get; set; } = 25f;
    public static float SaboteurCooldownSeconds { get; set; } = 30f;
    public static float SaboteurBonusMeter { get; set; } = 4f;
    public static float UndertakerCooldownSeconds { get; set; } = 20f;
    public static float UndertakerCarryOffsetY { get; set; } = -0.45f;
    public static float TimebreakerDurationSeconds { get; set; } = 4f;
    public static float TimebreakerCooldownSeconds { get; set; } = 30f;
    public static float IllusionistDurationSeconds { get; set; } = 10f;
    public static float IllusionistCooldownSeconds { get; set; } = 30f;
    public static float GuardianProtectionDurationSeconds { get; set; } = 10f;
    public static float GuardianCooldownSeconds { get; set; } = 30f;
    public static float ChronologistCooldownSeconds { get; set; } = 25f;
    public static float DetectiveCooldownSeconds { get; set; } = 30f;
    public static float DetectiveRecentActivitySeconds { get; set; } = 30f;
    public static float MedicCooldownSeconds { get; set; } = 25f;
    public static float TrackerDurationSeconds { get; set; } = 18f;
    public static float TrackerCooldownSeconds { get; set; } = 30f;
    public static float AnalystCooldownSeconds { get; set; } = 25f;
    public static float SeerCooldownSeconds { get; set; } = 25f;
    public static float ForensicCooldownSeconds { get; set; } = 20f;
    public static float StabilizerCooldownSeconds { get; set; } = 25f;
    public static float StabilizerReductionAmount { get; set; } = 10f;
    public static float AnomalyCooldownSeconds { get; set; } = 20f;
    public static float ForgottenCooldownSeconds { get; set; } = 15f;
    public static int ForgottenRequiredMemories { get; set; } = 2;
    public static float BountyHunterCooldownSeconds { get; set; } = 25f;
    public static float OpportunistMeterThreshold { get; set; } = 50f;
    public static float HarbingerCooldownSeconds { get; set; } = 20f;
    public static int HarbingerRequiredOmens { get; set; } = 3;
    public static float HarbingerWinMeterThreshold { get; set; } = 75f;
    public static float HarbingerBonusMeter { get; set; } = 3f;
    public static float CollectorCooldownSeconds { get; set; } = 20f;
    public static int CollectorRequiredSamples { get; set; } = 3;
    public static float EngineerXCooldownSeconds { get; set; } = 25f;
    public static float EngineerXStabilizeAmount { get; set; } = 8f;
    public static float DispatcherCooldownSeconds { get; set; } = 25f;
    public static float LocksmithCooldownSeconds { get; set; } = 18f;
    public static float LocksmithUseRange { get; set; } = 1.8f;
    public static float TechnicianShieldDurationSeconds { get; set; } = 15f;
    public static float TechnicianCooldownSeconds { get; set; } = 30f;

    public static bool IsImplemented(RoleId role) => role is
        RoleId.Doppelganger or
        RoleId.Parasite or
        RoleId.Puppeteer or
        RoleId.Cleaner or
        RoleId.Devourer or
        RoleId.Corruptor or
        RoleId.Nightmare or
        RoleId.ShapeshifterX or
        RoleId.Riftmaker or
        RoleId.Saboteur or
        RoleId.Undertaker or
        RoleId.Timebreaker or
        RoleId.Illusionist or
        RoleId.Observer or
        RoleId.Witness or
        RoleId.Guardian or
        RoleId.Chronologist or
        RoleId.Detective or
        RoleId.Medic or
        RoleId.Tracker or
        RoleId.Analyst or
        RoleId.Seer or
        RoleId.Forensic or
        RoleId.Stabilizer or
        RoleId.Anomaly or
        RoleId.Forgotten or
        RoleId.BountyHunter or
        RoleId.Survivor or
        RoleId.Opportunist or
        RoleId.Harbinger or
        RoleId.Collector or
        RoleId.EngineerX or
        RoleId.Dispatcher or
        RoleId.Locksmith or
        RoleId.Technician;

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
        PuppeteerDurationSeconds = 6f;
        PuppeteerCooldownSeconds = 30f;
        PuppeteerFollowDistance = 0.85f;
        CleanerCooldownSeconds = 25f;
        DevourerCooldownSeconds = 35f;
        CorruptorDurationSeconds = 10f;
        CorruptorCooldownSeconds = 30f;
        NightmareDurationSeconds = 7f;
        NightmareCooldownSeconds = 30f;
        ShapeshifterXDurationSeconds = 12f;
        ShapeshifterXCooldownSeconds = 35f;
        RiftmakerAnchorDelaySeconds = 2f;
        RiftmakerCooldownSeconds = 25f;
        SaboteurCooldownSeconds = 30f;
        SaboteurBonusMeter = 4f;
        UndertakerCooldownSeconds = 20f;
        UndertakerCarryOffsetY = -0.45f;
        TimebreakerDurationSeconds = 4f;
        TimebreakerCooldownSeconds = 30f;
        IllusionistDurationSeconds = 10f;
        IllusionistCooldownSeconds = 30f;
        GuardianProtectionDurationSeconds = 10f;
        GuardianCooldownSeconds = 30f;
        ChronologistCooldownSeconds = 25f;
        DetectiveCooldownSeconds = 30f;
        DetectiveRecentActivitySeconds = 30f;
        MedicCooldownSeconds = 25f;
        TrackerDurationSeconds = 18f;
        TrackerCooldownSeconds = 30f;
        AnalystCooldownSeconds = 25f;
        SeerCooldownSeconds = 25f;
        ForensicCooldownSeconds = 20f;
        StabilizerCooldownSeconds = 25f;
        StabilizerReductionAmount = 10f;
        AnomalyCooldownSeconds = 20f;
        ForgottenCooldownSeconds = 15f;
        ForgottenRequiredMemories = 2;
        BountyHunterCooldownSeconds = 25f;
        OpportunistMeterThreshold = 50f;
        HarbingerCooldownSeconds = 20f;
        HarbingerRequiredOmens = 3;
        HarbingerWinMeterThreshold = 75f;
        HarbingerBonusMeter = 3f;
        CollectorCooldownSeconds = 20f;
        CollectorRequiredSamples = 3;
        EngineerXCooldownSeconds = 25f;
        EngineerXStabilizeAmount = 8f;
        DispatcherCooldownSeconds = 25f;
        LocksmithCooldownSeconds = 18f;
        LocksmithUseRange = 1.8f;
        TechnicianShieldDurationSeconds = 15f;
        TechnicianCooldownSeconds = 30f;
    }
}
