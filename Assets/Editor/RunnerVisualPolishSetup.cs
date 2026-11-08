#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class RunnerVisualPolishSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string OutputFolder = "Assets/VisualPolish";
    private const float LaneWidth = 3.5f;
    private const string VerificationSessionKey = "RUNNER_VISUAL_POLISH_TEST_PENDING";

    private sealed class Theme
    {
        public Material floor, stone, gold, lane, flame, cool;
        public GameObject decoration;
    }

    static RunnerVisualPolishSetup()
    {
        EditorApplication.playModeStateChanged -= VerificationPlayModeChanged;
        EditorApplication.playModeStateChanged += VerificationPlayModeChanged;
    }

    private static void VerificationPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(VerificationSessionKey, false))
        {
            SessionState.SetBool(VerificationSessionKey, false);
            new GameObject("Runner Visual Polish Play Mode Check").AddComponent<RunnerVisualPolishVerifier>();
        }
    }

    public static void FinishPlayModeVerification(bool passed, string details)
    {
        Debug.Log((passed ? "RUNNER_VISUAL_PLAYTEST_PASS | " : "RUNNER_VISUAL_PLAYTEST_FAIL | ") + details);
        if (!passed) Debug.LogError("Runner visual-polish Play Mode verification failed: " + details);
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 2);
    }

    [MenuItem("Tools/Runner Adventure/Run Three Stage Play Mode Verification")]
    public static void BeginPlayModeVerification()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode verification cannot begin while Unity is already entering or in Play Mode.");
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(!EditorSceneManager.GetActiveScene().isDirty, "Play Mode verification refused a dirty LostCave_Main scene.");
        SessionState.SetBool(VerificationSessionKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static T[] Components<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static GameObject Find(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            Transform found = transforms.FirstOrDefault(item => item.name == objectName);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        string name = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material EnsureMaterial(string name, Color color, Color emission, float smoothness)
    {
        string path = OutputFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = FindLitShader();
            Require(shader != null, "The active render pipeline Lit shader is unavailable; no materials were created.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindLitShader()
    {
        string shaderName = GraphicsSettings.currentRenderPipeline != null
            ? "Universal Render Pipeline/Lit"
            : "Standard";
        return Shader.Find(shaderName);
    }

    private static Mesh EnsureMesh(string name, Func<Mesh> factory)
    {
        string path = OutputFolder + "/" + name + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        mesh = factory();
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh MakeCrystalMesh()
    {
        const int sides = 8;
        var vertices = new List<Vector3> { new Vector3(0f, 1.25f, 0f) };
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float radius = i % 2 == 0 ? 0.36f : 0.29f;
            vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
        }
        int bottom = vertices.Count;
        vertices.Add(new Vector3(0f, -0.72f, 0f));
        var triangles = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            int a = 1 + i;
            int b = 1 + (i + 1) % sides;
            triangles.AddRange(new[] { 0, b, a, bottom, a, b });
        }
        var mesh = new Mesh { vertices = vertices.ToArray(), triangles = triangles.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh MakeKeyRingMesh()
    {
        const int around = 24, tube = 8;
        const float major = 0.27f, minor = 0.075f;
        var vertices = new Vector3[around * tube];
        var triangles = new int[around * tube * 6];
        for (int i = 0; i < around; i++)
        {
            float u = i * Mathf.PI * 2f / around;
            for (int j = 0; j < tube; j++)
            {
                float v = j * Mathf.PI * 2f / tube;
                float radius = major + minor * Mathf.Cos(v);
                vertices[i * tube + j] = new Vector3(radius * Mathf.Cos(u), radius * Mathf.Sin(u), minor * Mathf.Sin(v));
                int nextI = (i + 1) % around, nextJ = (j + 1) % tube;
                int index = (i * tube + j) * 6;
                int a = i * tube + j, b = nextI * tube + j, c = nextI * tube + nextJ, d = i * tube + nextJ;
                triangles[index] = a; triangles[index + 1] = b; triangles[index + 2] = d;
                triangles[index + 3] = b; triangles[index + 4] = c; triangles[index + 5] = d;
            }
        }
        var mesh = new Mesh { vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, bool boundaryCollider = false)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null && !boundaryCollider) UnityEngine.Object.DestroyImmediate(collider);
        return item;
    }

    private static void AddTorch(Transform parent, float x, float z, Material stone, Material flame, Color lightColor)
    {
        AddPrimitive(parent, "Torch stone base", PrimitiveType.Cylinder, new Vector3(x, 0.82f, z),
            new Vector3(0.18f, 0.82f, 0.18f), stone);
        GameObject fire = AddPrimitive(parent, "Torch emissive flame", PrimitiveType.Sphere, new Vector3(x, 1.85f, z),
            new Vector3(0.24f, 0.42f, 0.24f), flame);
        Light light = fire.AddComponent<Light>();
        light.type = LightType.Point; light.color = lightColor; light.intensity = 1.6f; light.range = 8f;
        light.shadows = LightShadows.None;
    }

    private static GameObject BuildDecoration(string assetName, int stage, Theme theme, GameObject treePrefab)
    {
        string path = OutputFolder + "/" + assetName + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject(assetName);
        // Continuous low rails follow the exact 20 m section length. They sit beyond
        // the outer lane centers and have harmless colliders, with no damage script.
        for (int side = -1; side <= 1; side += 2)
        {
            AddPrimitive(root.transform, "TRACK EDGE STONE", PrimitiveType.Cube,
                new Vector3(side * 3.88f, 0.52f, 10f), new Vector3(0.24f, 1.04f, 19.9f), theme.stone, true);
        }
        // Two restrained inlays divide the three 2.5 m lanes without covering them.
        for (int divider = -1; divider <= 1; divider += 2)
            AddPrimitive(root.transform, "LANE INLAY", PrimitiveType.Cube,
                new Vector3(divider * (LaneWidth * 0.5f), 0.025f, 10f), new Vector3(0.045f, 0.025f, 19.7f), theme.lane);

        if (stage == 0)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = 2.5f + i * 5f;
                    GameObject rock = AddPrimitive(root.transform, "CAVE WALL ROCK", PrimitiveType.Sphere,
                        new Vector3(side * (4.75f + (i % 2) * 0.35f), 0.4f + (i % 3) * 0.25f, z),
                        new Vector3(1.3f, 1.3f + (i % 2) * 0.5f, 2.4f), theme.stone);
                    rock.transform.localRotation = Quaternion.Euler(i * 9f, side * i * 13f, side * 6f);
                    AddPrimitive(root.transform, "CAVE STALAGMITE", PrimitiveType.Capsule,
                        new Vector3(side * 3.45f, 0.65f, z + 2f), new Vector3(0.33f, 0.9f, 0.33f), theme.stone);
                }
                AddTorch(root.transform, side * 3.52f, 5f, theme.stone, theme.flame, new Color(1f, 0.53f, 0.19f));
                AddTorch(root.transform, side * 3.52f, 15f, theme.stone, theme.flame, new Color(1f, 0.53f, 0.19f));
                if (treePrefab != null)
                {
                    GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
                    if (tree != null)
                    {
                        tree.name = "FOREST EDGE TREE"; tree.transform.SetParent(root.transform, false);
                        tree.transform.localPosition = new Vector3(side * 5.9f, 0f, 10f);
                        tree.transform.localRotation = Quaternion.Euler(0f, side * 12f, 0f);
                        tree.transform.localScale = Vector3.one * 0.72f;
                        foreach (Collider collider in tree.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                    }
                }
            }
        }
        else if (stage == 1)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = 3f + i * 7f;
                    AddPrimitive(root.transform, i == 1 ? "BROKEN RUINS PILLAR" : "RUINS PILLAR",
                        PrimitiveType.Cylinder, new Vector3(side * 3.43f, i == 1 ? 1.5f : 2.1f, z),
                        new Vector3(0.34f, i == 1 ? 1.45f : 2.05f, 0.34f), theme.stone);
                    AddPrimitive(root.transform, "MOSSY CAPITAL", PrimitiveType.Cube,
                        new Vector3(side * 3.43f, i == 1 ? 2.75f : 4.05f, z),
                        new Vector3(0.82f, 0.3f, 0.82f), theme.cool);
                }
                AddTorch(root.transform, side * 3.50f, 10f, theme.stone, theme.flame, new Color(1f, 0.72f, 0.39f));
            }
        }
        else
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = 2f + i * 8f;
                    AddPrimitive(root.transform, "VAULT COLUMN", PrimitiveType.Cylinder,
                        new Vector3(side * 3.48f, 2.1f, z), new Vector3(0.42f, 2.05f, 0.42f), theme.stone);
                    AddPrimitive(root.transform, "GOLD COLUMN BAND", PrimitiveType.Cylinder,
                        new Vector3(side * 3.48f, 2.0f, z), new Vector3(0.5f, 0.15f, 0.5f), theme.gold);
                    AddPrimitive(root.transform, "VAULT TREASURE PILE", PrimitiveType.Sphere,
                        new Vector3(side * 2.95f, 0.22f, z + 2.4f), new Vector3(0.65f, 0.35f, 0.65f), theme.gold);
                }
                AddTorch(root.transform, side * 3.48f, 10f, theme.stone, theme.flame, new Color(1f, 0.72f, 0.28f));
            }
        }

        GameObject gate = new GameObject("STAGE_GATE");
        gate.transform.SetParent(root.transform, false);
        for (int side = -1; side <= 1; side += 2)
            AddPrimitive(gate.transform, "ANCIENT GATE PIER", PrimitiveType.Cube,
                new Vector3(side * 3.45f, 2.25f, 0.6f), new Vector3(0.62f, 4.5f, 0.72f), theme.stone);
        AddPrimitive(gate.transform, "ANCIENT GATE LINTEL", PrimitiveType.Cube,
            new Vector3(0f, 4.45f, 0.6f), new Vector3(7.55f, 0.62f, 0.8f), theme.gold);
        gate.SetActive(false);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return saved;
    }

    private static void Layout(TMP_Text text, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, float fontSize,
        TextAlignmentOptions alignment)
    {
        if (text == null) return;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    private static void SetPolishedVisual(Transform root, string visualName, Mesh mesh, Material material, Vector3 scale)
    {
        Require(mesh != null && mesh.vertexCount > 0, "SetPolishedVisual requires a non-empty mesh for " + visualName + ".");
        Require(material != null, "SetPolishedVisual requires a material for " + visualName + ".");

        Transform existing = root.Find(visualName);
        GameObject visual = existing != null ? existing.gameObject : new GameObject(visualName);
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = scale;

        // Unity fake-null checks also catch components that were destroyed on an existing visual object.
        MeshFilter filter = visual.GetComponent<MeshFilter>();
        if (filter == null) filter = visual.AddComponent<MeshFilter>();
        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = visual.AddComponent<MeshRenderer>();
        Require(filter != null, "Could not obtain a MeshFilter for " + visualName + ".");
        Require(renderer != null, "Could not obtain a MeshRenderer for " + visualName + ".");

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
    }

    private static void PolishKey(TreasureKey key, Mesh ring, Material gold)
    {
        Transform oldVisual = key.visual;
        SetPolishedVisual(key.transform, "VISUAL_POLISH_GOLD_KEY", ring, gold, Vector3.one);
        Transform visual = key.transform.Find("VISUAL_POLISH_GOLD_KEY");
        if (oldVisual != null && oldVisual != visual)
        {
            foreach (Renderer renderer in oldVisual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
        // Vertical shaft and two teeth make the collectible read as a key at runner distance.
        AddVisualPrimitive(visual, "KEY SHAFT", PrimitiveType.Cylinder, new Vector3(0f, -0.47f, 0f), new Vector3(0.11f, 0.42f, 0.11f), gold);
        AddVisualPrimitive(visual, "KEY TOOTH A", PrimitiveType.Cube, new Vector3(0.15f, -0.72f, 0f), new Vector3(0.22f, 0.09f, 0.12f), gold);
        AddVisualPrimitive(visual, "KEY TOOTH B", PrimitiveType.Cube, new Vector3(0.11f, -0.57f, 0f), new Vector3(0.16f, 0.08f, 0.12f), gold);
        key.visual = visual;
        EditorUtility.SetDirty(key);
    }

    private static void AddVisualPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        if (parent.Find(name) != null) return;
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
        Collider collider = part.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = part.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
    }

    [MenuItem("Tools/Runner Adventure/Apply Visual Polish (Three Stages)")]
    public static void Configure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RunnerTrackManager manager = Components<RunnerTrackManager>(scene).SingleOrDefault();
        PlayerMovement player = Components<PlayerMovement>(scene).SingleOrDefault();
        CameraFollow camera = Components<CameraFollow>(scene).SingleOrDefault();
        GameManager game = Components<GameManager>(scene).SingleOrDefault();
        TreasureChest chest = Components<TreasureChest>(scene).SingleOrDefault();
        Canvas[] canvases = Components<Canvas>(scene);
        Canvas scoreCanvas = game != null && game.scoreText != null ? game.scoreText.canvas : null;
        Canvas stageCanvas = game != null && game.stageText != null ? game.stageText.canvas : null;
        Transform pickupRoot = Find(scene, "RUNNER_GAMEPLAY_PICKUPS")?.transform;
        RunnerTrackSection[] startup = manager != null && manager.sectionsContainer != null
            ? manager.sectionsContainer.GetComponentsInChildren<RunnerTrackSection>(true).Where(s => s.gameObject.activeInHierarchy).OrderBy(s => s.sequenceSlot).ToArray()
            : Array.Empty<RunnerTrackSection>();
        TreasureKey[] keys = Components<TreasureKey>(scene).Where(k => k.gameObject.activeInHierarchy).ToArray();
        Collectible[] crystals = Components<Collectible>(scene).Where(c => c.gameObject.activeInHierarchy && pickupRoot != null && c.transform.IsChildOf(pickupRoot)).ToArray();
        Shader litShader = FindLitShader();
        var route = new List<RunnerTrackSection>();
        if (manager != null && manager.forgottenEntranceSections != null)
            route.AddRange(manager.forgottenEntranceSections);
        if (manager != null) route.Add(manager.transitionToCrystalCaverns);
        if (manager != null) route.Add(manager.transitionToAncientRuins);
        if (manager != null && manager.ancientRuinsSections != null)
            route.AddRange(manager.ancientRuinsSections);
        if (manager != null) route.Add(manager.transitionToLostTemple);
        if (manager != null) route.Add(manager.transitionToFinalTreasureVault);
        if (manager != null && manager.finalTreasureVaultSections != null)
            route.AddRange(manager.finalTreasureVaultSections);

        // Full preflight completes before any scene or asset mutation.
        Require(manager != null && player != null && player.GetComponent<CharacterController>() != null && camera != null && game != null && chest != null,
            "Preflight failed: one of the existing runner, player, camera, GameManager or chest components is missing.");
        Require(Components<RunnerTrackManager>(scene).Length == 1 && Components<GameManager>(scene).Length == 1 &&
                Components<PlayerMovement>(scene).Length == 1 && Components<CameraFollow>(scene).Length == 1,
            "Preflight failed: duplicate core gameplay components exist.");
        Require(player.adventureMode == false && manager.player == player && camera.target == player.transform,
            "Preflight failed: the active scene is not configured as the saved three-lane runner.");
        Require(startup.Length == 4 && startup.Select(s => s.sequenceSlot).SequenceEqual(new[] { 0, 1, 2, 3 }),
            "Preflight failed: the four saved startup sections are missing or out of order.");
        foreach (RunnerTrackSection section in startup)
            Require(section.Validate(LaneWidth, out string error), "Preflight failed: " + error);
        Require(manager.forgottenEntranceSections != null && manager.forgottenEntranceSections.Length == 3 &&
                manager.ancientRuinsSections != null && manager.ancientRuinsSections.Length == 3 &&
                manager.finalTreasureVaultSections != null && manager.finalTreasureVaultSections.Length == 3 && route.Count == 13 &&
                route.All(section => section != null),
            "Preflight failed: one of the three stage prefab sets is incomplete.");
        float expectedLength = 0f;
        foreach (RunnerTrackSection section in route)
        {
            Require(section.Validate(LaneWidth, out string error), "Preflight failed: route " + error);
            Require(Mathf.Abs(section.sectionLength - 20f) < 0.01f,
                "Preflight failed: route section " + section.name + " is not a 20 metre module.");
            expectedLength += section.sectionLength;
        }
        Require(Mathf.Abs(expectedLength - 260f) < 0.01f,
            "Preflight failed: route module lengths do not reach the existing Z=260 finish.");
        Require(keys.Length == 4 && keys.Select(k => k.keyId).OrderBy(id => id).SequenceEqual(new[] { 0, 1, 2, 3 }),
            "Preflight failed: the active scene must have exactly one key for each ID 0-3.");
        foreach (TreasureKey key in keys)
        {
            float expectedStart = key.keyId < 2 ? 0f : 100f;
            float expectedEnd = key.keyId < 2 ? 100f : 200f;
            Require(key.GetComponent<Collider>() != null && key.GetComponent<Collider>().enabled && key.GetComponent<Collider>().isTrigger &&
                    key.visual != null && key.transform.position.x >= -LaneWidth && key.transform.position.x <= LaneWidth &&
                    key.transform.position.z >= expectedStart && key.transform.position.z < expectedEnd,
                "Preflight failed: key " + key.keyId + " lacks a reachable lane position, visual or trigger collider.");
        }
        Require(crystals.Length == 25 && crystals.All(c => c.GetComponent<Collider>() != null && c.GetComponent<Collider>().isTrigger),
            "Preflight failed: 25 active crystal trigger pickups are required before visual changes.");
        Require(chest.lid != null && chest.openingSparkles != null && chest.treasureLight != null &&
                chest.GetComponent<Collider>() != null && chest.GetComponent<Collider>().isTrigger && game.requiredTreasureKeys == 2,
            "Preflight failed: final chest collider or key requirement references are incomplete.");
        Require(game.startingHealth == 3 && game.scoreText != null && game.healthText != null && game.crystalCountText != null &&
                game.treasureKeysText != null && game.stageText != null && game.stageProgressText != null && game.stageProgressSlider != null &&
                scoreCanvas != null && stageCanvas != null && canvases.Length >= 1 && Components<UnityEngine.EventSystems.EventSystem>(scene).Length == 1,
            "Preflight failed: saved health or HUD references are incomplete.");
        Require(litShader != null, "Preflight failed: the active pipeline Lit shader is missing.");
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                    "Preflight failed: a missing script exists on " + child.name + ".");
        Require(EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path == ScenePath,
            "Preflight failed: LostCave_Main is not the first enabled Build Settings scene.");

        EnsureFolder(OutputFolder);
        var entrance = new Theme();
        entrance.floor = EnsureMaterial("M_FE_CaveFloor", new Color(0.20f, 0.17f, 0.14f), Color.black, 0.18f);
        entrance.stone = EnsureMaterial("M_FE_CaveStone", new Color(0.24f, 0.22f, 0.20f), Color.black, 0.12f);
        entrance.gold = EnsureMaterial("M_FE_WarmMetal", new Color(0.72f, 0.39f, 0.10f), new Color(0.25f, 0.08f, 0.01f), 0.62f);
        entrance.lane = EnsureMaterial("M_FE_LaneInlay", new Color(0.32f, 0.48f, 0.52f), new Color(0.03f, 0.16f, 0.19f), 0.55f);
        entrance.flame = EnsureMaterial("M_TorchFlame", new Color(1f, 0.35f, 0.06f), new Color(1f, 0.22f, 0.02f), 0.1f);
        entrance.cool = entrance.stone;
        var ruins = new Theme();
        ruins.floor = EnsureMaterial("M_AR_RuinsFloor", new Color(0.25f, 0.28f, 0.24f), Color.black, 0.22f);
        ruins.stone = EnsureMaterial("M_AR_RuinsStone", new Color(0.31f, 0.34f, 0.29f), Color.black, 0.2f);
        ruins.gold = EnsureMaterial("M_AR_Bronze", new Color(0.52f, 0.38f, 0.16f), new Color(0.10f, 0.035f, 0.005f), 0.55f);
        ruins.lane = EnsureMaterial("M_AR_LaneInlay", new Color(0.51f, 0.63f, 0.45f), new Color(0.08f, 0.13f, 0.04f), 0.5f);
        ruins.flame = entrance.flame;
        ruins.cool = EnsureMaterial("M_AR_Moss", new Color(0.20f, 0.31f, 0.22f), Color.black, 0.12f);
        var vault = new Theme();
        vault.floor = EnsureMaterial("M_FV_VaultFloor", new Color(0.14f, 0.12f, 0.10f), Color.black, 0.32f);
        vault.stone = EnsureMaterial("M_FV_VaultStone", new Color(0.21f, 0.19f, 0.16f), Color.black, 0.24f);
        vault.gold = EnsureMaterial("M_FV_TreasureGold", new Color(0.86f, 0.56f, 0.12f), new Color(0.30f, 0.13f, 0.01f), 0.72f);
        vault.lane = EnsureMaterial("M_FV_LaneInlay", new Color(0.78f, 0.57f, 0.25f), new Color(0.18f, 0.08f, 0.01f), 0.58f);
        vault.flame = EnsureMaterial("M_FV_GoldenFlame", new Color(1f, 0.62f, 0.12f), new Color(1f, 0.33f, 0.04f), 0.1f);
        vault.cool = vault.gold;
        Material keyGold = EnsureMaterial("M_Collectible_KeyGold", new Color(0.95f, 0.63f, 0.12f), new Color(0.36f, 0.16f, 0.015f), 0.82f);
        Material crystalBlue = EnsureMaterial("M_Collectible_CrystalBlue", new Color(0.10f, 0.68f, 0.95f), new Color(0.02f, 0.24f, 0.52f), 0.72f);
        Mesh crystalMesh = EnsureMesh("Crystal_DoublePyramid", MakeCrystalMesh);
        Mesh ringMesh = EnsureMesh("Key_Ring", MakeKeyRingMesh);
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab");
        entrance.decoration = BuildDecoration("RunnerDecoration_ForgottenEntrance", 0, entrance, treePrefab);
        ruins.decoration = BuildDecoration("RunnerDecoration_AncientRuins", 1, ruins, null);
        vault.decoration = BuildDecoration("RunnerDecoration_TreasureVault", 2, vault, null);
        AssetDatabase.SaveAssets();

        // Scene mutation starts only after successful preflight and asset creation.
        RunnerTrackVisuals visuals = manager.GetComponent<RunnerTrackVisuals>();
        if (visuals == null) visuals = Undo.AddComponent<RunnerTrackVisuals>(manager.gameObject);
        visuals.forgottenEntranceDecoration = entrance.decoration;
        visuals.ancientRuinsDecoration = ruins.decoration;
        visuals.treasureVaultDecoration = vault.decoration;
        visuals.forgottenEntranceFloor = entrance.floor;
        visuals.ancientRuinsFloor = ruins.floor;
        visuals.treasureVaultFloor = vault.floor;
        visuals.rockObstacleMaterial = entrance.stone;
        visuals.slideObstacleMaterial = entrance.gold;
        foreach (RunnerTrackSection section in Components<RunnerTrackSection>(scene).Where(s => s.gameObject.activeInHierarchy))
            visuals.RefreshSection(section);

        foreach (TreasureKey key in keys) PolishKey(key, ringMesh, keyGold);
        foreach (Collectible crystal in crystals)
        {
            SetPolishedVisual(crystal.transform, "VISUAL_POLISH_BLUE_CRYSTAL", crystalMesh, crystalBlue, Vector3.one);
            foreach (Renderer renderer in crystal.GetComponentsInChildren<Renderer>(true))
            {
                Transform polished = crystal.transform.Find("VISUAL_POLISH_BLUE_CRYSTAL");
                if (renderer.gameObject != polished.gameObject && !renderer.transform.IsChildOf(polished)) renderer.enabled = false;
            }
        }

        foreach (Canvas canvas in canvases)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }
        Layout(game.scoreText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(260f, 42f), 26f, TextAlignmentOptions.Left);
        Layout(game.crystalCountText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -72f), new Vector2(260f, 42f), 25f, TextAlignmentOptions.Left);
        Layout(game.healthText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(210f, 42f), 26f, TextAlignmentOptions.Right);
        Layout(game.treasureKeysText, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -72f), new Vector2(280f, 42f), 25f, TextAlignmentOptions.Right);
        Layout(game.stageText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(560f, 42f), 28f, TextAlignmentOptions.Center);
        Layout(game.stageProgressText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(220f, 34f), 22f, TextAlignmentOptions.Center);
        if (game.interactionHintText != null)
            Layout(game.interactionHintText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(800f, 48f), 26f, TextAlignmentOptions.Center);

        game.startingHealth = 3;
        game.damageCooldown = Mathf.Max(1f, game.damageCooldown);
        chest.treasureLight.color = new Color(1f, 0.67f, 0.22f);
        chest.treasureLight.intensity = 2.2f;
        chest.treasureLight.range = 9f;
        if (chest.openingSparkles != null)
        {
            ParticleSystem.MainModule particles = chest.openingSparkles.main;
            particles.startColor = new Color(1f, 0.66f, 0.12f, 1f);
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.28f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.14f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.06f, 0.055f);
        RenderSettings.ambientIntensity = 0.8f;

        EditorUtility.SetDirty(visuals); EditorUtility.SetDirty(manager); EditorUtility.SetDirty(game); EditorUtility.SetDirty(chest);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "SaveScene failed for LostCave_Main.");
        AssetDatabase.SaveAssets();
        Scene reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateReloadedScene(reopened);
        Debug.Log("RUNNER_VISUAL_POLISH_SAVED | stage-decorations=3 | lane-inlays=true | collectibles=4-keys+25-crystals | health=3 | HUD=anchored-scaled | scene-reopened=true");
    }

    private static void ValidateReloadedScene(Scene scene)
    {
        RunnerTrackManager manager = Components<RunnerTrackManager>(scene).SingleOrDefault();
        GameManager game = Components<GameManager>(scene).SingleOrDefault();
        RunnerTrackVisuals visuals = manager != null ? manager.GetComponent<RunnerTrackVisuals>() : null;
        TreasureKey[] keys = Components<TreasureKey>(scene).Where(k => k.gameObject.activeInHierarchy).ToArray();
        Canvas[] canvases = Components<Canvas>(scene);
        Require(manager != null && visuals != null && game != null && visuals.forgottenEntranceDecoration != null &&
                visuals.ancientRuinsDecoration != null && visuals.treasureVaultDecoration != null,
            "Reload validation failed: runner decoration references are incomplete.");
        Require(keys.Length == 4 && keys.Select(k => k.keyId).OrderBy(id => id).SequenceEqual(new[] { 0, 1, 2, 3 }),
            "Reload validation failed: active key IDs are not unique 0-3.");
        Require(Components<Collectible>(scene).Count(c => c.gameObject.activeInHierarchy && c.transform.Find("VISUAL_POLISH_BLUE_CRYSTAL") != null) == 25,
            "Reload validation failed: crystal visuals did not persist.");
        Require(game.startingHealth == 3 && game.damageCooldown >= 1f && canvases.Length >= 1 &&
                game.scoreText != null && game.healthText != null && game.treasureKeysText != null && game.stageText != null &&
                game.crystalCountText != null && game.stageProgressText != null && game.stageProgressSlider != null,
            "Reload validation failed: health or HUD references did not persist.");
        foreach (RunnerTrackSection section in Components<RunnerTrackSection>(scene).Where(s => s.gameObject.activeInHierarchy))
            Require(section.transform.Find("RUNNER_ENVIRONMENT_DECORATION") != null,
                "Reload validation failed: startup track section " + section.sequenceSlot + " has no decoration.");
        Require(EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path == ScenePath,
            "Reload validation failed: LostCave_Main is not first in Build Settings.");
    }
}
#endif
