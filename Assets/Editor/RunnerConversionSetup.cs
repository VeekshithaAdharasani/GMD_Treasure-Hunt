#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class RunnerConversionSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string BlockoutName = "WORLD BLOCKOUT - TEMP (replace during art pass)";
    private const float LaneWidth = 3.5f;
    private const float TotalTrackLength = 260f;

    private sealed class Segment
    {
        public RunnerTrackSection prefab;
        public float startZ;
        public float endZ;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = Find(root, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject Find(GameObject root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root.transform)
        {
            GameObject found = Find(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }

    private static T[] FindComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static List<RunnerTrackSection> BuildRoute(RunnerTrackManager manager)
    {
        return new List<RunnerTrackSection>()
        {
            manager.forgottenEntranceSections[0], manager.forgottenEntranceSections[1], manager.forgottenEntranceSections[2],
            manager.transitionToCrystalCaverns, manager.transitionToAncientRuins,
            manager.ancientRuinsSections[0], manager.ancientRuinsSections[1], manager.ancientRuinsSections[2],
            manager.transitionToLostTemple, manager.transitionToFinalTreasureVault,
            manager.finalTreasureVaultSections[0], manager.finalTreasureVaultSections[1], manager.finalTreasureVaultSections[2]
        };
    }

    private static List<Segment> ValidateRoute(List<RunnerTrackSection> route)
    {
        var segments = new List<Segment>();
        float nextZ = 0f;
        foreach (RunnerTrackSection prefab in route)
        {
            Require(prefab != null, "Preflight failed: a required track section prefab is missing.");
            Require(prefab.Validate(LaneWidth, out string error), "Preflight failed: " + error);
            Require(Mathf.Abs(prefab.sectionLength - 20f) < 0.01f,
                "Preflight failed: the three-stage route expects 20 metre modules, but " + prefab.name + " has length " + prefab.sectionLength + ".");
            segments.Add(new Segment { prefab = prefab, startZ = nextZ, endZ = nextZ + prefab.sectionLength });
            nextZ += prefab.sectionLength;
        }
        Require(Mathf.Abs(nextZ - TotalTrackLength) < 0.01f, "Preflight failed: the three-stage track length is not 260 metres.");
        return segments;
    }

    private static bool PointOverlapsObstacle(Segment segment, Vector3 point)
    {
        Obstacle[] obstacles = segment.prefab.GetComponentsInChildren<Obstacle>(true);
        foreach (Obstacle obstacle in obstacles)
        {
            Collider[] colliders = obstacle.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled) continue;
                Bounds bounds = collider.bounds;
                bounds.center += Vector3.forward * segment.startZ;
                bounds.Expand(new Vector3(1.6f, 1.2f, 1.6f));
                if (bounds.Contains(point)) return true;
            }
        }
        return false;
    }

    private static void ValidatePickupPoint(List<Segment> segments, Vector3 point, string label)
    {
        int lane = Mathf.RoundToInt(point.x / LaneWidth);
        Require(lane >= -1 && lane <= 1 && Mathf.Abs(point.x - lane * LaneWidth) < 0.01f,
            "Preflight failed: " + label + " is outside a runner lane.");
        Segment segment = segments.FirstOrDefault(item => point.z >= item.startZ && point.z < item.endZ);
        Require(segment != null, "Preflight failed: " + label + " is outside the active track bounds.");
        Require(!PointOverlapsObstacle(segment, point), "Preflight failed: " + label + " overlaps an obstacle in " + segment.prefab.name + ".");
    }

    private static TMP_Text MakeText(Scene scene, Canvas canvas, TMP_Text template, string objectName, string value)
    {
        GameObject textObject = Find(scene, objectName);
        if (textObject == null)
        {
            textObject = UnityEngine.Object.Instantiate(template.gameObject, canvas.transform, false);
            textObject.name = objectName;
        }
        else if (textObject.transform.parent != canvas.transform)
        {
            textObject.transform.SetParent(canvas.transform, false);
        }
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = 24f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
        return text;
    }

    private static void SetTextRect(TMP_Text text, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.alignment = alignment;
    }

    private static void ClearPersistentListeners(Button button)
    {
        for (int index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
            UnityEventTools.RemovePersistentListener(button.onClick, index);
    }

    private static Slider MakeProgressSlider(Scene scene, Canvas canvas)
    {
        GameObject go = Find(scene, "RunnerStageProgressSlider");
        if (go == null)
        {
            go = new GameObject("RunnerStageProgressSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(canvas.transform, false);
        }
        else if (go.transform.parent != canvas.transform)
        {
            go.transform.SetParent(canvas.transform, false);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -87f);
        rect.sizeDelta = new Vector2(300f, 16f);

        Image background = go.GetComponent<Image>();
        background.color = new Color(0.08f, 0.06f, 0.04f, 0.9f);
        background.raycastTarget = false;

        GameObject fillAreaObject = Find(go, "Fill Area");
        if (fillAreaObject == null)
        {
            fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObject.transform.SetParent(go.transform, false);
        }
        RectTransform fillArea = fillAreaObject.GetComponent<RectTransform>();
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(3f, 3f);
        fillArea.offsetMax = new Vector2(-3f, -3f);

        GameObject fillObject = Find(fillAreaObject, "Fill");
        if (fillObject == null)
        {
            fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(fillAreaObject.transform, false);
        }
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(1f, 0.72f, 0.16f, 1f);
        fillImage.raycastTarget = false;

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.targetGraphic = background;
        slider.fillRect = fillRect;
        slider.handleRect = null;
        go.SetActive(true);
        return slider;
    }

    private static GameObject GetOrCreateSceneRoot(Scene scene, string name)
    {
        GameObject root = Find(scene, name);
        if (root == null)
        {
            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        return root;
    }

    private static void PlaceCrystalTrail(Scene scene, GameObject pickupRoot, GameObject crystalPrefab, List<Segment> segments,
        string trailName, float laneX, float startZ, int count)
    {
        for (int index = 0; index < count; index++)
        {
            string itemName = trailName + "_" + index.ToString("00");
            Vector3 position = new Vector3(laneX, 1f, startZ + index * 2f);
            ValidatePickupPoint(segments, position, itemName);

            GameObject item = Find(scene, itemName);
            if (item == null)
            {
                item = PrefabUtility.InstantiatePrefab(crystalPrefab, scene) as GameObject;
                Require(item != null, "Crystal creation failed for " + itemName + ".");
                item.name = itemName;
            }
            if (item.transform.parent != pickupRoot.transform) item.transform.SetParent(pickupRoot.transform, false);
            item.transform.SetPositionAndRotation(position, Quaternion.identity);
            item.SetActive(true);
        }
    }

    [MenuItem("Tools/Runner Adventure/Configure Three Stage Runner")]
    public static void Configure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Preflight: resolve every required scene object, reference and route prefab before mutation.
        GameObject blockout = Find(scene, BlockoutName);
        PlayerMovement[] players = FindComponents<PlayerMovement>(scene);
        CameraFollow[] cameras = FindComponents<CameraFollow>(scene);
        RunnerTrackManager[] managers = FindComponents<RunnerTrackManager>(scene);
        GameManager[] gameManagers = FindComponents<GameManager>(scene);
        TreasureChest[] chests = FindComponents<TreasureChest>(scene);
        Canvas[] canvases = FindComponents<Canvas>(scene);
        EventSystem[] eventSystems = FindComponents<EventSystem>(scene);

        Require(blockout != null, "Preflight failed: the confirmed world blockout root is missing.");
        Require(players.Length == 1 && players[0].GetComponent<CharacterController>() != null,
            "Preflight failed: the scene must have one PlayerMovement with a CharacterController.");
        Require(cameras.Length == 1 && cameras[0].GetComponent<Camera>() != null,
            "Preflight failed: one CameraFollow camera is required.");
        Require(managers.Length == 1, "Preflight failed: the scene must have exactly one RunnerTrackManager.");
        Require(gameManagers.Length == 1, "Preflight failed: the scene must have exactly one GameManager.");
        Require(chests.Length == 1, "Preflight failed: exactly one existing TreasureChest is required.");
        Require(canvases.Length > 0, "Preflight failed: the existing Canvas is missing.");
        Require(eventSystems.Length == 1 && eventSystems[0].gameObject.activeInHierarchy,
            "Preflight failed: exactly one active EventSystem is required.");

        RunnerTrackManager manager = managers[0];
        GameManager gameManager = gameManagers[0];
        PlayerMovement player = players[0];
        CameraFollow cameraFollow = cameras[0];
        TreasureChest chest = chests[0];
        Canvas scoreCanvas = gameManager.scoreText != null ? gameManager.scoreText.canvas : null;
        Canvas stageCanvas = gameManager.stageText != null ? gameManager.stageText.canvas : null;
        Canvas instructionsCanvas = gameManager.instructionsPanel != null ? gameManager.instructionsPanel.GetComponentInParent<Canvas>() : null;
        CharacterController controller = player.GetComponent<CharacterController>();

        Require(gameManager.scoreText != null && gameManager.healthText != null && gameManager.treasureKeysText != null &&
                gameManager.stageText != null && gameManager.winPanel != null && gameManager.gameOverPanel != null &&
                gameManager.finalStatsText != null && gameManager.mainMenuPanel != null && gameManager.instructionsPanel != null &&
                gameManager.pausePanel != null && gameManager.playButton != null && gameManager.instructionsButton != null &&
                gameManager.exitButton != null && gameManager.resumeButton != null,
            "Preflight failed: an existing required HUD panel, text reference or menu button is missing.");
        Require(scoreCanvas != null && stageCanvas != null && instructionsCanvas != null &&
                canvases.Contains(scoreCanvas) && canvases.Contains(stageCanvas) && canvases.Contains(instructionsCanvas),
            "Preflight failed: an existing HUD or instruction panel is not attached to one of the saved Canvases.");
        GameObject gameOverRestartObject = Find(gameManager.gameOverPanel, "RestartButton");
        GameObject victoryRestartObject = Find(gameManager.winPanel, "RestartAdventureButton");
        Button gameOverRestartButton = gameOverRestartObject != null ? gameOverRestartObject.GetComponent<Button>() : null;
        Button victoryRestartButton = victoryRestartObject != null ? victoryRestartObject.GetComponent<Button>() : null;
        Require(gameOverRestartButton != null && victoryRestartButton != null,
            "Preflight failed: the existing Game Over and Victory Restart buttons are missing.");
        Require(chest.lid != null && chest.openingSparkles != null && chest.treasureLight != null,
            "Preflight failed: the existing chest lid, sparkles or treasure light reference is missing.");
        Collider chestCollider = chest.GetComponent<Collider>();
        Require(chestCollider != null && chestCollider.enabled && chestCollider.isTrigger,
            "Preflight failed: the existing chest needs an enabled trigger collider.");

        Require(manager.forgottenEntranceSections != null && manager.forgottenEntranceSections.Length == 3 &&
                manager.ancientRuinsSections != null && manager.ancientRuinsSections.Length == 3 &&
                manager.finalTreasureVaultSections != null && manager.finalTreasureVaultSections.Length == 3 &&
                manager.transitionToCrystalCaverns != null && manager.transitionToAncientRuins != null &&
                manager.transitionToLostTemple != null && manager.transitionToFinalTreasureVault != null,
            "Preflight failed: one of the existing three-level section sets or transition prefabs is missing.");
        List<RunnerTrackSection> route = BuildRoute(manager);
        List<Segment> segments = ValidateRoute(route);

        TreasureKey[] keys = FindComponents<TreasureKey>(scene);
        Require(keys.Length == 2 && keys.Select(key => key.keyId).Distinct().Count() == 2 &&
                new[] { 0, 1 }.All(id => keys.Any(key => key.keyId == id)),
            "Preflight failed: the scene must contain exactly one existing TreasureKey for each ID 0-1.");
        foreach (TreasureKey key in keys)
        {
            Collider collider = key.GetComponent<Collider>();
            Require(collider != null && collider.enabled && collider.isTrigger && key.visual != null &&
                    key.visual.GetComponentInChildren<Renderer>(true) != null,
                "Preflight failed: key " + key.keyId + " lacks a visible model or enabled trigger collider.");
        }

        Vector3[] keyPositions =
        {
            new Vector3(0f, 1f, 70f),
            new Vector3(0f, 1f, 210f)
        };
        for (int id = 0; id < keyPositions.Length; id++)
        {
            float expectedStart = id == 0 ? 0f : 180f;
            float expectedEnd = id == 0 ? 180f : 360f;
            Require(keyPositions[id].z >= expectedStart && keyPositions[id].z < expectedEnd,
                "Preflight failed: key " + id + " is outside its assigned level.");
            ValidatePickupPoint(segments, keyPositions[id], "GoldenKey_ID_" + id);
        }

        string crystalPrefabPath = "Assets/Prefabs/Runner/Pickups/Runner_Crystal_Blue.prefab";
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(crystalPrefabPath);
        Collectible crystalLogic = crystalPrefab != null ? crystalPrefab.GetComponent<Collectible>() : null;
        Collider crystalCollider = crystalPrefab != null ? crystalPrefab.GetComponent<Collider>() : null;
        Require(crystalLogic != null && crystalCollider != null && crystalCollider.isTrigger &&
                crystalPrefab.GetComponentInChildren<Renderer>(true) != null,
            "Preflight failed: the existing blue crystal prefab is missing its model, Collectible or trigger collider.");

        float[][] trails =
        {
            new[] { 64f, 0f },
            new[] { 82f, 0f },
            new[] { 162f, 0f },
            new[] { 182f, 0f },
            new[] { 242f, 0f }
        };
        foreach (float[] trail in trails)
        {
            for (int index = 0; index < 5; index++)
                ValidatePickupPoint(segments, new Vector3(trail[1], 1f, trail[0] + index * 2f), "CrystalTrail");
        }

        GameObject existingRuntime = Find(manager.gameObject, "RUNNER_TRACK_RUNTIME");
        RunnerTrackSection[] runtimeSections = existingRuntime != null
            ? existingRuntime.GetComponentsInChildren<RunnerTrackSection>(true)
            : Array.Empty<RunnerTrackSection>();
        Require(runtimeSections.Length == 0 || runtimeSections.Length == 4,
            "Preflight failed: the runner runtime container has a partial preview section set.");
        if (runtimeSections.Length == 4)
        {
            runtimeSections = runtimeSections.OrderBy(section => section.sequenceSlot).ToArray();
            for (int i = 0; i < runtimeSections.Length; i++)
                Require(runtimeSections[i].sequenceSlot == i && runtimeSections[i].Validate(LaneWidth, out _),
                    "Preflight failed: the existing runtime preview sections are inconsistent.");
        }

        // All references and placements are valid. Scene mutations begin here.
        GameObject pickupRoot = GetOrCreateSceneRoot(scene, "RUNNER_GAMEPLAY_PICKUPS");
        GameObject objectiveRoot = GetOrCreateSceneRoot(scene, "RUNNER_FINAL_OBJECTIVE");
        GameObject runtimeContainer = existingRuntime;
        if (runtimeContainer == null)
        {
            runtimeContainer = new GameObject("RUNNER_TRACK_RUNTIME");
            runtimeContainer.transform.SetParent(manager.transform, false);
            runtimeContainer.transform.localPosition = Vector3.zero;
            runtimeContainer.transform.localRotation = Quaternion.identity;
            runtimeContainer.transform.localScale = Vector3.one;
        }

        foreach (RunnerTrackSection section in FindComponents<RunnerTrackSection>(scene))
        {
            if (!section.transform.IsChildOf(runtimeContainer.transform)) section.gameObject.SetActive(false);
        }

        if (runtimeSections.Length == 0)
        {
            float previewZ = 0f;
            for (int slot = 0; slot < 4; slot++)
            {
                RunnerTrackSection prefab = route[slot];
                GameObject previewObject = PrefabUtility.InstantiatePrefab(prefab.gameObject, scene) as GameObject;
                Require(previewObject != null, "Failed to create track preview section " + slot + ".");
                RunnerTrackSection preview = previewObject.GetComponent<RunnerTrackSection>();
                preview.transform.SetParent(runtimeContainer.transform, false);
                preview.transform.SetPositionAndRotation(new Vector3(0f, 0f, previewZ), Quaternion.identity);
                preview.sequenceSlot = slot;
                // Legacy section prefabs may include key pickup props. Use only
                // the four explicitly placed scene keys in this runner route.
                foreach (TreasureKey embeddedKey in preview.GetComponentsInChildren<TreasureKey>(true))
                    embeddedKey.gameObject.SetActive(false);
                preview.gameObject.SetActive(true);
                previewZ += preview.sectionLength;
            }
        }

        manager.player = player;
        manager.sectionsContainer = runtimeContainer.transform;
        manager.sectionsPerBiome = 3;
        manager.nextSequenceSlot = 4;
        manager.nextStartZ = 80f;
        manager.spawnAheadDistance = Mathf.Max(80f, manager.spawnAheadDistance);
        manager.enabled = true;

        controller.enabled = false;
        player.adventureMode = false;
        player.laneWidth = LaneWidth;
        player.autoRunSpeed = 8f;
        player.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
        controller.enabled = true;
        cameraFollow.target = player.transform;
        player.cameraTransform = cameraFollow.transform;

        foreach (TreasureKey key in keys)
        {
            if (key.transform.parent != pickupRoot.transform) key.transform.SetParent(pickupRoot.transform, true);
            key.transform.position = keyPositions[key.keyId];
            key.gameObject.SetActive(true);
            if (key.visual != null) key.visual.gameObject.SetActive(true);
        }

        chest.transform.SetParent(objectiveRoot.transform, true);
        chest.transform.SetPositionAndRotation(new Vector3(0f, 0f, 256f), Quaternion.identity);
        chest.gameObject.SetActive(true);

        PlaceCrystalTrail(scene, pickupRoot, crystalPrefab, segments, "RunnerCrystal_L1_TransitionA", 0f, 64f, 5);
        PlaceCrystalTrail(scene, pickupRoot, crystalPrefab, segments, "RunnerCrystal_L1_TransitionB", 0f, 82f, 5);
        PlaceCrystalTrail(scene, pickupRoot, crystalPrefab, segments, "RunnerCrystal_L2_TransitionA", 0f, 162f, 5);
        PlaceCrystalTrail(scene, pickupRoot, crystalPrefab, segments, "RunnerCrystal_L2_TransitionB", 0f, 182f, 5);
        PlaceCrystalTrail(scene, pickupRoot, crystalPrefab, segments, "RunnerCrystal_L3_FinalApproach", 0f, 242f, 5);

        foreach (Collectible collectible in FindComponents<Collectible>(scene))
        {
            if (!collectible.transform.IsChildOf(pickupRoot.transform)) collectible.gameObject.SetActive(false);
        }

        blockout.SetActive(false);
        foreach (AdventurePortal portal in FindComponents<AdventurePortal>(scene)) portal.gameObject.SetActive(false);
        foreach (RegionTrigger trigger in FindComponents<RegionTrigger>(scene)) trigger.enabled = false;
        foreach (AdventureLevelManager levelManager in FindComponents<AdventureLevelManager>(scene)) levelManager.enabled = false;

        foreach (Canvas existingCanvas in canvases)
        {
            CanvasScaler scaler = existingCanvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = existingCanvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        TMP_Text crystalText = MakeText(scene, scoreCanvas, gameManager.scoreText, "RunnerCrystalCountText", "Crystals: 0");
        TMP_Text progressText = MakeText(scene, stageCanvas, gameManager.stageText, "RunnerStageProgressText", "STAGE 0%");
        gameManager.crystalCountText = crystalText;
        gameManager.stageProgressText = progressText;
        gameManager.stageProgressSlider = MakeProgressSlider(scene, stageCanvas);
        gameManager.gameOverRestartButton = gameOverRestartButton;
        gameManager.victoryRestartButton = victoryRestartButton;
        ClearPersistentListeners(gameOverRestartButton);
        ClearPersistentListeners(victoryRestartButton);
        SetTextRect(gameManager.scoreText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(220f, 36f), TextAlignmentOptions.Left);
        SetTextRect(crystalText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -58f), new Vector2(220f, 36f), TextAlignmentOptions.Left);
        SetTextRect(gameManager.healthText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(170f, 36f), TextAlignmentOptions.Right);
        SetTextRect(gameManager.treasureKeysText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -58f), new Vector2(220f, 36f), TextAlignmentOptions.Right);
        SetTextRect(gameManager.stageText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(500f, 38f), TextAlignmentOptions.Center);
        SetTextRect(progressText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -57f), new Vector2(180f, 28f), TextAlignmentOptions.Center);
        if (gameManager.interactionHintText != null)
            SetTextRect(gameManager.interactionHintText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(720f, 42f), TextAlignmentOptions.Center);

        GameObject instructionsBody = Find(scene, "RunnerInstructionsBody");
        if (instructionsBody == null)
        {
            TMP_Text instructionText = MakeText(scene, instructionsCanvas, gameManager.stageText, "RunnerInstructionsBody",
                "AUTO RUN: forward\nA / Left Arrow: move left    D / Right Arrow: move right\nSpace: jump    S / Down Arrow / Ctrl: slide\nCollect blue crystals for score and find both golden keys.\nReach the Treasure Vault chest and press E to open it.");
            instructionText.transform.SetParent(gameManager.instructionsPanel.transform, false);
            instructionText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform instructionsRect = instructionText.rectTransform;
            instructionsRect.anchorMin = new Vector2(0.5f, 0.5f);
            instructionsRect.anchorMax = new Vector2(0.5f, 0.5f);
            instructionsRect.pivot = new Vector2(0.5f, 0.5f);
            instructionsRect.anchoredPosition = new Vector2(0f, 20f);
            instructionsRect.sizeDelta = new Vector2(700f, 250f);
            instructionText.fontSize = 24f;
            instructionText.alignment = TextAlignmentOptions.Center;
        }

        gameManager.startingHealth = 3;
        gameManager.requiredTreasureKeys = 2;
        gameManager.mainMenuPanel.SetActive(true);
        gameManager.instructionsPanel.SetActive(false);
        gameManager.pausePanel.SetActive(false);
        gameManager.gameOverPanel.SetActive(false);
        gameManager.winPanel.SetActive(false);
        if (gameManager.lockedTreasurePanel != null) gameManager.lockedTreasurePanel.SetActive(false);

        ConfigureBuildSettings();
        ValidateConfiguredScene(scene, player, cameraFollow, manager, gameManager, chest, blockout, pickupRoot, objectiveRoot, runtimeContainer, keyPositions, canvases.Length);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(chest);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "SaveScene failed for LostCave_Main.");

        Scene reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateReloadedScene(reopened);
        Debug.Log("THREE_STAGE_RUNNER_SETUP_SAVED | Forgotten Entrance, Ancient Ruins, Treasure Vault; finite 25-section route; two reachable keys; 25 crystal pickups; chest at Z=500; LostCave_Main remains first in Build Settings.");
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        int index = scenes.FindIndex(item => item.path == ScenePath);
        if (index < 0) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        else
        {
            scenes.RemoveAt(index);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ValidateConfiguredScene(Scene scene, PlayerMovement player, CameraFollow cameraFollow, RunnerTrackManager manager,
        GameManager gameManager, TreasureChest chest, GameObject blockout, GameObject pickupRoot, GameObject objectiveRoot,
        GameObject runtimeContainer, Vector3[] keyPositions, int expectedCanvasCount)
    {
        Require(!blockout.activeSelf, "Validation failed: the free-roam blockout is still active.");
        Require(!player.adventureMode && Mathf.Abs(player.autoRunSpeed - 8f) < 0.01f && Mathf.Abs(player.laneWidth - LaneWidth) < 0.01f,
            "Validation failed: player runner movement configuration is incorrect.");
        Require(cameraFollow.target == player.transform, "Validation failed: follow camera target is incorrect.");
        Require(manager.sectionsContainer == runtimeContainer.transform && manager.nextSequenceSlot == 4 && Mathf.Abs(manager.nextStartZ - 80f) < 0.01f,
            "Validation failed: finite runner sequence was not initialized at the preview exit.");
        RunnerTrackSection[] previews = runtimeContainer.GetComponentsInChildren<RunnerTrackSection>(true).OrderBy(section => section.sequenceSlot).ToArray();
        Require(previews.Length == 4, "Validation failed: four connected startup track sections are required.");
        for (int i = 0; i < previews.Length; i++)
        {
            bool valid = previews[i].Validate(LaneWidth, out string error);
            Require(previews[i].sequenceSlot == i && valid, "Validation failed: " + (valid ? "preview sequence slots are not contiguous." : error));
            if (i > 0) Require(Mathf.Abs(previews[i - 1].EndZ - previews[i].StartZ) < 0.01f, "Validation failed: startup track sections have a gap or overlap.");
        }

        TreasureKey[] keys = FindComponents<TreasureKey>(scene).Where(key => key.gameObject.activeInHierarchy).ToArray();
        Require(keys.Length == 2 && keys.Select(key => key.keyId).Distinct().Count() == 2, "Validation failed: there must be exactly two unique keys.");
        foreach (TreasureKey key in keys)
        {
            Collider collider = key.GetComponent<Collider>();
            Require(key.keyId >= 0 && key.keyId <= 1 && key.gameObject.activeInHierarchy &&
                    key.transform.IsChildOf(pickupRoot.transform) && collider != null && collider.enabled && collider.isTrigger &&
                    key.visual != null && key.visual.GetComponentInChildren<Renderer>(true) != null,
                "Validation failed: key " + key.keyId + " is inactive, misplaced, invisible or lacks its trigger collider.");
            Require(Vector3.Distance(key.transform.position, keyPositions[key.keyId]) < 0.01f,
                "Validation failed: key " + key.keyId + " moved from its reachable lane position.");
        }

        Collectible[] crystals = FindComponents<Collectible>(scene).Where(item => item.transform.IsChildOf(pickupRoot.transform)).ToArray();
        Require(crystals.Length == 25 && crystals.All(item => item.gameObject.activeInHierarchy && item.GetComponent<Collider>() != null && item.GetComponent<Collider>().isTrigger),
            "Validation failed: the five crystal trails must contain 25 active trigger pickups.");
        foreach (AdventurePortal portal in FindComponents<AdventurePortal>(scene))
            Require(!portal.gameObject.activeInHierarchy, "Validation failed: a free-roam portal remains active.");

        Collider chestCollider = chest.GetComponent<Collider>();
        Require(chest.transform.IsChildOf(objectiveRoot.transform) && Mathf.Abs(chest.transform.position.z - 256f) < 0.01f &&
                chestCollider != null && chestCollider.enabled && chestCollider.isTrigger && chest.lid != null &&
                chest.openingSparkles != null && chest.treasureLight != null,
            "Validation failed: final chest position, trigger or references are invalid.");
        Require(gameManager.crystalCountText != null && gameManager.stageProgressText != null && gameManager.stageProgressSlider != null &&
                gameManager.treasureKeysText != null && gameManager.scoreText != null && gameManager.healthText != null &&
                gameManager.gameOverRestartButton != null && gameManager.victoryRestartButton != null,
            "Validation failed: runner HUD references are incomplete.");
        Require(FindComponents<Canvas>(scene).Length == expectedCanvasCount && FindComponents<EventSystem>(scene).Length == 1,
            "Validation failed: the existing Canvas/EventSystem count changed.");

        EditorBuildSettingsScene first = EditorBuildSettings.scenes.FirstOrDefault();
        Require(first != null && first.enabled && first.path == ScenePath, "Validation failed: LostCave_Main is not the first enabled Build Settings scene.");
    }

    private static void ValidateReloadedScene(Scene scene)
    {
        PlayerMovement player = FindComponents<PlayerMovement>(scene).SingleOrDefault();
        CameraFollow camera = FindComponents<CameraFollow>(scene).SingleOrDefault();
        RunnerTrackManager manager = FindComponents<RunnerTrackManager>(scene).SingleOrDefault();
        GameManager gameManager = FindComponents<GameManager>(scene).SingleOrDefault();
        TreasureChest chest = FindComponents<TreasureChest>(scene).SingleOrDefault();
        Require(player != null && camera != null && manager != null && gameManager != null && chest != null,
            "Reload verification failed: a required gameplay component is missing.");
        Require(!player.adventureMode && camera.target == player.transform && manager.sectionsContainer != null,
            "Reload verification failed: runner controller, camera or track references did not persist.");
        TreasureKey[] keys = FindComponents<TreasureKey>(scene).Where(key => key.gameObject.activeInHierarchy).ToArray();
        Require(keys.Length == 2 && keys.Select(key => key.keyId).Distinct().Count() == 2 &&
                new[] { 0, 1 }.All(id => keys.Any(key => key.keyId == id && key.gameObject.activeInHierarchy && key.GetComponent<Collider>() != null && key.GetComponent<Collider>().isTrigger)),
            "Reload verification failed: IDs 0-3 are not unique, active and trigger-equipped.");
        Require(FindComponents<Collectible>(scene).Count(item => item.transform.name.StartsWith("RunnerCrystal_", StringComparison.Ordinal)) == 25,
            "Reload verification failed: crystal trails are not fully serialized.");
        GameObject blockout = Find(scene, BlockoutName);
        Require(blockout != null && !blockout.activeInHierarchy && chest.gameObject.activeInHierarchy && Mathf.Abs(chest.transform.position.z - 256f) < 0.01f,
            "Reload verification failed: the blockout is active or final chest placement did not persist.");
        Require(gameManager.crystalCountText != null && gameManager.stageProgressText != null && gameManager.stageProgressSlider != null,
            "Reload verification failed: runner HUD references did not persist.");
    }
}
#endif
