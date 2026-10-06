using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Core;

public static class ParadoxGame
{
    public static ParadoxState State { get; } = new();

    public static void Reset()
    {
        State.Reset();
        ParadoxEventRuntime.Reset();
    }

    public static IReadOnlyList<ParadoxEvent> AddFrom(ParadoxMeterSource source) =>
        AddMeter(ParadoxMeterRules.AmountFor(source));

    public static IReadOnlyList<ParadoxEvent> AddMeter(float amount)
    {
        if (!AmongUsClient.Instance.AmHost)
            return Array.Empty<ParadoxEvent>();

        var events = State.Add(amount);
        BroadcastMeter();

        foreach (var paradoxEvent in events)
        {
            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX threshold reached: {(int)paradoxEvent.Threshold}% ({paradoxEvent.LocalizationKey})");
            ParadoxEventRuntime.Trigger(paradoxEvent);
        }

        return events;
    }

    public static void ApplySyncedMeter(float meter)
    {
        var previous = State.Meter;
        State.Set(meter);
        ParadoxEventRuntime.TriggerCrossed(previous, State.Meter);
        ParadoxPlugin.Instance.Log.LogInfo($"Paradox Meter synchronized: {State.Meter:0.#}%");
    }

    public static void BroadcastMeter()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || !AmongUsClient.Instance.AmHost)
            return;

        Rpc<SyncParadoxMeterRpc>.Instance.Send(
            player,
            new SyncParadoxMeterRpc.Data(State.Meter),
            immediately: true);
    }
}
