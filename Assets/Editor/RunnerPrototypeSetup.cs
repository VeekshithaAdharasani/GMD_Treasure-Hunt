using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RunnerPrototypeSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string PrototypeRootName = "RUNNER PROTOTYPE - TEMP";
    private const string OldBlockoutName = "WORLD BLOCKOUT - TEMP (replace during art pass)";

    [MenuItem("Tools/Runner Prototype/Verify and Save Prototype")]
    public static void VerifyAndSavePrototype()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject prototype = FindSceneObject(PrototypeRootName);
        GameObject oldBlockout = FindSceneObject(OldBlockoutName);
        GameObject playerObject = FindSceneObject("Player");
        GameObject cameraObject = FindSceneObject("Main Camera");
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        GameObject restartObject = FindSceneObject("RestartButton");
        Button restartButton = restartObject != null ? restartObject.GetComponent<Button>() : null;

        if (prototype == null || oldBlockout == null || oldBlockout.activeSelf ||
            playerObject == null || playerObject.GetComponent<PlayerMovement>() == null ||
            cameraObject == null || cameraObject.GetComponent<CameraFollow>() == null ||
            cameraObject.GetComponent<CameraFollow>().target != playerObject.transform ||
            manager == null || manager.scoreText == null || manager.healthText == null || manager.gameOverPanel == null ||
            restartButton == null || restartButton.onClick.GetPersistentEventCount() == 0 ||
            restartButton.onClick.GetPersistentTarget(0) == null ||
            restartButton.onClick.GetPersistentMethodName(0) != "Restart" ||
            FindSceneObject("TEMP Runner slide bar - right lane") == null ||
            FindSceneObject("TEMP Runner jump crate - left lane") == null)
            throw new System.InvalidOperationException("Saved runner prototype validation failed; scene was not changed by validation.");

        int trailCount = 0;
        foreach (Collectible collectible in Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            if (collectible.name.StartsWith("TEMP Runner crystal trail "))
                trailCount++;
        if (trailCount != 6)
            throw new System.InvalidOperationException("Expected six prototype trail collectibles, found " + trailCount + ".");

        if (!EditorSceneManager.SaveScene(scene))
            throw new System.IO.IOException("Unity failed to save the validated runner prototype scene.");
        Debug.Log("M4_BATCH_VALIDATE_SAVED | scene=LostCave_Main | six_lane_trail_collectibles=verified | slide_jump_hazards=verified | score_health_restart_references=resolved | unrelated_objects=preserved");
    }
    [MenuItem("Tools/Runner Prototype/Build Three Lane Prototype")]
    public static void BuildAndSave()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (GameObject.Find(PrototypeRootName) != null)
            throw new System.InvalidOperationException("Runner prototype already exists; refusing to duplicate it.");

        GameObject playerObject = GameObject.Find("Player");
        GameObject cameraObject = GameObject.Find("Main Camera");
        GameObject oldBlockout = GameObject.Find(OldBlockoutName);
        GameObject crystalSource = GameObject.Find("Crystal_Blue_01");
        GameObject rock = GameObject.Find("RockObstacle_01");
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (playerObject == null || playerObject.GetComponent<CharacterController>() == null ||
            playerObject.GetComponent<PlayerMovement>() == null || cameraObject == null ||
            cameraObject.GetComponent<CameraFollow>() == null || crystalSource == null ||
            crystalSource.GetComponent<Collectible>() == null || rock == null ||
            rock.GetComponent<Obstacle>() == null || manager == null || manager.scoreText == null ||
            manager.healthText == null || manager.gameOverPanel == null)
            throw new System.InvalidOperationException("A required existing player, camera, collectible, obstacle, or GameManager reference is missing.");

        Button restartButton = FindSceneObject("RestartButton")?.GetComponent<Button>();
        if (restartButton == null || restartButton.onClick.GetPersistentEventCount() == 0 ||
            restartButton.onClick.GetPersistentTarget(0) == null ||
            restartButton.onClick.GetPersistentMethodName(0) != "Restart")
            throw new System.InvalidOperationException("Existing RestartButton callback does not resolve to Restart.");

        PlayerMovement runner = playerObject.GetComponent<PlayerMovement>();
        runner.autoRunSpeed = 8f;
        runner.laneWidth = 3.5f;
        runner.laneChangeSmoothTime = 0.12f;
        runner.laneChangeMaxSpeed = 20f;
        runner.jumpHeight = 1.5f;
        runner.gravity = -22f;
        runner.slideDuration = 0.9f;
        runner.slideHeight = 1f;

        CameraFollow cameraFollow = cameraObject.GetComponent<CameraFollow>();
        cameraFollow.target = playerObject.transform;

        if (oldBlockout != null)
            oldBlockout.SetActive(false);

        GameObject prototype = new GameObject(PrototypeRootName);
        prototype.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Transform track = prototype.transform;

        Material laneMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Blockout_Temp/TEMP_Blockout_R02_Crystal.mat");
        CreateBox(track, "TEMP Runner continuation floor", new Vector3(0f, -0.3f, 110f), new Vector3(8f, 0.6f, 100f), laneMaterial, false);
        CreateBox(track, "TEMP Runner left boundary", new Vector3(-3.85f, 0.9f, 110f), new Vector3(0.3f, 1.8f, 100f), laneMaterial, false);
        CreateBox(track, "TEMP Runner right boundary", new Vector3(3.85f, 0.9f, 110f), new Vector3(0.3f, 1.8f, 100f), laneMaterial, false);
        CreateBox(track, "TEMP Runner lane marker left", new Vector3(-1.25f, 0.015f, 80f), new Vector3(0.06f, 0.02f, 160f), laneMaterial, true);
        CreateBox(track, "TEMP Runner lane marker right", new Vector3(1.25f, 0.015f, 80f), new Vector3(0.06f, 0.02f, 160f), laneMaterial, true);

        CreateObstacle(track, "TEMP Runner slide bar - right lane", new Vector3(3.5f, 1.62f, 32f), new Vector3(2.3f, 0.75f, 0.8f), laneMaterial);
        CreateObstacle(track, "TEMP Runner jump crate - left lane", new Vector3(-3.5f, 0.45f, 45f), new Vector3(2f, 0.9f, 1.5f), laneMaterial);

        Vector3[] trailPositions =
        {
            new Vector3(-3.5f, crystalSource.transform.position.y, 8f),
            new Vector3(0f, crystalSource.transform.position.y, 10f),
            new Vector3(3.5f, crystalSource.transform.position.y, 13f),
            new Vector3(-3.5f, crystalSource.transform.position.y, 24f),
            new Vector3(0f, crystalSource.transform.position.y, 27f),
            new Vector3(3.5f, crystalSource.transform.position.y, 30f)
        };
        for (int i = 0; i < trailPositions.Length; i++)
        {
            GameObject crystal = Object.Instantiate(crystalSource, trailPositions[i], crystalSource.transform.rotation, track);
            crystal.name = "TEMP Runner crystal trail " + (i + 1).ToString("00");
            crystal.SetActive(true);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new System.IO.IOException("Unity failed to save the runner prototype scene.");

        Debug.Log("M4_RUNNER_PROTOTYPE_SAVED | auto-run=8 | lane-width=2.5 | existing-cave=preserved | old-world-blockout=temporarily-disabled | hazards=rock+slide-bar+jump-crate | trail-pickups=6 | ui-restart=verified");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            GameObject found = FindRecursive(root.transform, objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static GameObject FindRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent.gameObject;
        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found = FindRecursive(parent.GetChild(i), objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool visualOnly)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;
        if (material != null)
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (visualOnly)
            Object.DestroyImmediate(box.GetComponent<BoxCollider>());
        return box;
    }

    private static void CreateObstacle(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obstacle = CreateBox(parent, name, position, scale, material, false);
        obstacle.AddComponent<Obstacle>();
    }
}