using Paradox.Core;

namespace Paradox.Settings;

/// <summary>
/// Host preferences for per-action instability; separate from match meter state.
/// </summary>
public static class ParadoxGameplaySettings
{
    private static readonly int[] Choices = { 0, 1, 2, 3, 4, 5, 8, 10, 12, 15, 20 };

    private static readonly Dictionary<ParadoxMeterSource, int> Gains = new()
    {
        [ParadoxMeterSource.Kill] = 10,
        [ParadoxMeterSource.Sabotage] = 4,
        [ParadoxMeterSource.RoleAbility] = 2,
        [ParadoxMeterSource.Anomaly] = 5
    };

    public static int GetGain(ParadoxMeterSource source) =>
        Gains.TryGetValue(source, out var value) ? value : 0;

    public static void SetGain(ParadoxMeterSource source, int value)
    {
        if (!Enum.IsDefined(typeof(ParadoxMeterSource), source))
            return;

        Gains[source] = Math.Clamp(value, 0, 20);
    }

    public static int CycleGain(ParadoxMeterSource source, int direction)
    {
        var current = GetGain(source);
        var index = Array.IndexOf(Choices, current);
        if (index < 0)
        {
            index = Array.FindIndex(Choices, choice => choice >= current);
            if (index < 0)
                index = Choices.Length - 1;
        }

        var next = (index + (direction >= 0 ? 1 : -1) + Choices.Length) % Choices.Length;
        SetGain(source, Choices[next]);
        return GetGain(source);
    }
}
