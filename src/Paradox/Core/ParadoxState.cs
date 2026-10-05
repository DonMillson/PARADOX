namespace Paradox.Core;

public sealed class ParadoxState
{
    private readonly HashSet<ParadoxThreshold> _triggered = new();

    public float Meter { get; private set; }

    public void Reset()
    {
        Meter = 0f;
        _triggered.Clear();
    }

    public void Set(float value)
    {
        Meter = Math.Clamp(value, 0f, 100f);
    }

    public IReadOnlyList<ParadoxEvent> Add(float amount)
    {
        Meter = Math.Clamp(Meter + amount, 0f, 100f);

        var events = new List<ParadoxEvent>();
        foreach (var threshold in new[]
                 {
                     ParadoxThreshold.Disturbance,
                     ParadoxThreshold.Distortion,
                     ParadoxThreshold.Instability,
                     ParadoxThreshold.Event
                 })
        {
            if (Meter >= (int)threshold && _triggered.Add(threshold))
                events.Add(ParadoxEventCatalog.For(threshold));
        }

        return events;
    }
}
