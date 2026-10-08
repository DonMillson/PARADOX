using Paradox.Settings;

namespace Paradox.Core;

public static class ParadoxMeterRules
{
    public static float AmountFor(ParadoxMeterSource source) =>
        ParadoxGameplaySettings.GetGain(source);
}
