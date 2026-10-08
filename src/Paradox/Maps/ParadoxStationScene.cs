using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.Maps;

/// <summary>
/// Deterministic walkable station prototype, rendered in the active Among Us
/// world well away from existing maps. This is NOT a registered custom ShipStatus.
/// The host decides whether the experimental instance is active.
/// </summary>
public static class ParadoxStationScene
{
    public const float CellSize = 0.72f;
    public static readonly Vector2 Origin = new(110f, 110f);
    private const int RoomHalfWidth = 4;
    private const int RoomHalfHeight = 3;

    private static GameObject? _root;
    private static Sprite? _white;
    private static Texture2D? _floorTexture;
    private static Sprite? _floorSprite;

    public static bool IsBuilt => _root != null;
    public static Vector2 Spawn =>
        RoomPosition(ParadoxStation.Rooms.Single(r => r.IsSpawn));

    public static Vector2 RoomPosition(ParadoxStation.Room room) =>
        Origin + new Vector2(room.GridX * 10f * CellSize, room.GridY * 10f * CellSize);

    public static Vector2 VentPosition(string roomId)
    {
        var room = ParadoxStation.Rooms.First(r => r.Id == roomId);
        return RoomPosition(room) + new Vector2(-1.8f, 1.3f);
    }

    public static Vector2 ConsolePosition(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= ParadoxStation.Rooms.Count)
            return Origin;
        return RoomPosition(ParadoxStation.Rooms[roomIndex]) +
            new Vector2(1.8f, -1.3f);
    }

    public static void Build()
    {
        if (_root != null)
            return;
        if (!ParadoxStation.TryValidateDesign(out var reason))
            throw new InvalidOperationException("Invalid PARADOX STATION: " + reason);

        var walkable = BuildWalkableTiles();
        if (walkable.Count == 0)
            throw new InvalidOperationException("Station geometry contains no walkable tiles.");

        var root = new GameObject("PARADOX_STATION_ExperimentalScene");
        root.transform.position = new Vector3(Origin.x, Origin.y, 0f);
        _root = root;

        try
        {
            CreateFloor(root.transform, walkable);
            CreateBoundaryWalls(root.transform, walkable);
            CreateRoomLabels(root.transform);
            CreateVentMarkers(root.transform);
            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX STATION scene built ({walkable.Count} floor cells).");
        }
        catch
        {
            Reset();
            throw;
        }
    }

    public static void Reset()
    {
        if (_root != null)
            Object.Destroy(_root);
        _root = null;

        if (_floorSprite != null)
            Object.Destroy(_floorSprite);
        _floorSprite = null;

        if (_floorTexture != null)
            Object.Destroy(_floorTexture);
        _floorTexture = null;
    }

    private static HashSet<(int X, int Y)> BuildWalkableTiles()
    {
        var tiles = new HashSet<(int X, int Y)>();

        foreach (var room in ParadoxStation.Rooms)
        {
            var cx = room.GridX * 10;
            var cy = room.GridY * 10;
            for (var x = -RoomHalfWidth; x <= RoomHalfWidth; x++)
            for (var y = -RoomHalfHeight; y <= RoomHalfHeight; y++)
                tiles.Add((cx + x, cy + y));
        }

        foreach (var corridor in ParadoxStation.Corridors)
        {
            var from = ParadoxStation.Rooms.First(r => r.Id == corridor.FromRoomId);
            var to = ParadoxStation.Rooms.First(r => r.Id == corridor.ToRoomId);
            var x0 = from.GridX * 10;
            var y0 = from.GridY * 10;
            var x1 = to.GridX * 10;
            var y1 = to.GridY * 10;
            var steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) * 3;
            for (var i = 0; i <= steps; i++)
            {
                var t = steps == 0 ? 0f : (float)i / steps;
                var x = (int)Math.Round(x0 + (x1 - x0) * t);
                var y = (int)Math.Round(y0 + (y1 - y0) * t);
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                    tiles.Add((x + dx, y + dy));
            }
        }

        return tiles;
    }

    private static void CreateFloor(
        Transform parent, HashSet<(int X, int Y)> tiles)
    {
        var minX = tiles.Min(t => t.X);
        var maxX = tiles.Max(t => t.X);
        var minY = tiles.Min(t => t.Y);
        var maxY = tiles.Max(t => t.Y);
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        _floorTexture = texture;

        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
        {
            if (!tiles.Contains((x, y)))
            {
                texture.SetPixel(x - minX, y - minY, Color.clear);
                continue;
            }

            var accent = (x + y) % 7 == 0;
            var shade = ((x + y) & 1) == 0;
            texture.SetPixel(x - minX, y - minY,
                accent ? new Color32(27, 75, 83, 255) :
                shade ? new Color32(21, 43, 61, 255) :
                        new Color32(19, 39, 55, 255));
        }

        texture.Apply();
        var sprite = Sprite.Create(texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            1f / CellSize);
        _floorSprite = sprite;
        var go = new GameObject("StationFloor");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(
            (minX + maxX) * 0.5f * CellSize,
            (minY + maxY) * 0.5f * CellSize, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -25;
    }

    private static void CreateBoundaryWalls(
        Transform parent, HashSet<(int X, int Y)> tiles)
    {
        var wallLayer = LayerMask.NameToLayer("Ship");
        if (wallLayer < 0)
            wallLayer = LayerMask.NameToLayer("Walls");
        if (wallLayer < 0)
            wallLayer = 0;

        var wallCount = 0;
        foreach (var tile in tiles)
        {
            var x = tile.X * CellSize;
            var y = tile.Y * CellSize;
            if (!tiles.Contains((tile.X, tile.Y + 1)))
                Wall(parent, x, y + CellSize / 2, CellSize, 0.14f, wallLayer, wallCount++);
            if (!tiles.Contains((tile.X, tile.Y - 1)))
                Wall(parent, x, y - CellSize / 2, CellSize, 0.14f, wallLayer, wallCount++);
            if (!tiles.Contains((tile.X + 1, tile.Y)))
                Wall(parent, x + CellSize / 2, y, 0.14f, CellSize, wallLayer, wallCount++);
            if (!tiles.Contains((tile.X - 1, tile.Y)))
                Wall(parent, x - CellSize / 2, y, 0.14f, CellSize, wallLayer, wallCount++);
        }

        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX STATION built {wallCount} static wall colliders (layer {wallLayer}).");
    }

    private static void Wall(
        Transform parent, float x, float y,
        float width, float height, int layer, int index)
    {
        var go = new GameObject("StationWall_" + index);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite();
        renderer.color = new Color32(69, 193, 208, 255);
        renderer.sortingOrder = 8;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = false;
    }

    private static void CreateRoomLabels(Transform parent)
    {
        var template = Object.FindObjectOfType<TextMeshPro>();
        for (var i = 0; i < ParadoxStation.Rooms.Count; i++)
        {
            var room = ParadoxStation.Rooms[i];
            var local = RoomPosition(room) - Origin;
            var label = new GameObject("StationLabel_" + room.Id);
            label.transform.SetParent(parent, false);
            label.transform.localPosition =
                new Vector3(local.x, local.y + 1.2f, -0.1f);
            label.transform.localScale = Vector3.one * 0.24f;

            var text = label.AddComponent<TextMeshPro>();
            if (template != null && template.font != null)
                text.font = template.font;
            text.text = ParadoxPlugin.Localizer.Get("map.paradoxStation.room." + room.Id);
            text.fontSize = 2.1f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = room.IsSpawn
                ? new Color32(128, 255, 186, 255)
                : new Color32(209, 234, 248, 255);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.renderer.sortingOrder = 20;
            text.rectTransform.sizeDelta = new Vector2(32, 4);
            Paradox.UI.ParadoxFontSupport.ApplyTo(text);

            var console = ConsolePosition(i) - Origin;
            var marker = new GameObject("StationConsole_" + room.TaskId);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition =
                new Vector3(console.x, console.y, 0f);
            marker.transform.localScale = new Vector3(0.52f, 0.52f, 1f);
            var icon = marker.AddComponent<SpriteRenderer>();
            icon.sprite = WhiteSprite();
            icon.color = new Color32(240, 192, 73, 255);
            icon.sortingOrder = 10;
        }
    }

    private static void CreateVentMarkers(Transform parent)
    {
        var distinct = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in ParadoxStation.Vents)
        {
            distinct.Add(link.FromRoomId);
            distinct.Add(link.ToRoomId);
        }

        foreach (var roomId in distinct)
        {
            var pos = VentPosition(roomId) - Origin;
            var go = new GameObject("StationVent_" + roomId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(0.75f, 0.52f, 1f);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = WhiteSprite();
            sprite.color = new Color32(245, 121, 51, 255);
            sprite.sortingOrder = 11;
        }
    }

    private static Sprite WhiteSprite()
    {
        if (_white != null)
            return _white;
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _white = Sprite.Create(texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), 1f);
        return _white;
    }
}
