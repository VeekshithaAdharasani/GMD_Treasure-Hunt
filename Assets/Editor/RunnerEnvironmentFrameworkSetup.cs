using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Guarded, idempotent wiring for the shared environment framework.</summary>
public static class RunnerEnvironmentFrameworkSetup
{
    private const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    private const string ConfigFolder = "Assets/VisualPolish/EnvironmentFramework";

    [MenuItem("Lost Cave/Environment Framework/Configure Shared Regions")]
    public static void Configure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("M1 preflight failed: LostCave_Main could not be loaded.");

        RunnerTrackManager manager = FindInScene<RunnerTrackManager>(scene);
        RunnerTrackVisuals visuals = FindInScene<RunnerTrackVisuals>(scene);
        RunnerWorldAtmosphere atmosphere = FindInScene<RunnerWorldAtmosphere>(scene);
        string preflightFailure = ValidateBeforeMutation(manager, visuals, atmosphere);
        if (!string.IsNullOrEmpty(preflightFailure))
            throw new InvalidOperationException("M1 preflight failed: " + preflightFailure);
        string assetPathFailure = ValidateConfigAssetPaths();
        if (!string.IsNullOrEmpty(assetPathFailure))
            throw new InvalidOperationException("M1 preflight failed: " + assetPathFailure);

        EnsureAssetFolder(ConfigFolder);
        RunnerRegionConfig forgotten = LoadOrCreate("RunnerRegion_ForgottenEntrance.asset");
        RunnerRegionConfig caverns = LoadOrCreate("RunnerRegion_CrystalCaverns.asset");
        RunnerRegionConfig vault = LoadOrCreate("RunnerRegion_AncientTreasureVault.asset");

        ConfigureRegion(forgotten, RunnerRegionId.ForgottenEntrance, "Forgotten Entrance", 0, 2, 3,
            visuals.forgottenEntranceDecoration, visuals.forgottenEntranceFloor, 0, atmosphere.firstStageEndZ,
            atmosphere.blendWidth, atmosphere);
        ConfigureRegion(caverns, RunnerRegionId.CrystalCaverns, "Crystal Caverns", 3, 8, 8,
            visuals.crystalCavernsDecoration, visuals.crystalCavernsFloor, 1, atmosphere.secondStageEndZ,
            atmosphere.blendWidth, atmosphere);
        ConfigureRegion(vault, RunnerRegionId.AncientTreasureVault, "Ancient Treasure Vault", 9, 12, -1,
            visuals.treasureVaultDecoration, visuals.treasureVaultFloor, 2, 0f,
            atmosphere.blendWidth, atmosphere);

        RunnerRegionConfig[] configs = { forgotten, caverns, vault };
        visuals.regionConfigs = configs;
        atmosphere.regionConfigs = configs;

        ValidateConfiguredReferences(visuals, atmosphere, configs);
        EditorUtility.SetDirty(visuals);
        EditorUtility.SetDirty(atmosphere);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("M1 setup failed: Unity did not save LostCave_Main.");

        AssetDatabase.Refresh();
        Debug.Log("M1_SHARED_ENVIRONMENT_FRAMEWORK_SAVED | regionConfigs=3 | routeSlots=0-12 | transitionSlots=3,8 | scene=" + ScenePath);
    }

    private static string ValidateBeforeMutation(RunnerTrackManager manager, RunnerTrackVisuals visuals, RunnerWorldAtmosphere atmosphere)
    {
        if (manager == null) return "RunnerTrackManager is missing from LostCave_Main.";
        if (visuals == null || manager.sectionVisuals != visuals) return "RunnerTrackVisuals is missing or not the manager's assigned visual system.";
        if (atmosphere == null || atmosphere.player == null) return "RunnerWorldAtmosphere or its player reference is missing.";
        if (manager.player == null || atmosphere.player != manager.player.transform || visuals.transform != manager.transform || manager.sectionsContainer == null)
            return "Runner player reference or route/visual ownership is invalid.";
        if (!ValidCatalog(manager.forgottenEntranceSections, RunnerBiome.ForgottenEntrance) ||
            !ValidCatalog(manager.crystalCavernsSections, RunnerBiome.CrystalCaverns) ||
            !ValidCatalog(manager.finalTreasureVaultSections, RunnerBiome.FinalTreasureVault))
            return "The existing 13-slot route needs its three authored section catalogs (three modules per catalog).";
        if (manager.transitionToCrystalCaverns == null || manager.transitionToFinalTreasureVault == null)
            return "One of the two existing transition section prefabs is missing.";
        if (manager.transitionToCrystalCaverns == manager.transitionToFinalTreasureVault)
            return "The two route transition slots must use distinct existing section prefabs.";
        if (visuals.forgottenEntranceDecoration == null || visuals.crystalCavernsDecoration == null || visuals.treasureVaultDecoration == null)
            return "An active region decoration prefab is missing.";
        if (visuals.forgottenEntranceFloor == null || visuals.crystalCavernsFloor == null || visuals.treasureVaultFloor == null)
            return "An active region floor material is missing.";
        if (atmosphere.fogColors == null || atmosphere.fogColors.Length < 3 || atmosphere.skyAmbient == null || atmosphere.skyAmbient.Length < 3 ||
            atmosphere.equatorAmbient == null || atmosphere.equatorAmbient.Length < 3 || atmosphere.groundAmbient == null || atmosphere.groundAmbient.Length < 3 ||
            atmosphere.fogDensities == null || atmosphere.fogDensities.Length < 3)
            return "The saved three-region atmosphere profiles are incomplete.";

        for (int i = 0; i < manager.forgottenEntranceSections.Length; i++)
            if (!ValidateSection(manager.forgottenEntranceSections[i], out string error)) return error;
        for (int i = 0; i < manager.crystalCavernsSections.Length; i++)
            if (!ValidateSection(manager.crystalCavernsSections[i], out string error)) return error;
        for (int i = 0; i < manager.finalTreasureVaultSections.Length; i++)
            if (!ValidateSection(manager.finalTreasureVaultSections[i], out string error)) return error;
        if (!ValidateSection(manager.transitionToCrystalCaverns, out string firstTransitionError)) return firstTransitionError;
        if (!ValidateSection(manager.transitionToFinalTreasureVault, out string secondTransitionError)) return secondTransitionError;

        return null;
    }

    private static bool ValidCatalog(RunnerTrackSection[] sections, RunnerBiome expectedBiome)
    {
        if (sections == null || sections.Length != 3) return false;
        for (int i = 0; i < sections.Length; i++)
            if (sections[i] == null || sections[i].biome != expectedBiome) return false;
        return true;
    }

    private static string ValidateConfigAssetPaths()
    {
        string[] names =
        {
            "RunnerRegion_ForgottenEntrance.asset",
            "RunnerRegion_CrystalCaverns.asset",
            "RunnerRegion_AncientTreasureVault.asset"
        };
        for (int i = 0; i < names.Length; i++)
        {
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(ConfigFolder + "/" + names[i]);
            if (existing != null && !(existing is RunnerRegionConfig))
                return "Config asset destination is occupied by another asset: " + names[i];
        }
        return null;
    }

    private static bool ValidateSection(RunnerTrackSection section, out string error)
    {
        if (section == null) { error = "A saved route section reference is missing."; return false; }
        if (!section.Validate(3.5f, out string validationError))
        {
            error = "Route section preflight failed for " + section.name + ": " + validationError;
            return false;
        }
        error = null;
        return true;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        string parent = "Assets";
        string[] parts = assetFolder.Substring("Assets/".Length).Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            string next = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, parts[i]);
            parent = next;
        }
    }

    private static RunnerRegionConfig LoadOrCreate(string fileName)
    {
        string path = ConfigFolder + "/" + fileName;
        RunnerRegionConfig config = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(path);
        if (config != null) return config;
        config = ScriptableObject.CreateInstance<RunnerRegionConfig>();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private static void ConfigureRegion(RunnerRegionConfig config, RunnerRegionId id, string displayName,
        int firstSlot, int lastSlot, int transitionSlot, GameObject decoration, Material floor, int profileIndex,
        float transitionCenter, float transitionWidth, RunnerWorldAtmosphere atmosphere)
    {
        config.regionId = id;
        config.displayName = displayName;
        config.firstVisualSlot = firstSlot;
        config.lastVisualSlot = lastSlot;
        config.outgoingTransitionSlot = transitionSlot;
        config.baseDecoration = decoration;
        config.decorationVariants = decoration != null ? new[] { decoration } : Array.Empty<GameObject>();
        config.decorationScaleVariants = new[] { 0.985f, 1f, 1.015f };
        config.floorMaterial = floor;
        config.fogColor = atmosphere.fogColors[profileIndex];
        config.fogDensity = atmosphere.fogDensities[profileIndex];
        config.ambientSky = atmosphere.skyAmbient[profileIndex];
        config.ambientEquator = atmosphere.equatorAmbient[profileIndex];
        config.ambientGround = atmosphere.groundAmbient[profileIndex];
        config.ambientIntensity = 0.9f;
        config.transitionCenterZ = transitionCenter;
        config.transitionBlendWidth = Mathf.Max(1f, transitionWidth);
        config.transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        EditorUtility.SetDirty(config);
    }

    private static void ValidateConfiguredReferences(RunnerTrackVisuals visuals, RunnerWorldAtmosphere atmosphere,
        RunnerRegionConfig[] configs)
    {
        if (configs == null || configs.Length != 3 || visuals.regionConfigs != configs || atmosphere.regionConfigs != configs)
            throw new InvalidOperationException("M1 final validation failed: all three region configs were not assigned.");
        for (int i = 0; i < configs.Length; i++)
        {
            if (configs[i] == null || configs[i].floorMaterial == null || configs[i].baseDecoration == null ||
                configs[i].decorationVariants == null || configs[i].decorationVariants.Length == 0 ||
                configs[i].decorationVariants[0] == null || (int)configs[i].regionId != i ||
                configs[i].firstVisualSlot != (i == 0 ? 0 : i == 1 ? 3 : 9) ||
                configs[i].lastVisualSlot != (i == 0 ? 2 : i == 1 ? 8 : 12) ||
                configs[i].outgoingTransitionSlot != (i == 0 ? 3 : i == 1 ? 8 : -1))
                throw new InvalidOperationException("M1 final validation failed for region config index " + i + ".");
        }
    }
}
