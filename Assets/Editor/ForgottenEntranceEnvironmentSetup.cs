#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ForgottenEntranceEnvironmentSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string ConfigPath = "Assets/VisualPolish/EnvironmentFramework/RunnerRegion_ForgottenEntrance.asset";
    private const string BaseDecorationPath = "Assets/VisualPolish/RunnerDecoration_ForgottenEntrance.prefab";
    private const string TreePath = "Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab";
    private const string GrassPath = "Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/grass01.prefab";
    private const string DirtMaterialPath = "Assets/Fantasy Forest Environment Free Sample/Materials/dirt01.mat";
    private const string RuinsStonePath = "Assets/VisualPolish/M_AR_RuinsStone.mat";
    private const string OutputFolder = "Assets/VisualPolish/ForgottenEntrance";
    private const string GeometryFolder = OutputFolder + "/Geometry";
    private const string MaterialPath = OutputFolder + "/M_FE_JungleBank.mat";
    private const float SectionLength = 20f;
    private const float LaneSpacing = 3.5f;
    private const float ShoulderOuterX = 6.4f;
    private const float BankOuterX = 14f;

    private static readonly string[] SectionPaths =
    {
        "Assets/Prefabs/Runner/Sections/FE_01_ForestGate.prefab",
        "Assets/Prefabs/Runner/Sections/FE_02_CaveRun.prefab",
        "Assets/Prefabs/Runner/Sections/FE_03_EntranceHazards.prefab"
    };

    private static readonly string[] VariantPaths =
    {
        OutputFolder + "/RunnerDecoration_ForgottenGateway.prefab",
        OutputFolder + "/RunnerDecoration_AncientCaveEntrance.prefab",
        OutputFolder + "/RunnerDecoration_LostJunglePath.prefab"
    };

    private static readonly string[] MeshPaths =
    {
        GeometryFolder + "/FE_JungleBank_Gateway.asset",
        GeometryFolder + "/FE_JungleBank_AncientCaveEntrance.asset",
        GeometryFolder + "/FE_JungleBank_LostJunglePath.asset"
    };

    [MenuItem("Tools/Lost Cave/Build Forgotten Entrance Environment")]
    public static void Configure()
    {
        try
        {
            ConfigureGuarded();
        }
        catch (Exception exception)
        {
            Debug.LogError("FORGOTTEN_ENTRANCE_SETUP_ABORTED: " + exception);
            throw;
        }
    }

    private static void ConfigureGuarded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building the Forgotten Entrance assets.");

        RunnerRegionConfig config = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(ConfigPath);
        GameObject sourceDecoration = AssetDatabase.LoadAssetAtPath<GameObject>(BaseDecorationPath);
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePath);
        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPath);
        Material dirtMaterial = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath);
        Material ruinsStone = AssetDatabase.LoadAssetAtPath<Material>(RuinsStonePath);
        if (config == null || sourceDecoration == null || treePrefab == null || grassPrefab == null || dirtMaterial == null || ruinsStone == null)
            throw new InvalidOperationException("A required configuration, source decoration, tree, grass, dirt, or ruins-stone asset reference is missing.");

        if (config.regionId != RunnerRegionId.ForgottenEntrance || config.firstVisualSlot != 0 ||
            config.lastVisualSlot != 2 || config.outgoingTransitionSlot != 3)
            throw new InvalidOperationException("Forgotten Entrance region-to-slot mapping changed; refusing to alter configuration.");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sceneFullPath = Path.Combine(projectRoot, ScenePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sceneFullPath))
            throw new FileNotFoundException("LostCave_Main scene is missing.", sceneFullPath);
        string sceneText = File.ReadAllText(sceneFullPath);
        string configGuid = AssetDatabase.AssetPathToGUID(ConfigPath);
        if (string.IsNullOrEmpty(configGuid) || CountOccurrences(sceneText, "guid: " + configGuid) < 2)
            throw new InvalidOperationException("LostCave_Main no longer references the Forgotten Entrance config from its visual and atmosphere components.");
        string visualSeedMatch = Regex.Match(sceneText, @"variationSeed:\s*(\d+)").Groups[1].Value;
        if (!int.TryParse(visualSeedMatch, out int seed))
            throw new InvalidOperationException("Could not resolve the saved RunnerTrackVisuals variation seed.");

        for (int i = 0; i < SectionPaths.Length; i++)
        {
            RunnerTrackSection section = AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(SectionPaths[i]);
            if (section == null)
                throw new InvalidOperationException("Forgotten Entrance section preflight failed: " + SectionPaths[i] + " is missing.");
            if (Math.Abs(section.sectionLength - SectionLength) > 0.001f)
                throw new InvalidOperationException("Forgotten Entrance section length changed: " + SectionPaths[i]);
            if (!section.Validate(LaneSpacing, out string validationError))
                throw new InvalidOperationException("Forgotten Entrance section preflight failed: " + validationError);
            string sectionGuid = AssetDatabase.AssetPathToGUID(SectionPaths[i]);
            if (string.IsNullOrEmpty(sectionGuid) || !sceneText.Contains("guid: " + sectionGuid))
                throw new InvalidOperationException("Scene track catalog no longer references " + SectionPaths[i]);
        }

        ValidateShoulderGeometry(AssetDatabase.LoadAssetAtPath<GameObject>(SectionPaths[0]));
        ValidateRenderablePrefab(treePrefab, "tree");
        ValidateRenderablePrefab(grassPrefab, "grass");
        if (sourceDecoration.transform.Find("STAGE_GATE") == null)
            throw new InvalidOperationException("The existing decoration prefab no longer has the STAGE_GATE root expected by RunnerTrackVisuals.");

        int[] chosenIndices = Enumerable.Range(0, 3)
            .Select(slot => RunnerEnvironmentVariation.SelectIndex((int)config.regionId, slot, 3, seed))
            .ToArray();
        if (chosenIndices.Distinct().Count() != 3)
            throw new InvalidOperationException("The saved route seed does not select three distinct decoration variants for slots 0, 1, and 2.");

        bool[] variantsExist = VariantPaths.Select(path => File.Exists(AssetFullPath(path))).ToArray();
        bool[] meshesExist = MeshPaths.Select(path => File.Exists(AssetFullPath(path))).ToArray();
        bool materialExists = File.Exists(AssetFullPath(MaterialPath));
        bool anyOutputExists = variantsExist.Any(value => value) || meshesExist.Any(value => value) || materialExists;
        bool allOutputsExist = variantsExist.All(value => value) && meshesExist.All(value => value) && materialExists;
        if (anyOutputExists)
        {
            if (!allOutputsExist)
                throw new InvalidOperationException("Only part of the Forgotten Entrance output set exists; refusing partial overwrite.");
            GameObject[] configuredVariants = config.decorationVariants;
            bool configMatches = configuredVariants != null && configuredVariants.Length == 3;
            if (configMatches)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    string expectedPath = VariantPaths[DesiredVariantIndex(slot)];
                    configMatches &= AssetDatabase.GetAssetPath(configuredVariants[chosenIndices[slot]]) == expectedPath;
                }
            }
            if (!configMatches)
                throw new InvalidOperationException("Generated environment assets exist but the region configuration does not match; refusing to overwrite user changes.");
            ValidateSavedVariantAssets();
            Debug.Log("FORGOTTEN_ENTRANCE_SETUP_ALREADY_CONFIGURED: three variants and terrain banks already exist; no changes made.");
            return;
        }

        // Generate and validate all geometry in memory before the first AssetDatabase write.
        Mesh[] bankMeshes =
        {
            BuildBankMesh("FE_JungleBank_Gateway", 13),
            BuildBankMesh("FE_JungleBank_AncientCaveEntrance", 71),
            BuildBankMesh("FE_JungleBank_LostJunglePath", 139)
        };
        Material bankMaterial = new Material(dirtMaterial) { name = "M_FE_JungleBank" };
        if (bankMaterial.HasProperty("_BaseMap")) bankMaterial.SetTextureScale("_BaseMap", new Vector2(2.5f, 4f));
        else if (bankMaterial.HasProperty("_MainTex")) bankMaterial.SetTextureScale("_MainTex", new Vector2(2.5f, 4f));
        else throw new InvalidOperationException("The existing dirt material has no supported texture property for the terrain bank.");

        int[] treeCounts = { 8, 6, 14 };
        int[] grassCounts = { 32, 28, 52 };
        int[] phases = { 13, 71, 139 };
        GameObject[] inMemoryVariants = new GameObject[3];
        var contents = new List<GameObject>();
        try
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(BaseDecorationPath);
                contents.Add(root);
                ConfigureVariant(root, treePrefab, grassPrefab, bankMeshes[i], bankMaterial,
                    ruinsStone, i, phases[i], treeCounts[i], grassCounts[i]);
                ValidateVariantInMemory(root, treeCounts[i], grassCounts[i], i);
                inMemoryVariants[i] = root;
            }
        }
        catch
        {
            foreach (GameObject root in contents) if (root != null) PrefabUtility.UnloadPrefabContents(root);
            UnityEngine.Object.DestroyImmediate(bankMaterial);
            foreach (Mesh mesh in bankMeshes) if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            throw;
        }

        // All required inputs, route mapping, source geometry and generated in-memory objects passed preflight.
        EnsureAssetFolder(OutputFolder);
        EnsureAssetFolder(GeometryFolder);
        AssetDatabase.CreateAsset(bankMaterial, MaterialPath);
        for (int i = 0; i < bankMeshes.Length; i++) AssetDatabase.CreateAsset(bankMeshes[i], MeshPaths[i]);

        try
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(inMemoryVariants[i], VariantPaths[i]);
                if (saved == null) throw new InvalidOperationException("Unity failed to save generated prefab: " + VariantPaths[i]);
            }
        }
        finally
        {
            foreach (GameObject root in contents) if (root != null) PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject[] variants = VariantPaths.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)).ToArray();
        if (variants.Any(variant => variant == null))
            throw new InvalidOperationException("One or more generated Forgotten Entrance prefabs could not be reopened.");
        for (int slot = 0; slot < 3; slot++)
        {
            int variantIndex = chosenIndices[slot];
            if (variants[variantIndex] == null)
                throw new InvalidOperationException("Variant assignment validation failed for route slot " + slot + ".");
        }

        // Slot-specific assignment is calculated against the saved route seed:
        // slot 0 -> Forgotten Gateway, slot 1 -> Lost Jungle Path, slot 2 -> Ancient Cave Entrance.
        config.decorationVariants = new GameObject[3];
        config.decorationVariants[chosenIndices[0]] = variants[0];
        config.decorationVariants[chosenIndices[2]] = variants[1];
        config.decorationVariants[chosenIndices[1]] = variants[2];
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RunnerRegionConfig reloaded = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(ConfigPath);
        if (reloaded == null || reloaded.decorationVariants == null || reloaded.decorationVariants.Length != 3)
            throw new InvalidOperationException("The region configuration did not retain all three decoration references.");
        for (int slot = 0; slot < 3; slot++)
        {
            GameObject actual = reloaded.GetDecoration(slot, seed);
            string actualPath = AssetDatabase.GetAssetPath(actual);
            if (actualPath != VariantPaths[DesiredVariantIndex(slot)])
                throw new InvalidOperationException("Route slot " + slot + " resolved to " + actualPath + " instead of its intended section environment.");
        }
        ValidateSavedVariantAssets();
        Debug.Log("FORGOTTEN_ENTRANCE_SETUP_SAVED: configured Forgotten Gateway, Lost Jungle Path, and Ancient Cave Entrance for slots 0-2; 28 tree instances and 112 grass clumps across the three modular sections; route scene was not modified.");
    }

    private static int DesiredVariantIndex(int slot)
    {
        if (slot == 0) return 0;
        if (slot == 1) return 2;
        return 1;
    }

    private static string AssetFullPath(string assetPath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void ValidateShoulderGeometry(GameObject section)
    {
        foreach (string side in new[] { "FOREST SHOULDER LEFT", "FOREST SHOULDER RIGHT" })
        {
            Transform shoulder = section.transform.Find(side);
            BoxCollider collider = shoulder != null ? shoulder.GetComponent<BoxCollider>() : null;
            if (collider == null || !collider.enabled || collider.isTrigger)
                throw new InvalidOperationException("A solid shoulder collider is required for foliage support: " + side);
            Vector3 center = shoulder.localPosition;
            Vector3 size = Vector3.Scale(collider.size, shoulder.localScale);
            float inner = Math.Abs(center.x) - size.x * 0.5f;
            float outer = Math.Abs(center.x) + size.x * 0.5f;
            if (Math.Abs(inner - 4f) > 0.01f || Math.Abs(outer - ShoulderOuterX) > 0.01f ||
                Math.Abs(center.y + 0.3f) > 0.01f || Math.Abs(size.y - 0.6f) > 0.01f)
                throw new InvalidOperationException("Forest shoulder dimensions changed; foliage and bank placement must be remeasured.");
        }
    }

    private static void ValidateRenderablePrefab(GameObject prefab, string label)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0 || renderers.Any(renderer => renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0 ||
                                                               renderer.sharedMaterials.Any(material => material == null)))
            throw new InvalidOperationException("The " + label + " prefab has missing renderers or materials.");
        if (prefab.GetComponentsInChildren<MeshFilter>(true).Any(filter => filter.sharedMesh == null))
            throw new InvalidOperationException("The " + label + " prefab contains a MeshFilter with no mesh.");
    }

    private static void ConfigureVariant(GameObject root, GameObject treePrefab, GameObject grassPrefab,
        Mesh bankMesh, Material bankMaterial, Material ruinsStone, int variant, int phase, int treeCount, int grassCount)
    {
        root.name = VariantPaths[variant].Substring(VariantPaths[variant].LastIndexOf('/') + 1).Replace(".prefab", string.Empty);
        if (variant == 0)
        {
            DisableNamed(root.transform, "CAVE WALL ROCK");
            DisableNamed(root.transform, "CAVE STALAGMITE");
            KeepFirstLights(root, 2);
        }
        else if (variant == 1)
        {
            RenameNamed(root.transform, "STAGE_GATE", "CAVE_MOUTH_ARCH");
            KeepFirstLights(root, 2);
        }
        else
        {
            DisableNamed(root.transform, "CAVE WALL ROCK");
            DisableNamed(root.transform, "CAVE STALAGMITE");
            DisableNamed(root.transform, "Torch stone base");
            DisableNamed(root.transform, "Torch emissive flame");
            DisableAllLights(root);
        }

        ApplyRuinsStoneToGate(root.transform, ruinsStone);
        AddBank(root.transform, bankMesh, bankMaterial);
        Transform treeGroup = new GameObject("JUNGLE_CANOPY_TREES").transform;
        treeGroup.SetParent(root.transform, false);
        Transform grassGroup = new GameObject("JUNGLE_SHOULDER_GRASS").transform;
        grassGroup.SetParent(root.transform, false);
        AddTrees(treeGroup, treePrefab, variant, phase, treeCount);
        AddGrass(grassGroup, grassPrefab, variant, phase, grassCount);
    }

    private static void ApplyRuinsStoneToGate(Transform root, Material material)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith("ANCIENT GATE", StringComparison.Ordinal)) continue;
            foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }
    }

    private static void DisableNamed(Transform root, string exactName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == exactName) child.gameObject.SetActive(false);
    }

    private static void RenameNamed(Transform root, string oldName, string newName)
    {
        Transform target = root.Find(oldName);
        if (target == null) throw new InvalidOperationException("Required gate object is missing: " + oldName);
        target.name = newName;
    }

    private static void KeepFirstLights(GameObject root, int keep)
    {
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = keep; i < lights.Length; i++) lights[i].enabled = false;
    }

    private static void DisableAllLights(GameObject root)
    {
        foreach (Light light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;
    }

    private static void AddBank(Transform root, Mesh mesh, Material material)
    {
        GameObject bank = new GameObject("JUNGLE_TERRAIN_BANKS");
        bank.transform.SetParent(root, false);
        MeshFilter filter = bank.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = bank.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void AddTrees(Transform group, GameObject treePrefab, int variant, int phase, int treeCount)
    {
        List<Vector3> positions = TreePositions(variant);
        if (positions.Count != treeCount) throw new InvalidOperationException("Tree layout count does not match expected section density.");
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 position = positions[i];
            GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab, group) as GameObject;
            if (tree == null) throw new InvalidOperationException("Could not instantiate nested tree prefab.");
            tree.name = "Tree_" + (i + 1).ToString("00");
            tree.transform.localPosition = new Vector3(position.x, BankHeight(Math.Abs(position.x), position.z, phase) + 0.02f, position.z);
            float scale = variant == 2 ? 0.52f + (i % 4) * 0.055f : 0.58f + (i % 3) * 0.07f;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, (i * 47 + phase) % 360, 0f);
        }
    }

    private static List<Vector3> TreePositions(int variant)
    {
        var result = new List<Vector3>();
        if (variant == 0)
        {
            result.AddRange(new[] {
                new Vector3(-5.7f, 0f, 1.6f), new Vector3(5.8f, 0f, 6.5f),
                new Vector3(-8.0f, 0f, 8.4f), new Vector3(8.2f, 0f, 15.4f),
                new Vector3(-6.0f, 0f, 18.2f), new Vector3(6.1f, 0f, 2.5f),
                new Vector3(-9.8f, 0f, 13.4f), new Vector3(10.1f, 0f, 4.2f)
            });
        }
        else if (variant == 1)
        {
            result.AddRange(new[] {
                new Vector3(-5.8f, 0f, 2.2f), new Vector3(6.0f, 0f, 5.8f),
                new Vector3(-8.1f, 0f, 8.7f), new Vector3(8.4f, 0f, 13.8f),
                new Vector3(-10.4f, 0f, 16.9f), new Vector3(10.6f, 0f, 3.5f)
            });
        }
        else
        {
            result.AddRange(new[] {
                new Vector3(-5.5f, 0f, 1.0f), new Vector3(5.6f, 0f, 3.8f),
                new Vector3(-7.8f, 0f, 5.9f), new Vector3(8.0f, 0f, 8.6f),
                new Vector3(-10.2f, 0f, 11.0f), new Vector3(10.5f, 0f, 13.7f),
                new Vector3(-5.9f, 0f, 16.1f), new Vector3(6.0f, 0f, 18.8f),
                new Vector3(-12.3f, 0f, 2.9f), new Vector3(12.4f, 0f, 6.0f),
                new Vector3(-8.6f, 0f, 14.8f), new Vector3(8.8f, 0f, 19.3f),
                new Vector3(-6.8f, 0f, 9.9f), new Vector3(7.0f, 0f, 11.8f)
            });
        }
        return result;
    }

    private static void AddGrass(Transform group, GameObject grassPrefab, int variant, int phase, int grassCount)
    {
        System.Random random = new System.Random(phase * 7919 + variant * 313);
        int perSide = grassCount / 2;
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            float side = sideIndex == 0 ? -1f : 1f;
            for (int i = 0; i < perSide; i++)
            {
                GameObject grass = PrefabUtility.InstantiatePrefab(grassPrefab, group) as GameObject;
                if (grass == null) throw new InvalidOperationException("Could not instantiate nested grass prefab.");
                grass.name = "Grass_" + (sideIndex == 0 ? "L_" : "R_") + (i + 1).ToString("00");
                float z = Mathf.Lerp(0.45f, SectionLength - 0.45f, (i + 0.35f + (float)random.NextDouble() * 0.3f) / perSide);
                float xAbs = Mathf.Lerp(4.18f, 13.65f, (float)random.NextDouble());
                grass.transform.localPosition = new Vector3(side * xAbs, BankHeight(xAbs, z, phase) + 0.015f, z);
                float scale = Mathf.Lerp(0.78f, variant == 2 ? 1.45f : 1.30f, (float)random.NextDouble());
                grass.transform.localScale = Vector3.one * scale;
                grass.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            }
        }
    }

    private static float BankHeight(float absX, float z, int phase)
    {
        float t = Mathf.Clamp01((absX - ShoulderOuterX) / (BankOuterX - ShoulderOuterX));
        if (t <= 0f) return 0f;
        float[] profileT = { 0f, 0.18f, 0.45f, 0.75f, 1f };
        float[] profileY = { 0f, 0.26f, 0.10f, -0.24f, -0.66f };
        int segment = 0;
        while (segment < profileT.Length - 2 && t > profileT[segment + 1]) segment++;
        float u = Mathf.InverseLerp(profileT[segment], profileT[segment + 1], t);
        float baseY = Mathf.Lerp(profileY[segment], profileY[segment + 1], u);
        float wave = Mathf.Sin(z * 0.58f + phase * 0.071f) * 0.20f * t;
        return baseY + wave;
    }

    private static Mesh BuildBankMesh(string name, int phase)
    {
        const int zSegments = 10;
        float[] crossT = { 0f, 0.18f, 0.45f, 0.75f, 1f };
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            float side = sideIndex == 0 ? -1f : 1f;
            int start = vertices.Count;
            for (int zi = 0; zi <= zSegments; zi++)
            {
                float z = SectionLength * zi / zSegments;
                for (int xi = 0; xi < crossT.Length; xi++)
                {
                    float t = crossT[xi];
                    float absX = Mathf.Lerp(ShoulderOuterX, BankOuterX, t);
                    vertices.Add(new Vector3(side * absX, BankHeight(absX, z, phase), z));
                    uv.Add(new Vector2(t * 2.5f, z / 4f));
                }
            }

            for (int zi = 0; zi < zSegments; zi++)
            {
                for (int xi = 0; xi < crossT.Length - 1; xi++)
                {
                    int a = start + zi * crossT.Length + xi;
                    int b = a + crossT.Length;
                    int c = a + 1;
                    int d = b + 1;
                    if (side > 0f) triangles.AddRange(new[] { a, b, c, c, b, d });
                    else triangles.AddRange(new[] { a, c, b, c, d, b });
                }
            }
        }

        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void ValidateVariantInMemory(GameObject root, int expectedTrees, int expectedGrass, int variant)
    {
        Transform treeGroup = root.transform.Find("JUNGLE_CANOPY_TREES");
        Transform grassGroup = root.transform.Find("JUNGLE_SHOULDER_GRASS");
        Transform bank = root.transform.Find("JUNGLE_TERRAIN_BANKS");
        if (treeGroup == null || grassGroup == null || bank == null)
            throw new InvalidOperationException("Generated environment module is missing a foliage or terrain group.");
        if (treeGroup.childCount != expectedTrees || grassGroup.childCount != expectedGrass)
            throw new InvalidOperationException("Generated module foliage count validation failed.");
        MeshFilter filter = bank.GetComponent<MeshFilter>();
        MeshRenderer renderer = bank.GetComponent<MeshRenderer>();
        if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null)
            throw new InvalidOperationException("Generated terrain bank has a missing mesh or material.");
        if (variant == 1 && root.transform.Find("CAVE_MOUTH_ARCH") == null)
            throw new InvalidOperationException("Ancient Cave Entrance variant lost its renamed arch.");
    }

    private static void ValidateSavedVariantAssets()
    {
        int[] expectedTrees = { 8, 6, 14 };
        int[] expectedGrass = { 32, 28, 52 };
        for (int i = 0; i < VariantPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPaths[i]);
            if (prefab == null) throw new InvalidOperationException("Saved prefab is missing: " + VariantPaths[i]);
            Transform trees = prefab.transform.Find("JUNGLE_CANOPY_TREES");
            Transform grass = prefab.transform.Find("JUNGLE_SHOULDER_GRASS");
            Transform bank = prefab.transform.Find("JUNGLE_TERRAIN_BANKS");
            if (trees == null || trees.childCount != expectedTrees[i] ||
                grass == null || grass.childCount != expectedGrass[i] ||
                bank == null || bank.GetComponent<MeshFilter>() == null || bank.GetComponent<MeshFilter>().sharedMesh == null)
                throw new InvalidOperationException("Saved prefab child-count or geometry validation failed: " + VariantPaths[i]);
            if (prefab.GetComponentsInChildren<TreasureKey>(true).Length != 0 ||
                prefab.GetComponentsInChildren<RunnerLaneHazard>(true).Length != 0)
                throw new InvalidOperationException("Generated scenery unexpectedly contains gameplay components: " + VariantPaths[i]);
        }
    }

    private static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            string grandparent = Path.GetDirectoryName(parent).Replace('\\', '/');
            string parentName = Path.GetFileName(parent);
            if (!AssetDatabase.IsValidFolder(grandparent)) throw new InvalidOperationException("Missing parent asset folder: " + grandparent);
            AssetDatabase.CreateFolder(grandparent, parentName);
        }
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif