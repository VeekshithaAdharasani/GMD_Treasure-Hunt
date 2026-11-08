#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuditFixPassSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string BackupPath = "Backups/FixPass_20261107_144337_736";
    private const float Lane = 3.5f;

    [MenuItem("Tools/Lost Cave/Apply Audited Fix Pass")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Fix pass requires Edit Mode.");
        if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, BackupPath)))
            throw new InvalidOperationException("Verified fix-pass backup is missing; refusing to modify the scene.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameManager game = Single<GameManager>(scene, "GameManager");
        RunnerTrackManager track = Single<RunnerTrackManager>(scene, "RunnerTrackManager");
        PlayerMovement player = Single<PlayerMovement>(scene, "PlayerMovement");
        CameraFollow cameraFollow = Single<CameraFollow>(scene, "CameraFollow");
        GameObject pickupRoot = FindObject(scene, "RUNNER_GAMEPLAY_PICKUPS");
        GameObject objectiveRoot = FindObject(scene, "RUNNER_FINAL_OBJECTIVE");
        GameObject chestObject = FindObject(scene, "FINAL VAULT TREASURE CHEST");
        TreasureChest chest = chestObject != null ? chestObject.GetComponent<TreasureChest>() : null;
        Transform sectionRoot = track.sectionsContainer;
        RunnerTrackSection[] allSections = sectionRoot != null ? sectionRoot.GetComponentsInChildren<RunnerTrackSection>(true) : Array.Empty<RunnerTrackSection>();

        // Complete preflight: no scene mutation occurs before every requirement below passes.
        Require(scene.name == "LostCave_Main", "Unexpected active scene.");
        Require(track.enabled && sectionRoot != null && FindObject(scene, "RUNNER_TRACK_RUNTIME") == sectionRoot.gameObject, "Runner section container is missing or mismatched.");
        Require(track.forgottenEntranceSections != null && track.forgottenEntranceSections.Length >= 3 && track.crystalCavernsSections != null && track.crystalCavernsSections.Length >= 3 && track.finalTreasureVaultSections != null && track.finalTreasureVaultSections.Length >= 3 && track.transitionToCrystalCaverns != null && track.transitionToFinalTreasureVault != null, "A required stage section catalog or transition is missing.");
        Require(player.GetComponent<CharacterController>() != null && Math.Abs(player.transform.position.x) < 0.01f && Math.Abs(player.autoRunSpeed - 8f) < 0.01f, "Player start or controller settings differ from the approved runner setup.");
        Require(cameraFollow.target == player.transform, "Existing camera target is not the player.");
        Require(game.startingHealth == 3 && game.scoreText != null && game.healthText != null && game.treasureKeysText != null && game.stageText != null && game.stageProgressSlider != null && game.winPanel != null && game.gameOverPanel != null, "Required gameplay HUD or state panels are missing.");
        Require(pickupRoot != null && objectiveRoot != null && chestObject != null && chest != null && chest.transform.IsChildOf(objectiveRoot.transform), "Pickup root, final chest, or final objective parent is missing.");
        Require(chest.transform.parent != null && chest.transform.parent.name == "RUNNER_FINAL_OBJECTIVE", "Chest is not under the verified final-objective root.");
        Collider chestTrigger = chest.GetComponent<Collider>();
        Require(chest.lid != null && chest.openingSparkles != null && chest.treasureLight != null && chestTrigger != null && chestTrigger.enabled && chestTrigger.isTrigger, "Final chest references or trigger collider are invalid.");
        Require(Math.Abs(chest.transform.position.z - 495f) < 0.01f || Math.Abs(chest.transform.position.z - 500f) < 0.01f, "Chest must be at its measured original world Z=495 or the idempotent route-end Z=500.");

        RunnerTrackSection[] startup = new RunnerTrackSection[4];
        string[] expectedPrefabEnds = { "FE_01_ForestGate.prefab", "FE_02_CaveRun.prefab", "FE_03_EntranceHazards.prefab", "FE_01_ForestGate.prefab" };
        for (int slot = 0; slot < 3; slot++)
            startup[slot] = allSections.SingleOrDefault(s => s.sequenceSlot == slot && Math.Abs(s.StartZ - slot * 20f) < 0.05f);
        startup[3] = allSections.SingleOrDefault(s => s.sequenceSlot == 3 && s.name == "FINAL_ROUTE_PREVIEW_SLOT_3" && Math.Abs(s.StartZ - 60f) < 0.05f);
        for (int slot = 0; slot < startup.Length; slot++)
        {
            Require(startup[slot] != null, "Missing correct startup section for slot " + slot + ".");
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(startup[slot].gameObject);
            Require(prefabPath.EndsWith(expectedPrefabEnds[slot], StringComparison.Ordinal), "Wrong prefab in startup slot " + slot + ": " + prefabPath);
            Require(startup[slot].Validate(3.5f, out string geometryError), "Invalid startup section slot " + slot + ": " + geometryError);
            if (slot > 0) Require(Math.Abs(startup[slot - 1].EndZ - startup[slot].StartZ) < 0.01f, "Startup section seam is not connected at slot " + slot + ".");
        }
        TreasureKey[] namedKeys = { FindKey(scene, "GoldenKey_ID_0"), FindKey(scene, "GoldenKey_ID_1"), FindKey(scene, "GoldenKey_ID_2"), FindKey(scene, "GoldenKey_ID_3") };
        for (int i = 0; i < namedKeys.Length; i++)
        {
            Require(namedKeys[i] != null && namedKeys[i].keyId == i, "Missing or incorrectly assigned named key ID " + i + ".");
            Collider keyTrigger = namedKeys[i].GetComponent<Collider>();
            Require(keyTrigger != null && keyTrigger.enabled && keyTrigger.isTrigger && namedKeys[i].visual != null && namedKeys[i].visual.GetComponentInChildren<Renderer>(true) != null, "Key " + i + " visual or trigger is invalid.");
        }
        TreasureKey[] duplicateRootKeys = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true))
            .Where(k => k.transform.parent == null &&
                ((k.name == "TreasureKey_0" && k.keyId == 0 && Vector3.Distance(k.transform.position, new Vector3(0f, 1.2f, 60f)) < 0.01f) ||
                 (k.name == "TreasureKey_1" && k.keyId == 1 && Vector3.Distance(k.transform.position, new Vector3(0f, 1.2f, 200f)) < 0.01f))).ToArray();
        Require(duplicateRootKeys.Length == 4 &&
                duplicateRootKeys.Count(k => k.name == "TreasureKey_0" && k.keyId == 0) == 2 &&
                duplicateRootKeys.Count(k => k.name == "TreasureKey_1" && k.keyId == 1) == 2 &&
                duplicateRootKeys.All(k =>
                {
                    Collider c = k.GetComponent<Collider>();
                    return c != null && c.enabled && c.isTrigger && k.visual != null &&
                           k.visual.GetComponentInChildren<Renderer>(true) != null;
                }), "Expected exactly two verified duplicate root key-ID-0 previews at Z=60 and two key-ID-1 previews at Z=200.");
        GameObject bluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Runner/Pickups/Runner_Crystal_Blue.prefab");
        Material purpleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VisualPolish/RunnerFinalImprovements/Pickup_MultiplierPurple.mat");
        Require(bluePrefab != null && bluePrefab.GetComponent<Collectible>() != null && bluePrefab.GetComponent<Collider>() is { enabled: true, isTrigger: true } && bluePrefab.GetComponentInChildren<Renderer>(true) != null && purpleMaterial != null, "Existing crystal prefab or reusable purple material is missing.");
        Require(EditorBuildSettings.scenes.Any(s => s.path == ScenePath), "LostCave_Main is absent from Build Settings.");
        Transform[] existingPurples = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "PurpleCrystal_CC_Left" || t.name == "PurpleCrystal_CC_Right").ToArray();
        Require(existingPurples.GroupBy(t => t.name).All(g => g.Count() == 1), "Duplicate purple crystal instances already exist.");

        // Apply the audited corrections only after the complete preflight above.
        foreach (RunnerTrackSection section in allSections) section.gameObject.SetActive(startup.Contains(section));
        for (int slot = 0; slot < startup.Length; slot++) startup[slot].sequenceSlot = slot;
        foreach (RunnerTrackSection section in startup)
            foreach (TreasureKey embedded in section.GetComponentsInChildren<TreasureKey>(true)) embedded.gameObject.SetActive(false);

        foreach (TreasureKey duplicateRootKey in duplicateRootKeys) duplicateRootKey.gameObject.SetActive(false);
        namedKeys[0].keyId = 0;
        namedKeys[0].transform.position = new Vector3(0f, 1f, 70f);
        namedKeys[0].gameObject.SetActive(true);
        namedKeys[1].keyId = 1;
        namedKeys[1].transform.position = new Vector3(0f, 1f, 210f);
        namedKeys[1].gameObject.SetActive(true);
        namedKeys[2].gameObject.SetActive(false);
        namedKeys[3].gameObject.SetActive(false);

        Vector3[] purplePositions = { new Vector3(-3.5f, 1f, 210f), new Vector3(3.5f, 1f, 210f) };
        string[] purpleNames = { "PurpleCrystal_CC_Left", "PurpleCrystal_CC_Right" };
        for (int i = 0; i < purpleNames.Length; i++)
        {
            GameObject purpleObject = FindObject(scene, purpleNames[i]);
            if (purpleObject == null)
            {
                purpleObject = PrefabUtility.InstantiatePrefab(bluePrefab, scene) as GameObject;
                if (purpleObject == null) throw new InvalidOperationException("Could not instantiate existing blue crystal pickup for purple variant.");
                purpleObject.transform.SetParent(pickupRoot.transform, false);
            }
            purpleObject.name = purpleNames[i];
            purpleObject.transform.position = purplePositions[i];
            Collectible collectible = purpleObject.GetComponent<Collectible>();
            Collider pickupTrigger = purpleObject.GetComponent<Collider>();
            if (collectible == null || pickupTrigger == null || !pickupTrigger.isTrigger) throw new InvalidOperationException("Purple crystal clone lacks the existing collectible or trigger component.");
            collectible.treasureType = Collectible.TreasureType.BlueCrystal;
            collectible.powerUpType = Collectible.PowerUpType.None;
            collectible.purpleCrystalVariant = true;
            foreach (Renderer renderer in purpleObject.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = purpleMaterial;
            purpleObject.SetActive(true);
        }

        game.requiredTreasureKeys = 2;
        game.maxCheckpointRespawns = 2;
        Transform objective = chest.transform.parent;
        objective.position += Vector3.forward * (500f - chest.transform.position.z);
        foreach (TMP_Text text in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
        {
            text.text = text.text.Replace("collect all four golden keys", "find both golden keys")
                                 .Replace("collect four ancient keys", "find both golden keys")
                                 .Replace("collect four unique keys", "find both golden keys");
        }



        Validate(scene, track, game, startup, namedKeys, chest, purpleNames);
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        int mainIndex = Array.FindIndex(buildScenes, s => s.path == ScenePath);
        if (mainIndex < 0) throw new InvalidOperationException("LostCave_Main disappeared from Build Settings after preflight.");
        buildScenes[mainIndex].enabled = true;
        if (mainIndex != 0)
        {
            EditorBuildSettingsScene main = buildScenes[mainIndex];
            buildScenes = buildScenes.Where((s, i) => i != mainIndex).ToArray();
            buildScenes = new[] { main }.Concat(buildScenes).ToArray();
        }
        for (int i = 0; i < buildScenes.Length; i++)
            if (buildScenes[i].path == "Assets/Scenes/SampleScene.unity") buildScenes[i].enabled = false;
        EditorBuildSettings.scenes = buildScenes;
        Require(EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes[0].path == ScenePath && EditorBuildSettings.scenes[0].enabled, "Build Settings must place LostCave_Main first and enabled.");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("LostCave_Main save failed after validation.");
        AssetDatabase.SaveAssets();
        Scene reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Validate(reopened, Single<RunnerTrackManager>(reopened, "RunnerTrackManager"), Single<GameManager>(reopened, "GameManager"),
            reopened.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RunnerTrackSection>(true)).Where(s => s.gameObject.activeInHierarchy).OrderBy(s => s.sequenceSlot).ToArray(),
            new[] { FindKey(reopened, "GoldenKey_ID_0"), FindKey(reopened, "GoldenKey_ID_1"), FindKey(reopened, "GoldenKey_ID_2"), FindKey(reopened, "GoldenKey_ID_3") },
            FindObject(reopened, "FINAL VAULT TREASURE CHEST").GetComponent<TreasureChest>(), purpleNames);
        Require(EditorBuildSettings.scenes.First(s => s.path == ScenePath).enabled && EditorBuildSettings.scenes.First(s => s.enabled).path == ScenePath, "Build Settings verification failed.");
        Debug.Log("AUDIT_FIX_PASS_SCENE_SAVED | startupSlots=0-3 | keys=0,1 | duplicateRootKeysDisabled=4 | key1Z=210 | chestZ=500 | purpleCrystals=2 | regions=3 | buildScene=LostCave_Main");
    }

    private static void Validate(Scene scene, RunnerTrackManager track, GameManager game, RunnerTrackSection[] startup, TreasureKey[] keys, TreasureChest chest, string[] purpleNames)
    {
        Require(startup.Length == 4, "Exactly four connected startup sections are required.");
        for (int i = 0; i < startup.Length; i++)
        {
            Require(startup[i] != null && startup[i].sequenceSlot == i && startup[i].gameObject.activeInHierarchy && startup[i].Validate(3.5f, out _), "Startup slot validation failed at " + i + ".");
            Require(Math.Abs(startup[i].StartZ - i * 20f) < 0.05f, "Startup section Z order is invalid at slot " + i + ".");
            if (i > 0) Require(Math.Abs(startup[i - 1].EndZ - startup[i].StartZ) < 0.01f, "Startup seam validation failed at slot " + i + ".");
        }
        Require(game.requiredTreasureKeys == 2 && game.startingHealth == 3 && game.maxCheckpointRespawns == 2, "Two-key or health/retry settings failed validation.");
        Require(keys.Length == 4 && keys[0].keyId == 0 && keys[1].keyId == 1 && keys[2].keyId == 2 && keys[3].keyId == 3, "Explicit key IDs were lost or duplicated.");

        TreasureKey[] remainingDuplicateRootKeys = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true)).Where(k => k.transform.parent == null && ((k.name == "TreasureKey_0" && k.keyId == 0 && Vector3.Distance(k.transform.position, new Vector3(0f, 1.2f, 60f)) < 0.01f) || (k.name == "TreasureKey_1" && k.keyId == 1 && Vector3.Distance(k.transform.position, new Vector3(0f, 1.2f, 200f)) < 0.01f))).ToArray();
        Require(remainingDuplicateRootKeys.Length == 4 && remainingDuplicateRootKeys.All(k => !k.gameObject.activeInHierarchy), "All four verified root preview key pickups must remain disabled.");
        TreasureKey[] activeSceneKeys = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true)).Where(k => k.gameObject.activeInHierarchy).ToArray();
        Require(activeSceneKeys.Length == 2 && activeSceneKeys.Select(k => k.keyId).OrderBy(id => id).SequenceEqual(new[] { 0, 1 }), "Exactly the unique active key IDs 0 and 1 must remain in the playable scene.");
        Require(keys[0].gameObject.activeInHierarchy && keys[1].gameObject.activeInHierarchy && !keys[2].gameObject.activeInHierarchy && !keys[3].gameObject.activeInHierarchy, "Only IDs 0 and 1 should be active.");
        Require(new[] { keys[0], keys[1] }.All(k => k.visual != null && k.visual.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy)), "The two playable key models must have enabled visible renderers.");
        Require(Vector3.Distance(keys[0].transform.position, new Vector3(0f, 1f, 70f)) < 0.01f && Vector3.Distance(keys[1].transform.position, new Vector3(0f, 1f, 210f)) < 0.01f, "Key lane/stage positions failed validation.");
        Require(chest != null && chest.lid != null && chest.openingSparkles != null && chest.treasureLight != null && chest.GetComponent<Collider>() is { enabled: true, isTrigger: true } && Math.Abs(chest.transform.position.z - 500f) < 0.01f, "Final chest references, trigger, or route-end placement failed validation.");
        foreach (string name in purpleNames)
        {
            GameObject pickup = FindObject(scene, name);
            Collectible c = pickup != null ? pickup.GetComponent<Collectible>() : null;
            Require(pickup != null && pickup.activeInHierarchy && c != null && c.purpleCrystalVariant && c.treasureType == Collectible.TreasureType.BlueCrystal && pickup.GetComponent<Collider>() is { enabled: true, isTrigger: true } && pickup.GetComponentInChildren<Renderer>(true) != null, "Purple crystal trigger/model validation failed: " + name);
        }
        Require(track.sectionsContainer != null && track.transitionToCrystalCaverns != null && track.transitionToFinalTreasureVault != null, "Region progression references are missing.");
    }

    private static T Single<T>(Scene scene, string label) where T : Component
    {
        T[] found = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        Require(found.Length == 1, "Expected one " + label + ", found " + found.Length + ".");
        return found[0];
    }

    private static GameObject FindObject(Scene scene, string name)
    {
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();
    }

    private static TreasureKey FindKey(Scene scene, string name)
    {
        GameObject go = FindObject(scene, name);
        return go != null ? go.GetComponent<TreasureKey>() : null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("AUDIT_FIX_PASS_PREFLIGHT_FAILED | " + message);
    }
}
#endif
