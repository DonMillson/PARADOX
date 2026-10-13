using BepInEx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.Maps;

/// <summary>
/// Optional, original room sprite layers for the experimental walkable scene.
/// Images are distributed separately from the IL2CPP plugin DLL:
/// BepInEx/plugins/ParadoxAssets/StationRooms/{roomId}/{layer}.png
///
/// Loading room art does NOT turn this arena into a registered ShipStatus map.
/// A missing/incomplete art pack leaves the original procedural floor intact.
/// </summary>
public static class ParadoxStationArtwork
{
    private static readonly string[] Layers =
    {
        "01_floor", "02_walls", "03_props", "04_lights"
    };
    private static readonly int[] SortingOrders = { -22, -14, -8, 6 };
    private static readonly List<Texture2D> Textures = new();
    private static readonly List<Sprite> Sprites = new();

    // Matches the approximately 6.5 world-unit-wide rooms built by
    // ParadoxStationScene; avoids sprites overlapping adjacent rooms.
    private const float RoomArtWidth = 6.15f;
    public static int LoadedRooms { get; private set; }

    public static void AttachToScene(Transform parent)
    {
        Reset();

        var pack = Path.Combine(Paths.PluginPath, "ParadoxAssets", "StationRooms");
        if (!Directory.Exists(pack))
        {
            ParadoxPlugin.Instance.Log.LogInfo(
                $"Station art pack not installed. Expected: {pack}");
            return;
        }

        // Verify the complete pack first to avoid half-textured rooms.
        foreach (var room in ParadoxStation.Rooms)
        foreach (var layer in Layers)
        {
            var filename = Path.Combine(pack, room.Id, layer + ".png");
            if (!File.Exists(filename))
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    $"PARADOX STATION art pack incomplete: {filename}");
                return;
            }
        }

        try
        {
            foreach (var room in ParadoxStation.Rooms)
            {
                var roomRoot = new GameObject("Art_" + room.Id);
                roomRoot.transform.SetParent(parent, false);
                var local = ParadoxStationScene.RoomPosition(room) -
                            ParadoxStationScene.Origin;
                roomRoot.transform.localPosition =
                    new Vector3(local.x, local.y, 0f);

                for (var i = 0; i < Layers.Length; i++)
                {
                    var filename = Path.Combine(pack, room.Id, Layers[i] + ".png");
                    var bytes = File.ReadAllBytes(filename);
                    if (bytes.Length == 0)
                        throw new InvalidOperationException("Empty art layer: " + filename);

                    var texture = new Texture2D(2, 2);
                    texture.filterMode = FilterMode.Bilinear;
                    Textures.Add(texture);
                    if (!ImageConversion.LoadImage(texture, bytes))
                        throw new InvalidOperationException(
                            "Invalid PNG station artwork: " + filename);

                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        texture.width / RoomArtWidth);
                    Sprites.Add(sprite);

                    var artwork = new GameObject(Layers[i] + "_" + room.Id);
                    artwork.transform.SetParent(roomRoot.transform, false);
                    artwork.transform.localPosition = Vector3.zero;
                    var renderer = artwork.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = SortingOrders[i];
                }

                LoadedRooms++;
            }

            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX STATION: loaded {LoadedRooms} illustrated rooms " +
                $"({Sprites.Count} PNG layers) from {pack}.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Could not load optional station art: {e}");
            // The calling scene will discard the partial room renderers.
            throw;
        }
    }

    public static void Reset()
    {
        LoadedRooms = 0;
        foreach (var sprite in Sprites)
            if (sprite != null)
                Object.Destroy(sprite);
        Sprites.Clear();

        foreach (var texture in Textures)
            if (texture != null)
                Object.Destroy(texture);
        Textures.Clear();
    }
}
