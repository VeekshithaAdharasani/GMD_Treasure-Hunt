using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Milestone3BlockoutBuilder
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string WorldName = "WORLD BLOCKOUT - TEMP (replace during art pass)";
    private static Scene scene;
    private static Transform world;
    private static Material[] materials;
    private static Dictionary<string, TransformSnapshot> protectedObjects;

    private sealed class TransformSnapshot
    {
        public Transform Transform;
        public Vector3 Position;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
        public Quaternion Rotation;
        public Transform Parent;
    }

    [MenuItem("Tools/World Blockout/Build Five Region Blockout")]
    public static void BuildAndSave()
    {
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (FindSceneObject(WorldName) != null)
                throw new InvalidOperationException("Temporary world blockout root already exists; refusing to duplicate it.");

            VerifyExistingSceneReferences();
            CaptureProtectedObjects();
            CreateMaterials();
            world = new GameObject(WorldName).transform;
            world.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            world.localScale = Vector3.one;

            BuildRegion1();
            SaveAndVerify("R1", "R01 Forgotten Entrance - TEMP BLOCKOUT");

            BuildRegion2();
            SaveAndVerify("R2", "R02 Crystal Caverns - TEMP BLOCKOUT");

            BuildRegion3();
            SaveAndVerify("R3", "R03 Ancient Ruins - TEMP BLOCKOUT");

            BuildRegion4();
            SaveAndVerify("R4", "R04 Underground Temple - TEMP BLOCKOUT");

            BuildRegion5();
            SaveAndVerify("R5", "R05 Final Treasure Vault - TEMP BLOCKOUT");

            AssetDatabase.SaveAssets();
            VerifyExistingSceneReferences();
            VerifyProtectedObjects();
            Debug.Log("M3_BLOCKOUT_COMPLETE | scene=LostCave_Main | existing transforms and references preserved | regions=5");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/World Blockout/Frame Five Region Layout")]
    public static void OpenLayoutForReview()
    {
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = FindSceneObject(WorldName);
        if (root == null)
            throw new InvalidOperationException("Five-region blockout root is missing.");

        Selection.activeGameObject = root;
        EditorApplication.delayCall += () =>
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
                return;
            view.orthographic = true;
            view.LookAt(new Vector3(80f, 0f, 52f), Quaternion.Euler(90f, 0f, 0f), 205f);
            view.Repaint();
        };
    }
    public static void RefineBlockoutConnections()
    {
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (FindSceneObject(WorldName) == null)
                throw new InvalidOperationException("World blockout root is missing.");
            VerifyExistingSceneReferences();
            CaptureProtectedObjects();
            CreateMaterials();

            Transform divider = FindSceneObject("TEMP R03 side court divider fragment").transform;
            divider.position = new Vector3(100f, 1.5f, 118f);

            Transform westVaultWall = FindSceneObject("TEMP R05 outer vault wall west fragment").transform;
            westVaultWall.position = new Vector3(143f, 2.5f, 62f);
            Transform eastVaultWall = FindSceneObject("TEMP R05 outer vault wall east fragment").transform;
            eastVaultWall.position = new Vector3(201f, 2.5f, 62f);

            Transform treasurePlatform = FindSceneObject("R05 central treasure platform - BLOCKOUT ONLY").transform;
            treasurePlatform.localPosition = new Vector3(172f, 0.12f, 39f);
            treasurePlatform.localScale = new Vector3(16f, 0.24f, 14f);

            Transform region2 = FindSceneObject("R02 Crystal Caverns - TEMP BLOCKOUT").transform;
            if (FindSceneObject("TEMP R02 entry threshold left post") == null)
                Arch(region2, "R02 entry threshold", new Vector3(0f, 0f, 60f), 10f, materials[1], 0f);

            Transform region5 = FindSceneObject("R05 Final Treasure Vault - TEMP BLOCKOUT").transform;
            if (FindSceneObject("R05 northeast vault loop bridge") == null)
                Floor(region5, "R05 northeast vault loop bridge", new Vector3(205f, -0.3f, 80f), 8f, 8f, materials[4]);

            SaveAndVerify("connections", "R05 Final Treasure Vault - TEMP BLOCKOUT");
            Debug.Log("M3_CONNECTION_REFINEMENTS_COMPLETE | floor routes connected | protected transforms unchanged");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            GameObject found = FindRecursive(rootObject.transform, objectName);
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

    private static void VerifyExistingSceneReferences()
    {
        GameObject player = FindSceneObject("Player");
        GameObject playerModel = FindSceneObject("PlayerModel");
        GameObject cameraObject = FindSceneObject("Main Camera");
        GameObject managerObject = FindSceneObject("GameManager");
        GameObject restartObject = FindSceneObject("RestartButton");

        if (player == null || player.GetComponent<CharacterController>() == null)
            throw new InvalidOperationException("Existing Player or CharacterController reference is missing.");
        if (playerModel == null || playerModel.transform.parent != player.transform)
            throw new InvalidOperationException("Existing PlayerModel parent reference is missing.");
        CameraFollow cameraFollow = cameraObject != null ? cameraObject.GetComponent<CameraFollow>() : null;
        if (cameraFollow == null || cameraFollow.target != player.transform)
            throw new InvalidOperationException("Existing Main Camera target reference does not resolve to Player.");

        GameManager manager = managerObject != null ? managerObject.GetComponent<GameManager>() : null;
        if (manager == null || manager.scoreText == null || manager.healthText == null || manager.gameOverPanel == null)
            throw new InvalidOperationException("Existing GameManager UI references are incomplete.");
        if (restartObject == null)
            throw new InvalidOperationException("Existing RestartButton is missing.");
        Button restartButton = restartObject.GetComponent<Button>();
        if (restartButton == null || restartButton.onClick.GetPersistentEventCount() == 0 ||
            restartButton.onClick.GetPersistentTarget(0) == null ||
            restartButton.onClick.GetPersistentMethodName(0) != "Restart")
            throw new InvalidOperationException("Existing RestartButton callback does not resolve.");

        string[] requiredObjects =
        {
            "tree_1_right", "tree_1", "grass01", "Cave_Path",
            "Cave_Wall_Left", "Cave_Wall_Right", "Cave_Roof",
            "Arch_Top_01", "Arch_Pillar_Left_01", "Arch_Pillar_Right_01",
            "Crystal_Blue_01", "Crystal_Blue_02", "Crystal_Blue_03", "Crystal_Gold_01",
            "RockObstacle_01", "TreasureChest_Body", "TreasureChest_Lid"
        };
        foreach (string objectName in requiredObjects)
        {
            if (FindSceneObject(objectName) == null)
                throw new InvalidOperationException("Required existing object not found: " + objectName);
        }
    }

    private static void CaptureProtectedObjects()
    {
        string[] names =
        {
            "tree_1_right", "tree_1", "grass01", "Cave_Path",
            "Cave_Wall_Left", "Cave_Wall_Right", "Cave_Roof",
            "Arch_Top_01", "Arch_Pillar_Left_01", "Arch_Pillar_Right_01",
            "Crystal_Blue_01", "Crystal_Blue_02", "Crystal_Blue_03", "Crystal_Gold_01",
            "RockObstacle_01", "TreasureChest_Body", "TreasureChest_Lid",
            "Player", "PlayerModel", "Main Camera", "GameManager"
        };
        protectedObjects = new Dictionary<string, TransformSnapshot>();
        foreach (string objectName in names)
        {
            GameObject gameObject = FindSceneObject(objectName);
            if (gameObject == null)
                throw new InvalidOperationException("Cannot capture protected transform: " + objectName);
            Transform t = gameObject.transform;
            protectedObjects.Add(objectName, new TransformSnapshot
            {
                Transform = t,
                Position = t.position,
                LocalPosition = t.localPosition,
                LocalScale = t.localScale,
                Rotation = t.rotation,
                Parent = t.parent
            });
        }
    }

    private static void VerifyProtectedObjects()
    {
        foreach (KeyValuePair<string, TransformSnapshot> pair in protectedObjects)
        {
            TransformSnapshot snapshot = pair.Value;
            if (snapshot.Transform == null ||
                Vector3.Distance(snapshot.Position, snapshot.Transform.position) > 0.0001f ||
                Vector3.Distance(snapshot.LocalPosition, snapshot.Transform.localPosition) > 0.0001f ||
                Vector3.Distance(snapshot.LocalScale, snapshot.Transform.localScale) > 0.0001f ||
                Quaternion.Angle(snapshot.Rotation, snapshot.Transform.rotation) > 0.001f ||
                snapshot.Parent != snapshot.Transform.parent)
            {
                throw new InvalidOperationException("Protected existing transform changed: " + pair.Key);
            }
        }
    }

    private static void CreateMaterials()
    {
        const string folder = "Assets/Materials/Blockout_Temp";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Materials", "Blockout_Temp");

        Color[] colors =
        {
            new Color(0.39f, 0.52f, 0.35f, 1f),
            new Color(0.25f, 0.53f, 0.69f, 1f),
            new Color(0.66f, 0.43f, 0.27f, 1f),
            new Color(0.37f, 0.47f, 0.58f, 1f),
            new Color(0.77f, 0.59f, 0.25f, 1f)
        };
        string[] names = { "R01_Forest", "R02_Crystal", "R03_Ruins", "R04_Temple", "R05_Vault" };
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP/Lit shader is unavailable; refusing to create pink blockout materials.");

        materials = new Material[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = folder + "/TEMP_Blockout_" + names[i] + ".mat";
            materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (materials[i] == null)
            {
                Material material = new Material(shader);
                material.name = "TEMP_Blockout_" + names[i];
                material.SetColor("_BaseColor", colors[i]);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.18f);
                AssetDatabase.CreateAsset(material, path);
                materials[i] = material;
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static Transform CreateRegion(string name)
    {
        GameObject region = new GameObject(name);
        region.transform.SetParent(world, false);
        region.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        region.transform.localScale = Vector3.one;
        return region.transform;
    }

    private static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material material, float yaw = 0f)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = center;
        block.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        block.transform.localScale = size;
        MeshRenderer renderer = block.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return block;
    }

    private static void Floor(Transform parent, string name, Vector3 center, float width, float depth, Material material)
    {
        Box(parent, name, center, new Vector3(width, 0.6f, depth), material);
    }

    private static void Wall(Transform parent, string name, Vector3 center, Vector3 size, Material material, float yaw = 0f)
    {
        Box(parent, "TEMP " + name, center, size, material, yaw);
    }

    private static void Arch(Transform parent, string name, Vector3 position, float width, Material material, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = rotation * Vector3.right;
        Vector3 leftPost = position - right * (width * 0.5f);
        Vector3 rightPost = position + right * (width * 0.5f);
        leftPost.y = 2.4f;
        rightPost.y = 2.4f;
        Box(parent, "TEMP " + name + " left post", leftPost, new Vector3(0.8f, 4.8f, 1.1f), material, yaw);
        Box(parent, "TEMP " + name + " right post", rightPost, new Vector3(0.8f, 4.8f, 1.1f), material, yaw);
        Vector3 lintel = position + Vector3.up * 4.6f;
        Box(parent, "TEMP " + name + " lintel", lintel, new Vector3(width + 0.8f, 0.8f, 1.1f), material, yaw);
    }

    private static void Pillar(Transform parent, string name, Vector3 center, float width, float height, Material material)
    {
        Vector3 position = new Vector3(center.x, height * 0.5f, center.z);
        Box(parent, name, position, new Vector3(width, height, width), material);
    }

    private static void SaveAndVerify(string label, string expectedRegionRoot)
    {
        VerifyProtectedObjects();
        VerifyExistingSceneReferences();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Unity failed to save the scene after " + label + ".");

        string savedScene = File.ReadAllText(Path.GetFullPath(scene.path));
        if (!savedScene.Contains("m_Name: " + expectedRegionRoot) ||
            !savedScene.Contains("m_Name: " + WorldName))
            throw new IOException("Saved scene verification failed after " + label + ".");

        Transform region = FindSceneObject(expectedRegionRoot).transform;
        Debug.Log("M3_REGION_SAVED | " + label + " | direct_blockout_objects=" + region.childCount +
                  " | protected_positions=unchanged | core_references=resolved");
    }

    private static void BuildRegion1()
    {
        Transform region = CreateRegion("R01 Forgotten Entrance - TEMP BLOCKOUT");
        Floor(region, "R01 forest clearing floor", new Vector3(0f, -0.3f, -16f), 44f, 32f, materials[0]);
        Floor(region, "R01 west exploration pocket", new Vector3(-31f, -0.3f, -16f), 18f, 18f, materials[0]);
        Floor(region, "R01 west pocket connection", new Vector3(-23f, -0.3f, -16f), 8f, 10f, materials[0]);
        Floor(region, "R01 east grove terrace", new Vector3(31f, -0.3f, -16f), 18f, 20f, materials[0]);
        Floor(region, "R01 east terrace connection", new Vector3(22f, -0.3f, -8f), 10f, 16f, materials[0]);

        Wall(region, "R01 far forest marker A", new Vector3(-12f, 1.2f, -31.5f), new Vector3(12f, 2.4f, 0.8f), materials[0]);
        Wall(region, "R01 far forest marker B", new Vector3(12f, 1.2f, -31.5f), new Vector3(12f, 2.4f, 0.8f), materials[0]);
        Wall(region, "R01 west boundary fragment", new Vector3(-22.5f, 1.7f, -22f), new Vector3(0.8f, 3.4f, 14f), materials[0]);
        Wall(region, "R01 east boundary fragment", new Vector3(22.5f, 1.7f, -23f), new Vector3(0.8f, 3.4f, 12f), materials[0]);
        Pillar(region, "R01 low marker stone west", new Vector3(-14f, 0f, -12f), 1.4f, 1.2f, materials[0]);
        Pillar(region, "R01 low marker stone east", new Vector3(14f, 0f, -20f), 1.4f, 1.2f, materials[0]);
    }

    private static void BuildRegion2()
    {
        Transform region = CreateRegion("R02 Crystal Caverns - TEMP BLOCKOUT");
        Floor(region, "R02 central cavern chamber", new Vector3(0f, -0.3f, 82f), 56f, 44f, materials[1]);
        Floor(region, "R02 west crystal chamber", new Vector3(-42f, -0.3f, 77f), 28f, 28f, materials[1]);
        Floor(region, "R02 west chamber throat", new Vector3(-27f, -0.3f, 78f), 18f, 12f, materials[1]);
        Floor(region, "R02 north hidden grotto", new Vector3(-16f, -0.3f, 117f), 26f, 20f, materials[1]);
        Floor(region, "R02 grotto bend one", new Vector3(-35f, -0.3f, 97f), 18f, 12f, materials[1]);
        Floor(region, "R02 grotto bend two", new Vector3(-23f, -0.3f, 101f), 18f, 12f, materials[1]);
        Floor(region, "R02 grotto throat", new Vector3(-16f, -0.3f, 104f), 12f, 16f, materials[1]);

        Floor(region, "R02 east path leg", new Vector3(32f, -0.3f, 78f), 18f, 10f, materials[1]);
        Floor(region, "R02 east path turn", new Vector3(42f, -0.3f, 88f), 10f, 20f, materials[1]);
        Floor(region, "R02 east approach", new Vector3(54f, -0.3f, 98f), 18f, 10f, materials[1]);

        Wall(region, "R02 cave edge fragment west", new Vector3(-28.5f, 3f, 92f), new Vector3(1f, 6f, 20f), materials[1]);
        Wall(region, "R02 cave edge fragment north west", new Vector3(-26f, 3f, 104f), new Vector3(4f, 6f, 1f), materials[1]);
        Wall(region, "R02 cave edge fragment north east", new Vector3(16f, 3f, 104f), new Vector3(24f, 6f, 1f), materials[1]);
        Pillar(region, "R02 cavern marker west", new Vector3(-20f, 0f, 70f), 1.5f, 4f, materials[1]);
        Pillar(region, "R02 cavern marker east", new Vector3(20f, 0f, 94f), 1.5f, 4f, materials[1]);
        Arch(region, "R02 to R03 bend", new Vector3(55f, 0f, 98f), 10f, materials[1], 90f);
    }

    private static void BuildRegion3()
    {
        Transform region = CreateRegion("R03 Ancient Ruins - TEMP BLOCKOUT");
        Floor(region, "R03 open ruins plaza", new Vector3(78f, -0.3f, 99f), 42f, 38f, materials[2]);
        Floor(region, "R03 north inscription archive", new Vector3(79f, -0.3f, 138f), 28f, 20f, materials[2]);
        Floor(region, "R03 archive approach", new Vector3(79f, -0.3f, 120f), 12f, 16f, materials[2]);
        Floor(region, "R03 east side courtyard", new Vector3(113f, -0.3f, 100f), 28f, 30f, materials[2]);
        Floor(region, "R03 east courtyard connector", new Vector3(100f, -0.3f, 100f), 14f, 12f, materials[2]);
        Floor(region, "R03 temple approach", new Vector3(78f, -0.3f, 72f), 16f, 18f, materials[2]);

        Pillar(region, "R03 plaza broken pillar NW", new Vector3(61f, 0f, 114f), 2f, 5f, materials[2]);
        Pillar(region, "R03 plaza broken pillar NE", new Vector3(94f, 0f, 114f), 2f, 3.5f, materials[2]);
        Pillar(region, "R03 plaza broken pillar SW", new Vector3(61f, 0f, 84f), 2f, 4f, materials[2]);
        Pillar(region, "R03 plaza broken pillar SE", new Vector3(94f, 0f, 84f), 2f, 5.5f, materials[2]);
        Wall(region, "R03 broken north wall west", new Vector3(67f, 1.5f, 118.5f), new Vector3(18f, 3f, 0.8f), materials[2]);
        Wall(region, "R03 broken north wall east", new Vector3(94f, 1.5f, 118.5f), new Vector3(8f, 3f, 0.8f), materials[2]);
        Wall(region, "R03 side court divider fragment", new Vector3(100f, 1.5f, 109f), new Vector3(0.8f, 3f, 10f), materials[2]);
        Arch(region, "R03 to R04 descent", new Vector3(78f, 0f, 80f), 12f, materials[2], 0f);
    }

    private static void BuildRegion4()
    {
        Transform region = CreateRegion("R04 Underground Temple - TEMP BLOCKOUT");
        Floor(region, "R04 northern temple gallery", new Vector3(88f, -0.3f, 70f), 30f, 18f, materials[3]);
        Floor(region, "R04 central temple hall", new Vector3(88f, -0.3f, 39f), 50f, 34f, materials[3]);
        Floor(region, "R04 west trial room", new Vector3(43f, -0.3f, 39f), 24f, 24f, materials[3]);
        Floor(region, "R04 west hall connector", new Vector3(58f, -0.3f, 39f), 18f, 12f, materials[3]);
        Floor(region, "R04 east trial room", new Vector3(133f, -0.3f, 39f), 28f, 24f, materials[3]);
        Floor(region, "R04 east hall connector", new Vector3(116f, -0.3f, 39f), 16f, 12f, materials[3]);
        Floor(region, "R04 north hall connector", new Vector3(88f, -0.3f, 57f), 14f, 12f, materials[3]);

        Floor(region, "R04 west rear passage", new Vector3(49f, -0.3f, 58f), 14f, 16f, materials[3]);
        Floor(region, "R04 west gallery loop", new Vector3(62f, -0.3f, 66f), 26f, 14f, materials[3]);
        Floor(region, "R04 east rear passage", new Vector3(133f, -0.3f, 58f), 12f, 16f, materials[3]);
        Floor(region, "R04 east gallery loop", new Vector3(115f, -0.3f, 66f), 28f, 14f, materials[3]);
        Floor(region, "R04 south return gallery", new Vector3(88f, -0.3f, 13f), 54f, 18f, materials[3]);
        Floor(region, "R04 west return connector", new Vector3(59f, -0.3f, 20f), 14f, 14f, materials[3]);
        Floor(region, "R04 east return connector", new Vector3(116f, -0.3f, 20f), 16f, 14f, materials[3]);

        Pillar(region, "R04 nave column NW", new Vector3(72f, 0f, 52f), 1.8f, 6f, materials[3]);
        Pillar(region, "R04 nave column NE", new Vector3(104f, 0f, 52f), 1.8f, 6f, materials[3]);
        Pillar(region, "R04 nave column SW", new Vector3(72f, 0f, 27f), 1.8f, 6f, materials[3]);
        Pillar(region, "R04 nave column SE", new Vector3(104f, 0f, 27f), 1.8f, 6f, materials[3]);
        Wall(region, "R04 west trial marker", new Vector3(34f, 1.5f, 54f), new Vector3(0.8f, 3f, 10f), materials[3]);
        Wall(region, "R04 east trial marker", new Vector3(147f, 1.5f, 54f), new Vector3(0.8f, 3f, 10f), materials[3]);
        Arch(region, "R04 to R05 vault", new Vector3(144f, 0f, 39f), 12f, materials[3], 90f);
    }

    private static void BuildRegion5()
    {
        Transform region = CreateRegion("R05 Final Treasure Vault - TEMP BLOCKOUT");
        Floor(region, "R05 main vault chamber", new Vector3(172f, -0.3f, 39f), 58f, 58f, materials[4]);
        Floor(region, "R05 north optional vault", new Vector3(172f, -0.3f, 90f), 28f, 20f, materials[4]);
        Floor(region, "R05 north vault throat", new Vector3(172f, -0.3f, 73f), 14f, 16f, materials[4]);
        Floor(region, "R05 south optional vault", new Vector3(172f, -0.3f, -7f), 26f, 20f, materials[4]);
        Floor(region, "R05 south vault throat", new Vector3(172f, -0.3f, 8f), 14f, 14f, materials[4]);
        Floor(region, "R05 east hidden chamber", new Vector3(211f, -0.3f, 39f), 18f, 22f, materials[4]);
        Floor(region, "R05 hidden chamber throat", new Vector3(199f, -0.3f, 39f), 14f, 12f, materials[4]);
        Floor(region, "R05 east vault loop", new Vector3(212f, -0.3f, 65f), 12f, 32f, materials[4]);
        Floor(region, "R05 northern loop link", new Vector3(195f, -0.3f, 81f), 18f, 12f, materials[4]);

        Box(region, "R05 central treasure platform - BLOCKOUT ONLY",
            new Vector3(172f, 0.18f, 39f), new Vector3(16f, 0.36f, 14f), materials[4]);
        Pillar(region, "R05 vault ring pillar NW", new Vector3(151f, 0f, 58f), 1.8f, 7f, materials[4]);
        Pillar(region, "R05 vault ring pillar NE", new Vector3(193f, 0f, 58f), 1.8f, 7f, materials[4]);
        Pillar(region, "R05 vault ring pillar SW", new Vector3(151f, 0f, 20f), 1.8f, 7f, materials[4]);
        Pillar(region, "R05 vault ring pillar SE", new Vector3(193f, 0f, 20f), 1.8f, 7f, materials[4]);
        Wall(region, "R05 outer vault wall west fragment", new Vector3(143f, 2.5f, 39f), new Vector3(0.8f, 5f, 20f), materials[4]);
        Wall(region, "R05 outer vault wall east fragment", new Vector3(201f, 2.5f, 39f), new Vector3(0.8f, 5f, 18f), materials[4]);
    }
}



