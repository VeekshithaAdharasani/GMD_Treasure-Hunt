using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalImprovementsSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string ImprovementsFolder = "Assets/VisualPolish/RunnerFinalImprovements";
    private const string PickupFolder = "Assets/Prefabs/Runner/Pickups/PowerUps";
    private const string CoinPath = "Assets/Prefabs/Runner/Pickups/Runner_Coin.prefab";
    private static readonly Color[] Colors = {
        new Color(1f, 0.55f, 0.08f), new Color(0.1f, 0.8f, 1f),
        new Color(1f, 0.25f, 0.06f), new Color(0.7f, 0.2f, 1f),
        new Color(1f, 0.82f, 0.08f)
    };
    private static readonly PrimitiveType[] Shapes = {
        PrimitiveType.Cylinder, PrimitiveType.Sphere, PrimitiveType.Capsule,
        PrimitiveType.Cube, PrimitiveType.Cylinder
    };
    private static readonly string[] Names = { "CoinMagnet", "Shield", "SpeedBoost", "ScoreMultiplier", "TreasureBonus" };

    [MenuItem("Tools/Runner Adventure/Apply Final Improvements")]
    public static void Apply()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolder("Assets/Prefabs/Runner/Pickups", "PowerUps");
        EnsureFolder("Assets/VisualPolish", "RunnerFinalImprovements");
        GameObject[] powerUps = CreatePowerUpPrefabs();
        UpdateScene(powerUps);
        UpdateRegionConfigs();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("FINAL_IMPROVEMENTS_SETUP_PASS | route=60x20m | boundaries=400,800,1200 | powerups=5 | keys=2 | chest=1");
    }

    private static GameObject[] CreatePowerUpPrefabs()
    {
        GameObject coin = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        if (coin == null) throw new InvalidOperationException("Existing coin pickup prefab is missing.");
        GameObject[] result = new GameObject[Names.Length];
        for (int i = 0; i < result.Length; i++)
        {
            string path = PickupFolder + "/Runner_PowerUp_" + Names[i] + ".prefab";
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(coin);
            instance.name = "Runner_PowerUp_" + Names[i];
            Collectible collectible = instance.GetComponent<Collectible>();
            if (collectible == null) collectible = instance.AddComponent<Collectible>();
            collectible.powerUpType = (Collectible.PowerUpType)(i + 1);
            collectible.treasureType = Collectible.TreasureType.Coin;
            collectible.points = 0;
            Transform root = instance.transform.Find("POWERUP_VISUAL_ROOT");
            if (root != null) UnityEngine.Object.DestroyImmediate(root.gameObject);
            GameObject visual = new GameObject("POWERUP_VISUAL_ROOT");
            visual.transform.SetParent(instance.transform, false);
            visual.transform.localPosition = Vector3.up * 0.45f;
            visual.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
            GameObject shape = GameObject.CreatePrimitive(Shapes[i]);
            shape.name = "PowerUpShape_" + Names[i];
            shape.transform.SetParent(visual.transform, false);
            shape.transform.localScale = Shapes[i] == PrimitiveType.Cylinder ? new Vector3(0.8f, 0.16f, 0.8f) : Vector3.one;
            Collider shapeCollider = shape.GetComponent<Collider>();
            if (shapeCollider != null) UnityEngine.Object.DestroyImmediate(shapeCollider);
            Renderer renderer = shape.GetComponent<Renderer>();
            string materialPath = ImprovementsFolder + "/M_PowerUp_" + Names[i] + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.name = "M_PowerUp_" + Names[i];
                material.color = Colors[i];
                material.enableInstancing = true;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            result[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return result;
    }

    private static void UpdateScene(GameObject[] powerUps)
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        RunnerTrackManager track = Find<RunnerTrackManager>(scene);
        GameManager manager = Find<GameManager>(scene);
        if (track == null || manager == null) throw new InvalidOperationException("RunnerTrackManager or GameManager is missing.");
        track.powerUpPrefabs = powerUps;
        track.powerUpSeed = 84271;
        track.sectionsPerBiome = 3;

        TreasureKey[] allKeys = FindAll<TreasureKey>(scene);
        TreasureKey key0 = Array.Find(allKeys, k => k.keyId == 0 && k.gameObject.activeSelf);
        TreasureKey key1 = Array.Find(allKeys, k => k.keyId == 1 && k.gameObject.activeSelf);
        if (key0 == null || key1 == null) throw new InvalidOperationException("The existing ID 0 and ID 1 scene keys are required.");
        key0.transform.position = new Vector3(-3.5f, 1f, 100f);
        key1.transform.position = new Vector3(3.5f, 1f, 600f);
        foreach (TreasureKey key in allKeys)
            if (key.keyId > 1) key.gameObject.SetActive(false);
        manager.requiredTreasureKeys = 2;

        TreasureChest chest = Find<TreasureChest>(scene);
        if (chest == null) throw new InvalidOperationException("Existing interactive treasure chest is missing.");
        chest.transform.position = new Vector3(0f, 0f, 1196f);

        // Find and correct any visible two/four-key mismatch in the active scene copy.
        foreach (TMPro.TMP_Text text in FindAll<TMPro.TMP_Text>(scene))
        {
            string value = text.text;
            string updated = value.Replace("four golden keys", "two golden keys")
                                   .Replace("four unique keys", "two unique keys")
                                   .Replace("4/4", "2/2");
            if (updated != value) text.text = updated;
        }
        EditorUtility.SetDirty(track); EditorUtility.SetDirty(manager); EditorUtility.SetDirty(chest);
        EditorUtility.SetDirty(key0); EditorUtility.SetDirty(key1);
    }

    private static void UpdateRegionConfigs()
    {
        SetRegion("Assets/VisualPolish/EnvironmentFramework/RunnerRegion_ForgottenEntrance.asset", 0, 18, 19, 400f);
        SetRegion("Assets/VisualPolish/EnvironmentFramework/RunnerRegion_CrystalCaverns.asset", 20, 39, 40, 800f);
        SetRegion("Assets/VisualPolish/EnvironmentFramework/RunnerRegion_AncientTreasureVault.asset", 41, 59, -1, 0f);
    }

    private static void SetRegion(string path, int first, int last, int transition, float transitionZ)
    {
        RunnerRegionConfig config = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(path);
        if (config == null) throw new InvalidOperationException("Missing region configuration: " + path);
        config.firstVisualSlot = first; config.lastVisualSlot = last;
        config.outgoingTransitionSlot = transition; config.transitionCenterZ = transitionZ;
        EditorUtility.SetDirty(config);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
    private static T Find<T>(Scene scene) where T : Component
    { foreach (GameObject root in scene.GetRootGameObjects()) { T value = root.GetComponentInChildren<T>(true); if (value != null) return value; } return null; }
    private static T[] FindAll<T>(Scene scene) where T : Component
    { System.Collections.Generic.List<T> values = new System.Collections.Generic.List<T>(); foreach (GameObject root in scene.GetRootGameObjects()) values.AddRange(root.GetComponentsInChildren<T>(true)); return values.ToArray(); }
}
