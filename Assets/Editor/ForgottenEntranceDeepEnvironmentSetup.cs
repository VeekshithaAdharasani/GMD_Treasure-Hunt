using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds three distinct, collision-free Forgotten Entrance environment compositions to the
/// existing route decoration prefabs. It does not alter the scene, route, or gameplay assets.
/// </summary>
public static class ForgottenEntranceDeepEnvironmentSetup
{
    private const string Root = "Assets/VisualPolish/ForgottenEntrance";
    private const string Generated = Root + "/DeepEnvironment";
    private const string PrefabFolder = Root;
    private const string MarkerName = "FE_DEEP_ENVIRONMENT_V1";
    private static readonly string[] PrefabPaths =
    {
        PrefabFolder + "/RunnerDecoration_ForgottenGateway.prefab",
        PrefabFolder + "/RunnerDecoration_AncientCaveEntrance.prefab",
        PrefabFolder + "/RunnerDecoration_LostJunglePath.prefab"
    };

    [MenuItem("Lost Cave/Forgotten Entrance/Build Deep Environment")]
    public static void Configure()
    {
        if (!Preflight(out string error))
        {
            Debug.LogError("FE_DEEP_ENVIRONMENT_PREFLIGHT_FAIL | " + error);
            return;
        }

        string[] names = { "ForgottenGateway", "AncientCaveEntrance", "LostJunglePath" };
        if (AllAlreadyBuilt())
        {
            Debug.Log("FE_DEEP_ENVIRONMENT_ALREADY_BUILT | All three prefabs contain the reviewed environment group.");
            return;
        }

        try
        {
            EnsureFolder(Generated);
            Material stone = CreateMaterial("M_FE_DeepWeatheredStone", new Color(0.56f, 0.49f, 0.39f),
                "T_StoneWeathered", Color.white, 0.22f);
            Material bark = CreateMaterial("M_FE_DeepRootBark", new Color(0.42f, 0.25f, 0.13f),
                "T_RootBark", Color.white, 0.13f);
            Material leaf = CreateMaterial("M_FE_DeepJungleLeaves", new Color(0.34f, 0.52f, 0.20f),
                "T_TropicalLeaf", Color.white, 0.08f);
            Material moss = CreateMaterial("M_FE_DeepMoss", new Color(0.31f, 0.42f, 0.16f),
                "T_MossLichen", Color.white, 0.05f);

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                EnvironmentMeshes meshes = BuildMeshes(i, names[i]);
                meshes.stone = SaveMesh(meshes.stone, names[i] + "_Architecture");
                meshes.roots = SaveMesh(meshes.roots, names[i] + "_RootsAndVines");
                meshes.foliage = SaveMesh(meshes.foliage, names[i] + "_LayeredFoliage");
                meshes.moss = SaveMesh(meshes.moss, names[i] + "_MossAndRubble");
                ApplyToPrefab(PrefabPaths[i], meshes, stone, bark, leaf, moss, i);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!FinalValidation(out error))
            {
                Debug.LogError("FE_DEEP_ENVIRONMENT_FINAL_VALIDATION_FAIL | " + error);
                return;
            }
            Debug.Log("FE_DEEP_ENVIRONMENT_SAVED | Three distinct prefab compositions added; route, scene, camera, and gameplay were not changed.");
        }
        catch (Exception exception)
        {
            Debug.LogError("FE_DEEP_ENVIRONMENT_SETUP_FAIL | " + exception);
        }
    }

    private static bool Preflight(out string error)
    {
        error = null;
        if (AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(
                "Assets/VisualPolish/EnvironmentFramework/RunnerRegion_ForgottenEntrance.asset") == null)
        {
            error = "Forgotten Entrance region configuration is missing.";
            return false;
        }
        RunnerRegionConfig config = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(
            "Assets/VisualPolish/EnvironmentFramework/RunnerRegion_ForgottenEntrance.asset");
        if (config.decorationVariants == null || config.decorationVariants.Length != PrefabPaths.Length)
        {
            error = "Forgotten Entrance must keep its three configured decoration variants.";
            return false;
        }
        foreach (string path in PrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                error = "Required decoration prefab is missing: " + path;
                return false;
            }
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
            {
                error = "Decoration target is not a prefab asset: " + path;
                return false;
            }
        }

        for (int i = 0; i < PrefabPaths.Length; i++)
        {
            GameObject configured = config.decorationVariants[i];
            GameObject expected = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[i]);
            if (configured == null || expected == null || configured != expected)
            {
                error = "Region configuration variant " + i + " no longer matches its expected Forgotten Entrance prefab.";
                return false;
            }
        }

        int built = 0;
        foreach (string path in PrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab.transform.Find(MarkerName) != null) built++;
        }
        if (built != 0 && built != PrefabPaths.Length)
        {
            error = "Partial deep-environment marker state found; refusing to make duplicate or incomplete prefab edits.";
            return false;
        }
        if (!AssetDatabase.IsValidFolder(Generated) && built != 0)
        {
            error = "Prefabs are marked built but the generated asset folder is missing.";
            return false;
        }
        if (built == 0 && AssetDatabase.IsValidFolder(Generated))
        {
            string[] existing = AssetDatabase.FindAssets(string.Empty, new[] { Generated });
            if (existing.Length > 0)
            {
                error = "Generated environment assets already exist without all three prefab markers; refusing to overwrite or mix partial output.";
                return false;
            }
        }
        return true;
    }

    private static bool AllAlreadyBuilt()
    {
        foreach (string path in PrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.transform.Find(MarkerName) == null) return false;
        }
        return true;
    }

    private static bool FinalValidation(out string error)
    {
        error = null;
        string[] expectedChildren = { "ARCHITECTURE", "ROOTS_AND_VINES", "LAYERED_FOLIAGE", "MOSS_AND_RUBBLE", "WARM_BOUNCE_LIGHTS" };
        foreach (string path in PrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Transform marker = prefab != null ? prefab.transform.Find(MarkerName) : null;
            if (marker == null) { error = "Missing generated environment root in " + path; return false; }
            foreach (string child in expectedChildren)
            {
                Transform part = marker.Find(child);
                MeshFilter filter = part != null ? part.GetComponent<MeshFilter>() : null;
                if (child == "WARM_BOUNCE_LIGHTS")
                {
                    if (part == null || part.GetComponentsInChildren<Light>(true).Length < 2)
                    { error = "Expected warm light pair is missing from " + path; return false; }
                }
                else if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount < 100)
                { error = "Generated mesh is missing or unexpectedly small: " + path + " / " + child; return false; }
            }
            if (marker.GetComponentsInChildren<Collider>(true).Length != 0)
            { error = "Generated environment visuals must remain collider-free to preserve runner controls: " + path; return false; }
        }
        return true;
    }

    private static void ApplyToPrefab(string path, EnvironmentMeshes meshes, Material stone, Material bark,
        Material leaf, Material moss, int variant)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform old = contents.transform.Find(MarkerName);
            if (old != null)
                throw new InvalidOperationException("An environment marker appeared after preflight; refusing to remove or replace it.");

            // Retain the established trees, grass, terrain bank, edge inlays, and torches.
            // Hide only the old cave/gate blockout pieces that conflict with this authored shell.
            string[] legacyNames = { "CAVE WALL ROCK", "CAVE STALAGMITE", "ANCIENT GATE PIER", "ANCIENT GATE LINTEL" };
            foreach (Transform child in contents.GetComponentsInChildren<Transform>(true))
                if (Array.IndexOf(legacyNames, child.name) >= 0) child.gameObject.SetActive(false);

            GameObject marker = new GameObject(MarkerName);
            marker.transform.SetParent(contents.transform, false);
            AddMeshChild(marker.transform, "ARCHITECTURE", meshes.stone, stone, moss);
            AddMeshChild(marker.transform, "ROOTS_AND_VINES", meshes.roots, bark, leaf);
            AddMeshChild(marker.transform, "LAYERED_FOLIAGE", meshes.foliage, leaf);
            AddMeshChild(marker.transform, "MOSS_AND_RUBBLE", meshes.moss, moss, stone);
            AddWarmLights(marker.transform, variant);

            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void AddMeshChild(Transform parent, string name, Mesh mesh, params Material[] materials)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        MeshFilter filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void AddWarmLights(Transform parent, int variant)
    {
        GameObject root = new GameObject("WARM_BOUNCE_LIGHTS");
        root.transform.SetParent(parent, false);
        float[] z = variant == 0 ? new[] { 4.2f, 14.7f } : variant == 1 ? new[] { 3.0f, 12.6f, 18.1f } : new[] { 5.5f, 15.3f };
        for (int i = 0; i < z.Length; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            GameObject lightObject = new GameObject("Warm stone sconce " + i);
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(side * 6.8f, 4.0f, z[i]);
            lightObject.transform.LookAt(new Vector3(0f, 0.4f, z[i] + 1.5f));
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.52f, 0.22f);
            light.intensity = 2.4f;
            light.range = 13f;
            light.spotAngle = 58f;
            light.shadows = LightShadows.None;
        }
    }

    private static EnvironmentMeshes BuildMeshes(int variant, string label)
    {
        var stone = new MeshBuilder();
        var roots = new MeshBuilder();
        var leaves = new MeshBuilder();
        var moss = new MeshBuilder();
        var random = new System.Random(2713 + variant * 127);

        if (variant == 0) BuildGateway(stone, moss, roots, leaves, random);
        else if (variant == 1) BuildCaveMouth(stone, moss, roots, leaves, random);
        else BuildJunglePath(stone, moss, roots, leaves, random);

        return new EnvironmentMeshes
        {
            stone = stone.ToMesh(label + " Architecture"),
            roots = roots.ToMesh(label + " Roots and Vines"),
            foliage = leaves.ToMesh(label + " Layered Foliage"),
            moss = moss.ToMesh(label + " Moss and Rubble")
        };
    }

    private static void BuildGateway(MeshBuilder stone, MeshBuilder moss, MeshBuilder roots, MeshBuilder leaves, System.Random random)
    {
        // Tall parallel ruin walls create a substantial architectural envelope around the track.
        AddSideWalls(stone, random, 7.6f, 7.1f, 0f, 19.5f, 7.0f, 1.55f);
        AddPillar(stone, new Vector3(-5.65f, 0f, 1.8f), 1.45f, 6.2f, 1.6f, random);
        AddPillar(stone, new Vector3(5.65f, 0f, 1.8f), 1.45f, 6.2f, 1.6f, random);
        AddArch(stone, 1.8f, 5.0f, 2.1f, 0.0f, random, 17);
        AddBrokenOverheadBeam(stone, new Vector3(0f, 7.15f, 10.4f), 14f, 0.85f, 1.0f, 0.12f);
        AddRoot(roots, new Vector3(-7.4f, 0.35f, 4.0f), new Vector3(-7.15f, 4.2f, 3.5f), new Vector3(-5.9f, 5.7f, 2.1f), new Vector3(-3.0f, 6.1f, 0.8f), 0.72f, 0.24f);
        AddRoot(roots, new Vector3(7.4f, 0.3f, 9.0f), new Vector3(7.05f, 4.8f, 8.0f), new Vector3(5.6f, 6.5f, 6.2f), new Vector3(2.3f, 6.8f, 4.9f), 0.8f, 0.22f);
        AddRoot(roots, new Vector3(-7.4f, 0.25f, 15.4f), new Vector3(-6.8f, 3.0f, 15.0f), new Vector3(-5.8f, 5.2f, 13.2f), new Vector3(-4.9f, 6.0f, 12.0f), 0.48f, 0.12f);
        AddVineCurtain(roots, leaves, 0f, 8.0f, 5.5f, 3.5f, 7, random);
        AddFoliageBanks(leaves, moss, random, 12, true);
        AddWallMoss(moss, random, 7.6f, 7.1f, 0f, 19.5f);
    }

    private static void BuildJunglePath(MeshBuilder stone, MeshBuilder moss, MeshBuilder roots, MeshBuilder leaves, System.Random random)
    {
        // Asymmetric collapsed colonnade and a high root canopy; the route remains open below it.
        AddSideWalls(stone, random, 8.0f, 7.25f, 0f, 19.5f, 6.0f, 1.55f);
        AddCollapsedWall(stone, random, -7.25f, 3.2f, 0.6f, 8.8f, 3.4f, 1.25f);
        AddPillar(stone, new Vector3(6.15f, 0f, 4.0f), 1.0f, 5.6f, 1.2f, random);
        AddPillar(stone, new Vector3(-6.0f, 0f, 16.8f), 1.1f, 4.8f, 1.2f, random);
        AddBrokenOverheadBeam(stone, new Vector3(-0.7f, 6.8f, 8.5f), 8.4f, 0.65f, 1.2f, -0.16f);
        AddRoot(roots, new Vector3(-8.0f, 0.25f, 2.0f), new Vector3(-7.1f, 4.4f, 4.5f), new Vector3(-3.9f, 6.6f, 6.2f), new Vector3(1.5f, 6.4f, 8.3f), 0.78f, 0.2f);
        AddRoot(roots, new Vector3(8.0f, 0.25f, 12.8f), new Vector3(7.2f, 4.3f, 11.2f), new Vector3(4.9f, 6.2f, 10.0f), new Vector3(3.9f, 6.5f, 8.6f), 0.66f, 0.16f);
        AddRoot(roots, new Vector3(7.9f, 0.3f, 18.0f), new Vector3(7.2f, 3.0f, 17.2f), new Vector3(6.3f, 4.8f, 15.6f), new Vector3(5.3f, 5.4f, 14.2f), 0.45f, 0.1f);
        AddVineCurtain(roots, leaves, -1.5f, 7.1f, 5.8f, 4.2f, 9, random);
        AddFoliageBanks(leaves, moss, random, 17, true);
        AddWallMoss(moss, random, 8.0f, 7.25f, 0f, 19.5f);
    }

    private static void BuildCaveMouth(MeshBuilder stone, MeshBuilder moss, MeshBuilder roots, MeshBuilder leaves, System.Random random)
    {
        // A monumental, broken cave threshold frames the onward path without closing the track seam.
        AddSideWalls(stone, random, 7.8f, 7.25f, 0f, 16.7f, 7.4f, 1.7f);
        AddPillar(stone, new Vector3(-5.65f, 0f, 16.4f), 1.65f, 7.2f, 1.8f, random);
        AddPillar(stone, new Vector3(5.65f, 0f, 16.4f), 1.65f, 7.2f, 1.8f, random);
        AddArch(stone, 16.4f, 5.05f, 2.45f, 0.10f, random, 19);
        AddCollapsedWall(stone, random, -8.0f, 4.0f, 0.3f, 4.7f, 0.9f, 0.4f);
        AddCollapsedWall(stone, random, 8.0f, 4.5f, 14.8f, 4.6f, 0.9f, -0.3f);
        AddRoot(roots, new Vector3(-8.1f, 0.25f, 5.5f), new Vector3(-7.2f, 4.7f, 8.0f), new Vector3(-5.8f, 6.4f, 12.0f), new Vector3(-4.6f, 6.8f, 15.4f), 0.82f, 0.2f);
        AddRoot(roots, new Vector3(8.1f, 0.2f, 13.8f), new Vector3(7.5f, 4.7f, 15.0f), new Vector3(6.8f, 6.8f, 17.5f), new Vector3(4.2f, 6.9f, 18.5f), 0.76f, 0.17f);
        AddRoot(roots, new Vector3(-7.5f, 0.2f, 18.0f), new Vector3(-6.4f, 3.2f, 18.2f), new Vector3(-5.2f, 5.2f, 17.5f), new Vector3(-4.6f, 6.2f, 16.8f), 0.54f, 0.13f);
        AddVineCurtain(roots, leaves, 0.4f, 8.0f, 5.5f, 4.1f, 8, random);
        AddFoliageBanks(leaves, moss, random, 14, false);
        AddWallMoss(moss, random, 7.8f, 7.25f, 0f, 16.7f);
    }

    private static void AddSideWalls(MeshBuilder b, System.Random r, float leftX, float rightX, float startZ,
        float endZ, float height, float thickness)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side < 0 ? -leftX : rightX;
            int rows = Mathf.RoundToInt(height / 0.88f);
            int cols = Mathf.CeilToInt((endZ - startZ) / 1.32f);
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                if (r.NextDouble() < (row > rows - 2 ? 0.18 : 0.04)) continue;
                float z = startZ + col * 1.32f + ((row & 1) == 0 ? 0f : 0.56f);
                if (z > endZ) continue;
                float y = 0.48f + row * 0.9f;
                AddStoneBlock(b, new Vector3(x + Range(r, -0.15f, 0.15f), y, z),
                    new Vector3(thickness, Range(r, 0.70f, 0.96f), Range(r, 1.08f, 1.48f)),
                    Range(r, -4f, 4f), StoneColor(r));
            }
        }
    }

    private static void AddCollapsedWall(MeshBuilder b, System.Random r, float x, float height, float z,
        float length, float thickness, float lean)
    {
        int rows = Mathf.Max(1, Mathf.RoundToInt(height / 0.8f));
        int cols = Mathf.CeilToInt(length / 1.25f);
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < cols; col++)
        {
            if (row >= rows - 2 && r.NextDouble() < 0.42) continue;
            float zz = z - length * 0.5f + col * 1.25f + (row % 2 == 0 ? 0f : 0.48f);
            float yy = 0.45f + row * 0.82f;
            float xx = x + lean * row / rows + Range(r, -0.1f, 0.1f);
            AddStoneBlock(b, new Vector3(xx, yy, zz), new Vector3(thickness, Range(r, 0.62f, 0.9f), Range(r, 0.95f, 1.42f)),
                lean * 7f, StoneColor(r));
        }
        for (int i = 0; i < 6; i++)
            AddStoneBlock(b, new Vector3(x + lean * 0.5f + Range(r, -1.4f, 1.4f), 0.35f + Range(r, 0.1f, 0.55f), z + Range(r, -length * .65f, length * .65f)),
                new Vector3(Range(r, 0.7f, 1.8f), Range(r, 0.3f, 0.9f), Range(r, 0.7f, 1.5f)), Range(r, -25f, 25f), StoneColor(r));
    }

    private static void AddPillar(MeshBuilder b, Vector3 basePosition, float width, float height, float depth, System.Random r)
    {
        int rows = Mathf.CeilToInt(height / 0.92f);
        for (int i = 0; i < rows; i++)
            AddStoneBlock(b, basePosition + Vector3.up * (0.48f + i * 0.91f),
                new Vector3(width * Range(r, 0.92f, 1.08f), 0.91f, depth * Range(r, 0.92f, 1.08f)), Range(r, -4f, 4f), StoneColor(r));
        AddStoneBlock(b, basePosition + Vector3.up * (height + 0.12f), new Vector3(width * 1.42f, 0.42f, depth * 1.45f), 0, new Color(.47f, .42f, .34f));
        AddStoneBlock(b, basePosition + Vector3.up * 0.22f, new Vector3(width * 1.3f, 0.40f, depth * 1.35f), 0, new Color(.31f, .28f, .23f));
    }

    private static void AddArch(MeshBuilder b, float z, float springY, float rx, float rz, System.Random r, int wedges)
    {
        for (int i = 0; i < wedges; i++)
        {
            float a = Mathf.PI * (i + 0.5f) / wedges;
            Vector3 center = new Vector3(Mathf.Lerp(-5.7f, 5.7f, i / (float)(wedges - 1)), springY + Mathf.Sin(a) * 2.0f, z + rz);
            float yaw = (i - (wedges - 1) * 0.5f) * (180f / wedges) * 0.55f;
            AddStoneBlock(b, center, new Vector3(1.12f, 0.62f, 1.28f), yaw, StoneColor(r));
        }
        // Carved bands create architectural relief along the inner jambs and crown.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int h = 1; h <= 4; h++)
                AddStoneBlock(b, new Vector3(side * 5.52f, h * 0.86f, z - 0.72f), new Vector3(0.20f, 0.12f, 0.12f), 0, new Color(.56f, .48f, .36f));
        }
        AddStoneBlock(b, new Vector3(0f, springY + 2.4f, z), new Vector3(1.2f, 0.86f, 1.42f), 0, new Color(.54f, .45f, .32f));
    }

    private static void AddBrokenOverheadBeam(MeshBuilder b, Vector3 center, float width, float height, float depth, float lean)
    {
        AddStoneBlock(b, center + new Vector3(-width * 0.25f, 0f, 0f), new Vector3(width * 0.55f, height, depth), lean * 10f, new Color(.38f, .34f, .28f));
        AddStoneBlock(b, center + new Vector3(width * 0.27f, -0.28f, 0.24f), new Vector3(width * 0.48f, height * 0.74f, depth * 0.92f), -lean * 8f, new Color(.33f, .29f, .24f));
    }

    private static void AddStoneBlock(MeshBuilder b, Vector3 center, Vector3 size, float yaw, Color color)
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3[] corners = new Vector3[8];
        float jx = size.x * .055f, jy = size.y * .035f, jz = size.z * .055f;
        for (int i = 0; i < 8; i++)
        {
            float sx = (i & 1) == 0 ? -0.5f : 0.5f;
            float sy = (i & 2) == 0 ? -0.5f : 0.5f;
            float sz = (i & 4) == 0 ? -0.5f : 0.5f;
            Vector3 local = new Vector3(sx * size.x + sx * jx, sy * size.y + sy * jy, sz * size.z + sz * jz);
            corners[i] = center + rot * local;
        }
        b.AddQuad(corners[0], corners[2], corners[3], corners[1], color * 0.78f);
        b.AddQuad(corners[4], corners[5], corners[7], corners[6], color * 1.12f);
        b.AddQuad(corners[0], corners[1], corners[5], corners[4], color * 0.90f);
        b.AddQuad(corners[2], corners[6], corners[7], corners[3], color * 0.72f);
        b.AddQuad(corners[0], corners[4], corners[6], corners[2], color * 0.84f);
        b.AddQuad(corners[1], corners[3], corners[7], corners[5], color);
    }

    private static void AddRoot(MeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float radius, float tip)
    {
        AddSweptCurve(b, p0, p1, p2, p3, radius, tip, new Color(.24f, .13f, .065f), 10, 16);
        Vector3 branchStart = Vector3.Lerp(p1, p2, .48f);
        Vector3 branchEnd = new Vector3(branchStart.x + Mathf.Sign(p3.x - p0.x) * 1.5f, branchStart.y + .9f, branchStart.z + .9f);
        AddSweptCurve(b, branchStart, Vector3.Lerp(branchStart, branchEnd, .34f) + Vector3.up * .65f,
            Vector3.Lerp(branchStart, branchEnd, .72f) + Vector3.up * .22f, branchEnd, radius * .42f, .045f, new Color(.22f, .12f, .055f), 7, 10);
    }

    private static void AddVineCurtain(MeshBuilder vines, MeshBuilder leaves, float centerX, float topY, float startZ, float span, int count, System.Random r)
    {
        for (int i = 0; i < count; i++)
        {
            // Hanging foliage stays over the outer shoulders, never over a runner lane.
            int side = (i & 1) == 0 ? -1 : 1;
            float x = side * Range(r, 4.85f, 6.65f);
            float z = startZ + Range(r, -1.8f, 1.8f);
            float length = Range(r, 2.3f, 4.4f);
            Vector3 p0 = new Vector3(x, topY, z);
            Vector3 p1 = new Vector3(x + Range(r, -.35f, .35f), topY - length * .32f, z + .22f);
            Vector3 p2 = new Vector3(x + Range(r, -.4f, .4f), topY - length * .78f, z - .22f);
            Vector3 p3 = new Vector3(x + Range(r, -.7f, .7f), topY - length, z + .15f);
            AddSweptCurve(vines, p0, p1, p2, p3, .075f, .035f, new Color(.13f, .24f, .075f), 7, 7);
            for (int leafIndex = 1; leafIndex <= 4; leafIndex++)
            {
                float t = leafIndex / 5f;
                Vector3 at = Bezier(p0, p1, p2, p3, t);
                AddLeaf(leaves, at, new Vector3((leafIndex % 2 == 0 ? 1 : -1) * .6f, -.18f, .15f), Range(r, .35f, .7f), Range(r, .14f, .24f), LeafColor(r));
            }
        }
    }

    private static void AddFoliageBanks(MeshBuilder leaves, MeshBuilder moss, System.Random r, int clusters, bool addFerns)
    {
        for (int i = 0; i < clusters; i++)
        {
            int side = (i & 1) == 0 ? -1 : 1;
            float x = side * Range(r, 5.0f, 5.5f);
            float z = Range(r, 0.2f, 19.6f);
            Vector3 basePos = new Vector3(x, .15f, z);
            for (int blade = 0; blade < 7; blade++)
            {
                float a = blade * (Mathf.PI * 2f / 7f) + Range(r, -.25f, .25f);
                Vector3 dir = new Vector3(side * (0.06f + Mathf.Abs(Mathf.Cos(a)) * .12f), 1f, Mathf.Sin(a) * .38f);
                float length = Range(r, addFerns ? 1.15f : 1.35f, addFerns ? 2.05f : 1.95f);
                AddLeaf(leaves, basePos + new Vector3(0, 0, .04f), dir, length, Range(r, .28f, .52f), LeafColor(r));
            }
            AddMossPad(moss, basePos + Vector3.up * .08f, new Vector2(Range(r, .7f, 1.3f), Range(r, .6f, 1.1f)), r);
        }
    }

    private static void AddLeaf(MeshBuilder b, Vector3 start, Vector3 direction, float length, float width, Color color)
    {
        direction.Normalize();
        Vector3 side = Vector3.Cross(direction, Vector3.forward);
        if (side.sqrMagnitude < .01f) side = Vector3.Cross(direction, Vector3.right);
        side.Normalize();
        Vector3[] previous = null;
        const int sections = 6;
        for (int i = 0; i <= sections; i++)
        {
            float t = i / (float)sections;
            Vector3 center = start + direction * (length * t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .18f);
            float w = width * Mathf.Pow(1f - t, .72f) * Mathf.Sin(Mathf.PI * (t * .92f + .04f));
            Vector3[] ring = { center - side * w, center + Vector3.up * .08f * Mathf.Sin(t * Mathf.PI), center + side * w };
            if (previous != null)
            {
                for (int edge = 0; edge < 2; edge++)
                {
                    Color c0 = Color.Lerp(color * .58f, color * 1.18f, edge == 0 ? .25f : .72f) * (1f - .18f * t);
                    b.AddQuad(previous[edge], ring[edge], ring[edge + 1], previous[edge + 1], c0);
                    b.AddQuad(previous[edge + 1], ring[edge + 1], ring[edge], previous[edge], c0);
                }
            }
            previous = ring;
        }
    }

    private static void AddWallMoss(MeshBuilder moss, System.Random r, float leftX, float rightX, float startZ, float endZ)
    {
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 22; i++)
            {
                float x = side < 0 ? -leftX + .68f : rightX - .68f;
                float z = Mathf.Lerp(startZ, endZ, i / 21f) + Range(r, -.4f, .4f);
                float y = Range(r, .6f, 4.2f);
                AddMossPad(moss, new Vector3(x, y, z), new Vector2(Range(r, .5f, 1.1f), Range(r, .5f, 1.25f)), r);
            }
    }

    private static void AddMossPad(MeshBuilder b, Vector3 center, Vector2 size, System.Random r)
    {
        float x = size.x * .5f, z = size.y * .5f;
        Color c = new Color(.28f, .37f, .14f) * Range(r, .75f, 1.25f);
        b.AddQuad(center + new Vector3(-x, 0, -z), center + new Vector3(x, .03f, -z), center + new Vector3(x * .82f, .01f, z), center + new Vector3(-x * .84f, -.02f, z), c);
    }

    private static void AddSweptCurve(MeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        float startRadius, float endRadius, Color color, int rings, int sides)
    {
        Vector3[] previous = null;
        for (int ring = 0; ring <= rings; ring++)
        {
            float t = ring / (float)rings;
            Vector3 center = Bezier(p0, p1, p2, p3, t);
            Vector3 tangent = (Bezier(p0, p1, p2, p3, Mathf.Min(1f, t + .01f)) - Bezier(p0, p1, p2, p3, Mathf.Max(0f, t - .01f))).normalized;
            Vector3 refAxis = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .88f ? Vector3.forward : Vector3.up;
            Vector3 axisA = Vector3.Cross(tangent, refAxis).normalized;
            Vector3 axisB = Vector3.Cross(tangent, axisA).normalized;
            float radius = Mathf.Lerp(startRadius, endRadius, t) * (1f + Mathf.Sin(t * 18f) * .035f);
            Vector3[] current = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * (Mathf.PI * 2f / sides);
                current[side] = center + (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radius;
            }
            if (previous != null)
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Color c = color * (0.86f + .16f * Mathf.Sin(side * 2.1f));
                b.AddQuad(previous[side], previous[next], current[next], current[side], c);
            }
            previous = current;
        }
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    private static Color StoneColor(System.Random r) => new Color(Range(r, .31f, .47f), Range(r, .27f, .41f), Range(r, .21f, .34f));
    private static Color LeafColor(System.Random r) => new Color(Range(r, .14f, .27f), Range(r, .25f, .43f), Range(r, .07f, .16f));
    private static float Range(System.Random r, float min, float max) => (float)(min + r.NextDouble() * (max - min));

    private static Material CreateMaterial(string materialName, Color tint, string textureName, Color textureBase, float smoothness)
    {
        string materialPath = Generated + "/" + materialName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existing != null) return existing;
        Texture2D texture = CreateTexture(textureName, textureBase);
        AssetDatabase.CreateAsset(texture, Generated + "/" + textureName + ".asset");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("No URP Lit or Standard shader was found.");
        var material = new Material(shader) { name = materialName, enableInstancing = true };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static Texture2D CreateTexture(string textureName, Color baseColor)
    {
        string path = Generated + "/" + textureName + ".asset";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true) { name = textureName, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var pixels = new Color[size * size];
        int seed = 0;
        for (int i = 0; i < textureName.Length; i++) seed = unchecked(seed * 31 + textureName[i]);
        var random = new System.Random(seed);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float n = Mathf.PerlinNoise((x + seed) * .047f, (y - seed) * .047f) * .36f + Mathf.PerlinNoise((x + seed) * .013f, (y - seed) * .013f) * .5f;
            float grain = (float)random.NextDouble() * .14f;
            float vein = textureName.Contains("Leaf") ? Mathf.Clamp01(1f - Mathf.Abs(x - size * .5f) / 4f) * .18f : 0f;
            float crack = textureName.Contains("Stone") && (y % 31 < 2 || (x + y * 3) % 97 < 2) ? -.16f : 0f;
            float moss = textureName.Contains("Moss") ? Mathf.PerlinNoise(x * .08f, y * .08f) * .25f : 0f;
            float factor = .72f + n * .38f + grain + vein + crack + moss;
            pixels[y * size + x] = new Color(Mathf.Clamp01(baseColor.r * factor), Mathf.Clamp01(baseColor.g * factor), Mathf.Clamp01(baseColor.b * factor), 1f);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = Generated + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            throw new InvalidOperationException("Generated mesh path already exists; refusing to overwrite: " + path);
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private sealed class EnvironmentMeshes { public Mesh stone, roots, foliage, moss; }

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++) { normals.Add(normal); colors.Add(color); }
            float u = Mathf.Max(.05f, Vector3.Distance(a, b));
            float v = Mathf.Max(.05f, Vector3.Distance(a, d));
            uvs.Add(Vector2.zero); uvs.Add(new Vector2(u, 0f)); uvs.Add(new Vector2(u, v)); uvs.Add(new Vector2(0f, v));
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
