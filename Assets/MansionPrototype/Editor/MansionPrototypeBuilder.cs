using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

public static class MansionPrototypeBuilder
{
    const string Folder = "Assets/MansionPrototype";
    const string ScenePath = "Assets/Scenes/MansionTest/MansionTest.unity";

    [MenuItem("Tools/Mansion/Build scripted prototype")]
    public static void BuildFromMenu()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
    }

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var previous = GameObject.Find("Mansion Prototype");
        if (previous) Object.DestroyImmediate(previous);
        foreach (string oldName in new[] { "Canvas", "Caretaker" })
        {
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == oldName);
            if (old) old.SetActive(false);
        }
        var player = Object.FindFirstObjectByType<Player>();
        var playerBody = player.GetComponent<Rigidbody>();
        player.transform.position = Ground(3, -.5f);
        player.walkIn3D = true; player.moveSpeed = 6.5f;
        ConfigureEnvironment();
        var camera = Object.FindFirstObjectByType<MansionCamera>();
        camera.target = player.transform; camera.purelyTopDown = true; camera.topDownSize = 11;
        camera.SendMessage("LateUpdate");
        // Use a broad, quiet light so labels and navigation remain legible from directly above.
        RenderSettings.ambientLight = new Color(.65f, .67f, .72f);
        var root = new GameObject("Mansion Prototype");
        var manager = root.AddComponent<GameManager>(); manager.scriptedPrototype = true; manager.numberOfActions = 1; manager.player = player;
        player.gameManager = manager;
        var director = root.AddComponent<MansionPrototype>(); director.player = player; director.gameManager = manager;
        director.dialogueFont = PrepareDialogueFont();
        director.dialogueScript = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Data/PrototypeDialogue.json");
        string[] names = { "Daisy", "Sebes", "Percy", "Elara", "Harold" };
        string[] files = { "Daisy Finn", "Sebes Yorke", "Percy Norn", "Elara Venn", "Harold Palmer" };
        Vector2[] positions = { new Vector2(-3.5f, 1.8f), new Vector2(4.2f, 4.6f), new Vector2(-3f, -.4f), new Vector2(5f, -.5f), new Vector2(-1.8f, 4.5f) };
        director.npcs = new NPCDialog[5]; director.portraits = new Texture2D[5]; director.portraitRegions = new Rect[5];
        Directory.CreateDirectory(Folder + "/Materials"); AssetDatabase.Refresh();
        for (int i = 0; i < names.Length; i++)
        {
            string path = Folder + "/Portraits/" + files[i] + ".JPG";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.isReadable = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            director.portraits[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            director.portraitRegions[i] = PortraitRegion(director.portraits[i]);
            var npc = new GameObject(names[i]); npc.transform.SetParent(root.transform); npc.transform.position = Ground(positions[i].x, positions[i].y);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; body.transform.SetParent(npc.transform, false);
            var materialPath = Folder + "/Materials/" + names[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
            material.color = PortraitBackground(director.portraits[i]); body.GetComponent<Renderer>().sharedMaterial = material;
            var talk = npc.AddComponent<NPCDialog>(); talk.prototype = director; talk.prototypeCharacterId = names[i]; talk.gameManager = manager; director.npcs[i] = talk;
            var labelGO = new GameObject("Name Label"); labelGO.transform.SetParent(npc.transform, false); labelGO.transform.localPosition = new Vector3(0, 12f, 1.05f); labelGO.transform.rotation = Quaternion.Euler(90,0,0);
            var label = labelGO.AddComponent<TextMeshPro>(); label.text = names[i].ToUpperInvariant(); label.fontSize = 4; label.alignment = TextAlignmentOptions.Center; label.color = new Color(.98f,.92f,.77f); label.outlineWidth = .22f;
            label.rectTransform.sizeDelta = new Vector2(6, 1.5f);
        }
        manager.NPCDialog = director.npcs[3];
        Physics.SyncTransforms();
        ValidateWalkability(player, director.npcs);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("MANSION PROTOTYPE BUILT: five characters, one time point, two-day dialogue and accusation flow.");
    }

    static Vector3 Ground(float x, float z)
    {
        var hits = Physics.RaycastAll(new Vector3(x, 2.5f, z), Vector3.down, 5f);
        var floor = hits.Where(h => h.collider.name == "Floor").OrderByDescending(h => h.point.y).First();
        return floor.point + Vector3.up * 1.03f;
    }

    static Color PortraitBackground(Texture2D texture)
    {
        // Median border samples avoid isolated JPEG artifacts at the corners.
        var red = new byte[40]; var green = new byte[40]; var blue = new byte[40];
        for (int i = 0; i < 10; i++)
        {
            int x = (i + 1) * texture.width / 11, y = (i + 1) * texture.height / 11;
            Color32[] samples = { texture.GetPixel(x, 0), texture.GetPixel(x, texture.height - 1), texture.GetPixel(0, y), texture.GetPixel(texture.width - 1, y) };
            for (int j = 0; j < 4; j++) { red[i * 4 + j] = samples[j].r; green[i * 4 + j] = samples[j].g; blue[i * 4 + j] = samples[j].b; }
        }
        Array.Sort(red); Array.Sort(green); Array.Sort(blue);
        return new Color32(red[20], green[20], blue[20], 255);
    }

    public static void RefreshFont()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Object.FindFirstObjectByType<MansionPrototype>().dialogueFont = PrepareDialogueFont();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    static TMP_FontAsset PrepareDialogueFont()
    {
        string path = Folder + "/Fonts/Crimson Text UI.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (!asset)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/Fonts/CrimsonText-Regular.ttf");
            asset = TMP_FontAsset.CreateFontAsset(source, 64, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = "Crimson Text UI";
            AssetDatabase.CreateAsset(asset, path);
            asset.material.name = "Crimson Text UI Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures)
            {
                texture.name = "Crimson Text UI Atlas";
                AssetDatabase.AddObjectToAsset(texture, asset);
            }
        }
        string ascii = new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray());
        string dialogue = File.ReadAllText(Folder + "/Data/PrototypeDialogue.json");
        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        asset.TryAddCharacters(new string((ascii + dialogue).Where(c => !char.IsControl(c)).Distinct().ToArray()));
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        foreach (var texture in asset.atlasTextures) EditorUtility.SetDirty(texture);
        EditorUtility.SetDirty(asset.material);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static void ConfigureEnvironment()
    {
        var mansion = GameObject.Find("Mansion");
        foreach (var node in mansion.GetComponentsInChildren<Transform>(true))
            if (node.name == "Backgorund1" || node.name == "Ceiling_Lamp1") node.gameObject.SetActive(false);

        var stairs = mansion.GetComponentsInChildren<MeshCollider>().First(c => c.name == "Stair");
        var floor = mansion.GetComponentsInChildren<MeshCollider>().First(c => c.name == "Floor");
        var upper = mansion.GetComponentsInChildren<MeshCollider>().First(c => c.name == "Floor2");
        Bounds b = stairs.bounds;
        stairs.enabled = false;
        var oldRamp = GameObject.Find("Prototype Stair Ramp");
        if (oldRamp) Object.DestroyImmediate(oldRamp);
        // One broad slope replaces the snagging step/railing collision, with a
        // short flat landing overlapping the upper floor to remove its raised lip.
        float bottomZ = b.max.z + .4f, topZ = upper.bounds.max.z + .15f;
        float bottomY = floor.bounds.max.y + .02f, topY = upper.bounds.max.y + .03f;
        float left = b.min.x - .1f, right = b.max.x + .1f;
        var mesh = new Mesh { name = "Smooth stair ramp collision" };
        mesh.vertices = new[] {
            new Vector3(left,bottomY,bottomZ), new Vector3(right,bottomY,bottomZ),
            new Vector3(left,topY,topZ), new Vector3(right,topY,topZ),
            new Vector3(left,topY,topZ-.8f), new Vector3(right,topY,topZ-.8f)
        };
        mesh.triangles = new[] {0,1,2, 1,3,2, 2,3,4, 3,5,4};
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Folder + "/Materials/StairRamp.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; }
        else AssetDatabase.CreateAsset(mesh,path);
        var ramp = new GameObject("Prototype Stair Ramp");
        ramp.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Configure a portrait crop in UV coordinates; original JPG files remain intact.
    static Rect PortraitRegion(Texture2D texture)
    {
        var pixels = texture.GetPixels32(); int w = texture.width, h = texture.height;
        Color32 background = pixels[0]; int minX = w, maxX = 0, minY = h, maxY = 0;
        for (int y = 0; y < h; y += 3)
            for (int x = 0; x < w; x += 3)
            {
                var c = pixels[y * w + x];
                if (Math.Abs(c.r-background.r)+Math.Abs(c.g-background.g)+Math.Abs(c.b-background.b) < 95) continue;
                minX = Math.Min(minX,x); maxX = Math.Max(maxX,x); minY = Math.Min(minY,y); maxY = Math.Max(maxY,y);
            }
        if (minX >= maxX) return new Rect(0,0,1,1);
        float height = (maxY-minY)*.62f;
        float width = height * .73f;
        float center = (minX+maxX)*.5f;
        float top = Math.Min(h,maxY+height*.035f);
        return new Rect(Mathf.Clamp(center-width*.5f,0,w-width)/w, Math.Max(0,top-height)/h, width/w, height/h);
    }

    static void ValidateWalkability(Player player, NPCDialog[] npcs)
    {
        var mansion = GameObject.Find("Mansion");
        var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(mansion.transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new System.Collections.Generic.List<NavMeshBuildMarkup>(), sources);
        var settings = NavMesh.GetSettingsByID(0); settings.agentRadius = .5f; settings.agentHeight = 2f; settings.agentClimb = .35f;
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(new Vector3(0,5,-15), new Vector3(100,40,100)), Vector3.zero, Quaternion.identity);
        if (!data) throw new Exception("Could not validate mansion walkability.");
        var instance = NavMesh.AddNavMeshData(data);
        try
        {
            if (!NavMesh.SamplePosition(player.transform.position-Vector3.up, out var start, .8f, NavMesh.AllAreas)) throw new Exception("Player spawn is not walkable.");
            foreach (var npc in npcs)
            {
                if (!NavMesh.SamplePosition(npc.transform.position-Vector3.up, out var end, .8f, NavMesh.AllAreas)) throw new Exception(npc.name+" is not on a walkable floor.");
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path) || path.status != NavMeshPathStatus.PathComplete) throw new Exception("No walking path to "+npc.name);
                Debug.Log("WALKABILITY PASS: "+npc.name);
            }
        }
        finally { instance.Remove(); Object.DestroyImmediate(data); }
    }
}
