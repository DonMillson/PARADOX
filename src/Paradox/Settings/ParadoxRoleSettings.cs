namespace Paradox.Settings;

public static class ParadoxRoleSettings
{
    public static float DoppelgangerDisguiseDurationSeconds { get; set; } = 12f;
    public static float DoppelgangerCooldownSeconds { get; set; } = 30f;

    public static void ResetDefaults()
    {
        DoppelgangerDisguiseDurationSeconds = 12f;
        DoppelgangerCooldownSeconds = 30f;
    }
}
