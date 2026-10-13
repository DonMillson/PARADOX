// PARADOX STATION production authoring: one connected twelve-room Unity scene.
// In Unity Editor run:
//   PARADOX > Production > Build Connected Station Scene
//
// Not the old F7 off-map prototype. This builds a real Unity scene with layered
// room art, traversable corridors, global boundary colliders, and placement
// metadata. It is NOT YET a selectable Among Us ShipStatus map: Among Us-specific
// components, real task minigames and native map registration must be integrated.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ParadoxStationProductionSceneBuilder
{
    [Serializable] private sealed class StationLayout
    {
        public int version;
        public float cellSize;
        public float roomWidth;
        public float roomHeight;
        public float corridorWidth;
        public RoomInfo[] rooms;
        public CorridorInfo[] corridors;
        public VentInfo[] vents;
        public WallInfo[] walls;
    }
    [Serializable] private sealed class RoomInfo
    {
        public string id;
        public string name;
        public float x;
        public float y;
        public bool isSpawn;
        public string taskId;
    }
    [Serializable] private sealed class PointInfo { public float x; public float y; }
    [Serializable] private sealed class CorridorInfo
    {
        public string id;
        public string fromRoom;
        public string toRoom;
        public PointInfo[] points;
    }
    [Serializable] private sealed class VentInfo { public string fromRoom; public string toRoom; }
    [Serializable] private sealed class WallInfo
    {
        public float x1; public float y1;
        public float x2; public float y2;
    }

    private const string LayoutPath = "Assets/Resources/StationProductionLayout.json";
    private const string ArtPath = "Assets/Art/StationRooms";
    private const string GeneratedPath = "Assets/Generated/StationProduction";
    private const float ArtPpu = 96f;

    [MenuItem("PARADOX/Production/Build Connected Station Scene")]
    public static void Build()
    {
        var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
        if (manifest == null)
            throw new InvalidOperationException(
                "Missing layout JSON. Run python tools/build_station_production_layout.py first.");

        StationLayout layout = JsonUtility.FromJson<StationLayout>(manifest.text);
        if (layout == null || layout.version != 1 || layout.rooms == null ||
            layout.rooms.Length != 12 || layout.corridors == null ||
            layout.corridors.Length != 16 || layout.vents == null ||
            layout.vents.Length != 3 || layout.walls == null ||
            layout.walls.Length < 20)
            throw new InvalidOperationException(
                "PARADOX production layout invalid. Rebuild the Python manifest.");

        EnsureFolder(GeneratedPath);
        string floorPath = GenerateFloorSpriteAsset();
        Sprite corridorFloor = AssetDatabase.LoadAssetAtPath<Sprite>(floorPath);
        if (corridorFloor == null)
            throw new InvalidOperationException("Station corridor floor sprite missing.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("PARADOX_STATION_PRODUCTION_MAP");
        GameObject floors = Child(root.transform, "Corridors_Floor");
        GameObject rooms = Child(root.transform, "Rooms");
        GameObject collision = Child(root.transform, "Navigation_Collision");
        GameObject markers = Child(root.transform, "PlacementMarkers_NOT_GAME_TASKS");

        foreach (CorridorInfo corridor in layout.corridors)
        {
            if (corridor.points == null || corridor.points.Length < 2)
                throw new InvalidOperationException("Invalid corridor: " + corridor.id);
            GameObject corridorGroup=Child(floors.transform,"Corridor_"+corridor.id);
            for (int i=1; i<corridor.points.Length; i++)
            {
                Vector2 a=Point(corridor.points[i-1]);
                Vector2 b=Point(corridor.points[i]);
                float distance=Vector2.Distance(a,b);
                if (distance < 0.05f)
                    continue;
                GameObject go=Child(corridorGroup.transform,"Segment_"+i);
                go.transform.position=new Vector3((a.x+b.x)/2,(a.y+b.y)/2,0);
                go.transform.rotation=Quaternion.Euler(
                    0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
                SpriteRenderer renderer=go.AddComponent<SpriteRenderer>();
                renderer.sprite=corridorFloor;
                renderer.drawMode=SpriteDrawMode.Tiled;
                renderer.size=new Vector2(distance+layout.corridorWidth,layout.corridorWidth);
                renderer.sortingOrder=-28;
            }
            // Review markers for the future dynamic door/airlock prefab system.
            GameObject routeMarker=Child(markers.transform,"Route_"+corridor.id);
            routeMarker.transform.position=Vector3.zero;
        }

        foreach(RoomInfo room in layout.rooms)
        {
            GameObject roomRoot=Child(rooms.transform,"Room_"+room.id);
            roomRoot.transform.position=new Vector3(room.x,room.y,0);
            for(int i=0; i<4; i++)
            {
                string name=string.Format("{0:00}_{1}",i+1,
                    new[]{"floor","walls","props","lights"}[i]);
                string filename=ArtPath+"/"+room.id+"/"+name+".png";
                Sprite sprite=LoadSprite(filename);
                GameObject layer=Child(roomRoot.transform,name);
                SpriteRenderer renderer=layer.AddComponent<SpriteRenderer>();
                renderer.sprite=sprite;
                renderer.sortingOrder=new[]{-20,0,10,20}[i];
            }

            // Spawns and tasks are PLACEHOLDERS for native Among Us components,
            // but have fixed, explicit transform locations in this scene.
            GameObject spawn=Child(roomRoot.transform,
                room.isSpawn ? "PlayerSpawn_Entry" : "RoomNavigationAnchor");
            spawn.transform.localPosition=Vector3.zero;
            GameObject consoleSocket=Child(roomRoot.transform,
                "TaskSocket_"+room.taskId);
            consoleSocket.transform.localPosition=new Vector3(3.4f,-2.0f,0);
        }

        // These colliders are derived from the unified walkable floor:
        // no solid room wall sits across a corridor entrance.
        for(int i=0; i<layout.walls.Length; i++)
        {
            WallInfo segment=layout.walls[i];
            Vector2 a=new Vector2(segment.x1,segment.y1);
            Vector2 b=new Vector2(segment.x2,segment.y2);
            float len=Vector2.Distance(a,b);
            if(len<0.1f) continue;
            GameObject go=Child(collision.transform,"Wall_"+i);
            go.transform.position=new Vector3((a.x+b.x)/2,(a.y+b.y)/2,0);
            go.transform.rotation=Quaternion.Euler(
                0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            BoxCollider2D box=go.AddComponent<BoxCollider2D>();
            box.size=new Vector2(len,0.20f);
            box.isTrigger=false;
        }

        foreach(VentInfo vent in layout.vents)
        {
            GameObject marker=Child(markers.transform,
                "VentLink_"+vent.fromRoom+"__"+vent.toRoom);
            marker.transform.position=Vector3.zero;
        }

        // Save a loadable GameObject prefab separately from the editor preview
        // scene. The prefab bundle can later be instantiated by a native
        // Among Us ShipStatus adapter; it deliberately has NO ShipStatus yet.
        string prefabPath=GeneratedPath+"/PARADOX_STATION_ENVIRONMENT.prefab";
        GameObject environment=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        if(environment==null)
            throw new InvalidOperationException("Could not create station environment prefab.");
        AssetImporter prefabImporter=AssetImporter.GetAtPath(prefabPath);
        if(prefabImporter==null)
            throw new InvalidOperationException("Station prefab importer missing.");
        prefabImporter.assetBundleName="paradoxstation-environment";
        prefabImporter.SaveAndReimport();

        string scenePath=GeneratedPath+"/ParadoxStation_ProductionLayout.unity";
        if(!EditorSceneManager.SaveScene(scene,scenePath))
            throw new InvalidOperationException("Unable to save production station scene");

        // This is a real Unity SCENE asset and can be built into an asset bundle.
        // IMPORTANT: native Among Us ShipStatus/map registration is NOT included.
        AssetImporter sceneImporter=AssetImporter.GetAtPath(scenePath);
        if(sceneImporter==null)
            throw new InvalidOperationException("Scene importer missing");
        sceneImporter.assetBundleName="paradoxstation-production-scene";
        sceneImporter.SaveAndReimport();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PARADOX: production Unity scene generated. " +
            "12 room art prefabs/layout, 16 traversable corridors and global " +
            layout.walls.Length + " combined boundary wall segments. " +
            "Do not ship as playable Among Us map until ShipStatus adapter is complete.");
    }

    [MenuItem("PARADOX/Production/Build Windows Scene AssetBundle")]
    public static void BuildBundle()
    {
        string scenePath=GeneratedPath+"/ParadoxStation_ProductionLayout.unity";
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)==null ||
           AssetDatabase.LoadAssetAtPath<GameObject>(
               GeneratedPath+"/PARADOX_STATION_ENVIRONMENT.prefab")==null)
            throw new InvalidOperationException(
                "Build connected scene and environment prefab before bundle export.");
        string output=Path.GetFullPath(
            Path.Combine(Application.dataPath,"../Build/StationWindows"));
        Directory.CreateDirectory(output);
        AssetBundleManifest result=BuildPipeline.BuildAssetBundles(
            output,BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows);
        if(result==null)
            throw new InvalidOperationException("AssetBundle build failed.");
        Debug.Log("PARADOX scene and environment prefab bundles at: "+output +
            ". They still require an Among Us ShipStatus map adapter.");
    }

    private static Sprite LoadSprite(string filename)
    {
        TextureImporter importer=AssetImporter.GetAtPath(filename) as TextureImporter;
        if(importer==null)
            throw new InvalidOperationException(
                "Missing station sprite: "+filename+
                ". Export with python tools/export_station_art.py");
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=ArtPpu;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(filename);
        if(sprite==null)
            throw new InvalidOperationException("Sprite import failed: "+filename);
        return sprite;
    }

    private static string GenerateFloorSpriteAsset()
    {
        string filename=GeneratedPath+"/CorridorFloor.png";
        if(!File.Exists(filename))
        {
            const int width=64;
            Texture2D texture=new Texture2D(width,width,TextureFormat.RGBA32,false);
            texture.filterMode=FilterMode.Bilinear;
            for(int y=0;y<width;y++)
            for(int x=0;x<width;x++)
            {
                bool seam=x%31==0 || y%31==0;
                bool rivet=(x%31==4 || x%31==27) &&
                           (y%31==4 || y%31==27);
                byte shade=(byte)(32+((x/8+y/8)%3)*5);
                texture.SetPixel(x,y,rivet?new Color32(117,175,180,255):
                    seam?new Color32(22,43,55,255):
                         new Color32(shade,(byte)(shade+21),(byte)(shade+33),255));
            }
            texture.Apply();
            File.WriteAllBytes(filename,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
        }
        var importer=AssetImporter.GetAtPath(filename) as TextureImporter;
        if(importer==null)
            throw new InvalidOperationException("Cannot import corridor texture.");
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spriteMeshType=SpriteMeshType.FullRect;
        importer.spritePixelsPerUnit=32f;
        importer.filterMode=FilterMode.Bilinear;
        importer.wrapMode=TextureWrapMode.Repeat;
        importer.SaveAndReimport();
        return filename;
    }

    private static Vector2 Point(PointInfo p)=>new Vector2(p.x,p.y);

    private static GameObject Child(Transform parent,string name)
    {
        GameObject go=new GameObject(name);
        go.transform.SetParent(parent,false);
        return go;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))
            return;
        string parent=Path.GetDirectoryName(path)?.Replace('\\','/')??"Assets";
        if(path=="Assets") return;
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
}
#endif
