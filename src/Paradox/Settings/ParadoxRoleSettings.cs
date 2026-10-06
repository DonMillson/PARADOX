namespace Paradox.Settings;

public static class ParadoxRoleSettings
{
    public static bool DoppelgangerEnabled { get; set; } = true;
    public static int DoppelgangerSpawnChancePercent { get; set; } = 100;
    public static float DoppelgangerDisguiseDurationSeconds { get; set; } = 12f;
    public static float DoppelgangerCooldownSeconds { get; set; } = 30f;

    public static bool ParasiteEnabled { get; set; } = true;
    public static int ParasiteSpawnChancePercent { get; set; } = 100;
    public static float ParasiteInfectionDurationSeconds { get; set; } = 15f;
    public static float ParasiteCooldownSeconds { get; set; } = 30f;

    public static void SetParasiteSpawnChance(int value) =>
        ParasiteSpawnChancePercent = Math.Clamp(value, 0, 100);

    public static void SetDoppelgangerSpawnChance(int value) =>
        DoppelgangerSpawnChancePercent = Math.Clamp(value, 0, 100);

    public static void ResetDefaults()
    {
        DoppelgangerEnabled = true;
        DoppelgangerSpawnChancePercent = 100;
        DoppelgangerDisguiseDurationSeconds = 12f;
        DoppelgangerCooldownSeconds = 30f;
        ParasiteEnabled = true;
        ParasiteSpawnChancePercent = 100;
        ParasiteInfectionDurationSeconds = 15f;
        ParasiteCooldownSeconds = 30f;
    }
}
