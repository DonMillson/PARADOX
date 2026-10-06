namespace Paradox.Roles.Observer;

public static class ObserverState
{
    private static readonly List<ObserverTrace> Traces = new();

    public static IReadOnlyList<ObserverTrace> Active => Traces;

    public static void Add(byte sourcePlayerId, float createdAt) =>
        Traces.Add(new ObserverTrace(sourcePlayerId, createdAt));

    public static void RemoveExpired(float now) =>
        Traces.RemoveAll(trace => trace.IsExpired(now));

    public static void Clear() => Traces.Clear();
}
