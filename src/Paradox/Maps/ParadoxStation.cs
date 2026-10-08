namespace Paradox.Maps;

/// <summary>
/// Original PARADOX STATION level-design manifest.
/// Positions are LOGICAL GRID COORDINATES, not Unity world positions;
/// a real playable custom ShipStatus, colliders, tasks and art assets
/// still need to be built and integrated.
/// </summary>
public static class ParadoxStation
{
    public const string Id = "paradox_station";
    public const string NameKey = "map.paradoxStation.name";

    public sealed record Room(
        string Id,
        string Name,
        int GridX,
        int GridY,
        bool IsSpawn,
        string TaskId);

    public sealed record Passage(string FromRoomId, string ToRoomId);

    public static IReadOnlyList<Room> Rooms { get; } = new[]
    {
        new Room("arrival_bay", "Arrival Bay", -3, 0, true, "verify_arrival_manifest"),
        new Room("cargo", "Cargo", -4, -2, false, "balance_cargo"),
        new Room("crew_quarters", "Crew Quarters", -4, 2, false, "restore_air_filters"),
        new Room("security", "Security", -2, 3, false, "scan_security_logs"),
        new Room("communications", "Communications", 0, 3, false, "calibrate_communications"),
        new Room("observation", "Observation", 2, 3, false, "align_star_scope"),
        new Room("temporal_lab", "Temporal Lab", 2, 1, false, "synchronize_clocks"),
        new Room("medical", "Medical", 0, 1, false, "check_medical_samples"),
        new Room("containment", "Containment", 4, 1, false, "seal_containment"),
        new Room("reactor_rift", "Reactor Rift", 2, -2, false, "stabilize_reactor_rift"),
        new Room("power_core", "Power Core", 0, -2, false, "reroute_power"),
        new Room("void_chamber", "Void Chamber", 4, -2, false, "isolate_void_signal")
    };

    // Undirected room-to-room corridor planning graph.
    public static IReadOnlyList<Passage> Corridors { get; } = new[]
    {
        new Passage("arrival_bay", "crew_quarters"),
        new Passage("arrival_bay", "cargo"),
        new Passage("arrival_bay", "medical"),
        new Passage("cargo", "power_core"),
        new Passage("crew_quarters", "security"),
        new Passage("security", "communications"),
        new Passage("communications", "observation"),
        new Passage("communications", "medical"),
        new Passage("observation", "temporal_lab"),
        new Passage("medical", "temporal_lab"),
        new Passage("medical", "power_core"),
        new Passage("temporal_lab", "containment"),
        new Passage("temporal_lab", "reactor_rift"),
        new Passage("power_core", "reactor_rift"),
        new Passage("reactor_rift", "void_chamber"),
        new Passage("containment", "void_chamber")
    };

    // Vent links are separate from public corridors and require custom prefabs.
    public static IReadOnlyList<Passage> Vents { get; } = new[]
    {
        new Passage("cargo", "power_core"),
        new Passage("security", "containment"),
        new Passage("reactor_rift", "void_chamber")
    };

    public static IReadOnlyList<string> PlannedRooms { get; } =
        Rooms.Select(room => room.Name).ToArray();

    /// <summary>Verify planned navigation before translating the manifest into scene prefabs.</summary>
    public static bool TryValidateDesign(out string reason)
    {
        reason = string.Empty;

        if (Rooms.Count != 12 || Rooms.Count(room => room.IsSpawn) != 1)
        {
            reason = "PARADOX STATION requires 12 rooms and exactly one initial spawn.";
            return false;
        }

        var roomIds = new HashSet<string>(StringComparer.Ordinal);
        var taskIds = new HashSet<string>(StringComparer.Ordinal);
        var coordinates = new HashSet<(int X, int Y)>();

        foreach (var room in Rooms)
        {
            if (string.IsNullOrWhiteSpace(room.Id) ||
                string.IsNullOrWhiteSpace(room.Name) ||
                string.IsNullOrWhiteSpace(room.TaskId) ||
                !roomIds.Add(room.Id) ||
                !taskIds.Add(room.TaskId) ||
                !coordinates.Add((room.GridX, room.GridY)))
            {
                reason = $"Duplicate or incomplete room/task: {room.Id}.";
                return false;
            }
        }

        var neighbors = roomIds.ToDictionary(
            id => id,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var passage in Corridors)
        {
            if (!roomIds.Contains(passage.FromRoomId) ||
                !roomIds.Contains(passage.ToRoomId) ||
                passage.FromRoomId == passage.ToRoomId)
            {
                reason = $"Invalid corridor {passage.FromRoomId} -> {passage.ToRoomId}.";
                return false;
            }

            neighbors[passage.FromRoomId].Add(passage.ToRoomId);
            neighbors[passage.ToRoomId].Add(passage.FromRoomId);
        }

        foreach (var passage in Vents)
        {
            if (!roomIds.Contains(passage.FromRoomId) ||
                !roomIds.Contains(passage.ToRoomId) ||
                passage.FromRoomId == passage.ToRoomId)
            {
                reason = $"Invalid vent {passage.FromRoomId} -> {passage.ToRoomId}.";
                return false;
            }
        }

        var spawn = Rooms.Single(room => room.IsSpawn).Id;
        var visited = new HashSet<string>(StringComparer.Ordinal) { spawn };
        var queue = new Queue<string>();
        queue.Enqueue(spawn);

        while (queue.Count > 0)
        {
            foreach (var next in neighbors[queue.Dequeue()])
            {
                if (visited.Add(next))
                    queue.Enqueue(next);
            }
        }

        if (visited.Count != Rooms.Count)
        {
            reason = $"Station is disconnected: {visited.Count}/{Rooms.Count} rooms reachable.";
            return false;
        }

        return true;
    }
}
