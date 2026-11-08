using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RunnerAdventureBuilder
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string SectionsPath = "Assets/Prefabs/Runner/Sections/";
    private const string PickupsPath = "Assets/Prefabs/Runner/Pickups/";
    private const string KeyPrefabPath = PickupsPath + "Runner_TreasureKey.prefab";
    private const float Length = 20f;
    private const float Lane = 3.5f;

    private static readonly string[] RuinsPaths = { SectionsPath + "AR_01_StoneCauseway.prefab", SectionsPath + "AR_02_BrokenBridge.prefab", SectionsPath + "AR_03_ArchiveRun.prefab" };
    private static readonly string[] TemplePaths = { SectionsPath + "LT_01_GoldenGate.prefab", SectionsPath + "LT_02_TrapHall.prefab", SectionsPath + "LT_03_SunHall.prefab" };
    private static readonly string[] VaultPaths = { SectionsPath + "FV_01_VaultApproach.prefab", SectionsPath + "FV_02_TreasureRun.prefab", SectionsPath + "FV_03_FinalPlatform.prefab" };
    private const string CavernToRuins = SectionsPath + "CC_AR_Transition.prefab";
    private const string RuinsToTemple = SectionsPath + "AR_LT_Transition.prefab";
    private const string TempleToVault = SectionsPath + "LT_FV_Transition.prefab";

    [MenuItem("Tools/Runner Adventure/Build Five Stage Adventure")]
    public static void BuildFiveStageAdventure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolders();
        Material cave = LoadMaterial("Assets/Materials/M_Cave Stone.mat");
        Material rock = LoadMaterial("Assets/Materials/M_RockObstacle.mat");
        Material blue = LoadMaterial("Assets/Materials/M_Crystal_Turquoise.mat");
        Material gold = LoadMaterial("Assets/Materials/M_Crystal_Gold.mat");
        Material ruins = LoadMaterial("Assets/Materials/Blockout_Temp/TEMP_Blockout_R03_Ruins.mat");
        Material temple = LoadMaterial("Assets/Materials/Blockout_Temp/TEMP_Blockout_R04_Temple.mat");
        Material vault = LoadMaterial("Assets/Materials/Blockout_Temp/TEMP_Blockout_R05_Vault.mat");
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Runner/Pickups/Runner_Crystal_Blue.prefab");
        if (crystalPrefab == null) throw new InvalidOperationException("Existing blue crystal runner prefab is missing.");

        GameObject keyPrefab = BuildKeyPrefab(gold);
        AddKeyToExistingPrefab("Assets/Prefabs/Runner/Sections/FE_01_ForestGate.prefab", keyPrefab, 0);
        AddKeyToExistingPrefab("Assets/Prefabs/Runner/Sections/CC_01_CrystalRun.prefab", keyPrefab, 1);

        BuildBiomeSet(RuinsPaths, RunnerBiome.AncientRuins, ruins, rock, blue, keyPrefab, crystalPrefab, -1, true);
        BuildBiomeSet(TemplePaths, RunnerBiome.LostTemple, temple, rock, blue, keyPrefab, crystalPrefab, -1, false);
        BuildBiomeSet(VaultPaths, RunnerBiome.FinalTreasureVault, vault, rock, blue, keyPrefab, crystalPrefab, -1, false);
        BuildTransition(CavernToRuins, "CC_AR_Transition", RunnerBiome.TransitionToAncientRuins, cave, ruins);
        BuildTransition(RuinsToTemple, "AR_LT_Transition", RunnerBiome.TransitionToLostTemple, ruins, temple);
        BuildTransition(TempleToVault, "LT_FV_Transition", RunnerBiome.TransitionToFinalTreasureVault, temple, vault);

        ConfigureScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("M6_ADVENTURE_BUILT | 5 finite stages | 4 unique reachable keys | 19 connected track sections");
    }

    [MenuItem("Tools/Runner Adventure/Rebalance All Gameplay Prefabs")]
    public static void RebalanceAllGameplayPrefabs()
    {
        RunnerBiomeBuilder.RebalanceFirstTwoBiomePrefabs();
        Material rock = LoadMaterial("Assets/Materials/M_RockObstacle.mat");
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Runner/Pickups/Runner_Crystal_Blue.prefab");
        if (crystalPrefab == null) throw new InvalidOperationException("Existing blue crystal runner prefab is missing.");

        RunnerTrackSection[][] stageSets =
        {
            new[] { AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/AR_01_StoneCauseway.prefab"), AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/AR_02_BrokenBridge.prefab"), AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/AR_03_ArchiveRun.prefab") },
            new[] { AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/LT_01_GoldenGate.prefab"), AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/LT_02_TrapHall.prefab"), AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/LT_03_SunHall.prefab") },
            new[] { AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/FV_01_VaultApproach.prefab"), AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/FV_02_TreasureRun.prefab"), AssetDatabase.LoadAssetAtPath<RunnerTrackSection>("Assets/Prefabs/Runner/Sections/FV_03_FinalPlatform.prefab") }
        };
        RunnerBiome[] biomes = { RunnerBiome.AncientRuins, RunnerBiome.LostTemple, RunnerBiome.FinalTreasureVault };
        for (int setIndex = 0; setIndex < stageSets.Length; setIndex++)
        {
            for (int sectionIndex = 0; sectionIndex < stageSets[setIndex].Length; sectionIndex++)
            {
                RunnerTrackSection section = stageSets[setIndex][sectionIndex];
                if (section == null) throw new InvalidOperationException("A planned stage section prefab is missing.");
                string prefabPath = AssetDatabase.GetAssetPath(section);
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    foreach (RunnerLaneHazard oldHazard in contents.GetComponentsInChildren<RunnerLaneHazard>(true))
                        UnityEngine.Object.DestroyImmediate(oldHazard.gameObject);
                    foreach (Collectible oldPickup in contents.GetComponentsInChildren<Collectible>(true))
                        if (oldPickup.enabled && oldPickup.name.StartsWith("Blue crystal trail "))
                            UnityEngine.Object.DestroyImmediate(oldPickup.gameObject);

                    AddSafeObstaclePattern(contents.transform, biomes[setIndex], sectionIndex, rock, true);
                    AddCrystalTrail(contents.transform, crystalPrefab, sectionIndex);
                    if (PrefabUtility.SaveAsPrefabAsset(contents, prefabPath) == null)
                        throw new InvalidOperationException("Failed to save balanced section " + prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateBalancedGameplayPrefabs();
        Debug.Log("BALANCED_GAMEPLAY_PREFABS_SAVED | 5 stages | safe rows | collectibles clear | scene untouched");
    }

    private static void ValidateBalancedGameplayPrefabs()
    {
        string[][] sets =
        {
            new[] { "Assets/Prefabs/Runner/Sections/FE_01_ForestGate.prefab", "Assets/Prefabs/Runner/Sections/FE_02_CaveRun.prefab", "Assets/Prefabs/Runner/Sections/FE_03_EntranceHazards.prefab" },
            new[] { "Assets/Prefabs/Runner/Sections/CC_01_CrystalRun.prefab", "Assets/Prefabs/Runner/Sections/CC_02_WaterfallRun.prefab", "Assets/Prefabs/Runner/Sections/CC_03_GemCrossing.prefab" },
            new[] { "Assets/Prefabs/Runner/Sections/AR_01_StoneCauseway.prefab", "Assets/Prefabs/Runner/Sections/AR_02_BrokenBridge.prefab", "Assets/Prefabs/Runner/Sections/AR_03_ArchiveRun.prefab" },
            new[] { "Assets/Prefabs/Runner/Sections/LT_01_GoldenGate.prefab", "Assets/Prefabs/Runner/Sections/LT_02_TrapHall.prefab", "Assets/Prefabs/Runner/Sections/LT_03_SunHall.prefab" },
            new[] { "Assets/Prefabs/Runner/Sections/FV_01_VaultApproach.prefab", "Assets/Prefabs/Runner/Sections/FV_02_TreasureRun.prefab", "Assets/Prefabs/Runner/Sections/FV_03_FinalPlatform.prefab" }
        };
        List<int> keyIds = new List<int>();
        foreach (string[] set in sets)
        {
            List<KeyValuePair<float, HashSet<int>>> rows = new List<KeyValuePair<float, HashSet<int>>>();
            for (int sectionIndex = 0; sectionIndex < set.Length; sectionIndex++)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(set[sectionIndex]);
                try
                {
                    foreach (TreasureKey key in contents.GetComponentsInChildren<TreasureKey>(true))
                    {
                        if (key.keyId >= 0 && key.keyId < 2) keyIds.Add(key.keyId);
                        Collider keyCollider = key.GetComponent<Collider>();
                        if (keyCollider == null || !keyCollider.enabled || !keyCollider.isTrigger)
                            throw new InvalidOperationException(set[sectionIndex] + ": key trigger is missing.");
                    }

                    foreach (RunnerLaneHazard hazard in contents.GetComponentsInChildren<RunnerLaneHazard>(true))
                    {
                        Collider collider = hazard.GetComponent<Collider>();
                        if (collider == null || !collider.enabled || hazard.GetComponent<Obstacle>() == null)
                            throw new InvalidOperationException(set[sectionIndex] + "/" + hazard.name + ": obstacle collision setup is incomplete.");
                        if (hazard.laneIndex < -1 || hazard.laneIndex > 1)
                            throw new InvalidOperationException(set[sectionIndex] + "/" + hazard.name + ": lane index is outside the three-lane track.");
                    }

                    Dictionary<int, HashSet<int>> blockedByRow = new Dictionary<int, HashSet<int>>();
                    Dictionary<int, float> zByRow = new Dictionary<int, float>();
                    foreach (RunnerLaneHazard hazard in contents.GetComponentsInChildren<RunnerLaneHazard>(true))
                    {
                        if (!blockedByRow.ContainsKey(hazard.patternRow))
                        {
                            blockedByRow[hazard.patternRow] = new HashSet<int>();
                            zByRow[hazard.patternRow] = hazard.transform.localPosition.z;
                        }
                        blockedByRow[hazard.patternRow].Add(hazard.laneIndex);
                        zByRow[hazard.patternRow] = Mathf.Min(zByRow[hazard.patternRow], hazard.transform.localPosition.z);
                    }
                    foreach (KeyValuePair<int, HashSet<int>> row in blockedByRow)
                    {
                        HashSet<int> safeLanes = new HashSet<int> { -1, 0, 1 };
                        foreach (int blockedLane in row.Value) safeLanes.Remove(blockedLane);
                        if (safeLanes.Count == 0)
                            throw new InvalidOperationException(set[sectionIndex] + ": obstacle row blocks every lane.");
                        rows.Add(new KeyValuePair<float, HashSet<int>>(sectionIndex * 20f + zByRow[row.Key], safeLanes));
                    }

                    Transform root = contents.transform;
                    foreach (Collectible pickup in contents.GetComponentsInChildren<Collectible>(true))
                    {
                        if (!pickup.enabled || !pickup.gameObject.activeInHierarchy) continue;
                        Vector3 local = root.InverseTransformPoint(pickup.transform.position);
                        int lane = Mathf.RoundToInt(local.x / Lane);
                        if (lane < -1 || lane > 1 || Mathf.Abs(local.x - lane * Lane) > 0.25f)
                            throw new InvalidOperationException(set[sectionIndex] + "/" + pickup.name + ": crystal is outside the playable lanes.");
                        Collider pickupCollider = pickup.GetComponent<Collider>();
                        if (pickupCollider == null) pickupCollider = pickup.GetComponentInChildren<Collider>(true);
                        if (pickupCollider == null || !pickupCollider.enabled || !pickupCollider.isTrigger)
                            throw new InvalidOperationException(set[sectionIndex] + "/" + pickup.name + ": collectible trigger is missing.");
                        foreach (RunnerLaneHazard hazard in contents.GetComponentsInChildren<RunnerLaneHazard>(true))
                            if (pickupCollider.bounds.Intersects(hazard.GetComponent<Collider>().bounds))
                                throw new InvalidOperationException(set[sectionIndex] + "/" + pickup.name + ": collectible overlaps an obstacle.");
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            rows.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                float gap = rows[rowIndex].Key - rows[rowIndex - 1].Key;
                if (gap < 11.99f)
                {
                    HashSet<int> sharedSafeLanes = new HashSet<int>(rows[rowIndex - 1].Value);
                    sharedSafeLanes.IntersectWith(rows[rowIndex].Value);
                    if (sharedSafeLanes.Count == 0)
                        throw new InvalidOperationException("Forced lane change has less than 1.5 seconds reaction time in " + set[0] + " (gap " + gap + "m).");
                }
            }
        }
        keyIds.Sort();
        if (keyIds.Count != 2 || keyIds[0] != 0 || keyIds[1] != 1)
            throw new InvalidOperationException("The two existing unique stage keys did not validate.");
    }

    private static bool setIndexIsLegacy(string path) { return path.Contains("FE_") || path.Contains("CC_"); }
    private static void BuildBiomeSet(string[] paths, RunnerBiome biome, Material floorMaterial, Material obstacleMaterial,
        Material crystalMaterial, GameObject keyPrefab, GameObject crystalPrefab, int keyId, bool brokenBridge)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject root = NewSectionRoot(System.IO.Path.GetFileNameWithoutExtension(paths[i]), biome, i);
            CreateCube(root.transform, "TRACK FLOOR", new Vector3(0f, -0.25f, Length * 0.5f), new Vector3(9f, 0.5f, Length), floorMaterial, true);
            CreateDecor(root.transform, biome, i, floorMaterial, obstacleMaterial);
            AddSafeObstaclePattern(root.transform, biome, i, obstacleMaterial, brokenBridge);
            AddCrystalTrail(root.transform, crystalPrefab, i);
            if (keyId >= 0 && i == 0) AddKey(root.transform, keyPrefab, keyId, new Vector3(0f, 1.45f, 4.5f));
            SavePrefab(root, paths[i]);
        }
    }

    private static void BuildTransition(string path, string name, RunnerBiome biome, Material from, Material to)
    {
        GameObject root = NewSectionRoot(name, biome, -1);
        CreateCube(root.transform, "TRACK FLOOR", new Vector3(0f, -0.25f, Length * 0.5f), new Vector3(9f, 0.5f, Length), from, true);
        CreateCube(root.transform, "TRANSITION ARCH LEFT", new Vector3(-3.5f, 2f, 10f), new Vector3(0.6f, 4f, 0.8f), to, false);
        CreateCube(root.transform, "TRANSITION ARCH RIGHT", new Vector3(3.5f, 2f, 10f), new Vector3(0.6f, 4f, 0.8f), to, false);
        CreateCube(root.transform, "TRANSITION ARCH TOP", new Vector3(0f, 3.85f, 10f), new Vector3(7.4f, 0.5f, 0.8f), to, false);
        SavePrefab(root, path);
    }

    private static void AddSafeObstaclePattern(Transform root, RunnerBiome biome, int sectionIndex, Material material, bool brokenBridge)
    {
        // One hazard per row instead of two. Each row leaves two open lanes.
        // The blocked lane repeats across section seams so a safe lane remains available.
        int[][] blockedLanePatterns =
        {
            new[] { -1, 0 },
            new[] { 0, 1 },
            new[] { 1, -1 }
        };
        int[] lanes = blockedLanePatterns[sectionIndex % blockedLanePatterns.Length];
        for (int row = 0; row < 2; row++)
        {
            int lane = lanes[row];
            bool slideBar = biome == RunnerBiome.LostTemple && row == 0;
            bool jumpBarrier = biome == RunnerBiome.AncientRuins && sectionIndex == 2 && row == 1;
            float z = row == 0 ? 4f : 16f;
            MakeHazard(root, lane, z, material, slideBar, jumpBarrier);
        }
        if (brokenBridge && sectionIndex == 1)
        {
            CreateCube(root, "BROKEN BRIDGE LEFT EDGE", new Vector3(-3.25f, -0.4f, 10f), new Vector3(1.5f, 0.25f, 3f), material, false);
            CreateCube(root, "BROKEN BRIDGE RIGHT EDGE", new Vector3(3.25f, -0.4f, 10f), new Vector3(1.5f, 0.25f, 3f), material, false);
        }
    }
    private static void MakeHazard(Transform parent, int lane, float z, Material material, bool slideBar, bool jumpBarrier)
    {
        GameObject hazard;
        if (slideBar)
            hazard = CreateCube(parent, "SLIDE UNDER TEMPLE BAR", new Vector3(lane * Lane, 2.45f, z), new Vector3(1.8f, 0.35f, 1.2f), material, true);
        else if (jumpBarrier)
            hazard = CreateCube(parent, "JUMP OVER RUINS BARRIER", new Vector3(lane * Lane, 0.38f, z), new Vector3(1.8f, 0.76f, 1.2f), material, true);
        else
            hazard = CreateCube(parent, "RUNNER ROCK OBSTACLE", new Vector3(lane * Lane, 0.65f, z), new Vector3(1.25f, 1.3f, 1.2f), material, true);
        hazard.AddComponent<Obstacle>();
        RunnerLaneHazard laneHazard = hazard.AddComponent<RunnerLaneHazard>();
        laneHazard.laneIndex = lane;
        laneHazard.patternRow = Mathf.RoundToInt(z / 6f);
        laneHazard.action = slideBar ? RunnerHazardAction.Slide : jumpBarrier ? RunnerHazardAction.Jump : RunnerHazardAction.Dodge;
    }

    private static void CreateDecor(Transform root, RunnerBiome biome, int index, Material material, Material accent)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 4f;
            if (biome == RunnerBiome.AncientRuins)
            {
                CreateCube(root, "RUINS PILLAR", new Vector3(x, 1.4f, 4f + index * 4f), new Vector3(0.65f, 2.8f, 0.8f), material, false);
                if (index == 1) CreateCube(root, "BROKEN RUINS ARCH", new Vector3(x * 0.72f, 2.8f, 15f), new Vector3(0.7f, 2f, 0.75f), accent, false);
            }
            else if (biome == RunnerBiome.LostTemple)
            {
                CreateCube(root, "TEMPLE GOLD PILLAR", new Vector3(x, 1.8f, 4f + index * 4f), new Vector3(0.8f, 3.6f, 0.9f), material, false);
                CreateCube(root, "TEMPLE TORCH", new Vector3(x * 0.82f, 2.4f, 12f), new Vector3(0.35f, 0.8f, 0.35f), accent, false);
            }
            else
            {
                CreateCube(root, "VAULT TREASURE PILLAR", new Vector3(x, 2f, 4f + index * 4f), new Vector3(0.85f, 4f, 1f), material, false);
                CreateCube(root, "VAULT GOLD TRIM", new Vector3(x * 0.72f, 2.8f, 15f), new Vector3(0.6f, 0.4f, 0.6f), accent, false);
            }
        }
    }

    private static void AddCrystalTrail(Transform root, GameObject prefab, int sectionIndex)
    {
        // Keep each trail in a lane that remains open across both obstacle rows.
        // These lanes correspond to the blocked-lane pattern in AddSafeObstaclePattern.
        int[] safeLane = { 1, -1, 0 };
        int lane = safeLane[sectionIndex % safeLane.Length];
        for (int i = 0; i < 3; i++)
        {
            GameObject crystal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            crystal.name = "Blue crystal trail " + (i + 1);
            crystal.transform.SetParent(root, false);
            crystal.transform.localPosition = new Vector3(lane * Lane, 1.35f, 8f + i * 2f);
            crystal.transform.localRotation = Quaternion.identity;
        }
    }
    private static GameObject BuildKeyPrefab(Material gold)
    {
        GameObject root = new GameObject("Runner_TreasureKey");
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.8f;
        GameObject visual = new GameObject("Golden Key Visual");
        visual.transform.SetParent(root.transform, false);
        GameObject shaft = CreatePrimitive(PrimitiveType.Cylinder, visual.transform, "Key Shaft", new Vector3(0f, 0f, 0f), new Vector3(0.11f, 0.11f, 0.55f), gold, false);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject ring = CreatePrimitive(PrimitiveType.Sphere, visual.transform, "Key Bow", new Vector3(0f, 0f, -0.46f), new Vector3(0.42f, 0.42f, 0.16f), gold, false);
        GameObject tooth = CreatePrimitive(PrimitiveType.Cube, visual.transform, "Key Tooth", new Vector3(0.16f, 0f, 0.3f), new Vector3(0.2f, 0.12f, 0.16f), gold, false);
        SphereCollider ringCollider = ring.GetComponent<SphereCollider>(); if (ringCollider != null) UnityEngine.Object.DestroyImmediate(ringCollider);
        BoxCollider toothCollider = tooth.GetComponent<BoxCollider>(); if (toothCollider != null) UnityEngine.Object.DestroyImmediate(toothCollider);
        TreasureKey key = root.AddComponent<TreasureKey>();
        key.visual = visual.transform;
        ParticleSystem particles = new GameObject("Key Collection Sparkles").AddComponent<ParticleSystem>();
        particles.transform.SetParent(root.transform, false);
        var main = particles.main; main.loop = false; main.duration = 0.6f; main.startLifetime = 0.8f; main.startSpeed = 2f; main.startSize = 0.16f;
        var emission = particles.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
        particles.gameObject.SetActive(true);
        key.collectSparkles = particles;
        string temp = "Assets/Prefabs/Runner/Pickups/Runner_TreasureKey.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, temp);
        UnityEngine.Object.DestroyImmediate(root);
        return saved;
    }

    private static void AddKeyToExistingPrefab(string path, GameObject keyPrefab, int keyId)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            TreasureKey[] keys = contents.GetComponentsInChildren<TreasureKey>(true);
            if (keys.Length > 1) throw new InvalidOperationException(path + " already contains more than one key.");
            if (keys.Length == 0) AddKey(contents.transform, keyPrefab, keyId, new Vector3(0f, 1.45f, 4.5f));
            else keys[0].keyId = keyId;
            if (contents.GetComponentsInChildren<TreasureKey>(true).Length != 1) throw new InvalidOperationException(path + " must contain exactly one key.");
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    private static void AddKey(Transform parent, GameObject prefab, int keyId, Vector3 localPosition)
    {
        GameObject key = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        key.transform.SetParent(parent, false);
        key.transform.localPosition = localPosition;
        key.GetComponent<TreasureKey>().keyId = keyId;
    }

    private static GameObject NewSectionRoot(string name, RunnerBiome biome, int slot)
    {
        GameObject root = new GameObject(name);
        RunnerTrackSection section = root.AddComponent<RunnerTrackSection>();
        section.biome = biome; section.sectionLength = Length; section.sequenceSlot = slot;
        section.entryAnchor = new GameObject("ENTRY").transform; section.entryAnchor.SetParent(root.transform, false);
        section.exitAnchor = new GameObject("EXIT").transform; section.exitAnchor.SetParent(root.transform, false); section.exitAnchor.localPosition = new Vector3(0f, 0f, Length);
        return root;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>(); if (renderer != null && material != null) renderer.sharedMaterial = material;
        if (!collider) { Collider c = obj.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c); }
        return obj;
    }

    private static GameObject CreatePrimitive(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = scale;
        Renderer r = obj.GetComponent<Renderer>(); if (r != null && material != null) r.sharedMaterial = material;
        if (!collider) { Collider c = obj.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c); }
        return obj;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new InvalidOperationException("Failed to save " + path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ConfigureScene(Scene scene)
    {
        RunnerTrackManager track = UnityEngine.Object.FindFirstObjectByType<RunnerTrackManager>();
        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (track == null || manager == null) throw new InvalidOperationException("LostCave_Main is missing its runner or GameManager.");

        RunnerTrackSection oldSlotSeven = null;
        foreach (RunnerTrackSection section in track.sectionsContainer.GetComponentsInChildren<RunnerTrackSection>(true))
            if (section.sequenceSlot == 7) oldSlotSeven = section;
        if (oldSlotSeven == null) throw new InvalidOperationException("Preview section slot 7 is missing.");
        Vector3 oldPosition = oldSlotSeven.transform.position;
        UnityEngine.Object.DestroyImmediate(oldSlotSeven.gameObject);
        RunnerTrackSection newTransition = (RunnerTrackSection)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(CavernToRuins), track.sectionsContainer);
        newTransition.transform.SetPositionAndRotation(oldPosition, Quaternion.identity); newTransition.sequenceSlot = 7;

        SerializedObject so = new SerializedObject(track);
        SetArray(so, "ancientRuinsSections", RuinsPaths);
        SetArray(so, "lostTempleSections", TemplePaths);
        SetArray(so, "finalTreasureVaultSections", VaultPaths);
        SetObject(so, "transitionToAncientRuins", AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(CavernToRuins));
        SetObject(so, "transitionToLostTemple", AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(RuinsToTemple));
        SetObject(so, "transitionToFinalTreasureVault", AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(TempleToVault));
        so.FindProperty("nextStartZ").floatValue = 160f; so.FindProperty("nextSequenceSlot").intValue = 8; so.ApplyModifiedPropertiesWithoutUndo();

        ConfigureFinalChest();
        ConfigureHud(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save LostCave_Main.");
    }

    private static void ConfigureFinalChest()
    {
        GameObject body = GameObject.Find("TreasureChest_Body");
        GameObject lid = GameObject.Find("TreasureChest_Lid");
        if (body == null || lid == null) throw new InvalidOperationException("Existing chest body/lid are missing.");
        GameObject root = GameObject.Find("FINAL VAULT TREASURE CHEST");
        if (root == null) root = new GameObject("FINAL VAULT TREASURE CHEST");
        root.transform.SetPositionAndRotation(new Vector3(0f, 0f, 60f), Quaternion.identity);
        body.transform.SetParent(root.transform, true); lid.transform.SetParent(root.transform, true);
        root.transform.position = new Vector3(0f, 0f, 378f);
        Collider[] oldColliders = root.GetComponentsInChildren<Collider>(true); foreach (Collider c in oldColliders) c.enabled = false;
        BoxCollider trigger = root.GetComponent<BoxCollider>(); if (trigger == null) trigger = root.AddComponent<BoxCollider>();
        trigger.enabled = true; trigger.isTrigger = true; trigger.center = new Vector3(0f, 1f, 0f); trigger.size = new Vector3(3.2f, 2.8f, 2.5f);
        TreasureChest chest = root.GetComponent<TreasureChest>(); if (chest == null) chest = root.AddComponent<TreasureChest>();
        chest.lid = lid.transform;
        Transform lightTransform = root.transform.Find("TREASURE GOLD LIGHT");
        if (lightTransform == null) { GameObject lightObject = new GameObject("TREASURE GOLD LIGHT"); lightObject.transform.SetParent(root.transform, false); lightTransform = lightObject.transform; }
        lightTransform.localPosition = new Vector3(0f, 2f, 0f);
        Light glow = lightTransform.GetComponent<Light>(); if (glow == null) glow = lightTransform.gameObject.AddComponent<Light>();
        glow.type = LightType.Point; glow.color = new Color(1f, 0.68f, 0.18f); glow.intensity = 6f; glow.range = 9f; glow.shadows = LightShadows.None; glow.enabled = false;
        Transform sparkleTransform = root.transform.Find("TREASURE OPENING SPARKLES");
        if (sparkleTransform == null) { GameObject sparkleObject = new GameObject("TREASURE OPENING SPARKLES"); sparkleObject.transform.SetParent(root.transform, false); sparkleTransform = sparkleObject.transform; }
        sparkleTransform.localPosition = new Vector3(0f, 1.4f, 0f);
        ParticleSystem sparkles = sparkleTransform.GetComponent<ParticleSystem>(); if (sparkles == null) sparkles = sparkleTransform.gameObject.AddComponent<ParticleSystem>();
        var main = sparkles.main; main.loop = false; main.duration = 1.6f; main.startLifetime = 1.1f; main.startSpeed = 2.5f; main.startSize = 0.2f; main.startColor = new Color(1f, 0.72f, 0.15f, 1f);
        var emission = sparkles.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 64) });
        var shape = sparkles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.65f;
        sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        chest.openingSparkles = sparkles; chest.treasureLight = glow;
    }

    private static void ConfigureHud(GameManager manager)
    {
        TMP_Text score = FindTextAnywhere("ScoreText");
        TMP_Text health = FindTextAnywhere("HealthText");
        if (score == null || health == null) throw new InvalidOperationException("Existing ScoreText or HealthText is missing.");
        Canvas hudCanvas = score.GetComponentInParent<Canvas>();
        Canvas overlayCanvas = manager.gameOverPanel != null ? manager.gameOverPanel.GetComponentInParent<Canvas>() : hudCanvas;
        if (hudCanvas == null || overlayCanvas == null) throw new InvalidOperationException("Existing HUD Canvas is missing.");
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        TMP_Text keyText = EnsureText(hudCanvas.transform, "TreasureKeyProgress", new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(440f, 42f), 26f, font);
        TMP_Text stageText = EnsureText(hudCanvas.transform, "AdventureStageProgress", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 32f), 20f, font);
        GameObject victory = EnsurePanel(overlayCanvas.transform, "VictoryPanel", "TREASURE FOUND!", "FinalRunStats", "RESTART ADVENTURE", manager, font);
        GameObject locked = EnsurePanel(overlayCanvas.transform, "LockedTreasurePanel", "TREASURE STILL SEALED", "LockedTreasureMessage", "RESTART ADVENTURE", manager, font);
        TMP_Text finalStats = FindText(victory.transform, "FinalRunStats");
        TMP_Text lockedMessage = FindText(locked.transform, "LockedTreasureMessage");
        SerializedObject so = new SerializedObject(manager);
        SetObject(so, "scoreText", score); SetObject(so, "healthText", health); SetObject(so, "treasureKeysText", keyText); SetObject(so, "stageText", stageText);
        SetObject(so, "winPanel", victory); SetObject(so, "lockedTreasurePanel", locked); SetObject(so, "finalStatsText", finalStats); SetObject(so, "lockedTreasureText", lockedMessage);
        so.FindProperty("requiredTreasureKeys").intValue = 2; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsurePanel(Transform parent, string name, string title, string detailName, string buttonLabel, GameManager manager, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        GameObject panel = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null) panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(700f, 380f);
        Image background = panel.GetComponent<Image>(); background.color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
        EnsureChildText(panel.transform, "PanelTitle", title, new Vector2(0f, 95f), new Vector2(620f, 75f), 42f, font);
        EnsureChildText(panel.transform, detailName, "", new Vector2(0f, 5f), new Vector2(620f, 100f), 26f, font);
        Transform buttonTr = panel.transform.Find("RestartAdventureButton");
        GameObject buttonObj = buttonTr != null ? buttonTr.gameObject : new GameObject("RestartAdventureButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (buttonTr == null) buttonObj.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>(); buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f); buttonRect.anchoredPosition = new Vector2(0f, 48f); buttonRect.sizeDelta = new Vector2(300f, 60f);
        buttonObj.GetComponent<Image>().color = new Color(0.68f, 0.4f, 0.08f, 1f);
        Button button = buttonObj.GetComponent<Button>(); button.onClick.RemoveAllListeners();
        while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, manager.RestartGame);
        EnsureChildText(buttonObj.transform, "ButtonText", buttonLabel, Vector2.zero, new Vector2(300f, 60f), 24f, font);
        panel.SetActive(false); return panel;
    }

    private static TMP_Text EnsureText(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
    {
        TMP_Text text = FindText(parent, name);
        if (text == null) text = EnsureChildText(parent, name, "", position, size, fontSize, font);
        RectTransform rect = text.rectTransform; rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
        text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; return text;
    }

    private static TMP_Text EnsureChildText(Transform parent, string name, string initial, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
    {
        Transform tr = parent.Find(name);
        GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (tr == null) go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>(); text.text = initial; text.font = font; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        return text;
    }

    private static TMP_Text FindText(Transform parent, string name)
    {
        Transform tr = parent.Find(name); return tr != null ? tr.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text FindTextAnywhere(string name)
    {
        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.gameObject.name == name) return text;
        return null;
    }

    private static void SetArray(SerializedObject so, string propertyName, string[] paths)
    {
        SerializedProperty p = so.FindProperty(propertyName); p.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(paths[i]);
    }

    private static void SetObject(SerializedObject so, string propertyName, UnityEngine.Object value)
    { SerializedProperty p = so.FindProperty(propertyName); if (p != null) p.objectReferenceValue = value; }

    private static Material LoadMaterial(string path)
    { Material material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material == null) throw new InvalidOperationException("Required existing material missing: " + path); return material; }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Runner/Sections")) throw new InvalidOperationException("Existing runner Sections folder missing.");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Runner/Pickups")) throw new InvalidOperationException("Existing runner Pickups folder missing.");
    }
}




