#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Safely assembles the existing 15 environment sections and 4 transitions
/// into the approved 3-level / 5-region adventure world.
/// Menu: Tools > Adventure World > Build All Levels
/// </summary>
public static class AdventureWorldBuilder
{
    private const string SectionsPath = "Assets/Prefabs/Runner/Sections";
    private const string ExpectedScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const float SectionDepth = 20f;

    private struct Entry
    {
        public string prefab;
        public float yRotation;
        public int region;
        public bool transition;

        public Entry(string prefab, float yRotation, int region, bool transition = false)
        {
            this.prefab = prefab;
            this.yRotation = yRotation;
            this.region = region;
            this.transition = transition;
        }
    }

    private static readonly Entry[] Entries =
    {
        // LEVEL 1 — THE FORGOTTEN DEPTHS
        new Entry("FE_01_ForestGate",       0f,   1),
        new Entry("FE_02_CaveRun",          0f,   1),
        new Entry("FE_03_EntranceHazards",  0f,   1),
        new Entry("FE_CC_Transition",       0f,   1, true),
        new Entry("CC_01_CrystalRun",       0f,   2),
        new Entry("CC_02_WaterfallRun",     0f,   2),
        new Entry("CC_03_GemCrossing",      0f,   2),

        // LEVEL 2 — THE ANCIENT MYSTERY
        new Entry("CC_AR_Transition",      0f,   3, true),
        new Entry("AR_01_StoneCauseway",   90f,   3),
        new Entry("AR_02_BrokenBridge",    90f,   3),
        new Entry("AR_03_ArchiveRun",      90f,   3),
        new Entry("AR_LT_Transition",      90f,   4, true),
        new Entry("LT_01_GoldenGate",     180f,   4),
        new Entry("LT_02_TrapHall",       180f,   4),
        new Entry("LT_03_SunHall",        180f,   4),

        // LEVEL 3 — THE LEGENDARY TREASURE
        new Entry("LT_FV_Transition",     180f,   5, true),
        new Entry("FV_01_VaultApproach",  270f,   5),
        new Entry("FV_02_TreasureRun",    270f,   5),
        new Entry("FV_03_FinalPlatform",  270f,   5)
    };

    [MenuItem("Tools/Adventure World/Build All Levels")]
    public static void BuildAllLevels()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ExpectedScenePath)
        {
            EditorUtility.DisplayDialog(
                "Wrong Scene",
                "Open Assets/Scenes/LostCave_Main.unity before building.",
                "OK");
            return;
        }

        // Never replace an existing build automatically.
        if (GameObject.Find("ADVENTURE WORLD") != null)
        {
            EditorUtility.DisplayDialog(
                "Adventure World Already Exists",
                "An ADVENTURE WORLD root already exists. Nothing was changed.\n\n" +
                "Inspect or back it up before deciding what to do next.",
                "OK");
            return;
        }

        // Validate every prefab before making any scene changes.
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        List<string> missing = new List<string>();

        foreach (Entry entry in Entries)
        {
            if (prefabs.ContainsKey(entry.prefab))
                continue;

            string path = SectionsPath + "/" + entry.prefab + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                missing.Add(path);
            else
                prefabs.Add(entry.prefab, prefab);
        }

        if (missing.Count > 0)
        {
            Debug.LogError("[AdventureWorldBuilder] Build cancelled. Missing prefabs:\n" +
                           string.Join("\n", missing));
            EditorUtility.DisplayDialog(
                "Missing Environment Prefabs",
                "Build cancelled before changing the scene.\n\nMissing:\n" +
                string.Join("\n", missing),
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Build Lost Cave Environments",
            "This places 15 existing region prefabs and 4 transitions.\n\n" +
            "It will NOT delete existing scene objects or overwrite an existing " +
            "ADVENTURE WORLD.\n\n" +
            "Make sure you have backed up LostCave_Main.unity before continuing.\n\n" +
            "Build all 3 levels now?",
            "Build", "Cancel");

        if (!confirmed)
            return;

        GameObject worldRoot = null;

        try
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Lost Cave Adventure World");

            worldRoot = new GameObject("ADVENTURE WORLD");
            Undo.RegisterCreatedObjectUndo(worldRoot, "Create Adventure World");

            GameObject[] levelRoots = new GameObject[4];
            levelRoots[1] = CreateChild(worldRoot.transform, "Level1_ForgottenDepths");
            levelRoots[2] = CreateChild(worldRoot.transform, "Level2_AncientMystery");
            levelRoots[3] = CreateChild(worldRoot.transform, "Level3_TreasureVault");

            Vector3 cursor = new Vector3(0f, 0f, 10f);
            Vector3 direction = Vector3.forward;
            Dictionary<int, Vector3> regionEntries = new Dictionary<int, Vector3>();

            foreach (Entry entry in Entries)
            {
                int level = entry.region <= 2 ? 1 : (entry.region <= 4 ? 2 : 3);
                Quaternion rotation = Quaternion.Euler(0f, entry.yRotation, 0f);

                GameObject instance = PrefabUtility.InstantiatePrefab(
                    prefabs[entry.prefab], levelRoots[level].transform) as GameObject;

                if (instance == null)
                    throw new InvalidOperationException("Could not instantiate " + entry.prefab);

                Undo.RegisterCreatedObjectUndo(instance, "Place " + entry.prefab);
                instance.name = entry.prefab;
                instance.transform.SetPositionAndRotation(cursor, rotation);

                if (!regionEntries.ContainsKey(entry.region))
                    regionEntries.Add(entry.region, cursor);

                direction = rotation * Vector3.forward;
                cursor += direction * (entry.transition ? SectionDepth * 0.5f : SectionDepth);

                Debug.Log("[AdventureWorldBuilder] Placed " + entry.prefab +
                          " at " + instance.transform.position);
            }

            string[] triggerNames =
            {
                "", "",
                "CrystalCaverns_Entry",
                "AncientRuins_Entry",
                "LostTemple_Entry",
                "TreasureVault_Entry"
            };

            for (int region = 2; region <= 5; region++)
            {
                if (!regionEntries.TryGetValue(region, out Vector3 entryPosition))
                    throw new InvalidOperationException("Missing entry position for region " + region);

                GameObject triggerObject = new GameObject(triggerNames[region]);
                Undo.RegisterCreatedObjectUndo(triggerObject, "Create Region Trigger");
                triggerObject.transform.SetParent(worldRoot.transform);
                triggerObject.transform.position = entryPosition + Vector3.up * 1.5f;

                BoxCollider box = triggerObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(10f, 4f, 2f);

                RegionTrigger trigger = triggerObject.AddComponent<RegionTrigger>();
                trigger.targetRegion = (AdventureLevelManager.Region)region;
            }

            // Reuse an existing manager; never destroy it.
            GameObject managerObject = GameObject.Find("AdventureManager");
            if (managerObject == null)
            {
                managerObject = new GameObject("AdventureManager");
                Undo.RegisterCreatedObjectUndo(managerObject, "Create Adventure Manager");
            }

            if (managerObject.GetComponent<AdventureLevelManager>() == null)
                Undo.AddComponent<AdventureLevelManager>(managerObject);

            // Disable the old dynamic track component only after successful placement.
            // The component and its GameObject remain in the scene.
            RunnerTrackManager[] oldManagers =
                UnityEngine.Object.FindObjectsByType<RunnerTrackManager>(FindObjectsSortMode.None);

            foreach (RunnerTrackManager oldManager in oldManagers)
            {
                Undo.RecordObject(oldManager, "Disable Runner Track Manager");
                oldManager.enabled = false;
                EditorUtility.SetDirty(oldManager);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[AdventureWorldBuilder] Placed " + Entries.Length +
                      " prefabs and 4 region triggers. Scene saved: " + saved);

            EditorUtility.DisplayDialog(
                "Environment Build Finished",
                "Placed all 15 region prefabs and 4 transitions.\n" +
                "Created region triggers and ensured AdventureManager exists.\n\n" +
                "Scene save: " + (saved ? "Success" : "Not saved") +
                "\n\nInspect the scene and test the connections in Play Mode. " +
                "The builder cannot certify perfect doorway alignment automatically.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            // Remove only the new world root created by this run.
            if (worldRoot != null)
                Undo.DestroyObjectImmediate(worldRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog(
                "Environment Build Failed",
                "Build stopped:\n\n" + exception.Message +
                "\n\nThe newly created ADVENTURE WORLD root was removed. " +
                "Pre-existing scene objects were not intentionally deleted. " +
                "Check the Console.",
                "OK");
        }
    }

    private static GameObject CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
        child.transform.SetParent(parent, false);
        return child;
    }
}
#endif
