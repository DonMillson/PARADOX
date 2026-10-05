namespace Paradox.Maps;

public static class ParadoxStation
{
    public const string Id = "paradox_station";
    public const string NameKey = "map.paradoxStation.name";

    public static IReadOnlyList<string> PlannedRooms { get; } = new[]
    {
        "Arrival Bay",
        "Reactor Rift",
        "Temporal Lab",
        "Observation",
        "Containment",
        "Medical",
        "Security",
        "Communications",
        "Power Core",
        "Cargo",
        "Crew Quarters",
        "Void Chamber"
    };
}
