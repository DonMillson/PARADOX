// Place the exported four PNGs in Assets/Art/ArrivalBay, then select:
// PARADOX > Build Arrival Bay Art Preview
//
// Intentionally a standalone art-preview Unity scene, NOT a registered
// Among Us ShipStatus map or a substitute for custom-map integration.
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ArrivalBayBuilder
{
    private const string Art = "Assets/Art/ArrivalBay";
    private const string Output = "Assets/Generated/ArrivalBay";
    private const float PixelsPerUnit = 96f;

    [MenuItem("PARADOX/Build Arrival Bay Art Preview")]
    public static void Build()
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(Output);

        string[] names = { "01_floor", "02_walls", "03_props", "04_lights" };
        foreach (string name in names)
        {
            string file = Art + "/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException(
                    "Missing exported Arrival Bay PNG: " + file
                    + ". Run tools/export_arrival_bay.py first.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        GameObject root = new GameObject("PARADOX_ArrivalBay_Art");
        try
        {
            for (int index = 0; index < names.Length; index++)
            {
                string name = names[index];
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    Art + "/" + name + ".png");
                if (sprite == null)
                    throw new InvalidOperationException("Sprite failed import: " + name);

                GameObject layer = new GameObject(name);
                layer.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = index * 10;
                renderer.color = Color.white;
            }

            // First-pass collision envelope, in sprite/PPU space.
            // The top airlock opening is intentional and should connect to
            // the eventual station corridor prefab.
            AddWall(root.transform, "left_wall", -8.50f, 0f, 0.32f, 5.4f);
            AddWall(root.transform, "right_wall", 8.50f, 0f, 0.32f, 5.4f);
            AddWall(root.transform, "bottom_bulkhead", 0f, -3.45f, 14.5f, 0.35f);
            AddWall(root.transform, "top_left_bulkhead", -4.80f, 3.45f, 6.7f, 0.35f);
            AddWall(root.transform, "top_right_bulkhead", 4.80f, 3.45f, 6.7f, 0.35f);

            string prefabPath = Output + "/ArrivalBay_Art.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Arrival Bay prefab creation failed.");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(prefab);

            GameObject cameraObject = new GameObject("ArrivalBay_PreviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.05f, 0.075f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            string scenePath = Output + "/ArrivalBay_ArtPreview.unity";
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException("Unable to save preview scene.");

            AssetDatabase.Refresh();
            Debug.Log("PARADOX: Arrival Bay art prefab and editor scene created at " + Output);
        }
        finally
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AddWall(
        Transform parent, string name,
        float x, float y, float width, float height)
    {
        GameObject wall = new GameObject("Collision_" + name);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = new Vector3(x, y, 0f);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);
        collider.isTrigger = false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string folder = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
