namespace Paradox.Core;

public static class ParadoxEventCatalog
{
    public static ParadoxEvent For(ParadoxThreshold threshold) => threshold switch
    {
        ParadoxThreshold.Disturbance => new(threshold, "event.25"),
        ParadoxThreshold.Distortion => new(threshold, "event.50"),
        ParadoxThreshold.Instability => new(threshold, "event.75"),
        ParadoxThreshold.Event => new(threshold, "event.100"),
        _ => throw new ArgumentOutOfRangeException(nameof(threshold))
    };
}
