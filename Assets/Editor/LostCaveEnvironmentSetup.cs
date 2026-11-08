#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotently configures the three intended environment themes and route.</summary>
public static class LostCaveEnvironmentSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string OutputFolder = "Assets/VisualPolish";
    private const string CavernPrefabPath = OutputFolder + "/RunnerDecoration_CrystalCaverns.prefab";

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }

    private static T[] Components<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static Material EnsureLitMaterial(string name, Color color, Color emission, float metallic)
    {
        string path = OutputFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Require(shader != null, "Could not find a Lit shader for " + name + ".");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.38f);
        if (emission.maxColorComponent > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return item;
    }

    private static void AddCrystalCluster(Transform parent, Mesh mesh, Material material, Vector3 position, float scale, Color lightColor)
    {
        GameObject cluster = new GameObject("Luminous Crystal Cluster");
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = position;
        for (int i = 0; i < 3; i++)
        {
            GameObject shard = new GameObject("Faceted Crystal Shard " + (i + 1));
            shard.transform.SetParent(cluster.transform, false);
            shard.transform.localPosition = new Vector3((i - 1) * scale * 0.42f, i == 1 ? scale * 0.38f : 0f, (i % 2) * scale * 0.22f);
            shard.transform.localRotation = Quaternion.Euler(0f, i * 39f, i == 1 ? 9f : -7f);
            shard.transform.localScale = new Vector3(scale * (i == 1 ? 0.78f : 0.54f), scale * (i == 1 ? 1.7f : 1.2f), scale * 0.54f);
            shard.AddComponent<MeshFilter>().sharedMesh = mesh;
            shard.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        Light glow = cluster.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = lightColor;
        glow.intensity = 2.0f;
        glow.range = 8f;
        glow.shadows = LightShadows.None;
    }

    private static GameObject BuildCavernPrefab(Material rock, Material water, Material crystal, Mesh crystalMesh)
    {
        GameObject root = new GameObject("RunnerDecoration_CrystalCaverns");
        root.transform.position = Vector3.zero;

        // A high rock ceiling and repeated ribs make this read as a deep cave, while
        // the supports and water stay outside the three playable lane centers.
        AddPrimitive(root.transform, "CAVERN CEILING", PrimitiveType.Cube,
            new Vector3(0f, 7.4f, 10f), new Vector3(12.2f, 0.9f, 20.3f), rock);
        for (int rib = 0; rib < 3; rib++)
        {
            float z = 1.2f + rib * 8.6f;
            for (int side = -1; side <= 1; side += 2)
            {
                AddPrimitive(root.transform, "CAVERN ARCH PILLAR", PrimitiveType.Cube,
                    new Vector3(side * 5.15f, 2.65f, z), new Vector3(1.05f, 5.3f, 1.1f), rock);
                AddPrimitive(root.transform, "STRATIFIED ROCK OUTCROP", PrimitiveType.Sphere,
                    new Vector3(side * 5.35f, 4.8f, z + 1.7f), new Vector3(2.0f, 1.6f, 2.8f), rock);
            }
            AddPrimitive(root.transform, "CAVERN ARCH CROWN", PrimitiveType.Cube,
                new Vector3(0f, 6.15f, z), new Vector3(10.2f, 0.72f, 1.15f), rock);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            AddPrimitive(root.transform, "UNDERGROUND RIVER", PrimitiveType.Cube,
                new Vector3(side * 4.62f, 0.035f, 10f), new Vector3(1.15f, 0.06f, 19.8f), water);
            for (int i = 0; i < 4; i++)
            {
                float z = 2.2f + i * 4.9f;
                float x = side * (4.95f + (i % 2) * 0.3f);
                AddPrimitive(root.transform, "STALAGMITE", PrimitiveType.Cylinder,
                    new Vector3(x, 0.66f, z), new Vector3(0.44f, 0.72f + (i % 2) * 0.22f, 0.44f), rock);
                AddPrimitive(root.transform, "STALACTITE", PrimitiveType.Cylinder,
                    new Vector3(x * 0.78f, 6.28f, z + 1.8f), new Vector3(0.34f, 0.92f, 0.34f), rock);
            }

            AddCrystalCluster(root.transform, crystalMesh, crystal, new Vector3(side * 4.55f, 0.78f, 4.5f), 0.95f, new Color(0.10f, 0.62f, 1f));
            AddCrystalCluster(root.transform, crystalMesh, crystal, new Vector3(side * 4.65f, 0.82f, 14.3f), 1.1f, new Color(0.16f, 0.74f, 1f));
            AddPrimitive(root.transform, "WATERFALL SHEET", PrimitiveType.Cube,
                new Vector3(side * 5.52f, 2.25f, 10f), new Vector3(0.06f, 3.8f, 2.2f), water);
        }

        // Ancient faceted arch stones and cyan runnels frame the track entrance/exit.
        for (int end = 0; end < 2; end++)
        {
            float z = end == 0 ? 0.25f : 19.75f;
            AddPrimitive(root.transform, "CRYSTAL CAVERN GATE BEAM", PrimitiveType.Cube,
                new Vector3(0f, 5.55f, z), new Vector3(7.2f, 0.55f, 0.9f), crystal);
        }
        return root;
    }

    private static GameObject EnsureCavernPrefab(Material rock, Material water, Material crystal, Mesh mesh)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CavernPrefabPath);
        if (prefab != null) return prefab;

        GameObject root = BuildCavernPrefab(rock, water, crystal, mesh);
        prefab = PrefabUtility.SaveAsPrefabAsset(root, CavernPrefabPath);
        Object.DestroyImmediate(root);
        Require(prefab != null, "Failed to save the Crystal Caverns environment prefab.");
        return prefab;
    }

    [MenuItem("Tools/The Lost Cave/Configure Three Environments")]
    public static void Configure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RunnerTrackManager manager = Components<RunnerTrackManager>(scene).SingleOrDefault();
        RunnerTrackVisuals visuals = manager != null ? manager.GetComponent<RunnerTrackVisuals>() : null;
        PlayerMovement player = Components<PlayerMovement>(scene).SingleOrDefault();
        CameraFollow camera = Components<CameraFollow>(scene).SingleOrDefault();
        GameManager game = Components<GameManager>(scene).SingleOrDefault();
        TreasureChest chest = Components<TreasureChest>(scene).SingleOrDefault();
        RunnerTrackSection[] active = Components<RunnerTrackSection>(scene)
            .Where(section => section.gameObject.activeInHierarchy).OrderBy(section => section.sequenceSlot).ToArray();
        TreasureKey[] keys = Components<TreasureKey>(scene).Where(key => key.gameObject.activeInHierarchy).ToArray();
        Collectible[] pickups = Components<Collectible>(scene).Where(pickup => pickup.gameObject.activeInHierarchy).ToArray();
        Mesh crystalMesh = AssetDatabase.LoadAssetAtPath<Mesh>(OutputFolder + "/Crystal_DoublePyramid.asset");
        GameObject[] roots = scene.GetRootGameObjects();

        // No scene or asset mutation occurs until every required reference has passed.
        Require(manager != null && visuals != null && player != null && camera != null && game != null && chest != null,
            "Preflight failed: runner, visual, player, camera, GameManager or chest reference is missing.");
        Require(manager.player == player && manager.sectionsContainer != null && camera.target == player.transform &&
                !player.adventureMode && active.Length == 4 && active.Select(section => section.sequenceSlot).SequenceEqual(new[] { 0, 1, 2, 3 }),
            "Preflight failed: the saved runner startup route is not the expected four contiguous sections.");
        Require(manager.forgottenEntranceSections != null && manager.forgottenEntranceSections.Length == 3 &&
                manager.crystalCavernsSections != null && manager.crystalCavernsSections.Length == 3 &&
                manager.finalTreasureVaultSections != null && manager.finalTreasureVaultSections.Length == 3 &&
                manager.transitionToCrystalCaverns != null && manager.transitionToFinalTreasureVault != null,
            "Preflight failed: a required section catalog or transition prefab is missing.");
        foreach (RunnerTrackSection section in active)
            Require(section.Validate(3.5f, out string error), "Preflight failed: " + error);
        RunnerTrackSection levelOneTransition = active.SingleOrDefault(section => section.sequenceSlot == 3);
        Require(levelOneTransition != null && Mathf.Abs(levelOneTransition.StartZ - 60f) < 0.01f &&
                Mathf.Abs(levelOneTransition.EndZ - 80f) < 0.01f,
            "Preflight failed: the saved Forgotten Entrance to Crystal Caverns transition is not at Z=60-80.");
        TreasureKey keyOne = keys.SingleOrDefault(key => key.keyId == 1);
        Vector3 keyOneTarget = levelOneTransition.transform.TransformPoint(new Vector3(3.5f, 1f, 12f));
        Require(keyOne != null && Mathf.Abs(keyOne.transform.position.x - 3.5f) < 0.01f &&
                Mathf.Abs(keyOne.transform.position.z - 92f) < 0.01f,
            "Preflight failed: Key ID 1 is not at the inspected source position; refusing to move a different key.");
        Bounds keyTargetBounds = new Bounds(keyOneTarget, new Vector3(0.8f, 1.5f, 0.9f));
        foreach (Obstacle obstacle in levelOneTransition.GetComponentsInChildren<Obstacle>(true))
            foreach (Collider obstacleCollider in obstacle.GetComponentsInChildren<Collider>(true))
                Require(!obstacleCollider.enabled || !obstacleCollider.bounds.Intersects(keyTargetBounds),
                    "Preflight failed: Key ID 1 target overlaps an obstacle collider.");
        Require(!pickups.Any(pickup => (pickup.transform.position - keyOneTarget).sqrMagnitude < 1.44f) &&
                !keys.Any(key => key.keyId != 1 && (key.transform.position - keyOneTarget).sqrMagnitude < 1.44f),
            "Preflight failed: Key ID 1 target overlaps another pickup or key.");
        Require(keys.Length == 4 && keys.Select(key => key.keyId).OrderBy(id => id).SequenceEqual(new[] { 0, 1, 2, 3 }),
            "Preflight failed: exactly one active key for each ID 0-3 is required.");
        foreach (TreasureKey key in keys)
        {
            float minZ = key.keyId < 2 ? 0f : 80f;
            float maxZ = key.keyId < 2 ? 80f : 180f;
            Vector3 keyPosition = key.keyId == 1 ? keyOneTarget : key.transform.position;
            Require(key.visual != null && key.GetComponent<Collider>() is { enabled: true, isTrigger: true } &&
                    keyPosition.x >= -3.5f && keyPosition.x <= 3.5f &&
                    keyPosition.z >= minZ && keyPosition.z < maxZ,
                "Preflight failed: key " + key.keyId + " is not visibly reachable on its intended stage.");
        }
        Require(chest.lid != null && chest.openingSparkles != null && chest.treasureLight != null &&
                chest.GetComponent<Collider>() is { enabled: true, isTrigger: true } && game.requiredTreasureKeys == 2 &&
                chest.transform.position.z > 220f && chest.transform.position.z <= 260f,
            "Preflight failed: the final chest is missing references, trigger collider or end-of-track placement.");
        Require(crystalMesh != null && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab") != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/grass01.prefab") != null,
            "Preflight failed: an existing crystal mesh or forest environment prefab is missing.");
        Require(EditorBuildSettings.scenes.FirstOrDefault(entry => entry.enabled)?.path == ScenePath,
            "Preflight failed: LostCave_Main is not the first enabled Build Settings scene.");
        foreach (GameObject root in roots)
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                    "Preflight failed: a missing script exists on " + child.name + ".");

        string folder = OutputFolder;
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "VisualPolish");
        Material cavernFloor = EnsureLitMaterial("M_CC_CavernFloor", new Color(0.10f, 0.15f, 0.19f), Color.black, 0.18f);
        Material cavernRock = EnsureLitMaterial("M_CC_CavernRock", new Color(0.16f, 0.22f, 0.27f), new Color(0.015f, 0.04f, 0.065f), 0.12f);
        Material cavernWater = EnsureLitMaterial("M_CC_UndergroundWater", new Color(0.025f, 0.23f, 0.34f), new Color(0.02f, 0.24f, 0.42f), 0.38f);
        Material cavernCrystal = EnsureLitMaterial("M_CC_CrystalGlow", new Color(0.20f, 0.80f, 1f), new Color(0.12f, 0.78f, 1f), 0.48f);
        GameObject cavernPrefab = EnsureCavernPrefab(cavernRock, cavernWater, cavernCrystal, crystalMesh);
        AssetDatabase.SaveAssets();

        // Key ID 1 belongs to the final safe lane in the transition module. Its
        // source spot at Z=92 was beyond the saved stage boundary at Z=80.
        keyOne.transform.position = keyOneTarget;
        EditorUtility.SetDirty(keyOne);
        visuals.crystalCavernsDecoration = cavernPrefab;
        visuals.crystalCavernsFloor = cavernFloor;
        if (manager.sectionVisuals == null) manager.sectionVisuals = visuals;
        foreach (RunnerTrackSection section in active) visuals.RefreshSection(section);

        RunnerWorldAtmosphere atmosphere = manager.GetComponent<RunnerWorldAtmosphere>();
        if (atmosphere == null) atmosphere = Undo.AddComponent<RunnerWorldAtmosphere>(manager.gameObject);
        atmosphere.player = player.transform;
        atmosphere.firstStageEndZ = 80f;
        atmosphere.secondStageEndZ = 180f;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(visuals);
        EditorUtility.SetDirty(atmosphere);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "SaveScene failed for LostCave_Main.");
        AssetDatabase.SaveAssets();

        Scene reloaded = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RunnerTrackManager loadedManager = Components<RunnerTrackManager>(reloaded).SingleOrDefault();
        RunnerTrackVisuals loadedVisuals = loadedManager != null ? loadedManager.GetComponent<RunnerTrackVisuals>() : null;
        Require(loadedManager != null && loadedVisuals != null && loadedVisuals.crystalCavernsDecoration != null &&
                loadedVisuals.crystalCavernsFloor != null && loadedManager.crystalCavernsSections.Length == 3 &&
                loadedManager.transitionToFinalTreasureVault != null &&
                Components<RunnerWorldAtmosphere>(reloaded).SingleOrDefault() != null,
            "Reload validation failed: Crystal Caverns references or lighting profile were not saved.");
        TreasureKey reloadedKeyOne = Components<TreasureKey>(reloaded).Single(key => key.keyId == 1);
        Require(Vector3.Distance(reloadedKeyOne.transform.position, keyOneTarget) < 0.01f,
            "Reload validation failed: Key ID 1 was not moved to its reachable Level 1 position.");
        Debug.Log("LOST_CAVE_ENVIRONMENTS_SAVED | stages=ForgottenEntrance,CrystalCaverns,AncientTreasureVault | route=13-slots | key1=(3.5,1,72) | cave-prefab=ready | atmosphere=3-profiles | scene-reopened=true");
    }
}
#endif
