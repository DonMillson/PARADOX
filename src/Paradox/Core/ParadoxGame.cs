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

            if (paradoxEvent.Threshold == ParadoxThreshold.Event)
            {
                TriggerFinalEvent();
                continue;
            }

            ParadoxEventRuntime.Trigger(paradoxEvent);
        }

        return events;
    }

    public static float ReduceMeter(float amount)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return 0f;

        var reduction = Math.Min(Math.Max(0f, amount), State.Meter);
        if (reduction <= 0f)
            return 0f;

        State.Set(State.Meter - reduction);
        BroadcastMeter();

        ParadoxPlugin.Instance.Log.LogInfo(
            $"Paradox Meter stabilized by {reduction:0.#}; now {State.Meter:0.#}%.");

        return reduction;
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

    private static void TriggerFinalEvent()
    {
        var finalEvent = ParadoxFinalEventSelector.Pick();
        ParadoxEventRuntime.TriggerFinal(finalEvent);

        var player = PlayerControl.LocalPlayer;
        if (player == null)
            return;

        Rpc<TriggerParadoxFinalEventRpc>.Instance.Send(
            player,
            new TriggerParadoxFinalEventRpc.Data((byte)finalEvent),
            immediately: true);
    }
}
