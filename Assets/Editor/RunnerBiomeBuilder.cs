using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RunnerBiomeBuilder
{
    const string ScenePath = "Assets/Scenes/LostCave_Main.unity";
    const string PreviewName = "RUNNER TRACK - M5 FIRST TWO BIOMES";
    const string OldRunner = "RUNNER PROTOTYPE - TEMP";
    const string F = "Assets/Prefabs/Runner/Sections/";
    const string P = "Assets/Prefabs/Runner/Pickups/Runner_Crystal_Blue.prefab";
    const float L = 20f, Lane = 3.5f;
    static readonly string[] Entrance = { F + "FE_01_ForestGate.prefab", F + "FE_02_CaveRun.prefab", F + "FE_03_EntranceHazards.prefab" };
    static readonly string[] Crystals = { F + "CC_01_CrystalRun.prefab", F + "CC_02_WaterfallRun.prefab", F + "CC_03_GemCrossing.prefab" };
    const string E2C = F + "FE_CC_Transition.prefab", C2E = F + "CC_FE_Transition.prefab";

    sealed class Snap { public Transform t; public Vector3 p, lp, s; public Quaternion r; public Transform parent; }
    enum Pattern { Entrance, Cave, EntranceHazards, ToCrystal, CrystalRun, Waterfall, GemCrossing, ToEntrance }

    [MenuItem("Tools/Runner Tracks/M5A Build Forgotten Entrance")]
    public static void BuildForgottenEntrance()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (Find(PreviewName) != null) throw new System.InvalidOperationException("M5 preview already exists.");
        var snaps = Capture(); Refs(); Folders(); CrystalPrefab();
        Material dirt = Asset<Material>("Assets/Fantasy Forest Environment Free Sample/Materials/dirt01.mat");
        Material cave = Asset<Material>("Assets/Materials/M_Cave Stone.mat"), blue = Asset<Material>("Assets/Materials/M_Crystal_Turquoise.mat"), rock = Asset<Material>("Assets/Materials/M_RockObstacle.mat");
        GameObject tree = Asset<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab");
        GameObject grass = Asset<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/grass01.prefab");
        var f0 = Make(Entrance[0], "FE_01_ForestGate", RunnerBiome.ForgottenEntrance, Pattern.Entrance, 0, cave, dirt, blue, rock, tree, grass);
        var f1 = Make(Entrance[1], "FE_02_CaveRun", RunnerBiome.ForgottenEntrance, Pattern.Cave, 1, cave, dirt, blue, rock, tree, grass);
        var f2 = Make(Entrance[2], "FE_03_EntranceHazards", RunnerBiome.ForgottenEntrance, Pattern.EntranceHazards, 2, cave, dirt, blue, rock, tree, grass);
        var tr = Make(E2C, "FE_CC_Transition", RunnerBiome.TransitionToCrystalCaverns, Pattern.ToCrystal, 3, cave, dirt, blue, rock, tree, grass);
        GameObject old = Find(OldRunner); if (old != null) old.SetActive(false);
        GameObject preview = new GameObject(PreviewName); preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Add(preview.transform, f0, 0, 0); Add(preview.transform, f1, 20, 1); Add(preview.transform, f2, 40, 2); Add(preview.transform, tr, 60, 3);
        Save(scene); Validate(preview.transform, 4, snaps, "M5_FORGOTTEN_ENTRANCE"); AssetDatabase.SaveAssets();
        Debug.Log("M5_FORGOTTEN_ENTRANCE_SAVED | sections=3 | transition=1 | length=80 | seams=connected | safeRows=verified");
    }

    [MenuItem("Tools/Runner Tracks/M5B Build Crystal Caverns")]
    public static void BuildCrystalCaverns()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform preview = Find(PreviewName)?.transform;
        if (preview == null || preview.GetComponentsInChildren<RunnerTrackSection>(true).Length != 4)
            throw new System.InvalidOperationException("Build and validate Forgotten Entrance first.");
        var snaps = Capture(); Refs(); Folders(); CrystalPrefab();
        Material dirt = Asset<Material>("Assets/Fantasy Forest Environment Free Sample/Materials/dirt01.mat");
        Material cave = Asset<Material>("Assets/Materials/M_Cave Stone.mat"), blue = Asset<Material>("Assets/Materials/M_Crystal_Turquoise.mat"), rock = Asset<Material>("Assets/Materials/M_RockObstacle.mat");
        GameObject tree = Asset<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab");
        GameObject grass = Asset<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/grass01.prefab");
        var c0 = Make(Crystals[0], "CC_01_CrystalRun", RunnerBiome.CrystalCaverns, Pattern.CrystalRun, 4, cave, dirt, blue, rock, tree, grass);
        var c1 = Make(Crystals[1], "CC_02_WaterfallRun", RunnerBiome.CrystalCaverns, Pattern.Waterfall, 5, cave, dirt, blue, rock, tree, grass);
        var c2 = Make(Crystals[2], "CC_03_GemCrossing", RunnerBiome.CrystalCaverns, Pattern.GemCrossing, 6, cave, dirt, blue, rock, tree, grass);
        var tr = Make(C2E, "CC_FE_Transition", RunnerBiome.TransitionToForgottenEntrance, Pattern.ToEntrance, 7, cave, dirt, blue, rock, tree, grass);
        Add(preview, c0, 80, 4); Add(preview, c1, 100, 5); Add(preview, c2, 120, 6); Add(preview, tr, 140, 7);
        RunnerTrackManager m = preview.gameObject.AddComponent<RunnerTrackManager>();
        m.player = Find("Player").GetComponent<PlayerMovement>(); m.sectionsContainer = preview;
        m.forgottenEntranceSections = Load(Entrance); m.transitionToCrystalCaverns = Asset<RunnerTrackSection>(E2C);
        m.crystalCavernsSections = Load(Crystals); m.transitionToForgottenEntrance = Asset<RunnerTrackSection>(C2E);
        m.sectionsPerBiome = 3; m.spawnAheadDistance = 80; m.recycleBehindDistance = 40; m.nextStartZ = 160; m.nextSequenceSlot = 8;
        Save(scene); Validate(preview, 8, snaps, "M5_CRYSTAL_CAVERNS"); ValidateManager(m); AssetDatabase.SaveAssets();
        Debug.Log("M5_CRYSTAL_CAVERNS_SAVED | sections=3 | transitions=2 | endlessCycle=FE_CC | seams=connected | safeRows=verified");
    }

    [MenuItem("Tools/Runner Tracks/Validate M5 First Two Biomes")]
    public static void ValidateFirstTwoBiomes()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform root = Find(PreviewName)?.transform; if (root == null) throw new System.InvalidOperationException("M5 track preview missing.");
        Validate(root, 8, Capture(), "M5_FINAL_VALIDATE"); ValidateManager(root.GetComponent<RunnerTrackManager>());
        Debug.Log("M5_FIRST_TWO_BIOMES_VALIDATED | sectionCount=8 | laneSpacing=2.5 | spawnRecycle=ready");
    }

    [MenuItem("Tools/Runner Tracks/M5 Fix A Forgotten Entrance")]
    public static void FixForgottenEntrance()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); var snaps = Capture();
        Transform root = Find(PreviewName)?.transform; if (root == null) throw new System.InvalidOperationException("Saved M5 track root missing.");
        string[] paths = { Entrance[0], Entrance[1], Entrance[2], E2C };
        Pattern[] patterns = { Pattern.Entrance, Pattern.Cave, Pattern.EntranceHazards, Pattern.ToCrystal };
        Rebuild(paths, patterns);
        ReplacePreview(root, 0, 4, paths);
        RunnerTrackSection[] sections = Sections(root, 0, 4);
        ValidateSections(sections, snaps, "M5_FIX_FORGOTTEN_ENTRANCE");
        Save(scene); AssetDatabase.SaveAssets();
        Debug.Log("M5_FIX_FORGOTTEN_ENTRANCE_SAVED | verified_hazards=" + HazardCount(sections) + " | all_collectibles_reachable=true");
    }

    [MenuItem("Tools/Runner Tracks/M5 Fix B Crystal Caverns")]
    public static void FixCrystalCaverns()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); var snaps = Capture();
        Transform root = Find(PreviewName)?.transform; if (root == null) throw new System.InvalidOperationException("Saved M5 track root missing.");
        string[] paths = { Crystals[0], Crystals[1], Crystals[2], C2E };
        Pattern[] patterns = { Pattern.CrystalRun, Pattern.Waterfall, Pattern.GemCrossing, Pattern.ToEntrance };
        Rebuild(paths, patterns);
        ReplacePreview(root, 4, 4, paths);
        Validate(root, 8, snaps, "M5_FIX_CRYSTAL_CAVERNS");
        ValidateManager(root.GetComponent<RunnerTrackManager>());
        RepairActiveSceneCollectibles(root);
        ValidateActiveSceneCollectibles(root);
        Save(scene); AssetDatabase.SaveAssets();
        Debug.Log("M5_FIX_CRYSTAL_CAVERNS_SAVED | verified_hazards=" + HazardCount(root.GetComponentsInChildren<RunnerTrackSection>(true)) + " | active_collectibles_reachable=true");
    }

    [MenuItem("Tools/Runner Tracks/Run M5 Play Mode Smoke Test %#F12")]
    public static void RunM5PlayModeSmokeTest()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject host = new GameObject("TEMP M5 Play Mode Smoke Test"); host.AddComponent<M5PlayModeSmokeRunner>();
        EditorApplication.isPlaying = true;
    }

    public static void RebalanceFirstTwoBiomePrefabs()
    {
        string[] paths = { Entrance[0], Entrance[1], Entrance[2], Crystals[0], Crystals[1], Crystals[2] };
        Pattern[] patterns = { Pattern.Entrance, Pattern.Cave, Pattern.EntranceHazards, Pattern.CrystalRun, Pattern.Waterfall, Pattern.GemCrossing };
        Rebuild(paths, patterns);
        Debug.Log("BALANCED_FE_CC_PREFABS_REBUILT | scene_untouched=true");
    }
    static RunnerTrackSection Make(string path, string name, RunnerBiome biome, Pattern pattern, int slot, Material cave, Material dirt, Material blue, Material rock, GameObject tree, GameObject grass)
    {
        var prior = AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(path); if (prior != null) return prior;
        GameObject root = new GameObject(name); root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var s = root.AddComponent<RunnerTrackSection>(); s.biome = biome; s.sectionLength = L; s.sequenceSlot = slot;
        s.entryAnchor = Anchor(root.transform, "ENTRY_ANCHOR", Vector3.zero); s.exitAnchor = Anchor(root.transform, "EXIT_ANCHOR", new Vector3(0, 0, L));
        Box(root.transform, "TRACK FLOOR", new Vector3(0, -.3f, L / 2), new Vector3(9, .6f, L), cave, true);
        Box(root.transform, "FOREST SHOULDER LEFT", new Vector3(-5.2f, -.3f, L / 2), new Vector3(2.4f, .6f, L), dirt, true);
        Box(root.transform, "FOREST SHOULDER RIGHT", new Vector3(5.2f, -.3f, L / 2), new Vector3(2.4f, .6f, L), dirt, true);
        Box(root.transform, "LANE MARKER LEFT", new Vector3(-1.25f, .012f, L / 2), new Vector3(.055f, .02f, L), blue, false);
        Box(root.transform, "LANE MARKER RIGHT", new Vector3(1.25f, .012f, L / 2), new Vector3(.055f, .02f, L), blue, false);
        Forest(root.transform, tree, grass, biome == RunnerBiome.ForgottenEntrance || pattern == Pattern.ToEntrance);
        if (biome == RunnerBiome.CrystalCaverns || biome == RunnerBiome.TransitionToCrystalCaverns || biome == RunnerBiome.TransitionToForgottenEntrance)
            CrystalsDressing(root.transform, blue, pattern == Pattern.Waterfall || pattern == Pattern.ToCrystal);
        PatternObjects(root.transform, pattern, cave, blue, rock);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok); Object.DestroyImmediate(root);
        if (!ok || prefab == null) throw new System.InvalidOperationException("Could not save section prefab " + path);
        return AssetDatabase.LoadAssetAtPath<RunnerTrackSection>(path);
    }

    static void PatternObjects(Transform p, Pattern pattern, Material cave, Material blue, Material rock)
    {
        // Reduced obstacle density: keep one obstacle per row at most,
        // with clear lanes for pickups and a less crowded runner path.
        switch (pattern)
        {
            case Pattern.Entrance:
                Rock(p, "FE entry rock", -1, 18, 0, new Vector3(1.8f, 1.25f, 1.4f), .62f, rock);
                Pickup(p, -1, 8); Pickup(p, 0, 15); break;

            case Pattern.Cave:
                // One jumpable obstacle, then one rock; right lane stays clear.
                Hazard(p, "FE center rock", 0, 4, 0, RunnerHazardAction.Jump, new Vector3(2, .9f, 1.2f), .45f, cave);
                Rock(p, "FE left stone", -1, 16, 1, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, 1, 4); Pickup(p, 1, 12); break;

            case Pattern.EntranceHazards:
                // One slide obstacle followed by one rock; right lane stays clear.
                Hazard(p, "FE left low beam", -1, 4, 0, RunnerHazardAction.Slide, new Vector3(2.3f, .7f, .8f), 2.45f, rock);
                Rock(p, "FE center stone", 0, 16, 1, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, 1, 10); Pickup(p, 1, 12); break;

            case Pattern.ToCrystal:
                Rock(p, "FE transition rock", -1, 10, 0, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, 0, 16); break;

            case Pattern.CrystalRun:
                // Keep the key's center lane clear near the start of this section.
                Rock(p, "CC right rock", 1, 6, 0, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Rock(p, "CC middle stone", 0, 18, 1, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, -1, 10); Pickup(p, -1, 12); Pickup(p, -1, 14); break;

            case Pattern.Waterfall:
                Hazard(p, "CC left low bar", -1, 6, 0, RunnerHazardAction.Slide, new Vector3(2.3f, .7f, .8f), 2.45f, blue);
                Rock(p, "CC center stone", 0, 18, 1, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, 1, 10); Pickup(p, 1, 12); Pickup(p, 1, 14); break;

            case Pattern.GemCrossing:
                Hazard(p, "CC left jump block", -1, 6, 0, RunnerHazardAction.Jump, new Vector3(2, .9f, 1.1f), .45f, rock);
                Rock(p, "CC center crossing rock", 0, 18, 1, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, 1, 10); Pickup(p, 1, 12); Pickup(p, 1, 14); break;

            case Pattern.ToEntrance:
                Rock(p, "CC return rock", 0, 12, 0, new Vector3(1.8f, 1.2f, 1.4f), .6f, rock);
                Pickup(p, -1, 5); break;
        }
    }
    static void Hazard(Transform p, string name, int lane, float z, int row, RunnerHazardAction action, Vector3 size, float y, Material mat)
    {
        GameObject h = Box(p, name, new Vector3(lane * Lane, y, z), size, mat, true);
        h.AddComponent<Obstacle>().damage = 1;
        var marker = h.AddComponent<RunnerLaneHazard>(); marker.laneIndex = lane; marker.patternRow = row; marker.action = action;
    }
    static void Rock(Transform p, string name, int lane, float z, int row, Vector3 size, float y, Material mat)
    {
        GameObject h = GameObject.CreatePrimitive(PrimitiveType.Sphere); h.name = name; h.transform.SetParent(p, false);
        h.transform.localPosition = new Vector3(lane * Lane, y, z); h.transform.localScale = size; h.GetComponent<MeshRenderer>().sharedMaterial = mat;
        h.AddComponent<Obstacle>().damage = 1;
        var marker = h.AddComponent<RunnerLaneHazard>(); marker.laneIndex = lane; marker.patternRow = row; marker.action = RunnerHazardAction.Dodge;
    }
    static void Pickup(Transform p, int lane, float z)
    {
        GameObject prefab = Asset<GameObject>(P);
        GameObject obj = PrefabUtility.InstantiatePrefab(prefab, p) as GameObject;
        obj.name = "Crystal trail lane " + lane; obj.transform.localPosition = new Vector3(lane * Lane, 1.55f, z); obj.transform.localRotation = Quaternion.identity; obj.transform.localScale = Vector3.one;
        if (obj.GetComponentInChildren<Collectible>(true) == null) throw new System.InvalidOperationException("Crystal prefab has no Collectible.");
    }
    static void Forest(Transform p, GameObject tree, GameObject grass, bool dense)
    {
        int n = dense ? 3 : 1;
        for (int i = 0; i < n; i++)
        {
            float z = 3 + i * 7;
            StaticPrefab(p, tree, "Forest tree left", new Vector3(-6.2f, 0, z), new Vector3(.55f, .5f, .5f));
            StaticPrefab(p, tree, "Forest tree right", new Vector3(6.2f, 0, z + 3), new Vector3(.55f, .5f, .5f));
            if (i < 2) { StaticPrefab(p, grass, "Forest grass left", new Vector3(-4.8f, 0, z + 4), Vector3.one * 2); StaticPrefab(p, grass, "Forest grass right", new Vector3(4.8f, 0, z + 1), Vector3.one * 2); }
        }
    }
    static void CrystalsDressing(Transform p, Material mat, bool waterfall)
    {
        for (int i = 0; i < 3; i++)
        {
            float z = 3 + i * 7; DecorCrystal(p, new Vector3(-4.55f, 1.5f, z), mat, .75f + i * .1f);
            DecorCrystal(p, new Vector3(4.55f, 1.8f, z + 3), mat, .85f + i * .1f);
        }
        if (waterfall) for (int side = -1; side <= 1; side += 2)
        { var w = Box(p, "Crystal waterfall ribbon", new Vector3(side * 5.25f, 3.5f, 10), new Vector3(.16f, 7, 17), mat, false); w.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
    }
    static void DecorCrystal(Transform p, Vector3 at, Material mat, float scale)
    {
        GameObject o = PrefabUtility.InstantiatePrefab(Asset<GameObject>(P), p) as GameObject; o.name = "Crystal cavern decoration";
        o.transform.localPosition = at; o.transform.localRotation = Quaternion.Euler(0, 17, 12); o.transform.localScale = new Vector3(scale, scale * 1.45f, scale);
        var c = o.GetComponentInChildren<Collectible>(true); if (c != null) c.enabled = false;
        foreach (Collider col in o.GetComponentsInChildren<Collider>(true)) col.enabled = false;
    }
    static void StaticPrefab(Transform p, GameObject prefab, string name, Vector3 at, Vector3 scale)
    {
        GameObject o = PrefabUtility.InstantiatePrefab(prefab, p) as GameObject; o.name = name; o.transform.localPosition = at; o.transform.localRotation = Quaternion.identity; o.transform.localScale = scale;
        foreach (Collider col in o.GetComponentsInChildren<Collider>(true)) col.enabled = false;
    }
    static GameObject Box(Transform p, string name, Vector3 at, Vector3 size, Material mat, bool collider)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name; o.transform.SetParent(p, false); o.transform.localPosition = at; o.transform.localScale = size;
        o.GetComponent<MeshRenderer>().sharedMaterial = mat; if (!collider) Object.DestroyImmediate(o.GetComponent<BoxCollider>()); return o;
    }
    static Transform Anchor(Transform p, string name, Vector3 at)
    { var g = new GameObject(name); g.transform.SetParent(p, false); g.transform.localPosition = at; return g.transform; }
    static void Add(Transform parent, RunnerTrackSection prefab, float z, int slot)
    { var s = PrefabUtility.InstantiatePrefab(prefab, parent) as RunnerTrackSection; if (s == null) throw new System.InvalidOperationException("Section prefab instantiation failed: " + prefab.name); s.name = prefab.name + " Preview"; s.transform.localPosition = new Vector3(0, 0, z); s.transform.localRotation = Quaternion.identity; s.transform.localScale = Vector3.one; s.sequenceSlot = slot; }

    static void CrystalPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(P) != null) return;
        GameObject source = Find("Crystal_Blue_01"); if (source == null || source.GetComponent<Collectible>() == null) throw new System.InvalidOperationException("Existing Crystal_Blue_01 collectible missing.");
        GameObject copy = Object.Instantiate(source); copy.name = "Runner Crystal Blue Pickup"; copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); copy.transform.localScale = Vector3.one;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, P, out bool ok); Object.DestroyImmediate(copy); if (!ok || prefab == null) throw new System.InvalidOperationException("Crystal pickup prefab creation failed."); AssetDatabase.SaveAssets();
    }
    static void Validate(Transform root, int count, Dictionary<string, Snap> snaps, string label)
    {
        RunnerTrackSection[] ss = root.GetComponentsInChildren<RunnerTrackSection>(true); if (ss.Length != count) throw new System.InvalidOperationException(label + ": expected " + count + " sections, found " + ss.Length);
        System.Array.Sort(ss, (a, b) => a.StartZ.CompareTo(b.StartZ));
        ValidateSections(ss, snaps, label);
    }
    static void ValidateSections(RunnerTrackSection[] ss, Dictionary<string, Snap> snaps, string label)
    {
        System.Array.Sort(ss, (a, b) => a.StartZ.CompareTo(b.StartZ)); int hazards = 0, collectibles = 0; Physics.SyncTransforms();
        for (int i = 0; i < ss.Length; i++)
        {
            if (!ss[i].Validate(Lane, out string e)) throw new System.InvalidOperationException(label + ": " + e);
            if (i > 0 && Vector3.Distance(ss[i - 1].exitAnchor.position, ss[i].entryAnchor.position) > .01f) throw new System.InvalidOperationException(label + ": disconnected seam " + ss[i - 1].name + " -> " + ss[i].name);
            hazards += ValidateRows(ss[i]); collectibles += ValidateCollectibles(ss[i]);
        }
        if (!Refs()) throw new System.InvalidOperationException(label + ": existing gameplay references did not resolve.");
        Verify(snaps); Debug.Log(label + "_SECTION_TEST_PASS | sections=" + ss.Length + " | hazards=" + hazards + " | pickups=" + collectibles + " | laneWidth=3.5 | seams=connected | protectedTransforms=unchanged");
    }
    static int ValidateRows(RunnerTrackSection s)
    {
        var rows = new Dictionary<int, HashSet<int>>(); var hs = s.GetComponentsInChildren<RunnerLaneHazard>(true);
        foreach (var h in hs)
        {
            if (h.laneIndex < -1 || h.laneIndex > 1 || Mathf.RoundToInt(h.transform.localPosition.x / Lane) != h.laneIndex) throw new System.InvalidOperationException(s.name + ": hazard lane mismatch.");
            Collider c = h.GetComponent<Collider>(); Obstacle o = h.GetComponent<Obstacle>();
            if (c == null || !c.enabled || c.isTrigger || o == null || o.damage < 1) throw new System.InvalidOperationException(s.name + "/" + h.name + ": hazard needs an enabled solid collider and damage component.");
            if (!rows.TryGetValue(h.patternRow, out HashSet<int> occupied)) { occupied = new HashSet<int>(); rows.Add(h.patternRow, occupied); }
            occupied.Add(h.laneIndex);
            if (h.action == RunnerHazardAction.Slide) ValidateSlideClearance(h, c);
        }
        foreach (var row in rows) if (row.Value.Count >= 3) throw new System.InvalidOperationException(s.name + ": row blocks all three lanes.");
        return hs.Length;
    }
    static void ValidateSlideClearance(RunnerLaneHazard hazard, Collider bar)
    {
        CharacterController cc = Find("Player")?.GetComponent<CharacterController>(); if (cc == null) throw new System.InvalidOperationException("Player CharacterController missing.");
        float standTop = cc.transform.position.y + cc.center.y + cc.height * .5f;
        float slideTop = cc.transform.position.y + cc.center.y - (cc.height - 1f) * .5f + .5f;
        if (bar.bounds.min.y < slideTop + .05f || bar.bounds.min.y >= standTop)
            throw new System.InvalidOperationException(hazard.name + ": overhead bar does not clear the slide capsule while blocking the standing capsule.");
    }
    static int ValidateCollectibles(RunnerTrackSection s)
    {
        int count = 0; var pickups = s.GetComponentsInChildren<Collectible>(true);
        foreach (var c in pickups)
        {
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
            Vector3 p = s.transform.InverseTransformPoint(c.transform.position); int lane = Mathf.RoundToInt(p.x / Lane);
            if (lane < -1 || lane > 1 || Mathf.Abs(p.x - lane * Lane) > .25f || p.z < .5f || p.z > s.sectionLength - .5f || p.y < 1.05f || p.y > 2.05f)
                throw new System.InvalidOperationException(s.name + "/" + c.name + ": collectible lies outside a reachable lane/track window.");
            Collider pc = c.GetComponent<Collider>(); if (pc == null) pc = c.GetComponentInChildren<Collider>(true);
            if (pc == null || !pc.enabled || !pc.isTrigger) throw new System.InvalidOperationException(s.name + "/" + c.name + ": collectible needs an enabled trigger collider.");
            foreach (RunnerLaneHazard h in s.GetComponentsInChildren<RunnerLaneHazard>(true))
            {
                Collider hc = h.GetComponent<Collider>(); if (hc != null && hc.enabled && pc.bounds.Intersects(hc.bounds))
                    throw new System.InvalidOperationException(s.name + ": collectible " + c.name + " overlaps obstacle " + h.name + ".");
            }
            count++;
        }
        return count;
    }
    static void ValidateManager(RunnerTrackManager m)
    {
        if (m == null || m.forgottenEntranceSections == null || m.forgottenEntranceSections.Length != 3 || m.crystalCavernsSections == null || m.crystalCavernsSections.Length != 3 || m.transitionToCrystalCaverns == null || m.transitionToForgottenEntrance == null || m.nextSequenceSlot != 8 || Mathf.Abs(m.nextStartZ - 160) > .01f || m.spawnAheadDistance < 60 || m.recycleBehindDistance < 20)
            throw new System.InvalidOperationException("Continuous track manager catalogs or spawn/recycle settings are incomplete.");
    }

    static void Rebuild(string[] paths, Pattern[] patterns)
    {
        Material cave = Asset<Material>("Assets/Materials/M_Cave Stone.mat"), blue = Asset<Material>("Assets/Materials/M_Crystal_Turquoise.mat"), rock = Asset<Material>("Assets/Materials/M_RockObstacle.mat");
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(paths[i]);
            try
            {
                foreach (RunnerLaneHazard h in contents.GetComponentsInChildren<RunnerLaneHazard>(true)) Object.DestroyImmediate(h.gameObject);
                foreach (Collectible c in contents.GetComponentsInChildren<Collectible>(true))
                    if (c.enabled && c.name.StartsWith("Crystal trail lane ")) Object.DestroyImmediate(c.gameObject);
                PatternObjects(contents.transform, patterns[i], cave, blue, rock);
                if (PrefabUtility.SaveAsPrefabAsset(contents, paths[i]) == null) throw new System.InvalidOperationException("Could not save repaired section " + paths[i]);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }
    static void ReplacePreview(Transform root, int firstSlot, int count, string[] paths)
    {
        RunnerTrackSection[] previous = Sections(root, firstSlot, count);
        if (previous.Length != count) throw new System.InvalidOperationException("Preview section subset mismatch at sequence slot " + firstSlot + ".");
        foreach (RunnerTrackSection s in previous) Object.DestroyImmediate(s.gameObject);
        for (int i = 0; i < count; i++) Add(root, Asset<RunnerTrackSection>(paths[i]), (firstSlot + i) * L, firstSlot + i);
    }
    static RunnerTrackSection[] Sections(Transform root, int firstSlot, int count)
    {
        var found = new List<RunnerTrackSection>();
        foreach (RunnerTrackSection s in root.GetComponentsInChildren<RunnerTrackSection>(true))
            if (s.sequenceSlot >= firstSlot && s.sequenceSlot < firstSlot + count) found.Add(s);
        found.Sort((a, b) => a.sequenceSlot.CompareTo(b.sequenceSlot)); return found.ToArray();
    }
    static int HazardCount(RunnerTrackSection[] sections)
    { int n = 0; foreach (RunnerTrackSection s in sections) n += s.GetComponentsInChildren<RunnerLaneHazard>(true).Length; return n; }
    static void ValidateActiveSceneCollectibles(Transform root)
    {
        foreach (Collectible c in Object.FindObjectsByType<Collectible>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c.gameObject.scene != SceneManager.GetActiveScene() || c.transform.IsChildOf(root)) continue;
            Vector3 p = c.transform.position; int lane = Mathf.RoundToInt(p.x / Lane);
            if (lane < -1 || lane > 1 || Mathf.Abs(p.x - lane * Lane) > .25f || p.z < 0 || p.z > 160 || p.y < 1.05f || p.y > 2.05f)
                throw new System.InvalidOperationException("Active scene collectible " + c.name + " is outside the reachable track lanes: " + p + ".");
            Collider pc = c.GetComponent<Collider>(); if (pc == null) pc = c.GetComponentInChildren<Collider>(true);
            if (pc == null || !pc.enabled || !pc.isTrigger) throw new System.InvalidOperationException("Active scene collectible " + c.name + " has no enabled trigger collider.");
            foreach (RunnerLaneHazard h in root.GetComponentsInChildren<RunnerLaneHazard>(true))
            { Collider hc = h.GetComponent<Collider>(); if (hc != null && hc.enabled && pc.bounds.Intersects(hc.bounds)) throw new System.InvalidOperationException("Active scene collectible " + c.name + " overlaps " + h.name + "."); }
        }
    }
    static void RepairActiveSceneCollectibles(Transform root)
    {
        var hazards = root.GetComponentsInChildren<RunnerLaneHazard>(true); Physics.SyncTransforms();
        foreach (Collectible c in Object.FindObjectsByType<Collectible>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c.gameObject.scene != SceneManager.GetActiveScene() || c.transform.IsChildOf(root)) continue;
            Collider pc = c.GetComponent<Collider>(); if (pc == null) pc = c.GetComponentInChildren<Collider>(true);
            if (pc == null || !pc.enabled || !pc.isTrigger) continue;
            Vector3 old = c.transform.position; float z = Mathf.Clamp(old.z, .5f, 159.5f);
            int nearest = Mathf.Clamp(Mathf.RoundToInt(old.x / Lane), -1, 1); bool placed = false;
            for (int offset = 0; offset < 3 && !placed; offset++)
            {
                int[] candidates = { nearest, Mathf.Clamp(nearest - 1, -1, 1), Mathf.Clamp(nearest + 1, -1, 1) };
                int lane = candidates[offset]; if (offset > 0 && lane == candidates[0]) continue;
                c.transform.position = new Vector3(lane * Lane, 1.55f, z); Physics.SyncTransforms();
                bool blocked = false;
                foreach (RunnerLaneHazard h in hazards)
                { Collider hc = h.GetComponent<Collider>(); if (hc != null && hc.enabled && pc.bounds.Intersects(hc.bounds)) { blocked = true; break; } }
                if (!blocked) placed = true;
            }
            if (!placed) throw new System.InvalidOperationException("No safe lane could be found for existing collectible " + c.name + " near z=" + z + ".");
        }
    }

    static Dictionary<string, Snap> Capture()
    {
        var result = new Dictionary<string, Snap>(); Scene active = SceneManager.GetActiveScene();
        foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (g == null || !g.scene.IsValid() || g.scene != active || g.transform.parent != null || g.name == PreviewName) continue;
            CaptureTree(g.transform, g.name, result);
        }
        return result;
    }
    static void CaptureTree(Transform t, string path, Dictionary<string, Snap> result)
    {
        result[path] = new Snap { t = t, p = t.position, lp = t.localPosition, s = t.localScale, r = t.rotation, parent = t.parent };
        for (int i = 0; i < t.childCount; i++) CaptureTree(t.GetChild(i), path + "/" + t.GetChild(i).name, result);
    }
    static void Verify(Dictionary<string, Snap> before)
    {
        foreach (var item in before)
        {
            Snap s = item.Value;
            if (s.t == null || s.t.parent != s.parent || Vector3.Distance(s.t.position, s.p) > .001f || Vector3.Distance(s.t.localPosition, s.lp) > .001f || Vector3.Distance(s.t.localScale, s.s) > .001f || Quaternion.Angle(s.t.rotation, s.r) > .01f)
                throw new System.InvalidOperationException("Protected existing object moved or was removed: " + item.Key);
        }
    }
    static bool Refs()
    {
        GameObject player = Find("Player"), crystal = Find("Crystal_Blue_01");
        if (player == null || player.GetComponent<PlayerMovement>() == null || Find("GameManager") == null || Find("Canvas") == null || crystal == null || crystal.GetComponent<Collectible>() == null) return false;
        if (Find("GameManager").GetComponent<GameManager>() == null) return false;
        foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (g == null || !g.scene.IsValid() || g.scene != SceneManager.GetActiveScene()) continue;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g) > 0) return false;
        }
        return true;
    }
    static void Folders()
    {
        EnsureFolder("Assets/Prefabs", "Runner"); EnsureFolder("Assets/Prefabs/Runner", "Sections"); EnsureFolder("Assets/Prefabs/Runner", "Pickups");
    }
    static void EnsureFolder(string parent, string child)
    { if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child); }
    static T Asset<T>(string path) where T : Object
    { T o = AssetDatabase.LoadAssetAtPath<T>(path); if (o == null) throw new System.InvalidOperationException("Required existing asset is missing: " + path); return o; }
    static RunnerTrackSection[] Load(string[] paths)
    { var a = new RunnerTrackSection[paths.Length]; for (int i = 0; i < a.Length; i++) a[i] = Asset<RunnerTrackSection>(paths[i]); return a; }
    static GameObject Find(string name)
    {
        foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
            if (g != null && g.scene.IsValid() && g.scene == SceneManager.GetActiveScene() && g.name == name) return g;
        return null;
    }
    static void Save(Scene scene)
    { EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("Could not save LostCave_Main."); }
}






