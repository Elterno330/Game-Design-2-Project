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
        var manager = root.AddComponent<GameManager>(); manager.scriptedPrototype = true; manager.numberOfActions = 3; manager.player = player;
        player.gameManager = manager;
        var director = root.AddComponent<MansionPrototype>(); director.player = player; director.gameManager = manager;
        director.dialogueFont = PrepareDialogueFont();
        director.dialogueScript = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Data/PrototypeDialogue.json");
        string[] clueIds = { "blood", "wall_scratches", "tooth", "fur", "pawprint", "footprint", "stolen_item", "lost_item", "door_scratches", "broken_window" };
        director.clueIconIds = clueIds;
        director.clueIcons = new Sprite[clueIds.Length];
        for (int i = 0; i < clueIds.Length; i++)
        {
            string cluePath = Folder + "/Clues/" + clueIds[i] + ".png";
            var clueImporter = (TextureImporter)AssetImporter.GetAtPath(cluePath);
            clueImporter.textureType = TextureImporterType.Sprite;
            clueImporter.spriteImportMode = SpriteImportMode.Single;
            clueImporter.mipmapEnabled = false;
            clueImporter.maxTextureSize = 512;
            clueImporter.SaveAndReimport();
            director.clueIcons[i] = AssetDatabase.LoadAssetAtPath<Sprite>(cluePath);
        }
        string[] names = { "Daisy", "Elara", "Mataba", "Rowan", "Pell", "Sebes", "Shizumi", "Harold", "Marco", "Percy" };
        string[] files = { "Daisy Finn", "Elara Venn", "Mataba Eiji", "Rowan Hale", "Pell Venne", "Sebes Yorke", "Shizumi Kushinada", "Harold Palmer", "Marco Albizu", "Percy Norn" };
        Vector3[] positions = FindSpreadPositions(player, names.Length);
        director.npcs = new NPCDialog[names.Length]; director.portraits = new Texture2D[names.Length]; director.portraitRegions = new Rect[names.Length];
        Directory.CreateDirectory(Folder + "/Materials"); AssetDatabase.Refresh();
        for (int i = 0; i < names.Length; i++)
        {
            string path = Folder + "/Portraits/" + files[i] + ".JPG";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.isReadable = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            director.portraits[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            director.portraitRegions[i] = PortraitRegion(names[i], director.portraits[i]);
            var npc = new GameObject(names[i]); npc.transform.SetParent(root.transform); npc.transform.position = positions[i];
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
        Vector2[] cluePositions = {
            new Vector2(-4.3f, .3f), new Vector2(4.5f, .5f), new Vector2(-4.5f, 3.2f),
            new Vector2(3f, 3.1f), new Vector2(0f, 2.8f)
        };
        director.clueSpots = new Transform[cluePositions.Length];
        for (int i = 0; i < cluePositions.Length; i++)
        {
            var spot = new GameObject("Clue Spot " + (i + 1).ToString("00"));
            spot.transform.SetParent(root.transform);
            spot.transform.position = Ground(cluePositions[i].x, cluePositions[i].y) + Vector3.up * .35f;
            director.clueSpots[i] = spot.transform;
        }
        manager.NPCDialog = director.npcs.First(npc => npc.prototypeCharacterId == "Elara");
        Physics.SyncTransforms();
        ValidateWalkability(player, director.npcs);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("MANSION PROTOTYPE BUILT: ten characters, three time points, five-day dialogue and accusation flow.");
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
        // Keep the hand-validated collision ramp when it already exists. The
        // imported mansion contains several objects named "Stair", and using
        // the first one's narrow bounds caused the rebuilt ramp to become
        // impassable.
        if (existing) { Object.DestroyImmediate(mesh); mesh = existing; }
        else AssetDatabase.CreateAsset(mesh,path);
        var ramp = new GameObject("Prototype Stair Ramp");
        ramp.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Configure a portrait crop in UV coordinates; original JPG files remain intact.
    // Each source illustration has a different pose, so silhouette bounds are not a
    // reliable proxy for the face. These hand-authored anchors keep the face in the
    // same place in every dialogue portrait.
    static Rect PortraitRegion(string characterId, Texture2D texture)
    {
        var faceAnchors = new System.Collections.Generic.Dictionary<string, Vector2>
        {
            { "Daisy",   new Vector2(.49f, .83f) },
            { "Elara",   new Vector2(.53f, .80f) },
            { "Mataba",  new Vector2(.44f, .85f) },
            { "Rowan",   new Vector2(.50f, .82f) },
            { "Pell",    new Vector2(.44f, .86f) },
            { "Sebes",   new Vector2(.46f, .83f) },
            { "Shizumi", new Vector2(.48f, .81f) },
            { "Harold",  new Vector2(.51f, .82f) },
            { "Marco",   new Vector2(.45f, .81f) },
            { "Percy",   new Vector2(.45f, .84f) }
        };

        Vector2 face = faceAnchors.TryGetValue(characterId, out var anchor)
            ? anchor : new Vector2(.5f, .82f);
        const float cropHeight = .34f;
        float cropWidth = cropHeight * texture.height / texture.width * .82f;
        float x = Mathf.Clamp(face.x - cropWidth * .5f, 0f, 1f - cropWidth);
        // Keep the eyes slightly above center and leave room for shoulders.
        float y = Mathf.Clamp(face.y - cropHeight * .58f, 0f, 1f - cropHeight);
        return new Rect(x, y, cropWidth, cropHeight);
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

    static Vector3[] FindSpreadPositions(Player player, int count)
    {
        var mansion = GameObject.Find("Mansion");
        var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(mansion.transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
            new System.Collections.Generic.List<NavMeshBuildMarkup>(), sources);
        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = .5f; settings.agentHeight = 2f; settings.agentClimb = .35f;
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources,
            new Bounds(new Vector3(0, 5, -15), new Vector3(100, 40, 100)), Vector3.zero, Quaternion.identity);
        if (!data) throw new Exception("Could not build navigation data for character placement.");
        var instance = NavMesh.AddNavMeshData(data);
        try
        {
            if (!NavMesh.SamplePosition(player.transform.position - Vector3.up, out var start, 1f, NavMesh.AllAreas))
                throw new Exception("Player spawn is not on the navigation mesh.");
            var candidates = new System.Collections.Generic.List<Vector3>();
            var stairCollider = GameObject.Find("Prototype Stair Ramp")?.GetComponent<Collider>();
            Bounds stairBounds = stairCollider ? stairCollider.bounds : default;
            // Use a dense grid so that rejecting doors and their approach lanes
            // still leaves plenty of good room-interior positions to spread out.
            for (float z = -14f; z <= 7f; z += 1f)
                for (float x = -14f; x <= 14f; x += 1f)
                {
                    if (!TryGround(x, z, out Vector3 point)) continue;
                    // Do not place a character beneath or immediately beside the
                    // staircase. Expand beyond the ramp mesh to cover its visual shell.
                    if (stairCollider && point.x >= stairBounds.min.x - 1.5f && point.x <= stairBounds.max.x + 1.5f
                        && point.z >= stairBounds.min.z - 1.5f && point.z <= stairBounds.max.z + 1.5f) continue;
                    // These are the mansion's actual narrow connections.  The
                    // exclusion radius covers the opening plus its approaches in
                    // both rooms, so an NPC cannot become a door-shaped obstacle.
                    if (IsInAccessLane(point)) continue;
                    if (!NavMesh.SamplePosition(point - Vector3.up, out var hit, .8f, NavMesh.AllAreas)) continue;
                    // Doorways and other choke points sit close to a NavMesh edge
                    // even when the point itself is reachable. Keep NPC capsules
                    // about two player widths away from every navigation boundary.
                    if (!NavMesh.FindClosestEdge(hit.position, out var edge, NavMesh.AllAreas)) continue;
                    float edgeClearance = Vector2.Distance(
                        new Vector2(hit.position.x, hit.position.z),
                        new Vector2(edge.position.x, edge.position.z));
                    // The authored access lanes are excluded explicitly below;
                    // this smaller general margin keeps capsules off ordinary
                    // walls without eliminating the mansion's usable interiors.
                    if (edgeClearance < .75f) continue;
                    var path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path)
                        || path.status != NavMeshPathStatus.PathComplete) continue;
                    Vector3 standing = hit.position + Vector3.up * 1.03f;
                    if ((standing - player.transform.position).sqrMagnitude < 8f) continue;
                    if (!HasRoomClearance(standing)) continue;
                    candidates.Add(standing);
                }
            if (candidates.Count < count) throw new Exception("Not enough reachable character positions were found.");

            var selected = new System.Collections.Generic.List<Vector3>();
            while (selected.Count < count)
            {
                Vector3 best = candidates[0];
                float bestDistance = float.MinValue;
                foreach (Vector3 candidate in candidates)
                {
                    float minimum = selected.Count == 0
                        ? (candidate - player.transform.position).sqrMagnitude
                        : selected.Min(existing => (candidate - existing).sqrMagnitude);
                    if (minimum <= bestDistance) continue;
                    best = candidate; bestDistance = minimum;
                }
                selected.Add(best);
                candidates.Remove(best);
            }
            return selected.ToArray();
        }
        finally
        {
            instance.Remove();
            Object.DestroyImmediate(data);
        }
    }

    static bool TryGround(float x, float z, out Vector3 point)
    {
        var floor = Physics.RaycastAll(new Vector3(x, 2.5f, z), Vector3.down, 5f)
            .Where(hit => hit.collider.name == "Floor").OrderByDescending(hit => hit.point.y).FirstOrDefault();
        if (!floor.collider) { point = default; return false; }
        point = floor.point + Vector3.up * 1.03f;
        return true;
    }

    static bool HasRoomClearance(Vector3 standing)
    {
        // A reachable NavMesh point can still be the center of a doorway. Probe
        // around the full NPC capsule and keep only points with room on most
        // sides, preventing characters from becoming movable locked doors.
        Vector3[] directions =
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            new Vector3(1, 0, 1).normalized, new Vector3(1, 0, -1).normalized,
            new Vector3(-1, 0, 1).normalized, new Vector3(-1, 0, -1).normalized
        };
        const float clearance = 1.75f;
        int openDirections = 0;
        foreach (Vector3 direction in directions)
        {
            Vector3 target = standing + direction * clearance;
            if (Physics.Linecast(standing, target, out _, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)) continue;
            if (!NavMesh.SamplePosition(target - Vector3.up, out var navHit, .45f, NavMesh.AllAreas)) continue;
            Vector2 expected = new Vector2(target.x, target.z);
            Vector2 sampled = new Vector2(navHit.position.x, navHit.position.z);
            if (Vector2.Distance(expected, sampled) <= .45f) openDirections++;
        }
        // Furniture can obstruct a couple of probes even at a good room spot.
        // The explicit access-lane exclusion above handles the mansion doors.
        return openDirections >= 6;
    }

    static bool IsInAccessLane(Vector3 point)
    {
        // World-space centers of the three narrow passages in the current
        // mansion layout. Keep roughly two player lengths clear around each.
        Vector2[] passageCenters =
        {
            new Vector2(-14f, -2f),
            new Vector2(-6f, 0f),
            new Vector2(-2f, 6f)
        };
        Vector2 position = new Vector2(point.x, point.z);
        const float clearRadius = 3.25f;
        return passageCenters.Any(center => Vector2.Distance(position, center) < clearRadius);
    }
}
