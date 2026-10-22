namespace Paradox.Settings;

/// <summary>
/// Safe capacity policy for future 15–50 player private-server matches.
/// The policy does not bypass Innersloth's official matchmaking/server cap.
/// </summary>
public static class LargeLobbyPolicy
{
    public const int VanillaMaxPlayers = 15;
    public const int ExperimentalMaxPlayers = 50;
    public const int MinimumPlayers = 4;

    public static int ClampRequestedLimit(int requested) =>
        Math.Clamp(requested, VanillaMaxPlayers, ExperimentalMaxPlayers);

    public static bool CanUnlock(bool experimentalEnabled, bool privateServerSelected) =>
        experimentalEnabled && privateServerSelected;

    public static int EffectiveMaxPlayers(
        int requested, bool experimentalEnabled, bool privateServerSelected) =>
        CanUnlock(experimentalEnabled, privateServerSelected)
            ? ClampRequestedLimit(requested)
            : VanillaMaxPlayers;

    public static bool IsLargeLobby(int count) => count > VanillaMaxPlayers;

    /// <summary>
    /// Upper-bound verification only: actual network capacity, registration,
    /// meeting HUD paging and game-rule compatibility still need live tests.
    /// </summary>
    public static bool WithinExperimentalTarget(int count) =>
        count >= 0 && count <= ExperimentalMaxPlayers;
}
