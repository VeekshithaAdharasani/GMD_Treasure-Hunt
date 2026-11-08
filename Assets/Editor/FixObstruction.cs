using UnityEditor;
using UnityEngine;

public static class FixObstruction
{
    [MenuItem("Tools/FixObstruction")]
    public static void Run()
    {
        string[] roots = new string[] {
            "Assets/Prefabs/Runner/Sections",
            "Assets/VisualPolish",
        };
        foreach (var root in roots)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[]{root}))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Pickups")) continue;
                if (path.Contains("TextMeshPro")) continue;
                var contents = PrefabUtility.LoadPrefabContents(path);
                if (contents == null) continue;

                // determine clearance z range
                float zMin = -8f, zMax = 28f;
                var section = contents.GetComponent<RunnerTrackSection>();
                if (section != null) zMax = section.sectionLength + 8f;

                Bounds clearance = new Bounds(new Vector3(0f, 3f, (zMin+zMax)*0.5f), new Vector3(12f, 8f, (zMax-zMin)));

                foreach (Transform child in contents.transform)
                {
                    if (child.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                        child.GetComponentsInChildren<Collider>(true).Length == 0 &&
                        child.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                        continue;
                    if (IsGameplay(child)) continue;
                    Bounds b = GetBounds(child);
                    if (!b.Intersects(clearance)) continue;

                    Vector3 lp = child.localPosition;
                    if (Mathf.Abs(lp.x) < 4.5f)
                        lp.x = lp.x >= 0f ? 5.5f : -5.5f;
                    if (lp.y > 4f)
                        lp.y += 2f;
                    child.localPosition = lp;
                    child.localScale = child.localScale * 0.9f;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("FixObstruction done");
    }

    private static bool IsGameplay(Transform t)
    {
        string n = t.name.ToLowerInvariant();
        if (n.Contains("track") || n.Contains("floor") || n.Contains("lane") ||
            n.Contains("obstacle") || n.Contains("collectible") || n.Contains("key") ||
            n.Contains("chest") || n.Contains("trigger") || n.Contains("player") ||
            n.Contains("anchor"))
            return true;
        if (t.GetComponent<Obstacle>() != null || t.GetComponent<Collectible>() != null ||
            t.GetComponent<TreasureKey>() != null || t.GetComponent<RunnerLaneHazard>() != null)
            return true;
        return false;
    }

    private static Bounds GetBounds(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(t.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        // convert to local-ish by using center relative to prefab root? bounds is world; prefab root at origin so ok
        return b;
    }
}