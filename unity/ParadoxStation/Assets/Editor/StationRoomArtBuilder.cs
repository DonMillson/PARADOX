// PARADOX / Build All 12 Room Art Previews
// Standalone Unity 2D ART authoring only. No Among Us MapId or ShipStatus here.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StationRoomArtBuilder
{
    private const string ArtRoot = "Assets/Art/StationRooms";
    private const string GeneratedRoot = "Assets/Generated/StationRooms";
    private const float Ppu = 96f;

    private static readonly string[] Rooms =
    {
        "arrival_bay", "cargo", "crew_quarters", "security", "communications",
        "observation", "temporal_lab", "medical", "containment",
        "reactor_rift", "power_core", "void_chamber",
    };
    private static readonly string[] Layers =
    {
        "01_floor", "02_walls", "03_props", "04_lights"
    };

    [MenuItem("PARADOX/Build All 12 Room Art Previews")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedRoot);

        foreach (string id in Rooms)
        {
            foreach (string layer in Layers)
            {
                string path = ArtRoot + "/" + id + "/" + layer + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException(
                        "Missing original PNG art: " + path
                        + " -- run python tools/export_station_art.py");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Ppu;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Each room is its own reusable prefab, not one giant merged texture.
        // Spatial offsets here are ONLY for the Unity art inspection gallery,
        // not the logical game-world grid coordinates.
        for (int i = 0; i < Rooms.Length; i++)
        {
            string id = Rooms[i];
            string outputFolder = GeneratedRoot + "/" + id;
            EnsureFolder(outputFolder);
            GameObject root = new GameObject("PARADOX_Room_" + id);
            try
            {
                for (int j = 0; j < Layers.Length; j++)
                {
                    string layer = Layers[j];
                    string art = ArtRoot + "/" + id + "/" + layer + ".png";
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
                    if (sprite == null)
                        throw new InvalidOperationException("Cannot load " + art);

                    GameObject child = new GameObject(layer);
                    child.transform.SetParent(root.transform, false);
                    SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = 10 + j * 10;
                }

                // Placeholder boundary colliders are separate objects and
                // therefore replaceable with production-ready collision polygons.
                AddWall(root.transform, "left", -8.5f, 0f, 0.28f, 5.3f);
                AddWall(root.transform, "right", 8.5f, 0f, 0.28f, 5.3f);
                AddWall(root.transform, "bottom", 0f, -3.45f, 14.4f, 0.30f);
                AddWall(root.transform, "top_left", -4.6f, 3.45f, 6.8f, 0.30f);
                AddWall(root.transform, "top_right", 4.6f, 3.45f, 6.8f, 0.30f);

                string prefabPath = outputFolder + "/" + id + "_Art.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Prefab not saved: " + prefabPath);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                int col = i % 4;
                int row = i / 4;
                instance.transform.position =
                    new Vector3((col - 1.5f) * 21.5f, (1 - row) * 13.5f, 0f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        GameObject cameraObject = new GameObject("PARADOX_ArtGalleryCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 22.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.05f, 0.08f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        string scenePath = GeneratedRoot + "/Station_12RoomGallery.unity";
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new InvalidOperationException("Failed to save " + scenePath);

        AssetDatabase.Refresh();
        Debug.Log("PARADOX: Built 12 room art prefabs and gallery scene at " + scenePath);
    }

    private static void AddWall(
        Transform parent, string name, float x, float y, float w, float h)
    {
        GameObject go = new GameObject("Collision_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(w, h);
        collider.isTrigger = false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
