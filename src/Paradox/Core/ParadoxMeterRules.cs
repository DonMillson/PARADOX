namespace Paradox.Core;

public static class ParadoxMeterRules
{
    public static float AmountFor(ParadoxMeterSource source) => source switch
    {
        ParadoxMeterSource.Kill => 10f,
        ParadoxMeterSource.Sabotage => 4f,
        ParadoxMeterSource.RoleAbility => 2f,
        ParadoxMeterSource.Anomaly => 5f,
        _ => 0f
    };
}
