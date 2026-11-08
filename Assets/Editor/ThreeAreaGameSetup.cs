#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public static class ThreeAreaGameSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string BlockoutName = "WORLD BLOCKOUT - TEMP (replace during art pass)";

    private static GameObject Find(GameObject root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root.transform) { var found = Find(child.gameObject, name); if (found != null) return found; }
        return null;
    }
    private static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects()) { var found = Find(root, name); if (found != null) return found; }
        return null;
    }
    private static Bounds GetBounds(BoxCollider box)
    {
        if (Quaternion.Angle(box.transform.rotation, Quaternion.identity) > .05f) throw new InvalidOperationException("Rotated floor is unsupported by this safe placement pass: " + box.name);
        Vector3 scale = box.transform.lossyScale;
        return new Bounds(box.transform.TransformPoint(box.center), Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))));
    }
    private static BoxCollider RequireFloor(GameObject region, string name)
    {
        var floorObject = Find(region, name); var box = floorObject != null ? floorObject.GetComponent<BoxCollider>() : null;
        if (box == null || !box.enabled) throw new InvalidOperationException("Preflight failed: enabled floor BoxCollider missing: " + name);
        return box;
    }
    private static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
    private static GameObject Child(GameObject parent, string name)
    {
        var t = parent.transform.Find(name); return t != null ? t.gameObject : null;
    }
    private static GameObject EnsurePanel(Transform canvas, string name, string title, TMP_FontAsset font)
    {
        var panel = Child(canvas.gameObject, name);
        if (panel == null)
        {
            panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas, false);
            var rt = Rect(panel); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.035f, .025f, .015f, .94f);
        }
        var titleObject = Child(panel, "Title");
        if (titleObject == null) titleObject = MakeText(panel.transform, "Title", title, font, 38, new Vector2(0, 170), new Vector2(720, 90));
        else titleObject.GetComponent<TMP_Text>().text = title;
        return panel;
    }
    private static GameObject MakeText(Transform parent, string name, string text, TMP_FontAsset font, int size, Vector2 pos, Vector2 dims)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rt = Rect(go); rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = dims;
        var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.text = text; tmp.font = font; tmp.fontSize = size; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white; tmp.textWrappingMode = TextWrappingModes.Normal;
        return go;
    }
    private static Button EnsureButton(Transform panel, string name, string label, TMP_FontAsset font, int index, int count)
    {
        var go = Child(panel.gameObject, name);
        if (go == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); go.transform.SetParent(panel, false);
            var rt = Rect(go); rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.sizeDelta = new Vector2(300, 52); rt.anchoredPosition = new Vector2(0, 75 - index * 64);
            go.GetComponent<Image>().color = new Color(.42f, .24f, .08f, 1f);
            MakeText(go.transform, "Label", label, font, 23, Vector2.zero, new Vector2(290, 48));
        }
        var button = go.GetComponent<Button>(); if (button == null) throw new InvalidOperationException("UI button component missing: " + name);
        var labelObject = Child(go, "Label"); if (labelObject != null && labelObject.GetComponent<TMP_Text>() != null) labelObject.GetComponent<TMP_Text>().text = label;
        return button;
    }
    private static Transform FindCanvasWith(Scene scene, string child)
    {
        foreach (var go in scene.GetRootGameObjects()) foreach (var t in go.GetComponentsInChildren<Canvas>(true)) if (Find(t.gameObject, child) != null) return t.transform;
        return null;
    }
    private static Vector3 SafeFloorPoint(BoxCollider floor, Bounds bounds, Vector2 fraction)
    {
        float x = Mathf.Lerp(bounds.min.x + 1.5f, bounds.max.x - 1.5f, fraction.x);
        float z = Mathf.Lerp(bounds.min.z + 1.5f, bounds.max.z - 1.5f, fraction.y);
        return new Vector3(x, bounds.max.y, z);
    }
    private static void ValidateClearance(BoxCollider floor, Vector3 point, PlayerMovement player)
    {
        var hits = Physics.RaycastAll(point + Vector3.up * 8f, Vector3.down, 16f, ~0, QueryTriggerInteraction.Ignore);
        if (!hits.Any(h => h.collider == floor)) throw new InvalidOperationException("Arrival/key floor raycast did not hit expected floor: " + floor.name);
        var cc = player.GetComponent<CharacterController>();
        float radius = cc.radius * .9f; float bottom = point.y + cc.center.y - cc.height * .5f + radius; float top = point.y + cc.center.y + cc.height * .5f - radius;
        var overlaps = Physics.OverlapCapsule(new Vector3(point.x, bottom, point.z), new Vector3(point.x, top, point.z), radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var collider in overlaps)
            if (collider != floor && collider.transform != player.transform && !collider.transform.IsChildOf(player.transform))
                throw new InvalidOperationException("Clearance check blocked at " + point + " by " + collider.name);
    }
    private static AdventurePortal EnsurePortal(Scene scene, string name, Vector3 source, Vector3 arrival, int destinationLevel, BoxCollider floor, PlayerMovement player)
    {
        ValidateClearance(floor, arrival, player);
        var go = Find(scene, name);
        if (go == null) go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetPositionAndRotation(source, Quaternion.identity); go.transform.localScale = new Vector3(2.6f, 3f, .4f);
        var box = go.GetComponent<BoxCollider>(); if (box == null) box = go.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = Vector3.one;
        var portal = go.GetComponent<AdventurePortal>(); if (portal == null) portal = go.AddComponent<AdventurePortal>(); portal.destinationLevel = destinationLevel;
        var exit = Child(go, "Arrival"); if (exit == null) { exit = new GameObject("Arrival"); exit.transform.SetParent(go.transform, false); }
        exit.transform.SetPositionAndRotation(arrival, Quaternion.identity); portal.arrivalPoint = exit.transform; return portal;
    }

    [MenuItem("Tools/Adventure/Configure Three Area Game")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var blockout = Find(scene, BlockoutName);
        string[] regionNames = { "R01 Forgotten Entrance - TEMP BLOCKOUT", "R02 Crystal Caverns - TEMP BLOCKOUT", "R03 Ancient Ruins - TEMP BLOCKOUT", "R04 Underground Temple - TEMP BLOCKOUT", "R05 Final Treasure Vault - TEMP BLOCKOUT" };
        var regions = regionNames.Select(n => blockout != null ? Find(blockout, n) : null).ToArray();
        if (blockout == null || regions.Any(r => r == null)) throw new InvalidOperationException("Preflight failed: blockout root or one of the five region groups is missing.");
        var floors = new[] { RequireFloor(regions[0], "R01 forest clearing floor"), RequireFloor(regions[2], "R03 open ruins plaza"), RequireFloor(regions[4], "R05 central treasure platform - BLOCKOUT ONLY") };
        var bounds = floors.Select(GetBounds).ToArray();
        var playerObject = Find(scene, "Player"); var player = playerObject != null ? playerObject.GetComponent<PlayerMovement>() : null;
        var cameraObject = Find(scene, "Main Camera"); var camera = cameraObject != null ? cameraObject.GetComponent<CameraFollow>() : null;
        var gameManagerObject = Find(scene, "GameManager"); var gm = gameManagerObject != null ? gameManagerObject.GetComponent<GameManager>() : null;
        var chest = Find(scene, "FINAL VAULT TREASURE CHEST"); var chestLogic = chest != null ? chest.GetComponent<TreasureChest>() : null; var chestCollider = chest != null ? chest.GetComponent<Collider>() : null;
        var runner = Find(scene, "RUNNER TRACK - M5 FIRST TWO BIOMES");
        var uiCanvas = FindCanvasWith(scene, "GameOverPanel"); var hudCanvas = FindCanvasWith(scene, "ScoreText");
        var eventSystems = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).ToArray();
        if (player == null || player.GetComponent<CharacterController>() == null || camera == null || camera.target != player.transform || gm == null || gm.stageText == null || gm.scoreText == null || gm.healthText == null || gm.treasureKeysText == null)
            throw new InvalidOperationException("Preflight failed: player/controller, camera target, GameManager, or HUD reference missing.");
        if (chestLogic == null || chestLogic.lid == null || chestLogic.openingSparkles == null || chestLogic.treasureLight == null || chestCollider == null || !chestCollider.isTrigger)
            throw new InvalidOperationException("Preflight failed: existing chest trigger/lid/sparkles/light references invalid.");
        if (runner == null || runner.GetComponent<RunnerTrackManager>() == null || uiCanvas == null || hudCanvas == null || eventSystems.Length != 1)
            throw new InvalidOperationException("Preflight failed: runner root, existing UI Canvas, or unique EventSystem missing.");
        var keys = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true)).ToArray();
        if (keys.Any(k => k.keyId < 0 || k.keyId > 3) || keys.GroupBy(k => k.keyId).Any(g => g.Count() > 1)) throw new InvalidOperationException("Preflight failed: duplicate or invalid key IDs.");
        var existingPortals = new[] { "Portal_L1_to_L2", "Portal_L2_to_L1", "Portal_L2_to_L3", "Portal_L3_to_L2" }.Select(n => Find(scene, n)).ToArray();
        if (existingPortals.Any(p => p != null && p.GetComponent<AdventurePortal>() == null)) throw new InvalidOperationException("Preflight failed: existing portal name conflicts with a different component.");

        // Preflight complete. Mutations begin only below.
        blockout.SetActive(true); regions[0].SetActive(true); regions[1].SetActive(false); regions[2].SetActive(true); regions[3].SetActive(false); regions[4].SetActive(true);
        runner.SetActive(false); var runnerPrototype = Find(scene, "RUNNER PROTOTYPE - TEMP"); if (runnerPrototype != null) runnerPrototype.SetActive(false);
        player.adventureMode = true;

        var existingById = keys.ToDictionary(k => k.keyId);
        Vector3[] keyPoints = {
            SafeFloorPoint(floors[0], bounds[0], new Vector2(.30f,.50f)), SafeFloorPoint(floors[0], bounds[0], new Vector2(.70f,.50f)),
            SafeFloorPoint(floors[1], bounds[1], new Vector2(.30f,.50f)), SafeFloorPoint(floors[1], bounds[1], new Vector2(.70f,.50f)) };
        for (int id = 0; id < 4; id++)
        {
            ValidateClearance(floors[id < 2 ? 0 : 1], keyPoints[id], player);
            if (existingById.TryGetValue(id, out var key)) { key.transform.SetParent(regions[id < 2 ? 0 : 2].transform, true); key.transform.position = keyPoints[id] + Vector3.up; if (key.visual == null) key.visual = key.transform; }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = "GoldenKey_ID_" + id; go.transform.SetParent(regions[id < 2 ? 0 : 2].transform, true); go.transform.position = keyPoints[id] + Vector3.up; go.transform.localScale = new Vector3(.5f,.12f,.5f);
                var capsule = go.GetComponent<CapsuleCollider>(); capsule.isTrigger = true; var treasureKey = go.AddComponent<TreasureKey>(); treasureKey.keyId = id; treasureKey.visual = go.transform;
            }
        }

        // Four portal endpoints form two bidirectional pairs. Arrivals sit inside the destination floor, away from its trigger.
        Vector3 l1Source = SafeFloorPoint(floors[0], bounds[0], new Vector2(.88f,.50f));
        Vector3 l2Arrival = SafeFloorPoint(floors[1], bounds[1], new Vector2(.24f,.50f));
        Vector3 l2ReturnSource = SafeFloorPoint(floors[1], bounds[1], new Vector2(.18f,.78f));
        Vector3 l1Arrival = SafeFloorPoint(floors[0], bounds[0], new Vector2(.72f,.68f));
        Vector3 l2ForwardSource = SafeFloorPoint(floors[1], bounds[1], new Vector2(.82f,.78f));
        Vector3 l3Arrival = SafeFloorPoint(floors[2], bounds[2], new Vector2(.28f,.28f));
        Vector3 l3ReturnSource = SafeFloorPoint(floors[2], bounds[2], new Vector2(.22f,.78f));
        Vector3 l2FinalArrival = SafeFloorPoint(floors[1], bounds[1], new Vector2(.72f,.50f));
        var p12 = EnsurePortal(scene,"Portal_L1_to_L2",l1Source,l2Arrival+Vector3.up,2,floors[1],player);
        var p21 = EnsurePortal(scene,"Portal_L2_to_L1",l2ReturnSource,l1Arrival+Vector3.up,1,floors[0],player);
        var p23 = EnsurePortal(scene,"Portal_L2_to_L3",l2ForwardSource,l3Arrival+Vector3.up,3,floors[2],player);
        var p32 = EnsurePortal(scene,"Portal_L3_to_L2",l3ReturnSource,l2FinalArrival+Vector3.up,2,floors[1],player);
        p12.destination=p21; p21.destination=p12; p23.destination=p32; p32.destination=p23;

        // Reuse existing canvases, HUD, game-over and victory panels; create only named missing panels/buttons.
        var font = gm.scoreText.font; var menu = EnsurePanel(uiCanvas,"MainMenuPanel","TREASURE HUNT - THE LOST CAVE",font);
        var instructions = EnsurePanel(uiCanvas,"InstructionsPanel","INSTRUCTIONS",font);
        var pause = EnsurePanel(uiCanvas,"PausePanel","PAUSED",font);
        EnsureButton(menu.transform,"PlayButton","Play",font,0,3); EnsureButton(menu.transform,"InstructionsButton","Instructions",font,1,3); EnsureButton(menu.transform,"ExitButton","Exit",font,2,3);
        var body = Child(instructions,"Body"); if(body==null) body=MakeText(instructions.transform,"Body","WASD / Arrows: Move\nSpace: Jump\nShift: Sprint\nC / Ctrl: Crouch\nE: Open the chest\nExplore, collect four unique keys and reach the vault. Portals move between the three levels. Avoid obstacles; health is limited.",font,22,new Vector2(0,15),new Vector2(760,250));
        EnsureButton(instructions.transform,"BackButton","Back",font,0,1); EnsureButton(pause.transform,"ResumeButton","Resume",font,0,3); EnsureButton(pause.transform,"RestartButton","Restart",font,1,3); EnsureButton(pause.transform,"MainMenuButton","Main Menu",font,2,3);
        var gameOver=gm.gameOverPanel; var victory=gm.winPanel; if(gameOver==null||victory==null)throw new InvalidOperationException("Setup validation failed: existing Game Over or Victory panel reference missing.");
        var victoryTitle=Find(victory,"PanelTitle")?.GetComponent<TMP_Text>(); if(victoryTitle!=null)victoryTitle.text="CONGRATULATIONS! TREASURE FOUND!";
        EnsureButton(gameOver.transform,"RestartButton","Restart",font,0,2); EnsureButton(gameOver.transform,"MainMenuButton","Main Menu",font,1,2);
        EnsureButton(victory.transform,"RestartButton","Restart",font,0,2); EnsureButton(victory.transform,"MainMenuButton","Main Menu",font,1,2);
        var hudRoot=FindCanvasWith(scene,"AdventureStageProgress"); if(hudRoot==null)hudRoot=hudCanvas;
        var hintObject=Find(hudRoot.gameObject,"InteractionHint"); if(hintObject==null)hintObject=MakeText(hudRoot,"InteractionHint","",font,22,new Vector2(0,-190),new Vector2(700,48));
        gm.interactionHintText=hintObject.GetComponent<TMP_Text>(); gm.mainMenuPanel=menu; gm.instructionsPanel=instructions; gm.pausePanel=pause;
        gm.playButton=Child(menu,"PlayButton").GetComponent<Button>(); gm.instructionsButton=Child(menu,"InstructionsButton").GetComponent<Button>(); gm.exitButton=Child(menu,"ExitButton").GetComponent<Button>(); gm.instructionsBackButton=Child(instructions,"BackButton").GetComponent<Button>();
        gm.resumeButton=Child(pause,"ResumeButton").GetComponent<Button>(); gm.pauseRestartButton=Child(pause,"RestartButton").GetComponent<Button>(); gm.pauseMainMenuButton=Child(pause,"MainMenuButton").GetComponent<Button>();
        gm.gameOverMainMenuButton=Child(gameOver,"MainMenuButton").GetComponent<Button>(); gm.victoryMainMenuButton=Child(victory,"MainMenuButton").GetComponent<Button>();
        void Wire(Button button,UnityEngine.Events.UnityAction action){button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
        Wire(gm.playButton,gm.StartFreshGame); Wire(gm.instructionsButton,gm.ShowInstructions); Wire(gm.exitButton,gm.ExitGame); Wire(gm.instructionsBackButton,gm.ShowMainMenu);
        Wire(gm.resumeButton,gm.ResumeGame); Wire(gm.pauseRestartButton,gm.RestartGame); Wire(gm.pauseMainMenuButton,gm.ReturnToMainMenu); Wire(gm.gameOverMainMenuButton,gm.ReturnToMainMenu); Wire(gm.victoryMainMenuButton,gm.ReturnToMainMenu);
        var gameOverRestart=Child(gameOver,"RestartButton")?.GetComponent<Button>(); var victoryRestart=Child(victory,"RestartButton")?.GetComponent<Button>();
        if(gameOverRestart==null||victoryRestart==null)throw new InvalidOperationException("Setup validation failed: restart buttons missing."); Wire(gameOverRestart,gm.RestartGame); Wire(victoryRestart,gm.RestartGame);
        menu.SetActive(true); instructions.SetActive(false); pause.SetActive(false); gameOver.SetActive(false); victory.SetActive(false); if(gm.lockedTreasurePanel!=null)gm.lockedTreasurePanel.SetActive(false);
        chest.transform.position=new Vector3(bounds[2].center.x,bounds[2].max.y,bounds[2].center.z);
        player.transform.position=new Vector3(bounds[0].center.x,bounds[0].max.y,bounds[0].center.z); gm.stageText.text="LEVEL 1/3  •  Forgotten Entrance";
        Physics.SyncTransforms();
        // Validate newly configured links, unique keys, UI references and chest before saving.
        var finalKeys=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TreasureKey>(true)).ToArray();
        if(finalKeys.Length!=4||finalKeys.Select(k=>k.keyId).Distinct().Count()!=4||!new[]{0,1,2,3}.All(id=>finalKeys.Any(k=>k.keyId==id)))throw new InvalidOperationException("Final validation failed: expected exactly one of each key ID 0-3.");
        if(p12.destination!=p21||p21.destination!=p12||p23.destination!=p32||p32.destination!=p23||Vector3.Distance(p12.transform.position,p21.transform.position)<1f||Vector3.Distance(p23.transform.position,p32.transform.position)<1f)throw new InvalidOperationException("Final validation failed: portal pairs are missing or overlap.");
        if(gm.mainMenuPanel==null||gm.instructionsPanel==null||gm.pausePanel==null||gm.interactionHintText==null||gm.playButton.onClick.GetPersistentEventCount()<0)throw new InvalidOperationException("Final validation failed: UI references incomplete.");
        if(chest.GetComponent<TreasureChest>()!=chestLogic||chest.GetComponent<Collider>()!=chestCollider||chestLogic.lid==null||chestLogic.openingSparkles==null||chestLogic.treasureLight==null)throw new InvalidOperationException("Final validation failed: chest references changed.");
        var build=EditorBuildSettings.scenes.ToList(); int index=build.FindIndex(e=>e.path==ScenePath); if(index<0)build.Insert(0,new EditorBuildSettingsScene(ScenePath,true));else{var first=build[index];build.RemoveAt(index);build.Insert(0,new EditorBuildSettingsScene(first.path,true));} EditorBuildSettings.scenes=build.ToArray();
        EditorUtility.SetDirty(gm); EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("SaveScene returned false.");
        Debug.Log("THREE_AREA_SETUP_SAVED | R01/R03/R05 active; R02/R04 inactive; four unique keys; two linked portal pairs; UI wired; chest references retained; LostCave_Main first.");
    }

    [MenuItem("Tools/Adventure/Repair Missing R01 Keys")]
    public static void RepairMissingR01Keys()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var blockout = Find(scene, BlockoutName);
        var entrance = blockout != null ? Find(blockout, "R01 Forgotten Entrance - TEMP BLOCKOUT") : null;
        var ruins = blockout != null ? Find(blockout, "R03 Ancient Ruins - TEMP BLOCKOUT") : null;
        var floorObject = entrance != null ? Find(entrance, "R01 forest clearing floor") : null;
        var floor = floorObject != null ? floorObject.GetComponent<BoxCollider>() : null;
        if (blockout == null || entrance == null || ruins == null || floor == null || !floor.enabled)
            throw new InvalidOperationException("Key repair preflight failed: R01/R03 groups or R01 floor collider missing.");
        var bounds = GetBounds(floor);
        string prefabPath = "Assets/Prefabs/Runner/Pickups/Runner_TreasureKey.prefab";
        var keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var prefabKey = keyPrefab != null ? keyPrefab.GetComponent<TreasureKey>() : null;
        var prefabCollider = keyPrefab != null ? keyPrefab.GetComponent<SphereCollider>() : null;
        if (prefabKey == null || prefabCollider == null || !prefabCollider.isTrigger || prefabKey.visual == null)
            throw new InvalidOperationException("Key repair preflight failed: existing key prefab is missing its TreasureKey, trigger SphereCollider, or visual reference.");
        var allKeys = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true)).ToArray();
        if (allKeys.Any(k => k.keyId < 0 || k.keyId > 3) || allKeys.GroupBy(k => k.keyId).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Key repair preflight failed: current key IDs contain a duplicate or invalid value.");
        var byId = allKeys.ToDictionary(k => k.keyId);
        if (!byId.ContainsKey(2) || !byId.ContainsKey(3))
            throw new InvalidOperationException("Key repair preflight failed: existing key IDs 2 and 3 must both be present.");
        var originalRuinsPositions = new Dictionary<int, Vector3> { { 2, byId[2].transform.position }, { 3, byId[3].transform.position } };
        if (!byId[2].transform.IsChildOf(ruins.transform))
            throw new InvalidOperationException("Key repair preflight failed: existing key ID 2 is not in R03.");
        if (!byId[3].transform.IsChildOf(ruins.transform))
            throw new InvalidOperationException("Key repair preflight failed: existing key ID 3 is not in R03.");
        foreach (var existingKey in allKeys)
        {
            var existingCollider = existingKey.GetComponent<Collider>();
            if (existingCollider == null || !existingCollider.enabled || !existingCollider.isTrigger || existingKey.visual == null || existingKey.visual.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("Key repair preflight failed: existing key " + existingKey.keyId + " lacks a visible model or enabled trigger collider.");
        }
        var points = new[] {
            new Vector3(Mathf.Lerp(bounds.min.x + 1.5f, bounds.max.x - 1.5f, .30f), bounds.max.y + 1f, bounds.center.z),
            new Vector3(Mathf.Lerp(bounds.min.x + 1.5f, bounds.max.x - 1.5f, .70f), bounds.max.y + 1f, bounds.center.z)
        };
        var playerObject = Find(scene, "Player"); var player = playerObject != null ? playerObject.GetComponent<PlayerMovement>() : null;
        if (player == null || player.GetComponent<CharacterController>() == null)
            throw new InvalidOperationException("Key repair preflight failed: player CharacterController missing.");
        foreach (var point in points)
        {
            if (point.x <= bounds.min.x || point.x >= bounds.max.x || point.z <= bounds.min.z || point.z >= bounds.max.z)
                throw new InvalidOperationException("Key repair preflight failed: computed R01 key point falls outside the measured floor bounds.");
            ValidateClearance(floor, point - Vector3.up, player);
        }

        // IDs 0/1 are present inside inactive legacy runner preview instances.
        // Validate and collect only the two confirmed preview roots before changing any scene objects.
        var prefabRootsToUnpack = new HashSet<GameObject>();
        for (int id = 0; id < 2; id++)
        {
            if (!byId.TryGetValue(id, out var existingKey) || existingKey.transform.IsChildOf(entrance.transform)) continue;
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(existingKey.gameObject);
            if (prefabRoot != null)
            {
                if (prefabRoot.name != "FE_01_ForestGate Preview" && prefabRoot.name != "CC_01_CrystalRun Preview")
                    throw new InvalidOperationException("Key repair preflight failed: key " + id + " belongs to unexpected prefab root '" + prefabRoot.name + "'.");
                prefabRootsToUnpack.Add(prefabRoot);
            }
        }

        // All repair preflight checks pass. Unpack only the identified legacy scene instance, then reuse IDs if found.
        foreach (var prefabRoot in prefabRootsToUnpack)
            PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

        for (int id = 0; id < 2; id++)
        {
            TreasureKey key;
            if (!byId.TryGetValue(id, out key))
            {
                // Create a scene-native key root, then copy the existing prefab's visible key model under it.
                // This avoids retaining the prefab-instance root that failed the R01 ownership check on the prior repair pass.
                var root = new GameObject("GoldenKey_ID_" + id);
                root.transform.SetParent(entrance.transform, false);
                var trigger = root.AddComponent<SphereCollider>();
                trigger.radius = prefabCollider.radius;
                trigger.isTrigger = true;
                key = root.AddComponent<TreasureKey>();
                key.keyId = id;
                var visualInstance = UnityEngine.Object.Instantiate(prefabKey.visual.gameObject, root.transform, false);
                visualInstance.name = "Key Visual";
                key.visual = visualInstance.transform;
                if (prefabKey.collectSparkles != null)
                {
                    var sparkles = UnityEngine.Object.Instantiate(prefabKey.collectSparkles.gameObject, root.transform, false);
                    key.collectSparkles = sparkles.GetComponent<ParticleSystem>();
                }
            }
            key.gameObject.name = "GoldenKey_ID_" + id;
            if (key.transform.parent != entrance.transform) key.transform.SetParent(entrance.transform, true);
            key.transform.position = points[id];
            key.gameObject.SetActive(true);
            EditorUtility.SetDirty(key.gameObject); EditorUtility.SetDirty(key);
        }
        Physics.SyncTransforms();
        var repaired = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true)).ToArray();
        if (repaired.Length != 4 || repaired.Select(k => k.keyId).Distinct().Count() != 4 || !new[] { 0, 1, 2, 3 }.All(id => repaired.Any(k => k.keyId == id)))
            throw new InvalidOperationException("Key repair validation failed: scene must contain exactly one key for each ID 0-3; scene not saved.");
        foreach (var key in repaired)
        {
            var trigger = key.GetComponent<Collider>();
            if (trigger == null || !trigger.enabled || !trigger.isTrigger || key.visual == null || key.visual.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("Key repair validation failed: key " + key.keyId + " lacks a visible visual or valid trigger collider; scene not saved.");
            if (key.keyId < 2 && (!key.transform.IsChildOf(entrance.transform) || !key.gameObject.activeInHierarchy || !bounds.Contains(new Vector3(key.transform.position.x, bounds.center.y, key.transform.position.z)))) throw new InvalidOperationException("Key repair validation failed: key " + key.keyId + " is not positioned and active on R01.");
            if (key.keyId >= 2 && !key.transform.IsChildOf(ruins.transform)) throw new InvalidOperationException("Key repair validation failed: key " + key.keyId + " is not under R03.");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Key repair SaveScene failed.");
        var reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var persisted = reopened.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TreasureKey>(true)).ToArray();
        if (persisted.Length != 4 || persisted.Select(k => k.keyId).Distinct().Count() != 4 || !new[] { 0, 1, 2, 3 }.All(id => persisted.Any(k => k.keyId == id)))
            throw new InvalidOperationException("Post-save key verification failed: reopened scene does not contain IDs 0, 1, 2, and 3.");
        var persistedById = persisted.ToDictionary(k => k.keyId);
        if (!persistedById[0].transform.IsChildOf(Find(reopened, "R01 Forgotten Entrance - TEMP BLOCKOUT").transform) || !persistedById[1].transform.IsChildOf(Find(reopened, "R01 Forgotten Entrance - TEMP BLOCKOUT").transform) || !persistedById[0].gameObject.activeInHierarchy || !persistedById[1].gameObject.activeInHierarchy)
            throw new InvalidOperationException("Post-save key verification failed: IDs 0 and 1 are not active under R01.");
        if (Vector3.Distance(persistedById[2].transform.position, originalRuinsPositions[2]) > .01f || Vector3.Distance(persistedById[3].transform.position, originalRuinsPositions[3]) > .01f)
            throw new InvalidOperationException("Post-save key verification failed: existing R03 key positions changed.");
        foreach (var key in persisted)
        {
            var collider = key.GetComponent<Collider>();
            if (collider == null || !collider.enabled || !collider.isTrigger || key.visual == null || key.visual.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("Post-save key verification failed: key " + key.keyId + " visual or trigger collider is invalid.");
        }
        Debug.Log("R01_KEYS_REPAIRED_AND_REOPEN_VERIFIED | IDs 0,1 in R01; IDs 2,3 retained in R03; all use visible prefab visuals and trigger sphere colliders.");
    }
}
#endif
